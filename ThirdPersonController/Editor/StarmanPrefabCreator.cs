using UnityEngine;
using UnityEditor;
using System.IO;

namespace ThirdPersonController.Editor
{
    /// <summary>
    /// 注释已清理
    /// 注释已清理
    /// </summary>
    public class StarmanPrefabCreator : EditorWindow
    {
        [MenuItem("Tools/Create Starman Prefab")]
        public static void ShowWindow()
        {
            GetWindow<StarmanPrefabCreator>("创建 Starman Prefab");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("信息");
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "此工具将自动：\\n" +
                "[OK] fbx/Characters/starman 导入模型\\n" +
                "2. 创建并配置材质球\n" +
                "3. 娣诲姞鎵€鏈夊繀闇€缁勪欢\n" +
                "4. 创建预制体到 Assets/Prefabs/Enemies/",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("[OK] Starman Prefab", GUILayout.Height(50)))
            {
                CreateStarmanPrefab();
            }
            GUI.backgroundColor = Color.white;
        }
        
        private void CreateStarmanPrefab()
        {
            string modelPath = "Assets/fbx/Characters/starman/Meshy_AI_biped/Meshy_AI_Animation_Walking_frame_rate_60.fbx";
            
            // 注释已清理
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                EditorUtility.DisplayDialog("错误", 
                    "Model file not found.\nPath: " + modelPath + "\n\nPlease verify the model exists.",
                    "确定");
                return;
            }
            
            // 注释已清理
            string prefabFolder = "Assets/Prefabs/Enemies";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
            }
            
            // 注释已清理
            Material material = CreateMaterial();
            
            // 注释已清理
            GameObject instance = Instantiate(modelAsset);
            instance.name = "ENM_Starman_01";
            
            // 注释已清理
            ConfigureRenderer(instance, material);
            
            // 注释已清理
            AddComponents(instance);
            
            // 注释已清理
            instance.layer = LayerMask.NameToLayer("Enemy");
            
            // 注释已清理
            string prefabPath = prefabFolder + "/ENM_Starman_01.prefab";
            
            // 注释已清理
            if (File.Exists(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // 注释已清理
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            
            // 注释已清理
            DestroyImmediate(instance);
            
            // 注释已清理
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            EditorUtility.DisplayDialog("成功", 
                $"[OK] Starman 预制体创建成功！\\n\\n" +
                $"位置: {prefabPath}\\n\\n" +
                $"[OK] \\n" +
                $"[OK] (Standard Shader)\\n" +
                $"- Rigidbody\n" +
                $"- CapsuleCollider\n" +
                $"- NavMeshAgent\n" +
                $"- EnemyAI\n" +
                $"- EnemyHealth\n\n" +
                $"You can now drag this prefab into the scene.",
                "确定");
            
            Debug.Log($"[OK] Starman Prefab 创建完成: {prefabPath}");
        }
        
        private Material CreateMaterial()
        {
            string materialPath = "Assets/Prefabs/Enemies/MAT_Starman_01.mat";
            
            // 注释已清理
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                
                // 注释已清理
                material.SetFloat("_Mode", 0); // Opaque
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                
                // 注释已清理
                string texturePath = "Assets/fbx/Characters/starman/Meshy_AI_biped/";
                
                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Meshy_AI_texture_0.png");
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Meshy_AI_texture_0_normal.png");
                Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath + "Meshy_AI_texture_0_metallic.png");
                
                if (albedo != null)
                {
                    material.SetTexture("_MainTex", albedo);
                    material.SetColor("_Color", Color.white);
                }
                
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.SetFloat("_BumpScale", 1.0f);
                    material.EnableKeyword("_NORMALMAP");
                }
                
                if (metallic != null)
                {
                    material.SetTexture("_MetallicGlossMap", metallic);
                    material.EnableKeyword("_METALLICGLOSSMAP");
                }
                
                // 注释已清理
                material.SetFloat("_Metallic", 0.3f);
                material.SetFloat("_Glossiness", 0.4f);
                
                // 注释已清理
                AssetDatabase.CreateAsset(material, materialPath);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[OK] 材质创建完成: {materialPath}");
            }
            
            return material;
        }
        
        private void ConfigureRenderer(GameObject instance, Material material)
        {
            SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = material;
                
                // 注释已清理
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            
            // 注释已清理
            if (renderers.Length == 0)
            {
                MeshRenderer[] meshRenderers = instance.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in meshRenderers)
                {
                    renderer.material = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }
            }
        }
        
        private void AddComponents(GameObject instance)
        {
            // 1. Rigidbody
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = instance.AddComponent<Rigidbody>();
                rb.mass = 60f;
                rb.drag = 0f;
                rb.angularDrag = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            // 2. CapsuleCollider
            CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = instance.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0);
                col.radius = 0.4f;
                col.height = 1.8f;
                col.direction = 1;
            }
            
            // 3. NavMeshAgent
            UnityEngine.AI.NavMeshAgent agent = instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                agent = instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = 3f;
                agent.angularSpeed = 360f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 1.5f;
                agent.radius = 0.4f;
                agent.height = 1.8f;
                agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
            
            // 4. EnemyHealth
            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health == null)
            {
                health = instance.AddComponent<EnemyHealth>();
            }
            
            // 5. EnemyAI
            EnemyAI ai = instance.GetComponent<EnemyAI>();
            if (ai == null)
            {
                ai = instance.AddComponent<EnemyAI>();
            }
            
            // 注释已清理
            ai.detectionRange = 15f;
            ai.attackRange = 2f;
            ai.fieldOfView = 120f;
            ai.patrolSpeed = 2f;
            ai.chaseSpeed = 3f;
            ai.stoppingDistance = 1.5f;
            ai.attackCooldown = 1.5f;
            ai.attackDamage = 10;
            ai.attackKnockback = 3f;
            ai.playerLayer = LayerMask.GetMask("Player");
            ai.obstructionLayer = LayerMask.GetMask("Default", "Ground");
            
            // 注释已清理
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }
            ai.animator = animator;
        }
    }
}
