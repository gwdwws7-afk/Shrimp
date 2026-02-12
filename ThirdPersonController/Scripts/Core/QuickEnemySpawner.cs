using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 快速敌人生成器 - 用于测试连击系统
    /// 挂在任意 GameObject 上，按 G 键在玩家周围生成敌人
    /// </summary>
    public class QuickEnemySpawner : MonoBehaviour
    {
        [Header("References")]
        public GameObject enemyPrefab;  // 敌人预制体
        public Transform player;        // 玩家位置
        
        [Header("Spawn Settings")]
        public int spawnCount = 10;     // 每次生成数量
        public float spawnRadius = 5f;  // 生成半径
        public float spawnHeight = 0.5f; // 生成高度
        
        [Header("Enemy Settings")]
        public int enemyHealth = 50;    // 敌人血量
        public bool showDebugInfo = true;
        
        void Start()
        {
            if (player == null)
            {
                // 自动查找玩家
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log("🎮 快速敌人生成器已启动");
                Debug.Log("按 G 键生成敌人 | 按 H 键生成大量敌人(50个)");
            }
        }
        
        void Update()
        {
            // 按 G 生成敌人
            if (Input.GetKeyDown(KeyCode.G))
            {
                SpawnEnemies(spawnCount);
            }
            
            // 按 H 生成大量敌人（用于测试50连击）
            if (Input.GetKeyDown(KeyCode.H))
            {
                SpawnEnemies(50);
                Debug.Log("🎯 生成50个敌人！试着达成50连击！");
            }
            
            // 按 Delete 删除所有敌人
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                ClearAllEnemies();
            }
        }
        
        void SpawnEnemies(int count)
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("⚠️ 请先设置 enemyPrefab！");
                return;
            }
            
            if (player == null)
            {
                Debug.LogWarning("⚠️ 找不到玩家！");
                return;
            }
            
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                // 在玩家周围随机位置生成
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = player.position + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
                
                // 确保不会生成在玩家正下方
                if (Vector3.Distance(spawnPos, player.position) < 1f)
                {
                    spawnPos += new Vector3(2f, 0, 2f);
                }
                
                GameObject enemy = ObjectPoolManager.Spawn(enemyPrefab, spawnPos, Quaternion.identity);
                
                // 设置敌人血量
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    // 通过反射或直接修改（根据EnemyHealth的实现）
                    // 这里我们假设可以动态设置
                }
                
                // 让敌人面向玩家
                enemy.transform.LookAt(player);
                
                spawned++;
            }
            
            Debug.Log($"✅ 生成了 {spawned} 个敌人！");
        }
        
        void ClearAllEnemies()
        {
            // 查找场景中所有带有 EnemyHealth 的物体
            EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
            foreach (var enemy in enemies)
            {
                ObjectPoolManager.Despawn(enemy.gameObject);
            }
            
            Debug.Log($"🗑️ 清理了 {enemies.Length} 个敌人");
        }
    }
}
