"""Sync supported authoring CSS into the HTML input. Does not edit converter output."""
from pathlib import Path
import re

source = Path(__file__).parent
document = source / "Velora.html"
css = (source / "velora.css").read_text(encoding="utf-8")
geometry = (source / "geometry.css").read_text(encoding="utf-8")
markup = document.read_text(encoding="utf-8")
updated, count = re.subn(r"<style>.*?</style>", lambda match: "<style>\n" + css + "\n" + geometry + "\n</style>", markup, count=1, flags=re.S)
if count != 1:
    raise SystemExit("Expected exactly one embedded authoring stylesheet.")
document.write_text(updated, encoding="utf-8")
print("Synchronized CSS into Velora.html. Run the real HtmlToUitk CLI next.")
