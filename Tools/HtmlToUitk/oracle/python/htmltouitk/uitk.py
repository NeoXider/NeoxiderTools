import os

from .spec import load_spec

ASSET_DIR = None
_probe = os.path.dirname(os.path.abspath(__file__))
for _ in range(5):
    if os.path.isdir(os.path.join(_probe, "assets")):
        ASSET_DIR = os.path.join(_probe, "assets")
        break
    _probe = os.path.dirname(_probe)

_USS = load_spec("uss")
_DIALECT = load_spec("dialect")

USS_STANDARD = set(_USS["standard"])
USS_UNITY = set(_USS["unity"])
USS_PROPERTIES = USS_STANDARD | USS_UNITY
ANIMATABLE_FULL = set(_USS["animatableFull"])
LAYOUT_PROPERTIES = set(_USS["layoutProperties"])
INHERITED = set(_USS["inherited"])
EASINGS = set(_USS["easings"])
CSS_BROWSER_EASINGS = set(_USS["browserEasings"])
TEXT_ALIGNS = set(_USS["textAligns"])
ENUMS = dict((key, set(value)) for key, value in _USS["enums"].items())
LENGTH_PROPS = set(_USS["lengthProperties"])
NUMBER_PROPS = set(_USS["numberProperties"])
COLOR_PROPS = set(_USS["colorProperties"])

TAG_TO_UXML = dict(_DIALECT["tagToUxml"])
INPUT_TYPES = dict(_DIALECT["inputTypes"])
TAG_ADVICE = dict(_DIALECT["tagAdvice"])
DATA_UXML = list(_DIALECT["dataUxml"])
TEXT_ATTR = dict(_DIALECT["textAttribute"])
INTERACTIVE = set(_DIALECT["interactive"])
TEXT_ELEMENTS = set(_DIALECT["textElements"])
INNER_TEXT_TAGS = set(_DIALECT["innerTextTags"])
SAFE_ATTRS = set(_DIALECT["safeAttributes"])
VOID_TAGS = set(_DIALECT["voidTags"])
FOCUSABLE_DEFAULT = set(_DIALECT["focusableByDefault"])


def read_asset(name):
    with open(os.path.join(ASSET_DIR, name), "r", encoding="utf-8", newline="") as handle:
        return handle.read().replace("\r\n", "\n")
