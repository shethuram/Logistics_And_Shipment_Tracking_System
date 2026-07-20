import os

class Settings:
    ANTHROPIC_API_KEY: str = os.getenv("ANTHROPIC_API_KEY", "")
    ANTHROPIC_BASE_URL: str = os.getenv("ANTHROPIC_BASE_URL", "")
    ANTHROPIC_AUTH_TOKEN: str = os.getenv("ANTHROPIC_AUTH_TOKEN", "")
    ANTHROPIC_MODEL: str = os.getenv("ANTHROPIC_MODEL", "claude-sonnet-4-6")
    AGENT_API_KEY: str = os.getenv("AGENT_API_KEY", "PLACEHOLDER_AGENT_API_KEY")
    CORE_API_URL: str = os.getenv("CORE_API_URL", "http://localhost:5286")

    def __init__(self):
        kv_name = os.getenv("KEY_VAULT_NAME", "")
        if kv_name:
            try:
                from azure.identity import DefaultAzureCredential
                from azure.keyvault.secrets import SecretClient
                vault_url = f"https://{kv_name}.vault.azure.net/"
                client = SecretClient(vault_url=vault_url, credential=DefaultAzureCredential())
                
                try:
                    sec = client.get_secret("LlmSettings--ApiKey")
                    if sec and sec.value:
                        self.ANTHROPIC_API_KEY = sec.value
                except Exception:
                    pass
                try:
                    sec = client.get_secret("AgentSettings--ApiKey")
                    if sec and sec.value:
                        self.AGENT_API_KEY = sec.value
                except Exception:
                    pass
            except Exception as e:
                print(f"Key Vault integration skipped or failed: {e}")

settings = Settings()
