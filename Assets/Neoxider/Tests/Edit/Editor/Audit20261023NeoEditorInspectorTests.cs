using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorInspectorTests
    {
        private sealed class HeaderProbe : ScriptableObject
        {
            [Header("Probe header")] public int Value;
        }

        private sealed class ButtonProbeEditor : CustomEditorBase
        {
            public void RequestLiveLease() => EnsureLiveRepaint();

            protected override void ProcessAttributeAssignments()
            {
            }
        }

        [Test]
        public void AddressedLeaseInspection_LeavesAForeignClientAndSharedMembershipUntouched()
        {
            HeaderProbe holder = ScriptableObject.CreateInstance<HeaderProbe>();
            ButtonProbeEditor owned = (ButtonProbeEditor)UnityEditor.Editor.CreateEditor(holder, typeof(ButtonProbeEditor));
            ButtonProbeEditor foreign = (ButtonProbeEditor)UnityEditor.Editor.CreateEditor(holder, typeof(ButtonProbeEditor));
            try
            {
                double now = EditorApplication.timeSinceStartup;
                owned.RequestLiveLease();
                foreign.RequestLiveLease();
                int clients = CustomEditorBase.RepaintClientCount;

                Assert.That(owned.HasRepaintLease(double.MaxValue), Is.False);
                Assert.That(foreign.HasRepaintLease(now), Is.True);
                Assert.That(owned.IsRepaintClient, Is.True, "Inspection must not change membership.");
                Assert.That(foreign.IsRepaintClient, Is.True);
                Assert.That(CustomEditorBase.RepaintClientCount, Is.EqualTo(clients));
            }
            finally
            {
                Object.DestroyImmediate(owned);
                Object.DestroyImmediate(foreign);
                Object.DestroyImmediate(holder);
            }
        }

        private sealed class OverloadProbe : ScriptableObject
        {
            public int Number;
            public string Text;

            [Button]
            public void Apply(int value = 12) => Number = value;

            [Button]
            public void Apply(string value = "saved") => Text = value;
        }

        private sealed class ButtonProbeWindow : EditorWindow
        {
            public ButtonProbeEditor Editor;
            public bool Enabled;
            public int Clicks;
            public Rect ButtonRect;

            private void OnGUI()
            {
                if (Editor == null)
                {
                    return;
                }

                using (new EditorGUI.DisabledScope(!Enabled))
                {
                    if (Editor.DrawGradientButton("Action", 180f, 24f))
                    {
                        Clicks++;
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    ButtonRect = GUILayoutUtility.GetLastRect();
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HeaderSuppression_PropagatesDrawerFailureOnceAndRestoresDecorators(bool exitGui)
        {
            HeaderProbe holder = ScriptableObject.CreateInstance<HeaderProbe>();
            try
            {
                using (SerializedObject serialized = new(holder))
                {
                    SerializedProperty property = serialized.FindProperty(nameof(HeaderProbe.Value));
                    float before = EditorGUI.GetPropertyHeight(property);
                    int calls = 0;
                    Exception expected = exitGui ? new ExitGUIException() : new InvalidOperationException("Probe");

                    Exception actual = Assert.Catch<Exception>(() =>
                        CustomEditorBase.RunWithHeaderDecoratorsSuppressed(property, () =>
                        {
                            calls++;
                            throw expected;
                        }));

                    Assert.That(actual, Is.SameAs(expected));
                    Assert.That(calls, Is.EqualTo(1), "The callback may mutate serialized state before failing.");
                    Assert.That(EditorGUI.GetPropertyHeight(property), Is.EqualTo(before));
                }
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void OverloadedActions_KeepIndependentParameterDefaults()
        {
            OverloadProbe holder = ScriptableObject.CreateInstance<OverloadProbe>();
            ButtonProbeEditor editor = (ButtonProbeEditor)UnityEditor.Editor.CreateEditor(holder, typeof(ButtonProbeEditor));
            try
            {
                MethodInfo number = typeof(OverloadProbe).GetMethod(nameof(OverloadProbe.Apply), new[] { typeof(int) });
                MethodInfo text = typeof(OverloadProbe).GetMethod(nameof(OverloadProbe.Apply), new[] { typeof(string) });

                editor.InvokeButtonMethod(number, number.GetParameters());
                editor.InvokeButtonMethod(text, text.GetParameters());

                Assert.That(holder.Number, Is.EqualTo(12));
                Assert.That(holder.Text, Is.EqualTo("saved"));
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void DisabledGradientButton_IgnoresClicksWhileEnabledButtonReceivesThem()
        {
            ButtonProbeEditor editor = ScriptableObject.CreateInstance<ButtonProbeEditor>();
            ButtonProbeWindow window = ScriptableObject.CreateInstance<ButtonProbeWindow>();
            int previousHotControl = GUIUtility.hotControl;
            try
            {
                window.Editor = editor;
                window.ShowUtility();
                RepaintNow(window);
                Assume.That(window.ButtonRect.width, Is.GreaterThan(0f), "Requires an IMGUI graphics context.");

                Click(window);

                Assert.That(window.Clicks, Is.Zero);
                Assert.That(GUIUtility.hotControl, Is.EqualTo(previousHotControl));

                window.Enabled = true;
                RepaintNow(window);
                Click(window);
                Assert.That(window.Clicks, Is.EqualTo(1), "The same event path must activate an enabled action.");
            }
            finally
            {
                GUIUtility.hotControl = previousHotControl;
                window.Close();
                Object.DestroyImmediate(editor);
            }
        }

        private static void Click(ButtonProbeWindow window)
        {
            Vector2 point = window.ButtonRect.center;
            window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });
        }

        private static void RepaintNow(EditorWindow window)
        {
            // Unity exposes no public synchronous Layout + Repaint entry point.
            MethodInfo repaint = typeof(EditorWindow).GetMethod("RepaintImmediately",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assume.That(repaint, Is.Not.Null);
            repaint.Invoke(window, null);
        }
    }
}
