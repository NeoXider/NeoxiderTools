from g3 import *
import json as _j


def pg(css_head, style, body, meta=""):
    return ('<!DOCTYPE html>\n<html lang="en">\n<head>\n<meta charset="utf-8">\n%s'
            '<link rel="stylesheet" href="../../../../assets/uitk-base.css" data-uitk-harness>\n%s'
            '%s</head>\n<body>\n%s</body>\n</html>\n') % (meta, css_head, style, body.strip("\n") + "\n")


def link(href):
    return '<link rel="stylesheet" href="%s">\n' % href


def style(css):
    return "<style>\n" + css.strip("\n") + "\n</style>\n"


def jcfg(prefixes, extra=None, strings_files=None):
    c = {"projectRoot": ".", "uiRoot": "input/ui", "naming": {"prefixes": prefixes}}
    if strings_files:
        c["localization"] = {"files": strings_files, "locales": ["en"], "mockLocale": "en"}
    if extra:
        c.update(extra)
    return c


def jobdoc(html, css=None, uss=None, partials=None, out_extra=None, name=None, assemble="perFile"):
    inputs = {"html": html}
    if css:
        inputs["css"] = css
    if uss:
        inputs["uss"] = uss
    if partials:
        inputs["partials"] = partials
    out = {"root": "out", "assemble": assemble, "folders": {"uxml": "Screens", "uss": "Screens", "shared": "Shared", "strings": "Localization", "components": "Components"}}
    if out_extra:
        out.update(out_extra)
    d = {"schema": 1}
    if name:
        d["name"] = name
    d["config"] = "config.json"
    d["inputs"] = inputs
    d["output"] = out
    return d


def mkm(fid, title, proves, rules, tiers, mode, verdict, files, job, config, expected, diags, **kw):
    make(fid, title, proves, rules, tiers, mode, verdict, files, config=config, multi=True, expected=expected, diagnostics=diags,
         job=job, compare={"columns": False}, **kw)


def fixd(fixture_dir, specs):
    """specs: list of (file, needle, nth); fills lines and keeps 'related' placeholders"""
    p = ROOT + "/fixtures/" + fixture_dir + "/expected/diagnostics.json"
    dd = _j.load(open(p, encoding="utf-8"))
    for item, spec in zip(dd, specs):
        file, needle, nth = spec[0], spec[1], spec[2]
        item["file"] = file
        item["line"] = find_line(TEXTS[fixture_dir][file], needle, nth)
        if len(spec) > 3 and spec[3]:
            rf, rn, rnth = spec[3]
            item["related"] = [{"file": rf, "line": find_line(TEXTS[fixture_dir][rf], rn, rnth)}]
    open(p, "w", encoding="utf-8", newline="\n").write(dump(dd))


TEXTS = {}


def reg(fixture_dir, files):
    TEXTS[fixture_dir] = dict((k, v) for k, v in files.items() if isinstance(v, str))


def DD(rule, severity, tier, emulation=None):
    d = {"rule": rule, "severity": severity, "tier": tier, "file": "", "line": 0}
    if emulation:
        d["emulation"] = emulation
    return d


SHARED_CSS = '''
.sh { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.sh__box { width: 100px; height: 40px; background-color: #223344; }
'''
# ------------------------------------------------------------ M01
files = {"css/shared.css": SHARED_CSS.strip("\n") + "\n"}
for stem, pre, h in (("home", "hm", 10), ("about", "ab", 20), ("contact", "ct", 30)):
    files["pages/%s.html" % stem] = pg(link("../css/shared.css"), style(".%s__a { height: %dpx; }" % (pre, h)),
                                       '<div name="%s-root" class="sh">\n  <div name="%s-a" class="sh__box %s__a"></div>\n</div>' % (pre, pre, pre))
