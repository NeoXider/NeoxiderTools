using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Quest
{
    /// <summary>
    ///     Runtime-состояние одного квеста: статус и прогресс по целям. Получать только через QuestManager.GetState.
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

        /// <summary>ID квеста (из QuestConfig.Id).</summary>
        public string QuestId => _questId;

        /// <summary>Текущий статус квеста.</summary>
        public QuestStatus Status
        {
            get => _status;
            set => _status = value;
        }

        /// <summary>Количество целей.</summary>
        public int ObjectiveCount => _objectiveProgress.Count;

        /// <summary>Текущий прогресс по цели (для счётчиков — накопленное значение).</summary>
        public int GetObjectiveProgress(int index)
        {
            if (index < 0 || index >= _objectiveProgress.Count)
            {
                return 0;
            }

            return _objectiveProgress[index];
        }

        /// <summary>Установить прогресс по цели (вызывается из QuestManager).</summary>
        internal void SetObjectiveProgress(int index, int value)
        {
            if (index < 0 || index >= _objectiveProgress.Count)
            {
                return;
            }

            _objectiveProgress[index] = Mathf.Max(0, value);
        }

        /// <summary>Цель с заданным индексом выполнена.</summary>
        public bool IsObjectiveCompleted(int index)
        {
            if (index < 0 || index >= _objectiveCompleted.Count)
            {
                return false;
            }

            return _objectiveCompleted[index];
        }

        /// <summary>Пометить цель выполненной.</summary>
        internal void MarkObjectiveCompleted(int index)
        {
            if (index < 0 || index >= _objectiveCompleted.Count)
            {
                return;
            }

            _objectiveCompleted[index] = true;
        }

        /// <summary>Увеличить прогресс по цели на 1 (для счётчиков).</summary>
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
