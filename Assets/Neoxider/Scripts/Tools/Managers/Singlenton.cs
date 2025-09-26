using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        public class Singleton<T> : MonoBehaviour where T : Singleton<T>
        {
            public static bool CreateInstance = false;
            [SerializeField] protected bool _dontDestroyOnLoad = false;
            [SerializeField] protected bool _setInstanceOnAwake = true;

            private static T _instance;
            private bool _isInitialized = false;

            public static T I
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);

                        if (_instance == null && CreateInstance)
                        {
                            var obj = new GameObject(typeof(T).Name);
                            _instance = obj.AddComponent<T>();
                            _instance.OnInstanceCreated();
                        }

                        if (_instance != null)
                            if (!_instance._isInitialized)
                                _instance.Init();
                    }

                    return _instance;
                }
            }

            public static bool IsInitialized => _instance != null;

            protected virtual bool DontDestroyOnLoadEnabled => _dontDestroyOnLoad;

            protected virtual bool SetInstanceOnAwakeEnabled => _setInstanceOnAwake;

            protected virtual void OnInstanceCreated()
            {
            }

            protected virtual void Init()
            {
                if (_isInitialized) return;
                _isInitialized = true;
            }

            public static void DestroyInstance()
            {
                if (_instance != null)
                {
                    Destroy(_instance.gameObject);
                    _instance = null;
                }
            }

            protected virtual void Awake()
            {
                if (SetInstanceOnAwakeEnabled)
                {
                    if (_instance == null)
                    {
                        _instance = this as T;
                        if (DontDestroyOnLoadEnabled) DontDestroyOnLoad(gameObject);
                        Init();
                    }
                    else if (_instance != this)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}