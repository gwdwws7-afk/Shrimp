using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Flow")]
        public string startSceneName = "Level_01_TrenchRift";
        public bool loadSaveOnStart = true;

        [Header("Progression")]
        public bool useProgressionSelection = true;
        public List<ChapterData> chapters = new List<ChapterData>();
        public LevelData fallbackLevelData;

        [Header("Progression UI")]
        public bool showLevelSelect = true;
        public float levelListHeight = 220f;
        public string lockedLevelLabel = "Locked";

        [Header("Input")]
        public string startActionName = "MenuConfirm";
        public string quitActionName = "QuitMenu";
        public KeyCode startKey = KeyCode.Return;
        public KeyCode quitKey = KeyCode.Escape;
        public PlayerInputHandler inputHandler;

        [Header("UI")]
        public string titleText = "Abyss Warriors";
        public string subtitleText = "Press Enter to Start";
        public bool showSubtitle = true;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle infoStyle;
        private GUIStyle sectionStyle;
        private GUIStyle levelButtonStyle;
        private Vector2 levelScroll;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (loadSaveOnStart && SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }

            SetupStyles();
        }

        private void Update()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            bool startPressed = handler != null && handler.WasActionPressedThisFrame(startActionName, startKey);
            bool quitPressed = handler != null && handler.WasActionPressedThisFrame(quitActionName, quitKey);

            if (startPressed)
            {
                StartGame();
            }

            if (quitPressed)
            {
                QuitGame();
            }
        }

        private void OnGUI()
        {
            if (titleStyle == null)
            {
                SetupStyles();
            }

            float panelWidth = showLevelSelect ? 560f : 420f;
            float panelHeight = showLevelSelect ? 560f : 320f;
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Box(panelRect, string.Empty);

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(10f);
            GUILayout.Label(titleText, titleStyle);

            if (showSubtitle)
            {
                GUILayout.Space(8f);
                GUILayout.Label(GetSubtitleText(), subtitleStyle);
            }

            DrawNextLevelPreview();

            if (showLevelSelect)
            {
                DrawLevelSelection();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Start Game", buttonStyle))
            {
                StartGame();
            }

            if (GUILayout.Button("Quit", buttonStyle))
            {
                QuitGame();
            }

            GUILayout.Space(10f);
            GUILayout.EndArea();
        }

        private void StartGame()
        {
            if (loadSaveOnStart && SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }
            if (useProgressionSelection && TryStartProgressionLevel())
            {
                return;
            }

            SceneManager.LoadScene(startSceneName);
        }

        private void StartSelectedLevel(LevelData level)
        {
            if (level == null)
            {
                return;
            }

            ChapterData chapter = FindChapterForLevel(chapters, level);
            LevelSelectionContext.SetSelection(level, chapter);

            string sceneName = !string.IsNullOrEmpty(level.sceneName)
                ? level.sceneName
                : startSceneName;

            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void DrawNextLevelPreview()
        {
            if (!useProgressionSelection || infoStyle == null)
            {
                return;
            }

            List<int> completedLevels = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
                ? SaveManager.Instance.CurrentData.completedLevels
                : null;

            LevelData nextLevel = FindNextLevel(chapters, completedLevels) ?? fallbackLevelData;
            if (nextLevel == null)
            {
                return;
            }

            GUILayout.Space(12f);
            GUILayout.Label("下一关奖励预览");
            GUILayout.Label($"{nextLevel.levelId} - {nextLevel.levelName}", infoStyle);
            GUILayout.Label($"难度: {nextLevel.difficulty}  |  推荐等级 {nextLevel.recommendedLevel}", infoStyle);
            GUILayout.Label($"基础货币奖励: {nextLevel.baseCredits}", infoStyle);
        }

        private void DrawLevelSelection()
        {
            if (sectionStyle == null || levelButtonStyle == null)
            {
                return;
            }

            List<int> completedLevels = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
                ? SaveManager.Instance.CurrentData.completedLevels
                : null;

            LevelData nextLevel = FindNextLevel(chapters, completedLevels);

            GUILayout.Space(12f);
            GUILayout.Label("选择关卡", sectionStyle);
            levelScroll = GUILayout.BeginScrollView(levelScroll, GUILayout.Height(levelListHeight));

            List<LevelData> allLevels = CollectLevels(chapters);
            for (int i = 0; i < allLevels.Count; i++)
            {
                LevelData level = allLevels[i];
                if (level == null)
                {
                    continue;
                }

                bool completed = IsLevelCompleted(level, completedLevels);
                bool unlocked = completed || (nextLevel != null && level == nextLevel);
                string label = $"{level.levelId} - {level.levelName}";

                if (unlocked)
                {
                    if (GUILayout.Button(label, levelButtonStyle))
                    {
                        StartSelectedLevel(level);
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    GUILayout.Label($"{label} ({lockedLevelLabel})", infoStyle);
                }
            }

            GUILayout.EndScrollView();
        }

        private bool TryStartProgressionLevel()
        {
            if (chapters == null || chapters.Count == 0)
            {
                return false;
            }

            List<int> completedLevels = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
                ? SaveManager.Instance.CurrentData.completedLevels
                : null;

            LevelData nextLevel = FindNextLevel(chapters, completedLevels);
            if (nextLevel == null)
            {
                nextLevel = fallbackLevelData;
            }

            if (nextLevel == null)
            {
                return false;
            }

            ChapterData chapter = FindChapterForLevel(chapters, nextLevel);
            LevelSelectionContext.SetSelection(nextLevel, chapter);

            string sceneName = !string.IsNullOrEmpty(nextLevel.sceneName)
                ? nextLevel.sceneName
                : startSceneName;

            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            SceneManager.LoadScene(sceneName);
            return true;
        }

        private static LevelData FindNextLevel(List<ChapterData> chapterList, List<int> completedLevels)
        {
            if (chapterList == null || chapterList.Count == 0)
            {
                return null;
            }

            LevelData lastLevel = null;
            for (int i = 0; i < chapterList.Count; i++)
            {
                ChapterData chapter = chapterList[i];
                if (chapter == null || chapter.levels == null || chapter.levels.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < chapter.levels.Count; j++)
                {
                    LevelData level = chapter.levels[j];
                    if (level == null)
                    {
                        continue;
                    }

                    lastLevel = level;
                    int levelKey = ResolveLevelKey(level);
                    if (levelKey <= 0)
                    {
                        if (completedLevels == null || completedLevels.Count == 0)
                        {
                            return level;
                        }
                        continue;
                    }

                    if (completedLevels == null || !completedLevels.Contains(levelKey))
                    {
                        return level;
                    }
                }
            }

            return lastLevel;
        }

        private static int ResolveLevelKey(LevelData level)
        {
            if (level == null)
            {
                return 0;
            }

            int parsedLevel = 0;
            if (!string.IsNullOrEmpty(level.levelId) && level.levelId.StartsWith("LEVEL_"))
            {
                int.TryParse(level.levelId.Replace("LEVEL_", string.Empty), out parsedLevel);
            }

            if (level.chapterId > 0 && parsedLevel > 0)
            {
                return level.chapterId * 100 + parsedLevel;
            }

            return 0;
        }

        private static ChapterData FindChapterForLevel(List<ChapterData> chapterList, LevelData level)
        {
            if (chapterList == null || level == null)
            {
                return null;
            }

            for (int i = 0; i < chapterList.Count; i++)
            {
                ChapterData chapter = chapterList[i];
                if (chapter != null && chapter.levels != null && chapter.levels.Contains(level))
                {
                    return chapter;
                }
            }

            return null;
        }

        private void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            Debug.Log("QuitGame called (Editor)");
#endif
        }

        private void SetupStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.7f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fixedHeight = 42f
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.8f) }
            };

            levelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fixedHeight = 28f
            };
        }

        private static bool IsLevelCompleted(LevelData level, List<int> completedLevels)
        {
            int levelKey = ResolveLevelKey(level);
            if (levelKey <= 0 || completedLevels == null)
            {
                return false;
            }

            return completedLevels.Contains(levelKey);
        }

        private static List<LevelData> CollectLevels(List<ChapterData> chapterList)
        {
            List<LevelData> levels = new List<LevelData>();
            if (chapterList == null)
            {
                return levels;
            }

            for (int i = 0; i < chapterList.Count; i++)
            {
                ChapterData chapter = chapterList[i];
                if (chapter == null || chapter.levels == null)
                {
                    continue;
                }

                for (int j = 0; j < chapter.levels.Count; j++)
                {
                    LevelData level = chapter.levels[j];
                    if (level != null)
                    {
                        levels.Add(level);
                    }
                }
            }

            return levels;
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

        private string GetSubtitleText()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            string binding = handler != null
                ? handler.GetActionBindingLabel(startActionName, startKey)
                : PlayerInputHandler.GetFriendlyKeyLabel(startKey);

            if (string.IsNullOrEmpty(binding))
            {
                return subtitleText;
            }

            return $"Press {binding} to Start";
        }
    }
}
