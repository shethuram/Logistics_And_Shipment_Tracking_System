import os

class Settings:
    ANTHROPIC_API_KEY: str = os.getenv("ANTHROPIC_API_KEY", "")
    ANTHROPIC_BASE_URL: str = os.getenv("ANTHROPIC_BASE_URL", "")
    ANTHROPIC_AUTH_TOKEN: str = os.getenv("ANTHROPIC_AUTH_TOKEN", "")
    ANTHROPIC_MODEL: str = os.getenv("ANTHROPIC_MODEL", "claude-sonnet-4-6")
    AGENT_API_KEY: str = os.getenv("AGENT_API_KEY", "PLACEHOLDER_AGENT_API_KEY")
    CORE_API_URL: str = os.getenv("CORE_API_URL", "http://localhost:5286")

settings = Settings()