exp = {}
for stem, pre, h in (("home", "hm", 10), ("about", "ab", 20), ("contact", "ct", 30)):
    N = stem.capitalize()
    exp["Screens/%s.uxml" % N] = uxml(["../Shared/shared.uss", N + ".uss"], '''
    <ui:VisualElement name="%s-root" class="sh">
        <ui:VisualElement name="%s-a" class="sh__box %s__a" />
    </ui:VisualElement>''' % (pre, pre, pre))
    exp["Screens/%s.uss" % N] = ".%s__a {\n    height: %dpx;\n}\n" % (pre, h)
exp["Shared/shared.uss"] = '''.sh {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.sh__box {
    width: 100px;
    height: 40px;
    background-color: #223344;
}
'''
reg("M01-multipage-shared-css", files)
mkm("M01", "multipage-shared-css",
    ["three HTML pages in one job become three UXML files, each with its own USS for page-specific rules",
     "a CSS file linked by several pages becomes one shared USS (Shared/shared.uss) referenced by every UXML before the page USS; rules are never copied into page files",
     "screen names are the PascalCase stems of the input files"],
    [], ["Native"], "convert", "PASS", files, jobdoc(["input/pages/*.html"]), jcfg(["hm", "ab", "ct", "sh"]), exp, [])

# ------------------------------------------------------------ M02
files = {
    "css/a.css": '.ov { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }\n.ov__x { color: #111111; width: 10px; }\n',
    "css/b.css": '.ov__x { color: #333333; }\n',
}
files["pages/p.html"] = pg(link("../css/a.css") + style(".ov__x { color: #222222; }") + link("../css/b.css"), "",
                           '<div name="ov-root" class="ov">\n  <div name="ov-x" class="ov__x"></div>\n</div>')
exp = {"Screens/P.uxml": uxml(["P.uss"], '''
    <ui:VisualElement name="ov-root" class="ov">
        <ui:VisualElement name="ov-x" class="ov__x" />
    </ui:VisualElement>'''),
       "Screens/P.uss": '''.ov {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.ov__x {
    width: 10px;
    color: #333333;
}
'''}
reg("M02-css-link-order-override", files)
mkm("M02", "css-link-order-override",
    ["the cascade of one page is the document order of its link and style elements: a.css, the style block, b.css; the last definition wins",
     "page-specific sheets and style blocks merge into one page USS in cascade order and adjacent rules with the same selector merge, later declarations overriding earlier ones",
     "every override of a different value for the same selector is reported as J001 with the overridden definition in related"],
    ["J001"], ["Native"], "convert", "PASS WITH WARNINGS", files, jobdoc(["input/pages/p.html"]), jcfg(["ov"]), exp,
    [DD("J001", "Warning", "Native"), DD("J001", "Warning", "Native")])
fixd("M02-css-link-order-override", [
    ("css/b.css", ".ov__x { color: #333333", 1, ("pages/p.html", ".ov__x { color: #222222; }", 1)),
    ("pages/p.html", ".ov__x { color: #222222; }", 1, ("css/a.css", ".ov__x { color: #111111", 1))])

# ------------------------------------------------------------ M03
files = {"css/shared.css": '.cf { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }\n.cf__t { font-size: 24px; }\n'}
files["pages/a.html"] = pg(link("../css/shared.css"), style(".cf__t { font-size: 32px; }"),
                           '<div name="a-root" class="cf">\n  <div name="a-t" class="cf__t"></div>\n</div>')
files["pages/b.html"] = pg(link("../css/shared.css"), "",
                           '<div name="b-root" class="cf">\n  <div name="b-t" class="cf__t"></div>\n</div>')
exp = {"Screens/A.uxml": uxml(["../Shared/shared.uss", "A.uss"], '''
    <ui:VisualElement name="a-root" class="cf">
        <ui:VisualElement name="a-t" class="cf__t" />
    </ui:VisualElement>'''),
       "Screens/A.uss": ".cf__t {\n    font-size: 32px;\n}\n",
       "Screens/B.uxml": uxml(["../Shared/shared.uss"], '''
    <ui:VisualElement name="b-root" class="cf">
        <ui:VisualElement name="b-t" class="cf__t" />
    </ui:VisualElement>'''),
       "Shared/shared.uss": '''.cf {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.cf__t {
    font-size: 24px;
}
'''}
reg("M03-css-conflict-diagnostic", files)
mkm("M03", "css-conflict-diagnostic",
    ["a page rule that redefines a shared rule with a different value is J001, reported at the page definition with the shared definition in related",
     "a page without its own rules gets no page USS at all (only the shared reference)",
     "the conflict is a warning: both USS files are written and the cascade decides"],
    ["J001"], ["Native"], "convert", "PASS WITH WARNINGS", files, jobdoc(["input/pages/*.html"]), jcfg(["a", "b", "cf"]), exp,
    [DD("J001", "Warning", "Native")])
