using System.Collections.Generic;
using System.Reflection;
using Neo.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for the <b>Neo.Quest</b> module. Three <see cref="QuestConfig" />
    ///     instances are built at runtime (a kill counter, a two-step collect+talk quest, a reach-point)
    ///     and driven through the real singleton <see cref="QuestManager" />: AcceptQuest /
    ///     CompleteObjective / FailQuest / RestartQuest / ResetAllQuests, with OnQuestAccepted,
    ///     OnObjectiveProgress, OnObjectiveCompleted, OnQuestCompleted and OnQuestFailed feeding the
    ///     log. A demo gold tally hooked to OnQuestCompleted shows the reward pattern; states persist
    ///     across play sessions via SaveProvider (manager autoLoad/autoSave). A NoCode row drives the
    ///     same flow through a serialized <see cref="QuestNoCodeAction" /> bridge, and NotifyKill /
    ///     NotifyCollect buttons feed the counter objectives by TargetId. Robust in an empty scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Quest Demo")]
    public sealed class QuestDemoController : MonoBehaviour
    {
        private const int GoldPerQuest = 25;

        private NeoDemoShell.Context _shell;

        private QuestManager _quests;
        private QuestNoCodeAction _bridge;
        private readonly List<QuestConfig> _configs = new();
        private int _selected;
        private int _gold;

        private TMP_Text _titleBig;
        private TMP_Text _statusValue;
        private TMP_Text _objectiveValue;
        private TMP_Text _doneValue;
        private TMP_Text _goldValue;
        private Image _progressBar;

        private QuestConfig Selected => _configs[_selected];

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.Quest", new Color(1f, 0.72f, 0.25f));

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Quest · QuestManager",
                "QuestManager is a singleton quest registry: AcceptQuest starts a QuestConfig, " +
                "CompleteObjective drives counters, UnityEvents feed UI and rewards.",
                "Pick a quest, press Accept — QuestManager.AcceptQuest(config)",
                "+1 Progress calls CompleteObjective; counters tick to RequiredCount",
                "OnQuestCompleted grants the demo gold reward",
                "States persist via SaveProvider — Restart / Reset all to replay");

            BuildQuestManager();
            CreateDemoQuests();
            BuildBridge();

            _titleBig = _shell.AddBigLabel(Selected.Title);
            _progressBar = _shell.AddBar("Objectives progress", _shell.Accent);
            _statusValue = _shell.AddValueLabel("Status");
            _objectiveValue = _shell.AddValueLabel("Current objective");
            _doneValue = _shell.AddValueLabel("Objectives done");
            _goldValue = _shell.AddValueLabel("Gold  (reward via OnQuestCompleted)");

            _shell.AddButtonRow(
                (_configs[0].Title, () => Select(0)),
                (_configs[1].Title, () => Select(1)),
                (_configs[2].Title, () => Select(2)));
            _shell.AddButtonRow(
                ("Accept", Accept),
                ("+1 Progress", AddProgress),
                ("Complete step", CompleteStep));
            _shell.AddButtonRow(
                ("Fail", Fail),
                ("Restart", Restart),
                ("Reset all", ResetAll));
            _shell.AddButtonRow(
                ("NotifyKill", NotifyKill),
                ("NotifyCollect", NotifyCollect));
            _shell.AddButtonRow(
                ("NoCode Accept", NoCodeAccept),
                ("NoCode Complete", NoCodeComplete),
                ("NoCode Fail", NoCodeFail));

            Refresh();
            _shell.Log($"QuestManager ready — {_quests.AllQuests.Count} saved state(s) restored");
        }

        private void OnDestroy()
        {
            if (_quests != null)
            {
                _quests.OnQuestAccepted.RemoveListener(HandleAccepted);
                _quests.OnObjectiveProgress.RemoveListener(HandleProgress);
                _quests.OnObjectiveCompleted.RemoveListener(HandleObjectiveDone);
                _quests.OnQuestCompleted.RemoveListener(HandleCompleted);
                _quests.OnQuestFailed.RemoveListener(HandleFailed);
            }
        }

        private void BuildQuestManager()
        {
            if (!QuestManager.HasInstance)
            {
                QuestManager.CreateInstance = true;
            }

            _quests = QuestManager.I; // Init auto-loads persisted states from SaveProvider
            _quests.OnQuestAccepted.AddListener(HandleAccepted);
            _quests.OnObjectiveProgress.AddListener(HandleProgress);
            _quests.OnObjectiveCompleted.AddListener(HandleObjectiveDone);
            _quests.OnQuestCompleted.AddListener(HandleCompleted);
            _quests.OnQuestFailed.AddListener(HandleFailed);
        }

        private void BuildBridge()
        {
            // WHY: the same serialized QuestNoCodeAction a designer would drop in the inspector —
            // the NoCode buttons only flip its public fields and call Execute(), no manager calls.
            _bridge = gameObject.AddComponent<QuestNoCodeAction>();
            _bridge.OnResultMessage.AddListener(msg => _shell.Log($"NoCode → {msg}"));
        }

        private void NoCodeAccept()
        {
            _bridge.Action = QuestNoCodeAction.ActionType.Accept;
            _bridge.Quest = Selected;
            _bridge.Execute();
            Refresh();
        }

        private void NoCodeComplete()
        {
            QuestState state = _quests.GetState(Selected);
            int index = 0;
            if (state != null)
            {
                int firstIncomplete = FirstIncomplete(Selected, state);
                if (firstIncomplete >= 0)
                {
                    index = firstIncomplete;
                }
            }

            _bridge.Action = QuestNoCodeAction.ActionType.CompleteObjective;
            _bridge.Quest = Selected;
            _bridge.ObjectiveIndex = index;
            _bridge.Execute();
            Refresh();
        }

        private void NoCodeFail()
        {
            _bridge.Action = QuestNoCodeAction.ActionType.Fail;
            _bridge.Quest = Selected;
            _bridge.Execute();
            Refresh();
        }

        private void NotifyKill()
        {
            string target = FindCounterTarget(QuestObjectiveType.KillCount, "wolf");
            _quests.NotifyKill(target); // credits every active KillCount objective with this TargetId
            _shell.Log($"QuestManager.NotifyKill(\"{target}\")");
            Refresh();
        }

        private void NotifyCollect()
        {
            string target = FindCounterTarget(QuestObjectiveType.CollectCount, "herb");
            _quests.NotifyCollect(target);
            _shell.Log($"QuestManager.NotifyCollect(\"{target}\")");
            Refresh();
        }

        private string FindCounterTarget(QuestObjectiveType type, string fallback)
        {
            foreach (QuestObjectiveData obj in Selected.Objectives)
            {
                if (obj.Type == type && !string.IsNullOrEmpty(obj.TargetId))
                {
                    return obj.TargetId;
                }
            }

            foreach (QuestConfig config in _configs)
            {
                foreach (QuestObjectiveData obj in config.Objectives)
                {
                    if (obj.Type == type && !string.IsNullOrEmpty(obj.TargetId))
                    {
                        return obj.TargetId;
                    }
                }
            }

            return fallback;
        }

        private void CreateDemoQuests()
        {
            _configs.Add(CreateQuest("demo_wolf_hunt", "Wolf Hunt",
                "Thin the pack prowling the forest road.",
                new QuestObjectiveData
                {
                    Type = QuestObjectiveType.KillCount,
                    TargetId = "wolf",
                    RequiredCount = 3,
                    DisplayText = "Hunt forest wolves"
                }));

            _configs.Add(CreateQuest("demo_herbal_aid", "Herbal Aid",
                "Gather herbs, then report to the healer.",
                new QuestObjectiveData
                {
                    Type = QuestObjectiveType.CollectCount,
                    TargetId = "herb",
                    RequiredCount = 4,
                    DisplayText = "Gather moon herbs"
                },
                new QuestObjectiveData
                {
                    Type = QuestObjectiveType.Talk,
                    TargetId = "healer",
                    DisplayText = "Talk to the healer"
                }));

            _configs.Add(CreateQuest("demo_scout_ruins", "Scout Ruins",
                "Find the entrance to the sunken ruins.",
                new QuestObjectiveData
                {
                    Type = QuestObjectiveType.ReachPoint,
                    TargetId = "ruins_gate",
                    DisplayText = "Reach the ruins gate"
                }));
        }

        private void Select(int index)
        {
            _selected = index;
            _shell.Log($"selected \"{Selected.Title}\" ({Selected.Id})");
            Refresh();
        }

        private void Accept()
        {
            bool accepted = _quests.AcceptQuest(Selected); // false when already accepted/completed/failed
            if (!accepted)
            {
                _shell.Log($"AcceptQuest({Selected.Id}) → false (state exists — Restart to replay)");
            }

            Refresh();
        }

        private void AddProgress()
        {
            QuestConfig quest = Selected;
            QuestState state = _quests.GetState(quest);
            if (state == null || state.Status != QuestStatus.Active)
            {
                _shell.Log($"\"{quest.Title}\" is not active — press Accept first");
                return;
            }

            int index = FirstIncomplete(quest, state);
            if (index < 0)
            {
                Refresh();
                return;
            }

            _quests.CompleteObjective(quest, index); // +1 for counters, instant for trigger objectives
            Refresh();
        }

        private void CompleteStep()
        {
            QuestConfig quest = Selected;
            QuestState state = _quests.GetState(quest);
            if (state == null || state.Status != QuestStatus.Active)
            {
                _shell.Log($"\"{quest.Title}\" is not active — press Accept first");
                return;
            }

            int index = FirstIncomplete(quest, state);
            if (index < 0)
            {
                Refresh();
                return;
            }

            int guard = Mathf.Max(1, quest.Objectives[index].RequiredCount);
            int calls = 0;
            while (!state.IsObjectiveCompleted(index) && calls < guard)
            {
                _quests.CompleteObjective(quest, index);
                calls++;
            }

            _shell.Log($"CompleteObjective({quest.Id}, {index}) x{calls} → step done");
            Refresh();
        }

        private void Fail()
        {
            _quests.FailQuest(Selected); // no-op unless the quest is Active
            _shell.Log($"QuestManager.FailQuest({Selected.Id})");
            Refresh();
        }

        private void Restart()
        {
            bool ok = _quests.RestartQuest(Selected); // ResetQuest + AcceptQuest in one call
            _shell.Log($"QuestManager.RestartQuest({Selected.Id}) → {ok}");
            Refresh();
        }

        private void ResetAll()
        {
            _quests.ResetAllQuests();
            _shell.Log("QuestManager.ResetAllQuests() → registry cleared");
            Refresh();
        }

        private void HandleAccepted(string questId)
        {
            _shell.Log($"event OnQuestAccepted → {questId}");
            Refresh();
        }

        private void HandleProgress(string questId, int objectiveIndex, int currentCount)
        {
            _shell.Log($"event OnObjectiveProgress → {questId}[{objectiveIndex}] = {currentCount}");
            Refresh();
        }

        private void HandleObjectiveDone(string questId, int objectiveIndex)
        {
            _shell.Log($"event OnObjectiveCompleted → {questId}[{objectiveIndex}]");
            Refresh();
        }

        private void HandleCompleted(string questId)
        {
            // WHY: rewards are not part of Neo.Quest — games hook them to OnQuestCompleted like this.
            _gold += GoldPerQuest;
            _shell.Log($"event OnQuestCompleted → {questId} (+{GoldPerQuest} gold)");
            Refresh();
        }

        private void HandleFailed(string questId)
        {
            _shell.Log($"event OnQuestFailed → {questId}");
            Refresh();
        }

        private void Refresh()
        {
            QuestConfig quest = Selected;
            QuestState state = _quests.GetState(quest);
            QuestStatus status = state?.Status ?? QuestStatus.NotStarted;

            _titleBig.text = quest.Title;
            _statusValue.text = status.ToString();
            _goldValue.text = _gold.ToString();

            int total = quest.Objectives.Count;
            int done = 0;
            float fill = 0f;
            for (int i = 0; i < total; i++)
            {
                if (state != null && state.IsObjectiveCompleted(i))
                {
                    done++;
                    fill += 1f;
                    continue;
                }

                QuestObjectiveData obj = quest.Objectives[i];
                if (IsCounter(obj))
                {
                    int required = Mathf.Max(1, obj.RequiredCount);
                    fill += _quests.GetObjectiveProgress(quest, i) / (float)required;
                }
            }

            _doneValue.text = $"{done} / {total}";
            _objectiveValue.text = DescribeCurrent(quest, state, status);
            Neo.Samples.Survivor.SurvivorUI.SetFill(_progressBar, total > 0 ? fill / total : 0f);
        }

        private string DescribeCurrent(QuestConfig quest, QuestState state, QuestStatus status)
        {
            switch (status)
            {
                case QuestStatus.NotStarted:
                    return "press Accept";
                case QuestStatus.Completed:
                    return "all objectives done";
                case QuestStatus.Failed:
                    return "failed — press Restart";
            }

            // WHY: OnObjectiveCompleted fires for the last objective while the quest is still Active,
            // so all objectives may already be done here.
            int index = FirstIncomplete(quest, state);
            if (index < 0)
            {
                return "all objectives done";
            }

            QuestObjectiveData obj = quest.Objectives[index];
            string text = string.IsNullOrEmpty(obj.DisplayText)
                ? $"{obj.Type} {obj.TargetId}"
                : obj.DisplayText;
            return IsCounter(obj)
                ? $"{text}  {_quests.GetObjectiveProgress(quest, index)}/{obj.RequiredCount}"
                : text;
        }

        private static bool IsCounter(QuestObjectiveData objective)
        {
            return objective.Type == QuestObjectiveType.KillCount ||
                   objective.Type == QuestObjectiveType.CollectCount;
        }

        private static int FirstIncomplete(QuestConfig quest, QuestState state)
        {
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                if (!state.IsObjectiveCompleted(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private static QuestConfig CreateQuest(string id, string title, string description,
            params QuestObjectiveData[] objectives)
        {
            QuestConfig quest = ScriptableObject.CreateInstance<QuestConfig>();
            // WHY: QuestConfig fields are private serialized (asset-authored); runtime instances are
            // built via reflection the same way QuestDemoView and the edit-mode tests do.
            SetPrivateField(quest, "_id", id);
            SetPrivateField(quest, "_title", title);
            SetPrivateField(quest, "_description", description);
            SetPrivateField(quest, "_objectives", new List<QuestObjectiveData>(objectives));
            return quest;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
