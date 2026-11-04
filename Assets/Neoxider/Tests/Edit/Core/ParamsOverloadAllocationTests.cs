using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Source guard: <c>Mathf.Max</c> and <c>Mathf.Min</c> with three or more arguments bind to the
    ///     <c>params float[]</c> / <c>params int[]</c> overloads and allocate an array on every call.
    /// </summary>
    public sealed class ParamsOverloadAllocationTests
    {
        private static readonly string[] Roots = { "Assets/Neoxider/Scripts", "Assets/Neoxider/Editor" };

        [Test]
        public void PackageSources_DoNotCallParamsMathfMaxOrMin()
        {
            List<string> offenders = new();
            int scanned = 0;
            foreach (string root in Roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    scanned++;
                    string text = File.ReadAllText(file);
                    CollectParamsCalls(file, text, "Mathf.Max(", offenders);
                    CollectParamsCalls(file, text, "Mathf.Min(", offenders);
                }
            }

            Assert.That(scanned, Is.GreaterThan(0), "The package sources must be visible to this test.");
            Assert.That(offenders, Is.Empty,
                "Nest two-argument calls instead, e.g. Mathf.Max(a, Mathf.Max(b, c)).");
        }

        [TestCase("Mathf.Max(a, b)", 0)]
        [TestCase("Mathf.Max(a, Mathf.Max(b, c))", 0)]
        [TestCase("Mathf.Max(F(a, b), c)", 0)]
        [TestCase("Mathf.Min(a, b, c)", 1)]
        [TestCase("Mathf.Max(new[] { a, b })", 0)]
        public void Scanner_CountsTopLevelArguments(string code, int expected)
        {
            List<string> offenders = new();
            CollectParamsCalls("sample.cs", code, "Mathf.Max(", offenders);
            CollectParamsCalls("sample.cs", code, "Mathf.Min(", offenders);
            Assert.That(offenders, Has.Count.EqualTo(expected));
        }

        private static void CollectParamsCalls(string file, string text, string call, List<string> offenders)
        {
            int index = text.IndexOf(call, System.StringComparison.Ordinal);
            while (index >= 0)
            {
                int depth = 1;
                int arguments = 1;
                int i = index + call.Length;
                for (; i < text.Length && depth > 0; i++)
                {
                    char c = text[i];
                    if (c == '(' || c == '[' || c == '{')
                    {
                        depth++;
                    }
                    else if (c == ')' || c == ']' || c == '}')
                    {
                        depth--;
                    }
                    else if (c == ',' && depth == 1)
                    {
                        arguments++;
                    }
                }

                if (arguments >= 3)
                {
                    int line = 1;
                    for (int k = 0; k < index; k++)
                    {
                        if (text[k] == '\n')
                        {
                            line++;
                        }
                    }

                    offenders.Add(file + ":" + line + " " + text.Substring(index, i - index));
                }

                index = text.IndexOf(call, i, System.StringComparison.Ordinal);
            }
        }
    }
}
