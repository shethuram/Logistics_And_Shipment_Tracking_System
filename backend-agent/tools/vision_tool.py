import base64
import httpx
import json
from anthropic import AsyncAnthropic
from config import settings
from schemas.verification import ExtractedLicenseData

async def extract_license_data(image_url: str) -> ExtractedLicenseData:
    async with httpx.AsyncClient() as client:
        resp = await client.get(image_url)
        if resp.status_code != 200:
            raise Exception(f"Failed to download license image from {image_url}")
        image_bytes = resp.content
        media_type = resp.headers.get("content-type", "image/jpeg")

    base64_image = base64.b64encode(image_bytes).decode("utf-8")

    with open("prompts/vision_prompt.txt", "r") as f:
        prompt_content = f.read()

    # Initialize client
    api_key = settings.ANTHROPIC_AUTH_TOKEN or settings.ANTHROPIC_API_KEY
    base_url = settings.ANTHROPIC_BASE_URL or None

    client_args = {"api_key": api_key}
    if base_url:
        client_args["base_url"] = base_url

    client = AsyncAnthropic(**client_args)
    model_name = settings.ANTHROPIC_MODEL

    message = await client.messages.create(
        model=model_name,
        max_tokens=1000,
        temperature=0.0,
        system="You are a strict data extraction system. Return ONLY valid JSON matching the schema, with no conversational wrapping or markdown formatting.",
        messages=[
            {
                "role": "user",
                "content": [
                    {
                        "type": "image",
                        "source": {
                            "type": "base64",
                            "media_type": media_type,
                            "data": base64_image
                        }
                    },
                    {
                        "type": "text",
                        "text": prompt_content
                    }
                ]
            }
        ]
    )

    raw_content = message.content[0].text
    
    # Strip markdown block formatting if LLM includes it
    if raw_content.startswith("```json"):
        raw_content = raw_content[7:]
    if raw_content.endswith("```"):
        raw_content = raw_content[:-3]
    raw_content = raw_content.strip()

    data = json.loads(raw_content)
    return ExtractedLicenseData(**data)
