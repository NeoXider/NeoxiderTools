import os
import re

from . import uitk
from .cssparse import (
    format_color,
    format_length,
    is_color_literal,
    parse_color,
    parse_number,
    parse_selector,
    parse_stylesheet,
    split_top,
    split_ws,
    var_refs,
)
from .rules import advice_for, make_finding
from .util import number, posix

MATH_RE = re.compile(r"\b(calc|min|max|clamp|env|attr)\(", re.I)
GRADIENT_RE = re.compile(r"\b(?:repeating-)?(?:linear|radial|conic)-gradient\(", re.I)
CSS_WIDE = {"inherit", "unset", "revert", "revert-layer"}
BAD_UNITS = {"em", "rem", "vh", "vw", "vmin", "vmax", "pt", "ch", "ex", "fr", "cm", "mm", "in", "pc"}
TIME_RE = re.compile(r"^([+-]?(?:\d+\.?\d*|\.\d+))(s|ms)$", re.I)
SELECTOR_RULES = {
    "combinator": "C015",
    "attr": "C015",
    "pelem": "C005",
    "functional": "C003",
    "structural": "C004",
    "pseudo": "C035",
    "type": "C016",
    "syntax": "C029",
}
GROUP_ORDER = [
    ("position", "left", "top", "right", "bottom", "display", "visibility", "overflow", "opacity"),
    ("flex-direction", "flex-wrap", "flex-grow", "flex-shrink", "flex-basis", "flex", "align-content", "align-items", "align-self", "justify-content"),
    ("width", "min-width", "max-width", "height", "min-height", "max-height", "aspect-ratio"),
    ("margin", "margin-top", "margin-right", "margin-bottom", "margin-left"),
    ("padding", "padding-top", "padding-right", "padding-bottom", "padding-left"),
    ("border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
     "border-color", "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
     "border-radius", "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius"),
    ("background-color", "background-image", "background-position", "background-position-x", "background-position-y",
     "background-repeat", "background-size", "-unity-background-scale-mode", "-unity-background-image-tint-color",
     "-unity-slice-left", "-unity-slice-top", "-unity-slice-right", "-unity-slice-bottom", "-unity-slice-scale", "-unity-slice-type"),
    ("color", "-unity-font-definition", "-unity-font", "-unity-font-style", "font-size", "letter-spacing", "word-spacing",
     "-unity-text-align", "white-space", "text-overflow", "text-shadow", "-unity-text-outline", "-unity-text-outline-width", "-unity-text-outline-color"),
    ("translate", "rotate", "scale", "transform-origin", "cursor", "filter", "backdrop-filter"),
    ("transition-property", "transition-duration", "transition-timing-function", "transition-delay"),
]
GROUP_INDEX = {}
for _g, _names in enumerate(GROUP_ORDER):
    for _i, _n in enumerate(_names):
        GROUP_INDEX[_n] = (_g, _i)
SHORTHANDS = {
    "margin": ("margin-top", "margin-right", "margin-bottom", "margin-left"),
    "padding": ("padding-top", "padding-right", "padding-bottom", "padding-left"),
    "border-width": ("border-top-width", "border-right-width", "border-bottom-width", "border-left-width"),
    "border-color": ("border-top-color", "border-right-color", "border-bottom-color", "border-left-color"),
    "border-radius": ("border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius"),
    "flex": ("flex-grow", "flex-shrink", "flex-basis"),
    "background-position": ("background-position-x", "background-position-y"),
}


class OutRule(object):
    __slots__ = ("selectors", "decls", "line", "order", "token", "specs")

    def __init__(self, selectors, decls, line, order, specs):
        self.selectors = selectors
        self.decls = decls
        self.line = line
        self.order = order
        self.specs = specs
        self.token = False


