using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorDocumentTests
    {
        [TestCase("[]")]
        [TestCase("null")]
        [TestCase("true")]
        [TestCase("42")]
        [TestCase("\"job\"")]
        public void NonObjectJobCannotReplaceAnExistingDocument(string json)
        {
            JobDocument document = JobDocument.Parse("{\"name\":\"retained\",\"inputs\":{\"html\":[\"menu.html\"]},\"output\":{\"root\":\"Generated\"}}");

            Assert.Throws<JsonParseException>(() => document = JobDocument.Parse(json));

            Assert.That(document.Name, Is.EqualTo("retained"));
            Assert.That(document.Html, Is.EqualTo(new[] { "menu.html" }));
            Assert.That(document.OutputRoot, Is.EqualTo("Generated"));
        }
    }
}
