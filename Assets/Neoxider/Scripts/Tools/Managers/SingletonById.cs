using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Синглтон по уникальному Id: в один момент времени только один экземпляр с данным Id.
    ///     При появлении второго объекта с тем же Id предыдущий уничтожается (побеждает новый).
    ///     Опционально объект можно не уничтожать при смене сцен (DontDestroyOnLoad).
    /// </summary>
    /// <typeparam name="T">Тип компонента (наследник SingletonById&lt;T&gt;).</typeparam>
    [NeoDoc("Tools/Managers/SingletonById.md")]
    [DisallowMultipleComponent]
    public class SingletonById<T> : MonoBehaviour where T : SingletonById<T>
    {
        private static readonly Dictionary<string, T> ById = new();

        [Header("Singleton by Id")]
        [Tooltip("Unique identifier. Only one instance per Id; when a new one appears, the previous is destroyed.")]
        [SerializeField] private string _id = "Default";

        [Tooltip("Do not destroy this object when loading new scenes (persists across scenes).")]
        [SerializeField] private bool _dontDestroyOnLoad;

        /// <summary>Уникальный идентификатор этого синглтона.</summary>
        public string Id => _id;

        /// <summary>Получить экземпляр по Id. Если нет — null.</summary>
        public static T Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return ById.TryGetValue(id, out var instance) ? instance : null;
        }

        /// <summary>Есть ли живой экземпляр с данным Id.</summary>
        public static bool Has(string id) => Get(id) != null;

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[SingletonById] На {gameObject.name} не задан Id, экземпляр не регистрируется.", this);
                return;
            }

            if (ById.TryGetValue(_id, out var existing) && existing != null && existing != this)
            {
                Destroy(existing.gameObject);
            }

            ById[_id] = (T)this;

            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (string.IsNullOrEmpty(_id))
                return;
            if (ById.TryGetValue(_id, out var current) && current == this)
            {
                ById.Remove(_id);
            }
        }
    }
}