class CssContext(object):
    def __init__(self, cfg, report, file_label, html_dir=None):
        self.cfg = cfg
        self.report = report
        self.file = file_label
        self.html_dir = html_dir
        self.offset = 0
        self.used_vars = []
        self.defined_vars = {}
        self.tokens = {}
        self.token_lines = {}
        self.rules = []
        self.order = 0
        self.keyframes = []
        self.disabled = set(cfg.get("rules.disable", []))

    def emit(self, rule_id, line, col, construct, message=None, severity=None, why=None, fix=None):
        if rule_id in self.disabled:
            return
        make_finding(self.report, rule_id, self.file, self.offset + line, col, construct, message, severity, why, fix)


def norm_time(token):
    match = TIME_RE.match(token.strip())
    if not match:
        return None
    return format_length(float(match.group(1)), match.group(2).lower())


def resolve_url(ctx, raw, decl):
    text = raw.strip()
    if len(text) >= 2 and text[0] in "\"'" and text[-1] == text[0]:
        text = text[1:-1]
    low = text.lower()
    if low.startswith("project://") or low.startswith("/") or low.startswith("resource("):
        return 'url("%s")' % text
    if low.startswith(("http:", "https:", "data:", "file:", "//")):
        ctx.emit("D012", decl.line, decl.col, "url(%s)" % text[:60], "image '%s' is not inside the project" % text[:60])
        return None
    if ctx.html_dir is None:
        return 'url("%s")' % text
    full = os.path.normpath(os.path.join(ctx.html_dir, text))
    root = ctx.cfg.project_root
    try:
        rel = posix(os.path.relpath(full, root))
    except ValueError:
        rel = None
    if rel is None or rel.startswith(".."):
        ctx.emit("D012", decl.line, decl.col, "url(%s)" % text[:60], "image '%s' resolves outside the project root" % text[:60])
        return None
    if not os.path.exists(full):
        ctx.emit("D012", decl.line, decl.col, "url(%s)" % text[:60], "image file '%s' does not exist" % rel)
        return None
    return 'url("/%s")' % rel


def norm_color_token(ctx, token, decl):
    low = token.lower()
    if "var(" in low:
        return token
    if low in ("currentcolor", "inherit"):
        ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "'currentColor' is not available in USS", fix="Write the colour explicitly or use a variable.")
        return None
    if low.startswith(("hsl(", "hsla(", "hwb(", "lab(", "lch(", "oklab(", "oklch(", "color(", "color-mix(")):
        ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "colour function is not supported by USS (use hex, rgb() or rgba())")
        return None
    parsed = parse_color(token)
    if parsed is None:
        if re.match(r"^[a-z]+$", low):
            return low
        ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "'%s' is not a valid colour" % token)
        return None
    if token.startswith("#") and len(token) in (4, 7):
        return token
    return format_color(parsed)


def check_lengths(ctx, decl, value, allow_pct=True, allow_auto=True, count=None):
    tokens = split_ws(value)
    out = []
    for token in tokens:
        low = token.lower()
        if "var(" in low:
            out.append(token)
            continue
        if low in ("initial",) or (allow_auto and low in ("auto", "none")) or (low == "none" and decl.name.startswith("max-")):
            out.append(low)
            continue
        parsed = parse_number(token)
        if parsed is None:
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "'%s' is not a valid length for '%s'" % (token, decl.name))
            return None
        num, unit = parsed
        if unit in BAD_UNITS:
            ctx.emit("C008", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "unit '%s' is not supported by USS (px and %% only)" % unit)
            return None
        if unit == "" and num != 0:
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "unitless length '%s' is invalid CSS; write %spx" % (token, number(num)))
            return None
        if unit not in ("", "px", "%"):
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "unit '%s' is not valid for '%s'" % (unit, decl.name))
            return None
        if unit == "%" and not allow_pct:
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "percentages are not valid for '%s'" % decl.name)
            return None
        out.append(format_length(num, unit))
    if count is not None and not (count[0] <= len(out) <= count[1]):
        ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "'%s' takes %d to %d values" % (decl.name, count[0], count[1]))
        return None
    return out


