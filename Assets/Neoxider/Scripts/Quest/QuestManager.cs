using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Neo.Condition;
using Neo.Save;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Quest
{
    /// <summary>
    ///     Single entry point for quests: state registry, accept/complete objectives, start-condition checks via
    ///     NeoCondition.
    ///     Singleton: quest registry, AcceptQuest/CompleteObjective, StartConditions evaluation. UnityEvent hooks and
    ///     QuestConfig overloads for code. See Quest/QuestManager.md.
    /// </summary>
    [NeoDoc("Quest/QuestManager.md")]
    [CreateFromMenu("Neoxider/Quest/QuestManager")]
    [AddComponentMenu("Neoxider/Quest/" + nameof(QuestManager))]
    public class QuestManager : Singleton<QuestManager>
    {
        [Header("Context")]
        [Tooltip(
            "Object passed to ConditionEntry.Evaluate(context) when checking StartConditions. Usually the player or a world manager.")]
        [SerializeField]
        private GameObject _conditionContext;

        [Header("Known Quests")]
        [Tooltip("Quest configs used to resolve questId -> QuestConfig in AcceptQuest(string).")]
        [SerializeField]
        private List<QuestConfig> _knownQuests = new();

        [Header("Events")] [Tooltip("Invoked when a quest is accepted (passes questId).")] [SerializeField]
        private UnityEvent<string> _onQuestAccepted = new();

        [Tooltip("Invoked when objective progress changes (questId, objectiveIndex, currentCount).")] [SerializeField]
        private UnityEvent<string, int, int> _onObjectiveProgress = new();

        [Tooltip("Invoked when an objective becomes completed (questId, objectiveIndex).")] [SerializeField]
        private UnityEvent<string, int> _onObjectiveCompleted = new();

        [Tooltip("Invoked when a quest is completed (all objectives done). Passes questId.")] [SerializeField]
        private UnityEvent<string> _onQuestCompleted = new();

        [Tooltip("Invoked when a quest fails (questId).")] [SerializeField]
        private UnityEvent<string> _onQuestFailed = new();

        [Tooltip("Invoked for any quest acceptance (no args, useful for simple bindings).")] [SerializeField]
        private UnityEvent _onAnyQuestAccepted = new();

        [Tooltip("Invoked for any quest completion (no args).")] [SerializeField]
        private UnityEvent _onAnyQuestCompleted = new();

        [Header("Editor (Inspector Buttons)")]
        [Tooltip("QuestId used by the editor buttons for AcceptQuest / CompleteObjective.")]
        [SerializeField]
        private string _editorQuestId = "";

        [Tooltip("Objective index used by the editor CompleteObjective button.")] [SerializeField]
        private int _editorObjectiveIndex;

        [Header("Persistence")] [Tooltip("The SaveProvider key used for persisting quest states.")] [SerializeField]
        private string _saveKey = "Settings_Quests";

        [Tooltip(
            "If true, quest states are saved after accept, progress, fail and reset. Nested changes made by callbacks are written once, when the outermost operation finishes.")]
        [SerializeField]
        private bool _autoSave = true;

        [Tooltip("If true, quests are automatically loaded on initialization.")] [SerializeField]
        private bool _autoLoad = true;

        private List<QuestState> _states = new();

        // WHY: notification loops iterate a snapshot because callbacks may accept/reset quests; pooled lists keep
        // NotifyKill/NotifyCollect allocation-free on survivor-scale kill rates, including nested notifications.
        private readonly List<List<QuestState>> _snapshotPool = new();

        // WHY: bumped on every structural change of _states (add/remove/replace) so loops can skip the O(n)
        // "is this snapshot state still the registered one" lookup while nothing was added or removed.
        private int _stateVersion;
        private int _operationDepth;
        private bool _savePending;

        /// <summary>Wrapper class for JSON serialization of quest states.</summary>
        [Serializable]
        private class QuestSaveData
        {
            public List<QuestState> States = new();
        }

        protected override void Init()
        {
            base.Init();
            if (_autoLoad)
            {
                Load();
            }
        }

        /// <summary>Save quest states to SaveProvider.</summary>
        public void Save()
        {
            if (string.IsNullOrEmpty(_saveKey))
            {
                return;
            }

            QuestSaveData data = new() { States = _states };
            SaveProvider.SetString(_saveKey, JsonUtility.ToJson(data));
        }

        /// <summary>
        ///     Load quest states from SaveProvider. Does not replay gameplay events; raises <see cref="QuestsLoaded" />
        ///     after the restored registry is published.
        /// </summary>
        public void Load()
        {
            if (string.IsNullOrEmpty(_saveKey))
            {
                return;
            }

            string json = SaveProvider.GetString(_saveKey, "");
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            bool published = false;
            try
            {
                QuestSaveData data = JsonUtility.FromJson<QuestSaveData>(json);
                if (data?.States != null)
                {
                    List<QuestState> loadedStates = new();
                    HashSet<string> loadedIds = new(StringComparer.Ordinal);
                    foreach (QuestState state in data.States)
                    {
                        if (state == null || string.IsNullOrEmpty(state.QuestId) || !loadedIds.Add(state.QuestId))
                        {
                            continue;
                        }

                        QuestConfig config = GetConfigById(state.QuestId);
                        ReconcileState(state, config);
                        loadedStates.Add(state);
                    }

                    // Publish only after normalization, so a failed load cannot poison the live registry.
                    _states = loadedStates;
                    _stateVersion++;
                    published = true;
                }
            }
            catch (Exception e)
            {
                NeoDiagnostics.LogError($"[QuestManager] Failed to load save data: {e.Message}");
            }

            if (published)
            {
                QuestsLoaded?.Invoke();
            }
        }

        /// <summary>Context for start-condition checks (ConditionEntry.Evaluate).</summary>
        public GameObject ConditionContext
        {
            get => _conditionContext;
            set => _conditionContext = value;
        }

        /// <summary>Event: quest accepted (questId).</summary>
        public UnityEvent<string> OnQuestAccepted => _onQuestAccepted;

        /// <summary>Event: objective progress (questId, objectiveIndex, currentCount).</summary>
        public UnityEvent<string, int, int> OnObjectiveProgress => _onObjectiveProgress;

        /// <summary>Event: quest completed (questId).</summary>
        public UnityEvent<string> OnQuestCompleted => _onQuestCompleted;

        /// <summary>Event: single objective completed (questId, objectiveIndex).</summary>
        public UnityEvent<string, int> OnObjectiveCompleted => _onObjectiveCompleted;

        /// <summary>Event: quest failed (questId).</summary>
        public UnityEvent<string> OnQuestFailed => _onQuestFailed;

        /// <summary>Event: any quest accepted (no arguments).</summary>
        public UnityEvent OnAnyQuestAccepted => _onAnyQuestAccepted;

        /// <summary>Event: any quest completed (no arguments).</summary>
        public UnityEvent OnAnyQuestCompleted => _onAnyQuestCompleted;

        /// <summary>All quest states.</summary>
        public IReadOnlyList<QuestState> AllQuests => _states;

        /// <summary>Registered quest configs used for id -> config resolution.</summary>
        public IReadOnlyList<QuestConfig> KnownQuests => _knownQuests;

        /// <summary>
        ///     Active quests only. Allocates a new list on every access; prefer
        ///     <see cref="GetActiveQuests(List{QuestState})" /> in code that runs often.
        /// </summary>
        public IReadOnlyList<QuestState> ActiveQuests
        {
            get
            {
                List<QuestState> list = new();
                GetActiveQuests(list);
                return list;
            }
        }

        /// <summary>Gets or sets whether quest states are saved automatically after each operation.</summary>
        public bool AutoSave
        {
            get => _autoSave;
            set => _autoSave = value;
        }

        /// <summary>C# event: quest accepted.</summary>
        public event Action<QuestConfig> QuestAccepted;

        /// <summary>C# event: objective progress (config, objectiveIndex, currentCount).</summary>
        public event Action<QuestConfig, int, int> ObjectiveProgress;

        /// <summary>C# event: an objective became completed (config, objectiveIndex). Raised after ObjectiveProgress.</summary>
        public event Action<QuestConfig, int> ObjectiveCompleted;

        /// <summary>C# event: quest completed.</summary>
        public event Action<QuestConfig> QuestCompleted;

        /// <summary>
        ///     C# event: an active quest changed to Failed (questId). Uses the id because a failed state may belong
        ///     to a config that is not registered.
        /// </summary>
        public event Action<string> QuestFailed;

        /// <summary>
        ///     C# event: a quest state was removed by <see cref="ResetQuest(string)" />, <see cref="ResetAllQuests" />
        ///     or the reset step of <see cref="RestartQuest(string)" /> (questId).
        /// </summary>
        public event Action<string> QuestReset;

        /// <summary>C# event: <see cref="Load" /> replaced the registry with saved states. Refresh UI from current state.</summary>
        public event Action QuestsLoaded;

        /// <summary>Accept quest by ID. Evaluates StartConditions using ConditionContext. For UnityEvent binding.</summary>
        /// <returns>true if the quest was accepted.</returns>
        public bool AcceptQuest(string questId)
        {
            QuestConfig config = GetConfigById(questId);
            return config != null && AcceptQuest(config);
        }

        /// <summary>Accept quest by config (type-safe from code).</summary>
        public bool AcceptQuest(QuestConfig quest)
        {
            return TryAcceptQuestInternal(quest, out _);
        }

        /// <summary>
        ///     Accept quest by config and report the failure reason (null id, existing state, start conditions).
        ///     Start conditions are evaluated once.
        /// </summary>
        public bool TryAcceptQuest(QuestConfig quest, out string failReason)
        {
            return TryAcceptQuestInternal(quest, out failReason);
        }

        private bool TryAcceptQuestInternal(QuestConfig quest, out string failReason)
        {
            if (!CanAcceptQuest(quest, out failReason))
            {
                return false;
            }

            string id = quest.Id;
            BeginOperation();
            try
            {
                // WHY: NotifyKill/NotifyCollect resolve configs by id via _knownQuests; a quest accepted
                // by direct config reference (runtime-built or unlisted) would silently never get credit.
                RegisterQuest(quest);

                QuestState state = new(id, quest.Objectives.Count);
                state.Status = QuestStatus.Active;
                _states.Add(state);
                _stateVersion++;
                MarkDirty();

                _onQuestAccepted?.Invoke(id);
                _onAnyQuestAccepted?.Invoke();
                QuestAccepted?.Invoke(quest);
            }
            finally
            {
                EndOperation();
            }

            return true;
        }

        /// <summary>Register a config for id -> config resolution (NotifyKill/NotifyCollect, AcceptQuest(string)).</summary>
        public void RegisterQuest(QuestConfig quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.Id))
            {
                return;
            }

            if (GetConfigById(quest.Id) == null)
            {
                _knownQuests.Add(quest);
                // Runtime configs may be registered after automatic loading has already restored their states.
                ReconcileState(GetState(quest.Id), quest);
            }
        }

        /// <summary>
        ///     Remove only the currently registered config instance. Quest states and saved progress are retained.
        ///     Call before destroying a runtime-created config; a stale owner cannot unregister its replacement.
        /// </summary>
        /// <returns>True only when the expected instance was the current registration and was removed.</returns>
        public bool UnregisterQuest(QuestConfig expected)
        {
            if (expected == null || string.IsNullOrEmpty(expected.Id))
            {
                return false;
            }

            for (int i = 0; i < _knownQuests.Count; i++)
            {
                QuestConfig current = _knownQuests[i];
                if (current == null || current.Id != expected.Id)
                {
                    continue;
                }

                if (!ReferenceEquals(current, expected))
                {
                    return false;
                }

                _knownQuests.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>Resolve a registered config by quest id. Null when unknown.</summary>
        public QuestConfig GetConfig(string questId)
        {
            return GetConfigById(questId);
        }

        /// <summary>Check acceptance (unknown/duplicate/start conditions) without accepting.</summary>
        public bool CanAcceptQuest(QuestConfig quest, out string reason)
        {
            reason = null;
            if (quest == null || string.IsNullOrEmpty(quest.Id))
            {
                reason = "Quest is null or has an empty id.";
                return false;
            }

            if (GetState(quest.Id) != null)
            {
                reason = "Already accepted or completed.";
                return false;
            }

            if (!EvaluateStartConditions(quest))
            {
                reason = "Start conditions not met.";
                return false;
            }

            // A condition getter/method can synchronously accept this quest itself.
            if (GetState(quest.Id) != null)
            {
                reason = "Already accepted or completed.";
                return false;
            }

            return true;
        }

        /// <summary>Try to accept a quest and report a failure reason.</summary>
        public bool TryAcceptQuest(string questId, out string failReason)
        {
            failReason = null;
            QuestConfig config = GetConfigById(questId);
            if (config == null)
            {
                failReason = "Quest not found.";
                return false;
            }

            return TryAcceptQuestInternal(config, out failReason);
        }

        /// <summary>Credit objective completion. Call from QuestNoCodeAction(CompleteObjective) or from code.</summary>
        public void CompleteObjective(string questId, int objectiveIndex)
        {
            QuestConfig config = GetConfigById(questId);
            if (config != null)
            {
                CompleteObjective(config, objectiveIndex);
            }
        }

        /// <summary>
        ///     Credit objective completion using a quest config: counters (KillCount/CollectCount) add one, other
        ///     objective types complete. Ignored for inactive quests, invalid indices and completed objectives.
        /// </summary>
        public void CompleteObjective(QuestConfig quest, int objectiveIndex)
        {
            AddObjectiveProgress(quest, objectiveIndex, 1);
        }

        /// <summary>
        ///     Add <paramref name="amount" /> to an objective of an active quest. Counter objectives (KillCount,
        ///     CollectCount) accumulate up to their RequiredCount; other objective types complete on any positive amount.
        /// </summary>
        /// <returns>True when progress was credited; false for a nonpositive amount, inactive quest, invalid index or completed objective.</returns>
        public bool AddObjectiveProgress(QuestConfig quest, int objectiveIndex, int amount)
        {
            if (quest == null || amount <= 0)
            {
                return false;
            }

            QuestState state = GetState(quest.Id);
            if (state == null || state.Status != QuestStatus.Active)
            {
                return false;
            }

            return CreditObjective(quest, state, objectiveIndex, amount);
        }

        /// <summary>Add progress to an objective by quest id. See <see cref="AddObjectiveProgress(QuestConfig,int,int)" />.</summary>
        public bool AddObjectiveProgress(string questId, int objectiveIndex, int amount)
        {
            QuestConfig config = GetConfigById(questId);
            return config != null && AddObjectiveProgress(config, objectiveIndex, amount);
        }

        /// <summary>Notify enemy kill (for KillCount objectives). Adds one to every matching active objective.</summary>
        public void NotifyKill(string enemyId)
        {
            Notify(QuestObjectiveType.KillCount, enemyId, 1);
        }

        /// <summary>Notify several kills of one enemy id at once (for KillCount objectives).</summary>
        public void NotifyKill(string enemyId, int amount)
        {
            Notify(QuestObjectiveType.KillCount, enemyId, amount);
        }

        /// <summary>Notify item collected (for CollectCount objectives). Adds one to every matching active objective.</summary>
        public void NotifyCollect(string itemId)
        {
            Notify(QuestObjectiveType.CollectCount, itemId, 1);
        }

        /// <summary>Notify several items of one id collected at once (for CollectCount objectives).</summary>
        public void NotifyCollect(string itemId, int amount)
        {
            Notify(QuestObjectiveType.CollectCount, itemId, amount);
        }

        /// <summary>Notify that a point was reached: completes active ReachPoint objectives whose TargetId matches.</summary>
        public void NotifyReachPoint(string pointId)
        {
            Notify(QuestObjectiveType.ReachPoint, pointId, 1);
        }

        /// <summary>Notify a conversation: completes active Talk objectives whose TargetId matches.</summary>
        public void NotifyTalk(string npcId)
        {
            Notify(QuestObjectiveType.Talk, npcId, 1);
        }

        /// <summary>
        ///     Credit every unfinished objective of the given type whose TargetId equals <paramref name="targetId" />
        ///     (ordinal) in all active quests. Counters add <paramref name="amount" /> (clamped to RequiredCount);
        ///     other types complete. Applies to the states present at entry; quests accepted or restarted by
        ///     callbacks receive the next notification. A null or empty <paramref name="targetId" /> credits nothing,
        ///     so objectives with an unset Target Id are reachable only by index. Allocation-free when warm.
        /// </summary>
        /// <returns>Number of objectives that received credit.</returns>
        public int Notify(QuestObjectiveType type, string targetId, int amount = 1)
        {
            // WHY: TargetId defaults to "", so an empty id would match every unconfigured objective of the type
            // (including default CustomCondition entries with an assigned ConditionEntry).
            if (amount <= 0 || string.IsNullOrEmpty(targetId) || _states.Count == 0)
            {
                return 0;
            }

            int credited = 0;
            BeginOperation();
            List<QuestState> snapshot = RentStateSnapshot();
            int snapshotVersion = _stateVersion;
            try
            {
                for (int s = 0; s < snapshot.Count; s++)
                {
                    QuestState state = snapshot[s];
                    if (!IsCurrentActive(state, snapshotVersion))
                    {
                        continue;
                    }

                    QuestConfig config = GetConfigById(state.QuestId);
                    if (config == null)
                    {
                        continue;
                    }

                    IReadOnlyList<QuestObjectiveData> objectives = config.Objectives;
                    for (int i = 0; i < objectives.Count; i++)
                    {
                        if (!IsCurrentActive(state, snapshotVersion))
                        {
                            break;
                        }

                        QuestObjectiveData objective = objectives[i];
                        if (objective == null || objective.Type != type ||
                            !string.Equals(objective.TargetId, targetId, StringComparison.Ordinal) ||
                            state.IsObjectiveCompleted(i))
                        {
                            continue;
                        }

                        if (CreditObjective(config, state, i, amount))
                        {
                            credited++;
                        }
                    }
                }
            }
            finally
            {
                ReturnStateSnapshot(snapshot);
                EndOperation();
            }

            return credited;
        }

        /// <summary>Get quest state by ID.</summary>
        public QuestState GetState(string questId)
        {
            for (int i = 0; i < _states.Count; i++)
            {
                QuestState s = _states[i];
                if (s.QuestId == questId)
                {
                    return s;
                }
            }

            return null;
        }

        /// <summary>Get quest state by config.</summary>
        public QuestState GetState(QuestConfig quest)
        {
            return quest == null ? null : GetState(quest.Id);
        }

        /// <summary>
        ///     Fill <paramref name="results" /> with the active quest states (cleared first) without allocating.
        /// </summary>
        /// <returns>Number of active quests written.</returns>
        public int GetActiveQuests(List<QuestState> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            for (int i = 0; i < _states.Count; i++)
            {
                QuestState state = _states[i];
                if (state.Status == QuestStatus.Active)
                {
                    results.Add(state);
                }
            }

            return results.Count;
        }

        /// <summary>Fail quest by ID (status -> Failed). Invokes OnQuestFailed.</summary>
        public void FailQuest(string questId)
        {
            QuestState state = GetState(questId);
            if (state == null || state.Status != QuestStatus.Active)
            {
                return;
            }

            BeginOperation();
            try
            {
                state.Status = QuestStatus.Failed;
                MarkDirty();
                _onQuestFailed?.Invoke(questId);
                QuestFailed?.Invoke(questId);
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>Fail quest by config.</summary>
        public void FailQuest(QuestConfig quest)
        {
            if (quest != null)
            {
                FailQuest(quest.Id);
            }
        }

        /// <summary>
        ///     Clear quest state (removes the entry from the registry). Allows replaying the quest.
        /// </summary>
        public bool ResetQuest(string questId)
        {
            QuestState state = GetState(questId);
            if (state == null)
            {
                return false;
            }

            BeginOperation();
            try
            {
                _states.Remove(state);
                _stateVersion++;
                MarkDirty();
                QuestReset?.Invoke(state.QuestId);
            }
            finally
            {
                EndOperation();
            }

            return true;
        }

        /// <summary>Clear quest state by config.</summary>
        public bool ResetQuest(QuestConfig quest)
        {
            return quest != null && ResetQuest(quest.Id);
        }

        /// <summary>
        ///     Restart quest: remove old state and try accepting again.
        /// </summary>
        public bool RestartQuest(string questId)
        {
            QuestConfig config = GetConfigById(questId);
            if (config == null)
            {
                return false;
            }

            return RestartQuest(config);
        }

        /// <summary>Restart quest by config.</summary>
        public bool RestartQuest(QuestConfig quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.Id))
            {
                return false;
            }

            BeginOperation();
            try
            {
                ResetQuest(quest.Id);
                return AcceptQuest(quest);
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>Clear all quest states (active, completed, and failed).</summary>
        public void ResetAllQuests()
        {
            BeginOperation();
            List<QuestState> removed = RentStateSnapshot();
            try
            {
                _states.Clear();
                _stateVersion++;
                MarkDirty();
                for (int i = 0; i < removed.Count && QuestReset != null; i++)
                {
                    QuestReset.Invoke(removed[i].QuestId);
                }
            }
            finally
            {
                ReturnStateSnapshot(removed);
                EndOperation();
            }
        }

        /// <summary>Accept quest using Editor Quest Id field (Inspector button).</summary>
        [Button("Accept Quest (Editor Id)", PlayModeOnly = true)]
        public void AcceptQuestFromEditor()
        {
            if (!string.IsNullOrEmpty(_editorQuestId))
            {
                AcceptQuest(_editorQuestId);
            }
        }

        /// <summary>Complete objective using Editor Quest Id and Objective Index (Inspector button).</summary>
        [Button("Complete Objective (Editor)", PlayModeOnly = true)]
        public void CompleteObjectiveFromEditor()
        {
            if (!string.IsNullOrEmpty(_editorQuestId))
            {
                CompleteObjective(_editorQuestId, _editorObjectiveIndex);
            }
        }

        /// <summary>Whether the quest is active.</summary>
        public bool IsActive(QuestConfig quest)
        {
            QuestState s = GetState(quest);
            return s != null && s.Status == QuestStatus.Active;
        }

        /// <summary>Whether the quest is completed.</summary>
        public bool IsCompleted(QuestConfig quest)
        {
            QuestState s = GetState(quest);
            return s != null && s.Status == QuestStatus.Completed;
        }

        /// <summary>Whether the quest is active (by id).</summary>
        public bool IsActive(string questId)
        {
            QuestState s = GetState(questId);
            return s != null && s.Status == QuestStatus.Active;
        }

        /// <summary>Whether the quest is completed (by id).</summary>
        public bool IsCompleted(string questId)
        {
            QuestState s = GetState(questId);
            return s != null && s.Status == QuestStatus.Completed;
        }

        /// <summary>Whether the quest is failed.</summary>
        public bool IsFailed(QuestConfig quest)
        {
            QuestState s = GetState(quest);
            return s != null && s.Status == QuestStatus.Failed;
        }

        /// <summary>Whether the quest is failed (by id).</summary>
        public bool IsFailed(string questId)
        {
            QuestState s = GetState(questId);
            return s != null && s.Status == QuestStatus.Failed;
        }

        /// <summary>Objective progress for display.</summary>
        public int GetObjectiveProgress(QuestConfig quest, int index)
        {
            QuestState s = GetState(quest);
            return s?.GetObjectiveProgress(index) ?? 0;
        }

        /// <summary>
        ///     Evaluate CustomCondition objectives that have an assigned ConditionEntry against
        ///     ConditionContext and complete those that pass. Call from checkpoints, timers, or UnityEvents.
        /// </summary>
        public void EvaluateObjectiveConditions()
        {
            if (_states.Count == 0)
            {
                return;
            }

            GameObject context = _conditionContext != null ? _conditionContext : gameObject;
            BeginOperation();
            // WHY: completion handlers may accept/reset quests, mutating _states mid-iteration.
            List<QuestState> snapshot = RentStateSnapshot();
            int snapshotVersion = _stateVersion;
            try
            {
                for (int s = 0; s < snapshot.Count; s++)
                {
                    QuestState state = snapshot[s];
                    if (!IsCurrentActive(state, snapshotVersion))
                    {
                        continue;
                    }

                    QuestConfig config = GetConfigById(state.QuestId);
                    if (config == null)
                    {
                        continue;
                    }

                    IReadOnlyList<QuestObjectiveData> objectives = config.Objectives;
                    for (int i = 0; i < objectives.Count; i++)
                    {
                        if (!IsCurrentActive(state, snapshotVersion))
                        {
                            break;
                        }

                        QuestObjectiveData obj = objectives[i];
                        if (obj == null || obj.Type != QuestObjectiveType.CustomCondition || obj.Condition == null ||
                            state.IsObjectiveCompleted(i))
                        {
                            continue;
                        }

                        bool conditionMet = obj.Condition.Evaluate(context);
                        // Predicates are user code too: never credit a state replaced during evaluation.
                        if (!IsCurrentActive(state, snapshotVersion))
                        {
                            break;
                        }

                        if (conditionMet)
                        {
                            CreditObjective(config, state, i, 1);
                        }
                    }
                }
            }
            finally
            {
                ReturnStateSnapshot(snapshot);
                EndOperation();
            }
        }

        /// <summary>
        ///     Commits credit to an active, current <paramref name="state" />, raises progress/completion events and
        ///     completes the quest when every objective is done. An exception from a progress/objective listener does
        ///     not skip the quest-completion step; it is rethrown after that step.
        /// </summary>
        private bool CreditObjective(QuestConfig quest, QuestState state, int objectiveIndex, int amount)
        {
            IReadOnlyList<QuestObjectiveData> objectives = quest.Objectives;
            if (objectiveIndex < 0 || objectiveIndex >= objectives.Count ||
                state.IsObjectiveCompleted(objectiveIndex))
            {
                return false;
            }

            QuestObjectiveData obj = objectives[objectiveIndex];
            if (obj == null)
            {
                return false;
            }

            if (objectiveIndex >= state.ObjectiveCount)
            {
                // WHY: a caller may pass a newer config instance with appended objectives than the one the state
                // was created/reconciled with; grow the lists so the credit is actually recorded.
                state.EnsureObjectiveCount(objectives.Count);
            }

            int newProgress;
            bool objectiveCompleted;
            if (obj.IsCounter)
            {
                int required = obj.TargetProgress;
                long accumulated = (long)state.GetObjectiveProgress(objectiveIndex) + amount;
                newProgress = accumulated >= required ? required : (int)accumulated;
                objectiveCompleted = newProgress >= required;
            }
            else
            {
                newProgress = 1;
                objectiveCompleted = true;
            }

            ExceptionDispatchInfo listenerFault = null;
            BeginOperation();
            try
            {
                state.SetObjectiveProgress(objectiveIndex, newProgress);
                // Commit the objective before callbacks can credit it again.
                if (objectiveCompleted)
                {
                    state.MarkObjectiveCompleted(objectiveIndex);
                }

                MarkDirty();

                try
                {
                    _onObjectiveProgress?.Invoke(quest.Id, objectiveIndex, newProgress);
                    ObjectiveProgress?.Invoke(quest, objectiveIndex, newProgress);
                    if (objectiveCompleted)
                    {
                        _onObjectiveCompleted?.Invoke(quest.Id, objectiveIndex);
                        ObjectiveCompleted?.Invoke(quest, objectiveIndex);
                    }
                }
                catch (Exception exception)
                {
                    // WHY: the committed objective is saved even when a listener throws. Skipping the completion
                    // step would persist an Active state whose objectives are all done, and no later credit could
                    // complete it; finish the transition first and rethrow the first fault afterwards.
                    listenerFault = ExceptionDispatchInfo.Capture(exception);
                }

                if (state.Status == QuestStatus.Active && ReferenceEquals(GetState(quest.Id), state) &&
                    AllObjectivesCompleted(state, quest))
                {
                    state.Status = QuestStatus.Completed;
                    try
                    {
                        _onQuestCompleted?.Invoke(quest.Id);
                        _onAnyQuestCompleted?.Invoke();
                        QuestCompleted?.Invoke(quest);
                    }
                    catch (Exception exception) when (listenerFault != null)
                    {
                        // WHY: the earlier objective-listener fault is the one rethrown; keep this one visible.
                        NeoDiagnostics.LogException(exception, this);
                    }
                }

                listenerFault?.Throw();
            }
            finally
            {
                EndOperation();
            }

            return true;
        }

        /// <summary>
        ///     True while the snapshot state is still Active and still the registered state for its id. Without any
        ///     structural change since the snapshot, every snapshot state is still registered (ids are unique).
        /// </summary>
        private bool IsCurrentActive(QuestState state, int snapshotVersion)
        {
            if (state == null || state.Status != QuestStatus.Active)
            {
                return false;
            }

            return snapshotVersion == _stateVersion || ReferenceEquals(GetState(state.QuestId), state);
        }

        private List<QuestState> RentStateSnapshot()
        {
            List<QuestState> snapshot;
            int last = _snapshotPool.Count - 1;
            if (last >= 0)
            {
                snapshot = _snapshotPool[last];
                _snapshotPool.RemoveAt(last);
            }
            else
            {
                snapshot = new List<QuestState>(Mathf.Max(4, _states.Count));
            }

            // WHY: indexed copy instead of AddRange — Mono's List.InsertRange allocates a temporary array.
            for (int i = 0; i < _states.Count; i++)
            {
                snapshot.Add(_states[i]);
            }

            return snapshot;
        }

        private void ReturnStateSnapshot(List<QuestState> snapshot)
        {
            snapshot.Clear();
            _snapshotPool.Add(snapshot);
        }

        private void BeginOperation()
        {
            _operationDepth++;
        }

        private void EndOperation()
        {
            _operationDepth--;
            if (_operationDepth > 0 || !_savePending)
            {
                return;
            }

            _savePending = false;
            if (_autoSave)
            {
                Save();
            }
        }

        private void MarkDirty()
        {
            if (_autoSave)
            {
                _savePending = true;
            }
        }

        private bool EvaluateStartConditions(QuestConfig quest)
        {
            IReadOnlyList<ConditionEntry> conditions = quest.StartConditions;
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            GameObject context = _conditionContext != null ? _conditionContext : gameObject;
            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionEntry entry = conditions[i];
                if (entry == null)
                {
                    continue;
                }

                if (!entry.Evaluate(context))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllObjectivesCompleted(QuestState state, QuestConfig config)
        {
            for (int i = 0; i < config.Objectives.Count; i++)
            {
                if (!state.IsObjectiveCompleted(i))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ReconcileState(QuestState state, QuestConfig config)
        {
            if (state == null)
            {
                return;
            }

            state.EnsureObjectiveCount(config != null ? config.Objectives.Count : 0);
            if (config == null)
            {
                return;
            }

            for (int i = 0; i < config.Objectives.Count; i++)
            {
                QuestObjectiveData objective = config.Objectives[i];
                if (objective != null && state.IsObjectiveCompleted(i) && !objective.IsCounter)
                {
                    state.SetObjectiveProgress(i, 1);
                }
            }
        }

        private QuestConfig GetConfigById(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            for (int i = 0; i < _knownQuests.Count; i++)
            {
                QuestConfig c = _knownQuests[i];
                if (c != null && c.Id == questId)
                {
                    return c;
                }
            }

            return null;
        }
    }
}