fixd("M03-css-conflict-diagnostic", [("pages/a.html", ".cf__t { font-size: 32px; }", 1, ("css/shared.css", ".cf__t { font-size: 24px; }", 1))])

# ------------------------------------------------------------ M04
files = {
    "ui/Skin/skin.uss": ':root {\n    --sk-ink: #101010;\n}\n\n.sk {\n    color: #ffffff;\n}\n',
    "pages/p.html": pg("", style(".pg { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }\n.pg__a { color: var(--sk-ink); height: 20px; }"),
                       '<div name="pg-root" class="pg">\n  <div name="pg-a" class="pg__a sk"></div>\n</div>'),
}
exp = {"Screens/P.uxml": uxml(["../Skin/skin.uss", "P.uss"], '''
    <ui:VisualElement name="pg-root" class="pg">
        <ui:VisualElement name="pg-a" class="pg__a sk" />
    </ui:VisualElement>'''),
       "Screens/P.uss": '''.pg {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.pg__a {
    height: 20px;
    color: var(--sk-ink);
}
'''}
reg("M04-uss-passthrough-merge", files)
mkm("M04", "uss-passthrough-merge",
    ["an existing USS listed in inputs.uss is not converted; it is referenced from the UXML in cascade position (before the page sheets with the default cssOrder before)",
     "its classes and variables feed the class check and the undefined var() check",
     "the reference is the path from <uiRoot>/Screens to the file, so pass-through files must live under uiRoot"],
    [], ["Native"], "convert", "PASS", files, jobdoc(["input/pages/p.html"], uss=["input/ui/Skin/skin.uss"]), jcfg(["pg", "sk"]), exp, [])

# ------------------------------------------------------------ M05
files = {
    "css/main.css": '@import "base.css";\n@import "theme.css";\n.im__x { width: 10px; }\n',
    "css/base.css": '@import "reset.css";\n.im__base { color: #aaaaaa; }\n',
    "css/theme.css": '.im__theme { color: #bbbbbb; }\n',
    "css/reset.css": '.im__reset { margin-left: 0; }\n',
    "css/cyc-a.css": '@import "cyc-b.css";\n.cy__a { width: 1px; }\n',
    "css/cyc-b.css": '@import "cyc-a.css";\n.cy__b { width: 2px; }\n',
    "pages/ok.html": pg(link("../css/main.css"), "",
                        '<div name="im-root">\n  <div name="im-a" class="im__reset im__base im__theme im__x"></div>\n</div>'),
    "pages/cycle.html": pg(link("../css/cyc-a.css"), "",
                           '<div name="cy-root">\n  <div name="cy-a" class="cy__a cy__b"></div>\n</div>'),
}
exp = {"Screens/Ok.uxml": uxml(["Ok.uss"], '''
    <ui:VisualElement name="im-root">
        <ui:VisualElement name="im-a" class="im__reset im__base im__theme im__x" />
    </ui:VisualElement>'''),
       "Screens/Ok.uss": '''.im__reset {
    margin-left: 0;
}

.im__x {
    width: 10px;
}

.im__base {
    color: #aaaaaa;
}

.im__theme {
    color: #bbbbbb;
}
'''}
reg("M05-import-chain-and-cycle", files)
mkm("M05", "import-chain-and-cycle",
    ["@import expands in place, depth first, in source order: main.css pulls reset, base, theme and then its own rules",
     "a circular @import is J003 at the import that closes the cycle, with the first import in related; the affected page is not written, other pages still are",
     "the job verdict is FAIL while the healthy page is written"],
    ["J003"], ["Native", "Reject"], "convert", "FAIL", files, jobdoc(["input/pages/*.html"]), jcfg(["im", "cy"]), exp,
    [DD("J003", "Error", "Reject")])
