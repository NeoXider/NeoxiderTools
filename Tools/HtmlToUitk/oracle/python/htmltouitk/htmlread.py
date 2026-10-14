import re
from collections import OrderedDict
from html.parser import HTMLParser

from .uitk import VOID_TAGS
from .util import read_text


class HText(object):
    __slots__ = ("text", "line", "col", "parent")

    def __init__(self, text, line, col):
        self.text = text
        self.line = line
        self.col = col
        self.parent = None


class HNode(object):
    def __init__(self, tag, attrs, line, col):
        self.tag = tag
        self.attrs = attrs
        self.line = line
        self.col = col
        self.children = []
        self.parent = None

    @property
    def name(self):
        return self.attrs.get("name") or self.attrs.get("id")

    @property
    def classes(self):
        return (self.attrs.get("class") or "").split()

    def elements(self):
        return [c for c in self.children if isinstance(c, HNode)]

    def texts(self):
        return [c for c in self.children if isinstance(c, HText)]

    def own_text(self):
        return re.sub(r"\s+", " ", "".join(t.text for t in self.texts())).strip()

    def walk(self):
        yield self
        for child in self.children:
            if isinstance(child, HNode):
                for item in child.walk():
                    yield item


class StyleBlock(object):
    def __init__(self, text, line, attrs):
        self.text = text
        self.line = line
        self.attrs = attrs


class HDoc(object):
    def __init__(self, path, text):
        self.path = path
        self.text = text
        self.body = HNode("body", OrderedDict(), 1, 1)
        self.html_attrs = OrderedDict()
        self.body_attrs = OrderedDict()
        self.styles = []
        self.links = []
        self.metas = []
        self.scripts = []
        self.title = None

    def meta(self, name, default=None):
        for attrs, _, _ in self.metas:
            if attrs.get("name") == name:
                return attrs.get("content", default)
        return default

    def elements(self):
        for child in self.body.children:
            if isinstance(child, HNode):
                for item in child.walk():
                    yield item


class _Builder(HTMLParser):
    def __init__(self, doc):
        HTMLParser.__init__(self, convert_charrefs=False)
        self.doc = doc
        self.stack = [doc.body]
        self.buffer = []
        self.buffer_pos = None
        self.in_head = False
        self.style_attrs = None
        self.style_line = 0
        self.script_attrs = None
        self.script_pos = None
        self.style_buf = []
        self.script_buf = []
        self.title_open = False

    def _flush(self):
        if not self.buffer:
            return
        text = "".join(self.buffer)
        line, col = self.buffer_pos
        self.buffer = []
        self.buffer_pos = None
        if not text.strip():
            return
        node = HText(text, line, col + 1)
        parent = self.stack[-1]
        node.parent = parent
        parent.children.append(node)

    def _add_text(self, text):
        if self.buffer_pos is None:
            self.buffer_pos = self.getpos()
        self.buffer.append(text)

    def handle_data(self, data):
        if self.style_attrs is not None:
            self.style_buf.append(data)
            return
        if self.script_attrs is not None:
            self.script_buf.append(data)
            return
        if self.title_open:
            self.doc.title = (self.doc.title or "") + data
            return
        self._add_text(data)

    def handle_entityref(self, name):
        table = {"amp": "&", "lt": "<", "gt": ">", "quot": '"', "apos": "'", "nbsp": " "}
        self._add_text(table.get(name, "&" + name + ";"))

    def handle_charref(self, name):
        try:
            code = int(name[1:], 16) if name[:1] in ("x", "X") else int(name)
            self._add_text(chr(code))
        except (ValueError, OverflowError):
            self._add_text("&#" + name + ";")

    def handle_comment(self, data):
        return

    def handle_decl(self, decl):
        return

    def _attrs(self, attrs):
        result = OrderedDict()
        for key, value in attrs:
            result[key.lower()] = "" if value is None else value
        return result

    def handle_starttag(self, tag, attrs):
        self._start(tag, attrs, False)

    def handle_startendtag(self, tag, attrs):
        self._start(tag, attrs, True)

    def _start(self, tag, attrs, selfclosing):
        tag = tag.lower()
        values = self._attrs(attrs)
        line, col = self.getpos()
        tag_text = self.get_starttag_text() or ""
        end_line = line + tag_text.count("\n")
        if tag == "html":
            self.doc.html_attrs = values
            return
        if tag == "head":
            self.in_head = True
            return
        if tag == "body":
            self.in_head = False
            self.doc.body_attrs = values
            self.doc.body.attrs = values
            self.doc.body.line = line
            return
        if tag == "title":
            self.title_open = True
            return
        if tag == "meta":
            self.doc.metas.append((values, line, col + 1))
            return
        if tag == "link":
            self.doc.links.append((values, line, col + 1))
            return
        if tag == "style":
            self._flush()
            self.style_attrs = values
            self.style_line = end_line
            self.style_buf = []
            return
        if tag == "script":
            self._flush()
            self.script_attrs = values
            self.script_pos = (line, col)
            self.script_buf = []
            if "src" in values:
                self.doc.scripts.append((values, line, col + 1, ""))
                self.script_attrs = None
            return
        self._flush()
        node = HNode(tag, values, line, col + 1)
        parent = self.stack[-1]
        node.parent = parent
        parent.children.append(node)
        if tag in VOID_TAGS or selfclosing:
            return
        self.stack.append(node)

    def handle_endtag(self, tag):
        tag = tag.lower()
        if tag == "title":
            self.title_open = False
            return
        if tag in ("head", "html", "body", "style", "script"):
            if tag == "style" and self.style_attrs is not None:
                self.doc.styles.append(StyleBlock("".join(self.style_buf), self.style_line, self.style_attrs))
                self.style_attrs = None
            if tag == "script" and self.script_attrs is not None:
                self.doc.scripts.append((self.script_attrs, self.script_pos[0], self.script_pos[1] + 1, "".join(self.script_buf)))
                self.script_attrs = None
            return
        self._flush()
        for index in range(len(self.stack) - 1, 0, -1):
            if self.stack[index].tag == tag:
                del self.stack[index:]
                return

    def close(self):
        self._flush()
        HTMLParser.close(self)


def parse_html(text, path=""):
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    doc = HDoc(path, text)
    builder = _Builder(doc)
    builder.feed(text)
    builder.close()
    return doc


def read_html(path):
    return parse_html(read_text(path), path)
