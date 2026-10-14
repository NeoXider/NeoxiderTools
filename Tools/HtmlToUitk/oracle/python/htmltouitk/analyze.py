import os
import re
from collections import OrderedDict

from . import uitk
from .cssparse import match_selector, parse_selector, parse_stylesheet, split_ws
from .cssprocess import CssContext, OutRule, process_stylesheet, resolve_url
from .htmlread import HNode, read_html
from .loc import LocTable, has_letters, key_matches, load_table, normalise_text
from .rules import make_finding
from .util import KEBAB, Report, posix, relpath

HARNESS_SCRIPTS = ("uitk-validate.js", "uitk-prototype.js")
PASSTHROUGH_PREFIX = "data-uxml-"


class UNode(object):
    def __init__(self, utype, h):
        self.type = utype
        self.tag = utype
        self.h = h
        self.name = None
        self.classes = []
        self.attrs = OrderedDict()
        self.children = []
        self.parent = None
        self.line = h.line if h is not None else 0
        self.col = h.col if h is not None else 0
        self.text_key = None
        self.mock = None
        self.hidden = False

    def walk(self):
        yield self
        for child in self.children:
            for item in child.walk():
                yield item


class Skin(object):
    def __init__(self, src, path, found):
        self.src = src
        self.path = path
        self.found = found
        self.classes = set()
        self.vars = OrderedDict()
        self.tokens = OrderedDict()


class Analysis(object):
    def __init__(self, path, cfg):
        self.path = path
        self.cfg = cfg
        self.label = posix(path)
        self.report = Report(self.label)
        self.doc = None
        self.screen = None
        self.roots = []
        self.skins = []
        self.rules = []
        self.tokens = OrderedDict()
        self.token_lines = {}
        self.keys = OrderedDict()
        self.new_keys = []
        self.loc = LocTable()
        self.has_table = False
        self.names = {}
        self.used_classes = OrderedDict()
        self.defined_classes = set()
        self.keyframes = []
        self.css_ctxs = []
        self.nodes = []
        self.uxml_styles = []

    def emit(self, rule_id, line, col, construct, message=None, severity=None, why=None, fix=None):
        if self.cfg.disabled(rule_id):
            return
        make_finding(self.report, rule_id, self.label, line, col, construct, message, severity, why, fix)


def screen_name(doc, path):
    for value in (doc.meta("uitk:screen"), doc.body_attrs.get("data-screen")):
        if value:
            return value.strip()
    return os.path.splitext(os.path.basename(path))[0]


def fill_template(template, screen, cfg):
    layout = cfg.data["layout"]
    return (template.replace("{screen}", screen).replace("{skin}", layout["skin"])
            .replace("{screens}", layout["screens"]).replace("{strings}", layout["strings"]))


def load_skin(an, attrs, line, col):
    src = attrs.get("data-uss", "").strip()
    folder = an.cfg.layout_dir("screens")
    path = os.path.normpath(os.path.join(folder, src))
    skin = Skin(src, path, os.path.isfile(path))
    if not skin.found:
        an.emit("D015", line, col, "data-uss=\"%s\"" % src, "skin stylesheet '%s' not found (looked at %s)" % (src, posix(path)))
        return skin
    from .util import read_text
    sheet = parse_stylesheet(read_text(path), path)
    for rule in sheet.rules:
        only_root = True
        for text, _ in rule.selectors:
            selector = parse_selector(text)
            for _, compound in selector.parts:
                for cls in compound.classes:
                    skin.classes.add(cls)
            if text.strip() != ":root":
                only_root = False
        for decl in rule.decls:
            if decl.name.startswith("--"):
                skin.vars[decl.name] = decl.value
                if only_root:
                    skin.tokens[decl.name] = decl.value
    return skin