fixd("M05-import-chain-and-cycle", [("css/cyc-b.css", '@import "cyc-a.css"', 1, ("css/cyc-a.css", '@import "cyc-b.css"', 1))])

# ------------------------------------------------------------ M06
files = {
    "css/ok.css": '@import "gone.css";\n.ms__a { width: 1px; }\n',
    "pages/p.html": pg(link("../css/missing.css") + link("../css/ok.css") + '<link rel="stylesheet" href="skin.css" data-uss="../Skin/nope.uss">\n', "",
                       '<div name="ms-root">\n  <div name="ms-a" class="ms__a"></div>\n</div>'),
}
reg("M06-missing-stylesheet-import", files)
mkm("M06", "missing-stylesheet-import",
    ["a linked stylesheet that does not exist, an @import that does not exist and a data-uss skin that does not exist are each J002",
     "nothing is written for a page with an Error; the diagnostics point at the link or import line"],
    ["J002"], ["Reject"], "validate", "FAIL", files, jobdoc(["input/pages/p.html"]), jcfg(["ms"]), {},
    [DD("J002", "Error", "Reject"), DD("J002", "Error", "Reject"), DD("J002", "Error", "Reject")])
fixd("M06-missing-stylesheet-import", [("css/ok.css", "gone.css", 1), ("pages/p.html", "missing.css", 1), ("pages/p.html", "nope.uss", 1)])

# ------------------------------------------------------------ M07 / M08 partials
PART = ('<template id="card">\n  <div name="card-root" class="pt__card">\n    <span name="card-title" class="pt__t" data-loc="pt.title">Title</span>\n  </div>\n</template>\n')
PART_FILE = '<!DOCTYPE html>\n<html><head><meta charset="utf-8"></head>\n<body>\n' + PART + '</body></html>\n'
HOME = pg("", style(".pt { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }\n.pt__slot { height: 40px; }\n.pt__card { height: 40px; background-color: #223344; }\n.pt__t { font-size: 16px; }"),
          '<div name="home-root" class="pt">\n  <div name="home-card-1" class="pt__slot" data-include="../partials/card.html#card"></div>\n  <div name="home-card-2" class="pt__slot" data-include="../partials/card.html#card"></div>\n</div>')
files = {"partials/card.html": PART_FILE, "pages/home.html": HOME,
         "Localization/Home.strings.json": strings("pt", {"pt.title": "Title"})}
cfgp = jcfg(["pt", "home", "card"], strings_files=["input/Localization/Home.strings.json"])
home_uss = '''.pt {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.pt__slot {
    height: 40px;
}

.pt__card {
    height: 40px;
    background-color: #223344;
}

.pt__t {
    font-size: 16px;
}
'''
exp = {"Screens/Home.uxml": UXML_HEAD + '''    <Style src="Home.uss" />
    <ui:Template name="Card" src="../Components/Card.uxml" />
    <ui:VisualElement name="home-root" class="pt">
        <ui:Instance template="Card" name="home-card-1" class="pt__slot" />
        <ui:Instance template="Card" name="home-card-2" class="pt__slot" />
    </ui:VisualElement>
''' + UXML_TAIL,
       "Screens/Home.uss": home_uss,
       "Components/Card.uxml": uxml([], '''
    <ui:VisualElement name="card-root" class="pt__card">
        <ui:Label name="card-title" text="pt.title" class="pt__t" />
    </ui:VisualElement>''')}
reg("M07-partials-templates", files)
mkm("M07", "partials-templates",
    ["<template id> in a partial file and data-include=\"file#id\" on an empty element resolve at conversion",
     "templates mode writes Components/<PascalId>.uxml once and the use sites become ui:Instance with the element's name and class; ui:Template follows the Style lines",
     "names inside a template live in the template scope, so two instances never collide"],
    [], ["Native"], "convert", "PASS", files, jobdoc(["input/pages/home.html"], partials=["input/partials/*.html"], out_extra={"partials": "templates"}), cfgp, exp, [])

