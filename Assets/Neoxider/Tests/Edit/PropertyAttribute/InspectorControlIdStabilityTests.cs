using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     IMGUI control ids are a running counter, and Unity compares ids across events (press on MouseDown, draw
    ///     on Repaint, keyboard focus, text selection). A call that allocates an id only while repainting shifts
    ///     every control after it by one on Repaint: the button below a banner looked pressed one row up, and a
    ///     text field lost its selection highlight.
    /// </summary>
    [TestFixture]
    public class InspectorControlIdStabilityTests
    {
        private sealed class ProbeEditor : CustomEditorBase
        {
            protected override void ProcessAttributeAssignments()
            {
            }
        }

        private sealed class IdProbeWindow : EditorWindow
        {
            public UnityEditor.Editor Inspected;
            public readonly List<(EventType type, int id)> Samples = new();

            private void OnGUI()
            {
                if (Inspected == null)
                {
                    return;
                }

                EventType type = Event.current.type;
                if (type != EventType.Layout && type != EventType.Repaint)
                {
                    return;
                }

                Inspected.OnInspectorGUI();
                Samples.Add((type, GUIUtility.GetControlID(FocusType.Passive)));
            }
        }

        // WHY: only calls that never allocate a control id or a layout entry may sit in a Repaint-only branch.
        private static readonly Regex GuiCall = new(@"\b(GUI|EditorGUI|EditorGUILayout|GUILayout)\.(\w+)\s*\(",
            RegexOptions.Compiled);

        private static readonly HashSet<string> IdFreeCalls = new()
        {
            "GUI.DrawTexture", "GUI.DrawTextureWithTexCoords", "GUI.Label", "EditorGUI.DrawRect"
        };

        private static readonly Regex RepaintCondition = new(@"Event\.current\.type\s*==\s*EventType\.Repaint",
            RegexOptions.Compiled);

        [Test]
        public void RepaintOnlyBranches_DoNotAllocateControlIds()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "Neoxider"));
            Assume.That(Directory.Exists(root), "Package sources are not under Assets/Neoxider in this layout.");

            List<string> offenders = new();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.Length).Replace('\\', '/');
                bool editorCode = relative.StartsWith("/Editor/") || relative.Contains("/Editor/") ||
                                  relative.StartsWith("/Scripts/PropertyAttribute/");
                if (!editorCode || relative.StartsWith("/Samples~") || relative.StartsWith("/Tests/") ||
                    relative.StartsWith("/ThirdParty/"))
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                foreach (Match condition in RepaintCondition.Matches(text))
                {
                    string guarded = ExtractGuardedStatement(text, condition.Index + condition.Length);
                    foreach (Match call in GuiCall.Matches(guarded))
                    {
                        string name = call.Groups[1].Value + "." + call.Groups[2].Value;
                        if (!IdFreeCalls.Contains(name))
                        {
                            int line = 1 + CountNewlines(text, condition.Index);
                            offenders.Add($"{relative}:{line}: {name}(");
                        }
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "These calls allocate a control id (or layout entry) only on Repaint and shift every control " +
                "after them:\n" + string.Join("\n", offenders));
        }

        [Test]
        public void ScannerFlagsAnIdAllocatingCallInARepaintBranch()
        {
            string guarded = ExtractGuardedStatement(
                "if (Event.current.type == EventType.Repaint) { GUI.BeginGroup(rect); GUI.EndGroup(); } next();", 0);

            Assert.That(guarded, Does.Contain("GUI.BeginGroup"));
            Assert.That(guarded, Does.Not.Contain("next"));
            Assert.That(IdFreeCalls, Does.Not.Contain("GUI.BeginGroup"));
        }

        [Test]
        public void InspectorHeader_AllocatesTheSameControlIdsOnLayoutAndRepaint()
        {
            GameObject go = new("IdProbe");
            IdProbeWindow window = null;
            UnityEditor.Editor editor = null;
            try
            {
                Component target = go.AddComponent<Neo.Bonus.SpinController>();
                editor = UnityEditor.Editor.CreateEditor(target, typeof(ProbeEditor));
                window = EditorWindow.CreateInstance<IdProbeWindow>();
                window.Inspected = editor;
                window.ShowUtility();
                RepaintNow(window);
                RepaintNow(window);

                List<int> layout = new();
                List<int> repaint = new();
                foreach ((EventType type, int id) sample in window.Samples)
                {
                    (sample.type == EventType.Layout ? layout : repaint).Add(sample.id);
                }

                Assume.That(layout, Is.Not.Empty, "This run has no IMGUI (batchmode without graphics).");
                Assume.That(repaint, Is.Not.Empty, "This run has no IMGUI (batchmode without graphics).");
                Assert.That(repaint[repaint.Count - 1], Is.EqualTo(layout[layout.Count - 1]),
                    "A control after the header got a different id on Repaint than on Layout.");
            }
            finally
            {
                if (window != null)
                {
                    window.Close();
                }

                if (editor != null)
                {
                    Object.DestroyImmediate(editor);
                }

                Object.DestroyImmediate(go);
            }
        }

        private static void RepaintNow(EditorWindow window)
        {
            // WHY: RepaintImmediately is internal; it runs a full Layout + Repaint pass right now.
            typeof(EditorWindow).GetMethod("RepaintImmediately",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public)?.Invoke(window, null);
        }

        private static int CountNewlines(string text, int upTo)
        {
            int count = 0;
            for (int i = 0; i < upTo && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>The statement or brace block that follows the parenthesis closing an <c>if</c> condition.</summary>
        private static string ExtractGuardedStatement(string text, int from)
        {
            int i = text.IndexOf(')', from);
            if (i < 0)
            {
                return string.Empty;
            }

            i++;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (i >= text.Length)
            {
                return string.Empty;
            }

            int start = i;
            if (text[i] != '{')
            {
                int end = text.IndexOf(';', i);
                return end < 0 ? text.Substring(start) : text.Substring(start, end - start + 1);
            }

            int depth = 0;
            for (; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    depth++;
                }
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, i - start + 1);
                    }
                }
            }

            return text.Substring(start);
        }
    }
}
