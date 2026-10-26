using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.NoCode;
using Neo.Reactive;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{


    public sealed class Audit20261023NeoNoCodeFollowupTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase(false, NoCodeFloatUpdateMode.Reactive)]
        [TestCase(true, NoCodeFloatUpdateMode.Reactive)]
        [TestCase(false, NoCodeFloatUpdateMode.Poll)]
        [TestCase(true, NoCodeFloatUpdateMode.Poll)]
        public void Validation_OrdinarySource_ContinuesPolling(bool formatted, NoCodeFloatUpdateMode mode)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            MonoBehaviour view = CreateTextView(source, formatted, mode,
                nameof(Audit20261023NeoNoCodeFollowupSource.Score), true, out TMP_Text text);
            Assert.AreEqual("0.25", text.text);
            int notifications = 0;
            if (view is NoCodeFormattedText formattedView)
            {
                formattedView.OnTextChanged.AddListener(_ => notifications++);
            }

            using SerializedObject serialized = new(view);
            serialized.FindProperty("_pollIntervalSeconds").floatValue = 0.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeValidation(view);

            PollDue(view);
            PollDue(view);

            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications, "Changing only the interval must not publish the same string again.");
            source.Score = 0.75f;
            PollDue(view);

            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
            PollDue(view);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Validation_DoesNotReadSourceGetter_UntilMainThreadPoll(bool formatted)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            MonoBehaviour view = CreateTextView(source, formatted, NoCodeFloatUpdateMode.Reactive,
                nameof(Audit20261023NeoNoCodeFollowupSource.CountedScore), true, out TMP_Text text);
            int before = source.ReadCount;
            int notifications = 0;
            if (view is NoCodeFormattedText formattedView)
            {
                formattedView.OnTextChanged.AddListener(_ => notifications++);
            }

            source.Score = 0.75f;

            InvokeValidation(view);

            Assert.AreEqual(before, source.ReadCount, "OnValidate must not invoke a bound property getter.");
            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications);
            PollDue(view);
            Assert.Greater(source.ReadCount, before);
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
            PollDue(view);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Validation_Once_DoesNotReadOrRepaintOnLateUpdate(bool formatted)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            MonoBehaviour view = CreateTextView(source, formatted, NoCodeFloatUpdateMode.Once,
                nameof(Audit20261023NeoNoCodeFollowupSource.CountedScore), true, out TMP_Text text);
            int before = source.ReadCount;
            int notifications = 0;
            if (view is NoCodeFormattedText formattedView)
            {
                formattedView.OnTextChanged.AddListener(_ => notifications++);
            }

            source.Score = 0.75f;

            InvokeValidation(view);
            PollDue(view);
            PollDue(view);

            Assert.AreEqual(before, source.ReadCount);
            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications);
            Refresh(view);
            Assert.Greater(source.ReadCount, before);
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
            Refresh(view);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Validation_PollingDisabled_RequiresExplicitRefresh(bool formatted)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            MonoBehaviour view = CreateTextView(source, formatted, NoCodeFloatUpdateMode.Reactive,
                nameof(Audit20261023NeoNoCodeFollowupSource.Score), false, out TMP_Text text);
            int notifications = 0;
            if (view is NoCodeFormattedText formattedView)
            {
                formattedView.OnTextChanged.AddListener(_ => notifications++);
            }

            source.Score = 0.75f;

            InvokeValidation(view);
            PollDue(view);

            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications);
            Refresh(view);
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
            Refresh(view);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Validation_ReactiveToPlainSource_ReconcilesBeforePollGate(bool formatted)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            MonoBehaviour view = CreateTextView(source, formatted, NoCodeFloatUpdateMode.Reactive,
                nameof(Audit20261023NeoNoCodeFollowupSource.ReactiveScore), true, out TMP_Text text);
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);
            int notifications = 0;
            if (view is NoCodeFormattedText formattedView)
            {
                formattedView.OnTextChanged.AddListener(_ => notifications++);
            }

            Configure(view, source, nameof(Audit20261023NeoNoCodeFollowupSource.Score),
                NoCodeFloatUpdateMode.Reactive, true);

            PollDue(view);

            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications, "Changing the source without changing its output must not notify.");
            source.Score = 0.75f;
            PollDue(view);
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
            PollDue(view);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Validation_ChangeModeToOnce_UnsubscribesWithoutRepainting(bool formatted)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            MonoBehaviour view = CreateTextView(source, formatted, NoCodeFloatUpdateMode.Reactive,
                nameof(Audit20261023NeoNoCodeFollowupSource.ReactiveScore), true, out TMP_Text text);
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);
            int notifications = 0;
            if (view is NoCodeFormattedText formattedView)
            {
                formattedView.OnTextChanged.AddListener(_ => notifications++);
            }

            using SerializedObject serialized = new(view);
            serialized.FindProperty("_updateMode").enumValueIndex = (int)NoCodeFloatUpdateMode.Once;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeValidation(view);

            PollDue(view);
            source.ReactiveScore.Value = 0.75f;

            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications);
            Refresh(view);
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
            Refresh(view);
            Assert.AreEqual(formatted ? 1 : 0, notifications);
        }

        [TestCase(NoCodeFloatUpdateMode.Reactive)]
        [TestCase(NoCodeFloatUpdateMode.Poll)]
        public void FormattedValidation_TargetRebind_RendersSameTextWithoutNotification(NoCodeFloatUpdateMode mode)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            NoCodeFormattedText view = (NoCodeFormattedText)CreateTextView(source, true, mode,
                nameof(Audit20261023NeoNoCodeFollowupSource.Score), true, out TMP_Text previous);
            int notifications = 0;
            view.OnTextChanged.AddListener(_ => notifications++);
            TMP_Text replacement = CreateObject(true).AddComponent<TextMeshProUGUI>();
            replacement.text = "waiting";
            using SerializedObject serialized = new(view);
            serialized.FindProperty("_tmpText").objectReferenceValue = replacement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeValidation(view);

            PollDue(view);
            PollDue(view);

            Assert.AreEqual("0.25", replacement.text, "The newly assigned target needs the unchanged current text.");
            Assert.AreEqual("0.25", previous.text);
            Assert.AreEqual(0, notifications);
            source.Score = 0.75f;
            PollDue(view);
            Assert.AreEqual("0.75", replacement.text);
            Assert.AreEqual("0.25", previous.text);
            Assert.AreEqual(1, notifications);
            PollDue(view);
            Assert.AreEqual(1, notifications);
        }

        [TestCase(NoCodeFloatUpdateMode.Reactive)]
        [TestCase(NoCodeFloatUpdateMode.Poll)]
        public void FormattedValidation_FormatNotifiesOnlyWhenResultChanges(NoCodeFloatUpdateMode mode)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            NoCodeFormattedText view = (NoCodeFormattedText)CreateTextView(source, true, mode,
                nameof(Audit20261023NeoNoCodeFollowupSource.Score), true, out TMP_Text text);
            int notifications = 0;
            view.OnTextChanged.AddListener(_ => notifications++);
            using SerializedObject serialized = new(view);
            serialized.FindProperty("_format").stringValue = "{0:0.00}";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeValidation(view);

            PollDue(view);

            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications, "An equivalent format must preserve the notification baseline.");
            serialized.Update();
            serialized.FindProperty("_format").stringValue = "Score {0:0.00}";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeValidation(view);
            PollDue(view);
            Assert.AreEqual("Score 0.25", text.text);
            Assert.AreEqual(1, notifications);
            PollDue(view);
            Assert.AreEqual(1, notifications);
            source.Score = 0.75f;
            PollDue(view);
            Assert.AreEqual("Score 0.75", text.text);
            Assert.AreEqual(2, notifications);
        }

        [Test]
        public void FormattedValidation_OnceTargetRebind_WaitsForExplicitRenderWithoutDuplicateEvent()
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            NoCodeFormattedText view = (NoCodeFormattedText)CreateTextView(source, true, NoCodeFloatUpdateMode.Once,
                nameof(Audit20261023NeoNoCodeFollowupSource.CountedScore), true, out TMP_Text previous);
            int before = source.ReadCount;
            int notifications = 0;
            view.OnTextChanged.AddListener(_ => notifications++);
            TMP_Text replacement = CreateObject(true).AddComponent<TextMeshProUGUI>();
            replacement.text = "waiting";
            using SerializedObject serialized = new(view);
            serialized.FindProperty("_tmpText").objectReferenceValue = replacement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeValidation(view);

            PollDue(view);
            PollDue(view);

            Assert.AreEqual(before, source.ReadCount);
            Assert.AreEqual("waiting", replacement.text);
            Assert.AreEqual("0.25", previous.text);
            Assert.AreEqual(0, notifications);
            view.RefreshFromSource();
            Assert.AreEqual("0.25", replacement.text);
            Assert.AreEqual(0, notifications);
            source.Score = 0.75f;
            view.RefreshFromSource();
            Assert.AreEqual("0.75", replacement.text);
            Assert.AreEqual(1, notifications);
            view.RefreshFromSource();
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void FormattedEnableHook_PublishesInitialResultOnceThenResumesChangeDetection()
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            NoCodeFormattedText view = (NoCodeFormattedText)CreateTextView(source, true, NoCodeFloatUpdateMode.Reactive,
                nameof(Audit20261023NeoNoCodeFollowupSource.Score), true, out TMP_Text text);
            int notifications = 0;
            view.OnTextChanged.AddListener(_ => notifications++);

            typeof(NoCodeFormattedText).GetMethod("OnEnable", PrivateInstance).Invoke(view, null);
            view.RefreshFromSource();

            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(1, notifications, "Each enable still publishes its initial result once.");
            source.Score = 0.75f;
            PollDue(view);
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(2, notifications);
        }

        [Test]
        public void SliderCallback_ReactiveToPlainRewire_EnablesPollingForCurrentBinding()
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateSource();
            GameObject root = CreateObject(true);
            root.SetActive(false);
            Slider slider = root.AddComponent<Slider>();
            SetProgress progress = root.AddComponent<SetProgress>();
            Configure(progress, source, nameof(Audit20261023NeoNoCodeFollowupSource.ReactiveScore),
                NoCodeFloatUpdateMode.Reactive, true);
            using SerializedObject serialized = new(progress);
            serialized.FindProperty("_slider").objectReferenceValue = slider;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            progress.EditorInvokeRefreshFromSource();
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);
            Assert.That(slider.normalizedValue, Is.EqualTo(0.25f).Within(0.00001f));
            slider.onValueChanged.AddListener(_ =>
                progress.Binding.MemberName = nameof(Audit20261023NeoNoCodeFollowupSource.Score));
            source.Score = 0.5f;

            source.ReactiveScore.Value = 0.5f;

            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            Assert.That(slider.normalizedValue, Is.EqualTo(0.5f).Within(0.00001f));
            source.Score = 0.75f;
            PollDue(progress);
            Assert.That(slider.normalizedValue, Is.EqualTo(0.75f).Within(0.00001f));
        }

        private Audit20261023NeoNoCodeFollowupSource CreateSource()
        {
            return CreateObject(false).AddComponent<Audit20261023NeoNoCodeFollowupSource>();
        }

        private GameObject CreateObject(bool ui)
        {
            GameObject root = ui
                ? new GameObject("Audit20261023NeoNoCode followup UI", typeof(RectTransform))
                : new GameObject("Audit20261023NeoNoCode followup source");
            _objects.Add(root);
            return root;
        }

        private MonoBehaviour CreateTextView(Audit20261023NeoNoCodeFollowupSource source, bool formatted,
            NoCodeFloatUpdateMode mode, string member, bool pollInLateUpdate, out TMP_Text text)
        {
            GameObject root = CreateObject(true);
            root.SetActive(false);
            text = root.AddComponent<TextMeshProUGUI>();
            MonoBehaviour view = formatted
                ? (MonoBehaviour)root.AddComponent<NoCodeFormattedText>()
                : root.AddComponent<NoCodeBindText>();
            Configure(view, source, member, mode, pollInLateUpdate);
            root.SetActive(true);
            Refresh(view);
            return view;
        }

        private static void Configure(MonoBehaviour view, Audit20261023NeoNoCodeFollowupSource source,
            string member, NoCodeFloatUpdateMode mode, bool pollInLateUpdate)
        {
            using SerializedObject serialized = new(view);
            SerializedProperty binding;
            if (view is NoCodeFormattedText)
            {
                SerializedProperty values = serialized.FindProperty("_values");
                values.arraySize = 1;
                binding = values.GetArrayElementAtIndex(0);
                serialized.FindProperty("_format").stringValue = "{0}";
            }
            else
            {
                binding = serialized.FindProperty("_binding");
            }

            binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = source.gameObject;
            binding.FindPropertyRelative("_componentTypeName").stringValue = typeof(Audit20261023NeoNoCodeFollowupSource).FullName;
            binding.FindPropertyRelative("_memberName").stringValue = member;
            serialized.FindProperty("_updateMode").enumValueIndex = (int)mode;
            serialized.FindProperty("_pollInLateUpdate").boolValue = pollInLateUpdate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeValidation(view);
        }

        private static void Refresh(MonoBehaviour view)
        {
            if (view is NoCodeFormattedText formatted)
            {
                formatted.RefreshFromSource();
            }
            else
            {
                ((NoCodeFloatBindingBehaviour)view).EditorInvokeRefreshFromSource();
            }
        }

        private static void InvokeValidation(MonoBehaviour view)
        {
            MethodInfo validation = view.GetType().GetMethod("OnValidate", PrivateInstance);
            validation.Invoke(view, null);
        }

        private static void PollDue(MonoBehaviour view)
        {
            // Exercise the actual scheduling gate; reflection only supplies the editor lifecycle/deadline seam.
            Type owner = view is NoCodeFormattedText ? typeof(NoCodeFormattedText) : typeof(NoCodeFloatBindingBehaviour);
            owner.GetField("_nextPollTime", PrivateInstance).SetValue(view, -1f);
            owner.GetMethod("LateUpdate", PrivateInstance).Invoke(view, null);
        }
    }
}
