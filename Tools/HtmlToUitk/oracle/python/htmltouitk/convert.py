import json
import os
import re
from collections import OrderedDict
from xml.sax.saxutils import quoteattr

from . import uitk
from .cssparse import parse_color, parse_selector, format_color, split_ws
from .cssprocess import SHORTHANDS, canonical_order
from .util import Report, dumps, posix, write_text

LAYOUT_SET = set("""
position left top right bottom display visibility overflow opacity flex flex-direction flex-wrap flex-grow
flex-shrink flex-basis align-content align-items align-self justify-content width min-width max-width height
min-height max-height aspect-ratio margin margin-top margin-right margin-bottom margin-left padding
padding-top padding-right padding-bottom padding-left
""".split())

ATTR_ORDER = ["name", "text", "label", "title", "class", "value", "low-value", "high-value", "choices", "index",
              "placeholder-text", "max-length", "multiline", "password", "readonly", "picking-mode", "tooltip",
              "enabled", "focusable", "tabindex"]

NAMESPACES = {"ui": "UnityEngine.UIElements", "uie": "UnityEditor.UIElements"}


def normalise_token_value(value):
    parsed = None
    text = value.strip()
    if re.match(r"^rgba?\(", text, re.I):
        parsed = parse_color(text)
    if parsed is not None:
        return format_color(parsed)
    return re.sub(r"\s+", " ", text)


class Section(object):
    VARIABLES = 0
    LAYOUT = 1
    COMPONENTS = 2
    STATES = 3
    UTILITIES = 4


def classify(rule, cfg):
    selectors = [parse_selector(text) for text in rule.selectors]
    utility = re.compile(cfg.get("uss.utilityPattern", r"^u-"))
    dynamic = re.compile(cfg.get("naming.dynamicClassPattern") or r"^$")
    if rule.token and all(s == ":root" for s in rule.selectors):
        return Section.VARIABLES
    for selector in selectors:
        for _, compound in selector.parts:
            if compound.pseudos and not (len(selector.parts) == 1 and compound.pseudos[0][0] == "root"):
                return Section.STATES
            if any(dynamic.match(cls) for cls in compound.classes):
                return Section.STATES
    if all(len(s.parts) == 1 and len(s.parts[0][1].classes) == 1 and not s.parts[0][1].id and utility.match(s.parts[0][1].classes[0]) for s in selectors):
        return Section.UTILITIES
    if rule.decls and all(name in LAYOUT_SET for name, _ in rule.decls):
        return Section.LAYOUT
    return Section.COMPONENTS


def related(name_a, name_b):
    if name_a == name_b:
        return True
    for short, longs in SHORTHANDS.items():
        if (name_a == short and name_b in longs) or (name_b == short and name_a in longs):
            return True
    return False


def conflicts(rule_a, rule_b):
    for name_a, _ in rule_a.decls:
        for name_b, _ in rule_b.decls:
            if related(name_a, name_b):
                return True
    return False


def same_spec(rule_a, rule_b):
    return bool(set(rule_a.specs) & set(rule_b.specs))


def order_rules(rules, cfg):
    sections = [classify(rule, cfg) for rule in rules]
    if not cfg.get("uss.sectionOrder", True):
        return list(rules), sections
    for a in range(len(rules)):
        for b in range(a + 1, len(rules)):
            if sections[a] > sections[b] and conflicts(rules[a], rules[b]) and same_spec(rules[a], rules[b]):
                return list(rules), sections
    indexed = sorted(range(len(rules)), key=lambda i: (sections[i], i))
    return [rules[i] for i in indexed], sections


