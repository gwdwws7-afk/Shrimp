using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class EnemyPoolPrewarmer : MonoBehaviour
    {
        [System.Serializable]
        public class PrewarmEntry
        {
            public GameObject prefab;
            public int count = 50;
        }

        [Header("Prewarm")]
        public bool runOnStart = true;
        public int batchSize = 10;
        public float batchDelay = 0f;
        public List<PrewarmEntry> entries = new List<PrewarmEntry>();

        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(PrewarmRoutine());
            }
        }

        private IEnumerator PrewarmRoutine()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                PrewarmEntry entry = entries[i];
                if (entry == null || entry.prefab == null || entry.count <= 0)
                {
                    continue;
                }

                int spawned = 0;
                while (spawned < entry.count)
                {
                    int batch = Mathf.Min(batchSize, entry.count - spawned);
                    for (int b = 0; b < batch; b++)
                    {
                        GameObject obj = ObjectPoolManager.Spawn(entry.prefab, transform.position, Quaternion.identity);
                        ObjectPoolManager.Despawn(obj);
                    }

                    spawned += batch;
                    if (batchDelay > 0f)
                    {
                        yield return new WaitForSeconds(batchDelay);
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }
    }
}