def common_value_checks(ctx, decl):
    value = decl.value
    if MATH_RE.search(value):
        ctx.emit("C006", decl.line, decl.col, "%s: %s" % (decl.name, value))
        return False
    if GRADIENT_RE.search(value):
        ctx.emit("C011", decl.line, decl.col, "%s: %s" % (decl.name, value))
        return False
    if value.strip().lower() in CSS_WIDE:
        ctx.emit("C018", decl.line, decl.col, "%s: %s" % (decl.name, value))
        return False
    for name, has_fallback in var_refs(value):
        ctx.used_vars.append((name, has_fallback, ctx.offset + decl.line, decl.col))
    return True


def expand_sides(values):
    if len(values) == 1:
        return [values[0]] * 4
    if len(values) == 2:
        return [values[0], values[1], values[0], values[1]]
    if len(values) == 3:
        return [values[0], values[1], values[2], values[1]]
    return list(values[:4])


def translate_transition_list(ctx, decl, items):
    props, durations, timings, delays = [], [], [], []
    for item in items:
        tokens = split_ws(item)
        prop = None
        times = []
        timing = None
        for token in tokens:
            low = token.lower()
            if norm_time(token) is not None:
                times.append(norm_time(token))
            elif low in uitk.EASINGS or low.startswith(("cubic-bezier(", "steps(")) or low in ("step-start", "step-end"):
                timing = low
            elif prop is None:
                prop = low
            else:
                ctx.emit("R009", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "unexpected token '%s' in the transition" % token)
                return None
        if prop is None:
            prop = "all"
        if len(times) > 2:
            ctx.emit("R009", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "more than two time values")
            return None
        if timing is not None and timing not in uitk.EASINGS:
            ctx.emit("R007", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "easing '%s' is not supported by USS" % timing)
            return None
        expanded = [prop]
        if prop == "transform":
            expanded = ["translate", "rotate", "scale"]
        for single in expanded:
            if single not in ("all", "none", "initial", "ignored") and single not in uitk.USS_PROPERTIES and not single.startswith("--"):
                ctx.emit("C023", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "transition property '%s' is not a USS property" % single)
                return None
            if single in uitk.USS_PROPERTIES and single not in uitk.ANIMATABLE_FULL:
                ctx.emit("R006", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "'%s' does not animate in UI Toolkit" % single)
            if single in uitk.LAYOUT_PROPERTIES and single not in ("all",):
                ctx.emit("R008", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "transition on layout property '%s'" % single)
            props.append(single)
            durations.append(times[0] if times else "0s")
            timings.append(timing or "ease")
            delays.append(times[1] if len(times) > 1 else "0s")
    return props, durations, timings, delays


def _join(values):
    return ", ".join(values)


def handle_transition(ctx, decl, out):
    items = [part.strip() for part in split_top(decl.value, ",") if part.strip()]
    if decl.name == "transition":
        if not items or decl.value.strip().lower() in ("none",):
            out.append(("transition-property", "none"))
            return
        result = translate_transition_list(ctx, decl, items)
        if result is None:
            return
        props, durations, timings, delays = result
        out.append(("transition-property", _join(props)))
        out.append(("transition-duration", _join(durations)))
        if any(t != "ease" for t in timings):
            out.append(("transition-timing-function", _join(timings)))
        if any(d != "0s" for d in delays):
            out.append(("transition-delay", _join(delays)))
        return
    if decl.name == "transition-property":
        fake = [part + " 0s" for part in items]
        result = translate_transition_list(ctx, decl, fake)
        if result is not None:
            out.append(("transition-property", _join(result[0])))
        return
    if decl.name in ("transition-duration", "transition-delay"):
        values = []
        for part in items:
            norm = norm_time(part) if part.lower() != "initial" else "0s"
            if norm is None:
                ctx.emit("R009", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "'%s' is not a time (use s or ms)" % part)
                return
            values.append(norm)
        out.append((decl.name, _join(values)))
        return
    if decl.name == "transition-timing-function":
        values = []
        for part in items:
            low = part.lower()
            if low not in uitk.EASINGS:
                ctx.emit("R007", decl.line, decl.col, "%s: %s" % (decl.name, decl.value), "easing '%s' is not supported by USS" % part)
                return
            values.append(low)
        out.append((decl.name, _join(values)))


