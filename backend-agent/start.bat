@echo off
set ANTHROPIC_BASE_URL=https://proxy.llm-gateway.ready.presidio.com
set ANTHROPIC_AUTH_TOKEN=sk-oOweMpLodbE7u3BqSrvAcg
set ANTHROPIC_MODEL=claude-sonnet-4-6
set AGENT_API_KEY=SecretAgentKey123!
"%~dp0venv\Scripts\python" main.py
