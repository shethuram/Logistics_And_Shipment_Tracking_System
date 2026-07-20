from typing import List

class ExtractedLicenseData:
    def __init__(self, name: str = "", license_number: str = "", expiry_date: str = "", classes: List[str] = None, confidence_score: float = 0.0):
        self.name = name
        self.license_number = license_number
        self.expiry_date = expiry_date
        self.classes = classes or []
        self.confidence_score = confidence_score
