using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Neo.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Quest</b> demo scene (QuestDemo.unity). The frame, the quest journal,
    ///     the QuestManager with its quest assets, the reward wallet, the QuestNoCodeAction objects and every
    ///     persistent listener are saved in the scene; this component
    ///     builds no UI. Journal buttons call the public methods below (the C# path: TryAcceptQuest,
    ///     AddObjectiveProgress, FailQuest, RestartQuest, ResetAllQuests), and the C# events of
    ///     <see cref="QuestManager" /> refresh the authored labels and feed the log.
    /// </summary>
    /// <remarks>
    ///     Added to a scene without quest assets (a bare scene or a test host), the controller registers three
    ///     session-only fallback quests and destroys them again in OnDestroy.
    /// </remarks>
    [AddComponentMenu("Neoxider/Demos/Quest Demo")]
    public sealed class QuestDemoController : MonoBehaviour
    {
        [Header("Module")]
        [Tooltip("Scene QuestManager. When empty, the singleton is resolved (and created on play if the scene has none).")]
        [SerializeField]
        private QuestManager _manager;

        [Tooltip("Quest assets in selector-button order. Empty: three session-only fallback quests are created.")]
        [SerializeField]
        private QuestConfig[] _quests = Array.Empty<QuestConfig>();

        [Header("Authored UI")]
        [Tooltip("Selector buttons, one per quest; the selected one is tinted.")]
        [SerializeField]
        private Button[] _selectButtons = Array.Empty<Button>();

        [SerializeField] private Color _selectedTint = new(1f, 0.72f, 0.25f, 1f);
        [SerializeField] private Color _idleTint = new(0.62f, 0.64f, 0.74f, 1f);
        [SerializeField] private TMP_Text _titleValue;
        [SerializeField] private TMP_Text _descriptionValue;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TMP_Text _statusValue;
        [SerializeField] private TMP_Text _objectiveValue;
        [SerializeField] private TMP_Text _doneValue;
        [SerializeField] private TMP_Text _journalValue;
        [SerializeField] private DemoLogFeed _log;

        private readonly List<QuestConfig> _ownedFallback = new();
        private readonly List<QuestState> _activeBuffer = new();
        private readonly StringBuilder _text = new();
        private int _selected;
        private bool _subscribed;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _log != null && _titleValue != null && _statusValue != null &&
                                    _objectiveValue != null && _quests != null && _quests.Length > 0 &&
                                    _ownedFallback.Count == 0;

        public QuestManager Manager => _manager;
        public int SelectedIndex => _selected;
        public QuestConfig SelectedQuest => _quests != null && _quests.Length > 0 ? _quests[_selected] : null;

        private void Start()
        {
            ResolveManager();
            if (_quests == null || _quests.Length == 0)
            {
                CreateFallbackQuests();
            }

            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            for (int i = 0; i < _quests.Length; i++)
            {
                // WHY: target notifications resolve configs by id; registering keeps them working even when the
                // scene's QuestManager was not given the assets in Known Quests.
                _manager.RegisterQuest(_quests[i]);
            }

            Subscribe();
            RefreshView();
            Log("QuestManager ready · " + _manager.AllQuests.Count + " saved state(s) restored from SaveProvider");
        }

        private void OnDestroy()
        {
            Unsubscribe();
            for (int i = 0; i < _ownedFallback.Count; i++)
            {
                QuestConfig config = _ownedFallback[i];
                // WHY: the registry may now hold another owner's config with the same id; only ours is removed.
                if (_manager != null)
                {
                    _manager.UnregisterQuest(config);
                }

                DestroyOwned(config);
            }

            _ownedFallback.Clear();
        }

        /// <summary>Selector button target.</summary>
        public void Select(int index)
        {
            if (_quests == null || index < 0 || index >= _quests.Length)
            {
                return;
            }

            _selected = index;
            QuestConfig quest = _quests[index];
            Log("selected \"" + quest.Title + "\" (" + quest.Id + ")");
            RefreshView();
        }

        /// <summary>Button target: <see cref="QuestManager.TryAcceptQuest(QuestConfig, out string)" />.</summary>
        public void Accept()
        {
            QuestConfig quest = SelectedQuest;
            if (quest == null || _manager == null)
            {
                return;
            }

            if (!_manager.TryAcceptQuest(quest, out string reason))
            {
                Log("TryAcceptQuest(" + quest.Id + ") -> false: " + reason + " (Restart replays it)");
            }

            RefreshView();
        }

        /// <summary>Button target: one unit of progress on the first open objective (AddObjectiveProgress).</summary>
        public void AddProgress()
        {
            if (!TryGetOpenObjective(out QuestConfig quest, out int index))
            {
                return;
            }

            _manager.AddObjectiveProgress(quest, index, 1);
            Log("AddObjectiveProgress(" + quest.Id + ", " + index + ", 1)");
        }

        /// <summary>Button target: finishes the first open objective in one call (amount is clamped to RequiredCount).</summary>
        public void CompleteStep()
        {
            if (!TryGetOpenObjective(out QuestConfig quest, out int index))
            {
                return;
            }

            int remaining = Mathf.Max(1,
                quest.Objectives[index].TargetProgress - _manager.GetObjectiveProgress(quest, index));
            _manager.AddObjectiveProgress(quest, index, remaining);
            Log("AddObjectiveProgress(" + quest.Id + ", " + index + ", " + remaining + ") -> step done");
        }

        /// <summary>Button target: <see cref="QuestManager.FailQuest(QuestConfig)" /> (no-op unless Active).</summary>
        public void Fail()
        {
            QuestConfig quest = SelectedQuest;
            if (quest == null || _manager == null)
            {
                return;
            }

            if (!_manager.IsActive(quest))
            {
                Log("\"" + quest.Title + "\" is not active - FailQuest ignored");
                return;
            }

            _manager.FailQuest(quest);
        }

        /// <summary>Button target: <see cref="QuestManager.RestartQuest(QuestConfig)" /> (reset + accept).</summary>
        public void Restart()
        {
            QuestConfig quest = SelectedQuest;
            if (quest == null || _manager == null)
            {
                return;
            }

            bool restarted = _manager.RestartQuest(quest);
            Log("RestartQuest(" + quest.Id + ") -> " + restarted);
            RefreshView();
        }

        /// <summary>Button target: <see cref="QuestManager.ResetAllQuests" />.</summary>
        public void ResetAll()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.ResetAllQuests();
            Log("ResetAllQuests() -> registry cleared, every quest can be accepted again");
            RefreshView();
        }

        /// <summary>QuestNoCodeAction.OnResultMessage listener: shows the NoCode result in the log.</summary>
        public void LogNoCodeResult(string message)
        {
            Log("NoCode -> " + message);
        }

        /// <summary>Re-reads the selected quest and the journal into the authored labels.</summary>
        public void RefreshView()
        {
            QuestConfig quest = SelectedQuest;
            if (quest == null || _manager == null)
            {
                return;
            }

            QuestState state = _manager.GetState(quest);
            QuestStatus status = state != null ? state.Status : QuestStatus.NotStarted;
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

                QuestObjectiveData objective = quest.Objectives[i];
                if (objective != null && objective.IsCounter)
                {
                    fill += _manager.GetObjectiveProgress(quest, i) / (float)objective.TargetProgress;
                }
            }

            SetText(_titleValue, quest.Title);
            SetText(_descriptionValue, quest.Description);
            SetText(_statusValue, DescribeStatus(status));
            SetText(_doneValue, done + " / " + total);
            SetText(_objectiveValue, "Objective: " + DescribeCurrent(quest, state, status));
            if (_progressFill != null)
            {
                _progressFill.rectTransform.anchorMax = new Vector2(total > 0 ? Mathf.Clamp01(fill / total) : 0f, 1f);
            }

            for (int i = 0; i < _selectButtons.Length; i++)
            {
                Button button = _selectButtons[i];
                if (button != null && button.targetGraphic != null)
                {
                    button.targetGraphic.color = i == _selected ? _selectedTint : _idleTint;
                }
            }

            RefreshJournal();
        }

        private void RefreshJournal()
        {
            if (_journalValue == null)
            {
                return;
            }

            _text.Clear();
            for (int i = 0; i < _quests.Length; i++)
            {
                QuestConfig quest = _quests[i];
                if (quest == null)
                {
                    continue;
                }

                QuestState state = _manager.GetState(quest);
                int done = 0;
                for (int objective = 0; objective < quest.Objectives.Count; objective++)
                {
                    if (state != null && state.IsObjectiveCompleted(objective))
                    {
                        done++;
                    }
                }

                _text.Append(i == _selected ? "<b>" : string.Empty).Append(quest.Title)
                    .Append(i == _selected ? "</b>" : string.Empty).Append("  ·  ")
                    .Append(DescribeStatus(state != null ? state.Status : QuestStatus.NotStarted))
                    .Append("  ").Append(done).Append('/').Append(quest.Objectives.Count).Append('\n');
            }

            _text.Append("<color=#9EA3BD>GetActiveQuests -> ").Append(_manager.GetActiveQuests(_activeBuffer))
                .Append(" active</color>");
            _journalValue.text = _text.ToString();
        }

        private bool TryGetOpenObjective(out QuestConfig quest, out int index)
        {
            quest = SelectedQuest;
            index = -1;
            if (quest == null || _manager == null)
            {
                return false;
            }

            QuestState state = _manager.GetState(quest);
            if (state == null || state.Status != QuestStatus.Active)
            {
                Log("\"" + quest.Title + "\" is not active - press Accept first");
                return false;
            }

            index = FirstIncomplete(quest, state);
            if (index < 0)
            {
                RefreshView();
                return false;
            }

            return true;
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
                    return "failed - press Restart";
            }

            // WHY: OnObjectiveCompleted fires for the last objective while the quest is still Active,
            // so every objective may already be done here.
            int index = FirstIncomplete(quest, state);
            if (index < 0)
            {
                return "all done";
            }

            QuestObjectiveData objective = quest.Objectives[index];
            string text = string.IsNullOrEmpty(objective.DisplayText)
                ? objective.Type + " " + objective.TargetId
                : objective.DisplayText;
            return objective.IsCounter
                ? text + "  " + _manager.GetObjectiveProgress(quest, index) + "/" + objective.TargetProgress
                : text;
        }

        private static string DescribeStatus(QuestStatus status)
        {
            switch (status)
            {
                case QuestStatus.Active:
                    return "<color=#40C9F0>Active</color>";
                case QuestStatus.Completed:
                    return "<color=#43D9A3>Completed</color>";
                case QuestStatus.Failed:
                    return "<color=#FF5C7A>Failed</color>";
                default:
                    return "Not started";
            }
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

        private void HandleAccepted(QuestConfig quest)
        {
            Log("event QuestAccepted -> " + quest.Title);
            RefreshView();
        }

        private void HandleProgress(QuestConfig quest, int objectiveIndex, int currentCount)
        {
            Log("event ObjectiveProgress -> " + quest.Title + "[" + objectiveIndex + "] = " + currentCount);
            RefreshView();
        }

        private void HandleObjectiveCompleted(QuestConfig quest, int objectiveIndex)
        {
            Log("event ObjectiveCompleted -> " + quest.Title + "[" + objectiveIndex + "]");
            RefreshView();
        }

        private void HandleCompleted(QuestConfig quest)
        {
            Log("<color=#43D9A3>event QuestCompleted -> " + quest.Title +
                "</color> · OnQuestCompleted pays the reward (Inspector)");
            RefreshView();
        }

        private void HandleFailed(string questId)
        {
            Log("<color=#FF5C7A>event QuestFailed -> " + questId + "</color>");
            RefreshView();
        }

        private void HandleReset(string questId)
        {
            Log("event QuestReset -> " + questId);
            RefreshView();
        }

        private void HandleLoaded()
        {
            RefreshView();
        }

        private void Subscribe()
        {
            if (_subscribed || _manager == null)
            {
                return;
            }

            _manager.QuestAccepted += HandleAccepted;
            _manager.ObjectiveProgress += HandleProgress;
            _manager.ObjectiveCompleted += HandleObjectiveCompleted;
            _manager.QuestCompleted += HandleCompleted;
            _manager.QuestFailed += HandleFailed;
            _manager.QuestReset += HandleReset;
            _manager.QuestsLoaded += HandleLoaded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _manager == null)
            {
                return;
            }

            _manager.QuestAccepted -= HandleAccepted;
            _manager.ObjectiveProgress -= HandleProgress;
            _manager.ObjectiveCompleted -= HandleObjectiveCompleted;
            _manager.QuestCompleted -= HandleCompleted;
            _manager.QuestFailed -= HandleFailed;
            _manager.QuestReset -= HandleReset;
            _manager.QuestsLoaded -= HandleLoaded;
            _subscribed = false;
        }

        private void ResolveManager()
        {
            if (_manager != null)
            {
                return;
            }

            // WHY: an authored scene assigns its QuestManager; the singleton fallback keeps the controller usable in
            // an empty scene without leaving lazy creation switched on for the rest of the session.
            bool previous = QuestManager.CreateInstance;
            QuestManager.CreateInstance = true;
            _manager = QuestManager.I;
            QuestManager.CreateInstance = previous;
        }

        private void CreateFallbackQuests()
        {
            // WHY: mirrors the demo quest assets (same ids), so saved progress matches both paths.
            _quests = new[]
            {
                CreateQuest("demo_wolf_hunt", "Wolf Hunt", "Thin the pack prowling the forest road.",
                    Objective(QuestObjectiveType.KillCount, "wolf", 3, "Hunt forest wolves")),
                CreateQuest("demo_herbal_aid", "Herbal Aid", "Gather herbs, then report to the healer.",
                    Objective(QuestObjectiveType.CollectCount, "herb", 4, "Gather moon herbs"),
                    Objective(QuestObjectiveType.Talk, "healer", 1, "Talk to the healer")),
                CreateQuest("demo_scout_ruins", "Scout Ruins", "Find the entrance to the sunken ruins.",
                    Objective(QuestObjectiveType.ReachPoint, "ruins_gate", 1, "Reach the ruins gate"))
            };
        }

        private static QuestObjectiveData Objective(QuestObjectiveType type, string targetId, int requiredCount,
            string displayText)
        {
            return new QuestObjectiveData
            {
                Type = type,
                TargetId = targetId,
                RequiredCount = requiredCount,
                DisplayText = displayText
            };
        }

        private QuestConfig CreateQuest(string id, string title, string description,
            params QuestObjectiveData[] objectives)
        {
            QuestConfig quest = ScriptableObject.CreateInstance<QuestConfig>();
            _ownedFallback.Add(quest);
            quest.name = id;
            // WHY: QuestConfig fields are private serialized (asset-authored); session-only instances are filled
            // through reflection the same way the edit-mode tests build them.
            SetPrivateField(quest, "_id", id);
            SetPrivateField(quest, "_title", title);
            SetPrivateField(quest, "_description", description);
            SetPrivateField(quest, "_objectives", new List<QuestObjectiveData>(objectives));
            return quest;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static void DestroyOwned(Object owned)
        {
            if (owned == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(owned);
            }
            else
            {
                DestroyImmediate(owned);
            }
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[QuestDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
