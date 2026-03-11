using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    /// <summary>
    /// UIAutoSetup 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class UIAutoSetup : MonoBehaviour
    {
        [Header("自动配置设置")]
        public bool autoSetupOnStart = true;
        public bool createIfNotExists = true;
        public bool logDebugInfo = true;
        
        [Header("设置")]
        public GameObject damageTextPrefab; // UI 引用，用于驱动界面表现与信息同步。
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupAllUI();
            }
        }
        
        /// <summary>
        /// 设置Setup All UI，统一写入入口，便于约束副作用。
        /// </summary>
        public void SetupAllUI()
        {
            if (logDebugInfo) Debug.Log("[UIAutoSetup] 开始自动配置 UI。");
            
// 围绕 UIManager 执行该步骤，用于保证流程状态与后续分支一致。
            UIManager uiManager = SetupUIManager();
            if (uiManager == null)
            {
                Debug.LogError("[UIAutoSetup] 配置失败：未能获取或创建 UIManager。");
                return;
            }
            
// 围绕 Canvas 执行该步骤，用于保证流程状态与后续分支一致。
            Canvas canvas = SetupCanvas();
            
// 围绕 SetupHPBar 执行该步骤，用于保证流程状态与后续分支一致。
            SetupHPBar(uiManager, canvas);
            SetupStaminaBar(uiManager, canvas);
            SetupMusouBar(uiManager, canvas);
            SetupExperienceBar(uiManager, canvas);
            SetupStrongholdWavePanel(uiManager, canvas);
            SetupComboCounter(uiManager, canvas);
            SetupSkillBar(uiManager, canvas);
            SetupDamageTextSystem(uiManager, canvas);
            
            if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
        }
        
        /// <summary>
        /// 设置Setup UIManager，统一写入入口，便于约束副作用。
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
                    if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 UIManager。");
                }
                else
                {
                    Debug.LogWarning("[UIAutoSetup] 未找到 UIManager，且未启用自动创建。");
                    return null;
                }
            }
            else
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 使用已存在的 UIManager。");
            }
            
            return uiManager;
        }
        
        /// <summary>
        /// 设置Setup Canvas，统一写入入口，便于约束副作用。
        /// </summary>
        private Canvas SetupCanvas()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                if (createIfNotExists)
                {
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
                    GameObject canvasObj = new GameObject("MainCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 0;
                    
// 围绕 CanvasScaler 执行该步骤，用于保持上下文语义一致。
                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                    
// 围绕 canvasObj 执行该步骤，用于保持上下文语义一致。
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
// 围绕 CreateEventSystem 执行该步骤，用于保证流程状态与后续分支一致。
                    CreateEventSystem();
                    
                    if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 MainCanvas。");
                }
                else
                {
                    Debug.LogWarning("[UIAutoSetup] 未找到 Canvas，且未启用自动创建。");
                    return null;
                }
            }
            else
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 使用已存在的 Canvas: " + canvas.name);
            }
            
            return canvas;
        }
        
        /// <summary>
        /// 创建Event System，按默认规则构建实例并纳入管理。
        /// </summary>
        private void CreateEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 EventSystem。");
            }
        }
        
        /// <summary>
        /// 设置Setup HPBar，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupHPBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.hpBar != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }
            
// 场景级兜底查找依赖，降低手动绑定遗漏风险。
            UI_HPBar hpBar = FindObjectOfType<UI_HPBar>();
            
            if (hpBar == null && createIfNotExists)
            {
                hpBar = CreateHPBarUI(canvas);
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 HPBar。");
            }
            
            if (hpBar != null)
            {
                uiManager.hpBar = hpBar;
                
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
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
        /// 创建HPBar UI，按默认规则构建实例并纳入管理。
        /// </summary>
        private UI_HPBar CreateHPBarUI(Canvas canvas)
        {
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject hpBarObj = new GameObject("HPBar");
            hpBarObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = hpBarObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -20);
            rectTransform.sizeDelta = new Vector2(300, 40);
            
            UI_HPBar hpBar = hpBarObj.AddComponent<UI_HPBar>();
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hpBarObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(hpBarObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(5, 5);
            sliderRect.offsetMax = new Vector2(-5, -5);
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 hpBar 执行该步骤，用于保持上下文语义一致。
            hpBar.hpSlider = slider;
            hpBar.fillImage = fillImage;
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
        /// 设置Setup Stamina Bar，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupStaminaBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.staminaBar != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }
            
            UI_StaminaBar staminaBar = FindObjectOfType<UI_StaminaBar>();
            
            if (staminaBar == null && createIfNotExists)
            {
                staminaBar = CreateStaminaBarUI(canvas);
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 StaminaBar。");
            }
            
            if (staminaBar != null)
            {
                uiManager.staminaBar = staminaBar;
            }
        }

        /// <summary>
        /// 设置Setup Musou Bar，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupMusouBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.musouBar != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }

            UI_MusouBar musouBar = FindObjectOfType<UI_MusouBar>();

            if (musouBar == null && createIfNotExists)
            {
                musouBar = CreateMusouBarUI(canvas);
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 MusouBar。");
            }

            if (musouBar != null)
            {
                uiManager.musouBar = musouBar;
            }
        }
        
        /// <summary>
        /// 创建Stamina Bar UI，按默认规则构建实例并纳入管理。
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
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(staminaBarObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(staminaBarObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(3, 3);
            sliderRect.offsetMax = new Vector2(-3, -3);
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
        /// 创建Musou Bar UI，按默认规则构建实例并纳入管理。
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

// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
            readyText.text = "V 可释放";

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
        /// 设置Setup Stronghold Wave Panel，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupStrongholdWavePanel(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.strongholdWavePanel != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }

            UI_StrongholdWavePanel panel = FindObjectOfType<UI_StrongholdWavePanel>();
            if (panel == null && createIfNotExists)
            {
                panel = CreateStrongholdWavePanelUI(canvas);
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 StrongholdWavePanel。");
            }

            if (panel != null)
            {
                uiManager.strongholdWavePanel = panel;
            }
        }

        /// <summary>
        /// 创建Stronghold Wave Panel UI，按默认规则构建实例并纳入管理。
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
        /// 设置Setup Experience Bar，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupExperienceBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.experienceBar != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }

            UI_ExperienceBar experienceBar = FindObjectOfType<UI_ExperienceBar>();

            if (experienceBar == null && createIfNotExists)
            {
                experienceBar = CreateExperienceBarUI(canvas);
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 ExperienceBar。");
            }

            if (experienceBar != null)
            {
                uiManager.experienceBar = experienceBar;
            }
        }

        /// <summary>
        /// 创建Experience Bar UI，按默认规则构建实例并纳入管理。
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
        /// 设置Setup Combo Counter，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupComboCounter(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.comboCounter != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }
            
            UI_ComboCounter comboCounter = FindObjectOfType<UI_ComboCounter>();
            
            if (comboCounter == null && createIfNotExists)
            {
                comboCounter = CreateComboCounterUI(canvas);
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 ComboCounter。");
            }
            
            if (comboCounter != null)
            {
                uiManager.comboCounter = comboCounter;
            }
        }
        
        /// <summary>
        /// 创建Combo Counter UI，按默认规则构建实例并纳入管理。
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
            
