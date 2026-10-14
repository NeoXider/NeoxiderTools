using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Core.Uxml
{
    public sealed class UxmlReader : IUxmlReader
    {
        private const int MaxDepth = 256;

        public UiDocument Read(string text, string path, IDiagnosticSink sink)
        {
            var document = new UiDocument { Name = PathUtil.Stem(path ?? ""), SourcePath = path };
            var roots = new List<UxmlElement>();
            var styles = new List<StyleRef>();
            var templates = new List<TemplateRef>();
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                CloseInput = false
            };
            try
            {
                using (var stringReader = new StringReader(text ?? ""))
                using (XmlReader reader = XmlReader.Create(stringReader, settings))
                {
                    var lineInfo = (IXmlLineInfo)reader;
                    while (reader.Read() && reader.NodeType != XmlNodeType.Element) { }
                    if (reader.NodeType != XmlNodeType.Element) Fail(sink, path, "the document has no root element", 1, 1);
                    else if (reader.LocalName != "UXML") Fail(sink, path, "the root element must be UXML, found '" + reader.Name + "'", lineInfo.LineNumber, lineInfo.LinePosition);
                    else if (!reader.IsEmptyElement)
                    {
                        int rootDepth = reader.Depth;
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == rootDepth) break;
                            if (reader.NodeType != XmlNodeType.Element) continue;
                            string local = reader.LocalName;
                            if (local == "Template")
                            {
                                templates.Add(new TemplateRef { Name = reader.GetAttribute("name"), Src = reader.GetAttribute("src"), Span = SpanAt(path, lineInfo) });
                                SkipElement(reader);
                            }
                            else if (local == "Style")
                            {
                                styles.Add(new StyleRef { Src = reader.GetAttribute("src"), Span = SpanAt(path, lineInfo) });
                                SkipElement(reader);
                            }
                            else roots.Add(ReadElement(reader, null, 1));
                        }
                    }
                }
            }
            catch (XmlException ex)
            {
                Fail(sink, path, ex.Message, ex.LineNumber, ex.LinePosition);
            }
            catch (InvalidDataException ex)
            {
                Fail(sink, path, ex.Message, 1, 1);
            }
            document.Roots = roots;
            document.Styles = styles;
            document.Templates = templates;
            return document;
        }

        private static SourceSpan SpanAt(string path, IXmlLineInfo info)
        {
            var pos = new SourcePos(0, Math.Max(1, info.LineNumber), Math.Max(1, info.LinePosition));
            return new SourceSpan(path, pos, pos);
        }

        private static void SkipElement(XmlReader reader)
        {
            if (!reader.IsEmptyElement) reader.Skip();
        }

        private static void Fail(IDiagnosticSink sink, string path, string message, int line, int column)
        {
            if (sink == null) return;
            var pos = new SourcePos(0, Math.Max(1, line), Math.Max(1, column));
            sink.Report(new Diagnostic
            {
                RuleId = "U001",
                Severity = Severity.Error,
                Tier = Tier.Reject,
                Span = new SourceSpan(path, pos, pos),
                Message = "UXML syntax error: " + message,
                Why = "The UXML document is not well-formed XML.",
                Suggestion = "Fix the markup at this position."
            });
        }

        private static string TypeName(XmlReader reader)
        {
            string ns = reader.NamespaceURI;
            if (ns == "UnityEngine.UIElements" || (ns.Length == 0 && reader.Prefix.Length == 0)) return reader.LocalName;
            if (ns == "UnityEditor.UIElements") return "uie:" + reader.LocalName;
            return reader.Prefix.Length > 0 ? reader.Prefix + ":" + reader.LocalName : reader.LocalName;
        }

        private static UxmlElement ReadElement(XmlReader reader, UxmlElement parent, int depth)
        {
            if (depth > MaxDepth) throw new InvalidDataException("elements are nested deeper than " + MaxDepth);
            var element = new UxmlElement { UxmlType = TypeName(reader), Parent = parent };
            var attributes = new List<UxmlAttribute>();
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "xmlns" || reader.Prefix == "xmlns") continue;
                    attributes.Add(new UxmlAttribute { Name = reader.Name, Value = reader.Value });
                }
                reader.MoveToElement();
            }
            element.Attributes = attributes;
            var children = new List<UxmlElement>();
            if (!reader.IsEmptyElement)
            {
                int elementDepth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == elementDepth) break;
                    if (reader.NodeType == XmlNodeType.Element) children.Add(ReadElement(reader, element, depth + 1));
                }
            }
            element.Children = children;
            return element;
        }
    }
}