exp = {"Screens/Home.uxml": uxml(["Home.uss"], '''
    <ui:VisualElement name="home-root" class="pt">
        <ui:VisualElement name="home-card-1" class="pt__slot">
            <ui:VisualElement name="card-root" class="pt__card">
                <ui:Label name="card-title" text="pt.title" class="pt__t" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="home-card-2" class="pt__slot">
            <ui:VisualElement name="card-root-2" class="pt__card">
                <ui:Label name="card-title-2" text="pt.title" class="pt__t" />
            </ui:VisualElement>
        </ui:VisualElement>
    </ui:VisualElement>'''),
       "Screens/Home.uss": home_uss}
reg("M08-partials-inline", files)
mkm("M08", "partials-inline",
    ["inline mode copies the partial into every use site under a wrapper VisualElement that keeps the include element's name and class",
     "names inside the second and later copies get the suffix -2, -3 and the rename is reported as J011 (info)",
     "no Components file is written"],
    ["J011"], ["Native"], "convert", "PASS", files, jobdoc(["input/pages/home.html"], partials=["input/partials/*.html"], out_extra={"partials": "inline"}), cfgp, exp,
    [DD("J011", "Info", "Native")])
fixd("M08-partials-inline", [("pages/home.html", 'name="home-card-2"', 1)])

# ------------------------------------------------------------ M09
files = {
    "partials/a.html": '<template id="a">\n  <div name="a-root" class="cc__a">\n    <div name="a-inc" data-include="b.html#b"></div>\n  </div>\n</template>\n',
    "partials/b.html": '<template id="b">\n  <div name="b-root" class="cc__b">\n    <div name="b-inc" data-include="a.html#a"></div>\n  </div>\n</template>\n',
    "pages/p.html": pg("", style(".cc__a { height: 1px; }\n.cc__b { height: 2px; }"),
                       '<div name="cc-root">\n  <div name="cc-use" data-include="../partials/a.html#a"></div>\n</div>'),
}
reg("M09-partials-include-cycle", files)
mkm("M09", "partials-include-cycle",
    ["a includes b and b includes a: J007 at the include that closes the cycle, the first include in related; the page is not converted"],
    ["J007"], ["Reject"], "validate", "FAIL", files, jobdoc(["input/pages/p.html"], partials=["input/partials/*.html"]), jcfg(["cc", "a", "b"]), {},
    [DD("J007", "Error", "Reject")])
fixd("M09-partials-include-cycle", [("partials/b.html", 'data-include="a.html#a"', 1, ("partials/a.html", 'data-include="b.html#b"', 1))])

# ------------------------------------------------------------ M10
files = {
    "pages/home.html": pg("", style(".hm { flex-grow: 1; }\n.hm__b { height: 44px; min-width: 44px; }"),
                          '<div name="home-root" class="hm" data-start>\n  <button name="home-next" class="hm__b" data-nav="settings">&#8594;</button>\n</div>'),
    "pages/settings.html": pg("", style(".st { flex-grow: 1; }\n.st__b { height: 44px; min-width: 44px; }"),
                              '<div name="settings-root" class="st">\n  <button name="settings-back" class="st__b" data-back>&#8592;</button>\n</div>'),
}
exp = {"Screens/App.uxml": uxml(["App.uss"], '''
    <ui:VisualElement name="app-root">
        <ui:VisualElement name="home-root" class="hm">
            <ui:Button name="home-next" text="%s" class="hm__b" />
        </ui:VisualElement>
        <ui:VisualElement name="settings-root" class="st is-hidden">
            <ui:Button name="settings-back" text="%s" class="st__b" />
        </ui:VisualElement>
    </ui:VisualElement>''' % (chr(0x2192), chr(0x2190))),
       "Screens/App.uss": '''.hm {
    flex-grow: 1;
}

.hm__b {
    min-width: 44px;
    height: 44px;
}

.st {
    flex-grow: 1;
}

.st__b {
    min-width: 44px;
    height: 44px;
}

.is-hidden {
    display: none;
}
''',
       "Screens/App.nav.json": dump({"schema": 1, "document": "App", "start": "home",
                                     "pages": [{"id": "home", "element": "home-root", "focus": "home-next"}, {"id": "settings", "element": "settings-root", "focus": "settings-back"}],
                                     "edges": [{"from": "home", "trigger": "home-next", "action": "navigate", "to": "settings", "mode": "push"},
                                               {"from": "settings", "trigger": "settings-back", "action": "back"}]}),
       "Screens/App.runtime.json": dump({"schema": 1, "components": [{"id": "UitkRouter", "uses": "App.nav.json"}]})}
