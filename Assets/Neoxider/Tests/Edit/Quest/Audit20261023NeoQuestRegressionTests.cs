using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using Neo.Quest;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoQuestRegressionTests
    {
        private readonly List<ScriptableObject> _assets = new();
        private GameObject _root;
        private QuestManager _manager;
        private string _saveKey;

        [SetUp]
        public void SetUp()
        {
            _saveKey = "Audit20261023NeoQuest_" + Guid.NewGuid().ToString("N");
            _root = new GameObject("Audit20261023NeoQuest");
            _root.SetActive(false);
            _manager = _root.AddComponent<QuestManager>();
            SetField(_manager, "_autoLoad", false);
            SetField(_manager, "_autoSave", false);
            SetField(_manager, "_saveKey", _saveKey);
            _manager.ConditionContext = _root;
            // Initialize only after persistence is isolated, then keep the condition context inactive.
            _root.SetActive(true);
            _root.SetActive(false);
            Assert.That(QuestManager.Instance, Is.SameAs(_manager));
        }

        [TearDown]
        public void TearDown()
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
            SaveProvider.DeleteKey(_saveKey);
        }

        [TestCase(QuestObjectiveType.CustomCondition)]
        [TestCase(QuestObjectiveType.ReachPoint)]
        [TestCase(QuestObjectiveType.Talk)]
        public void BinaryObjective_StoresReportedProgressAndPersistsIt(QuestObjectiveType type)
        {
            QuestConfig quest = CreateQuest("binary", new QuestObjectiveData { Type = type });
            int progressInCallback = -1;
            _manager.OnObjectiveProgress.AddListener((id, index, count) =>
            {
                progressInCallback = _manager.GetObjectiveProgress(quest, index);
                Assert.That(count, Is.EqualTo(1));
            });
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            _manager.CompleteObjective(quest, 0);
            _manager.Save();
            _manager.Load();

            Assert.That(progressInCallback, Is.EqualTo(1));
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(1));
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(0), Is.True);
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CounterProgressCallback_RepeatedCreditCompletesOnlyOnce(bool useCSharpEvent)
        {
            QuestConfig quest = CreateQuest("counter", Counter(QuestObjectiveType.KillCount));
            bool reentered = false;
            int progressEvents = 0;
            int objectiveEvents = 0;
            int questEvents = 0;
            Action<QuestConfig, int, int> callback = (config, index, count) =>
            {
                progressEvents++;
                if (!reentered)
                {
                    reentered = true;
                    _manager.CompleteObjective(quest, index);
                }
            };
            if (useCSharpEvent)
            {
                _manager.ObjectiveProgress += callback;
            }
            else
            {
                _manager.OnObjectiveProgress.AddListener((id, index, count) => callback(quest, index, count));
            }

            _manager.OnObjectiveCompleted.AddListener((id, index) => objectiveEvents++);
            _manager.OnQuestCompleted.AddListener(id => questEvents++);
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            _manager.CompleteObjective(quest, 0);

            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(1));
            Assert.That(progressEvents, Is.EqualTo(1));
            Assert.That(objectiveEvents, Is.EqualTo(1));
            Assert.That(questEvents, Is.EqualTo(1));
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [Test]
        public void ObjectiveCompletedCallback_CompletesAnotherObjectiveWithoutDuplicateQuestRewards()
        {
            QuestConfig quest = CreateQuest("nested", new QuestObjectiveData(), new QuestObjectiveData());
            int objectiveEvents = 0;
            int questEvents = 0;
            _manager.OnObjectiveCompleted.AddListener((id, index) =>
            {
                objectiveEvents++;
                if (index == 0)
                {
                    _manager.CompleteObjective(quest, 1);
                }
            });
            _manager.OnQuestCompleted.AddListener(id => questEvents++);
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            _manager.CompleteObjective(quest, 0);

            Assert.That(objectiveEvents, Is.EqualTo(2));
            Assert.That(questEvents, Is.EqualTo(1));
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(1), Is.True);
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [Test]
        public void ProgressCallback_FailsQuestWithoutCompletionOverwritingFailure()
        {
            QuestConfig quest = CreateQuest("failed", new QuestObjectiveData());
            int completionEvents = 0;
            _manager.OnObjectiveProgress.AddListener((id, index, count) => _manager.FailQuest(quest));
            _manager.OnQuestCompleted.AddListener(id => completionEvents++);
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            _manager.CompleteObjective(quest, 0);

            Assert.That(_manager.IsFailed(quest), Is.True);
            Assert.That(completionEvents, Is.Zero);
        }

        [Test]
        public void ProgressCallback_RestartsQuestWithoutCompletingDetachedState()
        {
            QuestConfig quest = CreateQuest("restarted", new QuestObjectiveData());
            int completionEvents = 0;
            _manager.OnObjectiveProgress.AddListener((id, index, count) => _manager.RestartQuest(quest));
            _manager.OnQuestCompleted.AddListener(id => completionEvents++);
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestState original = _manager.GetState(quest);

            _manager.CompleteObjective(quest, 0);

            Assert.That(_manager.GetState(quest), Is.Not.SameAs(original));
            Assert.That(_manager.IsActive(quest), Is.True);
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(0), Is.False);
            Assert.That(completionEvents, Is.Zero);
        }

        [TestCase(QuestObjectiveType.KillCount)]
        [TestCase(QuestObjectiveType.CollectCount)]
        [TestCase(QuestObjectiveType.CustomCondition)]
        public void Notification_RestartedQuestDoesNotReceiveRemainderOfOldNotification(QuestObjectiveType type)
        {
            QuestObjectiveData first = Counter(type);
            QuestObjectiveData second = Counter(type);
            first.Condition = ContextActiveCondition(false);
            second.Condition = ContextActiveCondition(false);
            QuestConfig quest = CreateQuest("notification", first, second);
            _manager.OnObjectiveCompleted.AddListener((id, index) =>
            {
                if (index == 0)
                {
                    Assert.That(_manager.RestartQuest(quest), Is.True);
                }
            });
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestState original = _manager.GetState(quest);

            if (type == QuestObjectiveType.KillCount)
            {
                _manager.NotifyKill("target");
            }
            else if (type == QuestObjectiveType.CollectCount)
            {
                _manager.NotifyCollect("target");
            }
            else
            {
                _manager.EvaluateObjectiveConditions();
            }

            QuestState replacement = _manager.GetState(quest);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(replacement.Status, Is.EqualTo(QuestStatus.Active));
            Assert.That(replacement.GetObjectiveProgress(0), Is.Zero);
            Assert.That(replacement.GetObjectiveProgress(1), Is.Zero);
            Assert.That(replacement.IsObjectiveCompleted(1), Is.False);
        }

        [Test]
        public void Load_NormalizesDuplicateIdsAndUnequalListsWhileKeepingUnknownQuests()
        {
            QuestObjectiveData counter = Counter(QuestObjectiveType.CollectCount);
            counter.RequiredCount = 3;
            QuestConfig quest = CreateQuest("loaded", counter, new QuestObjectiveData());
            _manager.RegisterQuest(quest);
            SaveProvider.SetString(_saveKey,
                "{\"States\":[" +
                "{\"_questId\":\"loaded\",\"_status\":1,\"_objectiveProgress\":[2],\"_objectiveCompleted\":[]}," +
                "{\"_questId\":\"loaded\",\"_status\":1,\"_objectiveProgress\":[99],\"_objectiveCompleted\":[]}," +
                "{\"_questId\":\"\",\"_status\":1,\"_objectiveProgress\":[],\"_objectiveCompleted\":[]}," +
                "{\"_questId\":\"unknown\",\"_status\":1,\"_objectiveProgress\":null,\"_objectiveCompleted\":[true]}]}");

            _manager.Load();

            Assert.That(_manager.AllQuests.Count, Is.EqualTo(2));
            Assert.That(_manager.ActiveQuests.Count, Is.EqualTo(2));
            Assert.That(_manager.GetState(quest).ObjectiveCount, Is.EqualTo(2));
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(2));
            Assert.That(_manager.GetState("unknown").ObjectiveCount, Is.EqualTo(1));
            Assert.That(_manager.GetState("unknown").IsObjectiveCompleted(0), Is.True);
            _manager.CompleteObjective(quest, 0);
            _manager.CompleteObjective(quest, 1);
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [Test]
        public void RegisterQuest_AfterLoadExpandsStateForRuntimeConfig()
        {
            SaveProvider.SetString(_saveKey,
                "{\"States\":[{\"_questId\":\"late\",\"_status\":1,\"_objectiveProgress\":[0],\"_objectiveCompleted\":[false]}]}");
            _manager.Load();
            QuestConfig quest = CreateQuest("late", new QuestObjectiveData(), new QuestObjectiveData());

            _manager.RegisterQuest(quest);
            _manager.CompleteObjective(quest, 0);
            _manager.CompleteObjective(quest, 1);

            Assert.That(_manager.GetState(quest).ObjectiveCount, Is.EqualTo(2));
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(1), Is.True);
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [Test]
        public void Load_LegacyCompletedBinaryObjectiveReconstructsProgress()
        {
            QuestConfig quest = CreateQuest("legacy", new QuestObjectiveData { Type = QuestObjectiveType.Talk });
            _manager.RegisterQuest(quest);
            SaveProvider.SetString(_saveKey,
                "{\"States\":[{\"_questId\":\"legacy\",\"_status\":2,\"_objectiveProgress\":[0],\"_objectiveCompleted\":[true]}]}");

            _manager.Load();

            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(1));
            Assert.That(_manager.IsCompleted(quest), Is.True);
        }

        [TestCase(QuestObjectiveType.CustomCondition, 0, 1)]
        [TestCase(QuestObjectiveType.ReachPoint, 0, 1)]
        [TestCase(QuestObjectiveType.Talk, 0, 1)]
        [TestCase(QuestObjectiveType.KillCount, 0, 0)]
        [TestCase(QuestObjectiveType.KillCount, 4, 4)]
        [TestCase(QuestObjectiveType.CollectCount, 0, 0)]
        [TestCase(QuestObjectiveType.CollectCount, 4, 4)]
        public void RegisterQuest_AfterLoadReconcilesBinaryProgressWithoutEventsOrSaving(
            QuestObjectiveType type, int savedProgress, int expectedProgress)
        {
            string json = "{\"States\":[{\"_questId\":\"late_legacy\",\"_status\":2," +
                          "\"_objectiveProgress\":[" + savedProgress + "],\"_objectiveCompleted\":[true]}]}";
            SaveProvider.SetString(_saveKey, json);
            _manager.Load();
            QuestState original = _manager.GetState("late_legacy");
            Assert.That(original.GetObjectiveProgress(0), Is.EqualTo(savedProgress));
            QuestConfig quest = CreateQuest("late_legacy",
                new QuestObjectiveData { Type = type, RequiredCount = 10 }, new QuestObjectiveData());
            int events = 0;
            ObserveGameplayEvents(() => events++);
            SetField(_manager, "_autoSave", true);

            _manager.RegisterQuest(quest);
            _manager.RegisterQuest(quest);

            Assert.That(_manager.GetState(quest), Is.SameAs(original));
            Assert.That(original.Status, Is.EqualTo(QuestStatus.Completed));
            Assert.That(original.ObjectiveCount, Is.EqualTo(2));
            Assert.That(original.GetObjectiveProgress(0), Is.EqualTo(expectedProgress));
            Assert.That(original.IsObjectiveCompleted(0), Is.True);
            Assert.That(original.GetObjectiveProgress(1), Is.Zero);
            Assert.That(original.IsObjectiveCompleted(1), Is.False);
            Assert.That(_manager.KnownQuests.Count, Is.EqualTo(1));
            Assert.That(events, Is.Zero);
            Assert.That(SaveProvider.GetString(_saveKey, "missing"), Is.EqualTo(json));
        }

        [TestCase(QuestStatus.Active)]
        [TestCase(QuestStatus.Completed)]
        [TestCase(QuestStatus.Failed)]
        public void UnregisterQuest_RequiresCurrentInstanceAndPreservesStateAndSave(QuestStatus status)
        {
            QuestConfig owned = CreateQuest("owned",
                new QuestObjectiveData { Type = QuestObjectiveType.CollectCount, RequiredCount = 3 });
            QuestConfig replacement = CreateQuest("owned",
                new QuestObjectiveData { Type = QuestObjectiveType.CollectCount, RequiredCount = 3 });
            Assert.That(_manager.AcceptQuest(owned), Is.True);
            _manager.CompleteObjective(owned, 0);
            if (status == QuestStatus.Completed)
            {
                _manager.CompleteObjective(owned, 0);
                _manager.CompleteObjective(owned, 0);
            }
            else if (status == QuestStatus.Failed)
            {
                _manager.FailQuest(owned);
            }

            QuestState original = _manager.GetState(owned);
            int progress = original.GetObjectiveProgress(0);
            bool completed = original.IsObjectiveCompleted(0);
            _manager.Save();
            string json = SaveProvider.GetString(_saveKey, "missing");
            int events = 0;
            ObserveGameplayEvents(() => events++);
            SetField(_manager, "_autoSave", true);

            _manager.RegisterQuest(replacement);
            Assert.That(_manager.GetConfig(owned.Id), Is.SameAs(owned));
            Assert.That(_manager.UnregisterQuest(null), Is.False);
            Assert.That(_manager.UnregisterQuest(replacement), Is.False);
            Assert.That(_manager.GetConfig(owned.Id), Is.SameAs(owned));
            Assert.That(_manager.UnregisterQuest(owned), Is.True);
            Assert.That(_manager.GetConfig(owned.Id), Is.Null);
            Assert.That(_manager.KnownQuests.Count, Is.Zero);
            Assert.That(_manager.UnregisterQuest(owned), Is.False);
            Assert.That(_manager.GetState(owned), Is.SameAs(original));
            Assert.That(_manager.AllQuests.Count, Is.EqualTo(1));
            Assert.That(original.Status, Is.EqualTo(status));
            Assert.That(original.GetObjectiveProgress(0), Is.EqualTo(progress));
            Assert.That(original.IsObjectiveCompleted(0), Is.EqualTo(completed));

            _manager.RegisterQuest(replacement);
            Assert.That(_manager.UnregisterQuest(owned), Is.False);
            UnityEngine.Object.DestroyImmediate(owned);
            Assert.That(_manager.UnregisterQuest(owned), Is.False);
            Assert.That(_manager.GetConfig(replacement.Id), Is.SameAs(replacement));
            Assert.That(_manager.GetState(replacement), Is.SameAs(original));
            Assert.That(_manager.KnownQuests.Count, Is.EqualTo(1));
            Assert.That(original.Status, Is.EqualTo(status));
            Assert.That(original.GetObjectiveProgress(0), Is.EqualTo(progress));
            Assert.That(original.IsObjectiveCompleted(0), Is.EqualTo(completed));
            Assert.That(events, Is.Zero);
            Assert.That(SaveProvider.GetString(_saveKey, "missing"), Is.EqualTo(json));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ObjectivePredicate_RestartsQuestWithoutCreditingReplacement(bool predicateResult)
        {
            Audit20261023NeoQuestConditionProbe probe = _root.AddComponent<Audit20261023NeoQuestConditionProbe>();
            probe.Result = predicateResult;
            ConditionEntry condition = ProbeCondition(probe);
            QuestConfig quest = CreateQuest("predicate_restart",
                new QuestObjectiveData { Condition = condition }, new QuestObjectiveData { Condition = condition });
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestState original = _manager.GetState(quest);
            bool restarted = false;
            probe.OnRead = () => restarted = _manager.RestartQuest(quest);
            int progressEvents = 0;
            int objectiveEvents = 0;
            int completionEvents = 0;
            int typedCompletionEvents = 0;
            _manager.OnObjectiveProgress.AddListener((id, index, count) => progressEvents++);
            _manager.ObjectiveProgress += (config, index, count) => progressEvents++;
            _manager.OnObjectiveCompleted.AddListener((id, index) => objectiveEvents++);
            _manager.OnQuestCompleted.AddListener(id => completionEvents++);
            _manager.QuestCompleted += config => typedCompletionEvents++;

            _manager.EvaluateObjectiveConditions();

            QuestState replacement = _manager.GetState(quest);
            Assert.That(restarted, Is.True);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(replacement.Status, Is.EqualTo(QuestStatus.Active));
            Assert.That(replacement.GetObjectiveProgress(0), Is.Zero);
            Assert.That(replacement.GetObjectiveProgress(1), Is.Zero);
            Assert.That(replacement.IsObjectiveCompleted(0), Is.False);
            Assert.That(replacement.IsObjectiveCompleted(1), Is.False);
            Assert.That(probe.ReadCount, Is.EqualTo(1));
            Assert.That(progressEvents, Is.Zero);
            Assert.That(objectiveEvents, Is.Zero);
            Assert.That(completionEvents, Is.Zero);
            Assert.That(typedCompletionEvents, Is.Zero);

            probe.Result = true;
            _manager.EvaluateObjectiveConditions();

            Assert.That(_manager.IsCompleted(quest), Is.True);
            Assert.That(probe.ReadCount, Is.EqualTo(3));
            Assert.That(progressEvents, Is.EqualTo(4));
            Assert.That(objectiveEvents, Is.EqualTo(2));
            Assert.That(completionEvents, Is.EqualTo(1));
            Assert.That(typedCompletionEvents, Is.EqualTo(1));
        }

        [TestCase("config")]
        [TestCase("id")]
        [TestCase("try")]
        public void Acceptance_StartPredicateNestedAcceptPublishesOneState(string entryPoint)
        {
            Audit20261023NeoQuestConditionProbe probe = _root.AddComponent<Audit20261023NeoQuestConditionProbe>();
            QuestConfig quest = CreateQuest("predicate_accept", new QuestObjectiveData());
            SetField(quest, "_startConditions", new List<ConditionEntry> { ProbeCondition(probe) });
            _manager.RegisterQuest(quest);
            bool nestedAccepted = false;
            probe.OnRead = () => nestedAccepted = _manager.AcceptQuest(quest);
            List<string> events = ObserveAcceptanceOrder();
            string reason = null;
            bool accepted;

            switch (entryPoint)
            {
                case "id":
                    accepted = _manager.AcceptQuest(quest.Id);
                    break;
                case "try":
                    accepted = _manager.TryAcceptQuest(quest.Id, out reason);
                    break;
                default:
                    accepted = _manager.AcceptQuest(quest);
                    break;
            }

            Assert.That(nestedAccepted, Is.True);
            Assert.That(accepted, Is.False);
            Assert.That(_manager.AllQuests.Count, Is.EqualTo(1));
            Assert.That(_manager.ActiveQuests.Count, Is.EqualTo(1));
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.Zero);
            Assert.That(probe.ReadCount, Is.EqualTo(2));
            Assert.That(events, Is.EqualTo(new[] { "quest", "any", "typed" }));
            if (entryPoint == "try")
            {
                Assert.That(reason, Is.EqualTo("Already accepted or completed."));
            }
        }

        [Test]
        public void CanAcceptQuest_StartPredicateCreatedStateMakesAvailabilityFalse()
        {
            Audit20261023NeoQuestConditionProbe probe = _root.AddComponent<Audit20261023NeoQuestConditionProbe>();
            QuestConfig quest = CreateQuest("predicate_availability", new QuestObjectiveData());
            SetField(quest, "_startConditions", new List<ConditionEntry> { ProbeCondition(probe) });
            bool nestedAccepted = false;
            probe.OnRead = () => nestedAccepted = _manager.AcceptQuest(quest);
            List<string> events = ObserveAcceptanceOrder();

            bool available = _manager.CanAcceptQuest(quest, out string reason);

            Assert.That(nestedAccepted, Is.True);
            Assert.That(available, Is.False);
            Assert.That(reason, Is.EqualTo("Already accepted or completed."));
            Assert.That(_manager.AllQuests.Count, Is.EqualTo(1));
            Assert.That(_manager.IsActive(quest), Is.True);
            Assert.That(probe.ReadCount, Is.EqualTo(2));
            Assert.That(events, Is.EqualTo(new[] { "quest", "any", "typed" }));
            Assert.That(_manager.CanAcceptQuest(quest, out reason), Is.False);
            Assert.That(probe.ReadCount, Is.EqualTo(2));
        }

        [Test]
        public void TryAcceptQuest_EvaluatesStartPredicateOnceAndPreservesAcceptanceOrder()
        {
            Audit20261023NeoQuestConditionProbe probe = _root.AddComponent<Audit20261023NeoQuestConditionProbe>();
            QuestConfig quest = CreateQuest("predicate_once", new QuestObjectiveData());
            SetField(quest, "_startConditions", new List<ConditionEntry> { ProbeCondition(probe) });
            _manager.RegisterQuest(quest);
            List<string> events = ObserveAcceptanceOrder();

            bool accepted = _manager.TryAcceptQuest(quest.Id, out string reason);

            Assert.That(accepted, Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(probe.ReadCount, Is.EqualTo(1));
            Assert.That(_manager.AllQuests.Count, Is.EqualTo(1));
            Assert.That(_manager.IsActive(quest), Is.True);
            Assert.That(events, Is.EqualTo(new[] { "quest", "any", "typed" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FlowGate_RejectsManagerIneligibleQuest(bool emptyId)
        {
            QuestConfig quest = CreateQuest(emptyId ? "" : "locked", new QuestObjectiveData());
            if (!emptyId)
            {
                SetField(quest, "_startConditions", new List<ConditionEntry> { ContextActiveCondition(true) });
            }

            QuestFlowConfig flow = CreateFlow(quest);

            Assert.That(flow.CanAcceptQuest(_manager, quest, out string reason), Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);
            Assert.That(_manager.GetState(quest), Is.Null);
        }

        [Test]
        public void StrictChain_NullSlotDoesNotUnlockUnfinishedPredecessor()
        {
            QuestConfig first = CreateQuest("first", new QuestObjectiveData());
            QuestConfig last = CreateQuest("last", new QuestObjectiveData());
            QuestFlowConfig flow = CreateFlow(first, null, last);

            Assert.That(flow.TryGetPreviousInStrictChain(last, out QuestConfig previous), Is.True);
            Assert.That(previous, Is.SameAs(first));
            Assert.That(flow.BuildOrderedQuestList(), Is.EqualTo(new[] { first, last }));
            Assert.That(flow.CanAcceptQuest(_manager, last, out string reason), Is.False);
            Assert.That(reason, Does.Contain(first.Id));
            Assert.That(_manager.AcceptQuest(first), Is.True);
            _manager.CompleteObjective(first, 0);
            Assert.That(flow.CanAcceptQuest(_manager, last, out reason), Is.True);
            Assert.That(reason, Is.Null);
        }

        [TestCase(QuestNoCodeAction.ActionType.CompleteObjective, false)]
        [TestCase(QuestNoCodeAction.ActionType.CompleteObjective, true)]
        [TestCase(QuestNoCodeAction.ActionType.Fail, false)]
        [TestCase(QuestNoCodeAction.ActionType.Fail, true)]
        public void NoCodeAction_InactiveQuestEmitsFailure(QuestNoCodeAction.ActionType action, bool completed)
        {
            QuestConfig quest = CreateQuest("bridge", new QuestObjectiveData());
            if (completed)
            {
                Assert.That(_manager.AcceptQuest(quest), Is.True);
                _manager.CompleteObjective(quest, 0);
            }

            AssertActionFailure(quest, action, 0);
            Assert.That(_manager.IsCompleted(quest), Is.EqualTo(completed));
            Assert.That(_manager.IsFailed(quest), Is.False);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void NoCodeAction_InvalidObjectiveEmitsFailure(int index)
        {
            QuestConfig quest = CreateQuest("bridge_index", new QuestObjectiveData());
            Assert.That(_manager.AcceptQuest(quest), Is.True);

            AssertActionFailure(quest, QuestNoCodeAction.ActionType.CompleteObjective, index);

            Assert.That(_manager.IsActive(quest), Is.True);
            Assert.That(_manager.GetState(quest).IsObjectiveCompleted(0), Is.False);
        }

        [Test]
        public void NoCodeAction_AlreadyCompletedObjectiveDoesNotEmitSuccess()
        {
            QuestConfig quest = CreateQuest("bridge_done", new QuestObjectiveData(), new QuestObjectiveData());
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            _manager.CompleteObjective(quest, 0);

            AssertActionFailure(quest, QuestNoCodeAction.ActionType.CompleteObjective, 0);

            Assert.That(_manager.IsActive(quest), Is.True);
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(1));
        }

        [Test]
        public void NoCodeAction_CounterCreditsUntilCompleteThenRejectsFurtherCredit()
        {
            QuestObjectiveData objective = Counter(QuestObjectiveType.CollectCount);
            objective.RequiredCount = 2;
            QuestConfig quest = CreateQuest("bridge_counter", objective);
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestNoCodeAction bridge = _root.AddComponent<QuestNoCodeAction>();
            bridge.Quest = quest;
            bridge.Action = QuestNoCodeAction.ActionType.CompleteObjective;
            int successes = 0;
            int failures = 0;
            bridge.OnSuccess.AddListener(() => successes++);
            bridge.OnFailed.AddListener(reason => failures++);

            bridge.Execute();
            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(1));
            Assert.That(_manager.IsActive(quest), Is.True);
            bridge.Execute();
            bridge.Execute();

            Assert.That(_manager.GetObjectiveProgress(quest, 0), Is.EqualTo(2));
            Assert.That(_manager.IsCompleted(quest), Is.True);
            Assert.That(successes, Is.EqualTo(2));
            Assert.That(failures, Is.EqualTo(1));
        }

        [Test]
        public void NoCodeAction_FailActiveQuestSucceedsOnlyOnce()
        {
            QuestConfig quest = CreateQuest("bridge_fail", new QuestObjectiveData());
            Assert.That(_manager.AcceptQuest(quest), Is.True);
            QuestNoCodeAction bridge = _root.AddComponent<QuestNoCodeAction>();
            bridge.Quest = quest;
            bridge.Action = QuestNoCodeAction.ActionType.Fail;
            int successes = 0;
            int failures = 0;
            bridge.OnSuccess.AddListener(() => successes++);
            bridge.OnFailed.AddListener(reason => failures++);

            bridge.Execute();
            bridge.Execute();

            Assert.That(_manager.IsFailed(quest), Is.True);
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(failures, Is.EqualTo(1));
        }

        [Test]
        public void NoCodeAction_UnknownActionEmitsFailure()
        {
            QuestConfig quest = CreateQuest("bridge_unknown", new QuestObjectiveData());

            AssertActionFailure(quest, (QuestNoCodeAction.ActionType)int.MaxValue, 0);

            Assert.That(_manager.GetState(quest), Is.Null);
        }

        private void AssertActionFailure(QuestConfig quest, QuestNoCodeAction.ActionType action, int index)
        {
            QuestNoCodeAction bridge = _root.AddComponent<QuestNoCodeAction>();
            bridge.Quest = quest;
            bridge.Action = action;
            bridge.ObjectiveIndex = index;
            int successes = 0;
            int failures = 0;
            string result = null;
            bridge.OnSuccess.AddListener(() => successes++);
            bridge.OnFailed.AddListener(reason => failures++);
            bridge.OnResultMessage.AddListener(message => result = message);

            bridge.Execute();

            Assert.That(successes, Is.Zero);
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(result, Is.Not.Null.And.Not.Empty);
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

        private static QuestObjectiveData Counter(QuestObjectiveType type)
        {
            return new QuestObjectiveData { Type = type, TargetId = "target", RequiredCount = 1 };
        }

        private static ConditionEntry ContextActiveCondition(bool required)
        {
            return new ConditionEntry
            {
                Source = SourceMode.GameObject,
                PropertyName = nameof(GameObject.activeSelf),
                CurrentValueType = Neo.Condition.ValueType.Bool,
                Compare = CompareOp.Equal,
                ThresholdBool = required
            };
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

        private List<string> ObserveAcceptanceOrder()
        {
            List<string> events = new();
            _manager.OnQuestAccepted.AddListener(id => events.Add("quest"));
            _manager.OnAnyQuestAccepted.AddListener(() => events.Add("any"));
            _manager.QuestAccepted += config => events.Add("typed");
            return events;
        }

        private void ObserveGameplayEvents(Action onEvent)
        {
            _manager.OnQuestAccepted.AddListener(id => onEvent());
            _manager.OnAnyQuestAccepted.AddListener(() => onEvent());
            _manager.QuestAccepted += config => onEvent();
            _manager.OnObjectiveProgress.AddListener((id, index, count) => onEvent());
            _manager.ObjectiveProgress += (config, index, count) => onEvent();
            _manager.OnObjectiveCompleted.AddListener((id, index) => onEvent());
            _manager.OnQuestCompleted.AddListener(id => onEvent());
            _manager.OnAnyQuestCompleted.AddListener(() => onEvent());
            _manager.QuestCompleted += config => onEvent();
            _manager.OnQuestFailed.AddListener(id => onEvent());
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing serialized authoring field: " + name);
            field.SetValue(target, value);
        }
    }
}
