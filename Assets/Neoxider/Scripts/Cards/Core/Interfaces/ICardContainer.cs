using System;
using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    ///     Базовый интерфейс контейнера карт (модельный слой).
    /// </summary>
    public interface ICardContainer
    {
        CardLocation Location { get; }
        int Count { get; }
        IReadOnlyList<CardData> Data { get; }
        event Action OnChanged;

        bool CanAdd(CardData card);
        void Add(CardData card);
        bool Remove(CardData card);
        List<CardData> RemoveAll();
        void Clear();
    }
}