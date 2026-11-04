using UnityEngine;
using UnityEngine.Events;

namespace Neo.Quest
{
    /// <summary>
    ///     Universal no-code quest action: Accept/Complete/Fail/Restart/Reset/ResetAll, target notifications
    ///     (kill/collect/reach/talk) and objective-condition evaluation. Configured in the Inspector and invoked from
    ///     UnityEvent with no arguments; each action maps to one typed <see cref="QuestManager" /> method.
    /// </summary>
    [NeoDoc("Quest/QuestBridge.md")]
    [CreateFromMenu("Neoxider/Quest/Quest NoCode Action")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestNoCodeAction))]
    public class QuestNoCodeAction : MonoBehaviour
    {
        /// <summary>Quest operation performed by <see cref="Execute" />. Values are serialized; append only.</summary>
        public enum ActionType
        {
            /// <summary>QuestManager.AcceptQuest(Quest), gated by Flow Config when assigned.</summary>
            Accept,

            /// <summary>QuestManager.AddObjectiveProgress(Quest, Objective Index, Amount).</summary>
            CompleteObjective,

            /// <summary>QuestManager.FailQuest(Quest).</summary>
            Fail,

            /// <summary>QuestManager.RestartQuest(Quest).</summary>
            Restart,

            /// <summary>QuestManager.ResetQuest(Quest).</summary>
            Reset,

            /// <summary>QuestManager.ResetAllQuests().</summary>
            ResetAll,

            /// <summary>QuestManager.NotifyKill(Target Id, Amount) for KillCount objectives.</summary>
            NotifyKill,

            /// <summary>QuestManager.NotifyCollect(Target Id, Amount) for CollectCount objectives.</summary>
            NotifyCollect,

            /// <summary>QuestManager.NotifyReachPoint(Target Id) for ReachPoint objectives.</summary>
            NotifyReachPoint,

            /// <summary>QuestManager.NotifyTalk(Target Id) for Talk objectives.</summary>
            NotifyTalk,

            /// <summary>QuestManager.EvaluateObjectiveConditions() for CustomCondition objectives.</summary>
            EvaluateObjectiveConditions
        }

        [Header("Action")] [Tooltip("Operation performed by Execute().")] [SerializeField]
        private ActionType _actionType = ActionType.Accept;

        [Tooltip("Target quest for Accept, CompleteObjective, Fail, Restart and Reset.")] [SerializeField]
        private QuestConfig _quest;

        [Tooltip("Objective index (0-based, in QuestConfig order) used by CompleteObjective.")] [SerializeField]
        private int _objectiveIndex;

        [Tooltip("Target id for the Notify actions; must equal the objective's Target Id (case-sensitive).")]
        [SerializeField]
        private string _targetId = "";

        [Tooltip(
            "Progress added by CompleteObjective and NotifyKill/NotifyCollect to counter objectives. Binary objectives complete on any positive amount.")]
        [SerializeField]
        [Min(1)]
        private int _amount = 1;

        [Header("Optional Flow Gate")]
        [Tooltip("Optional QuestFlowConfig whose strict chains must allow the quest on Accept/Restart.")]
        [SerializeField]
        private QuestFlowConfig _flowConfig;

        [Header("Events")] [Tooltip("Raised when the action was applied.")] [SerializeField]
        private UnityEvent _onSuccess = new();

        [Tooltip("Raised with a reason when the action was rejected or credited nothing.")] [SerializeField]
        private UnityEvent<string> _onFailed = new();

        [Tooltip("Raised with a message for both success and failure (UI/log).")] [SerializeField]
        private UnityEvent<string> _onResultMessage = new();

        private NotifyMessageCache _successMessage;
        private NotifyMessageCache _failureMessage;

        private struct NotifyMessageCache
        {
            public QuestObjectiveType Type;
            public string TargetId;
            public int Credited;
            public string Text;
        }

        /// <summary>Selected quest action.</summary>
        public ActionType Action
        {
            get => _actionType;
            set => _actionType = value;
        }

        /// <summary>Target quest for all actions except ResetAll.</summary>
        public QuestConfig Quest
        {
            get => _quest;
            set => _quest = value;
        }

        /// <summary>Objective index used by CompleteObjective.</summary>
        public int ObjectiveIndex
        {
            get => _objectiveIndex;
            set => _objectiveIndex = value;
        }

        /// <summary>Target id used by NotifyKill, NotifyCollect, NotifyReachPoint and NotifyTalk.</summary>
        public string TargetId
        {
            get => _targetId;
            set => _targetId = value ?? "";
        }

        /// <summary>Progress amount used by CompleteObjective and counter notifications (at least 1).</summary>
        public int Amount
        {
            get => _amount;
            set => _amount = Mathf.Max(1, value);
        }

        /// <summary>Optional flow gate checked on Accept/Restart.</summary>
        public QuestFlowConfig FlowConfig
        {
            get => _flowConfig;
            set => _flowConfig = value;
        }

        /// <summary>Event: action succeeded.</summary>
        public UnityEvent OnSuccess => _onSuccess;

        /// <summary>Event: action failed (reason).</summary>
        public UnityEvent<string> OnFailed => _onFailed;

        /// <summary>Event: result message for both success and failure.</summary>
        public UnityEvent<string> OnResultMessage => _onResultMessage;

        /// <summary>Sets the target id (1-arg overload for UnityEvent wiring before Execute()).</summary>
        public void SetTargetId(string targetId)
        {
            TargetId = targetId;
        }

        [Button("Reset", PlayModeOnly = true)]
        public void Reset()
        {
            // WHY: Unity invokes Reset() as an editor lifecycle callback when the component is added
            // or reset from the context menu — that must not fire quest actions or UnityEvents.
            if (!Application.isPlaying)
            {
                return;
            }

            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteResetInternal(manager);
        }

        [Button("Execute Action", PlayModeOnly = true)]
        public void Execute()
        {
            switch (_actionType)
            {
                case ActionType.Accept:
                    Accept();
                    break;
                case ActionType.CompleteObjective:
                    CompleteObjective();
                    break;
                case ActionType.Fail:
                    Fail();
                    break;
                case ActionType.Restart:
                    Restart();
                    break;
                case ActionType.Reset:
                    Reset();
                    break;
                case ActionType.ResetAll:
                    ResetAll();
                    break;
                case ActionType.NotifyKill:
                    Notify(QuestObjectiveType.KillCount);
                    break;
                case ActionType.NotifyCollect:
                    Notify(QuestObjectiveType.CollectCount);
                    break;
                case ActionType.NotifyReachPoint:
                    Notify(QuestObjectiveType.ReachPoint);
                    break;
                case ActionType.NotifyTalk:
                    Notify(QuestObjectiveType.Talk);
                    break;
                case ActionType.EvaluateObjectiveConditions:
                    EvaluateObjectiveConditions();
                    break;
                default:
                    EmitFailed("Unsupported quest action.");
                    break;
            }
        }

        [Button("Accept", PlayModeOnly = true)]
        public void Accept()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteAcceptInternal(manager);
        }

        [Button("Complete Objective", PlayModeOnly = true)]
        public void CompleteObjective()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteCompleteObjectiveInternal(manager);
        }

        [Button("Fail", PlayModeOnly = true)]
        public void Fail()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteFailInternal(manager);
        }

        [Button("Restart", PlayModeOnly = true)]
        public void Restart()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteRestartInternal(manager);
        }

        [Button("Reset All", PlayModeOnly = true)]
        public void ResetAll()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            manager.ResetAllQuests();
            EmitSuccess("All quests reset.");
        }

        /// <summary>Runs QuestManager.EvaluateObjectiveConditions() and emits success.</summary>
        [Button("Evaluate Objective Conditions", PlayModeOnly = true)]
        public void EvaluateObjectiveConditions()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            manager.EvaluateObjectiveConditions();
            EmitSuccess("Objective conditions evaluated.");
        }

        /// <summary>
        ///     Credits every active objective of <paramref name="type" /> whose Target Id equals <see cref="TargetId" />.
        ///     Emits success when at least one objective was credited, otherwise a failure with the reason. The result
        ///     message is reused while type, target id and credited count repeat, so warm calls do not allocate.
        /// </summary>
        public void Notify(QuestObjectiveType type)
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            if (string.IsNullOrEmpty(_targetId))
            {
                EmitFailed("Target id is empty.");
                return;
            }

            int credited = manager.Notify(type, _targetId, _amount);
            if (credited > 0)
            {
                EmitSuccess(GetNotifyMessage(type, credited));
                return;
            }

            EmitFailed(GetNotifyMessage(type, 0));
        }

        // WHY: enemy/pickup prefabs (often pooled) repeat the same notification; reusing the last formatted result
        // keeps the NoCode route allocation-free once warm instead of building a string per kill.
        private string GetNotifyMessage(QuestObjectiveType type, int credited)
        {
            bool success = credited > 0;
            ref NotifyMessageCache cache = ref success ? ref _successMessage : ref _failureMessage;
            if (cache.Text == null || cache.Type != type || cache.Credited != credited ||
                !string.Equals(cache.TargetId, _targetId, System.StringComparison.Ordinal))
            {
                cache.Type = type;
                cache.TargetId = _targetId;
                cache.Credited = credited;
                cache.Text = success
                    ? $"{type} '{_targetId}': {credited} objective(s) credited."
                    : $"No active {type} objective matches '{_targetId}'.";
            }

            return cache.Text;
        }

        private void ExecuteAcceptInternal(QuestManager manager)
        {
            if (!TryValidateQuest(out string error))
            {
                EmitFailed(error);
                return;
            }

            // WHY: the chain-only gate keeps start-condition predicates (user code) to one evaluation per attempt;
            // QuestFlowConfig.CanAcceptQuest followed by AcceptQuest evaluated them twice.
            if (_flowConfig != null && !_flowConfig.IsChainSatisfied(manager, _quest, out string chainReason))
            {
                EmitFailed(string.IsNullOrEmpty(chainReason) ? "Quest is locked by flow rules." : chainReason);
                return;
            }

            if (!manager.TryAcceptQuest(_quest, out string reason))
            {
                EmitFailed(string.IsNullOrEmpty(reason)
                    ? $"Accept failed: {_quest.Id}"
                    : $"Accept failed: {_quest.Id} ({reason})");
                return;
            }

            EmitSuccess($"Accepted: {_quest.Id}");
        }

        private void ExecuteCompleteObjectiveInternal(QuestManager manager)
        {
            if (!TryValidateQuest(out string error))
            {
                EmitFailed(error);
                return;
            }

            QuestState state = manager.GetState(_quest);
            if (state == null || state.Status != QuestStatus.Active)
            {
                EmitFailed("Quest is not active.");
                return;
            }

            if (_objectiveIndex < 0 || _objectiveIndex >= _quest.Objectives.Count ||
                state.IsObjectiveCompleted(_objectiveIndex))
            {
                EmitFailed("Objective is invalid or already completed.");
                return;
            }

            if (!manager.AddObjectiveProgress(_quest, _objectiveIndex, _amount))
            {
                EmitFailed("Objective progress was not credited.");
                return;
            }

            EmitSuccess($"CompleteObjective: {_quest.Id}[{_objectiveIndex}]");
        }

        private void ExecuteFailInternal(QuestManager manager)
        {
            if (!TryValidateQuest(out string error))
            {
                EmitFailed(error);
                return;
            }

            QuestState state = manager.GetState(_quest);
            if (state == null || state.Status != QuestStatus.Active)
            {
                EmitFailed("Quest is not active.");
                return;
            }

            manager.FailQuest(_quest);
            EmitSuccess($"Failed: {_quest.Id}");
        }

        private void ExecuteRestartInternal(QuestManager manager)
        {
            if (!TryValidateQuest(out string error))
            {
                EmitFailed(error);
                return;
            }

            // WHY: an existing state bypasses the chain gate because the quest was already admitted once.
            if (_flowConfig != null && manager.GetState(_quest) == null &&
                !_flowConfig.IsChainSatisfied(manager, _quest, out string reason))
            {
                EmitFailed(string.IsNullOrEmpty(reason) ? "Quest is locked by flow rules." : reason);
                return;
            }

            bool restarted = manager.RestartQuest(_quest);
            if (!restarted)
            {
                EmitFailed($"Restart failed: {_quest.Id}");
                return;
            }

            EmitSuccess($"Restarted: {_quest.Id}");
        }

        private void ExecuteResetInternal(QuestManager manager)
        {
            if (!TryValidateQuest(out string error))
            {
                EmitFailed(error);
                return;
            }

            bool reset = manager.ResetQuest(_quest);
            if (!reset)
            {
                EmitFailed($"Reset failed (state not found): {_quest.Id}");
                return;
            }

            EmitSuccess($"Reset: {_quest.Id}");
        }

        private bool TryGetManager(out QuestManager manager)
        {
            manager = QuestManager.Instance;
            if (manager != null)
            {
                return true;
            }

            EmitFailed("QuestManager not found.");
            return false;
        }

        private bool TryValidateQuest(out string error)
        {
            error = null;
            if (_quest != null && !string.IsNullOrEmpty(_quest.Id))
            {
                return true;
            }

            error = "Quest is not assigned or has an empty id.";
            return false;
        }

        private void EmitSuccess(string message)
        {
            _onSuccess?.Invoke();
            _onResultMessage?.Invoke(message);
        }

        private void EmitFailed(string reason)
        {
            _onFailed?.Invoke(reason);
            _onResultMessage?.Invoke(reason);
        }
    }
}