def handle_border(ctx, decl, out):
    name = decl.name
    side = None if name == "border" else name.split("-", 1)[1]
    width = style = color = None
    for token in split_ws(decl.value):
        low = token.lower()
        if low in ("none", "hidden", "solid", "dashed", "dotted", "double", "groove", "ridge", "inset", "outset"):
            style = low
        elif parse_number(token) is not None:
            width = token
        else:
            color = token
    if style in ("dashed", "dotted", "double", "groove", "ridge", "inset", "outset"):
        ctx.emit("C024", decl.line, decl.col, "%s: %s" % (name, decl.value))
        return
    parsed_width = parse_number(width) if width is not None else None
    if style in ("none", "hidden") or (parsed_width is not None and parsed_width[0] == 0):
        width = "0"
        color = None
    elif width is None and style == "solid":
        width = "3px"
    width_name = "border-width" if side is None else "border-%s-width" % side
    color_name = "border-color" if side is None else "border-%s-color" % side
    if width is not None:
        fixed = check_lengths(ctx, decl, width, allow_pct=False, allow_auto=False)
        if fixed is None:
            return
        out.append((width_name, fixed[0]))
    if color is not None:
        norm = norm_color_token(ctx, color, decl)
        if norm is None:
            return
        out.append((color_name, norm))


def handle_transform(ctx, decl, out):
    text = decl.value.strip()
    if text.lower() == "none":
        out.append(("rotate", "0deg"))
        return
    match = re.match(r"^(rotate|rotatez|translate|translatex|translatey|scale|scalex|scaley)\(([^()]*)\)$", text, re.I)
    if not match:
        ctx.emit("C022", decl.line, decl.col, "transform: %s" % text)
        return
    func = match.group(1).lower()
    args = [a.strip() for a in match.group(2).split(",")]
    if func in ("rotate", "rotatez"):
        parsed = parse_number(args[0]) if len(args) == 1 else None
        if not parsed or parsed[1] != "deg":
            ctx.emit("C023", decl.line, decl.col, "transform: %s" % text, "rotation must be in deg")
            return
        out.append(("rotate", format_length(parsed[0], "deg")))
    elif func.startswith("translate"):
        values = []
        for arg in args:
            fixed = check_lengths(ctx, decl, arg, allow_auto=False)
            if fixed is None:
                return
            values.append(fixed[0])
        if func == "translatex":
            values = [values[0], "0"]
        elif func == "translatey":
            values = ["0", values[0]]
        out.append(("translate", " ".join(values)))
    else:
        values = []
        for arg in args:
            parsed = parse_number(arg)
            if not parsed or parsed[1] != "":
                ctx.emit("C023", decl.line, decl.col, "transform: %s" % text, "scale takes plain numbers")
                return
            values.append(number(parsed[0]))
        if func == "scalex":
            values = [values[0], "1"]
        elif func == "scaley":
            values = ["1", values[0]]
        out.append(("scale", " ".join(values)))


