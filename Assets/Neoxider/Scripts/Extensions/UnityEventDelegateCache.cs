using System.Collections.Generic;
using UnityEngine.Events;

namespace Neo.Extensions
{
    /// <summary>
    ///     Кэш делегатов для корректной отписки от UnityEvent: хранит ссылки на те же экземпляры,
    ///     которые передавались в AddListener, чтобы вызывать RemoveListener с тем же объектом.
    ///     Используется при динамических подписках по индексу (кнопки, элементы списка).
    /// </summary>
    public class UnityEventDelegateCache
    {
        private readonly List<UnityAction> _delegates = new();

        /// <summary>Количество закэшированных делегатов.</summary>
        public int Count => _delegates.Count;

        /// <summary>Возвращает делегат по индексу для вызова RemoveListener.</summary>
        public UnityAction this[int index] => _delegates[index];

        /// <summary>Добавляет делегат в кэш. После этого его можно передать в AddListener и затем снять через UnsubscribeAt.</summary>
        public void Add(UnityAction action)
        {
            _delegates.Add(action);
        }

        /// <summary>Подписывает на событие делегат по индексу и сохраняет его в кэше (при необходимости расширяет кэш).</summary>
        /// <param name="index">Индекс слота (0..N-1).</param>
        /// <param name="evt">Событие (например button.onClick).</param>
        /// <param name="action">Делегат (например () => Handler(index)).</param>
        public void SubscribeAt(int index, UnityEvent evt, UnityAction action)
        {
            while (_delegates.Count <= index)
            {
                _delegates.Add(null);
            }

            _delegates[index] = action;
            evt?.AddListener(action);
        }

        /// <summary>Отписывает от события делегат, сохранённый по индексу.</summary>
        public void UnsubscribeAt(int index, UnityEvent evt)
        {
            if (evt == null || index < 0 || index >= _delegates.Count)
            {
                return;
            }

            UnityAction a = _delegates[index];
            if (a != null)
            {
                evt.RemoveListener(a);
            }
        }

        /// <summary>Очищает кэш. Не снимает подписки с событий — перед Clear нужно вызвать UnsubscribeAt для каждого индекса.</summary>
        public void Clear()
        {
            _delegates.Clear();
        }
    }
}