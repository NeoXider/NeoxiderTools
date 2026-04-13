using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Neo.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Quest
{
    [AddComponentMenu("Neoxider/Demo/Quest/QuestDemoView")]
    public class QuestDemoView : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private QuestManager _questManager;

        [SerializeField] private QuestFlowConfig _questFlowConfig;
        [SerializeField] private List<QuestConfig> _quests = new();

        [Header("Buttons")] [SerializeField] private Button _prevQuestButton;
        [SerializeField] private Button _nextQuestButton;
        [SerializeField] private Button _acceptQuestButton;
        [SerializeField] private Button _failQuestButton;
        [SerializeField] private Button _completeNextObjectiveButton;
        [SerializeField] private Button _restartQuestButton;
        [SerializeField] private Button _resetAllQuestsButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _clearLogButton;

        [Header("Text (TMP)")] [SerializeField]
        private TMP_Text _selectedQuestTitleText;

        [SerializeField] private TMP_Text _selectedQuestDescriptionText;
        [SerializeField] private TMP_Text _selectedQuestStatusText;
        [SerializeField] private TMP_Text _selectedQuestObjectivesText;
        [SerializeField] private TMP_Text _allQuestsSummaryText;
        [SerializeField] private TMP_Text _eventLogText;

        [Header("Image")] [SerializeField] private Image _selectedQuestIconImage;

        [Header("Options")] [SerializeField] [Range(3, 30)]
        private int _maxLogLines = 12;

        [SerializeField] private bool _autoBindUiByName = true;

        [SerializeField] private bool _createDefaultRuntimeQuestsIfEmpty = true;

        [SerializeField] private bool _loadQuestsFromResourcesIfEmpty = true;

        [SerializeField] private string _resourcesQuestFolder = "Quests";

        [SerializeField] private string _resourcesQuestFlowPath = "Quests/QuestFlowConfig";

        private readonly List<string> _logLines = new();
        private int _selectedQuestIndex;

        private void Awake()
        {
            if (_questManager == null)
            {
                _questManager = QuestManager.Instance;
            }

            if (_autoBindUiByName)
            {
                TryAutoBindUiByName();
            }

            if (_loadQuestsFromResourcesIfEmpty)
            {
                TryLoadQuestsFromResources();
            }

            if (_createDefaultRuntimeQuestsIfEmpty)
            {
                EnsureDefaultRuntimeQuests();
            }

            if (_questFlowConfig != null)
            {
                List<QuestConfig> ordered = _questFlowConfig.BuildOrderedQuestList();
                if (ordered.Count > 0)
                {
                    _quests = ordered;
                }
            }
        }

        private void OnEnable()
        {
            BindButtons();
            BindManagerEvents();
            RefreshUI();
        }

        private void OnDisable()
        {
            UnbindButtons();
            UnbindManagerEvents();
        }

        public void PrevQuest()
        {
            if (_quests.Count == 0)
            {
                return;
            }

            _selectedQuestIndex--;
            if (_selectedQuestIndex < 0)
            {
                _selectedQuestIndex = _quests.Count - 1;
            }

            RefreshUI();
        }

        public void NextQuest()
        {
            if (_quests.Count == 0)
            {
                return;
            }

            _selectedQuestIndex++;
            if (_selectedQuestIndex >= _quests.Count)
            {
                _selectedQuestIndex = 0;
            }

            RefreshUI();
        }

        public void AcceptSelectedQuest()
        {
            QuestConfig quest = GetSelectedQuest();
            if (quest == null || _questManager == null)
            {
                return;
            }

            if (!CanAcceptQuestByFlow(quest, out string lockReason))
            {
                AddLog($"Accept blocked: {quest.Id} ({lockReason})");
                RefreshUI();
                return;
            }

            bool accepted = _questManager.AcceptQuest(quest);
            if (!accepted)
            {
                AddLog($"Accept failed: {quest.Id}");
            }

            RefreshUI();
        }

        public void FailSelectedQuest()
        {
            QuestConfig quest = GetSelectedQuest();
            if (quest == null || _questManager == null)
            {
                return;
            }

            _questManager.FailQuest(quest);
            RefreshUI();
        }

        public void CompleteNextObjective()
        {
            QuestConfig quest = GetSelectedQuest();
            if (quest == null || _questManager == null)
            {
                return;
            }

            QuestState state = _questManager.GetState(quest);
            if (state == null || state.Status != QuestStatus.Active)
            {
                AddLog($"Quest is not active: {quest.Id}");
                RefreshUI();
                return;
            }

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                if (!state.IsObjectiveCompleted(i))
                {
                    _questManager.CompleteObjective(quest, i);
                    RefreshUI();
                    return;
                }
            }

            AddLog($"All objectives already completed: {quest.Id}");
            RefreshUI();
        }

        public void CompleteObjectiveByIndex(int objectiveIndex)
        {
            QuestConfig quest = GetSelectedQuest();
            if (quest == null || _questManager == null)
            {
                return;
            }

            _questManager.CompleteObjective(quest, objectiveIndex);
            RefreshUI();
        }

        public void RestartSelectedQuest()
        {
            QuestConfig quest = GetSelectedQuest();
            if (quest == null || _questManager == null)
            {
                return;
            }

            bool restarted = _questManager.RestartQuest(quest);
            if (!restarted)
            {
                AddLog($"Restart failed: {quest.Id}");
            }

            RefreshUI();
        }

        public void ResetAllQuests()
        {
            if (_questManager == null)
            {
                return;
            }

            _questManager.ResetAllQuests();
            AddLog("All quests reset.");
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (_questManager == null)
            {
                _questManager = QuestManager.Instance;
            }

            ClampSelectedQuestIndex();
            QuestConfig selectedQuest = GetSelectedQuest();
            RenderSelectedQuest(selectedQuest);
            RenderQuestsSummary();
            RenderLog();
            UpdateButtonsState(selectedQuest);
        }

        public void ClearLog()
        {
            _logLines.Clear();
            RenderLog();
        }

        private void BindButtons()
        {
            if (_prevQuestButton != null)
            {
                _prevQuestButton.onClick.AddListener(PrevQuest);
            }

            if (_nextQuestButton != null)
            {
                _nextQuestButton.onClick.AddListener(NextQuest);
            }

            if (_acceptQuestButton != null)
            {
                _acceptQuestButton.onClick.AddListener(AcceptSelectedQuest);
            }

            if (_failQuestButton != null)
            {
                _failQuestButton.onClick.AddListener(FailSelectedQuest);
            }

            if (_completeNextObjectiveButton != null)
            {
                _completeNextObjectiveButton.onClick.AddListener(CompleteNextObjective);
            }

            if (_restartQuestButton != null)
            {
                _restartQuestButton.onClick.AddListener(RestartSelectedQuest);
            }

            if (_resetAllQuestsButton != null)
            {
                _resetAllQuestsButton.onClick.AddListener(ResetAllQuests);
            }

            if (_refreshButton != null)
            {
                _refreshButton.onClick.AddListener(RefreshUI);
            }

            if (_clearLogButton != null)
            {
                _clearLogButton.onClick.AddListener(ClearLog);
            }
        }

        private void UnbindButtons()
        {
            if (_prevQuestButton != null)
            {
                _prevQuestButton.onClick.RemoveListener(PrevQuest);
            }

            if (_nextQuestButton != null)
            {
                _nextQuestButton.onClick.RemoveListener(NextQuest);
            }

            if (_acceptQuestButton != null)
            {
                _acceptQuestButton.onClick.RemoveListener(AcceptSelectedQuest);
            }

            if (_failQuestButton != null)
            {
                _failQuestButton.onClick.RemoveListener(FailSelectedQuest);
            }

            if (_completeNextObjectiveButton != null)
            {
                _completeNextObjectiveButton.onClick.RemoveListener(CompleteNextObjective);
            }

            if (_restartQuestButton != null)
            {
                _restartQuestButton.onClick.RemoveListener(RestartSelectedQuest);
            }

            if (_resetAllQuestsButton != null)
            {
                _resetAllQuestsButton.onClick.RemoveListener(ResetAllQuests);
            }

            if (_refreshButton != null)
            {
                _refreshButton.onClick.RemoveListener(RefreshUI);
            }

            if (_clearLogButton != null)
            {
                _clearLogButton.onClick.RemoveListener(ClearLog);
            }
        }

        private void BindManagerEvents()
        {
            if (_questManager == null)
            {
                return;
            }

            _questManager.OnQuestAccepted.AddListener(HandleQuestAccepted);
            _questManager.OnQuestCompleted.AddListener(HandleQuestCompleted);
            _questManager.OnQuestFailed.AddListener(HandleQuestFailed);
            _questManager.OnObjectiveCompleted.AddListener(HandleObjectiveCompleted);
            _questManager.OnObjectiveProgress.AddListener(HandleObjectiveProgress);
        }

        private void UnbindManagerEvents()
        {
            if (_questManager == null)
            {
                return;
            }

            _questManager.OnQuestAccepted.RemoveListener(HandleQuestAccepted);
            _questManager.OnQuestCompleted.RemoveListener(HandleQuestCompleted);
            _questManager.OnQuestFailed.RemoveListener(HandleQuestFailed);
            _questManager.OnObjectiveCompleted.RemoveListener(HandleObjectiveCompleted);
            _questManager.OnObjectiveProgress.RemoveListener(HandleObjectiveProgress);
        }

        private void HandleQuestAccepted(string questId)
        {
            AddLog($"Accepted: {questId}");
            RefreshUI();
        }

        private void HandleQuestCompleted(string questId)
        {
            AddLog($"Completed: {questId}");
            RefreshUI();
        }

        private void HandleQuestFailed(string questId)
        {
            AddLog($"Failed: {questId}");
            RefreshUI();
        }

        private void HandleObjectiveCompleted(string questId, int objectiveIndex)
        {
            AddLog($"Objective complete: {questId} [{objectiveIndex}]");
            RefreshUI();
        }

        private void HandleObjectiveProgress(string questId, int objectiveIndex, int currentCount)
        {
            AddLog($"Objective progress: {questId} [{objectiveIndex}] = {currentCount}");
            RefreshUI();
        }

        private void RenderSelectedQuest(QuestConfig quest)
        {
            if (quest == null)
            {
                SetText(_selectedQuestTitleText, "Selected Quest: <none>");
                SetText(_selectedQuestDescriptionText, "Description: -");
                SetText(_selectedQuestStatusText, "Status: -");
                SetText(_selectedQuestObjectivesText, "Objectives: -");
                RenderQuestIcon(null);
                return;
            }

            QuestState state = _questManager != null ? _questManager.GetState(quest) : null;
            QuestStatus status = state != null ? state.Status : QuestStatus.NotStarted;
            bool canAccept = CanAcceptQuestByFlow(quest, out string lockReason);
            string lockSuffix = !canAccept && status == QuestStatus.NotStarted ? $" [Locked: {lockReason}]" : "";

            SetText(_selectedQuestTitleText,
                $"Selected Quest [{_selectedQuestIndex + 1}/{_quests.Count}]: {quest.Title} ({quest.Id})");
            SetText(_selectedQuestDescriptionText, $"Description: {quest.Description}");
            SetText(_selectedQuestStatusText, $"Status: {status}{lockSuffix}");
            SetText(_selectedQuestObjectivesText, BuildObjectivesText(quest, state));
            RenderQuestIcon(quest.Icon);
        }

        private void RenderQuestsSummary()
        {
            if (_allQuestsSummaryText == null)
            {
                return;
            }

            if (_quests.Count == 0)
            {
                _allQuestsSummaryText.text = "No quests assigned.";
                return;
            }

            StringBuilder builder = new();
            builder.AppendLine("Quests:");
            for (int i = 0; i < _quests.Count; i++)
            {
                QuestConfig quest = _quests[i];
                if (quest == null)
                {
                    builder.AppendLine($"{i + 1}. <null>");
                    continue;
                }

                QuestState state = _questManager != null ? _questManager.GetState(quest) : null;
                QuestStatus status = state != null ? state.Status : QuestStatus.NotStarted;
                string marker = i == _selectedQuestIndex ? ">" : " ";
                bool canAccept = CanAcceptQuestByFlow(quest, out _);
                string availabilitySuffix = status == QuestStatus.NotStarted && !canAccept ? " (Locked)" : "";
                builder.AppendLine($"{marker} {i + 1}. {quest.Title} [{status}]{availabilitySuffix}");
            }

            _allQuestsSummaryText.text = builder.ToString();
        }

        private void RenderLog()
        {
            if (_eventLogText == null)
            {
                return;
            }

            if (_logLines.Count == 0)
            {
                _eventLogText.text = "Event Log: -";
                return;
            }

            StringBuilder builder = new();
            builder.AppendLine("Event Log:");
            for (int i = 0; i < _logLines.Count; i++)
            {
                builder.AppendLine($"• {_logLines[i]}");
            }

            _eventLogText.text = builder.ToString();
        }

        private void AddLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _logLines.Add(message);
            while (_logLines.Count > _maxLogLines)
            {
                _logLines.RemoveAt(0);
            }

            RenderLog();
        }

        private void UpdateButtonsState(QuestConfig selectedQuest)
        {
            bool hasQuest = selectedQuest != null;
            bool canNavigate = _quests.Count > 1;

            SetButtonInteractable(_prevQuestButton, canNavigate);
            SetButtonInteractable(_nextQuestButton, canNavigate);
            SetButtonInteractable(_refreshButton, true);
            SetButtonInteractable(_clearLogButton, true);
            SetButtonInteractable(_resetAllQuestsButton, true);

            if (!hasQuest || _questManager == null)
            {
                SetButtonInteractable(_acceptQuestButton, false);
                SetButtonInteractable(_failQuestButton, false);
                SetButtonInteractable(_completeNextObjectiveButton, false);
                SetButtonInteractable(_restartQuestButton, false);
                return;
            }

            QuestState state = _questManager.GetState(selectedQuest);
            QuestStatus status = state != null ? state.Status : QuestStatus.NotStarted;
            bool canAccept = CanAcceptQuestByFlow(selectedQuest, out _);
            SetButtonInteractable(_acceptQuestButton, status == QuestStatus.NotStarted && canAccept);
            SetButtonInteractable(_failQuestButton, status == QuestStatus.Active);
            SetButtonInteractable(_completeNextObjectiveButton, status == QuestStatus.Active);
            SetButtonInteractable(_restartQuestButton, true);
        }

        private string BuildObjectivesText(QuestConfig quest, QuestState state)
        {
            if (quest.Objectives.Count == 0)
            {
                return "Objectives: none";
            }

            StringBuilder builder = new();
            builder.AppendLine("Objectives:");

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                QuestObjectiveData objective = quest.Objectives[i];
                bool completed = state != null && state.IsObjectiveCompleted(i);
                int progress = _questManager != null ? _questManager.GetObjectiveProgress(quest, i) : 0;
                int requiredCount = objective.RequiredCount > 0 ? objective.RequiredCount : 1;

                string title = objective.Type switch
                {
                    QuestObjectiveType.KillCount => $"Kill '{objective.TargetId}'",
                    QuestObjectiveType.CollectCount => $"Collect '{objective.TargetId}'",
                    QuestObjectiveType.ReachPoint => $"Reach '{objective.TargetId}'",
                    QuestObjectiveType.Talk => $"Talk '{objective.TargetId}'",
                    _ => $"Custom '{objective.TargetId}'"
                };
                if (!string.IsNullOrWhiteSpace(objective.DisplayText))
                {
                    title = objective.DisplayText;
                }

                string progressText = objective.Type == QuestObjectiveType.KillCount ||
                                      objective.Type == QuestObjectiveType.CollectCount
                    ? $"{progress}/{requiredCount}"
                    : completed
                        ? "done"
                        : "pending";

                string marker = completed ? "[x]" : "[ ]";
                builder.AppendLine($"{marker} {i}. {title} ({progressText})");
            }

            return builder.ToString();
        }

        private QuestConfig GetSelectedQuest()
        {
            if (_quests.Count == 0)
            {
                return null;
            }

            ClampSelectedQuestIndex();
            return _quests[_selectedQuestIndex];
        }

        private void ClampSelectedQuestIndex()
        {
            if (_quests.Count == 0)
            {
                _selectedQuestIndex = 0;
                return;
            }

            if (_selectedQuestIndex < 0)
            {
                _selectedQuestIndex = 0;
            }
            else if (_selectedQuestIndex >= _quests.Count)
            {
                _selectedQuestIndex = _quests.Count - 1;
            }
        }

        private static void SetText(TMP_Text target, string text)
        {
            if (target != null)
            {
                target.text = text;
            }
        }

        private static void SetButtonInteractable(Button button, bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private void TryAutoBindUiByName()
        {
            _prevQuestButton ??= FindButton("BtnPrevQuest");
            _nextQuestButton ??= FindButton("BtnNextQuest");
            _acceptQuestButton ??= FindButton("BtnAcceptQuest");
            _failQuestButton ??= FindButton("BtnFailQuest");
            _completeNextObjectiveButton ??= FindButton("BtnCompleteNextObjective");
            _restartQuestButton ??= FindButton("BtnRestartQuest");
            _resetAllQuestsButton ??= FindButton("BtnResetAllQuests");
            _refreshButton ??= FindButton("BtnRefresh");
            _clearLogButton ??= FindButton("BtnClearLog");

            _selectedQuestTitleText ??= FindText("TxtSelectedQuestTitle");
            _selectedQuestDescriptionText ??= FindText("TxtSelectedQuestDescription");
            _selectedQuestStatusText ??= FindText("TxtSelectedQuestStatus");
            _selectedQuestObjectivesText ??= FindText("TxtSelectedQuestObjectives");
            _allQuestsSummaryText ??= FindText("TxtAllQuestsSummary");
            _eventLogText ??= FindText("TxtEventLog");
            _selectedQuestIconImage ??= FindImage("ImgSelectedQuest");
        }

        private void EnsureDefaultRuntimeQuests()
        {
            if (_quests.Count > 0)
            {
                return;
            }

            _quests.Add(CreateRuntimeQuest(
                "quest_wolves",
                "Wolf Hunt",
                "Kill 3 wolves near the forest.",
                new List<QuestObjectiveData>
                {
                    new()
                    {
                        Type = QuestObjectiveType.KillCount,
                        TargetId = "wolf",
                        RequiredCount = 3,
                        DisplayText = "Eliminate wolves near the forest."
                    }
                }));

            _quests.Add(CreateRuntimeQuest(
                "quest_herbs",
                "Herbalist Help",
                "Collect 2 red herbs and then talk to the Herbalist.",
                new List<QuestObjectiveData>
                {
                    new()
                    {
                        Type = QuestObjectiveType.CollectCount,
                        TargetId = "red_herb",
                        RequiredCount = 2,
                        DisplayText = "Collect red herbs for the Herbalist."
                    },
                    new()
                    {
                        Type = QuestObjectiveType.Talk,
                        TargetId = "herbalist_npc",
                        RequiredCount = 1,
                        DisplayText = "Talk to the Herbalist."
                    }
                }));
        }

        private void TryLoadQuestsFromResources()
        {
            if (!_loadQuestsFromResourcesIfEmpty || _quests.Count > 0)
            {
                return;
            }

            if (_questFlowConfig == null && !string.IsNullOrWhiteSpace(_resourcesQuestFlowPath))
            {
                _questFlowConfig = Resources.Load<QuestFlowConfig>(_resourcesQuestFlowPath);
            }

            if (_questFlowConfig != null)
            {
                List<QuestConfig> ordered = _questFlowConfig.BuildOrderedQuestList();
                if (ordered.Count > 0)
                {
                    _quests.AddRange(ordered);
                    return;
                }
            }

            string folder = string.IsNullOrWhiteSpace(_resourcesQuestFolder) ? "Quests" : _resourcesQuestFolder;
            QuestConfig[] loaded = Resources.LoadAll<QuestConfig>(folder);
            if (loaded == null || loaded.Length == 0)
            {
                return;
            }

            loaded = loaded
                .Where(q => q != null)
                .OrderBy(q => q.Title)
                .ThenBy(q => q.Id)
                .ToArray();

            for (int i = 0; i < loaded.Length; i++)
            {
                QuestConfig quest = loaded[i];
                if (_quests.Contains(quest))
                {
                    continue;
                }

                _quests.Add(quest);
            }
        }

        private static QuestConfig CreateRuntimeQuest(string id, string title, string description,
            List<QuestObjectiveData> objectives)
        {
            QuestConfig quest = ScriptableObject.CreateInstance<QuestConfig>();
            SetPrivateField(quest, "_id", id);
            SetPrivateField(quest, "_title", title);
            SetPrivateField(quest, "_description", description);
            SetPrivateField(quest, "_objectives", objectives ?? new List<QuestObjectiveData>());
            SetPrivateField(quest, "_startConditions", new List<Neo.Condition.ConditionEntry>());
            SetPrivateField(quest, "_nextQuestIds", new List<string>());
            return quest;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static Button FindButton(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static TMP_Text FindText(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<TMP_Text>() : null;
        }

        private static Image FindImage(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Image>() : null;
        }

        private void RenderQuestIcon(Sprite icon)
        {
            if (_selectedQuestIconImage == null)
            {
                return;
            }

            _selectedQuestIconImage.sprite = icon;
            _selectedQuestIconImage.enabled = icon != null;
        }

        private bool CanAcceptQuestByFlow(QuestConfig quest, out string reason)
        {
            reason = null;
            if (quest == null || _questManager == null)
            {
                return false;
            }

            if (_questFlowConfig == null)
            {
                return _questManager.GetState(quest) == null;
            }

            return _questFlowConfig.CanAcceptQuest(_questManager, quest, out reason);
        }
    }
}
