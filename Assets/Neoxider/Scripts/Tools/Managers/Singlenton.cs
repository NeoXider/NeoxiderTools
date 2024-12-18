using UnityEngine;

namespace Neoxider
{
    namespace Tools
    {
        public class Singleton<T> : MonoBehaviour where T : Singleton<T>
        {
            [SerializeField] protected bool _dontDestroyOnLoad = false;

            private static T _instance;

            public static T Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            var obj = new GameObject(typeof(T).Name);
                            _instance = obj.AddComponent<T>();
                            _instance.OnInstanceCreated();
                        }

                        _instance.Initialize();
                    }
                    return _instance;
                }
            }

            public static bool IsInitialized => _instance != null;

            protected virtual void OnInstanceCreated() { }

            protected virtual void Initialize()
            {
                if (_dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
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
                if (_instance == null)
                {
                    _instance = this as T;
                    Initialize();
                }
                else if (_instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}