reg("M10-single-document-assembly", files)
mkm("M10", "single-document-assembly",
    ["assemble singleDocument merges all pages into one UXML named after the job (App.uxml) under a generated <job>-root container; each input file is a page with the file stem as id",
     "the start page is the one with data-start (else the first in path order); other pages get the hidden class and the page rules merge into App.uss in page order",
     "the nav map and the runtime manifest are written next to it; a data-nav without data-nav-mode is a push edge"],
    ["P011"], ["Runtime"], "convert", "PASS", files, jobdoc(["input/pages/*.html"], name="app", assemble="singleDocument"), jcfg(["hm", "st", "home", "settings", "is"]), exp,
    [DD("P011", "Info", "Runtime", "page-router")])
fixd("M10-single-document-assembly", [("pages/home.html", "data-start", 1)])

# ------------------------------------------------------------ M11
files = {
    "pages/a.html": pg("", style(".da { flex-grow: 1; }"), '<div name="a-root" class="da">\n  <span name="title" class="da">&#8226;</span>\n</div>'),
    "pages/b.html": pg("", style(".db { flex-grow: 1; }"), '<div name="b-root" class="db">\n  <span name="title" class="db">&#8226;</span>\n</div>'),
}
reg("M11-duplicate-name-across-pages", files)
mkm("M11", "duplicate-name-across-pages",
    ["the same element name in two pages is fine when each page is its own UXML, but J004 when the pages are assembled into one document",
     "the diagnostic is reported at the second definition with the first in related"],
    ["J004"], ["Reject"], "validate", "FAIL", files, jobdoc(["input/pages/*.html"], name="app", assemble="singleDocument"), jcfg(["a", "b", "da", "db", "title"]), {},
    [DD("J004", "Error", "Reject")])
fixd("M11-duplicate-name-across-pages", [("pages/b.html", 'name="title"', 1, ("pages/a.html", 'name="title"', 1))])

# ------------------------------------------------------------ M12
files = {"css/shared.css": '.us__used { height: 10px; }\n.us__a { height: 20px; }\n.us__never { height: 30px; }\n'}
files["pages/a.html"] = pg(link("../css/shared.css"), "", '<div name="a-root" class="us__used">\n  <div name="a-x" class="us__a"></div>\n</div>')
files["pages/b.html"] = pg(link("../css/shared.css"), "", '<div name="b-root" class="us__used"></div>')
exp = {"Screens/A.uxml": uxml(["../Shared/shared.uss"], '''
    <ui:VisualElement name="a-root" class="us__used">
        <ui:VisualElement name="a-x" class="us__a" />
    </ui:VisualElement>'''),
       "Screens/B.uxml": uxml(["../Shared/shared.uss"], '''
    <ui:VisualElement name="b-root" class="us__used" />'''),
       "Shared/shared.uss": '''.us__used {
    height: 10px;
}

.us__a {
    height: 20px;
}

.us__never {
    height: 30px;
}
'''}
reg("M12-unused-css-rules", files)
mkm("M12", "unused-css-rules",
    ["a shared rule that no page matches is J005 (warning) at its CSS location; a rule used by at least one page is not reported",
     "unused rules stay in the shared USS unless uss.pruneDeadRules is set"],
    ["J005"], ["Native"], "convert", "PASS WITH WARNINGS", files, jobdoc(["input/pages/*.html"]), jcfg(["a", "b", "us"]), exp,
    [DD("J005", "Warning", "Native")])
