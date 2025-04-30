using UnityEngine;

namespace Neo
{
    namespace Tools
    {
        public class Singleton<T> : MonoBehaviour where T : Singleton<T>
        {
            [SerializeField] protected bool _dontDestroyOnLoad = false;
            [SerializeField] protected bool _setInstanceOnAwake = true;
            public static bool CreateInstance = false;

            private static T _instance;

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
                        {
                            if (_instance._dontDestroyOnLoad)
                                DontDestroyOnLoad(_instance.gameObject);

                            _instance.Init();
                        }
                    }
                    return _instance;
                }
            }

            public static bool IsInitialized => _instance != null;

            protected virtual void OnInstanceCreated() { }

            protected virtual void Init()
            {

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
                if (_setInstanceOnAwake)
                {
                    if (_instance == null)
                    {
                        _instance = this as T;
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