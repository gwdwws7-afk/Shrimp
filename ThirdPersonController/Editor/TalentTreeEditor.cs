using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ThirdPersonController.Editor
{
    public class TalentTreeEditor : EditorWindow
    {
        private TalentTreeData talentTreeData;
        private Vector2 scrollPosition;
        private int selectedNodeIndex = -1;
        private string newNodeId = "";
        private string newNodeTitle = "";
        private int newNodeCost = 1;
        private TalentBranch newNodeBranch = TalentBranch.Offense;
        
        [MenuItem("Tools/Progression/Talent Tree Editor")]
        public static void ShowWindow()
        {
            GetWindow<TalentTreeEditor>("天赋树编辑器");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("天赋树编辑器", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            talentTreeData = (TalentTreeData)EditorGUILayout.ObjectField(talentTreeData, typeof(TalentTreeData), false);
            
            if (talentTreeData == null)
            {
                if (GUILayout.Button("创建新天赋树"))
                {
                    CreateNewTalentTree();
                }
                
                EditorGUILayout.HelpBox("请选择一个天赋树数据文件。", MessageType.Info);
                return;
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField($"Overview", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < talentTreeData.nodes.Count; i++)
            {
                DrawNode(talentTreeData.nodes[i], i);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            
            newNodeId = EditorGUILayout.TextField("ID:", newNodeId);
            newNodeTitle = EditorGUILayout.TextField("名称:", newNodeTitle);
            newNodeCost = EditorGUILayout.IntField("Settings", newNodeCost);
            newNodeBranch = (TalentBranch)EditorGUILayout.EnumPopup("分支:", newNodeBranch);
            
            if (GUILayout.Button("添加节点"))
            {
                AddNewNode();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("保存"))
            {
                SaveTalentTree();
            }
            
            if (GUILayout.Button("生成45节点模板"))
            {
                GenerateFullTree();
            }
        }
        
        private void DrawNode(TalentNodeData node, int index)
        {
            EditorGUILayout.BeginHorizontal();
            
            string branchIcon = "";
            switch (node.branch)
            {
                case TalentBranch.Offense:
                    branchIcon = "🔴";
                    break;
                case TalentBranch.Control:
                    branchIcon = "🔵";
                    break;
                case TalentBranch.Survival:
                    branchIcon = "🟢";
                    break;
            }
            
            bool isSelected = selectedNodeIndex == index;
            string label = $"{branchIcon} {node.id}: {node.title} (Cost: {node.cost})";
            
            if (isSelected)
            {
                GUI.backgroundColor = Color.yellow;
            }
            
            if (GUILayout.Button(label, EditorStyles.miniButtonLeft))
            {
                selectedNodeIndex = isSelected ? -1 : index;
            }
            
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(30)))
            {
                RemoveNode(index);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (isSelected)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                node.id = EditorGUILayout.TextField("ID:", node.id);
                node.title = EditorGUILayout.TextField("名称:", node.title);
                node.branch = (TalentBranch)EditorGUILayout.EnumPopup("分支:", node.branch);
                node.cost = EditorGUILayout.IntField("Settings", node.cost);
                
                EditorGUILayout.LabelField("前置节点:");
                for (int i = 0; i < node.prerequisites.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    node.prerequisites[i] = EditorGUILayout.TextField($"Prereq {i}:", node.prerequisites[i]);
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        node.prerequisites.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("添加前置节点"))
                {
                    node.prerequisites.Add("");
                }
                
                EditorGUILayout.LabelField("Settings");
                for (int i = 0; i < node.modifiers.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    StatModifier modifier = node.modifiers[i];
                    modifier.stat = (StatType)EditorGUILayout.EnumPopup(modifier.stat, GUILayout.Width(120));
                    modifier.type = (ModifierType)EditorGUILayout.EnumPopup(modifier.type, GUILayout.Width(80));
                    modifier.value = EditorGUILayout.FloatField(modifier.value);
                    node.modifiers[i] = modifier;
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        node.modifiers.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("Add Modifier"))
                {
                    node.modifiers.Add(new StatModifier { stat = StatType.AttackDamage, type = ModifierType.Percent, value = 0.1f });
                }
                
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }
        
        private void CreateNewTalentTree()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Talent Tree", "TalentTree_", "asset", "Save Talent Tree");
            if (!string.IsNullOrEmpty(path))
            {
                talentTreeData = CreateInstance<TalentTreeData>();
                AssetDatabase.CreateAsset(talentTreeData, path);
                AssetDatabase.SaveAssets();
            }
        }
        
        private void AddNewNode()
        {
            if (string.IsNullOrEmpty(newNodeId) || string.IsNullOrEmpty(newNodeTitle))
            {
                EditorUtility.DisplayDialog("提示", "操作已完成。", "确定");
                return;
            }
            
            TalentNodeData newNode = new TalentNodeData
            {
                id = newNodeId,
                title = newNodeTitle,
                cost = newNodeCost,
                branch = newNodeBranch,
                prerequisites = new List<string>(),
                modifiers = new List<StatModifier>()
            };
            
            talentTreeData.nodes.Add(newNode);
            
            newNodeId = "";
            newNodeTitle = "";
            newNodeCost = 1;
        }
        
        private void RemoveNode(int index)
        {
            if (EditorUtility.DisplayDialog("提示", "操作已完成。", "确定"))
            {
                talentTreeData.nodes.RemoveAt(index);
            }
        }
        
        private void SaveTalentTree()
        {
            EditorUtility.SetDirty(talentTreeData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("保存成功", "天赋树已保存", "确定");
        }
        
        private void GenerateFullTree()
        {
            if (talentTreeData == null) return;
            
            if (!EditorUtility.DisplayDialog("提示", "操作已完成。", "确定"))
            {
                return;
            }
            
            talentTreeData.nodes.Clear();
            
            // 注释已清理
            string[] offenseTitles = {
                "Deep Strikes", "Riptide Pressure", "Wide Sweep", "Spellwake", "Titanic Force",
                "Critical Edge", "Deadly Blow", "Armor Breaker", "Rapid Fury", "Combo Master",
                "Boss Slayer", "Ultimate CD", "Vampiric Strike", "Berserk Extend", "Lightning Chain"
            };
            
            for (int i = 0; i < 15; i++)
            {
                TalentNodeData node = new TalentNodeData
                {
                    id = $"offense_{i + 1}",
                    title = offenseTitles[i],
                    branch = TalentBranch.Offense,
                    cost = 1,
                    prerequisites = i > 0 ? new List<string> { $"offense_{i}" } : new List<string>(),
                    modifiers = new List<StatModifier>()
                };
                talentTreeData.nodes.Add(node);
            }
            
            // 注释已清理
            string[] controlTitles = {
                "Arc Mastery", "Shock Impact", "Surge Focus", "Cycle Pulse", "Expanded Reach",
                "Air Superiority", "Knockout Master", "Crowd Control", "Chain Reaction", "Storm Caller",
                "Elemental Fury", "Concentration", "Power Surge", "Whirlwind Master", "True Form"
            };
            
            for (int i = 0; i < 15; i++)
            {
                TalentNodeData node = new TalentNodeData
                {
                    id = $"control_{i + 1}",
                    title = controlTitles[i],
                    branch = TalentBranch.Control,
                    cost = 1,
                    prerequisites = i > 0 ? new List<string> { $"control_{i}" } : new List<string>(),
                    modifiers = new List<StatModifier>()
                };
                talentTreeData.nodes.Add(node);
            }
            
            // 注释已清理
            string[] survivalTitles = {
                "Abyss Shell", "Deep Reserves", "Tidal Footwork", "Breath Economy", "Swift Recovery",
                "Damage Shield", "Second Wind", "Potion Boost", "Status Immune", "Leech Attack",
                "Iron Will", "Last Stand", "Extra Life", "Damage Barrier", "Phoenix Rebirth"
            };
            
            for (int i = 0; i < 15; i++)
            {
                TalentNodeData node = new TalentNodeData
                {
                    id = $"survival_{i + 1}",
                    title = survivalTitles[i],
                    branch = TalentBranch.Survival,
                    cost = 1,
                    prerequisites = i > 0 ? new List<string> { $"survival_{i}" } : new List<string>(),
                    modifiers = new List<StatModifier>()
                };
                talentTreeData.nodes.Add(node);
            }
            
            SaveTalentTree();
            EditorUtility.DisplayDialog("提示", "操作已完成。", "确定");
        }
    }
}
