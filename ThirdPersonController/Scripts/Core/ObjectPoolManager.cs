using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            poolRoot = new GameObject("PoolRoot").transform;
            poolRoot.SetParent(transform);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            EnsureInstance();

            Queue<GameObject> queue = Instance.GetQueue(prefab);
            GameObject obj = null;

            while (queue.Count > 0 && obj == null)
            {
                obj = queue.Dequeue();
            }

            if (obj == null)
            {
                obj = Instantiate(prefab, position, rotation, parent);
                PooledObject pooled = obj.GetComponent<PooledObject>();
                if (pooled == null)
                {
                    pooled = obj.AddComponent<PooledObject>();
                }
                pooled.Initialize(prefab, Instance);
            }
            else
            {
                obj.transform.SetParent(parent);
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }

            NotifySpawned(obj);
            return obj;
        }

        public static void Despawn(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            PooledObject pooled = obj.GetComponent<PooledObject>();
            if (pooled == null || pooled.Prefab == null || pooled.Owner == null)
            {
                Destroy(obj);
                return;
            }

            NotifyDespawned(obj);
            obj.SetActive(false);
            obj.transform.SetParent(pooled.Owner.poolRoot);
            pooled.Owner.GetQueue(pooled.Prefab).Enqueue(obj);
        }

        private Queue<GameObject> GetQueue(GameObject prefab)
        {
            if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pools.Add(prefab, queue);
            }

            return queue;
        }

        private static void EnsureInstance()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject manager = new GameObject("ObjectPoolManager");
            Instance = manager.AddComponent<ObjectPoolManager>();
        }

        private static void NotifySpawned(GameObject obj)
        {
            IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawned();
            }
        }

        private static void NotifyDespawned(GameObject obj)
        {
            IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawned();
            }
        }
    }
}
