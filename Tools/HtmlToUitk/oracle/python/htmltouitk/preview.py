import json
import math
import os
import re
import xml.etree.ElementTree as ET
from collections import OrderedDict

from . import uitk
from .cssparse import parse_selector, parse_stylesheet, split_top, split_ws
from .loc import LocTable, load_table
from .util import posix, read_text

UXML_NS = "UnityEngine.UIElements"

TEXT_TYPES = {"Label", "Button", "TextElement"}
FOCUSABLE = {"Button", "TextField", "IntegerField", "FloatField", "Toggle", "RadioButton", "Slider", "SliderInt", "DropdownField"}


class VNode(object):
    def __init__(self, vtype, attrs):
        self.type = vtype
        self.attrs = attrs
        self.children = []
        self.parent = None
        self.name = attrs.get("name", "")
        self.classes = (attrs.get("class") or "").split()


class Report(object):
    def __init__(self):
        self.items = []

    def add(self, kind, what, where="", why=""):
        item = {"kind": kind, "what": what, "where": where, "why": why}
        if item not in self.items:
            self.items.append(item)


def local(tag):
    return tag.split("}")[-1]


def read_uxml(path, report, seen=None):
    seen = seen or set()
    root = ET.fromstring(read_text(path))
    styles = []
    templates = {}
    base = os.path.dirname(path)

    def convert(element):
        tag = local(element.tag)
        if tag in ("Style", "AttributeOverrides"):
            return None
        if tag == "Template":
            return None
        if tag == "Instance":
            name = element.get("template")
            src = templates.get(name)
            node = VNode("TemplateContainer", dict((k, v) for k, v in element.attrib.items() if k in ("name", "class")))
            if not src or not os.path.isfile(src):
                report.add("preview-approximation", "Instance of unknown template %s" % name, path)
                return node
            if src in seen:
                report.add("preview-approximation", "template cycle %s" % name, path)
                return node
            sub_root, sub_styles = read_uxml(src, report, seen | {path})
            for child in sub_root.children:
                child.parent = node
                node.children.append(child)
            styles.extend(sub_styles)
            return node
        node = VNode(tag, dict(element.attrib))
        for child in element:
            c = convert(child)
            if c is not None:
                c.parent = node
                node.children.append(c)
        return node

    for child in root:
        t = local(child.tag)
        if t == "Style" and child.get("src"):
            styles.append(os.path.normpath(os.path.join(base, child.get("src"))) if not child.get("src").startswith(("project:", "/")) else child.get("src"))
        elif t == "Template":
            templates[child.get("name")] = os.path.normpath(os.path.join(base, child.get("src", "")))
    holder = VNode("TemplateContainer", {"class": "uitk-root"})
    for child in root:
        n = convert(child)
        if n is not None:
            n.parent = holder
            holder.children.append(n)
    return holder, styles


def parse_panel_settings(path):
    info = {"scaleMode": 0, "reference": (1920, 1080), "match": 0.5, "matchMode": 0, "scale": 1.0}
    if not path or not os.path.isfile(path):
        return info
    text = read_text(path)
    m = re.search(r"m_ScaleMode:\s*(\d+)", text)
    if m:
        info["scaleMode"] = int(m.group(1))
    m = re.search(r"m_ReferenceResolution:\s*\{x:\s*([\d.]+),\s*y:\s*([\d.]+)\}", text)
    if m:
        info["reference"] = (float(m.group(1)), float(m.group(2)))
    m = re.search(r"m_Match:\s*([\d.]+)", text)
    if m:
        info["match"] = float(m.group(1))
    m = re.search(r"m_ScreenMatchMode:\s*(\d+)", text)
    if m:
        info["matchMode"] = int(m.group(1))
    m = re.search(r"m_Scale:\s*([\d.]+)", text)
    if m:
        info["scale"] = float(m.group(1))
    return info


