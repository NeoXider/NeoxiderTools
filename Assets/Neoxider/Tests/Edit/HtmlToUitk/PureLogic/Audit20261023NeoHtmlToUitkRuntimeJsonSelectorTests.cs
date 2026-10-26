using System;
using NUnit.Framework;
using Neo.HtmlToUitk.Animation;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Selectors;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public sealed class Audit20261023NeoHtmlToUitkRuntimeJsonSelectorTests
    {
        private sealed class Node : ISelectNode
        {
            public string TypeName { get; set; }
            public string Name { get; set; }
            public ISelectNode Parent { get; set; }
            public string ClassName { get; set; }
            public bool HasClass(string className) => ClassName == className;
        }

        [TestCase(1e-13)]
        [TestCase(-1e-13)]
        [TestCase(1.0000000000001)]
        [TestCase(-1.0000000000001)]
        [TestCase(double.Epsilon)]
        [TestCase(double.MaxValue)]
        public void WritingFiniteNumbersPreservesTheirValue(double number)
        {
            JsonValue original = JsonValue.NewObject().Set("value", JsonValue.FromNumber(number));
            string json = MiniJson.Write(original, false);
            double restored = MiniJson.Parse(json)["value"].AsNumber();
            Assert.That(BitConverter.DoubleToInt64Bits(restored), Is.EqualTo(BitConverter.DoubleToInt64Bits(number)), json);
        }

        [TestCase("01")]
        [TestCase("-01")]
        [TestCase("1.")]
        [TestCase("-1.")]
        [TestCase("1.e2")]
        [TestCase("1e")]
        [TestCase("1e+")]
        [TestCase("1e++2")]
        [TestCase("-")]
        [TestCase("1e309")]
        [TestCase("-1e309")]
        [TestCase("[01]")]
        [TestCase("{\"value\":1.}")]
        public void InvalidJsonNumbersProduceParseDiagnostics(string json)
        {
            Assert.That(MiniJson.TryParse(json, out JsonValue value, out string error), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Does.Contain("line"));
            Assert.Throws<JsonParseException>(() => MiniJson.Parse(json));
        }

        [TestCase('\t')]
        [TestCase('\n')]
        [TestCase('\0')]
        [TestCase('\u001f')]
        public void UnescapedControlCharactersAreRejected(char control)
        {
            string json = "\"a" + control + "b\"";
            Assert.That(MiniJson.TryParse(json, out JsonValue value, out string error), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Does.Contain("Control character"));
        }

        [Test]
        public void EscapedControlsAndSignedExponentsStillRoundTrip()
        {
            const string json = "{\"text\":\"a\\tb\\u0000\",\"numbers\":[-0.125,1e+2,2E-3,0]}";
            JsonValue parsed = MiniJson.Parse(json);
            Assert.That(parsed["text"].AsString(), Is.EqualTo("a\tb\0"));
            Assert.That(parsed["numbers"][0].AsNumber(), Is.EqualTo(-0.125));
            Assert.That(parsed["numbers"][1].AsNumber(), Is.EqualTo(100));
            Assert.That(parsed["numbers"][2].AsNumber(), Is.EqualTo(0.002));
            Assert.That(MiniJson.Parse(MiniJson.Write(parsed, false))["text"].AsString(), Is.EqualTo("a\tb\0"));
        }

        [TestCase(".card >")]
        [TestCase(".card >   ")]
        [TestCase(".menu >> Button")]
        [TestCase(".menu > > Button")]
        [TestCase("Button*")]
        [TestCase(".card*")]
        [TestCase("**")]
        [TestCase("#other#card")]
        public void MalformedSelectorsNeverMatchAnElement(string text)
        {
            Node menu = new Node { TypeName = "VisualElement", Name = "menu", ClassName = "menu" };
            Node card = new Node { TypeName = "Button", Name = "card", ClassName = "card", Parent = menu };
            SelectorList selector = SelectorList.Parse(text);
            Assert.That(selector.IsValid, Is.False);
            Assert.That(selector.Error, Is.Not.Empty);
            Assert.That(selector.Matches(card), Is.False);
        }

        [TestCase(".menu>Button.card")]
        [TestCase(".menu > *.card")]
        [TestCase(".menu .card")]
        [TestCase(".absent, Button#card.card")]
        [TestCase("#card#card")]
        public void SupportedSelectorsKeepTheirMatchingBehavior(string text)
        {
            Node menu = new Node { TypeName = "VisualElement", Name = "menu", ClassName = "menu" };
            Node card = new Node { TypeName = "Button", Name = "card", ClassName = "card", Parent = menu };
            SelectorList selector = SelectorList.Parse(text);
            Assert.That(selector.IsValid, Is.True, selector.Error);
            Assert.That(selector.Matches(card), Is.True);
        }

        [TestCase("cubic-bezier(0, NaN, 1, 1)")]
        [TestCase("cubic-bezier(0, Infinity, 1, 1)")]
        [TestCase("cubic-bezier(NaN, 0, 1, 1)")]
        [TestCase("cubic-bezier(0, 0, 1, -Infinity)")]
        [TestCase("cubic-bezier(0, 1e309, 1, 1)")]
        public void NonFiniteBezierCoordinatesUseTheUnknownEasingFallback(string name)
        {
            Assert.That(Easing.IsKnown(name), Is.False);
            Assert.That(Easing.Evaluate(name, 0.25), Is.EqualTo(0.25));
        }

        [Test]
        public void FiniteBezierOvershootCoordinatesRemainSupported()
        {
            Assert.That(Easing.IsKnown("cubic-bezier(0.2, -1, 0.8, 2)"), Is.True);
        }
    }
}
