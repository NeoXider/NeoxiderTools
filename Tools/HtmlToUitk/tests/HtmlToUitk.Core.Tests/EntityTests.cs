using System;
using HtmlToUitk.Core.Html;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class EntityTests
    {
        [TestCase("&amp;", "&")]
        [TestCase("&lt;", "<")]
        [TestCase("&gt;", ">")]
        [TestCase("&quot;", "\"")]
        [TestCase("&apos;", "'")]
        [TestCase("&nbsp;", " ")]
        [TestCase("&copy;", "©")]
        [TestCase("&reg;", "®")]
        [TestCase("&euro;", "€")]
        [TestCase("&hellip;", "…")]
        [TestCase("&mdash;", "—")]
        [TestCase("&rarr;", "→")]
        [TestCase("&alpha;", "α")]
        [TestCase("&Omega;", "Ω")]
        [TestCase("&eacute;", "é")]
        [TestCase("&Uuml;", "Ü")]
        [TestCase("&yuml;", "ÿ")]
        [TestCase("&hearts;", "♥")]
        [TestCase("&check;", "✓")]
        [TestCase("&#65;", "A")]
        [TestCase("&#x41;", "A")]
        [TestCase("&#X41;", "A")]
        [TestCase("&#128512;", "😀")]
        [TestCase("&#x1F600;", "😀")]
        [TestCase("&#65", "A")]
        [TestCase("&#0;", "�")]
        [TestCase("&#xD800;", "�")]
        [TestCase("&#x110000;", "�")]
        [TestCase("&#99999999999999999999;", "�")]
        [TestCase("&#128;", "€")]
        [TestCase("&#x9F;", "Ÿ")]
        [TestCase("&#129;", "\u0081")]
        [TestCase("&amp", "&")]
        [TestCase("&ampx", "&x")]
        [TestCase("&copy2024", "©2024")]
        [TestCase("&lt;b&gt;", "<b>")]
        [TestCase("a&amp;b&amp;c", "a&b&c")]
        [TestCase("&", "&")]
        [TestCase("&;", "&;")]
        [TestCase("&#;", "&#;")]
        [TestCase("&#x;", "&#x;")]
        [TestCase("&notanentity;", "¬anentity;")]
        [TestCase("&zzzz;", "&zzzz;")]
        [TestCase("&AMP;", "&")]
        [TestCase("&Amp;", "&Amp;")]
        [TestCase("& amp;", "& amp;")]
        [TestCase("plain", "plain")]
        [TestCase("", "")]
        [TestCase("&&amp;", "&&")]
        [TestCase("&#38;amp;", "&amp;")]
        public void DecodesText(string input, string expected)
        {
            Assert.That(HtmlEntities.Decode(input), Is.EqualTo(expected));
        }

        [TestCase("&amp=1", "&amp=1")]
        [TestCase("&ampx", "&ampx")]
        [TestCase("&amp;x", "&x")]
        [TestCase("a&copy", "a©")]
        [TestCase("&copy=", "&copy=")]
        [TestCase("&lt", "<")]
        [TestCase("?a=1&b=2&c", "?a=1&b=2&c")]
        [TestCase("?a=1&copy=2", "?a=1&copy=2")]
        [TestCase("&notin;", "∉")]
        public void AttributesAreMoreConservative(string input, string expected)
        {
            Assert.That(HtmlEntities.Decode(input, true), Is.EqualTo(expected));
        }

        [Test]
        public void NullDecodesToEmpty()
        {
            Assert.That(HtmlEntities.Decode(null), Is.EqualTo(""));
        }

        [Test]
        public void KnownNamesAreQueryable()
        {
            Assert.That(HtmlEntities.IsKnown("amp"), Is.True);
            Assert.That(HtmlEntities.IsKnown("nope"), Is.False);
            Assert.That(HtmlEntities.IsKnown(null), Is.False);
        }
    }
}