def panel_geometry(info, width, height):
    if info["scaleMode"] == 0:
        s = info["scale"]
    elif info["scaleMode"] == 2:
        rw, rh = info["reference"]
        mode = info["matchMode"]
        if mode == 0:
            s = 2 ** ((1 - info["match"]) * math.log(width / rw, 2) + info["match"] * math.log(height / rh, 2))
        elif mode == 1:
            s = min(width / rw, height / rh)
        else:
            s = max(width / rw, height / rh)
    else:
        s = 1.0
    return s, width / s, height / s


def esc_ident(name):
    out = ""
    for i, ch in enumerate(name):
        if re.match(r"[A-Za-z0-9_-]", ch) and not (i == 0 and ch.isdigit()):
            out += ch
        else:
            out += "\\" + (ch if not ch.isdigit() else "3%s " % ch)
    return out


class Translator(object):
    def __init__(self, cfg, out_dir, report, fallback_dirs):
        self.cfg = cfg
        self.out_dir = out_dir
        self.report = report
        self.fonts = OrderedDict()
        self.fallback = fallback_dirs
        self.css = []
        self.tint_seen = set()

    def resolve_path(self, rel, base_dir):
        if rel.startswith("/"):
            candidates = [os.path.normpath(os.path.join(self.cfg.project_root, rel.lstrip("/")))]
        else:
            candidates = [os.path.normpath(os.path.join(base_dir, rel))]
            for fb in self.fallback:
                candidates.append(os.path.normpath(os.path.join(fb, rel)))
        for c in candidates:
            if os.path.exists(c):
                return c
        return candidates[0]

    def url(self, raw, base_dir):
        text = raw.strip()
        m = re.match(r"^url\(\s*(.*?)\s*\)$", text, re.S)
        if m:
            text = m.group(1)
        if len(text) > 1 and text[0] in "\"'" and text[-1] == text[0]:
            text = text[1:-1]
        if text.startswith(("project:", "resource(", "unity-theme:")):
            return None
        full = self.resolve_path(text, base_dir)
        try:
            rel = posix(os.path.relpath(full, self.out_dir))
        except ValueError:
            rel = posix(full)
        return rel

    def selector(self, text):
        sel = parse_selector(text)
        parts = []
        for index, (comb, compound) in enumerate(sel.parts):
            s = ""
            if compound.tag:
                s += "ui-" + compound.tag.lower()
            if compound.universal and not compound.tag:
                s += "*"
            if compound.id:
                s += "#" + esc_ident(compound.id)
            for cls in compound.classes:
                s += "." + esc_ident(cls)
            for name, arg in compound.pseudos:
                if name == "hover":
                    s += ":hover"
                elif name == "active":
                    s += ":active"
                elif name == "focus":
                    s += ":focus"
                elif name == "disabled":
                    s += "[disabled]"
                elif name == "enabled":
                    s += ":not([disabled])"
                elif name == "checked":
                    s += "[checked]"
                elif name == "inactive":
                    s += ":not(:active)"
                elif name == "root":
                    s += ".uitk-root"
                else:
                    return None, ":" + name
            if not s:
                s = "*"
            parts.append((comb, s))
        css = ""
        for comb, s in parts:
            if comb is None:
                css += s
            elif comb == " ":
                css += " " + s
            else:
                css += " > " + s
        for kind, snippet in sel.problems:
            if kind in ("combinator", "attr", "pelem", "functional", "structural"):
                return None, snippet
        return css, None

    def declarations(self, rule, base_dir, where):
        out = []
        names = dict((d.name, d) for d in rule.decls)
        for d in rule.decls:
            n, v = d.name, d.value.strip()
            line = "%s:%d" % (where, d.line)
            if n.startswith("--"):
                out.append((n, v))
                continue
            if n not in uitk.USS_PROPERTIES:
                self.report.add("unsupported-uss", "%s: %s" % (n, v[:50]), line, "not a USS property of Unity 6000.6")
                continue
            if n == "background-image":
                if v == "none":
                    out.append(("--u-img", "none"))
                elif "url(" in v:
                    url = self.url(v, base_dir)
                    if url is None:
                        self.report.add("preview-approximation", "background-image %s not resolvable" % v[:50], line)
                    else:
                        out.append(("--u-img", 'url("%s")' % url))
                else:
                    self.report.add("preview-approximation", "background-image %s" % v[:50], line)
            elif n.startswith("-unity-slice-") and n != "-unity-slice-type":
                key = {"left": "--u-sl", "top": "--u-st", "right": "--u-sr", "bottom": "--u-sb", "scale": "--u-ss"}[n.rsplit("-", 1)[1]]
                out.append((key, re.sub(r"px$", "", v)))
            elif n == "-unity-background-scale-mode":
                if v == "stretch-to-fill":
                    out.append(("--m-stretch", "initial"))
                    out.append(("--m-fit", " "))
                elif v == "scale-to-fit":
                    out.append(("--m-stretch", " "))
                    out.append(("--m-fit", "initial"))
                    out.append(("--u-size", "contain"))
                elif v == "scale-and-crop":
                    out.append(("--m-stretch", " "))
                    out.append(("--m-fit", "initial"))
                    out.append(("--u-size", "cover"))
            elif n == "background-size":
                out.append(("--u-size", v.replace("auto", "auto")))
                out.append(("--m-stretch", " "))
                out.append(("--m-fit", "initial"))
            elif n == "background-position":
                out.append(("--u-pos", v))
            elif n == "background-repeat":
                out.append(("--u-rep", v))
            elif n == "-unity-background-image-tint-color":
                if re.sub(r"\s+", "", v.lower()) not in ("rgb(255,255,255)", "#ffffff", "#fff", "white", "rgba(255,255,255,1)"):
                    if (line, v) not in self.tint_seen:
                        self.tint_seen.add((line, v))
                        self.report.add("preview-approximation", "-unity-background-image-tint-color: %s ignored" % v, line, "tint multiplication is not reproduced")
            elif n == "-unity-text-align":
                m = re.match(r"^(upper|middle|lower)-(left|center|right)$", v)
                if m:
                    out.append(("--u-ta-v", {"upper": "flex-start", "middle": "center", "lower": "flex-end"}[m.group(1)]))
                    out.append(("--u-ta-h", m.group(2)))
            elif n == "-unity-font-style":
                out.append(("font-weight", "700" if "bold" in v else "400"))
                out.append(("font-style", "italic" if "italic" in v else "normal"))
            elif n in ("-unity-font-definition", "-unity-font"):
                url = self.url(v, base_dir) if "url(" in v else None
                if url:
                    fam = "uitk-font-%d" % (len(self.fonts) + 1)
                    for key, val in self.fonts.items():
                        if val == url:
                            fam = key
                    self.fonts[fam] = url
                    out.append(("font-family", '"%s", sans-serif' % fam))
                else:
                    self.report.add("preview-approximation", "%s: %s not resolvable" % (n, v[:50]), line)
            elif n.startswith("-unity-"):
                self.report.add("preview-approximation", "%s has no browser equivalent" % n, line)
            elif n.startswith("transition"):
                if self.cfg.get("preview.transitions"):
                    out.append((n, v))
            elif n == "flex" and re.match(r"^\d+(\.\d+)?$", v):
                out.append((n, v + " 1 0%"))
            elif n in uitk.LENGTH_PROPS or n in ("left", "top", "right", "bottom"):
                out.append((n, re.sub(r"(^|\s)(-?\d*\.?\d+)(?=\s|$)", lambda m: m.group(1) + (m.group(2) if float(m.group(2)) == 0 else m.group(2) + "px"), v)))
            elif n == "display":
                out.append((n, v))
            else:
                out.append((n, v))
        return out

    def sheet(self, path, text, base_dir=None):
        base_dir = base_dir or os.path.dirname(path)
        sheet = parse_stylesheet(text, path)
        label = os.path.basename(path)
        for line, kind, snippet in sheet.errors:
            self.report.add("unsupported-uss", snippet[:50], "%s:%d" % (label, line), "syntax: %s" % kind)
        for at in sheet.at_rules:
            if at.name == "import":
                continue
            self.report.add("unsupported-uss", "@%s" % at.name, "%s:%d" % (label, at.line), "at-rule is not part of USS")
        lines = []
        for rule in sheet.rules:
            if rule.at:
                continue
            selectors = []
            for text_, line in rule.selectors:
                css, bad = self.selector(text_)
                if css is None:
                    self.report.add("unsupported-uss", "selector %s" % text_[:60], "%s:%d" % (label, line), "unsupported part %s" % bad)
                else:
                    selectors.append(css)
            decls = self.declarations(rule, base_dir, label)
            if selectors and decls:
                lines.append("%s{%s}" % (",".join(selectors), ";".join("%s:%s" % d for d in decls)))
        return "\n".join(lines)


