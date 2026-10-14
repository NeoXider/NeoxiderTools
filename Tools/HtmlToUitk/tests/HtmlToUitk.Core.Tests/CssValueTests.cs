using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Css;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class CssValueTests
    {
        private static string Text(string input) => CssValueParser.Parse(input).ToText();

        [TestCase("10px", "10px")]
        [TestCase("0px", "0")]
        [TestCase("0", "0")]
        [TestCase("0%", "0%")]
        [TestCase("0s", "0s")]
        [TestCase("1.50em", "1.5em")]
        [TestCase(".5", "0.5")]
        [TestCase("-4px", "-4px")]
        [TestCase("+3px", "3px")]
        [TestCase("50%", "50%")]
        [TestCase("1e2px", "100px")]
        [TestCase("1e-2", "0.01")]
        [TestCase("10PX", "10px")]
        [TestCase("2.00006px", "2.0001px")]
        [TestCase("0.00001px", "0")]
        [TestCase("-0.00001", "0")]
        [TestCase("12.3456789px", "12.3457px")]
        [TestCase("100vw", "100vw")]
        [TestCase("1.5rem", "1.5rem")]
        [TestCase("90deg", "90deg")]
        [TestCase("250ms", "250ms")]
        [TestCase("#FFF", "#FFF")]
        [TestCase("#abcdef80", "#abcdef80")]
        [TestCase("rgb(1,2,3)", "rgb(1, 2, 3)")]
        [TestCase("rgba(30, 58, 58, 0.8)", "rgba(30, 58, 58, 0.8)")]
        [TestCase("rgba(0,0,0,.5)", "rgba(0, 0, 0, 0.5)")]
        [TestCase("rgb(0 0 0 / 50%)", "rgba(0, 0, 0, 0.5)")]
        [TestCase("rgb(100% 0% 0%)", "rgb(255, 0, 0)")]
        [TestCase("red", "red")]
        [TestCase("Inherit", "Inherit")]
        [TestCase("\"a b\"", "\"a b\"")]
        [TestCase("'single'", "\"single\"")]
        [TestCase("\"say \\\"hi\\\"\"", "\"say \\\"hi\\\"\"")]
        [TestCase("url(a.png)", "url(\"a.png\")")]
        [TestCase("url( \"x y.png\" )", "url(\"x y.png\")")]
        [TestCase("url('a')", "url(\"a\")")]
        [TestCase("url(data:image/png;base64,AAAA)", "url(\"data:image/png;base64,AAAA\")")]
        [TestCase("var(--a)", "var(--a)")]
        [TestCase("var(--a, 4px)", "var(--a, 4px)")]
        [TestCase("var( --a ,red )", "var(--a, red)")]
        [TestCase("var(--a, var(--b, 1px))", "var(--a, var(--b, 1px))")]
        [TestCase("var(--a, 1px 2px)", "var(--a, 1px 2px)")]
        [TestCase("var(--a, 1px, 2px)", "var(--a, 1px, 2px)")]
        [TestCase("calc(100% - 20px)", "calc(100% - 20px)")]
        [TestCase("calc( 1px+2px )", "calc(1px 2px)")]
        [TestCase("calc(100%/3)", "calc(100% / 3)")]
        [TestCase("translate(10px, 20px)", "translate(10px, 20px)")]
        [TestCase("cubic-bezier(0.1,0.7,1.0,0.1)", "cubic-bezier(0.1, 0.7, 1, 0.1)")]
        [TestCase("1px solid red", "1px solid red")]
        [TestCase("a, b, c", "a, b, c")]
        [TestCase("linear-gradient(to right, red 0%, blue 100%)", "linear-gradient(to right, red 0%, blue 100%)")]
        [TestCase("12px/1.5 Arial", "12px / 1.5 Arial")]
        [TestCase("  4px   8px  ", "4px 8px")]
        [TestCase("4px /* c */ 8px", "4px 8px")]
        [TestCase("rotate(45deg)", "rotate(45deg)")]
        [TestCase("-unity-font-style", "-unity-font-style")]
        [TestCase("--token", "--token")]
        [TestCase("1e999", "1e999")]
        [TestCase("calc(1px", "calc(1px)")]
        [TestCase("(1px)", "(1px)")]
        [TestCase("[a b]", "[a b]")]
        public void FormatsCanonically(string input, string expected)
        {
            Assert.That(Text(input), Is.EqualTo(expected));
        }

        [Test]
        public void EmptyValueIsRaw()
        {
            CssValue value = CssValueParser.Parse("");
            Assert.That(value, Is.TypeOf<CssRaw>());
            Assert.That(value.ToText(), Is.EqualTo(""));
        }

        [TestCase("10px", typeof(CssDimension))]
        [TestCase("10", typeof(CssNumber))]
        [TestCase("10%", typeof(CssPercent))]
        [TestCase("#fff", typeof(CssColor))]
        [TestCase("rgb(1,2,3)", typeof(CssColor))]
        [TestCase("rgba(1,2,3,0.5)", typeof(CssColor))]
        [TestCase("red", typeof(CssIdent))]
        [TestCase("\"x\"", typeof(CssString))]
        [TestCase("url(x.png)", typeof(CssUrl))]
        [TestCase("var(--x)", typeof(CssVarRef))]
        [TestCase("calc(1px + 2px)", typeof(CssFunction))]
        [TestCase("1px 2px", typeof(CssList))]
        [TestCase("a, b", typeof(CssList))]
        [TestCase("rgb(var(--r), 0, 0)", typeof(CssFunction))]
        [TestCase("hsl(10, 50%, 50%)", typeof(CssFunction))]
        public void ProducesTypedValues(string input, Type expected)
        {
            Assert.That(CssValueParser.Parse(input), Is.TypeOf(expected));
        }

        [Test]
        public void ColorComponentsAreRead()
        {
            var color = (CssColor)CssValueParser.Parse("rgba(10, 20, 30, 0.25)");
            Assert.That(color.R, Is.EqualTo(10));
            Assert.That(color.G, Is.EqualTo(20));
            Assert.That(color.B, Is.EqualTo(30));
            Assert.That(color.A, Is.EqualTo(0.25).Within(1e-9));
        }

        [TestCase("#fff", 255, 255, 255, 1.0)]
        [TestCase("#000", 0, 0, 0, 1.0)]
        [TestCase("#f008", 255, 0, 0, 0.5333)]
        [TestCase("#102030", 16, 32, 48, 1.0)]
        [TestCase("#10203040", 16, 32, 48, 0.251)]
        public void HexColorsAreDecoded(string input, int r, int g, int b, double a)
        {
            var color = (CssColor)CssValueParser.Parse(input);
            Assert.That(new[] { color.R, color.G, color.B }, Is.EqualTo(new[] { r, g, b }));
            Assert.That(color.A, Is.EqualTo(a).Within(0.001));
            Assert.That(color.Authored, Is.EqualTo(input));
        }

        [TestCase("#ff")]
        [TestCase("#fffff")]
        [TestCase("#ggg")]
        [TestCase("#")]
        public void InvalidHexIsKeptAsRaw(string input)
        {
            Assert.That(CssValueParser.Parse(input), Is.TypeOf<CssRaw>());
        }

        [TestCase("red", 255, 0, 0)]
        [TestCase("RebeccaPurple", 102, 51, 153)]
        [TestCase("transparent", 0, 0, 0)]
        [TestCase("whitesmoke", 245, 245, 245)]
        public void NamedColorsResolve(string name, int r, int g, int b)
        {
            CssColor color;
            Assert.That(ColorParser.TryParse(CssValueParser.Parse(name), out color), Is.True);
            Assert.That(new[] { color.R, color.G, color.B }, Is.EqualTo(new[] { r, g, b }));
        }

        [Test]
        public void UnknownNamesAreNotColors()
        {
            CssColor color;
            Assert.That(ColorParser.TryParse(CssValueParser.Parse("notacolor"), out color), Is.False);
            Assert.That(ColorParser.IsNamedColor("blue"), Is.True);
            Assert.That(ColorParser.IsNamedColor("flex"), Is.False);
            Assert.That(ColorParser.NamedColors.Count, Is.GreaterThanOrEqualTo(148));
        }

        [Test]
        public void VarRefsAreFoundThroughFunctionsAndLists()
        {
            CssValue value = CssValueParser.Parse("calc(var(--a) + 1px) var(--b, var(--c))");
            var refs = new List<CssVarRef>();
            CssValueParser.CollectVarRefs(value, refs);
            Assert.That(refs.ConvertAll(r => r.Name), Is.EqualTo(new[] { "--a", "--b", "--c" }));
            Assert.That(CssValueParser.HasVarRef(value), Is.True);
            Assert.That(CssValueParser.HasVarRef(CssValueParser.Parse("1px 2px")), Is.False);
        }

        [Test]
        public void VarFallbackIsPreserved()
        {
            var reference = (CssVarRef)CssValueParser.Parse("var(--gap, 8px)");
            Assert.That(reference.Name, Is.EqualTo("--gap"));
            Assert.That(reference.Fallback.ToText(), Is.EqualTo("8px"));
            Assert.That(((CssVarRef)CssValueParser.Parse("var(--gap)")).Fallback, Is.Null);
        }

        [Test]
        public void FunctionArgumentsAreSplitOnTopLevelCommas()
        {
            var function = (CssFunction)CssValueParser.Parse("drop-shadow(0 2px 4px rgba(0,0,0,.5))");
            Assert.That(function.Args.Count, Is.EqualTo(1));
            Assert.That(function.Args[0], Is.TypeOf<CssList>());
            var multi = (CssFunction)CssValueParser.Parse("cubic-bezier(0, 0, 1, 1)");
            Assert.That(multi.Args.Count, Is.EqualTo(4));
        }

        [Test]
        public void DeepNestingDoesNotOverflow()
        {
            string hostile = new string('(', 5000) + "1px" + new string(')', 5000);
            Assert.DoesNotThrow(() => CssValueParser.Parse(hostile));
            string functions = string.Concat(System.Linq.Enumerable.Repeat("f(", 5000));
            Assert.DoesNotThrow(() => CssValueParser.Parse(functions));
        }

        [Test]
        public void UnterminatedStringIsTolerated()
        {
            Assert.That(CssValueParser.Parse("\"abc").ToText(), Is.EqualTo("\"abc\""));
            Assert.That(CssValueParser.Parse("url(abc").ToText(), Is.EqualTo("url(\"abc\")"));
        }

        [TestCase(1.0, "1")]
        [TestCase(1.5, "1.5")]
        [TestCase(0.12346, "0.1235")]
        [TestCase(-0.0, "0")]
        [TestCase(100.0, "100")]
        [TestCase(0.99996, "1")]
        [TestCase(1234567.891, "1234567.891")]
        public void NumberFormatIsInvariantAndCapped(double value, string expected)
        {
            Assert.That(CssFormat.Number(value), Is.EqualTo(expected));
        }

        [Test]
        public void QuoteEscapesQuotesAndBackslashes()
        {
            Assert.That(CssFormat.Quote("a\"b\\c"), Is.EqualTo("\"a\\\"b\\\\c\""));
        }

        [Test]
        public void ColorTextPrefersAuthoredHexButCanonicalFunctions()
        {
            Assert.That(((CssColor)CssValueParser.Parse("#FfF")).ToText(), Is.EqualTo("#FfF"));
            Assert.That(((CssColor)CssValueParser.Parse("#fff")).ToCanonicalText(), Is.EqualTo("rgb(255, 255, 255)"));
            Assert.That(((CssColor)CssValueParser.Parse("rgba(1,2,3,1)")).ToText(), Is.EqualTo("rgb(1, 2, 3)"));
            Assert.That(((CssColor)CssValueParser.Parse("rgba(1,2,3,0.33333)")).ToText(), Is.EqualTo("rgba(1, 2, 3, 0.333)"));
        }

        [Test]
        public void ChannelsAreClamped()
        {
            var color = (CssColor)CssValueParser.Parse("rgba(300, -5, 10, 2)");
            Assert.That(new[] { color.R, color.G, color.B }, Is.EqualTo(new[] { 255, 0, 10 }));
            Assert.That(color.A, Is.EqualTo(1.0));
        }
    }
}
