using System.Collections;
using UnityEngine;

namespace ThirdPersonController
{
    public class EffectPoolManager : MonoBehaviour
    {
        public static EffectPoolManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public static void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation, float duration)
        {
            if (prefab == null)
            {
                return;
            }

            EnsureInstance();
            GameObject obj = ObjectPoolManager.Spawn(prefab, position, rotation);
            Instance.StartCoroutine(DespawnAfter(obj, duration));
        }

        private static void EnsureInstance()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject manager = new GameObject("EffectPoolManager");
            Instance = manager.AddComponent<EffectPoolManager>();
        }

        private static IEnumerator DespawnAfter(GameObject obj, float delay)
        {
            if (obj == null)
            {
                yield break;
            }

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            ObjectPoolManager.Despawn(obj);
        }
    }
}
