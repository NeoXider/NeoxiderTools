using System.Collections.Generic;
using System.Reflection;
using Neo.Quest;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class QuestManagerTests
    {
        private GameObject _go;
        private QuestManager _questManager;
        private QuestConfig _questConfig;

        private static QuestConfig CreateTestConfig(string id, int objectiveCount = 1)
        {
            QuestConfig config = ScriptableObject.CreateInstance<QuestConfig>();
            // WHY: _id is private serialized — inject via reflection
            typeof(QuestConfig)
                .GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(config, id);
            var objectives = new List<QuestObjectiveData>();
            for (int i = 0; i < objectiveCount; i++)
            {
                objectives.Add(new QuestObjectiveData());
            }

            typeof(QuestConfig)
                .GetField("_objectives", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(config, objectives);
            return config;
        }

        [SetUp]
        public void SetUp()
        {
            SaveProvider.DeleteAll();

            _go = new GameObject("QuestManagerTests");
            _questManager = _go.AddComponent<QuestManager>();

            // WHY: Disable autoLoad to prevent SaveProvider lookups during Init
            typeof(QuestManager)
                .GetField("_autoLoad", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_questManager, false);
            typeof(QuestManager)
                .GetField("_autoSave", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_questManager, false);

            _questConfig = CreateTestConfig("test_quest", 2);
            var knownQuests = typeof(QuestManager)
                .GetField("_knownQuests", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(_questManager) as List<QuestConfig>;
            knownQuests?.Add(_questConfig);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }

            if (_questConfig != null)
            {
                Object.DestroyImmediate(_questConfig);
            }

            // WHY: Reset singleton to avoid cross-test contamination
            ResetQuestManagerSingleton();
            SaveProvider.DeleteAll();
        }

        [Test]
        public void AcceptQuest_ByConfig_AddsQuestAndFiresEvent()
        {
            bool eventFired = false;
            _questManager.OnQuestAccepted.AddListener(q => eventFired = true);

            bool result = _questManager.AcceptQuest(_questConfig);

            Assert.IsTrue(result, "AcceptQuest should return true for a new quest");
            Assert.IsTrue(eventFired, "OnQuestAccepted event should fire");

            QuestState state = _questManager.GetState(_questConfig);
            Assert.IsNotNull(state, "GetState should return the quest state after acceptance");
            Assert.AreEqual(QuestStatus.Active, state.Status);
        }

        [Test]
        public void AcceptQuest_ByString_FindsConfigAndAccepts()
        {
            bool result = _questManager.AcceptQuest("test_quest");

            Assert.IsTrue(result);
            QuestState state = _questManager.GetState("test_quest");
            Assert.IsNotNull(state);
            Assert.AreEqual(QuestStatus.Active, state.Status);
        }

        [Test]
        public void AcceptQuest_DuplicateId_ReturnsFalse()
        {
            _questManager.AcceptQuest(_questConfig);
            bool second = _questManager.AcceptQuest(_questConfig);

            Assert.IsFalse(second, "Should not accept the same quest twice");
        }

        [Test]
        public void CompleteObjective_ByConfig_AdvancesProgress()
        {
            _questManager.AcceptQuest(_questConfig);
            _questManager.CompleteObjective(_questConfig, 0);

            QuestState state = _questManager.GetState(_questConfig);
            Assert.IsNotNull(state);
            Assert.IsTrue(state.IsObjectiveCompleted(0));
        }

        [Test]
        public void SaveAndLoad_PersistsQuestStates()
        {
            typeof(QuestManager)
                .GetField("_saveKey", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_questManager, "Test_QuestManager");

            _questManager.AcceptQuest(_questConfig);
            _questManager.CompleteObjective(_questConfig, 1);
            _questManager.Save();

            typeof(QuestManager)
                .GetField("_states", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_questManager, new List<QuestState>());

            Assert.IsNull(_questManager.GetState("test_quest"), "After clearing, state should be null");

            _questManager.Load();

            QuestState state = _questManager.GetState("test_quest");
            Assert.IsNotNull(state, "After Load, quest state should be restored");
            Assert.AreEqual(QuestStatus.Active, state.Status);
            Assert.IsTrue(state.IsObjectiveCompleted(1));
        }

        [Test]
        public void RuntimeStaticReset_ClearsQuestManagerSingletonCache()
        {
            Assert.AreSame(_questManager, QuestManager.Instance);

            ResetQuestManagerSingleton();

            Assert.IsFalse(QuestManager.HasInstance);
            Assert.AreSame(_questManager, QuestManager.Instance);
        }

        [Test]
        public void SceneReload_ReplacesQuestManagerSingletonCache()
        {
            Object.DestroyImmediate(_go);
            _go = null;
            ResetQuestManagerSingleton();

            GameObject sceneObject = null;
            GameObject reloadedObject = null;

            try
            {
                sceneObject = new GameObject("QuestManagerSceneReload");
                QuestManager sceneManager = sceneObject.AddComponent<QuestManager>();

                Assert.AreSame(sceneManager, QuestManager.Instance);

                Object.DestroyImmediate(sceneObject);
                sceneObject = null;

                Assert.IsFalse(QuestManager.HasInstance);
                Assert.IsTrue(sceneManager == null);

                reloadedObject = new GameObject("QuestManagerSceneReloaded");
                QuestManager reloadedManager = reloadedObject.AddComponent<QuestManager>();

                Assert.AreSame(reloadedManager, QuestManager.Instance);
            }
            finally
            {
                if (reloadedObject != null)
                {
                    Object.DestroyImmediate(reloadedObject);
                }

                if (sceneObject != null)
                {
                    Object.DestroyImmediate(sceneObject);
                }

                ResetQuestManagerSingleton();
            }
        }

        private static void ResetQuestManagerSingleton()
        {
            typeof(Singleton<QuestManager>)
                .GetMethod("ResetStaticStateForRuntime", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null);
        }
    }
}
