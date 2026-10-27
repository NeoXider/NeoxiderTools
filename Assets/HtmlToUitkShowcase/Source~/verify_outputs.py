"""Read-only structural/hash smoke check for the actual converter outputs."""
from pathlib import Path
from collections import Counter
import hashlib
import json
import re
import xml.etree.ElementTree as ET

root = Path(__file__).parent.parent
output = root / "Resources" / "Velora"
def read_json(relative):
    return json.loads((output / relative).read_text(encoding="utf-8"))

tree = ET.parse(output / "Screens/Velora.uxml")
names = [element.attrib["name"] for element in tree.iter() if "name" in element.attrib]
assert len(names) == len(set(names)), "Duplicate UXML names"
navigation = read_json("Screens/Velora.nav.json")
animations = read_json("Screens/Velora.anim.json")
media = read_json("Screens/Velora.mq.json")
manifest = read_json("generated-assets.json")
strings = read_json("Velora.strings.json")["en"]
diagnostics = read_json("diagnostics.json")
assert diagnostics["counts"]["error"] == 0
assert len(navigation["pages"]) == 4 and len(navigation["modals"]) == 2
assert all(entry["element"] in names for entry in navigation["pages"] + navigation["modals"])
assert all(edge["trigger"] in names for edge in navigation["edges"])
assert len(animations["animations"]) == 14
assert {entry["query"]["maxWidth"] for entry in media["classes"]} == {640, 900, 1100}
for asset in manifest["assets"]:
    assert hashlib.sha256((output / asset["path"]).read_bytes()).hexdigest() == asset["sha256"], asset["path"]
    assert asset["kind"] == "gradient" and asset["sRGB"] and not asset["mipmaps"] and asset["wrap"] == "clamp"
for element in tree.iter():
    content = element.attrib.get("text", "")
    assert not any(character.isalpha() for character in content) or content in strings, content

# Every actual Button has either a generated router trigger or a typed registration.
controller = (root / "Runtime/VeloraShowcaseController.cs").read_text(encoding="utf-8")
assert not re.search(r"\bvar\b", controller), "Explicit C# local types are required"
bound = {"vl-" + name for name in re.findall(r'Bind\("([^"]+)"', controller)}
bound.update(edge["trigger"] for edge in navigation["edges"])
bound.update(trigger for modal in navigation["modals"] for trigger in modal["closeTriggers"])
bound.update("vl-footer-source-" + page for page in ["home", "pricing", "about", "contact"])
bound.update("vl-home-faq-" + str(index) for index in range(4))
bound.update("vl-pricing-faq-" + str(index) for index in range(3))
buttons = {element.attrib["name"] for element in tree.iter() if element.tag.endswith("Button")}
assert not buttons - bound, "Unbound buttons: " + str(sorted(buttons - bound))
external_routes = {"nav-components": "components", "nav-blocks": "blocks", "nav-templates": "templates",
                   "nav-themes": "themes", "nav-changelog": "changelog", "mobile-home": "templates",
                   "mobile-features": "components", "mobile-blocks": "blocks", "mobile-themes": "themes",
                   "mobile-changelog": "changelog", "free-browse": "components"}
elements_by_name = {element.attrib["name"]: element for element in tree.iter() if "name" in element.attrib}
for name, route in external_routes.items():
    element = elements_by_name["vl-" + name]
    assert strings[element.attrib["text"]].endswith("↗"), "External transition must be visible: " + name
    assert "https://velora.colorlib.com/" + route in element.attrib["tooltip"], name
summary = {"verdict": diagnostics["verdict"], "errors": 0, "pages": 4, "modals": 2,
           "animations": len(animations["animations"]), "mediaSelectors": len(media["classes"]),
           "gradientHashesVerified": len(manifest["assets"]), "buttonsWithBehavior": len(buttons),
           "localizedTextEntries": len(strings), "uniqueNames": len(names),
           "externalDestinationsClearlyMarked": len(external_routes)}
print(json.dumps(summary, indent=2))
