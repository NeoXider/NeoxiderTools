using System.Collections.Generic;
using System.Reflection;
using Neo.NoCode;
using Neo.Reactive;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{


    // Exercises serialized Inspector authoring and explicit public refresh/reactive contracts in Edit Mode.
    // Unity dispatches validation when serialized edits are applied; no lifecycle message is invoked manually.
    // These tests do not claim native enable/disable or PlayerLoop scheduling coverage.
    public sealed class Audit20261023NeoNoCodeBindingTests
    {
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

        [Test]
        public void Binding_ReusedWithDifferentHost_ReadsCurrentHost()
        {
            Audit20261023NeoNoCodeSource first = CreateSource();
            Audit20261023NeoNoCodeSource second = CreateSource();
            first.Score = 10f;
            second.Score = 20f;
            ComponentFloatBinding binding = new()
            {
                ComponentTypeName = typeof(Audit20261023NeoNoCodeSource).FullName,
                MemberName = nameof(Audit20261023NeoNoCodeSource.Score)
            };

            Assert.IsTrue(binding.TryReadFloat(first, out float firstValue));
            Assert.AreEqual(10f, firstValue);
            Assert.IsTrue(binding.TryReadFloat(second, out float secondValue));
            Assert.AreEqual(20f, secondValue);
            Assert.IsTrue(binding.TryReadFloat(first, out float restoredValue));
            Assert.AreEqual(10f, restoredValue);
        }

        [Test]
        public void Binding_DestroyedExplicitRoot_CanBeClearedToUseHost()
        {
            Audit20261023NeoNoCodeSource host = CreateSource();
            Audit20261023NeoNoCodeSource source = CreateSource();
            host.Score = 12f;
            ComponentFloatBinding binding = new()
            {
                SourceRoot = source.gameObject,
                ComponentTypeName = typeof(Audit20261023NeoNoCodeSource).FullName,
                MemberName = nameof(Audit20261023NeoNoCodeSource.Score)
            };
            Object.DestroyImmediate(source.gameObject);

            Assert.IsFalse(binding.TryReadFloat(host, out _), "A destroyed explicit root must not fall back silently.");
            binding.SourceRoot = null;

            Assert.IsTrue(binding.TryReadFloat(host, out float value));
            Assert.AreEqual(12f, value, "Explicitly clearing the root restores host fallback.");
        }

        [Test]
        public void Binding_OverloadedIndexers_AreRejectedAsSources()
        {
            bool resolved = ComponentFloatBinding.TryResolveSupportedSourceMember(
                typeof(Audit20261023NeoNoCodeSource), "Item", out MemberInfo member);

            Assert.IsFalse(resolved);
            Assert.IsNull(member);
        }

        [Test]
        public void SingleBinding_SourceRootChanged_MovesSubscriptionAndUpdatesText()
        {
            Audit20261023NeoNoCodeSource first = CreateSource();
            Audit20261023NeoNoCodeSource second = CreateSource();
            NoCodeBindText binding = CreateTextBinding(first, out TMP_Text text);
            Assert.AreEqual("0.25", text.text);

            binding.Binding.SourceRoot = second.gameObject;
            binding.EditorInvokeRefreshFromSource();

            Assert.AreEqual(0, first.ReactiveScore.ListenerCount);
            Assert.AreEqual(1, second.ReactiveScore.ListenerCount);
            second.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
        }

        [Test]
        public void SingleBinding_ReplacedReactiveInstance_MovesSubscription()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeBindText binding = CreateTextBinding(source, out TMP_Text text);
            ReactivePropertyFloat previous = source.ReactiveScore;
            source.ReactiveScore = new ReactivePropertyFloat(0.5f);

            binding.EditorInvokeRefreshFromSource();

            Assert.AreEqual(0, previous.ListenerCount);
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SingleBinding_MemberChanged_SwitchesReactiveValueType(bool useBool)
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeBindText binding = CreateTextBinding(source, out TMP_Text text);
            binding.Binding.MemberName = useBool
                ? nameof(Audit20261023NeoNoCodeSource.ReactiveFlag)
                : nameof(Audit20261023NeoNoCodeSource.ReactiveLevel);

            binding.EditorInvokeRefreshFromSource();

            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            Assert.AreEqual(1, useBool ? source.ReactiveFlag.ListenerCount : source.ReactiveLevel.ListenerCount);
            source.ReactiveFlag.Value = true;
            source.ReactiveLevel.Value = 7;
            Assert.AreEqual(useBool ? "1" : "7", text.text);
        }

        [Test]
        public void SingleBinding_DisabledManualRefresh_DoesNotLeaveSubscription()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeBindText binding = CreateTextBinding(source, out TMP_Text text);
            binding.enabled = false;

            binding.EditorInvokeRefreshFromSource();

            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.25", text.text, "A disabled binding must not receive later source pushes.");
        }

        [Test]
        public void SingleBinding_DisabledByEarlierListener_IgnoresInFlightNotification()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeBindText binding = null;
            source.ReactiveScore.AddListener(_ => binding.enabled = false);
            binding = CreateTextBinding(source, out TMP_Text text);

            source.ReactiveScore.Value = 0.75f;

            Assert.IsFalse(binding.enabled);
            Assert.AreEqual("0.25", text.text);
        }

        [Test]
        public void SingleBinding_DestroyedTextTarget_FindsReplacement()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeBindText binding = CreateTextBinding(source, out TMP_Text previous);
            Object.DestroyImmediate(previous);
            TMP_Text replacement = binding.gameObject.AddComponent<TextMeshProUGUI>();
            replacement.text = "waiting";

            binding.EditorInvokeRefreshFromSource();

            Assert.AreEqual("0.25", replacement.text);
        }

        [Test]
        public void SingleBinding_InspectorTargetChanged_WritesToNewTarget()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeBindText binding = CreateTextBinding(source, out TMP_Text previous);
            TMP_Text replacement = CreateObject(true).AddComponent<TextMeshProUGUI>();
            replacement.text = "waiting";
            SerializedObject serialized = new(binding);
            serialized.FindProperty("_fallbackText").objectReferenceValue = replacement;
            Assert.IsTrue(serialized.ApplyModifiedPropertiesWithoutUndo(), "The Inspector target edit must be committed.");

            binding.EditorInvokeRefreshFromSource();
            Assert.AreEqual("0.25", replacement.text, "Serialized validation must invalidate the previous target cache.");
            Assert.AreEqual("0.25", previous.text);

            source.ReactiveScore.Value = 0.75f;

            Assert.AreEqual("0.75", replacement.text);
            Assert.AreEqual("0.25", previous.text);
        }

        [Test]
        public void FormattedText_RewiredSource_ReleasesOldSubscription()
        {
            Audit20261023NeoNoCodeSource first = CreateSource();
            Audit20261023NeoNoCodeSource second = CreateSource();
            NoCodeFormattedText formatted = CreateFormattedText(first, true, out TMP_Text text);

            WireFormattedText(formatted, second, "{0}", nameof(Audit20261023NeoNoCodeSource.ReactiveScore));
            formatted.RefreshFromSource();

            Assert.AreEqual(0, first.ReactiveScore.ListenerCount);
            Assert.AreEqual(1, second.ReactiveScore.ListenerCount);
            second.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
        }

        [Test]
        public void FormattedText_ReplacedReactiveInstance_MovesSubscription()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeFormattedText formatted = CreateFormattedText(source, true, out TMP_Text text);
            ReactivePropertyFloat previous = source.ReactiveScore;
            source.ReactiveScore = new ReactivePropertyFloat(0.5f);

            formatted.RefreshFromSource();

            Assert.AreEqual(0, previous.ListenerCount);
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FormattedText_MemberChanged_SwitchesReactiveValueType(bool useBool)
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeFormattedText formatted = CreateFormattedText(source, true, out TMP_Text text);
            WireFormattedText(formatted, source, "{0}", useBool
                ? nameof(Audit20261023NeoNoCodeSource.ReactiveFlag)
                : nameof(Audit20261023NeoNoCodeSource.ReactiveLevel));

            formatted.RefreshFromSource();

            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            Assert.AreEqual(1, useBool ? source.ReactiveFlag.ListenerCount : source.ReactiveLevel.ListenerCount);
            source.ReactiveFlag.Value = true;
            source.ReactiveLevel.Value = 7;
            Assert.AreEqual(useBool ? "1" : "7", text.text);
        }

        [Test]
        public void FormattedText_RemovedSource_UnsubscribesOnlyRemovedProperty()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeFormattedText formatted = CreateFormattedText(source, true, out TMP_Text text);
            WireFormattedText(formatted, source, "{0}/{1}",
                nameof(Audit20261023NeoNoCodeSource.ReactiveScore),
                nameof(Audit20261023NeoNoCodeSource.ReactiveLevel));
            formatted.RefreshFromSource();
            Assert.AreEqual("0.25/2", text.text);
            Assert.AreEqual(1, source.ReactiveLevel.ListenerCount);

            WireFormattedText(formatted, source, "{0}", nameof(Audit20261023NeoNoCodeSource.ReactiveScore));
            formatted.RefreshFromSource();

            Assert.AreEqual(0, source.ReactiveLevel.ListenerCount);
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
        }

        [Test]
        public void FormattedText_DuplicateSources_NotifyOnceAndReleaseOnDisabledRefresh()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeFormattedText formatted = CreateFormattedText(source, true, out TMP_Text text);
            WireFormattedText(formatted, source, "{0}/{1}",
                nameof(Audit20261023NeoNoCodeSource.ReactiveScore),
                nameof(Audit20261023NeoNoCodeSource.ReactiveScore));
            formatted.RefreshFromSource();
            int notifications = 0;
            formatted.OnTextChanged.AddListener(_ => notifications++);

            source.ReactiveScore.Value = 0.5f;

            Assert.AreEqual("0.5/0.5", text.text);
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);
            formatted.enabled = false;
            formatted.RefreshFromSource();
            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.5/0.5", text.text);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void FormattedText_DisabledByEarlierListener_IgnoresInFlightNotification()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeFormattedText formatted = null;
            source.ReactiveScore.AddListener(_ => formatted.enabled = false);
            formatted = CreateFormattedText(source, true, out TMP_Text text);
            int notifications = 0;
            formatted.OnTextChanged.AddListener(_ => notifications++);

            source.ReactiveScore.Value = 0.75f;

            Assert.IsFalse(formatted.enabled);
            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void FormattedText_LateTarget_ReceivesUnchangedTextWithoutDuplicateEvent()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeFormattedText formatted = CreateFormattedText(source, false, out _);
            int notifications = 0;
            formatted.OnTextChanged.AddListener(_ => notifications++);
            TMP_Text text = formatted.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "waiting";

            formatted.RefreshFromSource();
            formatted.RefreshFromSource();

            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(0, notifications, "Resolving a target must not report an unchanged formatted value twice.");
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void FormattedText_DestroyedTarget_ReceivesUnchangedTextOnReplacement()
        {
            Audit20261023NeoNoCodeSource source = CreateSource();
            NoCodeFormattedText formatted = CreateFormattedText(source, true, out TMP_Text previous);
            int notifications = 0;
            formatted.OnTextChanged.AddListener(_ => notifications++);
            Object.DestroyImmediate(previous);
            TMP_Text replacement = formatted.gameObject.AddComponent<TextMeshProUGUI>();
            replacement.text = "waiting";

            formatted.RefreshFromSource();

            Assert.AreEqual("0.25", replacement.text);
            Assert.AreEqual(0, notifications);
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", replacement.text);
            Assert.AreEqual(1, notifications);
        }

        private Audit20261023NeoNoCodeSource CreateSource()
        {
            return CreateObject(false).AddComponent<Audit20261023NeoNoCodeSource>();
        }

        private GameObject CreateObject(bool ui)
        {
            GameObject root = ui
                ? new GameObject("Audit20261023NeoNoCode UI", typeof(RectTransform))
                : new GameObject("Audit20261023NeoNoCode source");
            _objects.Add(root);
            return root;
        }

        private NoCodeBindText CreateTextBinding(Audit20261023NeoNoCodeSource source, out TMP_Text text)
        {
            GameObject root = CreateObject(true);
            root.SetActive(false);
            text = root.AddComponent<TextMeshProUGUI>();
            NoCodeBindText binding = root.AddComponent<NoCodeBindText>();
            binding.Binding.SourceRoot = source.gameObject;
            binding.Binding.ComponentTypeName = typeof(Audit20261023NeoNoCodeSource).FullName;
            binding.Binding.MemberName = nameof(Audit20261023NeoNoCodeSource.ReactiveScore);
            root.SetActive(true);
            binding.EditorInvokeRefreshFromSource();
            return binding;
        }

        private NoCodeFormattedText CreateFormattedText(Audit20261023NeoNoCodeSource source, bool addTarget,
            out TMP_Text text)
        {
            GameObject root = CreateObject(true);
            root.SetActive(false);
            text = addTarget ? root.AddComponent<TextMeshProUGUI>() : null;
            NoCodeFormattedText formatted = root.AddComponent<NoCodeFormattedText>();
            WireFormattedText(formatted, source, "{0}", nameof(Audit20261023NeoNoCodeSource.ReactiveScore));
            root.SetActive(true);
            formatted.RefreshFromSource();
            return formatted;
        }

        private static void WireFormattedText(NoCodeFormattedText formatted, Audit20261023NeoNoCodeSource source,
            string format, params string[] members)
        {
            SerializedObject serialized = new(formatted);
            SerializedProperty values = serialized.FindProperty("_values");
            values.arraySize = members.Length;
            for (int i = 0; i < members.Length; i++)
            {
                SerializedProperty binding = values.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = source.gameObject;
                binding.FindPropertyRelative("_componentTypeName").stringValue = typeof(Audit20261023NeoNoCodeSource).FullName;
                binding.FindPropertyRelative("_memberName").stringValue = members[i];
            }

            serialized.FindProperty("_format").stringValue = format;
            Assert.IsTrue(serialized.ApplyModifiedPropertiesWithoutUndo(), "The source/format edit must be committed.");
        }
    }
}
