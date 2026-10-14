import bisect
import re

IDENT = r"-?[_a-zA-Z][_a-zA-Z0-9-]*"
IDENT_RE = re.compile(IDENT)

NAMED_COLORS = {
    "transparent": (0, 0, 0, 0.0),
    "black": (0, 0, 0, 1.0),
    "white": (255, 255, 255, 1.0),
    "red": (255, 0, 0, 1.0),
    "green": (0, 128, 0, 1.0),
    "blue": (0, 0, 255, 1.0),
    "yellow": (255, 255, 0, 1.0),
    "cyan": (0, 255, 255, 1.0),
    "magenta": (255, 0, 255, 1.0),
    "gray": (128, 128, 128, 1.0),
    "grey": (128, 128, 128, 1.0),
    "orange": (255, 165, 0, 1.0),
    "purple": (128, 0, 128, 1.0),
    "pink": (255, 192, 203, 1.0),
    "brown": (165, 42, 42, 1.0),
    "gold": (255, 215, 0, 1.0),
    "silver": (192, 192, 192, 1.0),
}


class Decl(object):
    __slots__ = ("name", "value", "line", "col", "important")

    def __init__(self, name, value, line, col=1, important=False):
        self.name = name
        self.value = value
        self.line = line
        self.col = col
        self.important = important

    def __repr__(self):
        return "Decl(%s: %s)" % (self.name, self.value)


class Rule(object):
    __slots__ = ("selectors", "decls", "line", "at")

    def __init__(self, selectors, decls, line, at=()):
        self.selectors = selectors
        self.decls = decls
        self.line = line
        self.at = at


class AtRule(object):
    __slots__ = ("name", "prelude", "line", "rules", "decls", "has_block")

    def __init__(self, name, prelude, line, has_block):
        self.name = name
        self.prelude = prelude
        self.line = line
        self.rules = []
        self.decls = []
        self.has_block = has_block


class Sheet(object):
    def __init__(self, source=""):
        self.source = source
        self.rules = []
        self.at_rules = []
        self.errors = []


class _Scanner(object):
    def __init__(self, text):
        self.text = text
        self.pos = 0
        self.n = len(text)
        self.starts = [0]
        for match in re.finditer("\n", text):
            self.starts.append(match.end())

    def line(self, index):
        return bisect.bisect_right(self.starts, index)

    def col(self, index):
        return index - self.starts[bisect.bisect_right(self.starts, index) - 1] + 1

    def skip(self):
        text = self.text
        while self.pos < self.n:
            ch = text[self.pos]
            if ch in " \t\r\n\f":
                self.pos += 1
            elif text.startswith("/*", self.pos):
                end = text.find("*/", self.pos + 2)
                self.pos = self.n if end < 0 else end + 2
            else:
                break

    def read_prelude(self, stops):
        text = self.text
        start = self.pos
        depth = 0
        quote = None
        i = self.pos
        while i < self.n:
            ch = text[i]
            if quote:
                if ch == "\\":
                    i += 2
                    continue
                if ch == quote:
                    quote = None
            elif ch in "\"'":
                quote = ch
            elif text.startswith("/*", i):
                end = text.find("*/", i + 2)
                i = self.n if end < 0 else end + 2
                continue
            elif ch in "([":
                depth += 1
            elif ch in ")]":
                depth = max(0, depth - 1)
            elif depth == 0 and ch in stops:
                break
            i += 1
        self.pos = i
        return start, text[start:i]

    def read_block(self):
        text = self.text
        assert text[self.pos] == "{"
        depth = 0
        quote = None
        i = self.pos
        while i < self.n:
            ch = text[i]
            if quote:
                if ch == "\\":
                    i += 2
                    continue
                if ch == quote:
                    quote = None
            elif ch in "\"'":
                quote = ch
            elif text.startswith("/*", i):
                end = text.find("*/", i + 2)
                i = self.n if end < 0 else end + 2
                continue
            elif ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    start = self.pos + 1
                    self.pos = i + 1
                    return start, text[start:i], True
            i += 1
        start = self.pos + 1
        self.pos = self.n
        return start, text[start:], False


def split_top(text, sep):
    parts = []
    depth = 0
    quote = None
    start = 0
    i = 0
    while i < len(text):
        ch = text[i]
        if quote:
            if ch == "\\":
                i += 2
                continue
            if ch == quote:
                quote = None
        elif ch in "\"'":
            quote = ch
        elif ch in "([":
            depth += 1
        elif ch in ")]":
            depth = max(0, depth - 1)
        elif ch == sep and depth == 0:
            parts.append(text[start:i])
            start = i + 1
        i += 1
    parts.append(text[start:])
    return parts


