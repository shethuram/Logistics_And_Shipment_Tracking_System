import httpx
import json
import os
from datetime import datetime
from config import settings
from tools.vision_tool import extract_license_data
from tools.registry_tool import lookup_license_in_registry
from schemas.agent_tools import tools_schema
from utils.logger_config import logger
from anthropic import AsyncAnthropic

async def run_verification(driver_id: str, image_url: str, form_name: str, form_license_no: str):
    logger.info(f"Starting verification agent execution for Driver ID: {driver_id}")
    
    api_key = settings.ANTHROPIC_AUTH_TOKEN
    base_url = settings.ANTHROPIC_BASE_URL or None

    client_args = {"api_key": api_key}
    if base_url:
        client_args["base_url"] = base_url

    client = AsyncAnthropic(**client_args)

    # Load system prompt from modular prompt file
    prompt_path = os.path.join("prompts", "agent_system_prompt.txt")
    try:
        with open(prompt_path, "r") as f:
            base_system_prompt = f.read()
    except Exception as e:
        logger.error(f"Failed to read agent system prompt file: {str(e)}")
        await callback_to_core(driver_id, "AGENT_FAILED", {
            "error": f"Failed to load system prompt: {str(e)}"
        })
        return

    current_date_str = datetime.utcnow().strftime("%Y-%m-%d")
    system_prompt = f"{base_system_prompt}\nToday's date is: {current_date_str}.\nForm Name: {form_name}.\nForm License Number: {form_license_no}."

    user_prompt = f"Verify driver: {driver_id} using image URL: {image_url}."

    messages = [
        {"role": "user", "content": user_prompt}
    ]

    try:
        max_iterations = 10
        for i in range(max_iterations):
            logger.info(f"Starting agent execution iteration {i+1}")
            response = await client.messages.create(
                model=settings.ANTHROPIC_MODEL,
                max_tokens=1500,
                temperature=0.0,
                system=system_prompt,
                tools=tools_schema,
                messages=messages
            )

            messages.append({
                "role": "assistant",
                "content": response.content
            })

            # Log Claude's text reasoning blocks (thoughts)
            for content_block in response.content:
                if content_block.type == "text":
                    logger.info(f"[Agent Thought] {content_block.text.strip()}")

            if response.stop_reason != "tool_use":
                logger.info("Agent finished execution loop with final completion.")
                break

            # Handle tool invocation blocks
            for content_block in response.content:
                if content_block.type == "tool_use":
                    tool_name = content_block.name
                    tool_input = content_block.input
                    tool_use_id = content_block.id

                    logger.info(f"[Agent Tool Call] Invoking {tool_name} with arguments: {tool_input}")

                    try:
                        if tool_name == "extract_license_data":
                            extracted = await extract_license_data(tool_input["image_url"])
                            result = {
                                "name": extracted.name,
                                "license_number": extracted.license_number,
                                "expiry_date": extracted.expiry_date,
                                "classes": extracted.classes,
                                "confidence_score": extracted.confidence_score
                            }
                        elif tool_name == "lookup_license_in_registry":
                            result = lookup_license_in_registry(tool_input["license_number"]) or {}
                        elif tool_name == "submit_verification_result":
                            result = await callback_to_core(
                                driver_id=driver_id,
                                status=tool_input["status"],
                                report=tool_input["report"],
                                classes=tool_input["license_classes"],
                                allowed_vehicles=tool_input["allowed_vehicle_types"]
                            )
                        else:
                            result = {"error": f"Unknown tool: {tool_name}"}
                    except Exception as ex:
                        logger.error(f"Execution of tool {tool_name} failed: {str(ex)}")
                        result = {"error": f"Tool execution failed: {str(ex)}"}

                    logger.info(f"[Agent Tool Result] {tool_name} returned: {result}")

                    messages.append({
                        "role": "user",
                        "content": [
                            {
                                "type": "tool_result",
                                "tool_use_id": tool_use_id,
                                "content": json.dumps(result)
                            }
                        ]
                    })
    except Exception as e:
        logger.error(f"Agent loop runtime exception: {str(e)}")
        await callback_to_core(driver_id, "AGENT_FAILED", {
            "error": f"Agent loop execution failed: {str(e)}"
        })

async def callback_to_core(driver_id: str, status: str, report: dict, classes: list = None, allowed_vehicles: list = None):
    url = f"{settings.CORE_API_URL}/api/admin/drivers/{driver_id}/verification"
    logger.info(f"Submitting verification callback to C# Backend. Status: {status}")
    
    headers = {
        "Authorization": f"Bearer {settings.AGENT_API_KEY}",
        "Content-Type": "application/json"
    }
    
    payload = {
        "verificationStatus": status,
        "verificationReport": json.dumps(report),
        "licenseClasses": classes or [],
        "allowedVehicleTypes": allowed_vehicles or []
    }
    
    async with httpx.AsyncClient() as client:
        try:
            resp = await client.patch(url, json=payload, headers=headers, timeout=10.0)
            if resp.status_code == 200:
                logger.info("Verification callback completed successfully on C# backend.")
                return {"status": 200, "message": "Verification callback completed successfully."}
            else:
                logger.error(f"Core API callback failed. Status: {resp.status_code}, Body: {resp.text}")
                return {"status": resp.status_code, "error": resp.text}
        except Exception as e:
            logger.error(f"Exception calling core API callback: {str(e)}")
            return {"status": 500, "error": str(e)}