def effective_value(an, node, prop):
    best = None
    for rule in an.rules:
        if rule.token:
            continue
        value = None
        for name, val in rule.decls:
            if name == prop:
                value = val
        if value is None:
            continue
        for text, spec in zip(rule.selectors, rule.specs):
            selector = parse_selector(text)
            if any(compound.pseudos for _, compound in selector.parts):
                continue
            if match_selector(selector, node):
                key = (spec, rule.order)
                if best is None or key >= best[0]:
                    best = (key, value)
    return best[1] if best else None


def px_value(text):
    if text is None:
        return None
    match = re.match(r"^(-?[0-9.]+)px$", text.strip())
    return float(match.group(1)) if match else None


def check_class_name(an, h, cls, prefixes):
    pattern = an.cfg.get("naming.classPattern")
    if not re.match(pattern, cls):
        an.emit("N004", h.line, h.col, "class=\"%s\"" % cls, "class '%s' does not match %s" % (cls, pattern))
        return
    if prefixes and not has_prefix(cls, prefixes) and not cls.startswith("unity-"):
        an.emit("N005", h.line, h.col, "class=\"%s\"" % cls, "class '%s' has none of the prefixes %s" % (cls, ", ".join(prefixes)))


def has_prefix(text, prefixes):
    for prefix in prefixes:
        if text == prefix or text.startswith(prefix + "-") or text.startswith(prefix + "__") or text.startswith(prefix + "--"):
            return True
    return False


def add_key(an, key, mock, h, construct):
    pattern = an.cfg.get("localization.keyPattern")
    if not key_matches(pattern, key):
        an.emit("L002", h.line, h.col, construct, "key '%s' does not match %s" % (key, pattern))
    if key in an.keys:
        previous = an.keys[key]
        if mock and previous and normalise_text(mock) != normalise_text(previous):
            an.emit("L005", h.line, h.col, construct, "key '%s' has two mock texts: '%s' and '%s'" % (key, previous, mock))
        elif mock and not previous:
            an.keys[key] = mock
        return
    an.keys[key] = mock or ""
    mock_locale = an.cfg.get("localization.mockLocale")
    if an.has_table:
        if an.loc.has(key):
            table_text = an.loc.get(key, mock_locale)
            if mock and table_text is not None and normalise_text(mock) != normalise_text(table_text):
                an.emit("L004", h.line, h.col, construct, "mock text '%s' differs from the table text '%s' (%s)" % (mock, table_text, mock_locale))
        else:
            an.new_keys.append(key)
            an.emit("L003", h.line, h.col, construct, "key '%s' is not in the string table" % key)
    else:
        an.new_keys.append(key)


def map_element(an, h, parent):
    tag = h.tag
    attrs = h.attrs
    utype = None
    dialect_uxml = attrs.get("data-uxml")
    if dialect_uxml:
        if dialect_uxml in uitk.DATA_UXML:
            utype = dialect_uxml
        else:
            an.emit("D003", h.line, h.col, "data-uxml=\"%s\"" % dialect_uxml, "'%s' is not a known UXML element of the dialect" % dialect_uxml)
    if utype is None:
        if tag == "input":
            itype = (attrs.get("type") or "text").lower()
            utype = uitk.INPUT_TYPES.get(itype)
            if utype is None:
                an.emit("D002", h.line, h.col, "<input type=\"%s\">" % itype, "input type '%s' has no UI Toolkit control" % itype)
        elif tag in uitk.TAG_TO_UXML:
            utype = uitk.TAG_TO_UXML[tag]
        else:
            fix = uitk.TAG_ADVICE.get(tag)
            an.emit("D001", h.line, h.col, "<%s>" % tag, "<%s> has no UI Toolkit counterpart" % tag, fix=fix)
    placeholder = utype is None
    node = UNode(utype or "VisualElement", h)
    node.parent = parent
    return node, placeholder


