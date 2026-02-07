using UnityEngine;
using UnityEditor;

namespace ThirdPersonController.Editor
{
    /// <summary>
    /// 海星人敌人自动配置工具
    /// 选中模型后自动设置材质并创建Prefab
    /// </summary>
    public class StarfishEnemyConfigurator : EditorWindow
    {
        private GameObject modelPrefab;
        private bool createPrefab = true;
        private string prefabName = "ENM_Starfish_01";
        
        [MenuItem("Tools/Enemy Configurator/Configure Starfish Enemy")]
        public static void ShowWindow()
        {
            GetWindow<StarfishEnemyConfigurator>("海星人配置器");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🌟 海星人敌人配置器", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "自动配置海星人模型：\n" +
                "1. 设置材质和贴图\n" +
                "2. 添加碰撞体\n" +
                "3. 添加AI脚本\n" +
                "4. 创建Prefab",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            modelPrefab = EditorGUILayout.ObjectField(
                "模型预制体", modelPrefab, typeof(GameObject), false) as GameObject;
            
            EditorGUILayout.Space();
            
            createPrefab = EditorGUILayout.Toggle("创建Prefab", createPrefab);
            
            if (createPrefab)
            {
                prefabName = EditorGUILayout.TextField("Prefab名称", prefabName);
            }
            
            EditorGUILayout.Space();
            
            GUI.enabled = modelPrefab != null;
            
            if (GUILayout.Button("🚀 自动配置", GUILayout.Height(40)))
            {
                ConfigureStarfishEnemy();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("手动配置步骤:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. 将模型拖到场景中", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("2. 设置材质球 (Standard Shader)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("3. 绑定Albedo/Normal/Metallic贴图", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("4. 添加Rigidbody组件", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("5. 添加CapsuleCollider", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("6. 添加NavMeshAgent", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("7. 添加EnemyAI和EnemyHealth脚本", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("8. 设置Layer为Enemy", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("9. 拖到Project窗口创建Prefab", EditorStyles.miniLabel);
        }
        
        private void ConfigureStarfishEnemy()
        {
            if (modelPrefab == null) return;
            
            // 实例化到场景
            GameObject instance = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
            
            // 1. 设置材质
            SetupMaterials(instance);
            
            // 2. 添加物理组件
            SetupPhysics(instance);
            
            // 3. 添加AI组件
            SetupAI(instance);
            
            // 4. 设置层级
            instance.layer = LayerMask.NameToLayer("Enemy");
            
            // 5. 创建Prefab
            if (createPrefab)
            {
                CreatePrefab(instance);
            }
            
            // 选中实例
            Selection.activeGameObject = instance;
            
            EditorUtility.DisplayDialog("配置完成", 
                $"海星人敌人配置完成！\n" +
                $"Prefab名称: {prefabName}\n" +
                $"已添加到场景并选中。",
                "确定");
        }
        
        private void SetupMaterials(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                // 创建或获取材质
                Material mat = renderer.sharedMaterial;
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Standard"));
                }
                
                // 设置Standard Shader参数
                mat.shader = Shader.Find("Standard");
                mat.SetFloat("_Mode", 0); // Opaque
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
                
                // 尝试加载贴图
                string basePath = AssetDatabase.GetAssetPath(modelPrefab);
                string folderPath = System.IO.Path.GetDirectoryName(basePath);
                
                // 加载Albedo
                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    folderPath + "/Meshy_AI_texture_0.png");
                if (albedo != null) mat.SetTexture("_MainTex", albedo);
                
                // 加载Normal
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    folderPath + "/Meshy_AI_texture_0_normal.png");
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.SetFloat("_BumpScale", 1.0f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                
                // 加载Metallic
                Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    folderPath + "/Meshy_AI_texture_0_metallic.png");
                if (metallic != null)
                {
                    mat.SetTexture("_MetallicGlossMap", metallic);
                    mat.EnableKeyword("_METALLICGLOSSMAP");
                }
                
                // 设置金属度和光滑度
                mat.SetFloat("_Metallic", 0.3f);
                mat.SetFloat("_Glossiness", 0.4f);
                
                renderer.material = mat;
            }
        }
        
        private void SetupPhysics(GameObject instance)
        {
            // Rigidbody
            if (instance.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = instance.AddComponent<Rigidbody>();
                rb.mass = 60f;
                rb.drag = 0f;
                rb.angularDrag = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            // CapsuleCollider
            if (instance.GetComponent<CapsuleCollider>() == null)
            {
                CapsuleCollider col = instance.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0);
                col.radius = 0.4f;
                col.height = 1.8f;
                col.direction = 1; // Y-axis
            }
            
            // NavMeshAgent
            if (instance.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
            {
                UnityEngine.AI.NavMeshAgent agent = instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = 3f;
                agent.angularSpeed = 360f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 1.5f;
                agent.radius = 0.4f;
                agent.height = 1.8f;
            }
        }
        
        private void SetupAI(GameObject instance)
        {
            // EnemyHealth
            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health == null)
            {
                health = instance.AddComponent<EnemyHealth>();
            }
            // 通过反射设置maxHealth (如果字段是private)
            // health.maxHealth = 40;
            
            // EnemyAI
            EnemyAI ai = instance.GetComponent<EnemyAI>();
            if (ai == null)
            {
                ai = instance.AddComponent<EnemyAI>();
            }
            
            // 设置AI参数
            ai.detectionRange = 15f;
            ai.attackRange = 2f;
            ai.attackCooldown = 1.5f;
            ai.attackDamage = 8;
            ai.patrolSpeed = 2f;
            ai.chaseSpeed = 3f;
            ai.fieldOfView = 120f;
            
            // 设置Animator
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }
            ai.animator = animator;
        }
        
        private void CreatePrefab(GameObject instance)
        {
            string prefabPath = $"Assets/Prefabs/Enemies/{prefabName}.prefab";
            
            // 确保目录存在
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
            }
            
            // 创建或覆盖Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            
            Debug.Log($"✅ Prefab创建成功: {prefabPath}");
        }
    }
}