def load_sheet_chain(path, translator, seen, theme_css):
    if path in seen:
        return ""
    seen.add(path)
    text = read_text(path)
    out = []
    sheet = parse_stylesheet(text, path)
    for at in sheet.at_rules:
        if at.name == "import":
            m = re.search(r"url\(\s*[\"']?([^\"')]+)[\"']?\s*\)", at.prelude)
            if m:
                target = m.group(1)
                if target.startswith("unity-theme://"):
                    out.append(theme_css)
                else:
                    sub = translator.resolve_path(target, os.path.dirname(path))
                    if os.path.isfile(sub):
                        out.append(load_sheet_chain(sub, translator, seen, theme_css))
    out.append(translator.sheet(path, text))
    return "\n".join(out)


def esc(text):
    return (text or "").replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")


class Renderer(object):
    def __init__(self, cfg, table, locale, report, overrides, bindings, fragments):
        self.cfg = cfg
        self.table = table
        self.locale = locale
        self.report = report
        self.overrides = overrides
        self.bindings = bindings
        self.fragments = fragments

    def text_of(self, node, attr):
        name = node.name
        if name in self.overrides:
            return self.overrides[name]
        raw = node.attrs.get(attr, "")
        key = self.bindings.get(name)
        if key and self.table.has(key):
            return self.table.get(key, self.locale)
        if raw and self.table.has(raw):
            return self.table.get(raw, self.locale)
        return raw

    def attrs(self, node, extra_classes, tag, extra=""):
        classes = list(extra_classes) + node.classes
        a = ' data-ui="%s"' % node.type
        if node.name:
            a += ' id="%s" data-name="%s"' % (esc(node.name), esc(node.name))
        if classes:
            a += ' class="%s"' % esc(" ".join(classes))
        if node.attrs.get("enabled", "").lower() == "false":
            a += " disabled"
        style = node.attrs.get("style", "")
        if style:
            a += ' style="%s"' % esc(style)
        if node.type in FOCUSABLE or node.attrs.get("focusable") == "true":
            a += ' tabindex="0"'
        return a + extra

    def render(self, node):
        t = node.type
        children = list(node.children)
        if node.name in self.fragments:
            children = children + self.fragments[node.name]
        inner = "".join(self.render(c) for c in children)
        tag = "ui-" + t.lower()
        if t == "VisualElement" or t == "TemplateContainer" or t == "Box":
            cls = ["unity-box"] if t == "Box" else []
            return "<%s%s>%s</%s>" % (tag, self.attrs(node, cls, tag), inner, tag)
        if t in TEXT_TYPES:
            text = self.text_of(node, "text")
            cls = ["unity-text-element", "unity-label" if t == "Label" else "unity-button" if t == "Button" else "unity-text-element"]
            return "<%s%s data-ui-text>%s%s</%s>" % (tag, self.attrs(node, cls, tag), esc(text), inner, tag)
        if t == "Image":
            return "<%s%s></%s>" % (tag, self.attrs(node, ["unity-image"], tag), tag)
        if t == "Toggle" or t == "RadioButton":
            label = self.text_of(node, "label")
            checked = node.attrs.get("value", "").lower() == "true"
            cls = ["unity-base-field", "unity-base-boolean-field", "unity-toggle" if t == "Toggle" else "unity-radio-button"]
            body = ""
            if label:
                body += '<ui-label data-ui="Label" data-ui-text class="unity-text-element unity-label unity-base-field__label unity-toggle__label">%s</ui-label>' % esc(label)
            body += '<ui-visualelement data-ui="VisualElement" class="unity-base-field__input unity-base-boolean-field__input unity-toggle__input"><ui-visualelement data-ui="VisualElement" class="unity-toggle__checkmark"></ui-visualelement></ui-visualelement>'
            return "<%s%s%s>%s</%s>" % (tag, self.attrs(node, cls, tag), " checked" if checked else "", body, tag)
        if t in ("TextField", "IntegerField", "FloatField"):
            label = self.text_of(node, "label")
            value = self.overrides.get(node.name + "#value", node.attrs.get("value", ""))
            cls = ["unity-base-field", "unity-base-text-field", "unity-text-field"]
            body = ""
            if label:
                body += '<ui-label data-ui="Label" data-ui-text class="unity-text-element unity-label unity-base-field__label unity-base-text-field__label unity-text-field__label">%s</ui-label>' % esc(label)
            body += '<ui-visualelement data-ui="VisualElement" class="unity-base-text-field__input unity-base-field__input unity-text-field__input"><ui-textelement data-ui="TextElement" data-ui-text class="unity-text-element unity-text-element--inner-input-field-component">%s</ui-textelement></ui-visualelement>' % esc(value)
            return "<%s%s>%s</%s>" % (tag, self.attrs(node, cls, tag), body, tag)
        if t == "DropdownField":
            label = self.text_of(node, "label")
            choices = [c for c in (node.attrs.get("choices") or "").split(",") if c]
            idx = node.attrs.get("index")
            shown = ""
            if node.name + "#value" in self.overrides:
                shown = self.overrides[node.name + "#value"]
            elif choices and idx is not None and idx.isdigit() and int(idx) < len(choices):
                shown = choices[int(idx)]
                shown = self.table.get(shown, self.locale) or shown
            cls = ["unity-base-field", "unity-base-popup-field", "unity-popup-field"]
            body = ""
            if label:
                body += '<ui-label data-ui="Label" data-ui-text class="unity-text-element unity-label unity-base-field__label unity-popup-field__label">%s</ui-label>' % esc(label)
            body += '<ui-visualelement data-ui="VisualElement" class="unity-base-popup-field__input unity-base-field__input"><ui-textelement data-ui="TextElement" data-ui-text class="unity-text-element unity-base-popup-field__text">%s</ui-textelement><ui-visualelement data-ui="VisualElement" class="unity-base-popup-field__arrow"></ui-visualelement></ui-visualelement>' % esc(shown)
            return "<%s%s>%s</%s>" % (tag, self.attrs(node, cls, tag), body, tag)
        if t in ("Slider", "SliderInt"):
            label = self.text_of(node, "label")
            low = float(node.attrs.get("low-value", 0) or 0)
            high = float(node.attrs.get("high-value", 10) or 10)
            val = float(node.attrs.get("value", low) or low)
            frac = 0 if high == low else max(0.0, min(1.0, (val - low) / (high - low)))
            cls = ["unity-base-slider", "unity-base-slider--horizontal", "unity-slider", "unity-base-field"]
            body = ""
            if label:
                body += '<ui-label data-ui="Label" data-ui-text class="unity-text-element unity-label unity-base-field__label">%s</ui-label>' % esc(label)
            body += ('<ui-visualelement data-ui="VisualElement" class="unity-base-slider__input unity-base-field__input"><ui-visualelement data-ui="VisualElement" class="unity-base-slider__drag-container">'
                     '<ui-visualelement data-ui="VisualElement" class="unity-base-slider__tracker"></ui-visualelement>'
                     '<ui-visualelement data-ui="VisualElement" class="unity-base-slider__dragger-border"></ui-visualelement>'
                     '<ui-visualelement data-ui="VisualElement" class="unity-base-slider__dragger" style="--u-t:%s"></ui-visualelement></ui-visualelement></ui-visualelement>') % ("%.4f" % frac)
            return "<%s%s>%s</%s>" % (tag, self.attrs(node, cls, tag), body, tag)
        if t == "ScrollView":
            body = ('<ui-visualelement data-ui="VisualElement" class="unity-content-and-vertical-scroll-container" data-name="unity-content-and-vertical-scroll-container">'
                    '<ui-visualelement data-ui="VisualElement" class="unity-scroll-view__content-viewport" data-name="unity-content-viewport">'
                    '<ui-visualelement data-ui="VisualElement" class="unity-scroll-view__content-container" data-name="unity-content-container">%s</ui-visualelement></ui-visualelement>'
                    '<ui-scroller data-ui="Scroller" class="unity-scroller unity-scroller--vertical unity-scroll-view__vertical-scroller">'
                    '<ui-button data-ui="Button" class="unity-scroller__low-button"></ui-button>'
                    '<ui-slider data-ui="Slider" class="unity-base-slider unity-scroller__slider unity-base-slider--vertical"><ui-visualelement data-ui="VisualElement" class="unity-base-slider__input"><ui-visualelement data-ui="VisualElement" class="unity-base-slider__drag-container">'
                    '<ui-visualelement data-ui="VisualElement" class="unity-base-slider__tracker"></ui-visualelement><ui-visualelement data-ui="VisualElement" class="unity-base-slider__dragger-border"></ui-visualelement><ui-visualelement data-ui="VisualElement" class="unity-base-slider__dragger" style="--u-t:0"></ui-visualelement></ui-visualelement></ui-visualelement></ui-slider>'
                    '<ui-button data-ui="Button" class="unity-scroller__high-button"></ui-button></ui-scroller></ui-visualelement>'
                    '<ui-scroller data-ui="Scroller" class="unity-scroller unity-scroller--horizontal unity-scroll-view__horizontal-scroller"></ui-scroller>') % inner
            return "<%s%s>%s</%s>" % (tag, self.attrs(node, ["unity-scroll-view", "unity-scroll-view--vertical"], tag), body, tag)
        if t == "Foldout":
            text = self.text_of(node, "text")
            body = ('<ui-toggle data-ui="Toggle" class="unity-foldout__toggle unity-toggle"><ui-visualelement data-ui="VisualElement" class="unity-toggle__input"><ui-visualelement data-ui="VisualElement" class="unity-toggle__checkmark"></ui-visualelement></ui-visualelement>'
                    '<ui-label data-ui="Label" data-ui-text class="unity-text-element unity-label unity-foldout__text">%s</ui-label></ui-toggle>'
                    '<ui-visualelement data-ui="VisualElement" class="unity-foldout__content">%s</ui-visualelement>') % (esc(text), inner)
            return "<%s%s>%s</%s>" % (tag, self.attrs(node, ["unity-foldout"], tag), body, tag)
        if t == "ProgressBar":
            title = self.text_of(node, "title")
            low = float(node.attrs.get("low-value", 0) or 0)
            high = float(node.attrs.get("high-value", 100) or 100)
            val = float(node.attrs.get("value", 0) or 0)
            frac = 0 if high == low else max(0.0, min(1.0, (val - low) / (high - low)))
            body = ('<ui-visualelement data-ui="VisualElement" class="unity-progress-bar__container"><ui-visualelement data-ui="VisualElement" class="unity-progress-bar__background">'
                    '<ui-visualelement data-ui="VisualElement" class="unity-progress-bar__progress" style="--u-t:%.4f"></ui-visualelement></ui-visualelement>'
                    '<ui-label data-ui="Label" data-ui-text class="unity-text-element unity-label unity-progress-bar__title">%s</ui-label></ui-visualelement>') % (frac, esc(title))
            return "<%s%s>%s</%s>" % (tag, self.attrs(node, ["unity-progress-bar"], tag), body, tag)
        if t == "GroupBox":
            text = self.text_of(node, "text")
            lab = '<ui-label data-ui="Label" data-ui-text class="unity-text-element unity-label unity-group-box__label">%s</ui-label>' % esc(text) if text else ""
            return "<%s%s>%s%s</%s>" % (tag, self.attrs(node, ["unity-group-box"], tag), lab, inner, tag)
        self.report.add("preview-approximation", "%s drawn as a plain container" % t, node.name)
        return "<%s%s>%s</%s>" % (tag, self.attrs(node, ["unity-" + t.lower()], tag), inner, tag)