def build_node(an, h, parent):
    node, placeholder = map_element(an, h, parent)
    tag = h.tag
    attrs = h.attrs
    utype = node.type
    prefixes = an.cfg.get("naming.prefixes", [])
    for key, value in attrs.items():
        if key.startswith("on"):
            an.emit("D005", h.line, h.col, "%s=\"%s\"" % (key, value[:30]), "event handler attribute '%s'" % key)
        elif key == "href":
            an.emit("D005", h.line, h.col, "href=\"%s\"" % value[:30], "link attribute 'href'")
        elif key == "style":
            normalised = re.sub(r"\s+", "", value.strip().lower()).rstrip(";")
            if normalised == "display:none":
                node.hidden = True
            else:
                an.emit("D011", h.line, h.col, "style=\"%s\"" % value[:60], "inline style '%s'" % value[:60])
        elif key.startswith("data-") or key in uitk.SAFE_ATTRS or key == "class":
            continue
        else:
            an.emit("D004", h.line, h.col, "%s=\"%s\"" % (key, value[:30]), "attribute '%s' is ignored by the conversion" % key)
    if "hidden" in attrs:
        node.hidden = True
    name_attr, id_attr = attrs.get("name"), attrs.get("id")
    if name_attr and id_attr and name_attr != id_attr:
        an.emit("N006", h.line, h.col, "name=\"%s\" id=\"%s\"" % (name_attr, id_attr), "name '%s' and id '%s' differ" % (name_attr, id_attr))
    node.name = name_attr or id_attr
    if node.name:
        if node.name in an.names:
            an.emit("N001", h.line, h.col, "name=\"%s\"" % node.name, "name '%s' is already used at line %d" % (node.name, an.names[node.name]))
        else:
            an.names[node.name] = h.line
        if not KEBAB.match(node.name):
            an.emit("N002", h.line, h.col, "name=\"%s\"" % node.name, "name '%s' is not kebab-case" % node.name)
        elif prefixes and not has_prefix(node.name, prefixes):
            an.emit("N005", h.line, h.col, "name=\"%s\"" % node.name, "name '%s' has none of the prefixes %s" % (node.name, ", ".join(prefixes)))
    node.classes = h.classes
    for cls in node.classes:
        check_class_name(an, h, cls, prefixes)
        an.used_classes.setdefault(cls, h.line)
    if utype in uitk.INTERACTIVE and not node.name:
        an.emit("N003", h.line, h.col, "<%s>" % tag, "interactive <%s> (%s) has no name" % (tag, utype))
    inner = tag in uitk.INNER_TEXT_TAGS
    text_attr = uitk.TEXT_ATTR.get(utype)
    loc_key = attrs.get("data-loc")
    mock = h.own_text() if inner else (attrs.get("data-text") or "")
    if not inner:
        for text in h.texts():
            if text.text.strip() and tag not in ("option",):
                an.emit("D006", text.line, text.col, "text \"%s\"" % normalise_text(text.text)[:40], "text inside <%s> (%s)" % (tag, utype))
                break
    if loc_key is not None:
        loc_key = loc_key.strip()
        if text_attr is None:
            an.emit("L006", h.line, h.col, "data-loc=\"%s\"" % loc_key, "<%s> (%s) has no text property" % (tag, utype))
        else:
            node.text_key = loc_key
            node.mock = mock
            node.attrs[text_attr] = loc_key
            add_key(an, loc_key, mock, h, "data-loc=\"%s\"" % loc_key)
            if not inner and not attrs.get("data-text"):
                an.emit("L007", h.line, h.col, "<%s data-loc=\"%s\">" % (tag, loc_key))
    elif mock and text_attr is not None:
        if has_letters(mock) or not an.cfg.get("localization.allowGlyphOnly"):
            an.emit("L001", h.line, h.col, "\"%s\"" % mock[:50], "visible text \"%s\" has no data-loc key" % mock[:50])
        else:
            node.attrs[text_attr] = mock
            node.mock = mock
    elif mock and text_attr is None and inner:
        an.emit("L001", h.line, h.col, "\"%s\"" % mock[:50], "visible text \"%s\" has no data-loc key" % mock[:50])
    tooltip = attrs.get("data-loc-tooltip")
    if tooltip:
        node.attrs["tooltip"] = tooltip.strip()
        add_key(an, tooltip.strip(), "", h, "data-loc-tooltip=\"%s\"" % tooltip.strip())
    if "title" in attrs and attrs["title"].strip():
        an.emit("L001", h.line, h.col, "title=\"%s\"" % attrs["title"][:40], "title attribute is raw visible text; use data-loc-tooltip")
    if tag == "input" or tag == "textarea" or utype in ("TextField", "IntegerField", "FloatField"):
        ph = attrs.get("placeholder")
        key = attrs.get("data-loc-placeholder")
        if key:
            node.attrs["placeholder-text"] = key.strip()
            add_key(an, key.strip(), ph or "", h, "data-loc-placeholder=\"%s\"" % key.strip())
        elif ph and ph.strip():
            an.emit("L001", h.line, h.col, "placeholder=\"%s\"" % ph[:40], "placeholder \"%s\" has no data-loc-placeholder key" % ph[:40])
    apply_value_attrs(an, node, h)
    if attrs.get("data-picking", "").lower() == "ignore":
        node.attrs["picking-mode"] = "Ignore"
    if "disabled" in attrs:
        node.attrs["enabled"] = "false"
    if "tabindex" in attrs:
        node.attrs["tabindex"] = attrs["tabindex"]
    for key, value in attrs.items():
        if key.startswith(PASSTHROUGH_PREFIX) and key != "data-uxml":
            node.attrs[key[len(PASSTHROUGH_PREFIX):]] = value
    extra = an.cfg.get("uxml.interactiveAttributes", {})
    if extra and utype in uitk.INTERACTIVE:
        for key, value in extra.items():
            node.attrs.setdefault(key, value)
    if utype == "Image":
        handle_image(an, node, h)
    if tag == "select":
        handle_select(an, node, h)
        return node
    if h.elements() and utype in ("Label",):
        for child in h.elements():
            an.emit("D007", child.line, child.col, "<%s> inside <%s>" % (child.tag, tag), "Label <%s> contains <%s>" % (tag, child.tag))
            break
    if utype == "Button" and h.elements():
        an.emit("D008", h.line, h.col, "<button>", "button contains <%s>" % h.elements()[0].tag)
    if tag in uitk.VOID_TAGS or tag in ("progress", "textarea"):
        return node
    for child in h.elements():
        if utype == "Label":
            build_children_flat(an, child, node)
        else:
            sub = build_node(an, child, node)
            node.children.append(sub)
    return node