def dedupe_rules(rules):
    seen = {}
    for index, rule in enumerate(rules):
        seen[(tuple(rule.selectors), tuple(rule.decls))] = index
    kept = [rule for index, rule in enumerate(rules) if seen[(tuple(rule.selectors), tuple(rule.decls))] == index and rule.decls]
    merged = []
    for rule in kept:
        if merged and merged[-1].selectors == rule.selectors:
            previous = merged[-1]
            combined = OrderedDict()
            for name, value in previous.decls + rule.decls:
                combined.pop(name, None)
                combined[name] = value
            previous.decls = list(combined.items())
        else:
            clone = type(rule)(list(rule.selectors), list(rule.decls), rule.line, rule.order, list(rule.specs))
            clone.token = rule.token
            merged.append(clone)
    return merged


def is_dead(rule, an):
    dynamic = re.compile(an.cfg.get("naming.dynamicClassPattern") or r"^$")
    dynamic_names = set(an.cfg.get("naming.dynamicClasses", []))
    for text in rule.selectors:
        selector = parse_selector(text)
        subject = selector.parts[-1][1]
        if subject.id and subject.id in an.names:
            return False
        if subject.universal and not subject.classes:
            return False
        if not subject.classes:
            continue
        if all(cls in an.used_classes or cls in dynamic_names or dynamic.match(cls) for cls in subject.classes):
            return False
    return True


def format_rule(selectors, decls, indent):
    lines = []
    for index, text in enumerate(selectors):
        lines.append(text + ("," if index < len(selectors) - 1 else " {"))
    for name, value in decls:
        lines.append("%s%s: %s;" % (indent, name, value))
    lines.append("}")
    return "\n".join(lines)


def build_tokens(an):
    new = OrderedDict()
    for name, value in an.tokens.items():
        existing = an.skin_tokens.get(name)
        if existing is None or re.sub(r"\s+", "", existing.lower()) != re.sub(r"\s+", "", value.lower()):
            new[name] = normalise_token_value(value)
    return new


def build_uss(an, new_tokens):
    cfg = an.cfg
    indent = " " * cfg.get("uxml.indent", 4)
    blocks = []
    scope = cfg.get("uss.tokenScopeSelector")
    if scope:
        scoped = OrderedDict()
        if cfg.get("uss.redefineSkinTokens"):
            for name, value in an.skin_tokens.items():
                scoped[name] = normalise_token_value(value)
        for name, value in an.tokens.items():
            scoped[name] = normalise_token_value(value)
        if scoped:
            blocks.append(format_rule([scope], list(scoped.items()), indent))
    rules = []
    for rule in an.rules:
        clone = type(rule)(list(rule.selectors), list(rule.decls), rule.line, rule.order, list(rule.specs))
        clone.token = rule.token
        if rule.token:
            remaining = [(n, v) for n, v in rule.decls if not n.startswith("--")]
            if not remaining:
                continue
            clone.decls = remaining
            clone.token = False
        clone.decls = [(n, normalise_token_value(v) if n.startswith("--") else v) for n, v in clone.decls]
        clone.decls = canonical_order(clone.decls)
        rules.append(clone)
    rules = dedupe_rules(rules)
    if cfg.get("uss.pruneDeadRules"):
        rules = [rule for rule in rules if not is_dead(rule, an)]
    ordered, _ = order_rules(rules, cfg)
    ordered = dedupe_rules(ordered)
    for rule in ordered:
        blocks.append(format_rule(rule.selectors, rule.decls, indent))
    return "\n\n".join(blocks) + ("\n" if blocks else ""), len(ordered)


def build_tokens_uss(new_tokens, cfg):
    indent = " " * cfg.get("uxml.indent", 4)
    if not new_tokens:
        return ""
    return format_rule([":root"], list(new_tokens.items()), indent) + "\n"


def attr_sort_key(name):
    if name in ATTR_ORDER:
        return (0, ATTR_ORDER.index(name), name)
    return (1, 0, name)


