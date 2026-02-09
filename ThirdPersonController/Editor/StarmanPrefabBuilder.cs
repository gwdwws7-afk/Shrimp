using UnityEngine;
using UnityEditor;

namespace ThirdPersonController.Editor
{
    public class StarmanPrefabBuilder : EditorWindow
    {
        [MenuItem("Tools/Create Starman Prefab Now")]
        public static void ShowWindow()
        {
            GetWindow<StarmanPrefabBuilder>("创建 Starman Prefab");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("一键创建 Starman 敌人 Prefab", EditorStyles.boldLabel);
            
            if (GUILayout.Button("创建 Prefab", GUILayout.Height(40)))
            {
                BuildPrefab();
            }
        }
        
        private void BuildPrefab()
        {
            string modelPath = "Assets/fbx/Characters/starman/Meshy_AI_biped/Meshy_AI_Animation_Walking_frame_rate_60.fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            
            if (model == null)
            {
                EditorUtility.DisplayDialog("错误", "找不到模型文件", "确定");
                return;
            }
            
            // 创建文件夹
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
            
            // 创建材质
            Material mat = new Material(Shader.Find("Standard"));
            string texPath = "Assets/fbx/Characters/starman/Meshy_AI_biped/";
            
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath + "Meshy_AI_texture_0.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath + "Meshy_AI_texture_0_normal.png");
            Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath + "Meshy_AI_texture_0_metallic.png");
            
            if (albedo) mat.SetTexture("_MainTex", albedo);
            if (normal) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
            if (metallic) { mat.SetTexture("_MetallicGlossMap", metallic); mat.EnableKeyword("_METALLICGLOSSMAP"); }
            
            mat.SetFloat("_Metallic", 0.3f);
            mat.SetFloat("_Glossiness", 0.4f);
            
            AssetDatabase.CreateAsset(mat, "Assets/Prefabs/Enemies/MAT_Starman_01.mat");
            
            // 实例化并配置
            GameObject instance = Instantiate(model);
            instance.name = "ENM_Starman_01";
            
            // 设置材质
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.material = mat;
            
            // 添加组件
            instance.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            var col = instance.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.radius = 0.4f;
            col.height = 1.8f;
            
            var agent = instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3f;
            agent.radius = 0.4f;
            agent.height = 1.8f;
            
            instance.AddComponent<EnemyHealth>();
            
            var ai = instance.AddComponent<EnemyAI>();
            ai.animator = instance.GetComponent<Animator>();
            
            instance.layer = LayerMask.NameToLayer("Enemy");
            
            // 保存 Prefab
            string prefabPath = "Assets/Prefabs/Enemies/ENM_Starman_01.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath))
                AssetDatabase.DeleteAsset(prefabPath);
            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            DestroyImmediate(instance);
            
            if (prefab)
            {
                Selection.activeObject = prefab;
                EditorUtility.DisplayDialog("成功", "Prefab 创建成功！\n位置: Assets/Prefabs/Enemies/ENM_Starman_01.prefab", "确定");
            }
        }
    }
}
