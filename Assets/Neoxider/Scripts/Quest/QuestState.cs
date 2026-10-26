using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Runtime state for one quest: status and per-objective progress. Obtain only via QuestManager.GetState.
    /// </summary>
    [Serializable]
    public class QuestState
    {
        [SerializeField] private string _questId;
        [SerializeField] private QuestStatus _status = QuestStatus.NotStarted;
        [SerializeField] private List<int> _objectiveProgress = new();
        [SerializeField] private List<bool> _objectiveCompleted = new();

        public QuestState(string questId, int objectiveCount)
        {
            _questId = questId ?? "";
            for (int i = 0; i < objectiveCount; i++)
            {
                _objectiveProgress.Add(0);
                _objectiveCompleted.Add(false);
            }
        }

        /// <summary>Quest ID (from QuestConfig.Id).</summary>
        public string QuestId => _questId;

        /// <summary>Current quest status.</summary>
        public QuestStatus Status
        {
            get => _status;
            set => _status = value;
        }

        /// <summary>Number of objectives.</summary>
        public int ObjectiveCount => _objectiveProgress.Count;

        /// <summary>Current progress for an objective (for counters: accumulated value).</summary>
        public int GetObjectiveProgress(int index)
        {
            if (index < 0 || index >= _objectiveProgress.Count)
            {
                return 0;
            }

            return _objectiveProgress[index];
        }

        /// <summary>Set objective progress (called from QuestManager).</summary>
        internal void SetObjectiveProgress(int index, int value)
        {
            if (index < 0 || index >= _objectiveProgress.Count)
            {
                return;
            }

            _objectiveProgress[index] = Mathf.Max(0, value);
        }

        /// <summary>Whether the objective at the given index is completed.</summary>
        public bool IsObjectiveCompleted(int index)
        {
            if (index < 0 || index >= _objectiveCompleted.Count)
            {
                return false;
            }

            return _objectiveCompleted[index];
        }

        /// <summary>Mark an objective as completed.</summary>
        internal void MarkObjectiveCompleted(int index)
        {
            if (index < 0 || index >= _objectiveCompleted.Count)
            {
                return;
            }

            _objectiveCompleted[index] = true;
        }

        /// <summary>
        ///     Restores missing lists and grows both to the larger saved/config objective count.
        ///     Existing entries are preserved when configs change or are temporarily unavailable.
        /// </summary>
        internal void EnsureObjectiveCount(int objectiveCount)
        {
            _objectiveProgress ??= new List<int>();
            _objectiveCompleted ??= new List<bool>();
            objectiveCount = Mathf.Max(objectiveCount, Mathf.Max(_objectiveProgress.Count, _objectiveCompleted.Count));
            while (_objectiveProgress.Count < objectiveCount)
            {
                _objectiveProgress.Add(0);
            }

            while (_objectiveCompleted.Count < objectiveCount)
            {
                _objectiveCompleted.Add(false);
            }
        }

        /// <summary>Increase objective progress by amount (for counters).</summary>
        internal int AddObjectiveProgress(int index, int amount)
        {
            if (index < 0 || index >= _objectiveProgress.Count || amount <= 0)
            {
                return GetObjectiveProgress(index);
            }

            _objectiveProgress[index] += amount;
            return _objectiveProgress[index];
        }
    }
}
