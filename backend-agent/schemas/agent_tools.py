tools_schema = [
    {
        "name": "extract_license_data",
        "description": "Reads the driver's license card from the provided public image URL, downloads the image file, and extracts the Name, License Number, Expiry Date (YYYY-MM-DD), and License Classes (e.g. ['LMV', 'MCWG']).",
        "input_schema": {
            "type": "object",
            "properties": {
                "image_url": {
                    "type": "string",
                    "description": "The public storage URL of the driver's uploaded license image."
                }
            },
            "required": ["image_url"]
        }
    },
    {
        "name": "lookup_license_in_registry",
        "description": "Queries the mock government registry database to cross-reference the extracted driver's license number. Returns registry details (FullName, ExpiryDate, Status, Classes) or null if the license does not exist.",
        "input_schema": {
            "type": "object",
            "properties": {
                "license_number": {
                    "type": "string",
                    "description": "The exact license number extracted from the card."
                }
            },
            "required": ["license_number"]
        }
    },
    {
        "name": "submit_verification_result",
        "description": "Submits the final verification report and decisions back to the main C# API via callback.",
        "input_schema": {
            "type": "object",
            "properties": {
                "status": {
                    "type": "string",
                    "enum": ["AI_VERIFIED", "AI_FLAGGED"],
                    "description": "The final decision status."
                },
                "report": {
                    "type": "object",
                    "description": "The detailed comparison report JSON object."
                },
                "license_classes": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of license classes extracted from the card."
                },
                "allowed_vehicle_types": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of platform-approved vehicle types the driver is permitted to register."
                }
            },
            "required": ["status", "report", "license_classes", "allowed_vehicle_types"]
        }
    }
]