def build_children_flat(an, child, parent):
    sub = build_node(an, child, parent)
    parent.children.append(sub)


def apply_value_attrs(an, node, h):
    attrs = h.attrs
    utype = node.type
    if utype in ("Slider", "SliderInt", "MinMaxSlider"):
        if "min" in attrs:
            node.attrs["low-value"] = attrs["min"]
        if "max" in attrs:
            node.attrs["high-value"] = attrs["max"]
        if "value" in attrs:
            node.attrs["value"] = attrs["value"]
        if "step" in attrs:
            an.emit("D004", h.line, h.col, "step=\"%s\"" % attrs["step"], "attribute 'step' has no UXML slider equivalent")
    elif utype == "ProgressBar":
        if "value" in attrs:
            node.attrs["value"] = attrs["value"]
        if "max" in attrs:
            node.attrs["high-value"] = attrs["max"]
    elif utype in ("TextField", "IntegerField", "FloatField"):
        if "value" in attrs and attrs["value"] != "":
            node.attrs["value"] = attrs["value"]
        if "maxlength" in attrs:
            node.attrs["max-length"] = attrs["maxlength"]
        if "readonly" in attrs:
            node.attrs["readonly"] = "true"
        if h.tag == "textarea":
            node.attrs["multiline"] = "true"
        if (attrs.get("type") or "").lower() == "password":
            node.attrs["password"] = "true"
    elif utype in ("Toggle", "RadioButton"):
        if "checked" in attrs:
            node.attrs["value"] = "true"