def translate_rule_decls(ctx, rule):
    out = []
    names = dict((d.name, d) for d in rule.decls)
    handled_font = False
    for decl in rule.decls:
        name = decl.name
        value = decl.value
        if decl.important:
            ctx.emit("C017", decl.line, decl.col, "%s: %s !important" % (name, value))
        if name.startswith("--"):
            common_value_checks(ctx, decl)
            out.append((name, value))
            continue
        if not common_value_checks(ctx, decl):
            continue
        if name in ("gap", "row-gap", "column-gap", "grid-gap", "grid-row-gap", "grid-column-gap"):
            ctx.emit("C002", decl.line, decl.col, "%s: %s" % (name, value))
            continue
        if name == "display":
            low = value.strip().lower()
            if low in ("grid", "inline-grid"):
                ctx.emit("C001", decl.line, decl.col, "display: %s" % value)
            elif low not in ("flex", "none") and "var(" not in low:
                ctx.emit("C019", decl.line, decl.col, "display: %s" % value)
            else:
                out.append((name, low))
            continue
        if name == "position":
            low = value.strip().lower()
            if low not in ("relative", "absolute") and "var(" not in low:
                ctx.emit("C009", decl.line, decl.col, "position: %s" % value)
            else:
                out.append((name, low))
            continue
        if name == "z-index":
            ctx.emit("C021", decl.line, decl.col, "z-index: %s" % value)
            continue
        if name == "box-shadow":
            ctx.emit("C013", decl.line, decl.col, "box-shadow: %s" % value)
            continue
        if name in ("overflow", "overflow-x", "overflow-y"):
            low = value.strip().lower()
            if name != "overflow" or low not in ("visible", "hidden"):
                if name == "overflow" and "var(" in low:
                    out.append((name, value))
                else:
                    ctx.emit("C020", decl.line, decl.col, "%s: %s" % (name, value))
            else:
                out.append((name, low))
            continue
        if name == "box-sizing":
            if value.strip().lower() != "border-box":
                ctx.emit("C025", decl.line, decl.col, "box-sizing: %s" % value)
            continue
        if name in ("filter", "backdrop-filter"):
            ctx.emit("C010", decl.line, decl.col, "%s: %s" % (name, value))
            out.append((name, value))
            continue
        if name == "background":
            low = value.strip().lower()
            if low in ("none", "transparent"):
                out.append(("background-color", "rgba(0, 0, 0, 0)" if low == "transparent" else "rgba(0, 0, 0, 0)"))
            elif len(split_ws(value)) == 1 and (parse_color(value) is not None or "var(" in low):
                norm = norm_color_token(ctx, value.strip(), decl)
                if norm is not None:
                    out.append(("background-color", norm))
            else:
                ctx.emit("C012", decl.line, decl.col, "background: %s" % value)
            continue
        if name == "background-image":
            low = value.strip().lower()
            if len(split_top(value, ",")) > 1:
                ctx.emit("C012", decl.line, decl.col, "background-image: %s" % value[:80])
            elif low == "none" or "var(" in low:
                out.append((name, "none" if low == "none" else value))
            else:
                match = re.match(r"^url\(\s*(.*?)\s*\)$", value.strip(), re.I | re.S)
                if not match:
                    ctx.emit("C023", decl.line, decl.col, "background-image: %s" % value[:80], "expected none or a single url()")
                else:
                    url = resolve_url(ctx, match.group(1), decl)
                    if url is not None:
                        out.append((name, url))
            continue
        if name in ("background-clip", "background-origin", "background-attachment", "background-blend-mode"):
            ctx.emit("C012", decl.line, decl.col, "%s: %s" % (name, value))
            continue
        if name == "text-shadow":
            if len(split_top(value, ",")) > 1:
                ctx.emit("C026", decl.line, decl.col, "text-shadow: %s" % value[:80])
            else:
                tokens = split_ws(value)
                fixed = []
                for token in tokens:
                    if parse_number(token) is not None:
                        got = check_lengths(ctx, decl, token, allow_pct=False, allow_auto=False)
                        if got is None:
                            fixed = None
                            break
                        fixed.append(got[0])
                    else:
                        norm = norm_color_token(ctx, token, decl)
                        if norm is None:
                            fixed = None
                            break
                        fixed.append(norm)
                if fixed is not None:
                    out.append((name, " ".join(fixed)))
            continue
        if name == "border" or name in ("border-top", "border-right", "border-bottom", "border-left"):
            handle_border(ctx, decl, out)
            continue
        if name == "border-style":
            low = value.strip().lower()
            if low in ("none", "hidden"):
                out.append(("border-width", "0"))
            elif low != "solid":
                ctx.emit("C024", decl.line, decl.col, "border-style: %s" % value)
            continue
        if name in ("border-top-style", "border-right-style", "border-bottom-style", "border-left-style"):
            if value.strip().lower() not in ("solid", "none"):
                ctx.emit("C024", decl.line, decl.col, "%s: %s" % (name, value))
            continue
        if name == "border-radius" and "/" in value:
            ctx.emit("C023", decl.line, decl.col, "border-radius: %s" % value, "elliptical border-radius (a / b) is not supported by USS")
            continue
        if name == "transform":
            handle_transform(ctx, decl, out)
            continue
        if name == "inset":
            fixed = check_lengths(ctx, decl, value, count=(1, 4))
            if fixed:
                for key, item in zip(("top", "right", "bottom", "left"), expand_sides(fixed)):
                    out.append((key, item))
            continue
        if name == "flex-flow":
            for token in split_ws(value):
                low = token.lower()
                if low in uitk.ENUMS["flex-direction"]:
                    out.append(("flex-direction", low))
                elif low in uitk.ENUMS["flex-wrap"]:
                    out.append(("flex-wrap", low))
                else:
                    ctx.emit("C023", decl.line, decl.col, "flex-flow: %s" % value)
                    break
            continue
        if name in ("font-weight", "font-style"):
            if handled_font:
                continue
            handled_font = True
            weight = names.get("font-weight")
            style = names.get("font-style")
            bold = False
            italic = False
            ok = True
            if weight is not None:
                low = weight.value.strip().lower()
                if low in ("bold", "bolder"):
                    bold = True
                elif low in ("normal", "lighter"):
                    bold = False
                elif re.match(r"^\d+$", low):
                    bold = int(low) >= 600
                else:
                    ctx.emit("C023", weight.line, weight.col, "font-weight: %s" % weight.value)
                    ok = False
            if style is not None:
                low = style.value.strip().lower()
                if low in ("italic", "oblique"):
                    italic = True
                elif low != "normal":
                    ctx.emit("C023", style.line, style.col, "font-style: %s" % style.value)
                    ok = False
            if ok:
                out.append(("-unity-font-style", "bold-and-italic" if bold and italic else "bold" if bold else "italic" if italic else "normal"))
            continue
        if name == "font-family":
            family = split_top(value, ",")[0].strip().strip("\"'")
            fonts = ctx.cfg.get("fonts", {})
            if family in fonts:
                out.append(("-unity-font-definition", 'url("%s")' % fonts[family]))
            else:
                ctx.emit("C031", decl.line, decl.col, "font-family: %s" % value)
            continue
        if name == "text-align":
            if "-unity-text-align" in names:
                continue
            low = value.strip().lower()
            mapping = {"left": "left", "start": "left", "center": "center", "right": "right", "end": "right", "justify": "left"}
            if low in mapping:
                out.append(("-unity-text-align", "middle-" + mapping[low]))
                ctx.emit("T001", decl.line, decl.col, "text-align: %s" % value)
            else:
                ctx.emit("C023", decl.line, decl.col, "text-align: %s" % value)
            continue
        if name.startswith("transition"):
            handle_transition(ctx, decl, out)
            continue
        if name in ("animation", "animation-name", "animation-duration", "animation-delay", "animation-iteration-count",
                    "animation-direction", "animation-fill-mode", "animation-timing-function", "animation-play-state"):
            ctx.keyframes.append(("animation", decl))
            continue
        if name not in uitk.USS_PROPERTIES:
            ctx.emit("C014", decl.line, decl.col, "%s: %s" % (name, value[:60]), "'%s' is not a USS property" % name, fix=advice_for(name))
            continue
        checked = check_property_value(ctx, decl)
        if checked is not None:
            out.extend(checked)
    return dedupe_decls(out)


