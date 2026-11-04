using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Condition;
using Neo.Quest;
using Neo.Save;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Library pass 2026-11: allocation-free notifications, amount-based progress, target notifications for
    ///     binary objectives, batched auto-save, typed C# events and the extended NoCode bridge.
    /// </summary>
    [TestFixture]
    public sealed class QuestLibraryPassTests
    {
        private const int Iterations = 200;

        private readonly List<ScriptableObject> _assets = new();
        private GameObject _root;
        private QuestManager _manager;
        private string _saveKey;
        private ISaveProvider _previousProvider;
        private CountingSaveProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new CountingSaveProvider();
            SaveProvider.SetProvider(_provider);
            _saveKey = "QuestLibraryPass_" + Guid.NewGuid().ToString("N");
            _root = new GameObject("QuestLibraryPass");
            _root.SetActive(false);
            _manager = _root.AddComponent<QuestManager>();
            SetField(_manager, "_autoLoad", false);
            SetField(_manager, "_autoSave", false);
            SetField(_manager, "_saveKey", _saveKey);
            _manager.ConditionContext = _root;
            _root.SetActive(true);
            _root.SetActive(false);
            Assert.That(QuestManager.Instance, Is.SameAs(_manager));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                UnityEngine.Object.DestroyImmediate(_root);
                foreach (ScriptableObject asset in _assets)
                {
                    if (asset != null)
                    {
                        UnityEngine.Object.DestroyImmediate(asset);
                    }
                }

                _assets.Clear();
            }
            finally
            {
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [Test]
        public void NotifyKill_WarmCounterProgressAndMisses_AllocateNothing()
        {
            QuestConfig counter = CreateQuest("alloc_counter",
                Objective(QuestObjectiveType.KillCount, "orc", 1000000),
                Objective(QuestObjectiveType.CollectCount, "gem", 1000000));
            QuestConfig other = CreateQuest("alloc_other", Objective(QuestObjectiveType.Talk, "king"));
            Assert.That(_manager.AcceptQuest(counter), Is.True);
            Assert.That(_manager.AcceptQuest(other), Is.True);
            int progressEvents = 0;
            // WHY: a nested notification forces a second pooled snapshot; it must be reused when warm too.
            Action<QuestConfig, int, int> nested = (config, index, count) =>
            {
                progressEvents++;
                if (index == 0)
                {
                    _manager.NotifyCollect("gem");
                }
            };
            _manager.ObjectiveProgress += nested;
            _manager.NotifyKill("orc");
            _manager.NotifyKill("missing");
            _manager.EvaluateObjectiveConditions();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                _manager.NotifyKill("orc");
                _manager.NotifyKill("missing");
                _manager.EvaluateObjectiveConditions();
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(_manager.GetObjectiveProgress(counter, 0), Is.EqualTo(Iterations + 1));
            Assert.That(_manager.GetObjectiveProgress(counter, 1), Is.EqualTo(Iterations + 1));
            Assert.That(progressEvents, Is.EqualTo((Iterations + 1) * 2));
            _manager.ObjectiveProgress -= nested;
        }

        [Test]
        public void GetActiveQuests_FillsCallerBufferWithoutAllocating()
        {
            QuestConfig first = CreateQuest("active_a", Objective(QuestObjectiveType.Talk, "a"));
            QuestConfig second = CreateQuest("active_b", Objective(QuestObjectiveType.Talk, "b"));
            Assert.That(_manager.AcceptQuest(first), Is.True);
            Assert.That(_manager.AcceptQuest(second), Is.True);
            _manager.FailQuest(second);
            List<QuestState> buffer = new(4);
            _manager.GetActiveQuests(buffer);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            int count = 0;
            for (int i = 0; i < Iterations; i++)
            {
                count = _manager.GetActiveQuests(buffer);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer[0], Is.SameAs(_manager.GetState(first)));
            Assert.That(_manager.ActiveQuests, Is.EqualTo(buffer));
            Assert.Throws<ArgumentNullException>(() => _manager.GetActiveQuests(null));
        }

        [Test]
        public void NotifyKill_WithAmount_ClampsToRequiredCountAndCompletesOnce()
        {
            QuestConfig quest = CreateQuest("amount", Objective(QuestObjectiveType.KillCount, "orc", 5));
            List<int> progress = new();
            int objectiveEvents = 0;
            int typedObjectiveEvents = 0;
            int questEvents = 0;
            _manager.OnObjectiveProgress.AddListener((id, index, count) => progress.Add(count));
            _manager.OnObjectiveCompleted.AddListener((id, index) => objectiveEvents++);
            _manager.ObjectiveCompleted += (config, index) => typedObjectiveEvents++;
            _manager.QuestCompleted += config => questEvents++;
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            _manager.NotifyKill("orc", 3);
            Assert.That(_manager.IsActive(quest), Is.True);
            _manager.NotifyKill("orc", 10);
            _manager.NotifyKill("orc", 10);

            Assert.That(progress, Is.EqualTo(new[] { 3, 5 }));
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(5));
            Assert.That(objectiveEvents, Is.EqualTo(1));
            Assert.That(typedObjectiveEvents, Is.EqualTo(1));
            Assert.That(questEvents, Is.EqualTo(1));
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [Test]
        public void NotifyReachPointAndTalk_CompleteOnlyMatchingBinaryObjectives()
        {
            QuestConfig quest = CreateQuest("travel",
                Objective(QuestObjectiveType.ReachPoint, "castle"),
                Objective(QuestObjectiveType.Talk, "king"),
                Objective(QuestObjectiveType.KillCount, "king", 2));
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            Assert.That(_manager.Notify(QuestObjectiveType.Talk, "castle"), Is.Zero);
            _manager.NotifyReachPoint("castle");
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(0), Is.True);
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(1));

            _manager.NotifyTalk("king");
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(1), Is.True);
            Assert.That(_manager.GetObjectiveProgress(quest, 2), Is.Zero, "Talk must not credit a KillCount objective.");
            Assert.That(_manager.IsActive(quest), Is.True);

            Assert.That(_manager.Notify(QuestObjectiveType.KillCount, "king", 2), Is.EqualTo(1));
            Assert.That(_manager.IsCompleted(quest), Is.True);
            Assert.That(_manager.Notify(QuestObjectiveType.KillCount, "king"), Is.Zero);
        }

        [Test]
        public void AddObjectiveProgress_RejectsNonPositiveAmountsInactiveQuestsAndCompletedObjectives()
        {
            QuestConfig quest = CreateQuest("progress", Objective(QuestObjectiveType.CollectCount, "gem", 3));

            Assert.That(_manager.AddObjectiveProgress(quest, 0, 1), Is.False, "not accepted yet");
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            Assert.That(_manager.AddObjectiveProgress(quest, 0, 0), Is.False);
            Assert.That(_manager.AddObjectiveProgress(quest, 0, -4), Is.False);
            Assert.That(_manager.AddObjectiveProgress(quest, 1, 1), Is.False);
            Assert.That(_manager.AddObjectiveProgress("progress", 0, 2), Is.True);
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(2));
            Assert.That(_manager.AddObjectiveProgress(quest, 0, int.MaxValue), Is.True, "overflow-safe clamp");
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(3));
            Assert.That(_manager.AddObjectiveProgress(quest, 0, 1), Is.False);
        }

        [Test]
        public void CompleteObjective_WithNewerConfigAppendingObjective_RecordsTheCredit()
        {
            QuestConfig original = CreateQuest("grown", Objective(QuestObjectiveType.Talk, "a"));
            Assert.That(_manager.AcceptQuest(original), Is.True);
            QuestConfig grown = CreateQuest("grown", Objective(QuestObjectiveType.Talk, "a"),
                Objective(QuestObjectiveType.ReachPoint, "b"));
            List<int> completedIndices = new();
            _manager.OnObjectiveCompleted.AddListener((id, index) => completedIndices.Add(index));

            _manager.CompleteObjective(grown, 1);

            QuestState state = _manager.GetState(grown);
            Assert.That(completedIndices, Is.EqualTo(new[] { 1 }));
            Assert.That(state.ObjectiveCount, Is.EqualTo(2));
            Assert.That(state.IsObjectiveCompleted(1), Is.True, "the reported completion must be stored");
            Assert.That(state.GetObjectiveProgress(1), Is.EqualTo(1));
            Assert.That(_manager.IsActive(grown), Is.True);
        }

        [Test]
        public void AutoSave_ChainAcceptedByCompletionHandler_WritesOnceAfterOutermostOperation()
        {
            QuestConfig kill = CreateQuest("chain_kill", Objective(QuestObjectiveType.KillCount, "boar"));
            QuestConfig next = CreateQuest("chain_next", Objective(QuestObjectiveType.Talk, "elder"));
            _manager.RegisterQuest(next);
            Assert.That(_manager.AcceptQuest(kill), Is.True);
            _manager.AutoSave = true;
            _manager.QuestCompleted += config => _manager.AcceptQuest(next);
            _provider.ResetCounters();

            _manager.NotifyKill("boar");

            Assert.That(_provider.StringWrites(_saveKey), Is.EqualTo(1));
            string json = _provider.GetString(_saveKey);
            Assert.That(json, Does.Contain("chain_kill").And.Contain("chain_next"));
            Assert.That(_manager.IsActive(next), Is.True);

            _provider.ResetCounters();
            Assert.That(_manager.RestartQuest(kill), Is.True);
            Assert.That(_provider.StringWrites(_saveKey), Is.EqualTo(1), "reset + accept persist once");

            _provider.ResetCounters();
            _manager.AutoSave = false;
            _manager.NotifyKill("boar");
            Assert.That(_provider.StringWrites(_saveKey), Is.Zero);
        }

        [Test]
        public void AutoSave_ObjectiveListenerThrows_StillCompletesAndPersistsTheQuest()
        {
            QuestConfig quest = CreateQuest("throwing", Objective(QuestObjectiveType.Talk, "npc"));
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            _manager.AutoSave = true;
            int completedEvents = 0;
            _manager.ObjectiveProgress += (config, index, count) => throw new InvalidOperationException("listener");
            _manager.QuestCompleted += config => completedEvents++;
            _provider.ResetCounters();

            InvalidOperationException thrown =
                Assert.Throws<InvalidOperationException>(() => _manager.CompleteObjective(quest, 0));

            Assert.That(thrown.Message, Is.EqualTo("listener"));
            Assert.That(_provider.StringWrites(_saveKey), Is.EqualTo(1));
            Assert.That(_manager.IsCompleted(quest), Is.True, "a throwing objective listener must not skip completion");
            Assert.That(completedEvents, Is.EqualTo(1));

            QuestState live = _manager.GetState(quest);
            _manager.AutoSave = false;
            _manager.Load();

            Assert.That(_manager.GetState(quest), Is.Not.SameAs(live));
            Assert.That(_manager.IsCompleted(quest), Is.True, "the saved state must be completed, not Active-all-done");
        }

        [Test]
        public void ObjectiveAndCompletionListenersThrow_FirstFaultSurfacesAndLaterFaultIsLogged()
        {
            QuestConfig quest = CreateQuest("double_fault", Objective(QuestObjectiveType.KillCount, "orc"));
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            _manager.ObjectiveCompleted += (config, index) => throw new InvalidOperationException("objective listener");
            _manager.QuestCompleted += config => throw new ArgumentException("completion listener");
            bool previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: true);
            try
            {
                LogAssert.Expect(LogType.Exception, new Regex("ArgumentException: completion listener"));

                InvalidOperationException thrown =
                    Assert.Throws<InvalidOperationException>(() => _manager.NotifyKill("orc"));

                Assert.That(thrown.Message, Is.EqualTo("objective listener"));
                Assert.That(_manager.IsCompleted(quest), Is.True);
            }
            finally
            {
                NeoDiagnostics.Configure(errors: previousErrors);
            }
        }

        [Test]
        public void Notify_WithNullOrEmptyTargetId_CreditsNothing()
        {
            QuestConfig quest = CreateQuest("empty_target", new QuestObjectiveData(),
                Objective(QuestObjectiveType.Talk, ""), Objective(QuestObjectiveType.KillCount, ""));
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            Assert.That(_manager.Notify(QuestObjectiveType.CustomCondition, ""), Is.Zero);
            Assert.That(_manager.Notify(QuestObjectiveType.Talk, null), Is.Zero);
            Assert.That(_manager.Notify(QuestObjectiveType.KillCount, "", 5), Is.Zero);
            _manager.NotifyTalk("");
            _manager.NotifyReachPoint(null);
            _manager.NotifyKill("");
            _manager.NotifyCollect(null, 2);

            QuestState state = _manager.GetState(quest);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(state.IsObjectiveCompleted(i), Is.False, $"objective {i}");
                Assert.That(state.GetObjectiveProgress(i), Is.Zero, $"objective {i}");
            }

            Assert.That(_manager.AddObjectiveProgress(quest, 1, 1), Is.True, "index-based credit still reaches it");
        }

        [Test]
        public void TypedEvents_ReportFailResetResetAllAndLoad()
        {
            QuestConfig first = CreateQuest("typed_a", Objective(QuestObjectiveType.Talk, "a"));
            QuestConfig second = CreateQuest("typed_b", Objective(QuestObjectiveType.Talk, "b"));
            List<string> failed = new();
            List<string> reset = new();
            int loaded = 0;
            _manager.QuestFailed += failed.Add;
            _manager.QuestReset += reset.Add;
            _manager.QuestsLoaded += () => loaded++;
            Assert.That(_manager.AcceptQuest(first), Is.True);
            Assert.That(_manager.AcceptQuest(second), Is.True);

            _manager.FailQuest(first);
            _manager.FailQuest(first);
            Assert.That(failed, Is.EqualTo(new[] { "typed_a" }));

            Assert.That(_manager.ResetQuest(first), Is.True);
            Assert.That(_manager.ResetQuest(first), Is.False);
            Assert.That(reset, Is.EqualTo(new[] { "typed_a" }));

            Assert.That(_manager.AcceptQuest(first), Is.True);
            _manager.Save();
            _manager.ResetAllQuests();
            Assert.That(reset, Is.EqualTo(new[] { "typed_a", "typed_b", "typed_a" }));
            Assert.That(_manager.AllQuests, Is.Empty);

            _manager.Load();
            Assert.That(loaded, Is.EqualTo(1));
            Assert.That(_manager.AllQuests.Count, Is.EqualTo(2));
        }

        [Test]
        public void NoCodeNotify_CreditsTargetsAndReportsMisses()
        {
            QuestConfig quest = CreateQuest("bridge_notify", Objective(QuestObjectiveType.KillCount, "orc", 3),
                Objective(QuestObjectiveType.Talk, "smith"));
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestNoCodeAction bridge = _root.AddComponent<QuestNoCodeAction>();
            bridge.Action = QuestNoCodeAction.ActionType.NotifyKill;
            bridge.TargetId = "orc";
            bridge.Amount = 2;
            int successes = 0;
            List<string> failures = new();
            bridge.OnSuccess.AddListener(() => successes++);
            bridge.OnFailed.AddListener(failures.Add);

            bridge.Execute();
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(2));

            bridge.SetTargetId("dragon");
            bridge.Execute();
            bridge.SetTargetId("");
            bridge.Execute();

            bridge.Action = QuestNoCodeAction.ActionType.NotifyTalk;
            bridge.SetTargetId("smith");
            bridge.Execute();

            Assert.That(successes, Is.EqualTo(2));
            Assert.That(failures, Has.Count.EqualTo(2));
            Assert.That(failures[0], Does.Contain("dragon"));
            Assert.That(failures[1], Is.EqualTo("Target id is empty."));
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(1), Is.True);
            Assert.That(bridge.Amount, Is.EqualTo(2));
            bridge.Amount = 0;
            Assert.That(bridge.Amount, Is.EqualTo(1));
        }

        [Test]
        public void NoCodeNotify_RepeatedResults_AllocateNothingWhenWarm()
        {
            QuestConfig quest = CreateQuest("bridge_alloc", Objective(QuestObjectiveType.KillCount, "orc", 1000000));
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestNoCodeAction hit = _root.AddComponent<QuestNoCodeAction>();
            hit.Action = QuestNoCodeAction.ActionType.NotifyKill;
            hit.TargetId = "orc";
            QuestNoCodeAction miss = _root.AddComponent<QuestNoCodeAction>();
            miss.Action = QuestNoCodeAction.ActionType.NotifyKill;
            miss.TargetId = "dragon";
            int successes = 0;
            int failures = 0;
            string lastMessage = null;
            hit.OnSuccess.AddListener(() => successes++);
            hit.OnResultMessage.AddListener(message => lastMessage = message);
            miss.OnFailed.AddListener(reason => failures++);
            hit.Execute();
            miss.Execute();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                hit.Execute();
                miss.Execute();
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(successes, Is.EqualTo(Iterations + 1));
            Assert.That(failures, Is.EqualTo(Iterations + 1));
            Assert.That(lastMessage, Is.EqualTo("KillCount 'orc': 1 objective(s) credited."));
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(Iterations + 1));
        }

        [Test]
        public void NoCodeCompleteObjective_UsesAmountForCounters()
        {
            QuestConfig quest = CreateQuest("bridge_amount", Objective(QuestObjectiveType.CollectCount, "ore", 5));
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestNoCodeAction bridge = _root.AddComponent<QuestNoCodeAction>();
            bridge.Action = QuestNoCodeAction.ActionType.CompleteObjective;
            bridge.Quest = quest;
            bridge.Amount = 3;
            int successes = 0;
            bridge.OnSuccess.AddListener(() => successes++);

            bridge.Execute();
            bridge.Execute();

            Assert.That(successes, Is.EqualTo(2));
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(5));
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [Test]
        public void NoCodeEvaluateObjectiveConditions_CompletesPassingCustomCondition()
        {
            Audit20261023NeoQuestConditionProbe probe = _root.AddComponent<Audit20261023NeoQuestConditionProbe>();
            QuestConfig quest = CreateQuest("bridge_condition",
                new QuestObjectiveData { Condition = ProbeCondition(probe) });
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestNoCodeAction bridge = _root.AddComponent<QuestNoCodeAction>();
            bridge.Action = QuestNoCodeAction.ActionType.EvaluateObjectiveConditions;
            int successes = 0;
            bridge.OnSuccess.AddListener(() => successes++);

            bridge.Execute();

            Assert.That(successes, Is.EqualTo(1));
            Assert.That(probe.ReadCount, Is.EqualTo(1));
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [Test]
        public void NoCodeAccept_WithFlowGate_EvaluatesStartConditionsOnceAndKeepsChainLock()
        {
            Audit20261023NeoQuestConditionProbe probe = _root.AddComponent<Audit20261023NeoQuestConditionProbe>();
            QuestConfig first = CreateQuest("flow_first", Objective(QuestObjectiveType.Talk, "a"));
            QuestConfig second = CreateQuest("flow_second", Objective(QuestObjectiveType.Talk, "b"));
            SetField(second, "_startConditions", new List<ConditionEntry> { ProbeCondition(probe) });
            QuestNoCodeAction bridge = _root.AddComponent<QuestNoCodeAction>();
            bridge.Action = QuestNoCodeAction.ActionType.Accept;
            bridge.Quest = second;
            bridge.FlowConfig = CreateFlow(first, second);
            int successes = 0;
            List<string> failures = new();
            bridge.OnSuccess.AddListener(() => successes++);
            bridge.OnFailed.AddListener(failures.Add);

            bridge.Execute();

            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0], Does.Contain("flow_first"));
            Assert.That(probe.ReadCount, Is.Zero, "a chain lock must not run start-condition predicates");

            Assert.That(_manager.AcceptQuest(first), Is.True);
            _manager.CompleteObjective(first, 0);
            bridge.Execute();

            Assert.That(successes, Is.EqualTo(1));
            Assert.That(probe.ReadCount, Is.EqualTo(1), "start conditions are evaluated once per accept");
            Assert.That(_manager.IsActive(second), Is.True);
            Assert.That(bridge.FlowConfig.IsChainSatisfied(_manager, second, out string reason), Is.True, reason);
        }

        [Test]
        public void TryAcceptQuest_ByConfig_ReportsReasons()
        {
            QuestConfig quest = CreateQuest("try_config", Objective(QuestObjectiveType.Talk, "a"));

            Assert.That(_manager.TryAcceptQuest(quest, out string reason), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(_manager.TryAcceptQuest(quest, out reason), Is.False);
            Assert.That(reason, Is.EqualTo("Already accepted or completed."));
            Assert.That(_manager.TryAcceptQuest((QuestConfig)null, out reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void ValidateDefinition_ReportsEmptyIdMissingTargetsAndEmptyQuests()
        {
            QuestConfig valid = CreateQuest("valid", Objective(QuestObjectiveType.KillCount, "orc", 2),
                new QuestObjectiveData());
            QuestConfig broken = CreateQuest("", Objective(QuestObjectiveType.CollectCount, "", 2),
                Objective(QuestObjectiveType.Talk, ""));
            QuestConfig empty = CreateQuest("empty");
            QuestConfig blank = CreateQuest("  ", Objective(QuestObjectiveType.Talk, "a"));

            Assert.That(valid.ValidateDefinition(), Is.Empty);
            Assert.That(broken.ValidateDefinition(), Has.Count.EqualTo(3));
            Assert.That(empty.ValidateDefinition(), Has.Count.EqualTo(1));
            IReadOnlyList<string> blankIssues = blank.ValidateDefinition();
            Assert.That(blankIssues, Has.Count.EqualTo(1));
            Assert.That(blankIssues[0], Does.Contain("whitespace"));
            Assert.That(new QuestObjectiveData { Type = QuestObjectiveType.KillCount, RequiredCount = 0 }.TargetProgress,
                Is.EqualTo(1));
            Assert.That(new QuestObjectiveData { Type = QuestObjectiveType.Talk, RequiredCount = 9 }.TargetProgress,
                Is.EqualTo(1));
        }

        private QuestConfig CreateQuest(string id, params QuestObjectiveData[] objectives)
        {
            QuestConfig quest = ScriptableObject.CreateInstance<QuestConfig>();
            _assets.Add(quest);
            SetField(quest, "_id", id);
            SetField(quest, "_objectives", new List<QuestObjectiveData>(objectives));
            return quest;
        }

        private QuestFlowConfig CreateFlow(params QuestConfig[] quests)
        {
            QuestFlowConfig flow = ScriptableObject.CreateInstance<QuestFlowConfig>();
            _assets.Add(flow);
            QuestFlowConfig.QuestChain chain = new();
            SetField(chain, "_quests", new List<QuestConfig>(quests));
            SetField(flow, "_chains", new List<QuestFlowConfig.QuestChain> { chain });
            return flow;
        }

        private static QuestObjectiveData Objective(QuestObjectiveType type, string targetId, int requiredCount = 1)
        {
            return new QuestObjectiveData { Type = type, TargetId = targetId, RequiredCount = requiredCount };
        }

        private static ConditionEntry ProbeCondition(Audit20261023NeoQuestConditionProbe probe)
        {
            return new ConditionEntry
            {
                Source = SourceMode.Component,
                SourceObject = probe.gameObject,
                ComponentTypeName = typeof(Audit20261023NeoQuestConditionProbe).FullName,
                PropertyName = nameof(Audit20261023NeoQuestConditionProbe.Value),
                CurrentValueType = Neo.Condition.ValueType.Bool,
                Compare = CompareOp.Equal,
                ThresholdBool = true
            };
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Serialized field {name} is required by this fixture.");
            field.SetValue(target, value);
        }

        private sealed class CountingSaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _stringWrites = new(StringComparer.Ordinal);

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int StringWrites(string key)
            {
                return _stringWrites.TryGetValue(key, out int count) ? count : 0;
            }

            public void ResetCounters()
            {
                _stringWrites.Clear();
            }

            public int GetInt(string key, int defaultValue = 0)
            {
                return _values.TryGetValue(key, out object value) && value is int result ? result : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                SetValue(key, value);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _values.TryGetValue(key, out object value) && value is float result ? result : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                SetValue(key, value);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _values.TryGetValue(key, out object value) && value is string result ? result : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _stringWrites[key] = StringWrites(key) + 1;
                SetValue(key, value);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _values.TryGetValue(key, out object value) && value is bool result ? result : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                SetValue(key, value);
            }

            public bool HasKey(string key)
            {
                return _values.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll()
            {
                _values.Clear();
            }

            public void Save()
            {
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }

            private void SetValue(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
