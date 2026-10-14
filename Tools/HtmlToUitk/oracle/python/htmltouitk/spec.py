import json
import os

_HERE = os.path.dirname(os.path.abspath(__file__))
_CANDIDATES = []
_probe = _HERE
for _ in range(5):
    _CANDIDATES.append(os.path.join(_probe, "spec"))
    _probe = os.path.dirname(_probe)
_CACHE = {}


def spec_dir():
    for candidate in _CANDIDATES:
        if os.path.isdir(candidate):
            return candidate
    raise RuntimeError("spec directory not found next to the package")


def load_spec(name):
    if name not in _CACHE:
        with open(os.path.join(spec_dir(), name + ".json"), "r", encoding="utf-8") as handle:
            _CACHE[name] = json.load(handle)
    return _CACHE[name]