def split_ws(text):
    parts = []
    depth = 0
    quote = None
    current = []
    for ch in text.strip():
        if quote:
            current.append(ch)
            if ch == quote:
                quote = None
            continue
        if ch in "\"'":
            quote = ch
            current.append(ch)
        elif ch in "([":
            depth += 1
            current.append(ch)
        elif ch in ")]":
            depth = max(0, depth - 1)
            current.append(ch)
        elif ch.isspace() and depth == 0:
            if current:
                parts.append("".join(current))
                current = []
        else:
            current.append(ch)
    if current:
        parts.append("".join(current))
    return parts


def strip_comments(text):
    out = []
    i = 0
    n = len(text)
    while i < n:
        if text.startswith("/*", i):
            end = text.find("*/", i + 2)
            end = n if end < 0 else end + 2
            out.append(re.sub(r"[^\n]", "", text[i:end]))
            i = end
        else:
            out.append(text[i])
            i += 1
    return "".join(out)


def parse_decls(scanner, body, body_start):
    decls = []
    errors = []
    offset = 0
    for piece in split_top(body, ";"):
        piece_start = body_start + offset
        offset += len(piece) + 1
        stripped = strip_comments(piece)
        if not stripped.strip():
            continue
        lead = len(piece) - len(piece.lstrip())
        if "{" in stripped:
            errors.append((scanner.line(piece_start + lead), "nested", stripped.strip()[:60]))
            continue
        index = _colon_index(stripped)
        if index < 0:
            errors.append((scanner.line(piece_start + lead), "declaration", stripped.strip()[:60]))
            continue
        name = stripped[:index].strip()
        value = stripped[index + 1:].strip()
        if not name.startswith("--"):
            name = name.lower()
        important = False
        match = re.search(r"\s*!\s*important\s*$", value, re.I)
        if match:
            important = True
            value = value[:match.start()].strip()
        first = stripped.lstrip()
        position = piece_start + (len(piece) - len(piece.lstrip()))
        decls.append(Decl(name, value, scanner.line(position), scanner.col(position), important))
    return decls, errors


def _colon_index(text):
    depth = 0
    quote = None
    for i, ch in enumerate(text):
        if quote:
            if ch == quote:
                quote = None
        elif ch in "\"'":
            quote = ch
        elif ch in "([":
            depth += 1
        elif ch in ")]":
            depth = max(0, depth - 1)
        elif ch == ":" and depth == 0:
            return i
    return -1


def _parse_items(scanner, sheet, container, at_chain, nested):
    while True:
        scanner.skip()
        if scanner.pos >= scanner.n:
            return
        ch = scanner.text[scanner.pos]
        if ch == "}":
            if nested:
                scanner.pos += 1
                return
            scanner.pos += 1
            sheet.errors.append((scanner.line(scanner.pos - 1), "brace", "unmatched '}'"))
            continue
        if ch == "@":
            start = scanner.pos
            scanner.pos += 1
            match = IDENT_RE.match(scanner.text, scanner.pos)
            name = match.group(0).lower() if match else ""
            scanner.pos += len(name)
            _, prelude = scanner.read_prelude(";{")
            line = scanner.line(start)
            if scanner.pos < scanner.n and scanner.text[scanner.pos] == "{":
                at = AtRule(name, prelude.strip(), line, True)
                body_start, body, closed = scanner.read_block()
                if not closed:
                    sheet.errors.append((line, "brace", "unclosed @%s block" % name))
                if name in ("media", "supports", "container", "layer", "document"):
                    sub = Sheet(sheet.source)
                    _parse_items_offset(scanner, sub, body, body_start, at_chain + ("@" + name + " " + prelude.strip(),))
                    at.rules = sub.rules
                    sheet.errors.extend(sub.errors)
                elif name in ("keyframes", "-webkit-keyframes"):
                    sub = Sheet(sheet.source)
                    _parse_items_offset(scanner, sub, body, body_start, at_chain + ("@keyframes " + prelude.strip(),))
                    at.rules = sub.rules
                    sheet.errors.extend(sub.errors)
                else:
                    decls, errors = parse_decls(scanner, body, body_start)
                    at.decls = decls
                sheet.at_rules.append(at)
            else:
                at = AtRule(name, prelude.strip(), line, False)
                sheet.at_rules.append(at)
                if scanner.pos < scanner.n:
                    scanner.pos += 1
            continue
        start, prelude = scanner.read_prelude("{;")
        if scanner.pos >= scanner.n or scanner.text[scanner.pos] == ";":
            sheet.errors.append((scanner.line(start), "declaration", prelude.strip()[:60]))
            scanner.pos += 1
            continue
        line = scanner.line(start + (len(prelude) - len(prelude.lstrip())))
        body_start, body, closed = scanner.read_block()
        if not closed:
            sheet.errors.append((line, "brace", "unclosed block"))
        selectors = []
        offset = 0
        for part in split_top(prelude, ","):
            lead = len(part) - len(part.lstrip())
            text = strip_comments(part).strip()
            if text:
                selectors.append((text, scanner.line(start + offset + lead)))
            offset += len(part) + 1
        decls, errors = parse_decls(scanner, body, body_start)
        for err in errors:
            sheet.errors.append(err)
        rule = Rule(selectors, decls, line, at_chain)
        container.append(rule)


