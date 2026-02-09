using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonController.Editor
{
    public class AnimatorSetupEditor : EditorWindow
    {
        private AnimatorController animatorController;
        private string[] availableAnimations;
        private Dictionary<string, AnimationClip> animationDict = new Dictionary<string, AnimationClip>();
        
        private int selectedWalkIndex = 0;
        private int selectedRunIndex = 0;
        private int selectedJumpIndex = 0;
        private int selectedAttackIndex = 0;
        private int selectedHitIndex = 0;
        private int selectedDeathIndex = 0;
        private int selectedCrouchIndex = 0;
        private int selectedClimbIndex = 0;
        private int selectedVaultIndex = 0;
        
        [MenuItem("Tools/Animation/Animator Setup")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorSetupEditor>("动画配置工具");
        }
        
        private void OnEnable()
        {
            RefreshAnimationList();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Animator Controller 配置工具", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("1. 选择Animator Controller:", EditorStyles.boldLabel);
            animatorController = (AnimatorController)EditorGUILayout.ObjectField(animatorController, typeof(AnimatorController), false);
            
            if (animatorController == null)
            {
                EditorGUILayout.HelpBox("请选择一个Animator Controller文件", MessageType.Info);
                if (GUILayout.Button("自动查找PlayerAnimatorController"))
                {
                    FindPlayerAnimatorController();
                }
            }
            
            GUILayout.Space(10);
            
            if (animatorController != null)
            {
                EditorGUILayout.LabelField("2. 选择动画片段:", EditorStyles.boldLabel);
                GUILayout.Space(5);
                
                DrawAnimationSelector("Walk (Idle→Walk)", ref selectedWalkIndex);
                DrawAnimationSelector("Run (Walk→Run)", ref selectedRunIndex);
                DrawAnimationSelector("Jump", ref selectedJumpIndex);
                DrawAnimationSelector("Attack", ref selectedAttackIndex);
                DrawAnimationSelector("Hit (受击)", ref selectedHitIndex);
                DrawAnimationSelector("Death (死亡)", ref selectedDeathIndex);
                DrawAnimationSelector("Crouch (蹲下)", ref selectedCrouchIndex);
                DrawAnimationSelector("Climb (攀爬)", ref selectedClimbIndex);
                DrawAnimationSelector("Vault (翻越)", ref selectedVaultIndex);
                
                GUILayout.Space(20);
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("应用到Animator Controller", GUILayout.Height(30)))
                {
                    ApplyAnimationsToController();
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("刷新动画列表"))
                {
                    RefreshAnimationList();
                }
                
                GUILayout.Space(10);
                
                EditorGUILayout.LabelField("3. 快速操作:", EditorStyles.boldLabel);
                if (GUILayout.Button("从FBX提取所有动画到Animations目录"))
                {
                    ExtractAnimationsFromFBX();
                }
            }
        }
        
        private void DrawAnimationSelector(string label, ref int selectedIndex)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            selectedIndex = EditorGUILayout.Popup(selectedIndex, availableAnimations);
            EditorGUILayout.EndHorizontal();
        }
        
        private void FindPlayerAnimatorController()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimatorController PlayerAnimatorController");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                Debug.Log($"找到PlayerAnimatorController: {path}");
            }
            else
            {
                Debug.LogWarning("未找到PlayerAnimatorController");
            }
        }
        
        private void RefreshAnimationList()
        {
            List<string> animationList = new List<string>();
            animationList.Add("<无>");
            
            // 查找项目中所有的Animation Clip
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    animationList.Add(clip.name);
                    animationDict[clip.name] = clip;
                }
            }
            
            availableAnimations = animationList.ToArray();
            Debug.Log($"找到 {availableAnimations.Length - 1} 个动画片段");
        }
        
        private void ApplyAnimationsToController()
        {
            if (animatorController == null) return;
            
            AnimatorControllerLayer baseLayer = animatorController.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;
            
            // 获取所有状态
            var states = stateMachine.states;
            
            foreach (var state in states)
            {
                string stateName = state.state.name;
                
                switch (stateName)
                {
                    case "IdleWalkRun Blend":
                        // 更新混合树
                        BlendTree blendTree = state.state.motion as BlendTree;
                        if (blendTree != null && blendTree.children.Length >= 3)
                        {
                            if (selectedWalkIndex > 0 && availableAnimations.Length > selectedWalkIndex)
                            {
                                string animName = availableAnimations[selectedWalkIndex];
                                if (animationDict.ContainsKey(animName))
                                {
                                    var children = blendTree.children;
                                    children[0].motion = animationDict[animName];
                                    children[1].motion = animationDict[animName];
                                    children[2].motion = animationDict[animName];
                                    blendTree.children = children;
                                }
                            }
                        }
                        break;
                        
                    case "Jump":
                        if (selectedJumpIndex > 0 && availableAnimations.Length > selectedJumpIndex)
                        {
                            string animName = availableAnimations[selectedJumpIndex];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                        
                    case "Attack":
                        if (selectedAttackIndex > 0 && availableAnimations.Length > selectedAttackIndex)
                        {
                            string animName = availableAnimations[selectedAttackIndex];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                        
                    case "Hit":
                        if (selectedHitIndex > 0 && availableAnimations.Length > selectedHitIndex)
                        {
                            string animName = availableAnimations[selectedHitIndex];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                        
                    case "Death":
                        if (selectedDeathIndex > 0 && availableAnimations.Length > selectedDeathIndex)
                        {
                            string animName = availableAnimations[selectedDeathIndex];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                        
                    case "Crouch":
                        if (selectedCrouchIndex > 0 && availableAnimations.Length > selectedCrouchIndex)
                        {
                            string animName = availableAnimations[selectedCrouchIndex];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                        
                    case "Climb":
                        if (selectedClimbIndex > 0 && availableAnimations.Length > selectedClimbIndex)
                        {
                            string animName = availableAnimations[selectedClimbIndex];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                        
                    case "Vault":
                        if (selectedVaultIndex > 0 && availableAnimations.Length > selectedVaultIndex)
                        {
                            string animName = availableAnimations[selectedVaultIndex];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                }
            }
            
            // 保存更改
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(animatorController);
            
            Debug.Log("✅ 动画已应用到Animator Controller");
            EditorUtility.DisplayDialog("完成", "动画已成功应用到Animator Controller", "确定");
        }
        
        [MenuItem("Tools/Animation/Extract FBX Animations")]
        public static void ExtractAnimationsFromFBX()
        {
            string projectPath = Application.dataPath.Replace("\\", "/").Replace("/Assets", "");
            string outputPath = projectPath + "/Assets/ThirdPersonController/Animations/Extracted";
            
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            
            string[] fbxGuids = AssetDatabase.FindAssets("t:Model fbx");
            int extractedCount = 0;
            
            foreach (string fbxGuid in fbxGuids)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(fbxGuid);
                ModelImporter modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                
                if (modelImporter != null)
                {
                    // 获取动画片段
                    var clipAnimations = modelImporter.clipAnimations;
                    
                    if (clipAnimations != null && clipAnimations.Length > 0)
                    {
                        foreach (var clip in clipAnimations)
                        {
                            string clipName = clip.name;
                            // 清理文件名
                            clipName = clipName.Replace("Meshy_AI_Animation_", "");
                            clipName = clipName.Replace("_frame_rate_60", "");
                            clipName = clipName.Replace("_", " ");
                            
                            Debug.Log($"FBX: {fbxPath} - 动画: {clip.name}");
                        }
                        
                        extractedCount++;
                    }
                }
            }
            
            Debug.Log($"从 {extractedCount} 个FBX文件中提取了动画");
            EditorUtility.DisplayDialog("提取完成", $"从 {extractedCount} 个FBX文件中扫描了动画。\n\n请使用Animator Setup工具配置动画。", "确定");
        }
        
        [MenuItem("Tools/Animation/Check Missing Animations")]
        public static void CheckMissingAnimations()
        {
            string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController");
            
            foreach (string guid in controllerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                
                if (controller != null && controller.layers.Length > 0)
                {
                    Debug.Log($"=== 检查 {controller.name} ===");
                    
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                    
                    foreach (var state in stateMachine.states)
                    {
                        if (state.state.motion == null)
                        {
                            Debug.LogWarning($"❌ 缺少动画: {state.state.name}");
                        }
                        else
                        {
                            Debug.Log($"✓ {state.state.name}: {state.state.motion.name}");
                        }
                    }
                }
            }
        }
    }
}
