import json
from typing import Optional, Dict, Any

def lookup_license_in_registry(license_number: str) -> Optional[Dict[str, Any]]:
    # Normalize the license number format for matching (strip spaces and uppercase)
    normalized_input = license_number.replace(" ", "").upper()
    
    try:
        with open("registry-db.json", "r") as f:
            records = json.load(f)
            
        for record in records:
            normalized_record = record.get("LicenseNumber", "").replace(" ", "").upper()
            if normalized_record == normalized_input:
                return record
    except Exception:
        pass
        
    return None