fixd("M12-unused-css-rules", [("css/shared.css", ".us__never", 1)])

# ------------------------------------------------------------ M13
files = {
    "pages/a.html": pg("", style(".aa { height: 1px; }"), '<div name="aa-root" class="aa"></div>'),
    "pages/b.html": pg("", style(".bb { height: 2px; }"), '<div name="bb-root" class="bb"></div>'),
    "pages/c.html": pg("", style(".cc { height: 3px; }"), '<div name="cc-root" class="cc"></div>', meta='<meta name="uitk:screen" content="B">\n'),
}
exp = {"Screens/Main/Alpha.uxml": uxml(["Alpha.uss"], '    <ui:VisualElement name="aa-root" class="aa" />'),
       "Screens/Main/Alpha.uss": ".aa {\n    height: 1px;\n}\n",
       "Screens/B.uxml": uxml(["B.uss"], '    <ui:VisualElement name="bb-root" class="bb" />'),
       "Screens/B.uss": ".bb {\n    height: 2px;\n}\n",
       "Screens/B-2.uxml": uxml(["B-2.uss"], '    <ui:VisualElement name="cc-root" class="cc" />'),
       "Screens/B-2.uss": ".cc {\n    height: 3px;\n}\n"}
reg("M13-per-file-output-mapping", files)
job = jobdoc(["input/pages/*.html"])
job["output"]["perFile"] = {"input/pages/a.html": {"name": "Alpha", "folder": "Screens/Main"}}
mkm("M13", "per-file-output-mapping",
    ["output.perFile overrides the screen name and the output folder of one input (Alpha in Screens/Main), and the UXML refers to its own USS in the same folder",
     "two inputs that derive the same screen name (stem b and meta uitk:screen B) get a deterministic suffix in path order (B, B-2) and the later one reports J012 (info)"],
    ["J012"], ["Native"], "convert", "PASS", files, job, jcfg(["aa", "bb", "cc"]), exp, [DD("J012", "Info", "Native")])
fixd("M13-per-file-output-mapping", [("pages/c.html", 'uitk:screen', 1)])

# ------------------------------------------------------------ M14
files = {
    "css/tokens.css": ':root {\n    --tk-ink: #0b1014;\n    --tk-gold: #e4c07a;\n}\n',
    "css/base.css": '.tk { position: absolute; left: 0; top: 0; right: 0; bottom: 0; background-color: var(--tk-ink); }\n.tk__a { color: var(--tk-gold); }\n',
}
files["pages/a.html"] = pg(link("../css/tokens.css") + link("../css/base.css"), style(":root { --a-pad: 4px; }\n.a__p { padding-top: var(--a-pad); }"),
                           '<div name="a-root" class="tk">\n  <div name="a-p" class="tk__a a__p"></div>\n</div>')
files["pages/b.html"] = pg(link("../css/tokens.css") + link("../css/base.css"), "", '<div name="b-root" class="tk">\n  <div name="b-p" class="tk__a"></div>\n</div>')
exp = {"Shared/Tokens.uss": ':root {\n    --tk-ink: #0b1014;\n    --tk-gold: #e4c07a;\n}\n',
       "Shared/base.uss": '''.tk {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
    background-color: var(--tk-ink);
}

.tk__a {
    color: var(--tk-gold);
}
''',
       "Screens/A.uxml": uxml(["../Shared/Tokens.uss", "../Shared/base.uss", "A.uss"], '''
    <ui:VisualElement name="a-root" class="tk">
        <ui:VisualElement name="a-p" class="tk__a a__p" />
    </ui:VisualElement>'''),
       "Screens/A.uss": ':root {\n    --a-pad: 4px;\n}\n\n.a__p {\n    padding-top: var(--a-pad);\n}\n',
       "Screens/B.uxml": uxml(["../Shared/Tokens.uss", "../Shared/base.uss"], '''
    <ui:VisualElement name="b-root" class="tk">
        <ui:VisualElement name="b-p" class="tk__a" />
    </ui:VisualElement>''')}
