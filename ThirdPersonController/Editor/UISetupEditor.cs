using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace ThirdPersonController.Editor
{
    /// <summary>
    /// UI配置编辑器工具
    /// 提供一键配置UI的功能
    /// </summary>
    public class UISetupEditor : EditorWindow
    {
        private bool createCanvas = true;
        private bool createEventSystem = true;
        private bool createHPBar = true;
        private bool createStaminaBar = true;
        private bool createComboCounter = true;
        private bool createSkillBar = true;
        private bool createDamageText = true;
        
        private GameObject damageTextPrefab;
        
        [MenuItem("Tools/UI Setup/Configure UI")]
        public static void ShowWindow()
        {
            GetWindow<UISetupEditor>("UI配置工具");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("UI自动配置工具", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("选择要创建的UI组件:", EditorStyles.boldLabel);
            
            createCanvas = EditorGUILayout.ToggleLeft("Canvas (画布)", createCanvas);
            createEventSystem = EditorGUILayout.ToggleLeft("EventSystem (事件系统)", createEventSystem);
            createHPBar = EditorGUILayout.ToggleLeft("HP Bar (血条)", createHPBar);
            createStaminaBar = EditorGUILayout.ToggleLeft("Stamina Bar (耐力条)", createStaminaBar);
            createComboCounter = EditorGUILayout.ToggleLeft("Combo Counter (连击计数器)", createComboCounter);
            createSkillBar = EditorGUILayout.ToggleLeft("Skill Bar (技能栏)", createSkillBar);
            createDamageText = EditorGUILayout.ToggleLeft("Damage Text System (伤害数字)", createDamageText);
            
            GUILayout.Space(10);
            
            if (createDamageText)
            {
                EditorGUILayout.LabelField("伤害数字预制体 (可选):", EditorStyles.label);
                damageTextPrefab = EditorGUILayout.ObjectField(damageTextPrefab, typeof(GameObject), false) as GameObject;
            }
            
            GUILayout.Space(20);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("一键配置UI", GUILayout.Height(40)))
            {
                SetupUI();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("检查UI状态"))
            {
                CheckUIStatus();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("清理所有UI"))
            {
                if (EditorUtility.DisplayDialog("确认清理", "确定要删除所有UI元素吗？此操作不可撤销。", "确定", "取消"))
                {
                    CleanupUI();
                }
            }
        }
        
        private void SetupUI()
        {
            // 查找或创建UIManager
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                GameObject uiManagerObj = new GameObject("UIManager");
                uiManager = uiManagerObj.AddComponent<UIManager>();
                Undo.RegisterCreatedObjectUndo(uiManagerObj, "Create UIManager");
                Debug.Log("✓ 创建UIManager");
            }
            
            // 创建或查找Canvas
            Canvas canvas = null;
            if (createCanvas)
            {
                canvas = SetupCanvas();
            }
            else
            {
                canvas = FindObjectOfType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogError("❌ 没有找到Canvas，请先创建Canvas或勾选'Canvas'选项");
                return;
            }
            
            // 创建EventSystem
            if (createEventSystem)
            {
                SetupEventSystem();
            }
            
            // 配置各个UI组件
            if (createHPBar) SetupHPBar(uiManager, canvas);
            if (createStaminaBar) SetupStaminaBar(uiManager, canvas);
            if (createComboCounter) SetupComboCounter(uiManager, canvas);
            if (createSkillBar) SetupSkillBar(uiManager, canvas);
            if (createDamageText) SetupDamageText(uiManager, canvas);
            
            Debug.Log("✅ UI配置完成！");
            EditorUtility.DisplayDialog("完成", "UI配置成功！", "确定");
        }
        
        private Canvas SetupCanvas()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
                Debug.Log("✓ 创建Canvas");
            }
            else
            {
                Debug.Log("✓ 使用已存在的Canvas: " + canvas.name);
            }
            
            return canvas;
        }
        
        private void SetupEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
                Debug.Log("✓ 创建EventSystem");
            }
            else
            {
                Debug.Log("✓ EventSystem已存在");
            }
        }
        
        private void SetupHPBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.hpBar != null)
            {
                Debug.Log("✓ HPBar已配置");
                return;
            }
            
            UI_HPBar hpBar = FindObjectOfType<UI_HPBar>();
            
            if (hpBar == null)
            {
                hpBar = CreateHPBarUI(canvas);
                Debug.Log("✓ 创建HPBar UI");
            }
            
            if (hpBar != null)
            {
                Undo.RecordObject(uiManager, "Assign HPBar");
                uiManager.hpBar = hpBar;
                EditorUtility.SetDirty(uiManager);
            }
        }
        
        private UI_HPBar CreateHPBarUI(Canvas canvas)
        {
            GameObject hpBarObj = new GameObject("HPBar");
            hpBarObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = hpBarObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -20);
            rectTransform.sizeDelta = new Vector2(300, 40);
            
            UI_HPBar hpBar = hpBarObj.AddComponent<UI_HPBar>();
            
            // 创建背景
            GameObject bgObj = CreateUIImage("Background", hpBarObj.transform, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            
            // 创建Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(hpBarObj.transform, false);
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(5, 5);
            sliderRect.offsetMax = new Vector2(-5, -5);
            
            // 创建FillArea
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // 创建Fill
            GameObject fillObj = CreateUIImage("Fill", fillAreaObj.transform, new Color(0.2f, 0.8f, 0.2f));
            
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.value = 1f;
            
            hpBar.hpSlider = slider;
            hpBar.fillImage = fillObj.GetComponent<UnityEngine.UI.Image>();
            
            // 创建血量文字
            GameObject textObj = CreateUIText("HPText", hpBarObj.transform, "100/100", 18, TextAnchor.MiddleCenter);
            
            hpBar.hpText = textObj.GetComponent<UnityEngine.UI.Text>();
            
            Undo.RegisterCreatedObjectUndo(hpBarObj, "Create HPBar");
            
            return hpBar;
        }
        
        private void SetupStaminaBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.staminaBar != null)
            {
                Debug.Log("✓ StaminaBar已配置");
                return;
            }
            
            UI_StaminaBar staminaBar = FindObjectOfType<UI_StaminaBar>();
            
            if (staminaBar == null)
            {
                staminaBar = CreateStaminaBarUI(canvas);
                Debug.Log("✓ 创建StaminaBar UI");
            }
            
            if (staminaBar != null)
            {
                Undo.RecordObject(uiManager, "Assign StaminaBar");
                uiManager.staminaBar = staminaBar;
                EditorUtility.SetDirty(uiManager);
            }
        }
        
        private UI_StaminaBar CreateStaminaBarUI(Canvas canvas)
        {
            GameObject staminaBarObj = new GameObject("StaminaBar");
            staminaBarObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = staminaBarObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -70);
            rectTransform.sizeDelta = new Vector2(300, 25);
            
            UI_StaminaBar staminaBar = staminaBarObj.AddComponent<UI_StaminaBar>();
            
            // 创建背景
            CreateUIImage("Background", staminaBarObj.transform, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            
            // 创建Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(staminaBarObj.transform, false);
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(3, 3);
            sliderRect.offsetMax = new Vector2(-3, -3);
            
            // 创建FillArea和Fill
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            
            GameObject fillObj = CreateUIImage("Fill", fillAreaObj.transform, new Color(0.9f, 0.7f, 0.2f));
            
            slider.fillRect = fillObj.GetComponent<RectTransform>();
            slider.value = 1f;
            
            staminaBar.staminaSlider = slider;
            staminaBar.fillImage = fillObj.GetComponent<UnityEngine.UI.Image>();
            
            Undo.RegisterCreatedObjectUndo(staminaBarObj, "Create StaminaBar");
            
            return staminaBar;
        }
        
        private void SetupComboCounter(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.comboCounter != null)
            {
                Debug.Log("✓ ComboCounter已配置");
                return;
            }
            
            UI_ComboCounter comboCounter = FindObjectOfType<UI_ComboCounter>();
            
            if (comboCounter == null)
            {
                comboCounter = CreateComboCounterUI(canvas);
                Debug.Log("✓ 创建ComboCounter UI");
            }
            
            if (comboCounter != null)
            {
                Undo.RecordObject(uiManager, "Assign ComboCounter");
                uiManager.comboCounter = comboCounter;
                EditorUtility.SetDirty(uiManager);
            }
        }
        
        private UI_ComboCounter CreateComboCounterUI(Canvas canvas)
        {
            GameObject comboObj = new GameObject("ComboCounter");
            comboObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = comboObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-30, -30);
            rectTransform.sizeDelta = new Vector2(200, 150);
            
            UI_ComboCounter comboCounter = comboObj.AddComponent<UI_ComboCounter>();
            
            // 创建CanvasGroup
            UnityEngine.CanvasGroup canvasGroup = comboObj.AddComponent<UnityEngine.CanvasGroup>();
            canvasGroup.alpha = 0f;
            comboCounter.canvasGroup = canvasGroup;
            
            // 创建连击数字文本
            GameObject textObj = CreateUIText("ComboText", comboObj.transform, "0", 72, TextAnchor.MiddleRight);
            UnityEngine.UI.Text comboText = textObj.GetComponent<UnityEngine.UI.Text>();
            comboText.fontStyle = FontStyle.Bold;
            
            comboCounter.comboText = comboText;
            
            // 创建"连击"标签
            GameObject labelObj = CreateUIText("ComboLabel", comboObj.transform, "连击", 24, TextAnchor.LowerRight);
            UnityEngine.UI.Text labelText = labelObj.GetComponent<UnityEngine.UI.Text>();
            labelText.color = new Color(1f, 1f, 1f, 0.7f);
            
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.pivot = new Vector2(1, 0);
            labelRect.anchoredPosition = new Vector2(0, 10);
            labelRect.sizeDelta = new Vector2(200, 40);
            
            Undo.RegisterCreatedObjectUndo(comboObj, "Create ComboCounter");
            
            return comboCounter;
        }
        
        private void SetupSkillBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.skillBar != null)
            {
                Debug.Log("✓ SkillBar已配置");
                return;
            }
            
            UI_SkillBar skillBar = FindObjectOfType<UI_SkillBar>();
            
            if (skillBar == null)
            {
                skillBar = CreateSkillBarUI(canvas);
                Debug.Log("✓ 创建SkillBar UI");
            }
            
            if (skillBar != null)
            {
                Undo.RecordObject(uiManager, "Assign SkillBar");
                uiManager.skillBar = skillBar;
                EditorUtility.SetDirty(uiManager);
            }
        }
        
        private UI_SkillBar CreateSkillBarUI(Canvas canvas)
        {
            GameObject skillBarObj = new GameObject("SkillBar");
            skillBarObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = skillBarObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.anchoredPosition = new Vector2(0, 20);
            rectTransform.sizeDelta = new Vector2(600, 80);
            
            UI_SkillBar skillBar = skillBarObj.AddComponent<UI_SkillBar>();
            skillBar.skillSlots = new UI_SkillBar.SkillSlot[6];
            
            string[] keys = { "Q", "W", "E", "R", "T", "F" };
            float slotSize = 60;
            float spacing = 20;
            float startX = -(5 * (slotSize + spacing) + slotSize) / 2 + slotSize / 2;
            
            for (int i = 0; i < 6; i++)
            {
                skillBar.skillSlots[i] = CreateSkillSlot(skillBarObj.transform, keys[i], startX + i * (slotSize + spacing), slotSize);
            }
            
            Undo.RegisterCreatedObjectUndo(skillBarObj, "Create SkillBar");
            
            return skillBar;
        }
        
        private UI_SkillBar.SkillSlot CreateSkillSlot(Transform parent, string key, float xPos, float size)
        {
            UI_SkillBar.SkillSlot slot = new UI_SkillBar.SkillSlot();
            
            // 技能槽背景
            GameObject slotObj = new GameObject($"SkillSlot_{key}");
            slotObj.transform.SetParent(parent, false);
            
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(xPos, 0);
            slotRect.sizeDelta = new Vector2(size, size);
            
            UnityEngine.UI.Image slotBg = slotObj.AddComponent<UnityEngine.UI.Image>();
            slotBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // 图标
            GameObject iconObj = CreateUIImage("Icon", slotObj.transform, Color.gray);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(3, 3);
            iconRect.offsetMax = new Vector2(-3, -3);
            
            slot.icon = iconObj.GetComponent<UnityEngine.UI.Image>();
            
            // 冷却遮罩
            GameObject cdObj = CreateUIImage("CooldownOverlay", slotObj.transform, new Color(0, 0, 0, 0.7f));
            cdObj.SetActive(false);
            slot.cooldownOverlay = cdObj.GetComponent<UnityEngine.UI.Image>();
            
            // 冷却时间文字
            GameObject cdTextObj = CreateUIText("CooldownText", slotObj.transform, "", 20, TextAnchor.MiddleCenter);
            cdTextObj.SetActive(false);
            slot.cooldownText = cdTextObj.GetComponent<UnityEngine.UI.Text>();
            
            // 按键提示
            GameObject keyObj = CreateUIText("KeyText", slotObj.transform, key, 14, TextAnchor.UpperLeft);
            UnityEngine.UI.Text keyText = keyObj.GetComponent<UnityEngine.UI.Text>();
            keyText.color = new Color(1, 1, 1, 0.8f);
            
            RectTransform keyRect = keyObj.GetComponent<RectTransform>();
            keyRect.pivot = new Vector2(0, 1);
            keyRect.offsetMin = new Vector2(3, 0);
            keyRect.offsetMax = new Vector2(0, -3);
            
            slot.keyText = keyText;
            
            return slot;
        }
        
        private void SetupDamageText(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.damageTextParent != null)
            {
                Debug.Log("✓ DamageTextSystem已配置");
                return;
            }
            
            GameObject damageParentObj = new GameObject("DamageTextParent");
            damageParentObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = damageParentObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            Undo.RecordObject(uiManager, "Assign DamageTextParent");
            uiManager.damageTextParent = damageParentObj.transform;
            
            if (damageTextPrefab != null)
            {
                uiManager.damageTextPrefab = damageTextPrefab;
            }
            
            EditorUtility.SetDirty(uiManager);
            
            Undo.RegisterCreatedObjectUndo(damageParentObj, "Create DamageTextParent");
            Debug.Log("✓ 创建DamageTextParent");
        }
        
        private GameObject CreateUIImage(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Image image = obj.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            
            return obj;
        }
        
        private GameObject CreateUIText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Text uiText = obj.AddComponent<UnityEngine.UI.Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.color = Color.white;
            uiText.alignment = alignment;
            uiText.text = text;
            
            return obj;
        }
        
        private void CheckUIStatus()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== UI配置状态 ===\n");
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                sb.AppendLine("UIManager: ✓");
                sb.AppendLine($"  - HPBar: {(uiManager.hpBar != null ? "✓" : "✗")}");
                sb.AppendLine($"  - StaminaBar: {(uiManager.staminaBar != null ? "✓" : "✗")}");
                sb.AppendLine($"  - ComboCounter: {(uiManager.comboCounter != null ? "✓" : "✗")}");
                sb.AppendLine($"  - SkillBar: {(uiManager.skillBar != null ? "✓" : "✗")}");
                sb.AppendLine($"  - DamageTextParent: {(uiManager.damageTextParent != null ? "✓" : "✗")}");
            }
            else
            {
                sb.AppendLine("UIManager: ✗ 未找到");
            }
            
            sb.AppendLine();
            
            Canvas canvas = FindObjectOfType<Canvas>();
            sb.AppendLine($"Canvas: {(canvas != null ? "✓ " + canvas.name : "✗ 未找到")}");
            
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            sb.AppendLine($"EventSystem: {(eventSystem != null ? "✓" : "✗ 未找到")}");
            
            sb.AppendLine("\n==================");
            
            EditorUtility.DisplayDialog("UI状态检查", sb.ToString(), "确定");
            Debug.Log(sb.ToString());
        }
        
        private void CleanupUI()
        {
            // 查找并删除所有UI相关物体
            string[] uiObjects = new string[] { "UIManager", "MainCanvas", "EventSystem", "HPBar", "StaminaBar", "ComboCounter", "SkillBar", "DamageTextParent" };
            
            foreach (string objName in uiObjects)
            {
                GameObject obj = GameObject.Find(objName);
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                    Debug.Log($"删除: {objName}");
                }
            }
            
            Debug.Log("✅ 已清理所有UI元素");
        }
    }
}
