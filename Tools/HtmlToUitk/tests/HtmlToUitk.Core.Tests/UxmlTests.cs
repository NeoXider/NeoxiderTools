using System;
using System.Collections.Generic;
using System.Linq;
using HtmlToUitk.Core.Uxml;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class UxmlTests
    {
        private static UxmlElement El(string type, params string[] attributes)
        {
            var element = new UxmlElement { UxmlType = type };
            var list = new List<UxmlAttribute>();
            for (int i = 0; i + 1 < attributes.Length; i += 2) list.Add(new UxmlAttribute { Name = attributes[i], Value = attributes[i + 1] });
            element.Attributes = list;
            return element;
        }

        private static UxmlElement With(UxmlElement parent, params UxmlElement[] children)
        {
            parent.Children = children;
            foreach (UxmlElement child in children) child.Parent = parent;
            return parent;
        }

        private static UiDocument Minimal()
        {
            UxmlElement root = With(El("VisualElement", "name", "mn-root", "class", "mn"),
                El("Label", "name", "mn-title", "text", "minimal.title", "class", "mn__title"),
                El("Button", "name", "mn-action", "text", "minimal.action", "class", "mn__action"));
            return new UiDocument { Name = "Minimal", Roots = new[] { root }, Styles = new[] { new StyleRef { Src = "Minimal.uss" } } };
        }

        private const string MinimalText =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\" editor-extension-mode=\"False\">\n" +
            "    <Style src=\"Minimal.uss\" />\n" +
            "    <ui:VisualElement name=\"mn-root\" class=\"mn\">\n" +
            "        <ui:Label name=\"mn-title\" text=\"minimal.title\" class=\"mn__title\" />\n" +
            "        <ui:Button name=\"mn-action\" text=\"minimal.action\" class=\"mn__action\" />\n" +
            "    </ui:VisualElement>\n" +
            "</ui:UXML>\n";

        [Test]
        public void WritesTheCanonicalMinimalDocument()
        {
            Assert.That(new UxmlWriter().Write(Minimal(), new UxmlWriterOptions()), Is.EqualTo(MinimalText));
        }

        [Test]
        public void WritingIsDeterministic()
        {
            string a = new UxmlWriter().Write(Minimal(), new UxmlWriterOptions());
            string b = new UxmlWriter().Write(Minimal(), new UxmlWriterOptions());
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Does.Not.Contain("\r"));
            Assert.That(a, Does.EndWith("</ui:UXML>\n"));
        }

        [Test]
        public void IndentIsConfigurable()
        {
            string text = new UxmlWriter().Write(Minimal(), new UxmlWriterOptions { Indent = 2 });
            Assert.That(text, Does.Contain("\n  <Style src=\"Minimal.uss\" />\n"));
            Assert.That(text, Does.Contain("\n    <ui:Label "));
        }

        [Test]
        public void NamespacesAndEditorModeFollowTheOptions()
        {
            string text = new UxmlWriter().Write(Minimal(), new UxmlWriterOptions { Namespaces = new[] { "ui" }, EditorExtensionMode = true, XmlDeclaration = false });
            Assert.That(text, Does.StartWith("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" editor-extension-mode=\"True\">\n"));
            Assert.That(text, Does.Not.Contain("xmlns:uie"));
            Assert.That(text, Does.Not.Contain("<?xml"));
        }

        [Test]
        public void UnknownNamespacePrefixesAreIgnored()
        {
            string text = new UxmlWriter().Write(Minimal(), new UxmlWriterOptions { Namespaces = new[] { "ui", "bogus" } });
            Assert.That(text, Does.Not.Contain("bogus"));
        }

        [Test]
        public void PrefixedTypesKeepTheirPrefix()
        {
            var document = new UiDocument { Roots = new[] { El("uie:ObjectField", "name", "o") } };
            string text = new UxmlWriter().Write(document, new UxmlWriterOptions());
            Assert.That(text, Does.Contain("<uie:ObjectField name=\"o\" />"));
        }

        [Test]
        public void TemplatesComeBeforeStyles()
        {
            var document = new UiDocument
            {
                Templates = new[] { new TemplateRef { Name = "Card", Src = "Components/Card.uxml" } },
                Styles = new[] { new StyleRef { Src = "a.uss" }, new StyleRef { Src = "b.uss" } },
                Roots = new[] { El("Instance", "template", "Card", "name", "c1") }
            };
            string text = new UxmlWriter().Write(document, new UxmlWriterOptions());
            int template = text.IndexOf("<ui:Template", StringComparison.Ordinal);
            int styleA = text.IndexOf("a.uss", StringComparison.Ordinal);
            int styleB = text.IndexOf("b.uss", StringComparison.Ordinal);
            Assert.That(template, Is.GreaterThan(0));
            Assert.That(template, Is.LessThan(styleA));
            Assert.That(styleA, Is.LessThan(styleB));
            Assert.That(text, Does.Contain("<ui:Template name=\"Card\" src=\"Components/Card.uxml\" />"));
        }

        [TestCase("plain", "plain")]
        [TestCase("a & b", "a &amp; b")]
        [TestCase("<tag>", "&lt;tag&gt;")]
        [TestCase("say \"hi\"", "say &quot;hi&quot;")]
        [TestCase("line\nbreak", "line&#10;break")]
        [TestCase("cr\rlf", "cr&#13;lf")]
        [TestCase("tab\there", "tab&#9;here")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("it's", "it's")]
        [TestCase("&amp;", "&amp;amp;")]
        public void EscapesAttributeValues(string input, string expected)
        {
            Assert.That(UxmlWriter.EscapeAttribute(input), Is.EqualTo(expected));
        }

        [Test]
        public void ReadsWhatItWrites()
        {
            UiDocument document = new UxmlReader().Read(MinimalText, "UI/Minimal.uxml", null);
            Assert.That(document.Name, Is.EqualTo("Minimal"));
            Assert.That(document.SourcePath, Is.EqualTo("UI/Minimal.uxml"));
            Assert.That(document.Styles.Select(s => s.Src), Is.EqualTo(new[] { "Minimal.uss" }));
            UxmlElement root = document.Roots.Single();
            Assert.That(root.UxmlType, Is.EqualTo("VisualElement"));
            Assert.That(root.Attributes.Select(a => a.Name + "=" + a.Value), Is.EqualTo(new[] { "name=mn-root", "class=mn" }));
            Assert.That(root.Children.Select(c => c.UxmlType), Is.EqualTo(new[] { "Label", "Button" }));
            Assert.That(ReferenceEquals(root.Children[0].Parent, root), Is.True);
            Assert.That(root.Children[1].GetAttribute("text"), Is.EqualTo("minimal.action"));
            Assert.That(new UxmlWriter().Write(document, new UxmlWriterOptions()), Is.EqualTo(MinimalText));
        }

        [Test]
        public void ReadsTemplatesAndEditorTypes()
        {
            string text = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\"><ui:Template name=\"T\" src=\"t.uxml\"/><ui:Instance template=\"T\"/><uie:ObjectField name=\"o\"/></ui:UXML>";
            UiDocument document = new UxmlReader().Read(text, "x.uxml", null);
            Assert.That(document.Templates.Single().Name, Is.EqualTo("T"));
            Assert.That(document.Templates.Single().Src, Is.EqualTo("t.uxml"));
            Assert.That(document.Roots.Select(r => r.UxmlType), Is.EqualTo(new[] { "Instance", "uie:ObjectField" }));
        }

        [Test]
        public void ReadsDefaultNamespaceDocuments()
        {
            string text = "<UXML xmlns=\"UnityEngine.UIElements\"><VisualElement name=\"r\"><Label text=\"x\"/></VisualElement></UXML>";
            UiDocument document = new UxmlReader().Read(text, "x.uxml", null);
            Assert.That(document.Roots.Single().UxmlType, Is.EqualTo("VisualElement"));
            Assert.That(document.Roots.Single().Children.Single().UxmlType, Is.EqualTo("Label"));
        }

        [Test]
        public void ReaderDecodesEntitiesInAttributes()
        {
            string text = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><ui:Label text=\"a &amp; b &quot;c&quot;\"/></ui:UXML>";
            UiDocument document = new UxmlReader().Read(text, "x.uxml", null);
            Assert.That(document.Roots.Single().GetAttribute("text"), Is.EqualTo("a & b \"c\""));
        }

        [Test]
        public void ReaderIgnoresCommentsAndText()
        {
            string text = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><!-- c --><ui:VisualElement>text<!-- d --></ui:VisualElement></ui:UXML>";
            UiDocument document = new UxmlReader().Read(text, "x.uxml", null);
            Assert.That(document.Roots.Single().Children, Is.Empty);
        }

        [TestCase("")]
        [TestCase("<ui:UXML")]
        [TestCase("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><ui:Label></ui:UXML>")]
        [TestCase("<root/>")]
        [TestCase("not xml at all")]
        [TestCase("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><ui:Label text=\"a\" text=\"b\"/></ui:UXML>")]
        [TestCase("<!DOCTYPE x [<!ENTITY a \"b\">]><ui:UXML xmlns:ui=\"UnityEngine.UIElements\"/>")]
        [TestCase("<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><undeclared:X/></ui:UXML>")]
        public void MalformedUxmlIsReportedNotThrown(string text)
        {
            var bag = new DiagnosticBag();
            UiDocument document = null;
            Assert.DoesNotThrow(() => document = new UxmlReader().Read(text, "bad.uxml", bag));
            Assert.That(document, Is.Not.Null);
            Assert.That(bag.Items.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(bag.Items.All(d => d.RuleId == "U001" && d.Severity == Severity.Error), Is.True);
            Assert.That(bag.Items[0].Span.File, Is.EqualTo("bad.uxml"));
        }

        [Test]
        public void WrongRootIsReported()
        {
            var bag = new DiagnosticBag();
            new UxmlReader().Read("<html/>", "x.uxml", bag);
            Assert.That(bag.Items.Single().Message, Does.Contain("UXML"));
        }

        [Test]
        public void DeeplyNestedUxmlIsRejectedWithoutOverflow()
        {
            string text = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\">" + string.Concat(Enumerable.Repeat("<ui:VisualElement>", 2000)) + string.Concat(Enumerable.Repeat("</ui:VisualElement>", 2000)) + "</ui:UXML>";
            var bag = new DiagnosticBag();
            Assert.DoesNotThrow(() => new UxmlReader().Read(text, "deep.uxml", bag));
            Assert.That(bag.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void ElementImplementsTheSharedElementView()
        {
            UxmlElement root = With(El("VisualElement", "name", "r", "class", "a b"), El("Label", "name", "l"));
            HtmlToUitk.Core.Html.IElement view = root;
            Assert.That(view.TypeName, Is.EqualTo("VisualElement"));
            Assert.That(view.Name, Is.EqualTo("r"));
            Assert.That(view.Classes, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(view.ChildElements.Count, Is.EqualTo(1));
            Assert.That(view.ChildElements[0].Parent, Is.SameAs(root));
            Assert.That(view.Parent, Is.Null);
        }

        [Test]
        public void StyleEngineWorksOnUxmlTrees()
        {
            UxmlElement root = With(El("VisualElement", "name", "r", "class", "a"), El("Button", "name", "b", "class", "btn"));
            var sheet = Support.Css("Button{color:red} .a .btn{margin-top:1px} #r > #b{opacity:.5}");
            var model = new HtmlToUitk.Core.Style.StyleEngine().Build(new[] { sheet }, new HtmlToUitk.Core.Style.StyleOptions(), null);
            var style = model.Resolve(root.Children[0], HtmlToUitk.Core.Style.PseudoStateSet.None);
            Assert.That(style.Properties.Keys.OrderBy(k => k), Is.EqualTo(new[] { "color", "margin-top", "opacity" }));
        }

        [Test]
        public void UxmlTypeSelectorsAreCaseSensitive()
        {
            UxmlElement root = With(El("VisualElement", "name", "r"), El("Button", "name", "b"));
            var sheet = Support.Css("Button{color:red} button{color:blue}");
            var model = new HtmlToUitk.Core.Style.StyleEngine().Build(new[] { sheet }, new HtmlToUitk.Core.Style.StyleOptions(), null);
            Assert.That(model.Resolve(root.Children[0], HtmlToUitk.Core.Style.PseudoStateSet.None).Properties["color"].Value.ToText(), Is.EqualTo("red"));
        }

        [Test]
        public void EditorInsertsAppendsAndRemoves()
        {
            UxmlElement root = With(El("VisualElement", "name", "r"), El("Label", "name", "a"), El("Label", "name", "c"));
            var editor = new UxmlElementEditor();
            UxmlElement b = El("Label", "name", "b");
            editor.InsertChild(root, 1, b);
            editor.AppendChild(root, El("Label", "name", "d"));
            Assert.That(root.Children.Select(c => c.Name), Is.EqualTo(new[] { "a", "b", "c", "d" }));
            Assert.That(ReferenceEquals(b.Parent, root), Is.True);
            editor.RemoveElement(b);
            Assert.That(root.Children.Select(c => c.Name), Is.EqualTo(new[] { "a", "c", "d" }));
            Assert.That(b.Parent, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.InsertChild(root, 9, El("X")));
        }

        [Test]
        public void EditorWrapsAndReplaces()
        {
            UxmlElement root = With(El("VisualElement", "name", "r"), El("Label", "name", "a"), El("Label", "name", "b"));
            var editor = new UxmlElementEditor();
            UxmlElement wrapper = editor.Wrap(root.Children[0], El("VisualElement", "name", "w"));
            Assert.That(root.Children.Select(c => c.Name), Is.EqualTo(new[] { "w", "b" }));
            Assert.That(wrapper.Children.Single().Name, Is.EqualTo("a"));
            Assert.That(ReferenceEquals(wrapper.Children[0].Parent, wrapper), Is.True);
            UxmlElement replacement = El("Button", "name", "z");
            editor.ReplaceElement(root.Children[1], replacement);
            Assert.That(root.Children.Select(c => c.Name), Is.EqualTo(new[] { "w", "z" }));
            Assert.That(ReferenceEquals(replacement.Parent, root), Is.True);
        }

        [Test]
        public void EditorHandlesRootsThroughTheDocument()
        {
            UxmlElement first = El("VisualElement", "name", "a");
            var document = new UiDocument { Roots = new[] { first } };
            var editor = new UxmlElementEditor(document);
            editor.Wrap(first, El("VisualElement", "name", "w"));
            Assert.That(document.Roots.Select(r => r.Name), Is.EqualTo(new[] { "w" }));
            Assert.Throws<InvalidOperationException>(() => new UxmlElementEditor().RemoveElement(El("X")));
        }

        [Test]
        public void EditorMovesChildren()
        {
            UxmlElement from = With(El("VisualElement", "name", "f"), El("Label", "name", "1"), El("Label", "name", "2"));
            UxmlElement to = With(El("ScrollView", "name", "t"), El("Label", "name", "0"));
            new UxmlElementEditor().MoveChildren(from, to);
            Assert.That(to.Children.Select(c => c.Name), Is.EqualTo(new[] { "0", "1", "2" }));
            Assert.That(from.Children, Is.Empty);
            Assert.That(to.Children.All(c => ReferenceEquals(c.Parent, to)), Is.True);
        }

        [Test]
        public void EditorManagesClassesAndAttributes()
        {
            UxmlElement element = El("Label", "name", "l", "class", "a");
            var editor = new UxmlElementEditor();
            editor.AddClass(element, "b");
            editor.AddClass(element, "b");
            Assert.That(element.GetAttribute("class"), Is.EqualTo("a b"));
            editor.RemoveClass(element, "a");
            Assert.That(element.GetAttribute("class"), Is.EqualTo("b"));
            editor.RemoveClass(element, "b");
            Assert.That(element.GetAttribute("class"), Is.Null);
            editor.RemoveClass(element, "zzz");
            editor.AddClass(element, "n");
            Assert.That(element.Attributes.Select(a => a.Name), Is.EqualTo(new[] { "name", "class" }));
            editor.SetAttribute(element, "text", "t");
            editor.SetAttribute(element, "name", "renamed");
            Assert.That(element.Attributes.Select(a => a.Name + "=" + a.Value), Is.EqualTo(new[] { "name=renamed", "class=n", "text=t" }));
            editor.SetAttribute(element, "text", null);
            Assert.That(element.GetAttribute("text"), Is.Null);
        }

        [Test]
        public void MetadataIsNeverWritten()
        {
            UxmlElement element = El("Label", "name", "l");
            element.LocKey = "k.x";
            element.MockText = "Hello";
            element.GeneratedBy = "pseudo-elements";
            string text = new UxmlWriter().Write(new UiDocument { Roots = new[] { element } }, new UxmlWriterOptions());
            Assert.That(text, Does.Not.Contain("k.x"));
            Assert.That(text, Does.Not.Contain("Hello"));
            Assert.That(text, Does.Not.Contain("pseudo"));
        }

        [Test]
        public void NullDocumentThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new UxmlWriter().Write(null, new UxmlWriterOptions()));
        }
    }
}
