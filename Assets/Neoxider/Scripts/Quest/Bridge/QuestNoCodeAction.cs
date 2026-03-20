using UnityEngine;
using UnityEngine.Events;

namespace Neo.Quest
{
    /// <summary>
    ///     Universal no-code quest action: Accept/Complete/Fail/Restart/Reset/ResetAll.
    ///     Configured in the Inspector and invoked from UnityEvent with no arguments.
    /// </summary>
    [NeoDoc("Quest/QuestBridge.md")]
    [CreateFromMenu("Neoxider/Quest/Quest NoCode Action")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestNoCodeAction))]
    public class QuestNoCodeAction : MonoBehaviour
    {
        public enum ActionType
        {
            Accept,
            CompleteObjective,
            Fail,
            Restart,
            Reset,
            ResetAll
        }

        [Header("Action")] [SerializeField] private ActionType _actionType = ActionType.Accept;
        [SerializeField] private QuestConfig _quest;
        [SerializeField] private int _objectiveIndex;

        [Header("Optional Flow Gate")] [SerializeField]
        private QuestFlowConfig _flowConfig;

        [Header("Events")] [SerializeField] private UnityEvent _onSuccess = new();
        [SerializeField] private UnityEvent<string> _onFailed = new();
        [SerializeField] private UnityEvent<string> _onResultMessage = new();

        [Button("Reset")]
        public void Reset()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteResetInternal(manager);
        }

        [Button("Execute Action")]
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
            }
        }

        [Button("Accept")]
        public void Accept()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteAcceptInternal(manager);
        }

        [Button("Complete Objective")]
        public void CompleteObjective()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteCompleteObjectiveInternal(manager);
        }

        [Button("Fail")]
        public void Fail()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteFailInternal(manager);
        }

        [Button("Restart")]
        public void Restart()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            ExecuteRestartInternal(manager);
        }

        [Button("Reset All")]
        public void ResetAll()
        {
            if (!TryGetManager(out QuestManager manager))
            {
                return;
            }

            manager.ResetAllQuests();
            EmitSuccess("All quests reset.");
        }

        private void ExecuteAcceptInternal(QuestManager manager)
        {
            if (!TryValidateQuest(out string error))
            {
                EmitFailed(error);
                return;
            }

            if (_flowConfig != null && !_flowConfig.CanAcceptQuest(manager, _quest, out string reason))
            {
                EmitFailed(string.IsNullOrEmpty(reason) ? "Quest is locked by flow rules." : reason);
                return;
            }

            bool accepted = manager.AcceptQuest(_quest);
            if (!accepted)
            {
                EmitFailed($"Accept failed: {_quest.Id}");
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

            manager.CompleteObjective(_quest, _objectiveIndex);
            EmitSuccess($"CompleteObjective: {_quest.Id}[{_objectiveIndex}]");
        }

        private void ExecuteFailInternal(QuestManager manager)
        {
            if (!TryValidateQuest(out string error))
            {
                EmitFailed(error);
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

            if (_flowConfig != null && !_flowConfig.CanAcceptQuest(manager, _quest, out string reason))
            {
                QuestState state = manager.GetState(_quest);
                bool canBypassBecauseHasState = state != null;
                if (!canBypassBecauseHasState)
                {
                    EmitFailed(string.IsNullOrEmpty(reason) ? "Quest is locked by flow rules." : reason);
                    return;
                }
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
            if (_quest != null)
            {
                return true;
            }

            error = "Quest is not assigned.";
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