def check_property_value(ctx, decl):
    name = decl.name
    value = decl.value.strip()
    low = value.lower()
    if "var(" in low:
        return [(name, value)]
    if low == "initial":
        return [(name, "initial")]
    if name in uitk.ENUMS:
        tokens = split_ws(low)
        allowed = uitk.ENUMS[name]
        if len(tokens) != 1 or tokens[0] not in allowed:
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (name, value), "'%s' is not valid for '%s' (expected %s)" % (value, name, ", ".join(sorted(allowed))))
            return None
        return [(name, tokens[0])]
    if name in uitk.COLOR_PROPS:
        tokens = split_ws(value)
        if name == "border-color":
            if not 1 <= len(tokens) <= 4:
                ctx.emit("C023", decl.line, decl.col, "%s: %s" % (name, value), "1 to 4 colours expected")
                return None
        elif len(tokens) != 1:
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (name, value), "one colour expected")
            return None
        fixed = []
        for token in tokens:
            norm = norm_color_token(ctx, token, decl)
            if norm is None:
                return None
            fixed.append(norm)
        return [(name, " ".join(fixed))]
    if name in uitk.LENGTH_PROPS:
        counts = {"margin": (1, 4), "padding": (1, 4), "border-width": (1, 4), "border-radius": (1, 4)}
        allow_pct = name not in ("border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width", "font-size", "letter-spacing", "word-spacing")
        fixed = check_lengths(ctx, decl, value, allow_pct=allow_pct, allow_auto=name not in ("border-radius", "font-size", "letter-spacing", "word-spacing"), count=counts.get(name))
        if fixed is None:
            return None
        if name == "font-size":
            minimum = ctx.cfg.get("rules.minFontSize")
            parsed = parse_number(fixed[0])
            if minimum and parsed and parsed[1] == "px" and parsed[0] < minimum:
                ctx.emit("R001", decl.line, decl.col, "font-size: %s" % fixed[0], "font-size %s is below the minimum %gpx" % (fixed[0], minimum))
        return [(name, " ".join(fixed))]
    if name in uitk.NUMBER_PROPS:
        if name == "aspect-ratio":
            return [(name, value)]
        parsed = parse_number(value)
        if parsed is None or parsed[1] not in ("", "%"):
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (name, value), "a number is expected")
            return None
        num = parsed[0] / 100.0 if parsed[1] == "%" else parsed[0]
        return [(name, number(num))]
    if name in ("flex",):
        tokens = split_ws(low)
        if tokens == ["none"] or tokens == ["initial"]:
            return [(name, tokens[0])]
        if tokens == ["auto"]:
            ctx.emit("C023", decl.line, decl.col, "flex: auto", "'flex: auto' is not accepted by USS", fix="Use flex: 1 1 auto.")
            return None
        fixed = []
        for token in tokens:
            parsed = parse_number(token)
            if parsed is None and token != "auto":
                ctx.emit("C023", decl.line, decl.col, "flex: %s" % value)
                return None
            if parsed is not None and parsed[1] in BAD_UNITS:
                ctx.emit("C008", decl.line, decl.col, "flex: %s" % value)
                return None
            fixed.append(token if parsed is None else format_length(parsed[0], parsed[1]))
        return [(name, " ".join(fixed))]
    if name in ("rotate",):
        parsed = parse_number(value)
        if parsed is None or parsed[1] != "deg":
            ctx.emit("C023", decl.line, decl.col, "%s: %s" % (name, value), "rotation must be in deg")
            return None
        return [(name, format_length(parsed[0], "deg"))]
    if name in ("translate", "transform-origin", "background-position", "background-position-x", "background-position-y", "background-size"):
        tokens = split_ws(value)
        fixed = []
        for token in tokens:
            parsed = parse_number(token)
            if parsed is None:
                fixed.append(token.lower())
                continue
            if parsed[1] in BAD_UNITS:
                ctx.emit("C008", decl.line, decl.col, "%s: %s" % (name, value))
                return None
            if parsed[1] == "" and parsed[0] != 0:
                ctx.emit("C023", decl.line, decl.col, "%s: %s" % (name, value), "unitless length '%s' is invalid CSS" % token)
                return None
            fixed.append(format_length(parsed[0], parsed[1]))
        return [(name, " ".join(fixed))]
    if name == "scale":
        tokens = split_ws(value)
        fixed = []
        for token in tokens:
            parsed = parse_number(token)
            if parsed is None or parsed[1] != "":
                ctx.emit("C023", decl.line, decl.col, "scale: %s" % value)
                return None
            fixed.append(number(parsed[0]))
        return [(name, " ".join(fixed))]
    if name in ("-unity-font-definition", "-unity-font"):
        return [(name, value)]
    return [(name, value)]


