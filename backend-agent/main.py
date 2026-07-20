import os
import sys

# Ensure bundled site-packages are in sys.path for Azure App Service
site_packages = os.path.join(os.path.dirname(__file__), ".python_packages", "lib", "site-packages")
if os.path.exists(site_packages) and site_packages not in sys.path:
    sys.path.insert(0, site_packages)

import json
import asyncio
import threading
from flask import Flask, request, jsonify
from agents.verification_agent import run_verification

if sys.platform.startswith('win'):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    except Exception:
        pass

app = Flask(__name__)

@app.route("/health", methods=["GET"])
def health():
    return jsonify({"status": "healthy"})

@app.route("/api/agent/verify", methods=["POST"])
def trigger_verify():
    data = request.get_json() or {}
    
    driver_id = data.get("driverId")
    image_url = data.get("imageUrl")
    full_name = data.get("fullName")
    license_number = data.get("licenseNumber")
    
    if not all([driver_id, image_url, full_name, license_number]):
        return jsonify({"error": "Missing required fields"}), 400

    # Run in background thread to avoid blocking response
    thread = threading.Thread(
        target=run_async_verification,
        args=(driver_id, image_url, full_name, license_number)
    )
    thread.daemon = True
    thread.start()

    return jsonify({"message": "AI verification task accepted and queued."})

def run_async_verification(driver_id, image_url, full_name, license_number):
    asyncio.run(run_verification(driver_id, image_url, full_name, license_number))

if __name__ == "__main__":
    app.run(host="127.0.0.1", port=8000)