def code_bindings(cfg):
    spec = cfg.get("preview.codeBindings")
    result = {}
    if not spec:
        return result
    consts = {}
    cons = spec.get("constants") or {}
    import glob as _glob
    for pattern in cons.get("files", []):
        for path in sorted(_glob.glob(os.path.join(cfg.project_root, pattern), recursive=True)):
            for m in re.finditer(cons.get("pattern", r"const string (?P<id>\w+)\s*=\s*\"(?P<value>[^\"]+)\""), read_text(path)):
                consts[m.group("id")] = m.group("value")
    for pattern in spec.get("sources", []):
        for path in sorted(_glob.glob(os.path.join(cfg.project_root, pattern), recursive=True)):
            text = read_text(path)
            for rx in spec.get("patterns", []):
                for m in re.finditer(rx, text):
                    key = m.group("key").strip()
                    lit = re.match(r'^"([^"]+)"$', key)
                    if lit:
                        key = lit.group(1)
                    else:
                        key = consts.get(key.split(".")[-1], key)
                    result[m.group("name")] = key
    return result


def load_fragments(specs, report):
    out = {}
    for name, path in specs.items():
        if not os.path.isfile(path):
            report.add("preview-approximation", "fragment %s missing" % path, name)
            continue
        holder, _ = read_uxml(path, report)
        out[name] = holder.children
    return out


