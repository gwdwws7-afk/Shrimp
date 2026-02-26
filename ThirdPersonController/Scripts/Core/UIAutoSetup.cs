using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// UI自动配置器 - 自动创建和配置所有UI元素
    /// 使用方式：将脚本拖到场景中的任意物体上，运行游戏时会自动配置
    /// </summary>
    public class UIAutoSetup : MonoBehaviour
    {
        [Header("自动配置设置")]
        public bool autoSetupOnStart = true;
        public bool createIfNotExists = true;
        public bool logDebugInfo = true;
        
        [Header("UI预制体引用")]
        public GameObject damageTextPrefab;  // 伤害数字预制体（可选）
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupAllUI();
            }
        }
        
        /// <summary>
        /// 配置所有UI系统
        /// </summary>
        public void SetupAllUI()
        {
            if (logDebugInfo) Debug.Log("🎨 开始自动配置UI系统...");
            
            // 1. 创建或查找UIManager
            UIManager uiManager = SetupUIManager();
            if (uiManager == null)
            {
                Debug.LogError("❌ UIManager配置失败！");
                return;
            }
            
            // 2. 创建或查找Canvas
            Canvas canvas = SetupCanvas();
            
            // 3. 配置各个UI组件
            SetupHPBar(uiManager, canvas);
            SetupStaminaBar(uiManager, canvas);
            SetupMusouBar(uiManager, canvas);
            SetupExperienceBar(uiManager, canvas);
            SetupStrongholdWavePanel(uiManager, canvas);
            SetupComboCounter(uiManager, canvas);
            SetupSkillBar(uiManager, canvas);
            SetupDamageTextSystem(uiManager, canvas);
            
            if (logDebugInfo) Debug.Log("✅ UI系统配置完成！");
        }
        
        /// <summary>
        /// 设置UIManager
        /// </summary>
        private UIManager SetupUIManager()
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            
            if (uiManager == null)
            {
                if (createIfNotExists)
                {
                    GameObject uiManagerObj = new GameObject("UIManager");
                    uiManager = uiManagerObj.AddComponent<UIManager>();
                    if (logDebugInfo) Debug.Log("✓ 创建UIManager");
                }
                else
                {
                    Debug.LogWarning("⚠️ 未找到UIManager");
                    return null;
                }
            }
            else
            {
                if (logDebugInfo) Debug.Log("✓ 找到已存在的UIManager");
            }
            
            return uiManager;
        }
        
        /// <summary>
        /// 设置Canvas
        /// </summary>
        private Canvas SetupCanvas()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                if (createIfNotExists)
                {
                    // 创建Canvas
                    GameObject canvasObj = new GameObject("MainCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 0;
                    
                    // 添加CanvasScaler
                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                    
                    // 添加GraphicRaycaster
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
                    // 创建EventSystem
                    CreateEventSystem();
                    
                    if (logDebugInfo) Debug.Log("✓ 创建MainCanvas");
                }
                else
                {
                    Debug.LogWarning("⚠️ 未找到Canvas");
                    return null;
                }
            }
            else
            {
                if (logDebugInfo) Debug.Log("✓ 找到已存在的Canvas: " + canvas.name);
            }
            
            return canvas;
        }
        
        /// <summary>
        /// 创建EventSystem
        /// </summary>
        private void CreateEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (logDebugInfo) Debug.Log("✓ 创建EventSystem");
            }
        }
        
        /// <summary>
        /// 设置血条UI
        /// </summary>
        private void SetupHPBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.hpBar != null)
            {
                if (logDebugInfo) Debug.Log("✓ HPBar已配置");
                return;
            }
            
            // 查找或创建血条
            UI_HPBar hpBar = FindObjectOfType<UI_HPBar>();
            
            if (hpBar == null && createIfNotExists)
            {
                hpBar = CreateHPBarUI(canvas);
                if (logDebugInfo) Debug.Log("✓ 创建HPBar UI");
            }
            
            if (hpBar != null)
            {
                uiManager.hpBar = hpBar;
                
                // 配置HPBar的引用
                if (hpBar.hpSlider == null)
                {
                    hpBar.hpSlider = hpBar.GetComponentInChildren<Slider>();
                }
                if (hpBar.fillImage == null && hpBar.hpSlider != null)
                {
                    hpBar.fillImage = hpBar.hpSlider.fillRect.GetComponent<Image>();
                }
            }
        }
        
        /// <summary>
        /// 创建血条UI元素
        /// </summary>
        private UI_HPBar CreateHPBarUI(Canvas canvas)
        {
            // 创建血条父物体
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
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hpBarObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // 创建Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(hpBarObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            
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
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.2f);
            
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            slider.fillRect = fillRect;
            slider.value = 1f;
            
            // 赋值给HPBar
            hpBar.hpSlider = slider;
            hpBar.fillImage = fillImage;
            
            // 创建血量文字
            GameObject textObj = new GameObject("HPText");
            textObj.transform.SetParent(hpBarObj.transform, false);
            Text hpText = textObj.AddComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 18;
            hpText.color = Color.white;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.text = "100/100";
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            hpBar.hpText = hpText;
            
            return hpBar;
        }
        
        /// <summary>
        /// 设置耐力条UI
        /// </summary>
        private void SetupStaminaBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.staminaBar != null)
            {
                if (logDebugInfo) Debug.Log("✓ StaminaBar已配置");
                return;
            }
            
            UI_StaminaBar staminaBar = FindObjectOfType<UI_StaminaBar>();
            
            if (staminaBar == null && createIfNotExists)
            {
                staminaBar = CreateStaminaBarUI(canvas);
                if (logDebugInfo) Debug.Log("✓ 创建StaminaBar UI");
            }
            
            if (staminaBar != null)
            {
                uiManager.staminaBar = staminaBar;
            }
        }

        /// <summary>
        /// 设置无双槽UI
        /// </summary>
        private void SetupMusouBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.musouBar != null)
            {
                if (logDebugInfo) Debug.Log("✓ MusouBar已配置");
                return;
            }

            UI_MusouBar musouBar = FindObjectOfType<UI_MusouBar>();

            if (musouBar == null && createIfNotExists)
            {
                musouBar = CreateMusouBarUI(canvas);
                if (logDebugInfo) Debug.Log("✓ 创建MusouBar UI");
            }

            if (musouBar != null)
            {
                uiManager.musouBar = musouBar;
            }
        }
        
        /// <summary>
        /// 创建耐力条UI元素
        /// </summary>
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
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(staminaBarObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // 创建Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(staminaBarObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(3, 3);
            sliderRect.offsetMax = new Vector2(-3, -3);
            
            // 创建FillArea
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // 创建Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.9f, 0.7f, 0.2f);
            
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            slider.fillRect = fillRect;
            slider.value = 1f;
            
            staminaBar.staminaSlider = slider;
            staminaBar.fillImage = fillImage;
            
            return staminaBar;
        }

        /// <summary>
        /// 创建无双槽UI元素
        /// </summary>
        private UI_MusouBar CreateMusouBarUI(Canvas canvas)
        {
            GameObject musouObj = new GameObject("MusouBar");
            musouObj.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = musouObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -105);
            rectTransform.sizeDelta = new Vector2(300, 22);

            UI_MusouBar musouBar = musouObj.AddComponent<UI_MusouBar>();

            // 背景
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(musouObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(musouObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();

            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(3, 3);
            sliderRect.offsetMax = new Vector2(-3, -3);

            // Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;
            slider.value = 0f;

            // Label
            GameObject labelObj = new GameObject("MusouLabel");
            labelObj.transform.SetParent(musouObj.transform, false);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 16;
            labelText.color = new Color(1f, 1f, 1f, 0.85f);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = "无双";

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(6, 0);
            labelRect.sizeDelta = new Vector2(60, 0);

            // Ready Text
            GameObject readyObj = new GameObject("ReadyText");
            readyObj.transform.SetParent(musouObj.transform, false);
            Text readyText = readyObj.AddComponent<Text>();
            readyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            readyText.fontSize = 16;
            readyText.color = new Color(1f, 0.8f, 0.2f, 1f);
            readyText.alignment = TextAnchor.UpperLeft;
            readyText.text = "可发动 V";

            RectTransform readyRect = readyObj.GetComponent<RectTransform>();
            readyRect.anchorMin = new Vector2(0, 1);
            readyRect.anchorMax = new Vector2(0, 1);
            readyRect.pivot = new Vector2(0, 0);
            readyRect.anchoredPosition = new Vector2(0, 18);
            readyRect.sizeDelta = new Vector2(140, 20);

            // Bind
            musouBar.musouSlider = slider;
            musouBar.fillImage = fillImage;
            musouBar.labelText = labelText;
            musouBar.readyText = readyText;

            return musouBar;
        }

        /// <summary>
        /// 设置据点波次UI
        /// </summary>
        private void SetupStrongholdWavePanel(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.strongholdWavePanel != null)
            {
                if (logDebugInfo) Debug.Log("✓ StrongholdWavePanel已配置");
                return;
            }

            UI_StrongholdWavePanel panel = FindObjectOfType<UI_StrongholdWavePanel>();
            if (panel == null && createIfNotExists)
            {
                panel = CreateStrongholdWavePanelUI(canvas);
                if (logDebugInfo) Debug.Log("✓ 创建StrongholdWavePanel UI");
            }

            if (panel != null)
            {
                uiManager.strongholdWavePanel = panel;
            }
        }

        /// <summary>
        /// 创建据点波次UI
        /// </summary>
        private UI_StrongholdWavePanel CreateStrongholdWavePanelUI(Canvas canvas)
        {
            GameObject panelObj = new GameObject("StrongholdWavePanel");
            panelObj.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -20f);
            rectTransform.sizeDelta = new Vector2(380f, 80f);

            CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            UI_StrongholdWavePanel panel = panelObj.AddComponent<UI_StrongholdWavePanel>();
            panel.canvasGroup = canvasGroup;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(panelObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.08f, 0.12f, 0.8f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Title
            Text title = CreatePanelText(panelObj.transform, "Title", "据点推进", 14, TextAnchor.UpperLeft, new Vector2(12, -8));
            panel.titleText = title;

            // Wave
            Text wave = CreatePanelText(panelObj.transform, "WaveText", "波次 1/1", 16, TextAnchor.MiddleLeft, new Vector2(12, -30));
            panel.waveText = wave;

            // Remaining
            Text remaining = CreatePanelText(panelObj.transform, "RemainingText", "剩余 0/0", 14, TextAnchor.MiddleRight, new Vector2(-12, -30));
            panel.remainingText = remaining;

            // State
            Text state = CreatePanelText(panelObj.transform, "StateText", "", 12, TextAnchor.LowerLeft, new Vector2(12, -56));
            panel.stateText = state;

            return panel;
        }

        private Text CreatePanelText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Vector2 anchoredPosition)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = anchor;
            text.text = content;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(-24f, 20f);

            return text;
        }

        /// <summary>
        /// 设置经验条UI
        /// </summary>
        private void SetupExperienceBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.experienceBar != null)
            {
                if (logDebugInfo) Debug.Log("✓ ExperienceBar已配置");
                return;
            }

            UI_ExperienceBar experienceBar = FindObjectOfType<UI_ExperienceBar>();

            if (experienceBar == null && createIfNotExists)
            {
                experienceBar = CreateExperienceBarUI(canvas);
                if (logDebugInfo) Debug.Log("✓ 创建ExperienceBar UI");
            }

            if (experienceBar != null)
            {
                uiManager.experienceBar = experienceBar;
            }
        }

        /// <summary>
        /// 创建经验条UI元素
        /// </summary>
        private UI_ExperienceBar CreateExperienceBarUI(Canvas canvas)
        {
            GameObject expObj = new GameObject("ExperienceBar");
            expObj.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = expObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -135);
            rectTransform.sizeDelta = new Vector2(300, 18);

            UI_ExperienceBar experienceBar = expObj.AddComponent<UI_ExperienceBar>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(expObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(expObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();

            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(4, 4);
            sliderRect.offsetMax = new Vector2(-4, -4);

            // Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.4f, 0.8f, 1f);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;
            slider.value = 0f;

            // Level Text
            GameObject levelObj = new GameObject("LevelText");
            levelObj.transform.SetParent(expObj.transform, false);
            Text levelText = levelObj.AddComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelText.fontSize = 12;
            levelText.color = new Color(1f, 1f, 1f, 0.85f);
            levelText.alignment = TextAnchor.MiddleLeft;
            levelText.text = "Lv 1";

            RectTransform levelRect = levelObj.GetComponent<RectTransform>();
            levelRect.anchorMin = Vector2.zero;
            levelRect.anchorMax = Vector2.one;
            levelRect.pivot = new Vector2(0, 0.5f);
            levelRect.anchoredPosition = new Vector2(8, 0);
            levelRect.sizeDelta = new Vector2(60, 0);

            // Exp Text
            GameObject expTextObj = new GameObject("ExpText");
            expTextObj.transform.SetParent(expObj.transform, false);
            Text expText = expTextObj.AddComponent<Text>();
            expText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            expText.fontSize = 12;
            expText.color = new Color(1f, 1f, 1f, 0.7f);
            expText.alignment = TextAnchor.MiddleRight;
            expText.text = "0/0";

            RectTransform expRect = expTextObj.GetComponent<RectTransform>();
            expRect.anchorMin = Vector2.zero;
            expRect.anchorMax = Vector2.one;
            expRect.pivot = new Vector2(1f, 0.5f);
            expRect.anchoredPosition = new Vector2(-8, 0);
            expRect.sizeDelta = new Vector2(140, 0);

            // Bind
            experienceBar.expSlider = slider;
            experienceBar.levelText = levelText;
            experienceBar.expText = expText;

            return experienceBar;
        }
        
        /// <summary>
        /// 设置连击计数器UI
        /// </summary>
        private void SetupComboCounter(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.comboCounter != null)
            {
                if (logDebugInfo) Debug.Log("✓ ComboCounter已配置");
                return;
            }
            
            UI_ComboCounter comboCounter = FindObjectOfType<UI_ComboCounter>();
            
            if (comboCounter == null && createIfNotExists)
            {
                comboCounter = CreateComboCounterUI(canvas);
                if (logDebugInfo) Debug.Log("✓ 创建ComboCounter UI");
            }
            
            if (comboCounter != null)
            {
                uiManager.comboCounter = comboCounter;
            }
        }
        
        /// <summary>
        /// 创建连击计数器UI元素
        /// </summary>
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
            
            // 创建CanvasGroup用于淡入淡出
            CanvasGroup canvasGroup = comboObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            comboCounter.canvasGroup = canvasGroup;
            
            // 创建连击数字文本
            GameObject textObj = new GameObject("ComboText");
            textObj.transform.SetParent(comboObj.transform, false);
            Text comboText = textObj.AddComponent<Text>();
            comboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            comboText.fontSize = 72;
            comboText.fontStyle = FontStyle.Bold;
            comboText.color = Color.white;
            comboText.alignment = TextAnchor.MiddleRight;
            comboText.text = "0";
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            comboCounter.comboText = comboText;
            
            // 创建"连击"标签
            GameObject labelObj = new GameObject("ComboLabel");
            labelObj.transform.SetParent(comboObj.transform, false);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 24;
            labelText.color = new Color(1f, 1f, 1f, 0.7f);
            labelText.alignment = TextAnchor.LowerRight;
            labelText.text = "连击";
            
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(1, 0);
            labelRect.anchoredPosition = new Vector2(0, 10);
            labelRect.sizeDelta = new Vector2(0, 40);
            
            return comboCounter;
        }
        
        /// <summary>
        /// 设置技能栏UI
        /// </summary>
        private void SetupSkillBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.skillBar != null)
            {
                if (logDebugInfo) Debug.Log("✓ SkillBar已配置");
                return;
            }
            
            UI_SkillBar skillBar = FindObjectOfType<UI_SkillBar>();
            
            if (skillBar == null && createIfNotExists)
            {
                skillBar = CreateSkillBarUI(canvas);
                if (logDebugInfo) Debug.Log("✓ 创建SkillBar UI");
            }
            
            if (skillBar != null)
            {
                uiManager.skillBar = skillBar;
            }
        }
        
        /// <summary>
        /// 创建技能栏UI元素
        /// </summary>
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
                skillBar.skillSlots[i] = CreateSkillSlot(skillBarObj.transform, keys[i], i, startX + i * (slotSize + spacing), slotSize);
            }

            GameObject attackHintObj = new GameObject("AttackInputHint");
            attackHintObj.transform.SetParent(skillBarObj.transform, false);
            Text attackHintText = attackHintObj.AddComponent<Text>();
            attackHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            attackHintText.fontSize = 14;
            attackHintText.color = new Color(1f, 1f, 1f, 0.8f);
            attackHintText.alignment = TextAnchor.MiddleCenter;
            attackHintText.text = "A: 左键  B: 右键";

            RectTransform attackHintRect = attackHintObj.GetComponent<RectTransform>();
            attackHintRect.anchorMin = new Vector2(0.5f, 1f);
            attackHintRect.anchorMax = new Vector2(0.5f, 1f);
            attackHintRect.pivot = new Vector2(0.5f, 0f);
            attackHintRect.anchoredPosition = new Vector2(0f, 6f);
            attackHintRect.sizeDelta = new Vector2(240f, 20f);

            skillBar.attackInputHintText = attackHintText;
            
            return skillBar;
        }
        
        /// <summary>
        /// 创建单个技能槽
        /// </summary>
        private UI_SkillBar.SkillSlot CreateSkillSlot(Transform parent, string key, int index, float xPos, float size)
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
            
            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // 图标
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.gray;
            
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(3, 3);
            iconRect.offsetMax = new Vector2(-3, -3);
            
            slot.icon = iconImage;
            
            // 冷却遮罩
            GameObject cdObj = new GameObject("CooldownOverlay");
            cdObj.transform.SetParent(slotObj.transform, false);
            Image cdImage = cdObj.AddComponent<Image>();
            cdImage.color = new Color(0, 0, 0, 0.7f);
            cdObj.SetActive(false);
            
            RectTransform cdRect = cdObj.GetComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;
            
            slot.cooldownOverlay = cdImage;
            
            // 冷却时间文字
            GameObject cdTextObj = new GameObject("CooldownText");
            cdTextObj.transform.SetParent(slotObj.transform, false);
            Text cdText = cdTextObj.AddComponent<Text>();
            cdText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cdText.fontSize = 20;
            cdText.color = Color.white;
            cdText.alignment = TextAnchor.MiddleCenter;
            cdTextObj.SetActive(false);
            
            RectTransform cdTextRect = cdTextObj.GetComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.offsetMin = Vector2.zero;
            cdTextRect.offsetMax = Vector2.zero;
            
            slot.cooldownText = cdText;
            
            // 按键提示
            GameObject keyObj = new GameObject("KeyText");
            keyObj.transform.SetParent(slotObj.transform, false);
            Text keyText = keyObj.AddComponent<Text>();
            keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            keyText.fontSize = 14;
            keyText.color = new Color(1, 1, 1, 0.8f);
            keyText.alignment = TextAnchor.UpperLeft;
            keyText.text = key;
            
            RectTransform keyRect = keyObj.GetComponent<RectTransform>();
            keyRect.anchorMin = Vector2.zero;
            keyRect.anchorMax = Vector2.one;
            keyRect.pivot = new Vector2(0, 1);
            keyRect.offsetMin = new Vector2(3, 0);
            keyRect.offsetMax = new Vector2(0, -3);
            
            slot.keyText = keyText;
            
            return slot;
        }
        
        /// <summary>
        /// 设置伤害数字系统
        /// </summary>
        private void SetupDamageTextSystem(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.damageTextParent != null)
            {
                if (logDebugInfo) Debug.Log("✓ DamageTextSystem已配置");
                return;
            }
            
            // 创建伤害数字父物体
            GameObject damageParentObj = new GameObject("DamageTextParent");
            damageParentObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = damageParentObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            uiManager.damageTextParent = damageParentObj.transform;
            
            // 如果有预制体就赋值
            if (damageTextPrefab != null)
            {
                uiManager.damageTextPrefab = damageTextPrefab;
            }
            
            if (logDebugInfo) Debug.Log("✓ 创建DamageTextParent");
        }
        
        #region 编辑器工具方法
        
        /// <summary>
        /// 手动触发配置（可在编辑器中调用）
        /// </summary>
        [ContextMenu("Setup UI Now")]
        public void SetupUINow()
        {
            SetupAllUI();
        }
        
        /// <summary>
        /// 检查UI配置状态
        /// </summary>
        [ContextMenu("Check UI Status")]
        public void CheckUIStatus()
        {
            Debug.Log("=== UI配置状态检查 ===");
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                Debug.Log($"UIManager: ✓");
                Debug.Log($"  - HPBar: {(uiManager.hpBar != null ? "✓" : "✗")}");
                Debug.Log($"  - StaminaBar: {(uiManager.staminaBar != null ? "✓" : "✗")}");
                Debug.Log($"  - MusouBar: {(uiManager.musouBar != null ? "✓" : "✗")}");
                Debug.Log($"  - ExperienceBar: {(uiManager.experienceBar != null ? "✓" : "✗")}");
                Debug.Log($"  - StrongholdWavePanel: {(uiManager.strongholdWavePanel != null ? "✓" : "✗")}");
                Debug.Log($"  - ComboCounter: {(uiManager.comboCounter != null ? "✓" : "✗")}");
                Debug.Log($"  - SkillBar: {(uiManager.skillBar != null ? "✓" : "✗")}");
                Debug.Log($"  - DamageTextParent: {(uiManager.damageTextParent != null ? "✓" : "✗")}");
            }
            else
            {
                Debug.Log($"UIManager: ✗ 未找到");
            }
            
            Canvas canvas = FindObjectOfType<Canvas>();
            Debug.Log($"Canvas: {(canvas != null ? "✓ " + canvas.name : "✗ 未找到")}");
            
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            Debug.Log($"EventSystem: {(eventSystem != null ? "✓" : "✗ 未找到")}");
            
            Debug.Log("=== 检查完成 ===");
        }
        
        #endregion
    }
}
