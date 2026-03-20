namespace Neo.Quest
{
    /// <summary>
    ///     Статус квеста в рантайме.
    /// </summary>
    public enum QuestStatus
    {
        /// <summary>Квест не взят.</summary>
        NotStarted,

        /// <summary>Квест активен, цели выполняются.</summary>
        Active,

        /// <summary>Все цели выполнены, квест завершён.</summary>
        Completed,

        /// <summary>Квест провален (опционально).</summary>
        Failed
    }
}
