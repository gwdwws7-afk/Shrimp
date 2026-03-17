using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// Debug-only spawner used for combat stress tests.
    /// </summary>
    public class QuickEnemySpawner : MonoBehaviour
    {
        [Header("References")]
        public GameObject enemyPrefab;
        public Transform player;
        public PlayerInputHandler inputHandler;

        [Header("Spawn Settings")]
        public int spawnCount = 10;
        public float spawnRadius = 5f;
        public float spawnHeight = 0.5f;

        [Header("Input")]
        public string spawnActionName = "DebugSpawnerWave";
        public string stressSpawnActionName = "DebugSpawnerStress";
        public string clearActionName = "DebugSpawnerClear";
        public KeyCode spawnKey = KeyCode.G;
        public KeyCode stressSpawnKey = KeyCode.F8;
        public KeyCode clearKey = KeyCode.Delete;

        [Header("Debug")]
        public bool showDebugInfo = true;

        private void Start()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }

            if (showDebugInfo)
            {
                string spawnBinding = ResolveBindingLabel(spawnActionName, spawnKey);
                string stressBinding = ResolveBindingLabel(stressSpawnActionName, stressSpawnKey);
                string clearBinding = ResolveBindingLabel(clearActionName, clearKey);
                Debug.Log($"[Spawner] {spawnBinding}=spawn wave, {stressBinding}=spawn 50, {clearBinding}=clear enemies.");
            }
        }

        private void Update()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            bool spawnPressed = handler != null && handler.WasActionPressedThisFrame(spawnActionName, spawnKey);
            if (spawnPressed)
            {
                SpawnEnemies(spawnCount);
            }

            bool stressPressed = handler != null && handler.WasActionPressedThisFrame(stressSpawnActionName, stressSpawnKey);
            if (stressPressed)
            {
                SpawnEnemies(50);
                Debug.Log("[Spawner] Spawned 50 enemies for stress test.");
            }

            bool clearPressed = handler != null && handler.WasActionPressedThisFrame(clearActionName, clearKey);
            if (clearPressed)
            {
                ClearAllEnemies();
            }
        }

        private PlayerInputHandler ResolveInputHandler()
        {
            if (inputHandler != null)
            {
                return inputHandler;
            }

            inputHandler = PlayerInputHandler.ResolveActiveInstance();
            return inputHandler;
        }

        private string ResolveBindingLabel(string actionName, KeyCode fallbackKey)
        {
            PlayerInputHandler handler = ResolveInputHandler();
            if (handler == null)
            {
                return PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey);
            }

            if (!handler.AreDebugHotkeysEnabled())
            {
                return "Disabled";
            }

            string binding = handler.GetActionBindingLabel(actionName, fallbackKey, includeGamepad: false);
            return string.IsNullOrEmpty(binding)
                ? PlayerInputHandler.GetFriendlyKeyLabel(fallbackKey)
                : binding;
        }

        private void SpawnEnemies(int count)
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("[Spawner] enemyPrefab is not assigned.");
                return;
            }

            if (player == null)
            {
                Debug.LogWarning("[Spawner] Player transform is missing, cannot spawn enemies.");
                return;
            }

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = player.position + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
                if (Vector3.Distance(spawnPos, player.position) < 1f)
                {
                    spawnPos += new Vector3(2f, 0f, 2f);
                }

                GameObject enemy = ObjectPoolManager.Spawn(enemyPrefab, spawnPos, Quaternion.identity);
                enemy.transform.LookAt(player);
                spawned++;
            }

            Debug.Log($"[Spawner] Spawned {spawned} enemies.");
        }

        private void ClearAllEnemies()
        {
            EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
            foreach (EnemyHealth enemy in enemies)
            {
                ObjectPoolManager.Despawn(enemy.gameObject);
            }

            Debug.Log($"[Spawner] Cleared {enemies.Length} enemies.");
        }
    }
}
