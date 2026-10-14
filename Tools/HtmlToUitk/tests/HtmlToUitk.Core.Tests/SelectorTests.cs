using System;
using System.Linq;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Style;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class SelectorTests
    {
        [TestCase("*", 0, 0, 0)]
        [TestCase("div", 0, 0, 1)]
        [TestCase(".a", 0, 1, 0)]
        [TestCase("#a", 1, 0, 0)]
        [TestCase("div.a#b", 1, 1, 1)]
        [TestCase("a b c", 0, 0, 3)]
        [TestCase(".a .b > .c", 0, 3, 0)]
        [TestCase("a:hover", 0, 1, 1)]
        [TestCase("a::before", 0, 0, 2)]
        [TestCase("a:before", 0, 0, 2)]
        [TestCase("[x]", 0, 1, 0)]
        [TestCase("[x=y]", 0, 1, 0)]
        [TestCase(":not(.a)", 0, 1, 0)]
        [TestCase(":not(#a, .b)", 1, 0, 0)]
        [TestCase(":is(.a, #b)", 1, 0, 0)]
        [TestCase(":where(#a)", 0, 0, 0)]
        [TestCase(":nth-child(2n+1)", 0, 1, 0)]
        [TestCase(".a:first-child", 0, 2, 0)]
        [TestCase("div:not(.x):hover", 0, 2, 1)]
        [TestCase("#a .b .c", 1, 2, 0)]
        [TestCase("ul > li + li ~ li", 0, 0, 4)]
        [TestCase(".a.b.c", 0, 3, 0)]
        [TestCase(":root", 0, 1, 0)]
        [TestCase("button:disabled", 0, 1, 1)]
        public void ComputesSpecificity(string selector, int ids, int classes, int types)
        {
            Specificity s = SelectorParser.Parse(selector).Specificity;
            Assert.That(new[] { s.Ids, s.Classes, s.Types }, Is.EqualTo(new[] { ids, classes, types }));
        }

        [Test]
        public void SpecificityOrdersByIdsThenClassesThenTypes()
        {
            Specificity a = SelectorParser.Parse("#a").Specificity;
            Specificity b = SelectorParser.Parse(".a.b.c.d").Specificity;
            Specificity c = SelectorParser.Parse("div div div").Specificity;
            Assert.That(a.CompareTo(b), Is.GreaterThan(0));
            Assert.That(b.CompareTo(c), Is.GreaterThan(0));
            Assert.That(c.CompareTo(Specificity.Zero), Is.GreaterThan(0));
            Assert.That(b.CompareTo(b), Is.EqualTo(0));
            Assert.That(new Specificity(0, 1, 0).Equals(new Specificity(0, 1, 0)), Is.True);
            Assert.That(new Specificity(1, 2, 3).Add(new Specificity(1, 1, 1)).ToString(), Is.EqualTo("(2,3,4)"));
        }

        [TestCase("  .a  ", ".a")]
        [TestCase("A  >  b", "A > b")]
        [TestCase("a>b", "a > b")]
        [TestCase("a+b", "a + b")]
        [TestCase("a~b", "a ~ b")]
        [TestCase("a   b", "a b")]
        [TestCase("a\n\tb", "a b")]
        [TestCase("*", "*")]
        [TestCase("*.a", ".a")]
        [TestCase("div#x.a.b", "div#x.a.b")]
        [TestCase("a:HOVER", "a:hover")]
        [TestCase("a::BEFORE", "a::before")]
        [TestCase("a:after", "a::after")]
        [TestCase("[ type = 'text' ]", "[type=\"text\"]")]
        [TestCase("[data-x=y i]", "[data-x=\"y\" i]")]
        [TestCase("[a~=b]", "[a~=\"b\"]")]
        [TestCase("[a|=b]", "[a|=\"b\"]")]
        [TestCase("[a^=b]", "[a^=\"b\"]")]
        [TestCase("[a$=b]", "[a$=\"b\"]")]
        [TestCase("[a*=b]", "[a*=\"b\"]")]
        [TestCase(":nth-child( 2N + 1 )", ":nth-child(odd)")]
        [TestCase(":nth-child(ODD)", ":nth-child(odd)")]
        [TestCase(":nth-child(3n+1)", ":nth-child(3n+1)")]
        [TestCase(":nth-child(even)", ":nth-child(even)")]
        [TestCase(":nth-child(-n+3)", ":nth-child(-n+3)")]
        [TestCase(":nth-child(3)", ":nth-child(3)")]
        [TestCase(":nth-of-type(n)", ":nth-of-type(n)")]
        [TestCase(":not(.a,.b)", ":not(.a, .b)")]
        [TestCase(":is(a b)", ":is(a b)")]
        [TestCase(":has(> a)", ":has(> a)")]
        [TestCase(".md\\:flex", ".md\\:flex")]
        [TestCase("#a\\.b", "#a\\.b")]
        public void FormatsSelectorsCanonically(string input, string expected)
        {
            Assert.That(SelectorParser.Parse(input).Text, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(">")]
        [TestCase("a >")]
        [TestCase("> a")]
        [TestCase("a > > b")]
        [TestCase("a >>> b")]
        [TestCase("..a")]
        [TestCase(".")]
        [TestCase("#")]
        [TestCase("a$")]
        [TestCase("a&b")]
        [TestCase("[a")]
        [TestCase("[]")]
        [TestCase("[a=]")]
        [TestCase("[a!=b]")]
        [TestCase(":")]
        [TestCase("::")]
        [TestCase(":nth-child(x)")]
        [TestCase(":nth-child()")]
        [TestCase(":not(")]
        [TestCase(":not()")]
        [TestCase(":not(..)")]
        [TestCase("a*")]
        [TestCase(".a*")]
        public void RejectsInvalidSelectors(string input)
        {
            var bag = new DiagnosticBag();
            var parsed = SelectorParser.ParseList(input, bag);
            Assert.That(parsed.Count, Is.EqualTo(0), input);
            Assert.That(bag.Count(Severity.Error), Is.GreaterThanOrEqualTo(1), input);
        }

        [Test]
        public void ListsSplitOnTopLevelCommasOnly()
        {
            var list = SelectorParser.ParseList(":is(a, b) c, [x=\"1,2\"], d", null);
            Assert.That(list.Select(s => s.Text), Is.EqualTo(new[] { ":is(a, b) c", "[x=\"1,2\"]", "d" }));
        }

        [Test]
        public void PartsCarryCombinators()
        {
            Selector selector = SelectorParser.Parse("a > b c + d ~ e");
            Assert.That(selector.Parts.Select(p => p.Combinator), Is.EqualTo(new[] { '\0', '>', ' ', '+', '~' }));
            Assert.That(selector.Parts.Select(p => p.Part.Type), Is.EqualTo(new[] { "a", "b", "c", "d", "e" }));
        }

        [Test]
        public void CompoundPartsAreStructured()
        {
            SimpleSelector part = SelectorParser.Parse("div#main.a.b[x=y]:hover::before").Parts[0].Part;
            Assert.That(part.Type, Is.EqualTo("div"));
            Assert.That(part.Id, Is.EqualTo("main"));
            Assert.That(part.Classes, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(part.Attributes.Single().Name, Is.EqualTo("x"));
            Assert.That(part.Attributes.Single().Value, Is.EqualTo("y"));
            Assert.That(part.Pseudos.Single().Name, Is.EqualTo("hover"));
            Assert.That(part.PseudoElements, Is.EqualTo(new[] { "before" }));
        }

        [Test]
        public void NthExpressionsAreDecoded()
        {
            PseudoClass odd = SelectorParser.Parse(":nth-child(odd)").Parts[0].Part.Pseudos[0];
            Assert.That(odd.HasNth && odd.NthA == 2 && odd.NthB == 1, Is.True);
            PseudoClass minus = SelectorParser.Parse(":nth-child(-n+3)").Parts[0].Part.Pseudos[0];
            Assert.That(minus.NthA == -1 && minus.NthB == 3, Is.True);
            PseudoClass plain = SelectorParser.Parse(":nth-last-child(4)").Parts[0].Part.Pseudos[0];
            Assert.That(plain.NthA == 0 && plain.NthB == 4, Is.True);
        }

        [TestCase("2n+1", true, 2, 1)]
        [TestCase("-n+3", true, -1, 3)]
        [TestCase("+n", true, 1, 0)]
        [TestCase("n", true, 1, 0)]
        [TestCase("3", true, 0, 3)]
        [TestCase("-3", true, 0, -3)]
        [TestCase("2n", true, 2, 0)]
        [TestCase("2n-1", true, 2, -1)]
        [TestCase("4n + 2", true, 4, 2)]
        [TestCase("odd of .x", true, 2, 1)]
        [TestCase("", false, 0, 0)]
        [TestCase("n+", false, 0, 0)]
        [TestCase("2n+x", false, 0, 0)]
        [TestCase("abc", false, 0, 0)]
        public void TryParseNthTable(string text, bool ok, int a, int b)
        {
            int ra, rb;
            Assert.That(SelectorParser.TryParseNth(text, out ra, out rb), Is.EqualTo(ok));
            if (ok) Assert.That(new[] { ra, rb }, Is.EqualTo(new[] { a, b }));
        }

        [TestCase(1, 0, 3, true)]
        [TestCase(2, 1, 1, true)]
        [TestCase(2, 1, 2, false)]
        [TestCase(2, 1, 3, true)]
        [TestCase(2, 0, 4, true)]
        [TestCase(2, 0, 0, true)]
        [TestCase(-1, 3, 1, true)]
        [TestCase(-1, 3, 3, true)]
        [TestCase(-1, 3, 4, false)]
        [TestCase(0, 2, 2, true)]
        [TestCase(0, 2, 3, false)]
        [TestCase(3, 1, 7, true)]
        [TestCase(3, 1, 6, false)]
        public void NthMatchesTable(int a, int b, int position, bool expected)
        {
            Assert.That(SelectorMatcher.NthMatches(a, b, position), Is.EqualTo(expected));
        }

        private const string Page =
            "<div name='root' class='page'>" +
            "<div name='head' class='bar top'><span name='t1' class='label'>x</span><button name='b1' class='btn primary' disabled>y</button></div>" +
            "<ul name='list'><li name='i1' class='item'>1</li><li name='i2' class='item sel'>2</li><li name='i3' class='item'>3</li><li name='i4' class='item'>4</li></ul>" +
            "<p name='para'></p><input name='in1' type='text'><input name='in2' type='checkbox' checked>" +
            "</div>";

        [TestCase(".btn", "b1")]
        [TestCase("button", "b1")]
        [TestCase("BUTTON", "b1")]
        [TestCase(".btn.primary", "b1")]
        [TestCase(".primary.btn", "b1")]
        [TestCase(".btn.missing", "")]
        [TestCase("#b1", "b1")]
        [TestCase("button#b1.btn", "b1")]
        [TestCase("#nope", "")]
        [TestCase(".page .label", "t1")]
        [TestCase(".page > .label", "")]
        [TestCase(".bar > .label", "t1")]
        [TestCase(".bar > *", "t1 b1")]
        [TestCase("ul li", "i1 i2 i3 i4")]
        [TestCase("ul > li", "i1 i2 i3 i4")]
        [TestCase("div li", "i1 i2 i3 i4")]
        [TestCase("li.sel + li", "i3")]
        [TestCase("li.sel ~ li", "i3 i4")]
        [TestCase("li + li", "i2 i3 i4")]
        [TestCase("li ~ li.item", "i2 i3 i4")]
        [TestCase("li:first-child", "i1")]
        [TestCase("li:last-child", "i4")]
        [TestCase("li:nth-child(2)", "i2")]
        [TestCase("li:nth-child(odd)", "i1 i3")]
        [TestCase("li:nth-child(even)", "i2 i4")]
        [TestCase("li:nth-child(2n+1)", "i1 i3")]
        [TestCase("li:nth-child(-n+2)", "i1 i2")]
        [TestCase("li:nth-last-child(1)", "i4")]
        [TestCase("li:nth-last-child(2)", "i3")]
        [TestCase("li:nth-of-type(3)", "i3")]
        [TestCase("li:nth-last-of-type(1)", "i4")]
        [TestCase("li:first-of-type", "i1")]
        [TestCase("li:last-of-type", "i4")]
        [TestCase("li:only-child", "")]
        [TestCase("span:only-child", "")]
        [TestCase("li:not(.sel)", "i1 i3 i4")]
        [TestCase("li:not(.sel):not(:first-child)", "i3 i4")]
        [TestCase("li:not(.sel, :last-child)", "i1 i3")]
        [TestCase("li:is(.sel, :last-child)", "i2 i4")]
        [TestCase("li:where(.sel)", "i2")]
        [TestCase("div:has(> button)", "head")]
        [TestCase("div:has(button)", "root head")]
        [TestCase("li:has(+ li.sel)", "i1")]
        [TestCase("li:has(~ li.sel)", "i1")]
        [TestCase("p:empty", "para")]
        [TestCase("ul:empty", "")]
        [TestCase("[type]", "in1 in2")]
        [TestCase("[type=text]", "in1")]
        [TestCase("[type='text']", "in1")]
        [TestCase("[type=TEXT]", "")]
        [TestCase("[type=TEXT i]", "in1")]
        [TestCase("[class~=btn]", "b1")]
        [TestCase("[class~=bt]", "")]
        [TestCase("[class|=bar]", "")]
        [TestCase("[class^=bar]", "head")]
        [TestCase("[class$=top]", "head")]
        [TestCase("[class*=rim]", "b1")]
        [TestCase("[type^='']", "")]
        [TestCase("input:checked", "in2")]
        [TestCase("button:disabled", "b1")]
        [TestCase("button:enabled", "")]
        [TestCase("input:enabled", "in1 in2")]
        [TestCase("input:disabled", "")]
        [TestCase(":root", "")]
        [TestCase("*", "root head t1 b1 list i1 i2 i3 i4 para in1 in2")]
        [TestCase(".item:hover", "")]
        [TestCase("li:unknown-pseudo", "")]
        [TestCase("li::before", "i1 i2 i3 i4")]
        public void MatchesElements(string selector, string expected)
        {
            Assert.That(Support.Query(Page, selector), Is.EqualTo(expected));
        }

        [TestCase(".item:hover", PseudoStateSet.Hover, "i1 i2 i3 i4")]
        [TestCase(".item:hover", PseudoStateSet.Active, "")]
        [TestCase(".item:active", PseudoStateSet.Active, "i1 i2 i3 i4")]
        [TestCase(".item:inactive", PseudoStateSet.None, "i1 i2 i3 i4")]
        [TestCase(".item:inactive", PseudoStateSet.Active, "")]
        [TestCase("input:focus", PseudoStateSet.Focus, "in1 in2")]
        [TestCase("input:checked", PseudoStateSet.Checked, "in1 in2")]
        [TestCase("li:disabled", PseudoStateSet.Disabled, "i1 i2 i3 i4")]
        [TestCase("li:enabled", PseudoStateSet.Disabled, "")]
        [TestCase(".bar:hover > .label", PseudoStateSet.Hover, "t1")]
        [TestCase(".bar:hover .label", PseudoStateSet.Hover, "t1")]
        [TestCase("div:hover li", PseudoStateSet.Hover, "i1 i2 i3 i4")]
        [TestCase("div:hover li", PseudoStateSet.None, "")]
        [TestCase("li:hover", PseudoStateSet.Root, "")]
        [TestCase("p:root", PseudoStateSet.Root, "para")]
        public void DynamicStatesApplyToTheSubjectAndPropagateUp(string selector, PseudoStateSet states, string expected)
        {
            Assert.That(Support.Query(Page, selector, states), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidSelectorQueryIsFlagged()
        {
            Assert.That(Support.Query(Page, ">>"), Is.EqualTo("<invalid>"));
        }

        [Test]
        public void MatchesRelativeNeedsAnAnchor()
        {
            Selector selector = SelectorParser.Parse(":has(a)").Parts[0].Part.Pseudos[0].Selectors[0];
            Assert.That(SelectorMatcher.MatchesRelative(selector, null), Is.False);
            Assert.That(SelectorMatcher.Matches(null, null), Is.False);
        }

        [Test]
        public void ConflictKeyIgnoresClassOrderAndSpacing()
        {
            Assert.That(StyleModel.ConflictKey(SelectorParser.Parse(".a.b")), Is.EqualTo(StyleModel.ConflictKey(SelectorParser.Parse(".b.a"))));
            Assert.That(StyleModel.ConflictKey(SelectorParser.Parse("a>b")), Is.EqualTo(StyleModel.ConflictKey(SelectorParser.Parse("a  >  b"))));
            Assert.That(StyleModel.ConflictKey(SelectorParser.Parse(".a")), Is.Not.EqualTo(StyleModel.ConflictKey(SelectorParser.Parse("#a"))));
        }
    }
}