def handle_image(an, node, h):
    src = h.attrs.get("src")
    if not node.name:
        an.emit("D013", h.line, h.col, "<img>", "<img> needs a name so its image can be bound by a #name rule")
        return
    if not src:
        return
    ctx = an.css_ctx_for_html()
    from .cssparse import Decl
    decl = Decl("background-image", src, h.line, h.col)
    ctx.offset = 0
    url = resolve_url(ctx, src, decl)
    if url is None:
        return
    rule = OutRule(["#" + node.name], [("background-image", url), ("-unity-background-scale-mode", "scale-to-fit")], h.line, 10 ** 6 + len(an.rules), [(1, 0, 0)])
    an.rules.append(rule)


def handle_select(an, node, h):
    keys = []
    selected = None
    for index, option in enumerate([c for c in h.elements() if c.tag == "option"]):
        text = option.own_text()
        key = option.attrs.get("data-loc")
        if key:
            key = key.strip()
            add_key(an, key, text, option, "data-loc=\"%s\"" % key)
            keys.append(key)
        elif text:
            if has_letters(text) or not an.cfg.get("localization.allowGlyphOnly"):
                an.emit("L001", option.line, option.col, "\"%s\"" % text[:50], "option text \"%s\" has no data-loc key" % text[:50])
            keys.append(text)
        if "selected" in option.attrs:
            selected = index
    for child in h.elements():
        if child.tag != "option":
            an.emit("D001", child.line, child.col, "<%s>" % child.tag, "<%s> inside <select> is not supported" % child.tag)
    if keys:
        node.attrs["choices"] = ",".join(keys)
    if selected is not None:
        node.attrs["index"] = str(selected)


def analyze(path, cfg, screen=None, extra_tables=None):
    from .util import read_text
    an = Analysis(path, cfg)
    an.doc = read_html(path)
    doc = an.doc
    an.screen = screen or screen_name(doc, path)
    an.html_dir = os.path.dirname(os.path.abspath(path))
    table_paths = []
    for template in cfg.get("localization.files", []):
        table_paths.append(cfg.abs_path(fill_template(template, an.screen, cfg)))
    meta = doc.meta("uitk:strings")
    if meta:
        table_paths.append(os.path.normpath(os.path.join(an.html_dir, meta)))
    an.loc = load_table(table_paths, cfg.get("localization.format"))
    an.has_table = bool(an.loc.sources)
    for attrs, line, col in doc.links:
        rel = (attrs.get("rel") or "").lower()
        if rel != "stylesheet":
            continue
        if "data-uitk-harness" in attrs:
            continue
        if "data-uss" in attrs:
            an.skins.append(load_skin(an, attrs, line, col))
            continue
        an.emit("D010", line, col, "<link href=\"%s\">" % attrs.get("href", ""), "stylesheet '%s' is not convertible" % attrs.get("href", ""))
    for attrs, line, col, text in doc.scripts:
        src = attrs.get("src", "")
        if "data-uitk-harness" in attrs or src.endswith(HARNESS_SCRIPTS):
            continue
        an.emit("D009", line, col, "<script%s>" % (" src=\"%s\"" % src if src else ""), "script is not convertible")
    skin_classes = set()
    skin_vars = OrderedDict()
    for skin in an.skins:
        skin_classes |= skin.classes
        skin_vars.update(skin.vars)
    css_ctx = CssContext(cfg, an.report, an.label, an.html_dir)
    an.css_ctx = css_ctx
    for block in doc.styles:
        if "data-uitk-harness" in block.attrs:
            continue
        css_ctx.offset = block.line - 1
        sheet = parse_stylesheet(block.text, an.label)
        process_stylesheet(css_ctx, sheet)
    an.rules = css_ctx.rules
    an.keyframes = css_ctx.keyframes
    an.tokens = css_ctx.tokens
    an.token_lines = css_ctx.token_lines
    an.css_ctx_for_html = lambda: css_ctx
    for child in doc.body.children:
        if isinstance(child, HNode):
            node = build_node(an, child, None)
            an.roots.append(node)
        elif child.text.strip():
            an.emit("D006", child.line, child.col, "text \"%s\"" % normalise_text(child.text)[:40], "text directly inside <body>")
    for root in an.roots:
        an.nodes.extend(root.walk())
    an.skin_classes = skin_classes
    css_classes = set()
    for rule in an.rules:
        for text in rule.selectors:
            for _, compound in parse_selector(text).parts:
                css_classes.update(compound.classes)
    an.defined_classes = css_classes | skin_classes
    check_classes(an)
    check_variables(an, skin_vars)
    check_tokens(an)
    check_rules_on_nodes(an)
    an.report.new_keys = list(an.new_keys)
    return an


