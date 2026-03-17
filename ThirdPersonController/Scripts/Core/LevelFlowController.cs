using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController
{
    public class LevelFlowController : MonoBehaviour
    {
        [Header("Level Data")]
        public LevelData levelData;
        public ChapterData chapterData;
        
        [Header("Scene Flow")]
        public string mainMenuSceneName = "MainMenu";
        public int levelId = 1;

        [Header("Input")]
        public string menuActionName = "MenuCancel";
        public KeyCode menuKey = KeyCode.Escape;
        public PlayerInputHandler inputHandler;

        [Header("UI")]
        public string levelTitle = "Sample Level";
        public bool showOverlay = true;

        [Header("Level Prep")]
        public bool showLevelIntro = true;
        public float introDuration = 3f;

        [Header("Flow UI")]
        public LevelFlowUIController flowUI;

        [Header("Systems")]
        public ComboMomentumRewardSystem comboRewards;
        public LevelRuntimeConfigurator runtimeConfigurator;
        public QuestDatabase questDatabase;
        public ProgressionMilestoneData progressionMilestones;
        public LongTermProgressionSystem longTermProgression;
        public EconomyConfig economyConfig;
        
        [Header("Timer")]
        public bool useTimer = false;
        public float levelTime = 0f;
        
        [Header("Lighting")]
        public bool ensureLightingOnStart = true;
        public float fallbackLightIntensity = 1f;

        private bool menuOpen;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private float currentLevelTime;
        private bool levelStarted;
        private bool levelEnded;
        private bool levelCompleted;

        private void Awake()
        {
            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }

            ApplyLevelSelection();
            InitializeRuntimeConfigurator();
            EnsureLongTermProgression();
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        }

        private void Start()
        {
            ApplyLevelData();
            if (runtimeConfigurator != null)
            {
                runtimeConfigurator.Apply();
            }

            if (ensureLightingOnStart)
            {
                EnsureLighting();
            }

            InitializeFlowUI();
            EnsureMomentumRewards();

            if (showLevelIntro)
            {
                if (flowUI != null)
                {
                    flowUI.ShowPrep(this);
                }
                else
                {
                    StartCoroutine(LevelIntroRoutine());
                }
            }
            else
            {
                StartLevel();
            }
        }

        private void ApplyLevelData()
        {
            if (levelData != null)
            {
                int parsedLevel = 0;
                if (!string.IsNullOrEmpty(levelData.levelId) && levelData.levelId.StartsWith("LEVEL_"))
                {
                    int.TryParse(levelData.levelId.Replace("LEVEL_", string.Empty), out parsedLevel);
                }

                if (levelData.chapterId > 0 && parsedLevel > 0)
                {
                    levelId = levelData.chapterId * 100 + parsedLevel;
                }
                levelTitle = levelData.levelName;
                useTimer = levelData.timeLimit > 0;
                levelTime = levelData.timeLimit;
            }
            
            if (chapterData != null)
            {
                levelTitle = $"{chapterData.chapterName} - {levelTitle}";
            }
        }
        
        private System.Collections.IEnumerator LevelIntroRoutine()
        {
            yield return new WaitForSeconds(introDuration);
            StartLevel();
        }
        
        private void StartLevel()
        {
            levelStarted = true;
            currentLevelTime = 0f;

            GameEvents.LevelStarted(levelId);
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.currentLevel = levelId;
            }
        }

        private void Update()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            bool menuPressed = handler != null && handler.WasActionPressedThisFrame(menuActionName, menuKey);

            if (menuPressed)
            {
                ToggleMenu();
            }
            
            if (levelStarted && !levelEnded && useTimer)
            {
                currentLevelTime += Time.deltaTime;
                
                if (levelTime > 0 && currentLevelTime >= levelTime)
                {
                    HandleTimeUp();
                }
            }
        }
        
        private void HandleTimeUp()
        {
            levelEnded = true;
            GameEvents.ShowMessage("Time's Up!", 3f);
            GameEvents.GameOver(false);
        }

        private void OnGUI()
        {
            if (!showOverlay || !menuOpen)
            {
                return;
            }

            if (titleStyle == null)
            {
                SetupStyles();
            }

            float panelWidth = 360f;
            float panelHeight = 220f;
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Box(panelRect, string.Empty);
            GUILayout.BeginArea(panelRect);
            GUILayout.Space(10f);
            GUILayout.Label(levelTitle, titleStyle);
            GUILayout.Space(20f);

            if (GUILayout.Button("Resume", buttonStyle))
            {
                CloseMenu();
            }

            if (GUILayout.Button("Exit to Main Menu", buttonStyle))
            {
                ExitToMenu();
            }

            GUILayout.EndArea();
        }

        private void ToggleMenu()
        {
            if (menuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        private void OpenMenu()
        {
            menuOpen = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CloseMenu()
        {
            menuOpen = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ExitToMenu()
        {
            menuOpen = false;
            Time.timeScale = 1f;
            
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void BeginFromPrep()
        {
            StartLevel();
        }

        public void RetryLevel()
        {
            menuOpen = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ExitToMenu(bool markComplete)
        {
            if (markComplete && !levelCompleted)
            {
                GameEvents.LevelCompleted(levelId);
                levelCompleted = true;
            }
            ExitToMenu();
        }

        public bool IsLevelStarted => levelStarted;
        public bool IsLevelEnded => levelEnded;

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void HandleGameOver(bool isVictory)
        {
            if (levelStarted)
            {
                levelEnded = true;
            }
        }

        private void HandlePlayerDeath()
        {
            if (levelStarted)
            {
                levelEnded = true;
            }
        }

        private void HandleLevelCompleted(int completedLevelId)
        {
            if (completedLevelId == levelId)
            {
                levelCompleted = true;
            }
        }

        private void SetupStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fixedHeight = 40f
            };
        }

        private void InitializeFlowUI()
        {
            if (flowUI == null)
            {
                flowUI = FindObjectOfType<LevelFlowUIController>();
            }

            if (flowUI == null)
            {
                GameObject uiObject = new GameObject("LevelFlowUI");
                flowUI = uiObject.AddComponent<LevelFlowUIController>();
            }

            flowUI.levelFlow = this;
        }

        private void InitializeRuntimeConfigurator()
        {
            if (runtimeConfigurator == null)
            {
                runtimeConfigurator = FindObjectOfType<LevelRuntimeConfigurator>();
            }

            if (runtimeConfigurator == null)
            {
                runtimeConfigurator = gameObject.AddComponent<LevelRuntimeConfigurator>();
                runtimeConfigurator.autoApplyOnAwake = false;
            }

            runtimeConfigurator.levelFlow = this;
            runtimeConfigurator.levelData = levelData;
            runtimeConfigurator.chapterData = chapterData;
            runtimeConfigurator.questDatabase = questDatabase;
            runtimeConfigurator.economyConfig = economyConfig;
            runtimeConfigurator.Apply();
        }

        private void ApplyLevelSelection()
        {
            if (!LevelSelectionContext.HasSelection)
            {
                return;
            }

            levelData = LevelSelectionContext.SelectedLevelData;
            if (LevelSelectionContext.SelectedChapterData != null)
            {
                chapterData = LevelSelectionContext.SelectedChapterData;
            }
        }

        private void EnsureLongTermProgression()
        {
            if (longTermProgression == null)
            {
                longTermProgression = FindObjectOfType<LongTermProgressionSystem>();
            }

            if (longTermProgression == null)
            {
                GameObject progressionObject = new GameObject("LongTermProgression");
                longTermProgression = progressionObject.AddComponent<LongTermProgressionSystem>();
            }

            longTermProgression.milestoneData = progressionMilestones;
        }

        private void EnsureMomentumRewards()
        {
            if (comboRewards == null)
            {
                comboRewards = FindObjectOfType<ComboMomentumRewardSystem>();
            }

            if (comboRewards == null)
            {
                GameObject rewardsObject = new GameObject("ComboMomentumRewards");
                comboRewards = rewardsObject.AddComponent<ComboMomentumRewardSystem>();
            }
        }

        private PlayerInputHandler ResolveInputHandler()
        {
            if (inputHandler != null)
            {
                return inputHandler;
            }

            inputHandler = PlayerInputHandler.ResolveActiveInstance();
            return inputHandler;
        }

        private void EnsureLighting()
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null && lights[i].type == LightType.Directional)
                    {
                        sun = lights[i];
                        break;
                    }
                }
            }

            if (sun == null)
            {
                GameObject lightObj = new GameObject("Directional Light (Auto)");
                sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = fallbackLightIntensity;
                lightObj.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            }

            if (sun != null)
            {
                sun.enabled = true;
                RenderSettings.sun = sun;
            }

            if (RenderSettings.skybox == null)
            {
                RenderSettings.skybox = Resources.GetBuiltinResource<Material>("Default-Skybox.mat");
            }

            DynamicGI.UpdateEnvironment();
        }
    }
}