def build_page(uxml_path, cfg, locale=None, viewports=None, title=None, out_path=None, fragments=None, overrides=None, backdrop=None):
    report = Report()
    out_dir = os.path.dirname(os.path.abspath(out_path)) if out_path else os.getcwd()
    fallback = [cfg.abs_path(p) for p in cfg.get("preview.fallbackDirs", [])]
    translator = Translator(cfg, out_dir, report, fallback)
    locales_cfg = cfg.get("localization.locales", ["en"])
    locale = locale or cfg.get("preview.locale") or cfg.get("localization.mockLocale")
    paths = []
    for template in cfg.get("localization.files", []):
        screen = os.path.splitext(os.path.basename(uxml_path))[0]
        paths.append(cfg.abs_path(template.replace("{screen}", screen)))
    table = load_table(paths, cfg.get("localization.format"))
    theme_css = uitk.read_asset("uitk-theme.css")
    seen = set()
    css_parts = [uitk.read_asset("uitk-base.css")]
    if cfg.get("preview.defaultTheme"):
        css_parts.append(theme_css)
    for theme in cfg.get("preview.theme", []):
        tp = cfg.abs_path(theme)
        if os.path.isfile(tp):
            css_parts.append(load_sheet_chain(tp, translator, seen, theme_css if not cfg.get("preview.defaultTheme") else ""))
        else:
            report.add("preview-approximation", "theme %s not found" % theme)
    root, style_paths = read_uxml(uxml_path, report)
    for sp in style_paths:
        full = sp if os.path.isfile(sp) else None
        if full is None:
            rel = sp
            for fb in fallback:
                cand = os.path.normpath(os.path.join(fb, os.path.relpath(sp, os.path.dirname(uxml_path))))
                if os.path.isfile(cand):
                    full = cand
                    break
        if full is None:
            report.add("preview-approximation", "style %s not found" % sp)
            continue
        css_parts.append(load_sheet_chain(full, translator, seen, ""))
    font_css = "".join('@font-face{font-family:"%s";src:url("%s")}' % (fam, url) for fam, url in translator.fonts.items())
    fragment_specs = dict(cfg.get("preview.fragments", {}))
    fragment_specs = dict((k, cfg.abs_path(v)) for k, v in fragment_specs.items())
    if fragments:
        fragment_specs.update(fragments)
    frags = load_fragments(fragment_specs, report)
    bindings = code_bindings(cfg)
    ov = dict(cfg.get("preview.textOverrides", {}))
    if overrides:
        ov.update(overrides)
    renderer = Renderer(cfg, table, locale, report, ov, bindings, frags)
    dom = renderer.render(root)
    panel = parse_panel_settings(cfg.abs_path(cfg.get("preview.panelSettings")) if cfg.get("preview.panelSettings") else None)
    vps = viewports or cfg.get("preview.viewports", ["1920x1080"])
    sections = []
    for vp in vps:
        w, h = [int(x) for x in vp.split("x")]
        s, lw, lh = panel_geometry(panel, w, h)
        sections.append('<h2 class="uitk-h">%s (panel scale %.4f, logical %dx%d)</h2><div class="uitk-screen" style="width:%dpx;height:%dpx"><div class="uitk-panel" style="width:%.2fpx;height:%.2fpx;transform:scale(%.5f)">%s</div></div>' % (vp, s, round(lw), round(lh), w, h, lw, lh, s, dom))
    bd = backdrop or cfg.get("preview.backdrop")
    chrome = ('.uitk-h{font:14px sans-serif;color:#9fb0ba;margin:12px 0 6px}.uitk-screen{position:relative;overflow:hidden;background:%s;margin-bottom:16px}'
              '.uitk-panel{position:absolute;left:0;top:0;transform-origin:0 0;overflow:hidden}.uitk-root{position:absolute;left:0;top:0;right:0;bottom:0}'
              'html,body{width:auto;height:auto;overflow:auto;background:#05080b}.uitk-report{font:12px sans-serif;color:#b9c4c9;margin:12px}.uitk-report li{margin:2px 0}') % (bd.replace(";", "") if bd.startswith("#") or bd.startswith("rgb") else "#101418")
    items = {}
    for it in report.items:
        items.setdefault(it["kind"], []).append(it)
    rep = ""
    for kind in sorted(items):
        rep += "<details class='uitk-report'><summary>%s (%d)</summary><ul>%s</ul></details>" % (kind, len(items[kind]), "".join("<li>%s <i>%s</i> %s</li>" % (esc(i["what"]), esc(i["where"]), esc(i["why"])) for i in items[kind]))
    html_text = ('<!DOCTYPE html>\n<html lang="%s"><head><meta charset="utf-8"><title>%s</title>\n<style>\n%s\n</style>\n<style>%s</style>\n<style>%s</style>\n</head>\n<body>\n%s\n%s\n'
                 '<script>(function(){var s=document.querySelectorAll("ui-scrollview");for(var i=0;i<s.length;i++){var v=s[i].querySelector(".unity-scroll-view__content-viewport");var c=v&&v.firstElementChild;var sc=s[i].querySelector(".unity-scroller--vertical");if(v&&c&&sc&&c.scrollHeight<=v.clientHeight+1){sc.style.display="none"}}})();</script>\n</body></html>\n') % (
        locale or "en", esc(title or os.path.basename(uxml_path)), "\n".join(css_parts), font_css, chrome, "\n".join(sections), rep)
    return html_text, report