def check_classes(an):
    mode = an.cfg.get("rules.undefinedClass")
    if mode == "off":
        return
    dynamic_pattern = an.cfg.get("naming.dynamicClassPattern")
    dynamic = set(an.cfg.get("naming.dynamicClasses", []))
    for cls, line in an.used_classes.items():
        if cls in an.defined_classes or cls in dynamic or cls.startswith("unity-"):
            continue
        if dynamic_pattern and re.match(dynamic_pattern, cls):
            continue
        an.emit("N007", line, 1, "class=\"%s\"" % cls, "class '%s' has no style rule" % cls, severity="FAIL" if mode == "fail" else "WARN")


def check_variables(an, skin_vars):
    ctx = an.css_ctx
    known = set(skin_vars) | set(ctx.defined_vars)
    for name, has_fallback, line, col in ctx.used_vars:
        if name not in known and not has_fallback:
            an.emit("C033", line, col, "var(%s)" % name, "var(%s) is not defined" % name)


def check_tokens(an):
    skin_tokens = OrderedDict()
    for skin in an.skins:
        skin_tokens.update(skin.tokens)
    an.skin_tokens = skin_tokens
    for name, value in an.tokens.items():
        if name in skin_tokens and re.sub(r"\s+", "", skin_tokens[name].lower()) != re.sub(r"\s+", "", value.lower()):
            an.emit("C034", an.token_lines.get(name, 1), 1, "%s: %s" % (name, value), "token %s is '%s' in the skin and '%s' here" % (name, skin_tokens[name], value))


def check_rules_on_nodes(an):
    cfg = an.cfg
    min_touch = cfg.get("rules.minTouchTarget")
    for node in an.nodes:
        h = node.h
        if node.type in uitk.INTERACTIVE and min_touch:
            height = px_value(effective_value(an, node, "height")) or px_value(effective_value(an, node, "min-height"))
            width = px_value(effective_value(an, node, "width"))
            if height is not None and height < min_touch:
                an.emit("R002", h.line, h.col, "<%s name=\"%s\">" % (h.tag, node.name or ""), "height %gpx is below the minimum %gpx" % (height, min_touch))
            elif width is not None and width < min_touch:
                an.emit("R002", h.line, h.col, "<%s name=\"%s\">" % (h.tag, node.name or ""), "width %gpx is below the minimum %gpx" % (width, min_touch))
        if node.type in uitk.TEXT_ELEMENTS and (node.mock or node.text_key):
            nowrap = effective_value(an, node, "white-space") == "nowrap"
            ellipsis = effective_value(an, node, "text-overflow") == "ellipsis"
            clipped = effective_value(an, node, "overflow") == "hidden"
            if nowrap and not (ellipsis and clipped):
                an.emit("R003", h.line, h.col, "<%s name=\"%s\">" % (h.tag, node.name or ""), "nowrap text without ellipsis and overflow: hidden")
            if ellipsis and not (nowrap and clipped):
                an.emit("C030", h.line, h.col, "<%s name=\"%s\">" % (h.tag, node.name or ""))
    if cfg.get("rules.colorLiterals") == "tokens-only":
        from .cssparse import is_color_literal
        for rule in an.rules:
            if rule.token:
                continue
            for name, value in rule.decls:
                if name.startswith("--"):
                    continue
                if any(is_color_literal(token) for token in split_ws(value)):
                    an.emit("R004", rule.line, 1, "%s: %s" % (name, value))
