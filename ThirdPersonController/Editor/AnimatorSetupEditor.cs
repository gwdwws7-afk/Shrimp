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
        private int selectedAttack2Index = 0;
        private int selectedAttack3Index = 0;
        private int selectedAttackBIndex = 0;
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
                EditorGUILayout.HelpBox("请选择一个 Animator Controller 文件。", MessageType.Info);
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
                DrawAnimationSelector("Attack 1 (Light)", ref selectedAttackIndex);
                DrawAnimationSelector("Attack 2 (Light)", ref selectedAttack2Index);
                DrawAnimationSelector("Attack 3 (Light)", ref selectedAttack3Index);
                DrawAnimationSelector("Attack B (Heavy)", ref selectedAttackBIndex);
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
                
                EditorGUILayout.LabelField("高级工具", EditorStyles.boldLabel);
                if (GUILayout.Button("从FBX提取所有动画到 Animations 目录"))
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
            animationList.Add("(None)");
            
            // 注释已清理
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
            Debug.Log($"[AnimatorSetup] 已刷新动画列表，共 {availableAnimations.Length - 1} 个可选动画。");
        }
        
        private void ApplyAnimationsToController()
        {
            if (animatorController == null) return;
            
            AnimatorControllerLayer baseLayer = animatorController.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;
            
            // 注释已清理
            var states = stateMachine.states;
            
            foreach (var state in states)
            {
                string stateName = state.state.name;
                
                switch (stateName)
                {
                    case "IdleWalkRun Blend":
                        // 注释已清理
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
                    case "Attack_2":
                        if (selectedAttack2Index > 0 && availableAnimations.Length > selectedAttack2Index)
                        {
                            string animName = availableAnimations[selectedAttack2Index];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                    case "Attack_3":
                        if (selectedAttack3Index > 0 && availableAnimations.Length > selectedAttack3Index)
                        {
                            string animName = availableAnimations[selectedAttack3Index];
                            if (animationDict.ContainsKey(animName))
                            {
                                state.state.motion = animationDict[animName];
                            }
                        }
                        break;
                    case "Attack_B":
                        if (selectedAttackBIndex > 0 && availableAnimations.Length > selectedAttackBIndex)
                        {
                            string animName = availableAnimations[selectedAttackBIndex];
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
            
            // 注释已清理
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(animatorController);
            
            Debug.Log("[AnimatorSetup] 动画已应用到 Animator Controller。");
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
                    // 注释已清理
                    var clipAnimations = modelImporter.clipAnimations;
                    
                    if (clipAnimations != null && clipAnimations.Length > 0)
                    {
                        foreach (var clip in clipAnimations)
                        {
                            string clipName = clip.name;
                            // 注释已清理
                            clipName = clipName.Replace("Meshy_AI_Animation_", "");
                            clipName = clipName.Replace("_frame_rate_60", "");
                            clipName = clipName.Replace("_", " ");
                            
                            Debug.Log($"FBX: {fbxPath} - 动画: {clip.name}");
                        }
                        
                        extractedCount++;
                    }
                }
            }
            
            Debug.Log($"[AnimatorSetup] 已扫描 {extractedCount} 个含动画片段的 FBX。");
            EditorUtility.DisplayDialog("提示", "操作已完成。", "确定");
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
                    Debug.Log($"[AnimatorSetup] 检查控制器: {controller.name}");
                    
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                    
                    foreach (var state in stateMachine.states)
                    {
                        if (state.state.motion == null)
                        {
                            Debug.LogWarning($"[AnimatorSetup] 缺少动画: {state.state.name}");
                        }
                        else
                        {
                            Debug.Log($"[AnimatorSetup] {state.state.name}: {state.state.motion.name}");
                        }
                    }
                }
            }
        }
    }
}
