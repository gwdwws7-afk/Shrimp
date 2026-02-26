using UnityEngine;
using UnityEditor;
using System.IO;

namespace ThirdPersonController.Editor
{
    /// <summary>
    /// Starman 敌人预制体一键创建器
    /// 自动配置材质、组件并创建 Prefab
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
            GUILayout.Label("🦈 创建 Starman 敌人预制体", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "此工具将自动：\n" +
                "1. 从 fbx/Characters/starman 导入模型\n" +
                "2. 创建并配置材质球\n" +
                "3. 添加所有必需组件\n" +
                "4. 创建预制体到 Assets/Prefabs/Enemies/",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 一键创建 Starman Prefab", GUILayout.Height(50)))
            {
                CreateStarmanPrefab();
            }
            GUI.backgroundColor = Color.white;
        }
        
        private void CreateStarmanPrefab()
        {
            string modelPath = "Assets/fbx/Characters/starman/Meshy_AI_biped/Meshy_AI_Animation_Walking_frame_rate_60.fbx";
            
            // 检查模型是否存在
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                EditorUtility.DisplayDialog("错误", 
                    "找不到模型文件！\n路径: " + modelPath + "\n\n请确认模型文件存在。", 
                    "确定");
                return;
            }
            
            // 创建预制体目录
            string prefabFolder = "Assets/Prefabs/Enemies";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
            }
            
            // 创建材质球
            Material material = CreateMaterial();
            
            // 实例化模型
            GameObject instance = Instantiate(modelAsset);
            instance.name = "ENM_Starman_01";
            
            // 配置材质
            ConfigureRenderer(instance, material);
            
            // 添加组件
            AddComponents(instance);
            
            // 设置层级
            instance.layer = LayerMask.NameToLayer("Enemy");
            
            // 保存预制体
            string prefabPath = prefabFolder + "/ENM_Starman_01.prefab";
            
            // 如果已存在则删除
            if (File.Exists(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // 创建预制体
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            
            // 销毁场景实例
            DestroyImmediate(instance);
            
            // 选中预制体
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            EditorUtility.DisplayDialog("成功", 
                $"✅ Starman 预制体创建成功！\n\n" +
                $"位置: {prefabPath}\n\n" +
                $"已配置:\n" +
                $"- 材质球 (Standard Shader)\n" +
                $"- Rigidbody\n" +
                $"- CapsuleCollider\n" +
                $"- NavMeshAgent\n" +
                $"- EnemyAI\n" +
                $"- EnemyHealth\n\n" +
                $"现在可以将此预制体拖到场景中使用！",
                "确定");
            
            Debug.Log($"✅ Starman Prefab 创建完成: {prefabPath}");
        }
        
        private Material CreateMaterial()
        {
            string materialPath = "Assets/Prefabs/Enemies/MAT_Starman_01.mat";
            
            // 加载或创建材质
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                
                // 配置 Standard Shader
                material.SetFloat("_Mode", 0); // Opaque
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                
                // 加载贴图
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
                
                // 设置参数
                material.SetFloat("_Metallic", 0.3f);
                material.SetFloat("_Glossiness", 0.4f);
                
                // 保存材质
                AssetDatabase.CreateAsset(material, materialPath);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"✅ 材质创建完成: {materialPath}");
            }
            
            return material;
        }
        
        private void ConfigureRenderer(GameObject instance, Material material)
        {
            SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = material;
                
                // 设置阴影
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            
            // 如果没有 SkinnedMeshRenderer，尝试 MeshRenderer
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
            
            // 配置 AI 参数
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
            
            // 设置 Animator
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }
            ai.animator = animator;
        }
    }
}
