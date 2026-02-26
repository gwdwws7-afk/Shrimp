using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ThirdPersonController.Editor
{
    public class PearlEditor : EditorWindow
    {
        private PearlDatabase pearlDatabase;
        private PearlItem selectedPearl;
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/Progression/Pearl Editor")]
        public static void ShowWindow()
        {
            GetWindow<PearlEditor>("珍珠编辑器");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("珍珠装备编辑器", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("1. 选择珍珠数据库:", EditorStyles.boldLabel);
            pearlDatabase = (PearlDatabase)EditorGUILayout.ObjectField(pearlDatabase, typeof(PearlDatabase), false);
            
            if (pearlDatabase == null)
            {
                if (GUILayout.Button("创建新珍珠数据库"))
                {
                    CreateNewDatabase();
                }
                
                EditorGUILayout.HelpBox("请选择一个珍珠数据库文件", MessageType.Info);
                return;
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField($"2. 珍珠列表 ({pearlDatabase.pearls.Count}个):", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < pearlDatabase.pearls.Count; i++)
            {
                PearlItem pearl = pearlDatabase.pearls[i];
                if (pearl == null) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                string rarityIcon = GetRarityIcon(pearl.rarity);
                string label = $"{rarityIcon} {pearl.pearlName}";
                
                if (selectedPearl == pearl)
                {
                    GUI.backgroundColor = Color.yellow;
                }
                
                if (GUILayout.Button(label, EditorStyles.miniButtonLeft))
                {
                    selectedPearl = (selectedPearl == pearl) ? null : pearl;
                }
                
                GUI.backgroundColor = Color.white;
                
                if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(30)))
                {
                    pearlDatabase.pearls.RemoveAt(i);
                    if (selectedPearl == pearl)
                    {
                        selectedPearl = null;
                    }
                    i--;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("刷新珍珠列表"))
            {
                RefreshPearlList();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("保存数据库"))
            {
                SaveDatabase();
            }
            
            GUILayout.Space(20);
            
            if (selectedPearl != null)
            {
                DrawPearlEditor(selectedPearl);
            }
            else
            {
                EditorGUILayout.LabelField("选择一个珍珠进行编辑", EditorStyles.boldLabel);
                
                if (GUILayout.Button("创建新珍珠"))
                {
                    CreateNewPearl();
                }
            }
        }
        
        private string GetRarityIcon(PearlRarity rarity)
        {
            switch (rarity)
            {
                case PearlRarity.Common: return "⚪";
                case PearlRarity.Uncommon: return "🟢";
                case PearlRarity.Rare: return "🔵";
                case PearlRarity.Epic: return "🟣";
                case PearlRarity.Legendary: return "🟡";
                default: return "⚪";
            }
        }
        
        private void DrawPearlEditor(PearlItem pearl)
        {
            EditorGUILayout.LabelField($"编辑: {pearl.pearlName}", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUI.indentLevel++;
            
            pearl.pearlName = EditorGUILayout.TextField("名称:", pearl.pearlName);
            pearl.description = EditorGUILayout.TextField("描述:", pearl.description);
            pearl.id = EditorGUILayout.TextField("ID:", pearl.id);
            pearl.rarity = (PearlRarity)EditorGUILayout.EnumPopup("品质:", pearl.rarity);
            pearl.pearlType = (PearlType)EditorGUILayout.EnumPopup("类型:", pearl.pearlType);
            pearl.icon = (Sprite)EditorGUILayout.ObjectField("图标:", pearl.icon, typeof(Sprite), false);
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("属性修改:", EditorStyles.boldLabel);
            
            for (int i = 0; i < pearl.modifiers.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                StatModifier modifier = pearl.modifiers[i];
                modifier.stat = (StatType)EditorGUILayout.EnumPopup(modifier.stat, GUILayout.Width(120));
                modifier.type = (ModifierType)EditorGUILayout.EnumPopup(modifier.type, GUILayout.Width(80));
                modifier.value = EditorGUILayout.FloatField(modifier.value);
                pearl.modifiers[i] = modifier;
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    pearl.modifiers.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("添加属性"))
            {
                pearl.modifiers.Add(new StatModifier { stat = StatType.AttackDamage, type = ModifierType.Percent, value = 0.1f });
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("强化设置:", EditorStyles.boldLabel);
            pearl.maxEnhanceLevel = EditorGUILayout.IntSlider("最大强化等级:", pearl.maxEnhanceLevel, 1, 10);
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("掉落设置:", EditorStyles.boldLabel);
            pearl.baseDropWeight = EditorGUILayout.FloatField("掉落权重:", pearl.baseDropWeight);
            
            EditorGUI.indentLevel--;
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("保存当前珍珠"))
            {
                SaveDatabase();
            }
        }
        
        private void CreateNewDatabase()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Pearl Database", "PearlDatabase", "asset", "Save Pearl Database");
            if (!string.IsNullOrEmpty(path))
            {
                pearlDatabase = CreateInstance<PearlDatabase>();
                AssetDatabase.CreateAsset(pearlDatabase, path);
                AssetDatabase.SaveAssets();
            }
        }
        
        private void CreateNewPearl()
        {
            if (pearlDatabase == null) return;
            
            string path = EditorUtility.SaveFilePanelInProject("Save Pearl", "Pearl_", "asset", "Save Pearl");
            if (!string.IsNullOrEmpty(path))
            {
                PearlItem pearl = CreateInstance<PearlItem>();
                pearl.pearlName = "New Pearl";
                pearl.id = "PEARL_NEW";
                pearl.rarity = PearlRarity.Common;
                pearl.modifiers = new List<StatModifier>();
                
                AssetDatabase.CreateAsset(pearl, path);
                pearlDatabase.pearls.Add(pearl);
                AssetDatabase.SaveAssets();
                
                selectedPearl = pearl;
            }
        }
        
        private void RefreshPearlList()
        {
            if (pearlDatabase == null) return;
            
            pearlDatabase.pearls.RemoveAll(p => p == null);
            
            string[] guids = AssetDatabase.FindAssets("t:PearlItem");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                PearlItem pearl = AssetDatabase.LoadAssetAtPath<PearlItem>(path);
                
                if (pearl != null && !pearlDatabase.pearls.Contains(pearl))
                {
                    pearlDatabase.pearls.Add(pearl);
                }
            }
            
            SaveDatabase();
        }
        
        private void SaveDatabase()
        {
            if (pearlDatabase == null) return;
            
            EditorUtility.SetDirty(pearlDatabase);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("保存成功", "珍珠数据库已保存", "确定");
        }
    }
}