reg("M14-shared-tokens-multi", files)
mkm("M14", "shared-tokens-multi",
    ["all :root variables of shared sheets collect in one Shared/Tokens.uss referenced first; a shared sheet that only holds :root gets no USS of its own",
     "page-local :root variables stay in the page USS (variables section first)"],
    [], ["Native"], "convert", "PASS", files, jobdoc(["input/pages/*.html"]), jcfg(["a", "b", "tk"]), exp, [])

# ------------------------------------------------------------ M15
files = {
    "pages/a.html": pg("", style(".fa { height: 1px; }"), '<div name="fa-root" class="fa"></div>'),
    "pages/sub/b.html": pg("", style(".fb { height: 2px; }"), '<div name="fb-root" class="fb"></div>').replace("../../../../assets", "../../../../../assets"),
    "extra/c.html": pg("", style(".fc { height: 3px; }"), '<div name="fc-root" class="fc"></div>').replace("../../../../assets", "../../../../assets"),
}
exp = {}
for N, pre, h in (("A", "fa", 1), ("B", "fb", 2), ("C", "fc", 3)):
    exp["Screens/%s.uxml" % N] = uxml([N + ".uss"], '    <ui:VisualElement name="%s-root" class="%s" />' % (pre, pre))
    exp["Screens/%s.uss" % N] = ".%s {\n    height: %dpx;\n}\n" % (pre, h)
reg("M15-job-file-folders-globs", files)
job = jobdoc(["input/pages", "input/extra/*.html", "input/nothing/*.html"])
mkm("M15", "job-file-folders-globs",
    ["inputs.html accepts a folder (recursive), a glob and a file; the job file drives the run exactly as the CLI and the Unity window do",
     "a glob that matches nothing is J010 (warning) reported at the job file with line 0; the other inputs are converted"],
    ["J010"], ["Native"], "convert", "PASS WITH WARNINGS", files, job, jcfg(["fa", "fb", "fc"]), exp, [DD("J010", "Warning", "Native")])
p = ROOT + "/fixtures/M15-job-file-folders-globs/expected/diagnostics.json"
dd = _j.load(open(p, encoding="utf-8"))
dd[0]["file"] = "job.json"
dd[0]["line"] = 0
open(p, "w", encoding="utf-8", newline="\n").write(dump(dd))

# ------------------------------------------------------------ M16
files = {
    "pages/a.html": pg("", style(".ia { background-color: #112233; }"), '<div name="ia-root" class="ia"></div>'),
    "pages/b.html": pg("", style(".ib { background-color: #223344; }"), '<div name="ib-root" class="ib"></div>'),
}
exp = {}
for N, pre, c in (("A", "ia", "#112233"), ("B", "ib", "#223344")):
    exp["Screens/%s.uxml" % N] = uxml([N + ".uss"], '    <ui:VisualElement name="%s-root" class="%s" />' % (pre, pre))
    exp["Screens/%s.uss" % N] = ".%s {\n    background-color: %s;\n}\n" % (pre, c)
reg("M16-incremental-regeneration-stable-bytes", files)
mkm("M16", "incremental-regeneration-stable-bytes",
    ["running the same job again produces identical bytes and rewrites no file (the commit stage skips unchanged content)",
     "after editing one input only the outputs of that page are rewritten (expected-final holds the changed files)"],
    [], ["Native"], "convert", "PASS", files, jobdoc(["input/pages/*.html"]), jcfg(["ia", "ib"]), exp, [],
    extra={"runs": 3, "edits": [{"run": 3, "file": "input/pages/b.html", "find": "#223344", "replace": "#334455"}],
           "expectRewrittenOnRun": {"2": [], "3": ["Screens/B.uss"]}})
write_file(ROOT + "/fixtures/M16-incremental-regeneration-stable-bytes/expected-final/Screens/B.uss", ".ib {\n    background-color: #334455;\n}\n")