def dedupe_decls(out):
    last = {}
    for index, (name, _) in enumerate(out):
        last[name] = index
    return [item for index, item in enumerate(out) if last[item[0]] == index]


def canonical_order(decls):
    names = [n for n, _ in decls]
    for short, longs in SHORTHANDS.items():
        if short in names and any(l in names for l in longs):
            return decls
    keyed = []
    for index, (name, value) in enumerate(decls):
        group = GROUP_INDEX.get(name)
        if name.startswith("--"):
            keyed.append(((-1, 0, index), (name, value)))
        elif group is None:
            keyed.append(((99, 0, index), (name, value)))
        else:
            keyed.append(((group[0], group[1], index), (name, value)))
    keyed.sort(key=lambda item: item[0])
    return [item for _, item in keyed]


def process_stylesheet(ctx, sheet):
    rules_out = []
    for line, kind, text in sheet.errors:
        if kind == "nested":
            ctx.emit("C028", line, 1, text)
        else:
            ctx.emit("C029", line, 1, text, "CSS syntax error (%s)" % kind)
    for at in sheet.at_rules:
        if at.name in ("keyframes", "-webkit-keyframes"):
            ctx.keyframes.append(("keyframes", at))
            continue
        if at.name == "charset":
            continue
        ctx.emit("C007", at.line, 1, "@%s %s" % (at.name, at.prelude[:50]), "@%s is not supported" % at.name)
    for rule in sheet.rules:
        if rule.at:
            continue
        valid = []
        specs = []
        for text, line in rule.selectors:
            selector = parse_selector(text)
            if selector.problems:
                for kind, snippet in selector.problems:
                    rid = SELECTOR_RULES.get(kind, "C015")
                    if kind == "pseudo":
                        ctx.emit(rid, line, 1, text, "pseudo-class %s is not supported by USS" % snippet)
                    elif kind == "type":
                        ctx.emit(rid, line, 1, text, "type selector '%s' is not supported in the dialect" % snippet)
                    else:
                        ctx.emit(rid, line, 1, text, "%s (%s)" % (RULE_TITLES.get(rid, rid), snippet))
                continue
            valid.append(selector.text())
            specs.append(selector.specificity())
        decls = translate_rule_decls(ctx, rule)
        if not valid:
            continue
        only_root = all(s == ":root" for s in valid)
        out = OutRule(valid, decls, ctx.offset + rule.line, ctx.order, specs)
        ctx.order += 1
        if only_root:
            out.token = any(n.startswith("--") for n, _ in decls)
            for name, value in decls:
                if name.startswith("--"):
                    ctx.tokens[name] = value
                    ctx.token_lines[name] = ctx.offset + rule.line
        for name, value in decls:
            if name.startswith("--"):
                ctx.defined_vars[name] = value
        rules_out.append(out)
    ctx.rules.extend(rules_out)
    return rules_out


def _titles():
    from .rules import RULES
    return dict((key, rule.title) for key, rule in RULES.items())


RULE_TITLES = _titles()
