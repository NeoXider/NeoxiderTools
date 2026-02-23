using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Reactive
{
    [Serializable]
    public class UnityEventFloat : UnityEvent<float> { }

    [Serializable]
    public class UnityEventInt : UnityEvent<int> { }

    [Serializable]
    public class UnityEventBool : UnityEvent<bool> { }

    /// <summary>
    ///     Реактивная переменная (float): значение + UnityEvent. API в стиле R3.
    /// </summary>
    [Serializable]
    public class ReactivePropertyFloat
    {
        [SerializeField] private float _value;
        [SerializeField] private UnityEventFloat _onChanged = new();

        public ReactivePropertyFloat() { }

        public ReactivePropertyFloat(float initialValue)
        {
            _value = initialValue;
        }

        /// <summary>Текущее значение (только чтение).</summary>
        public float CurrentValue => _value;

        /// <summary>Значение; при set вызывается OnChanged.</summary>
        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                _onChanged?.Invoke(_value);
            }
        }

        /// <summary>Подписка на изменение (из кода и из Inspector). AddListener / RemoveListener / RemoveAllListeners.</summary>
        public UnityEventFloat OnChanged => _onChanged;

        /// <summary>Подписаться на изменение (удобная обёртка над OnChanged.AddListener).</summary>
        public void AddListener(UnityAction<float> call) => _onChanged?.AddListener(call);

        /// <summary>Отписаться от изменения (удобная обёртка над OnChanged.RemoveListener).</summary>
        public void RemoveListener(UnityAction<float> call) => _onChanged?.RemoveListener(call);

        /// <summary>Отписать всех подписчиков (удобная обёртка над OnChanged.RemoveAllListeners).</summary>
        public void RemoveAllListeners() => _onChanged?.RemoveAllListeners();

        /// <summary>Установить значение и уведомить подписчиков.</summary>
        public void OnNext(float value)
        {
            Value = value;
        }

        /// <summary>Записать значение без вызова OnChanged (например при Load).</summary>
        public void SetValueWithoutNotify(float value)
        {
            _value = value;
        }

        /// <summary>Вызвать OnChanged с текущим значением.</summary>
        public void ForceNotify()
        {
            _onChanged?.Invoke(_value);
        }
    }

    /// <summary>
    ///     Реактивная переменная (int): значение + UnityEvent. API в стиле R3.
    /// </summary>
    [Serializable]
    public class ReactivePropertyInt
    {
        [SerializeField] private int _value;
        [SerializeField] private UnityEventInt _onChanged = new();

        public ReactivePropertyInt() { }

        public ReactivePropertyInt(int initialValue)
        {
            _value = initialValue;
        }

        /// <summary>Текущее значение (только чтение).</summary>
        public int CurrentValue => _value;

        /// <summary>Значение; при set вызывается OnChanged.</summary>
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _onChanged?.Invoke(_value);
            }
        }

        /// <summary>Подписка на изменение (из кода и из Inspector). AddListener / RemoveListener / RemoveAllListeners.</summary>
        public UnityEventInt OnChanged => _onChanged;

        /// <summary>Подписаться на изменение (удобная обёртка над OnChanged.AddListener).</summary>
        public void AddListener(UnityAction<int> call) => _onChanged?.AddListener(call);

        /// <summary>Отписаться от изменения (удобная обёртка над OnChanged.RemoveListener).</summary>
        public void RemoveListener(UnityAction<int> call) => _onChanged?.RemoveListener(call);

        /// <summary>Отписать всех подписчиков (удобная обёртка над OnChanged.RemoveAllListeners).</summary>
        public void RemoveAllListeners() => _onChanged?.RemoveAllListeners();

        /// <summary>Установить значение и уведомить подписчиков.</summary>
        public void OnNext(int value)
        {
            Value = value;
        }

        /// <summary>Записать значение без вызова OnChanged (например при Load).</summary>
        public void SetValueWithoutNotify(int value)
        {
            _value = value;
        }

        /// <summary>Вызвать OnChanged с текущим значением.</summary>
        public void ForceNotify()
        {
            _onChanged?.Invoke(_value);
        }
    }

    /// <summary>
    ///     Реактивная переменная (bool): значение + UnityEvent. API в стиле R3. По умолчанию false.
    /// </summary>
    [Serializable]
    public class ReactivePropertyBool
    {
        [SerializeField] private bool _value;
        [SerializeField] private UnityEventBool _onChanged = new();

        public ReactivePropertyBool() { }

        public ReactivePropertyBool(bool initialValue)
        {
            _value = initialValue;
        }

        /// <summary>Текущее значение (только чтение).</summary>
        public bool CurrentValue => _value;

        /// <summary>Значение; при set вызывается OnChanged.</summary>
        public bool Value
        {
            get => _value;
            set
            {
                _value = value;
                _onChanged?.Invoke(_value);
            }
        }

        /// <summary>Подписка на изменение (из кода и из Inspector). AddListener / RemoveListener / RemoveAllListeners.</summary>
        public UnityEventBool OnChanged => _onChanged;

        /// <summary>Подписаться на изменение (удобная обёртка над OnChanged.AddListener).</summary>
        public void AddListener(UnityAction<bool> call) => _onChanged?.AddListener(call);

        /// <summary>Отписаться от изменения (удобная обёртка над OnChanged.RemoveListener).</summary>
        public void RemoveListener(UnityAction<bool> call) => _onChanged?.RemoveListener(call);

        /// <summary>Отписать всех подписчиков (удобная обёртка над OnChanged.RemoveAllListeners).</summary>
        public void RemoveAllListeners() => _onChanged?.RemoveAllListeners();

        /// <summary>Установить значение и уведомить подписчиков.</summary>
        public void OnNext(bool value)
        {
            Value = value;
        }

        /// <summary>Записать значение без вызова OnChanged (например при Load).</summary>
        public void SetValueWithoutNotify(bool value)
        {
            _value = value;
        }

        /// <summary>Вызвать OnChanged с текущим значением.</summary>
        public void ForceNotify()
        {
            _onChanged?.Invoke(_value);
        }
    }
}
