using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.NoCode;
using Neo.Tests.Performance;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Covers the 2026-11 NoCode quality pass: one reflective read per refresh, no repeated formatting or output
    ///     writes for unchanged values, allocation-free retries and refreshes, and the typed C# API
    ///     (RefreshBinding, UpdateMode, SetValues/Format, SetRange, Find By Name setters).
    ///     EditMode contracts; LateUpdate is driven through the existing reflection seam, not the PlayerLoop.
    /// </summary>
    [TestFixture]
    public sealed class NoCodeQualityPassTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int Iterations = 200;
        private readonly List<GameObject> _objects = new();
        private object[] _boxSink;

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

        [TestCase(false)]
        [TestCase(true)]
        public void PlainSourceRefresh_ReadsTheGetterOnce(bool formatted)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateFollowupSource();
            MonoBehaviour view = CreateTextView(source.gameObject, typeof(Audit20261023NeoNoCodeFollowupSource),
                nameof(Audit20261023NeoNoCodeFollowupSource.CountedScore), NoCodeFloatUpdateMode.Reactive, formatted,
                out TMP_Text text);
            Assert.AreEqual("0.25", text.text);

            int before = source.ReadCount;
            Refresh(view);
            Assert.AreEqual(before + 1, source.ReadCount, "One refresh must read the bound member exactly once.");

            source.Score = 0.75f;
            before = source.ReadCount;
            PollDue(view);
            Assert.AreEqual(before + 1, source.ReadCount, "The Reactive poll fallback reads once per tick.");
            Assert.AreEqual("0.75", text.text);
        }

        [Test]
        public void BindText_UnchangedValue_IsNotRewrittenUntilForced()
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateFollowupSource();
            NoCodeBindText view = (NoCodeBindText)CreateTextView(source.gameObject,
                typeof(Audit20261023NeoNoCodeFollowupSource), nameof(Audit20261023NeoNoCodeFollowupSource.Score),
                NoCodeFloatUpdateMode.Poll, false, out TMP_Text text);
            Assert.AreEqual("0.25", text.text);

            text.text = "external";
            PollDue(view);
            Assert.AreEqual("external", text.text, "An unchanged value is not pushed to the output again.");

            view.ForceRewrite();
            Assert.AreEqual("0.25", text.text);

            source.Score = 0.5f;
            PollDue(view);
            Assert.AreEqual("0.5", text.text, "A changed value is written on the next poll.");
        }

        [Test]
        public void FormattedText_ReactiveRefreshWithoutChanges_DoesNotAllocate()
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateFollowupSource();
            NoCodeFormattedText view = (NoCodeFormattedText)CreateTextView(source.gameObject,
                typeof(Audit20261023NeoNoCodeFollowupSource),
                nameof(Audit20261023NeoNoCodeFollowupSource.ReactiveScore), NoCodeFloatUpdateMode.Reactive, true,
                out TMP_Text text);
            view.RefreshFromSource();
            view.RefreshFromSource();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                view.RefreshFromSource();
            }

            long allocated = allocationCounter.End();
            Assert.AreEqual("0.25", text.text);
            Assert.That(allocated, Is.Zero, "Unchanged inputs must not run string.Format on every refresh.");

            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
            Assert.AreEqual("0.75", view.CurrentText);
        }

        [Test]
        public void FormattedText_PlainRefresh_BoxesAtMostOncePerSource()
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateFollowupSource();
            NoCodeFormattedText view = (NoCodeFormattedText)CreateTextView(source.gameObject,
                typeof(Audit20261023NeoNoCodeFollowupSource), nameof(Audit20261023NeoNoCodeFollowupSource.Score),
                NoCodeFloatUpdateMode.Poll, true, out _);
            view.RefreshFromSource();

            long boxCalls = MeasureFloatBoxCalls();
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                view.RefreshFromSource();
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.LessThan(2 * boxCalls),
                "A plain float source is read (boxed) once per refresh and the unchanged text is not re-formatted.");
        }

        [Test]
        public void MissingComponent_WarmRetries_DoNotAllocate()
        {
            GameObject empty = CreateObject(false);
            NoCodeBindEditModeFloatSource host = CreateObject(false).AddComponent<NoCodeBindEditModeFloatSource>();
            ComponentFloatBinding binding = new()
            {
                SourceRoot = empty,
                ComponentTypeName = typeof(NoCodeBindEditModeFloatSource).FullName,
                MemberName = nameof(NoCodeBindEditModeFloatSource.Score)
            };
            Assert.That(binding.TryReadFloat(host, out _), Is.False);
            Assert.That(binding.EnsureCache(host, out string error), Is.False);
            Assert.That(error, Does.Contain("not found"), "The public diagnostic keeps its message.");

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                binding.TryReadFloat(host, out _);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero, "Late-spawn retries must not allocate component arrays or messages.");

            NoCodeBindEditModeFloatSource late = empty.AddComponent<NoCodeBindEditModeFloatSource>();
            late.Score = 12f;
            Assert.That(binding.TryReadFloat(host, out float value), Is.True);
            Assert.That(value, Is.EqualTo(12f));
        }

        [Test]
        public void FormattedText_SetValuesAndFormat_RenderThroughTypedApi()
        {
            NoCodeBindEditModeFloatSource source = CreateObject(false).AddComponent<NoCodeBindEditModeFloatSource>();
            GameObject root = CreateObject(true);
            TMP_Text text = root.AddComponent<TextMeshProUGUI>();
            NoCodeFormattedText view = root.AddComponent<NoCodeFormattedText>();
            List<string> published = new();
            view.OnTextChanged.AddListener(published.Add);
            ComponentFloatBinding binding = new()
            {
                SourceRoot = source.gameObject,
                ComponentTypeName = typeof(NoCodeBindEditModeFloatSource).FullName,
                MemberName = nameof(NoCodeBindEditModeFloatSource.Score)
            };

            view.SetValues(new[] { binding }, "HP {0:0}");
            view.RefreshFromSource();
            Assert.AreEqual("HP 40", text.text);
            Assert.That(view.Values.Count, Is.EqualTo(1));
            Assert.That(view.Values[0], Is.SameAs(binding));

            view.Format = "{0:0.0}";
            view.RefreshFromSource();
            Assert.AreEqual("40.0", text.text);
            Assert.AreEqual("40.0", view.CurrentText);
            Assert.That(published, Is.EqualTo(new[] { "HP 40", "40.0" }));
        }

        [Test]
        public void SetProgress_SetRange_ReappliesTheBoundValue()
        {
            NoCodeBindEditModeFloatSource source = CreateObject(false).AddComponent<NoCodeBindEditModeFloatSource>();
            GameObject root = CreateObject(true);
            Image image = root.AddComponent<Image>();
            SetProgress progress = root.AddComponent<SetProgress>();
            using (SerializedObject serialized = new(progress))
            {
                serialized.FindProperty("_image").objectReferenceValue = image;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            progress.Binding.SourceRoot = source.gameObject;
            progress.Binding.ComponentTypeName = typeof(NoCodeBindEditModeFloatSource).FullName;
            progress.Binding.MemberName = nameof(NoCodeBindEditModeFloatSource.Score);
            progress.UpdateMode = NoCodeFloatUpdateMode.Once;

            progress.SetRange(0f, 100f);
            progress.RefreshBinding();
            Assert.That(image.fillAmount, Is.EqualTo(0.4f).Within(1e-5f));

            progress.SetRange(0f, 80f);
            progress.RefreshBinding();
            Assert.That(progress.MaxValue, Is.EqualTo(80f));
            Assert.That(image.fillAmount, Is.EqualTo(0.5f).Within(1e-5f), "A new max (e.g. max HP) re-maps the bar.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UpdateModeSetter_SwitchingToPoll_ReleasesTheReactiveListener(bool formatted)
        {
            Audit20261023NeoNoCodeFollowupSource source = CreateFollowupSource();
            MonoBehaviour view = CreateTextView(source.gameObject, typeof(Audit20261023NeoNoCodeFollowupSource),
                nameof(Audit20261023NeoNoCodeFollowupSource.ReactiveScore), NoCodeFloatUpdateMode.Reactive, formatted,
                out TMP_Text text);
            Assert.AreEqual(1, source.ReactiveScore.ListenerCount);

            if (view is NoCodeFormattedText formattedView)
            {
                formattedView.UpdateMode = NoCodeFloatUpdateMode.Poll;
                formattedView.PollIntervalSeconds = 0f;
                Assert.That(formattedView.PollIntervalSeconds, Is.EqualTo(0.016f).Within(1e-6f));
            }
            else
            {
                NoCodeFloatBindingBehaviour single = (NoCodeFloatBindingBehaviour)view;
                single.UpdateMode = NoCodeFloatUpdateMode.Poll;
                single.PollIntervalSeconds = 0f;
                Assert.That(single.PollIntervalSeconds, Is.EqualTo(0.016f).Within(1e-6f));
            }

            PollDue(view);
            Assert.AreEqual(0, source.ReactiveScore.ListenerCount);
            source.ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.25", text.text, "Poll mode does not react to notifications.");
            PollDue(view);
            Assert.AreEqual("0.75", text.text);
        }

        [Test]
        public void FindByName_PublicSetters_ResolveTheNamedObject()
        {
            NoCodeBindEditModeFloatSource source = CreateObject(false).AddComponent<NoCodeBindEditModeFloatSource>();
            source.gameObject.name = "NoCodeQualityPass " + Guid.NewGuid().ToString("N");
            source.Score = 7f;
            NoCodeBindEditModeFloatSource host = CreateObject(false).AddComponent<NoCodeBindEditModeFloatSource>();
            ComponentFloatBinding binding = new()
            {
                UseSceneSearch = true,
                SearchObjectName = source.gameObject.name,
                ComponentTypeName = typeof(NoCodeBindEditModeFloatSource).FullName,
                MemberName = nameof(NoCodeBindEditModeFloatSource.Score)
            };

            Assert.That(binding.TryReadFloat(host, out float value), Is.True);
            Assert.That(value, Is.EqualTo(7f), "The named object wins over the host's own component.");
            Assert.That(binding.UseSceneSearch, Is.True);
        }

        private Audit20261023NeoNoCodeFollowupSource CreateFollowupSource()
        {
            return CreateObject(false).AddComponent<Audit20261023NeoNoCodeFollowupSource>();
        }

        private MonoBehaviour CreateTextView(GameObject sourceRoot, Type sourceType, string member,
            NoCodeFloatUpdateMode mode, bool formatted, out TMP_Text text)
        {
            GameObject root = CreateObject(true);
            root.SetActive(false);
            text = root.AddComponent<TextMeshProUGUI>();
            MonoBehaviour view = formatted
                ? root.AddComponent<NoCodeFormattedText>()
                : root.AddComponent<NoCodeBindText>();
            using (SerializedObject serialized = new(view))
            {
                SerializedProperty binding;
                if (formatted)
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

                binding.FindPropertyRelative("_sourceRoot").objectReferenceValue = sourceRoot;
                binding.FindPropertyRelative("_componentTypeName").stringValue = sourceType.FullName;
                binding.FindPropertyRelative("_memberName").stringValue = member;
                serialized.FindProperty("_updateMode").enumValueIndex = (int)mode;
                serialized.FindProperty("_pollInLateUpdate").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            InvokeValidation(view);
            root.SetActive(true);
            Refresh(view);
            return view;
        }

        private static void Refresh(MonoBehaviour view)
        {
            if (view is NoCodeFormattedText formatted)
            {
                formatted.RefreshFromSource();
            }
            else
            {
                ((NoCodeFloatBindingBehaviour)view).RefreshBinding();
            }
        }

        private static void InvokeValidation(MonoBehaviour view)
        {
            view.GetType().GetMethod("OnValidate", PrivateInstance)?.Invoke(view, null);
        }

        private static void PollDue(MonoBehaviour view)
        {
            Type owner = view is NoCodeFormattedText ? typeof(NoCodeFormattedText) : typeof(NoCodeFloatBindingBehaviour);
            owner.GetField("_nextPollTime", PrivateInstance).SetValue(view, -1f);
            owner.GetMethod("LateUpdate", PrivateInstance).Invoke(view, null);
        }

        /// <summary>
        ///     Positive control: <see cref="Iterations"/> float boxes must read as exactly one GC.Alloc call each, so the
        ///     "fewer than two boxes per refresh" bound is meaningful.
        /// </summary>
        /// <remarks>
        ///     WHY: the bound read 0 against 0 (and failed as "less than 0") while it was measured with the managed byte
        ///     counter, which is always 0 in the Unity 6 Editor.
        /// </remarks>
        private long MeasureFloatBoxCalls()
        {
            _boxSink ??= new object[1];
            _boxSink[0] = -1f;
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                _boxSink[0] = (float)i;
            }

            long boxCalls = allocationCounter.End();
            Assert.That(boxCalls, Is.EqualTo(Iterations), "Positive control: the counter must see one call per box.");
            return boxCalls;
        }

        private GameObject CreateObject(bool ui)
        {
            GameObject root = ui
                ? new GameObject("NoCodeQualityPass UI", typeof(RectTransform))
                : new GameObject("NoCodeQualityPass source");
            _objects.Add(root);
            return root;
        }
    }
}
