using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class NeoInspectorAnimationTests
    {
        private sealed class Probe : ScriptableObject
        {
            [Header("Group")] public int Grouped;

            public int Loose;

            [Button("Explicit", 88f)]
            private void FirstButton()
            {
            }

            [Button]
            public static void SecondButton()
            {
            }

            public void OrdinaryMethod()
            {
            }
        }

        private sealed class ProbeEditor : CustomEditorBase
        {
            public void RequestAnimation()
            {
                EnsureRepaint();
            }

            public void RequestLive()
            {
                EnsureLiveRepaint();
            }

            public (bool found, string name, float width, bool playModeOnly) Describe(MethodInfo method)
            {
                ButtonInfo? info = FindButtonAttribute(method);
                return info.HasValue
                    ? (true, info.Value.ButtonName, info.Value.Width, info.Value.PlayModeOnly)
                    : (false, null, 0f, false);
            }

            protected override void ProcessAttributeAssignments()
            {
            }
        }

        private bool _savedEnableAnimations;
        private bool _savedAnimateInPlayMode;
        private int _savedAnimationFps;
        private Probe _probe;
        private ProbeEditor _editor;
        private int _baselineClients;

        [SetUp]
        public void SetUp()
        {
            _savedEnableAnimations = CustomEditorSettings.EnableAnimations;
            _savedAnimateInPlayMode = CustomEditorSettings.AnimateInPlayMode;
            _savedAnimationFps = CustomEditorSettings.AnimationFps;

            _probe = ScriptableObject.CreateInstance<Probe>();
            _editor = (ProbeEditor)UnityEditor.Editor.CreateEditor(_probe, typeof(ProbeEditor));
            _baselineClients = CustomEditorBase.RepaintClientCount;
        }

        [TearDown]
        public void TearDown()
        {
            if (_editor != null)
            {
                Object.DestroyImmediate(_editor);
            }

            if (_probe != null)
            {
                Object.DestroyImmediate(_probe);
            }

            CustomEditorSettings.SetEnableAnimations(_savedEnableAnimations);
            CustomEditorSettings.SetAnimateInPlayMode(_savedAnimateInPlayMode);
            CustomEditorSettings.SetAnimationFps(_savedAnimationFps);
        }

        [Test]
        public void ShouldAnimate_MasterSwitchOff_NeverAnimates()
        {
            Assert.That(NeoInspectorAnimation.ShouldAnimate(false, false, false), Is.False);
            Assert.That(NeoInspectorAnimation.ShouldAnimate(false, true, false), Is.False);
            Assert.That(NeoInspectorAnimation.ShouldAnimate(false, false, true), Is.False);
            Assert.That(NeoInspectorAnimation.ShouldAnimate(false, true, true), Is.False);
        }

        [Test]
        public void ShouldAnimate_PlayMode_RunsOnlyWhenOptedIn()
        {
            Assert.That(NeoInspectorAnimation.ShouldAnimate(true, false, true), Is.False);
            Assert.That(NeoInspectorAnimation.ShouldAnimate(true, true, true), Is.True);
        }

        [Test]
        public void ShouldAnimate_EditMode_FollowsMasterSwitch()
        {
            Assert.That(NeoInspectorAnimation.ShouldAnimate(true, false, false), Is.True);
            Assert.That(NeoInspectorAnimation.ShouldAnimate(true, true, false), Is.True);
        }

        [Test]
        public void ClampFps_KeepsValueInsideSupportedRange()
        {
            Assert.That(NeoInspectorAnimation.ClampFps(0), Is.EqualTo(NeoInspectorAnimation.MinFps));
            Assert.That(NeoInspectorAnimation.ClampFps(-30), Is.EqualTo(NeoInspectorAnimation.MinFps));
            Assert.That(NeoInspectorAnimation.ClampFps(1000), Is.EqualTo(NeoInspectorAnimation.MaxFps));
            Assert.That(NeoInspectorAnimation.ClampFps(30), Is.EqualTo(30));
        }

        [Test]
        public void IsRepaintDue_HonoursTheFpsInterval()
        {
            Assert.That(NeoInspectorAnimation.IsRepaintDue(10.01, 10.0, 30), Is.False);
            Assert.That(NeoInspectorAnimation.IsRepaintDue(10.04, 10.0, 30), Is.True);
            Assert.That(NeoInspectorAnimation.IsRepaintDue(10.04, 10.0, 10), Is.False);
            Assert.That(NeoInspectorAnimation.IsRepaintDue(10.11, 10.0, 10), Is.True);
        }

        [Test]
        public void RepaintInterval_ClampsExtremeFps()
        {
            Assert.That(NeoInspectorAnimation.RepaintInterval(100000),
                Is.EqualTo(1.0 / NeoInspectorAnimation.MaxFps).Within(1e-9));
            Assert.That(NeoInspectorAnimation.RepaintInterval(0),
                Is.EqualTo(1.0 / NeoInspectorAnimation.MinFps).Within(1e-9));
        }

        [Test]
        public void Settings_AnimationFps_IsClampedOnWrite()
        {
            CustomEditorSettings.SetAnimationFps(100000);
            Assert.That(CustomEditorSettings.AnimationFps, Is.EqualTo(NeoInspectorAnimation.MaxFps));

            CustomEditorSettings.SetAnimationFps(0);
            Assert.That(CustomEditorSettings.AnimationFps, Is.EqualTo(NeoInspectorAnimation.MinFps));
        }

        [Test]
        public void IsActive_ReflectsTheMasterSwitchInEditMode()
        {
            Assume.That(EditorApplication.isPlaying, Is.False);

            CustomEditorSettings.SetEnableAnimations(true);
            Assert.That(NeoInspectorAnimation.IsActive(), Is.True);

            CustomEditorSettings.SetEnableAnimations(false);
            Assert.That(NeoInspectorAnimation.IsActive(), Is.False);
        }

        [Test]
        public void EnsureRepaint_WhileAnimationIsOff_DoesNotJoinTheTicker()
        {
            CustomEditorSettings.SetEnableAnimations(false);

            _editor.RequestAnimation();

            Assert.That(_editor.IsRepaintClient, Is.False);
            Assert.That(CustomEditorBase.RepaintClientCount, Is.EqualTo(_baselineClients));
        }

        [Test]
        public void EnsureRepaint_WhileAnimationIsOn_JoinsUntilTheLeaseLapses()
        {
            Assume.That(EditorApplication.isPlaying, Is.False);
            CustomEditorSettings.SetEnableAnimations(true);

            _editor.RequestAnimation();
            Assert.That(_editor.IsRepaintClient, Is.True);
            Assert.That(CustomEditorBase.RepaintClientCount, Is.EqualTo(_baselineClients + 1));

            CustomEditorBase.TickRepaintClients(EditorApplication.timeSinceStartup + 60.0);

            Assert.That(_editor.IsRepaintClient, Is.False, "A lapsed lease must drop the editor from the ticker.");
            Assert.That(CustomEditorBase.RepaintClientCount, Is.EqualTo(_baselineClients));
        }

        [Test]
        public void EnsureLiveRepaint_StillJoinsWhileAnimationIsOff()
        {
            CustomEditorSettings.SetEnableAnimations(false);

            _editor.RequestLive();

            Assert.That(_editor.IsRepaintClient, Is.True,
                "Live content (update check, condition result) must keep refreshing without animation.");
        }

        [Test]
        public void DisposedEditor_LeavesTheTicker()
        {
            CustomEditorSettings.SetEnableAnimations(false);
            _editor.RequestLive();
            Assert.That(CustomEditorBase.RepaintClientCount, Is.EqualTo(_baselineClients + 1));

            Object.DestroyImmediate(_editor);
            _editor = null;

            Assert.That(CustomEditorBase.RepaintClientCount, Is.EqualTo(_baselineClients));
        }

        [Test]
        public void GetButtonMethods_FindsOnlyButtonMethods_AndCachesTheResult()
        {
            MethodInfo[] first = _editor.GetButtonMethods();
            MethodInfo[] second = _editor.GetButtonMethods();

            Assert.That(first, Has.Length.EqualTo(2));
            Assert.That(first, Has.Some.Property(nameof(MemberInfo.Name)).EqualTo("FirstButton"));
            Assert.That(first, Has.Some.Property(nameof(MemberInfo.Name)).EqualTo("SecondButton"));
            Assert.That(first, Has.None.Property(nameof(MemberInfo.Name)).EqualTo("OrdinaryMethod"));
            Assert.That(second, Is.SameAs(first), "Reflection must run once per type, not once per IMGUI event.");
        }

        [Test]
        public void FindButtonAttribute_ReturnsTheAttributeData_AndIsStableAcrossCalls()
        {
            MethodInfo method = typeof(Probe).GetMethod("FirstButton",
                BindingFlags.Instance | BindingFlags.NonPublic);

            (bool found, string name, float width, bool playModeOnly) a = _editor.Describe(method);
            (bool found, string name, float width, bool playModeOnly) b = _editor.Describe(method);

            Assert.That(a.found, Is.True);
            Assert.That(a.name, Is.EqualTo("Explicit"));
            Assert.That(a.width, Is.EqualTo(88f));
            Assert.That(b, Is.EqualTo(a));

            MethodInfo plain = typeof(Probe).GetMethod(nameof(Probe.OrdinaryMethod));
            Assert.That(_editor.Describe(plain).found, Is.False);
            Assert.That(_editor.Describe(plain).found, Is.False);
        }

        [Test]
        public void TryGetHeaderTitleForProperty_IsCachedAndCorrect()
        {
            SerializedObject serialized = new(_probe);
            SerializedProperty grouped = serialized.FindProperty(nameof(Probe.Grouped));
            SerializedProperty loose = serialized.FindProperty(nameof(Probe.Loose));

            Assert.That(_editor.TryGetHeaderTitleForProperty(grouped), Is.EqualTo("Group"));
            Assert.That(_editor.TryGetHeaderTitleForProperty(grouped), Is.EqualTo("Group"));
            Assert.That(_editor.TryGetHeaderTitleForProperty(loose), Is.Null);
            Assert.That(_editor.TryGetHeaderTitleForProperty(loose), Is.Null);
        }

        [TestCase("Neo", true)]
        [TestCase("Neo.Tools", true)]
        [TestCase("Neo.Editor.Tests", true)]
        [TestCase("Neon", false)]
        [TestCase("NeoStuff.Tools", false)]
        [TestCase("My.Neo", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsNeoNamespace_MatchesTheNeoRootOnly(string typeNamespace, bool expected)
        {
            Assert.That(CustomEditorBase.IsNeoNamespace(typeNamespace), Is.EqualTo(expected));
        }

        [Test]
        public void DocRichTextPreview_IsNullForAMissingDoc_AndSurvivesCacheClears()
        {
            Assert.That(NeoDocHelper.GetDocRichTextPreview("Assets/DoesNotExist/Missing.md", 5), Is.Null);
            Assert.That(NeoDocHelper.GetDocRichTextPreview("Assets/DoesNotExist/Missing.md", 5), Is.Null);

            NeoDocHelper.ClearCaches();

            Assert.That(NeoDocHelper.GetDocRichTextPreview("Assets/DoesNotExist/Missing.md", 5), Is.Null);
        }
    }
}
