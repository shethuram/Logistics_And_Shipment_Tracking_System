import os
import sys

site_packages = os.path.join(os.path.dirname(__file__), ".python_packages", "lib", "site-packages")
if os.path.exists(site_packages) and site_packages not in sys.path:
    sys.path.insert(0, site_packages)

from main import app

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8000)
