import copy
import json
import os

from .util import read_text

DEFAULTS = {
    "projectRoot": ".",
    "uiRoot": "UI",
    "layout": {"screens": "Screens", "skin": "Skin", "strings": "Localization"},
    "localization": {
        "files": [],
        "format": "locale-map",
        "locales": ["en"],
        "mockLocale": "en",
        "keyPattern": r"^[a-z][A-Za-z0-9_-]*(\.[A-Za-z0-9_-]+)+$",
        "placeholder": "[TODO] ",
        "allowGlyphOnly": True,
        "keysStub": "{screen}.newkeys.json",
    },
    "naming": {
        "namePattern": r"^[a-z][a-z0-9]*(-[a-z0-9]+)*$",
        "classPattern": r"^[a-z][a-z0-9]*(-[a-z0-9]+)*(__[a-z0-9]+(-[a-z0-9]+)*)?(--[a-z0-9]+(-[a-z0-9]+)*)?$",
        "prefixes": [],
        "dynamicClassPattern": r"^is-",
        "dynamicClasses": [],
    },
    "rules": {
        "minFontSize": 13,
        "minTouchTarget": 40,
        "undefinedClass": "warn",
        "colorLiterals": "allow",
        "strictWarnings": False,
        "disable": [],
        "allowLayoutAnimation": False,
        "maxKeyframeSteps": 8,
    },
    "fonts": {},
    "uxml": {
        "indent": 4,
        "namespaces": ["ui", "uie"],
        "interactiveAttributes": {},
        "allowInlineStyles": ["display: none;"],
        "templateSrcStyle": "relative",
    },
    "uss": {
        "tokenScopeSelector": None,
        "redefineSkinTokens": False,
        "pruneDeadRules": False,
        "sectionOrder": True,
    },
    "output": {"tokensFile": "{skin}/{screen}.tokens.uss"},
    "lint": {"builtin": True, "external": None},
    "preview": {
        "theme": [],
        "panelSettings": None,
        "viewports": ["1920x1080"],
        "backdrop": "#101418",
        "locale": None,
        "codeBindings": None,
        "defaultTheme": True,
        "transitions": False,
        "textOverrides": {},
        "fragments": {},
        "fallbackDirs": [],
    },
    "pages": {"backend": "standalone", "classes": {"hidden": "is-hidden", "active": "is-active"}},
}


class ConfigError(Exception):
    pass


def _merge(base, extra, path, problems):
    for key, value in extra.items():
        if key not in base:
            problems.append("unknown config key: %s%s" % (path, key))
            base[key] = value
            continue
        if isinstance(base[key], dict) and isinstance(value, dict) and base[key]:
            _merge(base[key], value, path + key + ".", problems)
        else:
            base[key] = value


class Config(object):
    def __init__(self, data=None, base_dir=None, problems=None):
        self.data = data if data is not None else copy.deepcopy(DEFAULTS)
        self.base_dir = os.path.abspath(base_dir or os.getcwd())
        self.problems = problems or []

    def get(self, dotted, default=None):
        node = self.data
        for part in dotted.split("."):
            if not isinstance(node, dict) or part not in node:
                return default
            node = node[part]
        return node

    def abs_path(self, relative):
        if relative is None:
            return None
        if os.path.isabs(relative):
            return os.path.normpath(relative)
        return os.path.normpath(os.path.join(self.base_dir, relative))

    @property
    def project_root(self):
        return self.abs_path(self.data["projectRoot"])

    @property
    def ui_root(self):
        return os.path.normpath(os.path.join(self.project_root, self.data["uiRoot"]))

    def layout_dir(self, key):
        return os.path.normpath(os.path.join(self.ui_root, self.data["layout"][key]))

    def disabled(self, rule_id):
        return rule_id in self.data["rules"]["disable"]


def load_config(path=None):
    if path is None:
        return Config()
    if not os.path.isfile(path):
        raise ConfigError("config file not found: %s" % path)
    try:
        extra = json.loads(read_text(path))
    except ValueError as exc:
        raise ConfigError("config is not valid JSON: %s" % exc)
    if not isinstance(extra, dict):
        raise ConfigError("config root must be an object")
    extra.pop("$schema", None)
    data = copy.deepcopy(DEFAULTS)
    problems = []
    _merge(data, extra, "", problems)
    return Config(data, os.path.dirname(os.path.abspath(path)), problems)