def render_node(node, indent_text, depth, cfg, lines):
    pad = indent_text * depth
    prefix = "ui:"
    attrs = OrderedDict()
    if node.name:
        attrs["name"] = node.name
    for key, value in node.attrs.items():
        attrs[key] = value
    if node.classes:
        attrs["class"] = " ".join(node.classes)
    ordered = sorted(attrs.items(), key=lambda item: attr_sort_key(item[0]))
    if node.hidden:
        ordered.append(("style", "display: none;"))
    text = "".join(" %s=%s" % (k, quoteattr(v)) for k, v in ordered)
    tag = prefix + node.type
    if node.children:
        lines.append("%s<%s%s>" % (pad, tag, text))
        for child in node.children:
            render_node(child, indent_text, depth + 1, cfg, lines)
        lines.append("%s</%s>" % (pad, tag))
    else:
        lines.append("%s<%s%s />" % (pad, tag, text))


def style_src(from_dir, target):
    return posix(os.path.relpath(target, from_dir))


def build_uxml(an, style_srcs):
    cfg = an.cfg
    indent_text = " " * cfg.get("uxml.indent", 4)
    namespaces = cfg.get("uxml.namespaces", ["ui"])
    header = ['<?xml version="1.0" encoding="utf-8"?>']
    ns = " ".join('xmlns:%s="%s"' % (p, NAMESPACES[p]) for p in namespaces if p in NAMESPACES)
    header.append('<ui:UXML %s editor-extension-mode="False">' % ns)
    lines = list(header)
    for src in style_srcs:
        lines.append('%s<Style src="%s" />' % (indent_text, src))
    for root in an.roots:
        render_node(root, indent_text, 1, cfg, lines)
    lines.append("</ui:UXML>")
    return "\n".join(lines) + "\n"


def build_keys_stub(an):
    cfg = an.cfg
    locales = cfg.get("localization.locales", ["en"])
    mock_locale = cfg.get("localization.mockLocale")
    placeholder = cfg.get("localization.placeholder", "[TODO] ")
    data = OrderedDict()
    data["version"] = 1
    for locale in locales:
        table = OrderedDict()
        for key in an.new_keys:
            mock = an.keys.get(key, "")
            table[key] = mock if locale == mock_locale else placeholder + mock
        data[locale] = table
    return dumps(data)


class ConvertResult(object):
    def __init__(self):
        self.files = []
        self.analysis = None
        self.lint = []
        self.summary = OrderedDict()
        self.refused = False
        self.errors = []


def planned_paths(an, out_dir):
    cfg = an.cfg
    layout = cfg.data["layout"]
    screens = os.path.join(out_dir, layout["screens"])
    uxml = os.path.join(screens, an.screen + ".uxml")
    uss = os.path.join(screens, an.screen + ".uss")
    tokens_rel = cfg.get("output.tokensFile").replace("{skin}", layout["skin"]).replace("{screen}", an.screen).replace("{screens}", layout["screens"]).replace("{strings}", layout["strings"])
    tokens = os.path.join(out_dir, tokens_rel)
    stub_rel = cfg.get("localization.keysStub").replace("{screen}", an.screen)
    stub = os.path.join(out_dir, layout["strings"], stub_rel)
    return uxml, uss, tokens, stub


def generate(an, out_dir):
    uxml_path, uss_path, tokens_path, stub_path = planned_paths(an, out_dir)
    new_tokens = build_tokens(an)
    uss_text, rule_count = build_uss(an, new_tokens)
    tokens_text = build_tokens_uss(new_tokens, an.cfg) if not an.cfg.get("uss.tokenScopeSelector") or new_tokens else ""
    screens_dir = os.path.dirname(uxml_path)
    styles = [skin.src for skin in an.skins]
    if tokens_text:
        styles.append(style_src(screens_dir, tokens_path))
    styles.append(style_src(screens_dir, uss_path))
    uxml_text = build_uxml(an, styles)
    files = [(uxml_path, uxml_text), (uss_path, uss_text)]
    if tokens_text:
        files.append((tokens_path, tokens_text))
    if an.new_keys:
        files.append((stub_path, build_keys_stub(an)))
    return files, rule_count, new_tokens