def _parse_items_offset(scanner, sub, body, body_start, at_chain):
    inner = _Scanner(" " * body_start + body)
    inner.starts = scanner.starts
    inner.pos = body_start
    _parse_items(inner, sub, sub.rules, at_chain, False)


def parse_stylesheet(text, source=""):
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    sheet = Sheet(source)
    scanner = _Scanner(text)
    _parse_items(scanner, sheet, sheet.rules, (), False)
    return sheet


class Compound(object):
    __slots__ = ("tag", "id", "classes", "pseudos", "pelems", "attrs", "universal")

    def __init__(self):
        self.tag = None
        self.id = None
        self.classes = []
        self.pseudos = []
        self.pelems = []
        self.attrs = []
        self.universal = False

    def text(self):
        out = ""
        if self.tag:
            out += self.tag
        if self.universal:
            out += "*"
        if self.id:
            out += "#" + self.id
        for cls in self.classes:
            out += "." + cls
        for name, arg in self.pseudos:
            out += ":" + name + ("(" + arg + ")" if arg is not None else "")
        for name in self.pelems:
            out += "::" + name
        for attr in self.attrs:
            out += "[" + attr + "]"
        return out or "*"


class Selector(object):
    def __init__(self, raw):
        self.raw = raw
        self.parts = []
        self.problems = []

    def text(self):
        pieces = []
        for combinator, compound in self.parts:
            if combinator is None:
                pieces.append(compound.text())
            elif combinator == " ":
                pieces.append(" " + compound.text())
            else:
                pieces.append(" " + combinator + " " + compound.text())
        return "".join(pieces)

    def specificity(self):
        ids = classes = types = 0
        for _, compound in self.parts:
            if compound.id:
                ids += 1
            classes += len(compound.classes) + len(compound.pseudos) + len(compound.attrs)
            if compound.tag:
                types += 1
        return (ids, classes, types)


STRUCTURAL = {"first-child", "last-child", "only-child", "nth-child", "nth-last-child", "nth-of-type",
              "nth-last-of-type", "first-of-type", "last-of-type", "only-of-type", "empty"}
FUNCTIONAL = {"not", "is", "where", "has", "matches", "any"}
SUPPORTED_PSEUDO = {"hover", "active", "inactive", "focus", "disabled", "enabled", "checked", "root"}


def parse_selector(raw):
    sel = Selector(raw)
    text = raw.strip()
    i = 0
    n = len(text)
    comb = None
    current = Compound()
    started = False
    pending_space = False

    def flush(new_comb):
        nonlocal current, started, comb
        if started:
            sel.parts.append((comb, current))
            comb = new_comb
            current = Compound()
            started = False

    while i < n:
        ch = text[i]
        if ch.isspace():
            pending_space = True
            i += 1
            continue
        if ch in ">+~":
            if text.startswith(">>>", i):
                sel.problems.append(("combinator", ">>>"))
                i += 3
                flush(">")
                pending_space = False
                continue
            if ch != ">":
                sel.problems.append(("combinator", ch))
            flush(ch if ch != ">" else ">")
            i += 1
            pending_space = False
            while i < n and text[i].isspace():
                i += 1
            continue
        if pending_space and started:
            flush(" ")
        pending_space = False
        if ch == "*":
            current.universal = True
            started = True
            i += 1
        elif ch == "#":
            match = IDENT_RE.match(text, i + 1)
            current.id = match.group(0) if match else ""
            i += 1 + (len(match.group(0)) if match else 0)
            started = True
        elif ch == ".":
            match = IDENT_RE.match(text, i + 1)
            current.classes.append(match.group(0) if match else "")
            i += 1 + (len(match.group(0)) if match else 0)
            started = True
        elif ch == ":":
            double = text.startswith("::", i)
            i += 2 if double else 1
            match = IDENT_RE.match(text, i)
            name = match.group(0).lower() if match else ""
            i += len(name)
            arg = None
            if i < n and text[i] == "(":
                depth = 0
                j = i
                while j < n:
                    if text[j] == "(":
                        depth += 1
                    elif text[j] == ")":
                        depth -= 1
                        if depth == 0:
                            break
                    j += 1
                arg = text[i + 1:j]
                i = j + 1
            started = True
            if double or name in ("before", "after", "first-line", "first-letter", "placeholder", "selection", "backdrop", "marker"):
                current.pelems.append(name)
                sel.problems.append(("pelem", "::" + name))
            else:
                current.pseudos.append((name, arg))
                if name in FUNCTIONAL:
                    sel.problems.append(("functional", ":" + name + "()"))
                elif name in STRUCTURAL:
                    sel.problems.append(("structural", ":" + name))
                elif name not in SUPPORTED_PSEUDO:
                    sel.problems.append(("pseudo", ":" + name))
        elif ch == "[":
            j = text.find("]", i)
            j = n if j < 0 else j
            current.attrs.append(text[i + 1:j])
            sel.problems.append(("attr", text[i:j + 1]))
            i = j + 1
            started = True
        else:
            match = IDENT_RE.match(text, i)
            if not match:
                sel.problems.append(("syntax", ch))
                i += 1
                continue
            current.tag = match.group(0)
            sel.problems.append(("type", current.tag))
            i += len(match.group(0))
            started = True
    if started:
        sel.parts.append((comb, current))
    return sel


