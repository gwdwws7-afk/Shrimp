using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 单例模式基类
    /// 使用方法: public class MyManager : Singleton<MyManager>
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"{typeof(T)} (Singleton)";
                            
                            DontDestroyOnLoad(singletonObject);
                            
                            Debug.Log($"[Singleton] An instance of {typeof(T)} was created.");
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Multiple instances of {typeof(T)} found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            
            OnAwake();
        }

        protected virtual void OnAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
