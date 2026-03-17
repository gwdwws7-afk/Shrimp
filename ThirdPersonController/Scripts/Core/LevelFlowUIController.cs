using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController
{
    public class LevelFlowUIController : MonoBehaviour
    {
        [Header("References")]
        public LevelFlowController levelFlow;
        public UI_TalentEquipmentOverlay talentOverlay;
        public SessionRewardTracker rewardTracker;
        public StatisticsManager statisticsManager;
        public LongTermProgressionSystem longTermProgression;
        public PlayerInputHandler inputHandler;

        [Header("Prep")]
        public bool pauseDuringPrep = true;
        public string startActionName = "MenuConfirm";
        public KeyCode startKey = KeyCode.Return;
        public string startButtonLabel = "Start Battle";
        public string startHintLabel = "Press Enter to Start";
        public bool showRouteSelector = true;
        public string routeLabel = "成长路线";

        [Header("Result")]
        public bool pauseDuringResult = true;
        public string continueActionName = "MenuConfirm";
        public string retryActionName = "MenuRetry";
        public KeyCode continueKey = KeyCode.Return;
        public KeyCode retryKey = KeyCode.R;
        public string continueLabel = "继续";
        public string retryLabel = "重试";
        public string victoryTitle = "关卡完成";
        public string defeatTitle = "关卡失败";

        [Header("Layout")]
        public float prepPanelWidth = 980f;
        public float prepPanelHeight = 700f;
        public float resultPanelWidth = 560f;
        public float resultPanelHeight = 420f;

        private bool showPrep;
        private bool showResult;
        private bool lastVictory;
        private bool previousToggleState = true;

        private string levelTitle;
        private string levelDescription;
        private string objectiveText;

        private int cachedTalentPoints;
        private int cachedPearls;
        private int cachedCredits;
        private int cachedKills;
        private int cachedCombo;
        private string cachedTime;
        private string cachedMilestoneStatus;

        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            if (levelFlow == null)
            {
                levelFlow = FindObjectOfType<LevelFlowController>();
            }

            if (talentOverlay == null)
            {
                talentOverlay = FindObjectOfType<UI_TalentEquipmentOverlay>();
            }

            if (rewardTracker == null)
            {
                rewardTracker = FindObjectOfType<SessionRewardTracker>();
                if (rewardTracker == null)
                {
                    GameObject trackerObject = new GameObject("SessionRewardTracker");
                    rewardTracker = trackerObject.AddComponent<SessionRewardTracker>();
                }
            }

            if (statisticsManager == null)
            {
                statisticsManager = FindObjectOfType<StatisticsManager>();
            }

            if (longTermProgression == null)
            {
                longTermProgression = FindObjectOfType<LongTermProgressionSystem>();
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        }

        private void Update()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            bool startPressed = handler != null && handler.WasActionPressedThisFrame(startActionName, startKey);
            bool continuePressed = handler != null && handler.WasActionPressedThisFrame(continueActionName, continueKey);
            bool retryPressed = handler != null && handler.WasActionPressedThisFrame(retryActionName, retryKey);

            if (showPrep)
            {
                if (startPressed)
                {
                    StartFromPrep();
                }
                return;
            }

            if (showResult)
            {
                if (continuePressed)
                {
                    ContinueFromResult();
                }
                else if (retryPressed)
                {
                    RetryFromResult();
                }
            }
        }

        private void OnGUI()
        {
            if (showPrep)
            {
                DrawPrepPanel();
            }
            else if (showResult)
            {
                DrawResultPanel();
            }
        }

        public void ShowPrep(LevelFlowController flow)
        {
            levelFlow = flow;
            levelTitle = flow != null ? flow.levelTitle : "";
            levelDescription = flow != null && flow.levelData != null ? flow.levelData.description : string.Empty;
            objectiveText = BuildObjectiveText(flow);

            showPrep = true;
            showResult = false;

            if (pauseDuringPrep)
            {
                Time.timeScale = 0f;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (talentOverlay != null)
            {
                previousToggleState = talentOverlay.allowToggle;
                talentOverlay.allowToggle = false;
                talentOverlay.SetOpen(false, false);
            }
        }

        public void HidePrep()
        {
            showPrep = false;

            if (pauseDuringPrep)
            {
                Time.timeScale = 1f;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (talentOverlay != null)
            {
                talentOverlay.allowToggle = previousToggleState;
            }
        }

        public void ShowResult(bool isVictory)
        {
            if (showResult)
            {
                return;
            }

            lastVictory = isVictory;
            showResult = true;
            showPrep = false;

            if (pauseDuringResult)
            {
                Time.timeScale = 0f;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            CacheResultData();
        }

        private void StartFromPrep()
        {
            if (levelFlow != null)
            {
                levelFlow.BeginFromPrep();
            }
            HidePrep();
        }

        private void ContinueFromResult()
        {
            if (levelFlow != null)
            {
                levelFlow.ExitToMenu(lastVictory);
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void RetryFromResult()
        {
            if (levelFlow != null)
            {
                levelFlow.RetryLevel();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void HandleGameOver(bool isVictory)
        {
            ShowResult(isVictory);
        }

        private void HandlePlayerDeath()
        {
            ShowResult(false);
        }

        private void CacheResultData()
        {
            if (rewardTracker != null)
            {
                rewardTracker.CaptureEnd();
                cachedTalentPoints = rewardTracker.lastGainedTalentPoints;
                cachedPearls = rewardTracker.lastGainedPearls;
                cachedCredits = rewardTracker.lastGainedCredits;
            }
            else
            {
                cachedTalentPoints = 0;
                cachedPearls = 0;
                cachedCredits = 0;
            }

            if (statisticsManager != null)
            {
                cachedKills = statisticsManager.sessionKills;
                cachedCombo = statisticsManager.sessionHighestCombo;
                cachedTime = statisticsManager.GetSessionTimeFormatted();
            }
            else
            {
                cachedKills = 0;
                cachedCombo = 0;
                cachedTime = "0s";
            }

            if (longTermProgression != null)
            {
                cachedMilestoneStatus = longTermProgression.GetNextMilestoneStatus();
            }
            else
            {
                cachedMilestoneStatus = string.Empty;
            }
        }

        private string BuildObjectiveText(LevelFlowController flow)
        {
            if (flow == null)
            {
                return string.Empty;
            }

            LevelData data = flow.levelData;
            List<string> lines = new List<string>();

            if (data != null)
            {
                if (data.strongholds != null && data.strongholds.Count > 0)
                {
                    lines.Add("清理全部据点");
                }

                if (data.quests != null && data.quests.Count > 0)
                {
                    lines.Add($"完成 {data.quests.Count} 个任务");
                }

                if (data.timeLimit > 0)
                {
                    int minutes = Mathf.CeilToInt(data.timeLimit / 60f);
                    lines.Add($"限时: {minutes} 分钟.");
                }

                if (data.scoreTarget > 0)
                {
                    lines.Add($"目标分数: {data.scoreTarget}.");
                }
            }

            if (lines.Count == 0)
            {
                return "无额外目标";
            }

            return string.Join("\n", lines);
        }

        private void DrawPrepPanel()
        {
            EnsureStyles();

            Rect panelRect = new Rect(
                (Screen.width - prepPanelWidth) * 0.5f,
                (Screen.height - prepPanelHeight) * 0.5f,
                prepPanelWidth,
                prepPanelHeight);

            GUI.Box(panelRect, string.Empty);

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(8f);
            GUILayout.Label(levelTitle, titleStyle);

            if (!string.IsNullOrEmpty(levelDescription))
            {
                GUILayout.Space(4f);
                GUILayout.Label(levelDescription, bodyStyle);
            }

            if (!string.IsNullOrEmpty(objectiveText))
            {
                GUILayout.Space(4f);
                GUILayout.Label(objectiveText, smallStyle);
            }

            if (showRouteSelector)
            {
                DrawRouteSelector();
            }
            GUILayout.EndArea();

            Rect embedRect = new Rect(
                panelRect.x + 20f,
                panelRect.y + 120f,
                panelRect.width - 40f,
                panelRect.height - 200f);

            GUI.Box(embedRect, string.Empty);
            if (talentOverlay != null)
            {
                talentOverlay.DrawEmbedded(embedRect, false);
            }
            else
            {
                GUI.Label(new Rect(embedRect.x + 10f, embedRect.y + 10f, embedRect.width - 20f, 20f),
                    "未找到天赋/装备面板组件。", smallStyle);
            }

            Rect footerRect = new Rect(panelRect.x, panelRect.y + panelRect.height - 60f, panelRect.width, 50f);
            GUILayout.BeginArea(footerRect);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(startButtonLabel, buttonStyle, GUILayout.Width(180f), GUILayout.Height(36f)))
            {
                StartFromPrep();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label(GetStartHintLabel(), smallStyle);
            GUILayout.EndArea();
        }

        private void DrawResultPanel()
        {
            EnsureStyles();

            Rect panelRect = new Rect(
                (Screen.width - resultPanelWidth) * 0.5f,
                (Screen.height - resultPanelHeight) * 0.5f,
                resultPanelWidth,
                resultPanelHeight);

            GUI.Box(panelRect, string.Empty);

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(8f);
            GUILayout.Label(lastVictory ? victoryTitle : defeatTitle, titleStyle);
            GUILayout.Space(10f);

            GUILayout.Label("奖励", sectionStyle);
            GUILayout.Label($"+{cachedCredits} 货币", bodyStyle);
            GUILayout.Label($"+{cachedPearls} 珍珠", bodyStyle);
            GUILayout.Label($"+{cachedTalentPoints} 天赋点", bodyStyle);
            GUILayout.Space(10f);

            GUILayout.Label("统计", sectionStyle);
            GUILayout.Label($"击杀: {cachedKills}", bodyStyle);
            GUILayout.Label($"最高连击: {cachedCombo}", bodyStyle);
            GUILayout.Label($"时间: {cachedTime}", bodyStyle);
            if (!string.IsNullOrEmpty(cachedMilestoneStatus))
            {
                GUILayout.Space(8f);
                GUILayout.Label("进度", sectionStyle);
                GUILayout.Label(cachedMilestoneStatus, bodyStyle);
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(continueLabel, buttonStyle, GUILayout.Width(160f), GUILayout.Height(36f)))
            {
                ContinueFromResult();
            }
            GUILayout.Space(12f);
            if (GUILayout.Button(retryLabel, buttonStyle, GUILayout.Width(140f), GUILayout.Height(36f)))
            {
                RetryFromResult();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label(GetResultHintLabel(), smallStyle);
            GUILayout.EndArea();
        }

        private void DrawRouteSelector()
        {
            if (longTermProgression == null)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.Label(routeLabel, sectionStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("进攻", buttonStyle, GUILayout.Width(120f), GUILayout.Height(28f)))
            {
                longTermProgression.SetActiveRoute(ProgressionRoute.Offense);
            }
            if (GUILayout.Button("控场", buttonStyle, GUILayout.Width(120f), GUILayout.Height(28f)))
            {
                longTermProgression.SetActiveRoute(ProgressionRoute.Control);
            }
            if (GUILayout.Button("生存", buttonStyle, GUILayout.Width(120f), GUILayout.Height(28f)))
            {
                longTermProgression.SetActiveRoute(ProgressionRoute.Survival);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);
            GUILayout.Label($"当前: {GetRouteLabel(longTermProgression.activeRoute)}", smallStyle);
        }

        private static string GetRouteLabel(ProgressionRoute route)
        {
            switch (route)
            {
                case ProgressionRoute.Offense:
                    return "进攻";
                case ProgressionRoute.Control:
                    return "控场";
                case ProgressionRoute.Survival:
                    return "生存";
                default:
                    return route.ToString();
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.85f) }
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.7f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16
            };
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

        private string GetStartHintLabel()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            string binding = handler != null
                ? handler.GetActionBindingLabel(startActionName, startKey)
                : PlayerInputHandler.GetFriendlyKeyLabel(startKey);

            if (string.IsNullOrEmpty(binding))
            {
                return startHintLabel;
            }

            return $"Press {binding} to Start";
        }

        private string GetResultHintLabel()
        {
            PlayerInputHandler handler = ResolveInputHandler();
            string continueBinding = handler != null
                ? handler.GetActionBindingLabel(continueActionName, continueKey)
                : PlayerInputHandler.GetFriendlyKeyLabel(continueKey);
            string retryBinding = handler != null
                ? handler.GetActionBindingLabel(retryActionName, retryKey)
                : PlayerInputHandler.GetFriendlyKeyLabel(retryKey);

            if (string.IsNullOrEmpty(continueBinding))
            {
                continueBinding = PlayerInputHandler.GetFriendlyKeyLabel(continueKey);
            }

            if (string.IsNullOrEmpty(retryBinding))
            {
                retryBinding = PlayerInputHandler.GetFriendlyKeyLabel(retryKey);
            }

            return $"{continueBinding}: 继续   {retryBinding}: 重试";
        }
    }
}
