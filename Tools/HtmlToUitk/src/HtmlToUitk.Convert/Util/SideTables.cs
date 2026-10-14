using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Emulations;

namespace HtmlToUitk.Convert
{
    public sealed class ConfigExtras
    {
        public double RootFontSize = 16;
        public int ReferenceWidth = 1920;
        public int ReferenceHeight = 1080;

        private static readonly ConditionalWeakTable<ConversionConfig, ConfigExtras> Table = new ConditionalWeakTable<ConversionConfig, ConfigExtras>();

        public static ConfigExtras For(ConversionConfig config)
        {
            ConfigExtras extras;
            if (config != null && Table.TryGetValue(config, out extras)) return extras;
            var created = new ConfigExtras();
            if (config != null)
            {
                if (config.Unity != null && config.Unity.ReferenceResolution != null && config.Unity.ReferenceResolution.Count >= 2)
                {
                    created.ReferenceWidth = config.Unity.ReferenceResolution[0];
                    created.ReferenceHeight = config.Unity.ReferenceResolution[1];
                }

                Table.Add(config, created);
            }

            return created;
        }

        public static void Set(ConversionConfig config, ConfigExtras extras)
        {
            Table.Remove(config);
            Table.Add(config, extras);
        }
    }

    public sealed class RuntimeEntry
    {
        public string Id;
        public string Uses;
        public string Selector;
        public IReadOnlyDictionary<string, string> Params;
    }

    public sealed class DocumentExtras
    {
        public string DocumentPath;
        public readonly List<string> KeyOrder = new List<string>();
        public readonly Dictionary<string, string> KeyMocks = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly List<string> NewKeys = new List<string>();
        public readonly List<RuntimeEntry> Runtime = new List<RuntimeEntry>();
        public readonly List<string> Styles = new List<string>();
        public string RootName;
        public string Name;
        public string UxmlPath;
        public string UxmlFolder;
        public string KeysPath;
        public string AnimPath;
        public string NavPath;
        public string MediaPath;
        public string RuntimePath;
        public string TokensPath;
    }

    public sealed class TransformExtras
    {
        public readonly Dictionary<string, DocumentExtras> Documents = new Dictionary<string, DocumentExtras>(StringComparer.Ordinal);
        public readonly Dictionary<string, SpecSink> SpecsByDocument = new Dictionary<string, SpecSink>(StringComparer.Ordinal);

        private static readonly ConditionalWeakTable<TransformedModel, TransformExtras> Table = new ConditionalWeakTable<TransformedModel, TransformExtras>();

        public static TransformExtras For(TransformedModel model)
        {
            TransformExtras extras;
            return Table.TryGetValue(model, out extras) ? extras : new TransformExtras();
        }

        public static void Attach(TransformedModel model, TransformExtras extras)
        {
            Table.Remove(model);
            Table.Add(model, extras);
        }

        public DocumentExtras Get(string path)
        {
            DocumentExtras doc;
            if (!Documents.TryGetValue(path, out doc))
            {
                doc = new DocumentExtras { DocumentPath = path };
                Documents[path] = doc;
            }

            return doc;
        }
    }
}