def match_compound(compound, node):
    if compound.tag and compound.tag != node.tag:
        return False
    if compound.id and compound.id != node.name:
        return False
    for cls in compound.classes:
        if cls not in node.classes:
            return False
    return True


def match_selector(sel, node, index=None):
    if index is None:
        index = len(sel.parts) - 1
    if index < 0:
        return True
    comb, compound = sel.parts[index]
    if not match_compound(compound, node):
        return False
    if index == 0:
        return True
    if comb == ">":
        parent = node.parent
        return parent is not None and match_selector(sel, parent, index - 1)
    parent = node.parent
    while parent is not None:
        if match_selector(sel, parent, index - 1):
            return True
        parent = parent.parent
    return False


LENGTH_RE = re.compile(r"^([+-]?(?:\d+\.?\d*|\.\d+))(px|%|deg|s|ms|em|rem|vh|vw|vmin|vmax|pt|ch|ex|fr|cm|mm|in|pc|rad|turn|grad)?$", re.I)


def parse_number(token):
    match = LENGTH_RE.match(token.strip())
    if not match:
        return None
    return float(match.group(1)), (match.group(2) or "").lower()


HEX_RE = re.compile(r"^#([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")


def parse_color(token):
    token = token.strip()
    low = token.lower()
    if low in NAMED_COLORS:
        return NAMED_COLORS[low]
    match = HEX_RE.match(token)
    if match:
        digits = match.group(1)
        if len(digits) in (3, 4):
            digits = "".join(c * 2 for c in digits)
        r, g, b = int(digits[0:2], 16), int(digits[2:4], 16), int(digits[4:6], 16)
        a = int(digits[6:8], 16) / 255.0 if len(digits) == 8 else 1.0
        return (r, g, b, a)
    match = re.match(r"^rgba?\((.*)\)$", low)
    if match:
        body = match.group(1).replace("/", " ").replace(",", " ")
        parts = body.split()
        if len(parts) not in (3, 4):
            return None
        values = []
        for index, part in enumerate(parts):
            percent = part.endswith("%")
            try:
                number = float(part[:-1] if percent else part)
            except ValueError:
                return None
            if index < 3:
                values.append(int(round(number * 2.55)) if percent else int(round(number)))
            else:
                values.append(number / 100.0 if percent else number)
        alpha = values[3] if len(values) == 4 else 1.0
        return (max(0, min(255, values[0])), max(0, min(255, values[1])), max(0, min(255, values[2])), max(0.0, min(1.0, alpha)))
    return None


def format_color(rgba, authored=None):
    r, g, b, a = rgba
    if authored and HEX_RE.match(authored.strip()) and len(authored.strip()) in (4, 7):
        return authored.strip().upper() if authored.strip().upper() == authored.strip() else authored.strip()
    if a >= 0.9999:
        return "rgb(%d, %d, %d)" % (r, g, b)
    text = ("%.3f" % a).rstrip("0").rstrip(".")
    return "rgba(%d, %d, %d, %s)" % (r, g, b, text)


VAR_RE = re.compile(r"var\(\s*(--[A-Za-z0-9_-]+)\s*(,)?")


def var_refs(value):
    return [(m.group(1), bool(m.group(2))) for m in VAR_RE.finditer(value)]


def is_color_literal(token):
    token = token.strip()
    if HEX_RE.match(token):
        return True
    low = token.lower()
    if low.startswith("rgb(") or low.startswith("rgba(") or low.startswith("hsl(") or low.startswith("hsla("):
        return True
    return False


def format_length(number, unit):
    text = ("%.4f" % number).rstrip("0").rstrip(".")
    if text in ("", "-0"):
        text = "0"
    if text == "0" and unit in ("px", ""):
        return "0"
    return text + unit