// 围绕 CanvasGroup 执行该步骤，用于保持上下文语义一致。
            CanvasGroup canvasGroup = comboObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            comboCounter.canvasGroup = canvasGroup;
            
// 围绕 游戏连击 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 游戏连击 执行该步骤，用于保证流程状态与后续分支一致。
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
        /// 设置Setup Skill Bar，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupSkillBar(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.skillBar != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }
            
            UI_SkillBar skillBar = FindObjectOfType<UI_SkillBar>();
            
            if (skillBar == null && createIfNotExists)
            {
                skillBar = CreateSkillBarUI(canvas);
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 SkillBar。");
            }
            
            if (skillBar != null)
            {
                uiManager.skillBar = skillBar;
            }
        }
        
        /// <summary>
        /// 创建Skill Bar UI，按默认规则构建实例并纳入管理。
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
        /// 围绕 技能 执行该方法，确保调用链路与状态迁移一致。
        /// </summary>
        private UI_SkillBar.SkillSlot CreateSkillSlot(Transform parent, string key, int index, float xPos, float size)
        {
            UI_SkillBar.SkillSlot slot = new UI_SkillBar.SkillSlot();
            
// 围绕 游戏技能 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
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
        /// 设置Setup Damage Text System，统一写入入口，便于约束副作用。
        /// </summary>
        private void SetupDamageTextSystem(UIManager uiManager, Canvas canvas)
        {
            if (uiManager.damageTextParent != null)
            {
                if (logDebugInfo) Debug.Log("[UIAutoSetup] 目标 UI 已存在，跳过创建。");
                return;
            }
            
// 围绕 游戏伤害 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject damageParentObj = new GameObject("DamageTextParent");
            damageParentObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = damageParentObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            uiManager.damageTextParent = damageParentObj.transform;
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (damageTextPrefab != null)
            {
                uiManager.damageTextPrefab = damageTextPrefab;
            }
            
            if (logDebugInfo) Debug.Log("[UIAutoSetup] 已创建 DamageTextParent。");
        }
        
        #region 编辑器工具方法
        
        /// <summary>
        /// 设置Setup UINow，统一写入入口，便于约束副作用。
        /// </summary>
        [ContextMenu("Setup UI Now")]
        public void SetupUINow()
        {
            SetupAllUI();
        }
        
        /// <summary>
        /// 执行 Check UIStatus 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        [ContextMenu("Check UI Status")]
        public void CheckUIStatus()
        {
            Debug.Log("=== UI 状态检查 ===");
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                Debug.Log("UIManager: 已找到");
                Debug.Log($"  - HPBar: {(uiManager.hpBar != null ? "正常" : "缺失")}");
                Debug.Log($"  - StaminaBar: {(uiManager.staminaBar != null ? "正常" : "缺失")}");
                Debug.Log($"  - MusouBar: {(uiManager.musouBar != null ? "正常" : "缺失")}");
                Debug.Log($"  - ExperienceBar: {(uiManager.experienceBar != null ? "正常" : "缺失")}");
                Debug.Log($"  - StrongholdWavePanel: {(uiManager.strongholdWavePanel != null ? "正常" : "缺失")}");
                Debug.Log($"  - ComboCounter: {(uiManager.comboCounter != null ? "正常" : "缺失")}");
                Debug.Log($"  - SkillBar: {(uiManager.skillBar != null ? "正常" : "缺失")}");
                Debug.Log($"  - DamageTextParent: {(uiManager.damageTextParent != null ? "正常" : "缺失")}");
            }
            else
            {
                Debug.Log("UIManager: 未找到");
            }
            
            Canvas canvas = FindObjectOfType<Canvas>();
            Debug.Log($"Canvas: {(canvas != null ? "已找到 " + canvas.name : "未找到")}");
            
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            Debug.Log($"EventSystem: {(eventSystem != null ? "已找到" : "未找到")}");
            
            Debug.Log("=== 状态检查完成 ===");
        }
        
        #endregion
    }
}

