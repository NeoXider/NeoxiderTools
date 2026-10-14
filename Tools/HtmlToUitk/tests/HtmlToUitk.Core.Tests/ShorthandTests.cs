using System;
using System.Linq;
using HtmlToUitk.Core.Css;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class ShorthandTests
    {
        [TestCase("margin:4px", "margin-top: 4px; margin-right: 4px; margin-bottom: 4px; margin-left: 4px")]
        [TestCase("margin:1px 2px", "margin-top: 1px; margin-right: 2px; margin-bottom: 1px; margin-left: 2px")]
        [TestCase("margin:1px 2px 3px", "margin-top: 1px; margin-right: 2px; margin-bottom: 3px; margin-left: 2px")]
        [TestCase("margin:1px 2px 3px 4px", "margin-top: 1px; margin-right: 2px; margin-bottom: 3px; margin-left: 4px")]
        [TestCase("margin:0 auto", "margin-top: 0; margin-right: auto; margin-bottom: 0; margin-left: auto")]
        [TestCase("margin:auto", "margin-top: auto; margin-right: auto; margin-bottom: auto; margin-left: auto")]
        [TestCase("margin:1px 2px 3px 4px 5px", "margin: 1px 2px 3px 4px 5px")]
        [TestCase("padding:8px 16px", "padding-top: 8px; padding-right: 16px; padding-bottom: 8px; padding-left: 16px")]
        [TestCase("padding:1px 2px 3px 4px", "padding-top: 1px; padding-right: 2px; padding-bottom: 3px; padding-left: 4px")]
        [TestCase("padding:0", "padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0")]
        [TestCase("inset:0", "top: 0; right: 0; bottom: 0; left: 0")]
        [TestCase("inset:10px 20px", "top: 10px; right: 20px; bottom: 10px; left: 20px")]
        [TestCase("inset:1px 2px 3px", "top: 1px; right: 2px; bottom: 3px; left: 2px")]
        [TestCase("margin-inline:4px 8px", "margin-left: 4px; margin-right: 8px")]
        [TestCase("margin-inline:4px", "margin-left: 4px; margin-right: 4px")]
        [TestCase("margin-block:4px 8px", "margin-top: 4px; margin-bottom: 8px")]
        [TestCase("padding-inline:1px 2px", "padding-left: 1px; padding-right: 2px")]
        [TestCase("padding-block:5px", "padding-top: 5px; padding-bottom: 5px")]
        [TestCase("inset-inline:0", "left: 0; right: 0")]
        [TestCase("inset-block:1px 2px", "top: 1px; bottom: 2px")]
        [TestCase("border-width:1px 2px", "border-top-width: 1px; border-right-width: 2px; border-bottom-width: 1px; border-left-width: 2px")]
        [TestCase("border-color:red blue", "border-top-color: red; border-right-color: blue; border-bottom-color: red; border-left-color: blue")]
        [TestCase("border-style:solid", "border-top-style: solid; border-right-style: solid; border-bottom-style: solid; border-left-style: solid")]
        [TestCase("border-radius:4px", "border-top-left-radius: 4px; border-top-right-radius: 4px; border-bottom-right-radius: 4px; border-bottom-left-radius: 4px")]
        [TestCase("border-radius:1px 2px", "border-top-left-radius: 1px; border-top-right-radius: 2px; border-bottom-right-radius: 1px; border-bottom-left-radius: 2px")]
        [TestCase("border-radius:1px 2px 3px", "border-top-left-radius: 1px; border-top-right-radius: 2px; border-bottom-right-radius: 3px; border-bottom-left-radius: 2px")]
        [TestCase("border-radius:1px 2px 3px 4px", "border-top-left-radius: 1px; border-top-right-radius: 2px; border-bottom-right-radius: 3px; border-bottom-left-radius: 4px")]
        [TestCase("border-radius:50%", "border-top-left-radius: 50%; border-top-right-radius: 50%; border-bottom-right-radius: 50%; border-bottom-left-radius: 50%")]
        [TestCase("border-radius:10px / 20px", "border-radius: 10px / 20px")]
        public void ExpandsBoxShorthands(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        private const string BorderAll = "border-top-width: {w}; border-top-style: {s}; border-top-color: {c}; border-right-width: {w}; border-right-style: {s}; border-right-color: {c}; border-bottom-width: {w}; border-bottom-style: {s}; border-bottom-color: {c}; border-left-width: {w}; border-left-style: {s}; border-left-color: {c}";

        private static string Border(string w, string s, string c) => BorderAll.Replace("{w}", w).Replace("{s}", s).Replace("{c}", c);

        [Test]
        public void ExpandsBorderShorthand()
        {
            Assert.That(Support.Expand("border:1px solid red"), Is.EqualTo(Border("1px", "solid", "red")));
            Assert.That(Support.Expand("border:red 1px solid"), Is.EqualTo(Border("1px", "solid", "red")));
            Assert.That(Support.Expand("border:solid 2px #fff"), Is.EqualTo(Border("2px", "solid", "#fff")));
            Assert.That(Support.Expand("border:thin dotted rgba(0,0,0,.5)"), Is.EqualTo(Border("thin", "dotted", "rgba(0, 0, 0, 0.5)")));
        }

        [Test]
        public void BorderOmitsMissingPartsUnlessAskedToReset()
        {
            string none = Support.Expand("border:none");
            Assert.That(none, Is.EqualTo("border-top-style: none; border-right-style: none; border-bottom-style: none; border-left-style: none"));
            Assert.That(Support.Expand("border:0"), Does.StartWith("border-top-width: 0; border-right-width: 0"));
            Assert.That(Support.Expand("border:1px"), Does.Not.Contain("style"));
            Assert.That(Support.Expand("border:1px", true), Is.EqualTo(Border("1px", "initial", "initial")));
            Assert.That(Support.Expand("border:solid", true), Is.EqualTo(Border("initial", "solid", "initial")));
        }

        [TestCase("border-top:2px dashed #fff", "border-top-width: 2px; border-top-style: dashed; border-top-color: #fff")]
        [TestCase("border-right:1px solid", "border-right-width: 1px; border-right-style: solid")]
        [TestCase("border-bottom:red", "border-bottom-color: red")]
        [TestCase("border-left:3px", "border-left-width: 3px")]
        public void ExpandsSideBorders(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [TestCase("border:1px solid red blue")]
        [TestCase("border:1px 2px")]
        [TestCase("border:foo")]
        [TestCase("border:var(--b)")]
        [TestCase("border:1px solid var(--c)")]
        public void UnclassifiableBordersAreLeftAlone(string declaration)
        {
            Assert.That(Support.Expand(declaration), Does.StartWith("border: "));
        }

        [TestCase("flex:1", "flex-grow: 1; flex-shrink: 1; flex-basis: 0%")]
        [TestCase("flex:auto", "flex-grow: 1; flex-shrink: 1; flex-basis: auto")]
        [TestCase("flex:none", "flex-grow: 0; flex-shrink: 0; flex-basis: auto")]
        [TestCase("flex:2 3", "flex-grow: 2; flex-shrink: 3; flex-basis: 0%")]
        [TestCase("flex:1 100px", "flex-grow: 1; flex-shrink: 1; flex-basis: 100px")]
        [TestCase("flex:0 0 50%", "flex-grow: 0; flex-shrink: 0; flex-basis: 50%")]
        [TestCase("flex:1 2 3px", "flex-grow: 1; flex-shrink: 2; flex-basis: 3px")]
        [TestCase("flex:10px", "flex-grow: 1; flex-shrink: 1; flex-basis: 10px")]
        [TestCase("flex:0", "flex-grow: 0; flex-shrink: 1; flex-basis: 0%")]
        [TestCase("flex:foo bar", "flex: foo bar")]
        [TestCase("flex:1 1 1 1", "flex: 1 1 1 1")]
        [TestCase("flex:1 2 auto 4", "flex: 1 2 auto 4")]
        public void ExpandsFlex(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [TestCase("flex-flow:row wrap", "flex-direction: row; flex-wrap: wrap")]
        [TestCase("flex-flow:wrap", "flex-wrap: wrap")]
        [TestCase("flex-flow:column-reverse nowrap", "flex-direction: column-reverse; flex-wrap: nowrap")]
        [TestCase("flex-flow:wrap row", "flex-direction: row; flex-wrap: wrap")]
        [TestCase("flex-flow:row row", "flex-flow: row row")]
        [TestCase("flex-flow:1px", "flex-flow: 1px")]
        public void ExpandsFlexFlow(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [Test]
        public void FlexFlowResetsOmittedPartsWhenAsked()
        {
            Assert.That(Support.Expand("flex-flow:wrap", true), Is.EqualTo("flex-direction: initial; flex-wrap: wrap"));
        }

        [TestCase("overflow:hidden", "overflow-x: hidden; overflow-y: hidden")]
        [TestCase("overflow:auto hidden", "overflow-x: auto; overflow-y: hidden")]
        [TestCase("gap:4px", "row-gap: 4px; column-gap: 4px")]
        [TestCase("gap:4px 8px", "row-gap: 4px; column-gap: 8px")]
        [TestCase("place-items:center", "align-items: center; justify-items: center")]
        [TestCase("place-content:start end", "align-content: start; justify-content: end")]
        [TestCase("place-self:stretch", "align-self: stretch; justify-self: stretch")]
        public void ExpandsPairShorthands(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [TestCase("background:red", "background-color: red")]
        [TestCase("background:#fff", "background-color: #fff")]
        [TestCase("background:none", "background-image: none")]
        [TestCase("background:url(a.png)", "background-image: url(\"a.png\")")]
        [TestCase("background:url(a.png) no-repeat", "background-image: url(\"a.png\"); background-repeat: no-repeat")]
        [TestCase("background:url(a.png) no-repeat center / cover red", "background-color: red; background-image: url(\"a.png\"); background-position: center; background-size: cover; background-repeat: no-repeat")]
        [TestCase("background:linear-gradient(red, blue)", "background-image: linear-gradient(red, blue)")]
        [TestCase("background:center top", "background-position: center top")]
        [TestCase("background:10px 20px / 30px 40px", "background-position: 10px 20px; background-size: 30px 40px")]
        [TestCase("background:repeat-x fixed", "background-repeat: repeat-x; background-attachment: fixed")]
        [TestCase("background:padding-box content-box", "background-origin: padding-box; background-clip: content-box")]
        [TestCase("background:content-box", "background-origin: content-box; background-clip: content-box")]
        [TestCase("background:url(a.png), url(b.png)", "background: url(\"a.png\"), url(\"b.png\")")]
        [TestCase("background:red blue", "background: red blue")]
        [TestCase("background:url(a.png) url(b.png)", "background: url(\"a.png\") url(\"b.png\")")]
        [TestCase("background:var(--bg)", "background: var(--bg)")]
        [TestCase("background:wobble", "background: wobble")]
        public void ExpandsBackground(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [Test]
        public void BackgroundResetsOmittedPartsWhenAsked()
        {
            Assert.That(Support.Expand("background:red", true), Does.Contain("background-image: initial"));
            Assert.That(Support.Expand("background:red", true), Does.StartWith("background-color: red; background-image: initial"));
        }

        [TestCase("font:12px Arial", "font-size: 12px; font-family: Arial")]
        [TestCase("font:bold 14px sans-serif", "font-weight: bold; font-size: 14px; font-family: sans-serif")]
        [TestCase("font:italic bold 12px/1.5 \"A B\", serif", "font-style: italic; font-weight: bold; font-size: 12px; line-height: 1.5; font-family: \"A B\", serif")]
        [TestCase("font:normal 400 12px/normal Arial", "font-weight: 400; font-size: 12px; line-height: normal; font-family: Arial")]
        [TestCase("font:small-caps 700 1.5em Georgia", "font-variant: small-caps; font-weight: 700; font-size: 1.5em; font-family: Georgia")]
        [TestCase("font:large Times New Roman", "font-size: large; font-family: Times New Roman")]
        [TestCase("font:condensed 10px X", "font-stretch: condensed; font-size: 10px; font-family: X")]
        [TestCase("font:caption", "font: caption")]
        [TestCase("font:12px", "font: 12px")]
        [TestCase("font:bold Arial", "font: bold Arial")]
        [TestCase("font:var(--f)", "font: var(--f)")]
        public void ExpandsFont(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [TestCase("transition:opacity .2s", "transition-property: opacity; transition-duration: 0.2s; transition-timing-function: ease; transition-delay: 0s")]
        [TestCase("transition:opacity .2s ease-in 1s", "transition-property: opacity; transition-duration: 0.2s; transition-timing-function: ease-in; transition-delay: 1s")]
        [TestCase("transition:opacity .2s ease-in 1s, color 1s", "transition-property: opacity, color; transition-duration: 0.2s, 1s; transition-timing-function: ease-in, ease; transition-delay: 1s, 0s")]
        [TestCase("transition:none", "transition-property: none; transition-duration: 0s; transition-timing-function: ease; transition-delay: 0s")]
        [TestCase("transition:.3s linear", "transition-property: all; transition-duration: 0.3s; transition-timing-function: linear; transition-delay: 0s")]
        [TestCase("transition:opacity .2s cubic-bezier(0,0,1,1)", "transition-property: opacity; transition-duration: 0.2s; transition-timing-function: cubic-bezier(0, 0, 1, 1); transition-delay: 0s")]
        [TestCase("transition:all 250ms ease-out", "transition-property: all; transition-duration: 250ms; transition-timing-function: ease-out; transition-delay: 0s")]
        [TestCase("transition:a 1s 2s 3s", "transition: a 1s 2s 3s")]
        [TestCase("transition:a b", "transition: a b")]
        [TestCase("transition:var(--t)", "transition: var(--t)")]
        public void ExpandsTransition(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [TestCase("animation:fade .4s ease-out forwards", "animation-name: fade; animation-duration: 0.4s; animation-timing-function: ease-out; animation-delay: 0s; animation-iteration-count: 1; animation-direction: normal; animation-fill-mode: forwards; animation-play-state: running")]
        [TestCase("animation:spin 1s linear infinite", "animation-name: spin; animation-duration: 1s; animation-timing-function: linear; animation-delay: 0s; animation-iteration-count: infinite; animation-direction: normal; animation-fill-mode: none; animation-play-state: running")]
        [TestCase("animation:a 1s 2s 3 alternate both paused", "animation-name: a; animation-duration: 1s; animation-timing-function: ease; animation-delay: 2s; animation-iteration-count: 3; animation-direction: alternate; animation-fill-mode: both; animation-play-state: paused")]
        [TestCase("animation:a 1s, b 2s", "animation-name: a, b; animation-duration: 1s, 2s; animation-timing-function: ease, ease; animation-delay: 0s, 0s; animation-iteration-count: 1, 1; animation-direction: normal, normal; animation-fill-mode: none, none; animation-play-state: running, running")]
        [TestCase("animation:1s", "animation-name: none; animation-duration: 1s; animation-timing-function: ease; animation-delay: 0s; animation-iteration-count: 1; animation-direction: normal; animation-fill-mode: none; animation-play-state: running")]
        [TestCase("animation:a 1s 2s 3s", "animation: a 1s 2s 3s")]
        public void ExpandsAnimation(string declaration, string expected)
        {
            Assert.That(Support.Expand(declaration), Is.EqualTo(expected));
        }

        [Test]
        public void ImportantIsCopiedToEveryLonghand()
        {
            string text = Support.Expand("margin:1px!important");
            Assert.That(text.Split(';').Length, Is.EqualTo(4));
            Assert.That(text.Split(';').All(part => part.EndsWith("!important")), Is.True);
        }

        [TestCase("margin:inherit", 4)]
        [TestCase("border:initial", 12)]
        [TestCase("flex:unset", 3)]
        [TestCase("font:inherit", 7)]
        [TestCase("transition:revert", 4)]
        [TestCase("background:initial", 8)]
        public void CssWideKeywordsApplyToEveryLonghand(string declaration, int count)
        {
            string text = Support.Expand(declaration);
            Assert.That(text.Split(';').Length, Is.EqualTo(count));
            string keyword = declaration.Substring(declaration.IndexOf(':') + 1);
            Assert.That(text.Split(';').All(part => part.EndsWith(": " + keyword)), Is.True);
        }

        [Test]
        public void VariablesInBoxShorthandsStayTokens()
        {
            Assert.That(Support.Expand("margin:var(--a) var(--b)"), Is.EqualTo("margin-top: var(--a); margin-right: var(--b); margin-bottom: var(--a); margin-left: var(--b)"));
        }

        [Test]
        public void NonShorthandsAreReturnedUnchanged()
        {
            Assert.That(Support.Expand("color:red"), Is.EqualTo("color: red"));
            Assert.That(Support.Expand("margin-top:1px"), Is.EqualTo("margin-top: 1px"));
            Assert.That(ShorthandExpander.IsShorthand("color"), Is.False);
            Assert.That(ShorthandExpander.IsShorthand("margin"), Is.True);
        }

        [Test]
        public void TryExpandReportsWhyItFailed()
        {
            System.Collections.Generic.IReadOnlyList<Declaration> result;
            string problem;
            Assert.That(ShorthandExpander.TryExpand(Support.Decl("margin:1px 2px 3px 4px 5px"), null, out result, out problem), Is.False);
            Assert.That(problem, Does.Contain("1 to 4"));
            Assert.That(ShorthandExpander.TryExpand(Support.Decl("color:red"), null, out result, out problem), Is.False);
            Assert.That(problem, Is.EqualTo("not a shorthand"));
            Assert.That(ShorthandExpander.TryExpand(null, null, out result, out problem), Is.False);
        }


        [Test]
        public void LonghandNamesMatchTheExpansion()
        {
            foreach (string name in new[] { "margin", "padding", "inset", "border-width", "border-radius", "flex", "flex-flow", "overflow", "gap", "transition", "animation", "margin-inline", "place-items" })
            {
                var declaration = new Declaration { Name = name, Value = CssValueParser.Parse(name == "transition" ? "a 1s" : name == "animation" ? "a 1s" : name == "flex-flow" ? "row wrap" : name == "flex" ? "1 2 3px" : name == "overflow" || name == "gap" || name == "place-items" || name.EndsWith("inline") ? "a b" : "1px 2px 3px 4px") };
                var expanded = ShorthandExpander.Expand(declaration, ShorthandOptions.Default).Select(d => d.Name).ToArray();
                Assert.That(expanded, Is.EquivalentTo(ShorthandExpander.LonghandNames(name)), name);
            }
        }

        [Test]
        public void ExpansionKeepsSpans()
        {
            Declaration declaration = Support.Css(".a{\n  margin: 1px 2px;\n}").Rules[0].Declarations[0];
            foreach (Declaration longhand in ShorthandExpander.Expand(declaration))
            {
                Assert.That(longhand.Span.Start.Line, Is.EqualTo(2));
                Assert.That(longhand.Value.Span.Start.Line, Is.EqualTo(2));
            }
        }
    }
}
