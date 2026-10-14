from g2 import *
import json as _j


def put(fixture_dir, rel, content):
    write_file(ROOT + "/fixtures/" + fixture_dir + "/expected/" + rel, content)
    p = ROOT + "/fixtures/" + fixture_dir + "/fixture.json"
    meta = _j.load(open(p, encoding="utf-8"))
    meta["expectedStatus"]["expected/" + rel] = "authored"
    meta["expectedStatus"] = dict(sorted(meta["expectedStatus"].items()))
    write_file(p, dump(meta))


ROOT_BOX = ".%s { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }\n"
ROOT_USS = '''.%s {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}
'''

# ---------------------------------------------------------------- S28 calc-constants
css = ROOT_BOX % "cl" + '''
.cl__a {
    width: calc(100px + 20px);
    height: calc(2 * 8px);
    margin-left: calc(10px - 4px);
    padding-top: calc(50% + 10px);
}
'''
body = '''
<div name="cl-root" class="cl">
  <div name="cl-a" class="cl__a"></div>
</div>
'''
exp_uxml = uxml(["Calc.uss"], '''
    <ui:VisualElement name="cl-root" class="cl">
        <ui:VisualElement name="cl-a" class="cl__a" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "cl" + '''
.cl__a {
    width: 120px;
    height: 16px;
    margin-left: 6px;
}
'''
html = mk("S28", "calc-constants",
          ["calc() of constants and of same-unit terms folds to a literal (100px + 20px, 2 * 8px, 10px - 4px)",
           "mixed percent and pixel terms cannot be folded: C006 Reject with a flex rewrite; the declaration is dropped under --force"],
          ["C006"], ["ConvertTime", "Reject"], "convert", "FAIL", css, body, ["cl"],
          {"Screens/Calc.uxml": exp_uxml, "Screens/Calc.uss": exp_uss},
          [D("C006", "Error", "Reject", "page.html", 0), D("C006", "Info", "ConvertTime", "page.html", 0, "calc-fold"),
           D("C006", "Info", "ConvertTime", "page.html", 0, "calc-fold"), D("C006", "Info", "ConvertTime", "page.html", 0, "calc-fold")],
          "Calc", force=True)
fix_lines("S28-calc-constants", html, [("padding-top: calc", 1), ("width: calc", 1), ("height: calc", 1), ("margin-left: calc", 1)])

# ---------------------------------------------------------------- S29 viewport-and-relative-units
css = ROOT_BOX % "un" + '''
.un__a {
    font-size: 20px;
    padding-top: 0.5em;
    margin-left: 2rem;
    width: 10vw;
    height: 50vh;
    min-height: 10vmin;
    padding-left: calc(2rem + 8px);
}
'''
body = '''
<div name="un-root" class="un">
  <div name="un-a" class="un__a"></div>
</div>
'''
exp_uxml = uxml(["Units.uss"], '''
    <ui:VisualElement name="un-root" class="un">
        <ui:VisualElement name="un-a" class="un__a" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "un" + '''
.un__a {
    width: 192px;
    height: 540px;
    min-height: 108px;
    margin-left: 32px;
    padding-top: 10px;
    padding-left: 40px;
    font-size: 20px;
}
'''
html = mk("S29", "viewport-and-relative-units",
          ["vw, vh, vmin and vmax resolve to px against unity.referenceResolution (1920x1080 here): 10vw is 192px, 50vh is 540px, 10vmin is 108px",
           "em resolves against the element's own static font-size (0.5em of 20px is 10px), rem against emulation.rootFontSize (2rem is 32px)",
           "calc() runs after unit resolution, so calc(2rem + 8px) folds to 40px"],
          ["C006", "C008"], ["ConvertTime"], "convert", "PASS", css, body, ["un"],
          {"Screens/Units.uxml": exp_uxml, "Screens/Units.uss": exp_uss},
          [D("C008", "Info", "ConvertTime", "page.html", 0, "relative-units"), D("C008", "Info", "ConvertTime", "page.html", 0, "relative-units"),
           D("C008", "Info", "ConvertTime", "page.html", 0, "viewport-units"), D("C008", "Info", "ConvertTime", "page.html", 0, "viewport-units"),
           D("C008", "Info", "ConvertTime", "page.html", 0, "viewport-units"), D("C006", "Info", "ConvertTime", "page.html", 0, "calc-fold")],
          "Units", config_extra={"unity": {"referenceResolution": [1920, 1080]}, "emulation": {"rootFontSize": 16}})
fix_lines("S29-viewport-and-relative-units", html, [("padding-top: 0.5em", 1), ("margin-left: 2rem", 1), ("width: 10vw", 1), ("height: 50vh", 1), ("min-height: 10vmin", 1), ("padding-left: calc", 1)])

# ---------------------------------------------------------------- S30 media-breakpoints
css = ROOT_BOX % "rs" + '''
.rs__card { width: 400px; }
.rs__row { flex-direction: row; }
@media (max-width: 900px) { .rs__card { width: 100%; } }
@media (orientation: portrait) { .rs__row { flex-direction: column; } }
@media print { .rs__card { width: 10px; } }
'''
body = '''
<div name="rs-root" class="rs">
  <div name="rs-row" class="rs__row">
    <div name="rs-card" class="rs__card"></div>
  </div>
</div>
'''
exp_uxml = uxml(["Responsive.uss"], '''
    <ui:VisualElement name="rs-root" class="rs">
        <ui:VisualElement name="rs-row" class="rs__row">
            <ui:VisualElement name="rs-card" class="rs__card" />
        </ui:VisualElement>
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "rs" + '''
.rs__card {
    width: 400px;
}

.rs__row {
    flex-direction: row;
}

.mq-w-max-900 .rs__card {
    width: 100%;
}

.mq-portrait .rs__row {
    flex-direction: column;
}
'''
exp_mq = dump({"schema": 1, "root": "rs-root", "classes": [{"class": "mq-w-max-900", "query": {"maxWidth": 900}}, {"class": "mq-portrait", "query": {"orientation": "portrait"}}]})
exp_rt = dump({"schema": 1, "components": [{"id": "BreakpointWatcher", "uses": "Responsive.mq.json"}]})
html = mk("S30", "media-breakpoints",
          ["@media on width, height and orientation becomes a root class (mq-w-max-900, mq-portrait) plus a rule prefixed by that class, placed after all other rules",
           "Page.mq.json lists the classes and their queries for the BreakpointWatcher; Page.runtime.json lists the required component",
           "a media type such as print cannot be evaluated at runtime: C007 Reject"],
          ["C007"], ["Runtime", "Reject"], "convert", "FAIL", css, body, ["rs", "mq"],
          {"Screens/Responsive.uxml": exp_uxml, "Screens/Responsive.uss": exp_uss, "Screens/Responsive.mq.json": exp_mq,
           "Screens/Responsive.runtime.json": exp_rt},
          [D("C007", "Error", "Reject", "page.html", 0), D("C007", "Warning", "Runtime", "page.html", 0, "media-breakpoints"),
           D("C007", "Warning", "Runtime", "page.html", 0, "media-breakpoints")], "Responsive", force=True)
fix_lines("S30-media-breakpoints", html, [("@media print", 1), ("@media (max-width", 1), ("@media (orientation", 1)])

# ---------------------------------------------------------------- S31 overflow-scroll
css = ROOT_BOX % "sc" + '''
.sc__list { overflow: auto; height: 120px; }
.sc__wide { overflow-x: auto; width: 200px; }
.sc__both { overflow-x: auto; overflow-y: auto; height: 100px; width: 100px; }
.sc__t { font-size: 16px; }
'''
body = '''
<div name="sc-root" class="sc">
  <div name="sc-list" class="sc__list"><span name="sc-a" class="sc__t" data-loc="sc.a">A</span></div>
  <div name="sc-wide" class="sc__wide"><span name="sc-b" class="sc__t" data-loc="sc.b">B</span></div>
  <div name="sc-both" class="sc__both"><span name="sc-c" class="sc__t" data-loc="sc.c">C</span></div>
</div>
'''
exp_uxml = uxml(["Scroll.uss"], '''
    <ui:VisualElement name="sc-root" class="sc">
        <ui:ScrollView name="sc-list" class="sc__list">
            <ui:Label name="sc-a" text="sc.a" class="sc__t" />
        </ui:ScrollView>
        <ui:ScrollView name="sc-wide" class="sc__wide" mode="Horizontal">
            <ui:Label name="sc-b" text="sc.b" class="sc__t" />
        </ui:ScrollView>
        <ui:ScrollView name="sc-both" class="sc__both" mode="VerticalAndHorizontal">
            <ui:Label name="sc-c" text="sc.c" class="sc__t" />
        </ui:ScrollView>
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "sc" + '''
.sc__list {
    height: 120px;
}

.sc__wide {
    width: 200px;
}

.sc__both {
    width: 100px;
    height: 100px;
}

.sc__t {
    font-size: 16px;
}
'''
html = mk("S31", "overflow-scroll",
          ["overflow: auto or scroll (and overflow-y) turns the element into a ScrollView (Vertical is the default mode and is omitted)",
           "overflow-x alone gives mode=Horizontal, both axes give VerticalAndHorizontal; the overflow declarations disappear from the USS",
           "children move into the ScrollView unchanged (UXML places them in the content container)"],
          ["C020"], ["ConvertTime"], "convert", "PASS", css, body, ["sc"],
          {"Screens/Scroll.uxml": exp_uxml, "Screens/Scroll.uss": exp_uss},
          [D("C020", "Info", "ConvertTime", "page.html", 0, "overflow-scrollview")] * 3, "Scroll",
          strings_={"sc.a": "A", "sc.b": "B", "sc.c": "C"})
fix_lines("S31-overflow-scroll", html, [("overflow: auto", 1), ("overflow-x: auto; width", 1), ("overflow-x: auto; overflow-y", 1)])

# ---------------------------------------------------------------- S32 fixed-sticky-zindex
css = ROOT_BOX % "fx" + '''
.fx__a { z-index: 2; height: 20px; }
.fx__b { z-index: 1; height: 20px; }
.fx__c { height: 20px; }
.fx__toast { position: fixed; right: 16px; bottom: 16px; width: 120px; height: 40px; }
.fx__bar { position: sticky; top: 0; height: 30px; }
.fx__tail { height: 10px; }
'''
body = '''
<div name="fx-root" class="fx">
  <div name="fx-stack">
    <div name="fx-a" class="fx__a"></div>
    <div name="fx-b" class="fx__b"></div>
    <div name="fx-c" class="fx__c"></div>
  </div>
  <div name="fx-toast" class="fx__toast"></div>
  <div name="fx-bar" class="fx__bar"></div>
  <div name="fx-tail" class="fx__tail"></div>
</div>
'''
exp_uxml = uxml(["Layers.uss"], '''
    <ui:VisualElement name="fx-root" class="fx">
        <ui:VisualElement name="fx-stack">
            <ui:VisualElement name="fx-c" class="fx__c" />
            <ui:VisualElement name="fx-b" class="fx__b" />
            <ui:VisualElement name="fx-a" class="fx__a" />
        </ui:VisualElement>
        <ui:VisualElement name="fx-bar" class="fx__bar" />
        <ui:VisualElement name="fx-tail" class="fx__tail" />
        <ui:VisualElement name="fx-toast" class="fx__toast" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "fx" + '''
.fx__a {
    height: 20px;
}

.fx__b {
    height: 20px;
}

.fx__c {
    height: 20px;
}

.fx__toast {
    position: absolute;
    right: 16px;
    bottom: 16px;
    width: 120px;
    height: 40px;
}

.fx__bar {
    top: 0;
    height: 30px;
}

.fx__tail {
    height: 10px;
}
'''
html = mk("S32", "fixed-sticky-zindex",
          ["position: fixed moves the element to the end of the first root element (drawn last) and becomes absolute",
           "z-index reorders siblings: elements without z-index keep their order and come first, then z-index ascending (stable); the z-index declaration is dropped",
           "position: sticky has no equivalent: C009 Reject, only the position declaration is dropped under --force"],
          ["C009", "C021"], ["ConvertTime", "Reject"], "convert", "FAIL", css, body, ["fx"],
          {"Screens/Layers.uxml": exp_uxml, "Screens/Layers.uss": exp_uss},
          [D("C009", "Error", "Reject", "page.html", 0), D("C021", "Info", "ConvertTime", "page.html", 0, "z-order"),
           D("C021", "Info", "ConvertTime", "page.html", 0, "z-order"), D("C009", "Info", "ConvertTime", "page.html", 0, "fixed-to-root")],
          "Layers", force=True)
fix_lines("S32-fixed-sticky-zindex", html, [("position: sticky", 1), ("z-index: 2", 1), ("z-index: 1", 1), ("position: fixed", 1)])

# ---------------------------------------------------------------- S33 linear-gradient-native
css = ROOT_BOX % "gd" + '''
.gd__a { height: 40px; background-image: linear-gradient(180deg, #ff0000, #0000ff); }
.gd__b { height: 40px; background-image: linear-gradient(to right, #ff0000 0%, #00ff00 50%, #0000ff 100%); }
'''
body = '''
<div name="gd-root" class="gd">
  <div name="gd-a" class="gd__a"></div>
  <div name="gd-b" class="gd__b"></div>
</div>
'''
exp_uxml = uxml(["Gradients.uss"], '''
    <ui:VisualElement name="gd-root" class="gd">
        <ui:VisualElement name="gd-a" class="gd__a" />
        <ui:VisualElement name="gd-b" class="gd__b" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "gd" + '''
.gd__a {
    height: 40px;
    background-image: linear-gradient(180deg, #ff0000, #0000ff);
}

.gd__b {
    height: 40px;
    background-image: linear-gradient(90deg, #ff0000 0%, #00ff00 50%, #0000ff 100%);
}
'''
html = mk("S33", "linear-gradient-native",
          ["linear-gradient stays Native on the 6000.6 matrix, where the function exists in the assembly but is not documented: a warning asks for a screenshot check",
           "side keywords become angles (to right is 90deg, to bottom 180deg, to left 270deg, to top 0deg); colour stops are kept"],
          ["C011"], ["Native"], "convert", "PASS WITH WARNINGS", css, body, ["gd"],
          {"Screens/Gradients.uxml": exp_uxml, "Screens/Gradients.uss": exp_uss},
          [D("C011", "Warning", "Native", "page.html", 0), D("C011", "Warning", "Native", "page.html", 0)], "Gradients",
          notes="The exact syntax accepted by the Unity function is unverified (assembly-only evidence); the Unity operator confirms with a screenshot.")
fix_lines("S33-linear-gradient-native", html, [("linear-gradient(180deg", 1), ("linear-gradient(to right", 1)])

# ---------------------------------------------------------------- S34 radial-gradient-bake
key = "radial-gradient(circle, #ffffff, #000000)|64x64"
h = fnv1a(key)
css = ROOT_BOX % "rg" + '''
.rg__orb { width: 64px; height: 64px; background-image: radial-gradient(circle, #ffffff, #000000); }
'''
body = '''
<div name="rg-root" class="rg">
  <div name="rg-orb" class="rg__orb"></div>
</div>
'''
exp_uxml = uxml(["Orb.uss"], '''
    <ui:VisualElement name="rg-root" class="rg">
        <ui:VisualElement name="rg-orb" class="rg__orb" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "rg" + '''
.rg__orb {
    width: 64px;
    height: 64px;
    background-image: url("../Generated/gradient-%s.png");
}
''' % h
png_expect = {"width": 64, "height": 64, "tolerance": 8,
              "samples": [{"x": 32, "y": 32, "rgba": [251, 251, 251, 255]}, {"x": 0, "y": 0, "rgba": [4, 4, 4, 255]},
                          {"x": 32, "y": 0, "rgba": [78, 78, 78, 255]}]}
html = mk("S34", "radial-gradient-bake",
          ["radial-gradient has no USS function: it is baked into a PNG whose size is the declared width x height of the same rule (256x256 when unsized)",
           "the file name is gradient-<fnv1a of 'canonical value|WxH'>.png under the generated folder, referenced relative to the USS file; the declaration keeps its place",
           "pixel centres are at (x+0.5, y+0.5); circle means farthest-corner; the golden runner checks size and sample pixels within the tolerance"],
          ["C011"], ["ConvertTime"], "convert", "PASS", css, body, ["rg"],
          {"Screens/Orb.uxml": exp_uxml, "Screens/Orb.uss": exp_uss},
          [D("C011", "Info", "ConvertTime", "page.html", 0, "gradient-bake")], "Orb")
fix_lines("S34-radial-gradient-bake", html, [("radial-gradient", 1)])
put("S34-radial-gradient-bake", "Generated/gradient-%s.png.expect.json" % h, dump(png_expect))

# ---------------------------------------------------------------- S35 box-shadow-drop-shadow
css = ROOT_BOX % "bs" + '''
.bs__card { width: 200px; height: 100px; border-radius: 12px; box-shadow: 0 8px 16px rgba(0, 0, 0, 0.5); }
.bs__in { height: 20px; box-shadow: inset 0 0 4px #000000; }
'''
body = '''
<div name="bs-root" class="bs">
  <div name="bs-card" class="bs__card"></div>
  <div name="bs-in" class="bs__in"></div>
</div>
'''
exp_uxml = uxml(["Shadow.uss"], '''
    <ui:VisualElement name="bs-root" class="bs">
        <ui:VisualElement name="bs-card" class="bs__card" />
        <ui:VisualElement name="bs-in" class="bs__in" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "bs" + '''
.bs__card {
    width: 200px;
    height: 100px;
    border-radius: 12px;
    filter: drop-shadow(0 8px 16px rgba(0, 0, 0, 0.5));
}

.bs__in {
    height: 20px;
}
'''
html = mk("S35", "box-shadow-drop-shadow",
          ["box-shadow has no USS property, but the 6000.6 matrix has the drop-shadow filter: an outer shadow is rewritten to filter: drop-shadow(x y blur colour) (Native, with a warning that the shadow follows the rendered alpha and spread is not supported)",
           "an inset shadow cannot be expressed: C013 Reject; a matrix without drop-shadow would use the shadow-bake emulation (later fixture)"],
          ["C013"], ["Native", "Reject"], "convert", "FAIL", css, body, ["bs"],
          {"Screens/Shadow.uxml": exp_uxml, "Screens/Shadow.uss": exp_uss},
          [D("C013", "Error", "Reject", "page.html", 0), D("C013", "Warning", "Native", "page.html", 0)], "Shadow", force=True,
          notes="The drop-shadow syntax of the Unity filter is unverified; the Unity operator confirms with a screenshot.")
fix_lines("S35-box-shadow-drop-shadow", html, [("box-shadow: inset", 1), ("box-shadow: 0 8px", 1)])

# ---------------------------------------------------------------- S36 backdrop-blur-native
css = ROOT_BOX % "gl" + '''
.gl__panel { height: 80px; background-color: rgba(22, 32, 42, 0.6); backdrop-filter: blur(8px); }
'''
body = '''
<div name="gl-root" class="gl">
  <div name="gl-panel" class="gl__panel"></div>
</div>
'''
exp_uxml = uxml(["Glass.uss"], '''
    <ui:VisualElement name="gl-root" class="gl">
        <ui:VisualElement name="gl-panel" class="gl__panel" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "gl" + '''
.gl__panel {
    height: 80px;
    background-color: rgba(22, 32, 42, 0.6);
    backdrop-filter: blur(8px);
}
'''
html = mk("S36", "backdrop-blur-native",
          ["backdrop-filter: blur() passes through because the matrix contains the property and the blur filter",
           "the warning carries the matrix condition: it renders only on panels in Screen Space Overlay mode"],
          ["C010"], ["Native"], "convert", "PASS WITH WARNINGS", css, body, ["gl"],
          {"Screens/Glass.uxml": exp_uxml, "Screens/Glass.uss": exp_uss},
          [D("C010", "Warning", "Native", "page.html", 0)], "Glass")
fix_lines("S36-backdrop-blur-native", html, [("backdrop-filter", 1)])

# ---------------------------------------------------------------- S37 blur-runtime-fallback
exp_uss = ROOT_USS % "gl" + '''
.gl__panel {
    height: 80px;
    background-color: rgba(22, 32, 42, 0.6);
}
'''
exp_rt = dump({"schema": 1, "components": [{"id": "BackdropBlur", "selector": ".gl__panel", "params": {"radius": "8px"}}]})
patch = [{"op": "remove", "path": "filters", "item": "blur"}, {"op": "remove", "path": "properties", "key": "backdrop-filter"}]
html = mk("S37", "blur-runtime-fallback",
          ["with a matrix that lacks backdrop-filter and the blur filter the same input uses the BackdropBlur runtime component",
           "the declaration is dropped from the USS and the component, selector and radius are listed in Page.runtime.json"],
          ["C010"], ["Runtime"], "convert", "PASS WITH WARNINGS", css, body, ["gl"],
          {"Screens/Glass.uxml": exp_uxml, "Screens/Glass.uss": exp_uss, "Screens/Glass.runtime.json": exp_rt},
          [D("C010", "Warning", "Runtime", "page.html", 0, "backdrop-blur")], "Glass", patch=patch)
fix_lines("S37-blur-runtime-fallback", html, [("backdrop-filter", 1)])

# ---------------------------------------------------------------- S38 transform-decompose
css = ROOT_BOX % "td" + '''
.td__a { transform: translate(10px, 5px) rotate(45deg) scale(1.2); }
.td__b { transform: rotate(10deg) translate(5px, 0px); }
.td__c { transform: matrix(1, 0, 0, 1, 0, 0); }
'''
body = '''
<div name="td-root" class="td">
  <div name="td-a" class="td__a"></div>
  <div name="td-b" class="td__b"></div>
  <div name="td-c" class="td__c"></div>
</div>
'''
exp_uxml = uxml(["Transforms.uss"], '''
    <ui:VisualElement name="td-root" class="td">
        <ui:VisualElement name="td-a" class="td__a" />
        <ui:VisualElement name="td-b" class="td__b" />
        <ui:VisualElement name="td-c" class="td__c" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "td" + '''
.td__a {
    translate: 10px 5px;
    rotate: 45deg;
    scale: 1.2;
}
'''
html = mk("S38", "transform-decompose",
          ["a transform with translate, rotate and scale in that order (each at most once) becomes the three USS properties",
           "another order is not reducible: E001 Reject; matrix() is C022 Reject; both rules are dropped under --force"],
          ["C022", "E001"], ["ConvertTime", "Reject"], "convert", "FAIL", css, body, ["td"],
          {"Screens/Transforms.uxml": exp_uxml, "Screens/Transforms.uss": exp_uss},
          [D("E001", "Error", "Reject", "page.html", 0), D("C022", "Error", "Reject", "page.html", 0),
           D("C022", "Info", "ConvertTime", "page.html", 0, "transform-decompose")], "Transforms", force=True)
fix_lines("S38-transform-decompose", html, [("rotate(10deg) translate", 1), ("matrix(", 1), ("translate(10px, 5px) rotate", 1)])

# ---------------------------------------------------------------- S39 keyframes-spec
css = ROOT_BOX % "an" + '''
@keyframes fade-in-up { from { opacity: 0; translate: 0 12px; } to { opacity: 1; translate: 0 0; } }
@keyframes pulse { 0% { scale: 1; } 50% { scale: 1.1; } 100% { scale: 1; } }
.an__card { width: 200px; animation: fade-in-up 0.4s ease-out forwards; }
.an__pulse { width: 40px; animation: pulse 1s ease-in-out 0.2s 3 alternate; }
'''
body = '''
<div name="an-root" class="an">
  <div name="an-card" class="an__card"></div>
  <div name="an-pulse" class="an__pulse"></div>
</div>
'''
exp_uxml = uxml(["Anim.uss"], '''
    <ui:VisualElement name="an-root" class="an">
        <ui:VisualElement name="an-card" class="an__card" />
        <ui:VisualElement name="an-pulse" class="an__pulse" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "an" + '''
.an__card {
    width: 200px;
}

.an__pulse {
    width: 40px;
}
'''
exp_anim = dump({"schema": 1, "animations": [
    {"id": "fade-in-up", "keyframes": "fade-in-up", "duration": 0.4, "delay": 0, "iterations": 1, "direction": "normal", "fill": "forwards",
     "easing": "ease-out", "steps": [{"offset": 0, "set": {"opacity": "0", "translate": "0 12px"}}, {"offset": 1, "set": {"opacity": "1", "translate": "0 0"}}]},
    {"id": "pulse", "keyframes": "pulse", "duration": 1, "delay": 0.2, "iterations": 3, "direction": "alternate", "fill": "none",
     "easing": "ease-in-out", "steps": [{"offset": 0, "set": {"scale": "1"}}, {"offset": 0.5, "set": {"scale": "1.1"}}, {"offset": 1, "set": {"scale": "1"}}]}],
    "bindings": [{"selector": ".an__card", "animation": "fade-in-up", "trigger": "onShow"}, {"selector": ".an__pulse", "animation": "pulse", "trigger": "onShow"}]})
exp_rt = dump({"schema": 1, "components": [{"id": "UitkAnimator", "uses": "Anim.anim.json"}]})
html = mk("S39", "keyframes-spec",
          ["@keyframes plus animation become Page.anim.json (steps with offsets and USS values) and bindings; animation declarations leave the USS",
           "from and to are offsets 0 and 1, percentages divide by 100; shorthand tokens map to duration, delay, iterations, direction, fill and easing (Unity easing names)",
           "Page.runtime.json lists UitkAnimator; each @keyframes reports C007 as a runtime-tier warning"],
          ["C007"], ["Runtime"], "convert", "PASS WITH WARNINGS", css, body, ["an"],
          {"Screens/Anim.uxml": exp_uxml, "Screens/Anim.uss": exp_uss, "Screens/Anim.anim.json": exp_anim, "Screens/Anim.runtime.json": exp_rt},
          [D("C007", "Warning", "Runtime", "page.html", 0, "keyframes-animator"), D("C007", "Warning", "Runtime", "page.html", 0, "keyframes-animator")], "Anim")
fix_lines("S39-keyframes-spec", html, [("@keyframes fade-in-up", 1), ("@keyframes pulse", 1)])

# ---------------------------------------------------------------- S40 keyframes-layout-reject
css = ROOT_BOX % "kr" + '''
@keyframes grow {
    from { width: 10px; }
    to { width: 100px; box-shadow: 0 0 4px #000000; }
}
.kr__a { height: 20px; animation: grow 1s ease-out; }
.kr__b { height: 20px; animation: missing 1s; }
'''
body = '''
<div name="kr-root" class="kr">
  <div name="kr-a" class="kr__a"></div>
  <div name="kr-b" class="kr__b"></div>
</div>
'''
html = mk("S40", "keyframes-layout-reject",
          ["animating a layout property (width) is rejected (A003) with a translate or scale rewrite",
           "an animated property that is not a USS property (box-shadow) is rejected (A002); an animation that names a missing @keyframes is A001"],
          ["A001", "A002", "A003"], ["Reject"], "validate", "FAIL", css, body, ["kr"], {},
          [D("A003", "Error", "Reject", "page.html", 0), D("A002", "Error", "Reject", "page.html", 0),
           D("A001", "Error", "Reject", "page.html", 0)], "KeyframeRejects")
fix_lines("S40-keyframes-layout-reject", html, [("from { width: 10px", 1), ("to { width: 100px", 1), ("animation: missing", 1)])

# ---------------------------------------------------------------- S41 stagger-reduced-motion
css = ROOT_BOX % "st" + '''
@keyframes pop { from { opacity: 0; scale: 0.9; } to { opacity: 1; scale: 1; } }
@keyframes spin { from { rotate: 0deg; } to { rotate: 360deg; } }
.st__item { height: 24px; animation: pop 0.3s ease-out backwards; --anim-stagger: 0.05s; --anim-reduced-motion: skip; }
.st__spin { width: 24px; height: 24px; animation: spin 2s linear infinite; }
'''
body = '''
<div name="st-root" class="st">
  <div name="st-1" class="st__item"></div>
  <div name="st-2" class="st__item"></div>
  <div name="st-3" class="st__item"></div>
  <div name="st-spin" class="st__spin"></div>
</div>
'''
exp_uxml = uxml(["Motion.uss"], '''
    <ui:VisualElement name="st-root" class="st">
        <ui:VisualElement name="st-1" class="st__item" />
        <ui:VisualElement name="st-2" class="st__item" />
        <ui:VisualElement name="st-3" class="st__item" />
        <ui:VisualElement name="st-spin" class="st__spin" />
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "st" + '''
.st__item {
    height: 24px;
}

.st__spin {
    width: 24px;
    height: 24px;
}
'''
exp_anim = dump({"schema": 1, "animations": [
    {"id": "pop", "keyframes": "pop", "duration": 0.3, "delay": 0, "iterations": 1, "direction": "normal", "fill": "backwards", "easing": "ease-out",
     "steps": [{"offset": 0, "set": {"opacity": "0", "scale": "0.9"}}, {"offset": 1, "set": {"opacity": "1", "scale": "1"}}]},
    {"id": "spin", "keyframes": "spin", "duration": 2, "delay": 0, "iterations": "infinite", "direction": "normal", "fill": "none", "easing": "linear",
     "steps": [{"offset": 0, "set": {"rotate": "0deg"}}, {"offset": 1, "set": {"rotate": "360deg"}}]}],
    "bindings": [{"selector": ".st__item", "animation": "pop", "trigger": "onShow", "stagger": 0.05, "reducedMotion": "skip"},
                 {"selector": ".st__spin", "animation": "spin", "trigger": "onShow"}]})
exp_rt = dump({"schema": 1, "components": [{"id": "UitkAnimator", "uses": "Motion.anim.json"}]})
html = mk("S41", "stagger-reduced-motion",
          ["the custom properties --anim-stagger and --anim-reduced-motion (skip or simplify) in an animation rule are read by the converter, never written to the USS, and appear in the binding",
           "an infinite animation without reduced-motion handling is the warning A005",
           "--anim-trigger (onShow, onHide, onLoad, manual) selects the trigger and defaults to onShow"],
          ["A005", "C007"], ["Runtime", "Native"], "convert", "PASS WITH WARNINGS", css, body, ["st"],
          {"Screens/Motion.uxml": exp_uxml, "Screens/Motion.uss": exp_uss, "Screens/Motion.anim.json": exp_anim, "Screens/Motion.runtime.json": exp_rt},
          [D("C007", "Warning", "Runtime", "page.html", 0, "keyframes-animator"), D("C007", "Warning", "Runtime", "page.html", 0, "keyframes-animator"),
           D("A005", "Warning", "Native", "page.html", 0)], "Motion")
fix_lines("S41-stagger-reduced-motion", html, [("@keyframes pop", 1), ("@keyframes spin", 1), ("animation: spin", 1)])

# ---------------------------------------------------------------- S42 class-toggle-transitions
css = ROOT_BOX % "cp" + '''
.cp__panel {
    height: 80px;
    opacity: 0;
    translate: 0 12px;
    transition: opacity 0.2s ease-out, translate 0.2s ease-out;
}
.cp__panel.is-open { opacity: 1; translate: 0 0; }
'''
body = '''
<div name="cp-root" class="cp">
  <div name="cp-panel" class="cp__panel"></div>
</div>
'''
make("S42", "class-toggle-transitions",
     ["enter and leave animations driven by a class toggled in code need only a transition: no animation spec and no runtime component",
      "the dynamic class (is-open) is exempt from the undefined-class check by naming.dynamicClassPattern"],
     [], ["Native"], "convert", "PASS",
     {"page.html": page("Toggle", css, body)},
     config=cfg("input/none.json", ["cp", "is"]), main=["page.html"], oracle=True, compare={"columns": False})

# ---------------------------------------------------------------- S43 pages-nav-modal-tabs
css = ROOT_BOX % "app" + '''
.app__page { flex-grow: 1; }
.app__t { font-size: 16px; }
.app__b { height: 44px; min-width: 100px; }
.app__modal { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.app__tabs { flex-direction: row; }
.app__panel { flex-grow: 1; }
'''
body = '''
<div name="app-root" class="app">
  <section name="app-home" class="app__page" data-page="home" data-start data-transition="fade" data-transition-ms="220">
    <span name="app-home-title" class="app__t" data-loc="app.home.title">Home</span>
    <button name="app-home-settings" class="app__b" data-loc="app.home.settings" data-nav="settings" data-nav-mode="push" data-transition="slide-left" data-transition-ms="260">Settings</button>
    <button name="app-home-quit" class="app__b" data-loc="app.home.quit" data-open-modal="confirm">Quit</button>
    <div name="app-confirm" class="app__modal" data-modal="confirm">
      <span name="app-confirm-text" class="app__t" data-loc="app.confirm.text">Really quit?</span>
      <button name="app-confirm-cancel" class="app__b" data-loc="app.confirm.cancel" data-close>Cancel</button>
    </div>
  </section>
  <section name="app-settings" class="app__page" data-page="settings">
    <button name="app-settings-back" class="app__b" data-loc="app.settings.back" data-back>Back</button>
    <div name="app-tabs" class="app__tabs" data-tabs="settings-tabs">
      <button name="app-tab-audio" class="app__b" data-loc="app.tab.audio" data-tab="app-panel-audio">Audio</button>
      <button name="app-tab-video" class="app__b" data-loc="app.tab.video" data-tab="app-panel-video">Video</button>
    </div>
    <div name="app-panel-audio" class="app__panel"><span name="app-audio-text" class="app__t" data-loc="app.audio.text">Audio options</span></div>
    <div name="app-panel-video" class="app__panel"><span name="app-video-text" class="app__t" data-loc="app.video.text">Video options</span></div>
  </section>
</div>
'''
S43_STR = {"app.home.title": "Home", "app.home.settings": "Settings", "app.home.quit": "Quit", "app.confirm.text": "Really quit?",
           "app.confirm.cancel": "Cancel", "app.settings.back": "Back", "app.tab.audio": "Audio", "app.tab.video": "Video",
           "app.audio.text": "Audio options", "app.video.text": "Video options"}
exp_uxml = uxml(["App.uss"], '''
    <ui:VisualElement name="app-root" class="app">
        <ui:VisualElement name="app-home" class="app__page">
            <ui:Label name="app-home-title" text="app.home.title" class="app__t" />
            <ui:Button name="app-home-settings" text="app.home.settings" class="app__b" />
            <ui:Button name="app-home-quit" text="app.home.quit" class="app__b" />
            <ui:VisualElement name="app-confirm" class="app__modal is-hidden">
                <ui:Label name="app-confirm-text" text="app.confirm.text" class="app__t" />
                <ui:Button name="app-confirm-cancel" text="app.confirm.cancel" class="app__b" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="app-settings" class="app__page is-hidden">
            <ui:Button name="app-settings-back" text="app.settings.back" class="app__b" />
            <ui:VisualElement name="app-tabs" class="app__tabs">
                <ui:Button name="app-tab-audio" text="app.tab.audio" class="app__b" />
                <ui:Button name="app-tab-video" text="app.tab.video" class="app__b" />
            </ui:VisualElement>
            <ui:VisualElement name="app-panel-audio" class="app__panel">
                <ui:Label name="app-audio-text" text="app.audio.text" class="app__t" />
            </ui:VisualElement>
            <ui:VisualElement name="app-panel-video" class="app__panel is-hidden">
                <ui:Label name="app-video-text" text="app.video.text" class="app__t" />
            </ui:VisualElement>
        </ui:VisualElement>
    </ui:VisualElement>''')
exp_uss = ROOT_USS % "app" + '''
.app__page {
    flex-grow: 1;
}

.app__b {
    min-width: 100px;
    height: 44px;
}

.app__modal {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.app__tabs {
    flex-direction: row;
}

.app__panel {
    flex-grow: 1;
}

.app__t {
    font-size: 16px;
}

.is-hidden {
    display: none;
}
'''
exp_nav = dump({"schema": 1, "document": "App", "start": "home",
                "pages": [{"id": "home", "element": "app-home", "focus": "app-home-settings", "transition": {"in": "fade", "out": "fade", "ms": 220}},
                          {"id": "settings", "element": "app-settings", "focus": "app-settings-back"}],
                "edges": [{"from": "home", "trigger": "app-home-settings", "action": "navigate", "to": "settings", "mode": "push", "transition": "slide-left", "ms": 260},
                          {"from": "home", "trigger": "app-home-quit", "action": "openModal", "to": "confirm"},
                          {"from": "settings", "trigger": "app-settings-back", "action": "back"}],
                "modals": [{"id": "confirm", "element": "app-confirm", "ownerPage": "home", "closeTriggers": ["app-confirm-cancel"]}],
                "tabs": [{"group": "settings-tabs", "page": "settings", "default": "app-tab-audio",
                          "tabs": [{"tab": "app-tab-audio", "panel": "app-panel-audio"}, {"tab": "app-tab-video", "panel": "app-panel-video"}]}]})
exp_rt = dump({"schema": 1, "components": [{"id": "UitkRouter", "uses": "App.nav.json"}]})
html = mk("S43", "pages-nav-modal-tabs",
          ["data-page, data-start, data-nav, data-nav-mode, data-back, data-open-modal, data-modal, data-close, data-tabs and data-tab become Page.nav.json; the dialect attributes never reach the UXML",
           "pages other than the start page, modals and non-default tab panels get the hidden class (pages.classes.hidden); the converter adds the .is-hidden rule to the utilities section when the page CSS lacks it",
           "page focus is the first interactive element (or data-focus); edge transition and ms come from the trigger, page transition from the page element",
           "Page.runtime.json lists UitkRouter; the nav diagnostic is the info P011"],
          ["P011"], ["Runtime"], "convert", "PASS", css, body, ["app", "is"],
          {"Screens/App.uxml": exp_uxml, "Screens/App.uss": exp_uss, "Screens/App.nav.json": exp_nav, "Screens/App.runtime.json": exp_rt},
          [D("P011", "Info", "Runtime", "page.html", 0, "page-router")], "App", strings_=S43_STR)
fix_lines("S43-pages-nav-modal-tabs", html, [('data-page="home"', 1)])

# ---------------------------------------------------------------- S44 nav-validation-errors
css = ROOT_BOX % "er" + '''
.er__page { flex-grow: 1; }
.er__t { font-size: 16px; }
.er__b { height: 44px; min-width: 100px; }
.er__modal { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.er__tabs { flex-direction: row; }
'''
body = '''
<div name="er-root" class="er">
  <section name="er-a" class="er__page" data-page="a">
    <button name="er-a-go" class="er__b" data-loc="er.go" data-nav="nowhere">Go</button>
    <button name="er-a-spin" class="er__b" data-loc="er.go" data-nav="b" data-transition="spin">Go</button>
    <div name="er-m1" class="er__modal" data-modal="m"></div>
    <div name="er-m2" class="er__modal" data-modal="m"></div>
    <div name="er-tabs" class="er__tabs" data-tabs="g">
      <button name="er-tab" class="er__b" data-loc="er.go" data-tab="er-missing">Go</button>
    </div>
  </section>
  <section name="er-b" class="er__page" data-page="b">
    <span name="er-b-text" class="er__t" data-loc="er.text">Text only</span>
  </section>
  <section name="er-lost" class="er__page" data-page="lost">
    <button name="er-lost-btn" class="er__b" data-loc="er.go">Go</button>
  </section>
</div>
'''
html = mk("S44", "nav-validation-errors",
          ["a flow without data-start is P001; data-nav to an unknown page is P003; an unknown data-transition is P009",
           "duplicate modal ids are P007; a data-tab whose panel does not exist is P008",
           "a page without a focusable element is the warning P010; a page that nothing links to is the warning P004"],
          ["P001", "P003", "P004", "P007", "P008", "P009", "P010"], ["Reject", "Native"], "validate", "FAIL", css, body, ["er"], {},
          [D("P001", "Error", "Reject", "page.html", 0), D("P003", "Error", "Reject", "page.html", 0), D("P009", "Error", "Reject", "page.html", 0),
           D("P007", "Error", "Reject", "page.html", 0), D("P008", "Error", "Reject", "page.html", 0),
           D("P010", "Warning", "Native", "page.html", 0), D("P004", "Warning", "Native", "page.html", 0)], "NavErrors",
          strings_={"er.go": "Go", "er.text": "Text only"})
fix_lines("S44-nav-validation-errors", html, [('data-page="a"', 1), ('data-nav="nowhere"', 1), ('data-transition="spin"', 1), ('data-modal="m"', 2),
                                               ('data-tab="er-missing"', 1), ('data-page="b"', 1), ('data-page="lost"', 1)])
