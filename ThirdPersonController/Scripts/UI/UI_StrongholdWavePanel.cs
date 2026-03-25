using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    public class UI_StrongholdWavePanel : MonoBehaviour
    {
        [Header("UI")]
        public CanvasGroup canvasGroup;
        public Text titleText;
        public Text waveText;
        public Text remainingText;
        public Text stateText;

        [Header("Behavior")]
        public float fadeSpeed = 6f;
        public float statusMessageDuration = 1.8f;
        public bool showWhenInactive = false;
        public bool logStartupStatus = true;

        [Header("Fallback Overlay")]
        public bool useFallbackOverlay = true;
        public Vector2 fallbackPosition = new Vector2(20f, 20f);
        public float fallbackWidth = 320f;
        public int fallbackFontSize = 14;

        public StrongholdSequenceController sequenceController;

        private StrongholdController activeStronghold;
        private float statusTimer;
        private string statusMessage;
        private GUIStyle fallbackTitleStyle;
        private GUIStyle fallbackBodyStyle;
        private bool startupLogged;

        private void OnEnable()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            EnsureSequenceController();

            RefreshActiveStronghold();
        }

        private void Start()
        {
            LogStartupStatus();
        }

        private void OnDisable()
        {
            BindStronghold(null);
        }

        private void Update()
        {
            EnsureSequenceController();
            RefreshActiveStronghold();

            UpdateStatusTimer();
            UpdateDisplay();
        }

        private void RefreshActiveStronghold()
        {
            StrongholdController target = null;
            if (sequenceController != null)
            {
                target = sequenceController.ActiveStronghold;
            }

            if (target == null)
            {
                StrongholdController[] strongholds = FindObjectsOfType<StrongholdController>();
                for (int i = 0; i < strongholds.Length; i++)
                {
                    if (strongholds[i] != null && strongholds[i].IsRunning)
                    {
                        target = strongholds[i];
                        break;
                    }
                }
            }

            if (target != activeStronghold)
            {
                BindStronghold(target);
            }
        }

        private void BindStronghold(StrongholdController stronghold)
        {
            if (activeStronghold != null)
            {
                activeStronghold.OnStrongholdStarted -= HandleStrongholdStarted;
                activeStronghold.OnWaveStarted -= HandleWaveStarted;
                activeStronghold.OnWaveCompleted -= HandleWaveCompleted;
                activeStronghold.OnStrongholdCompleted -= HandleStrongholdCompleted;
            }

            activeStronghold = stronghold;

            if (activeStronghold != null)
            {
                activeStronghold.OnStrongholdStarted += HandleStrongholdStarted;
                activeStronghold.OnWaveStarted += HandleWaveStarted;
                activeStronghold.OnWaveCompleted += HandleWaveCompleted;
                activeStronghold.OnStrongholdCompleted += HandleStrongholdCompleted;
            }
        }

        private void UpdateStatusTimer()
        {
            if (statusTimer > 0f)
            {
                statusTimer -= Time.deltaTime;
                if (statusTimer <= 0f)
                {
                    statusMessage = string.Empty;
                }
            }
        }

        private void UpdateDisplay()
        {
            bool hasStronghold = activeStronghold != null;
            bool running = hasStronghold && activeStronghold.IsRunning;
            bool visible = showWhenInactive || running;

            if (canvasGroup != null)
            {
                float target = visible ? 1f : 0f;
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.deltaTime * fadeSpeed);
                canvasGroup.interactable = canvasGroup.alpha > 0.1f;
                canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.1f;
            }

            if (!hasStronghold || !running)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = Localize("ui.stronghold.title", "据点推进");
            }

            if (activeStronghold.TryGetWaveStatus(out int waveIndex, out int totalWaves, out int remaining, out int plannedTotal))
            {
                if (waveText != null)
                {
                    waveText.text = string.Format(
                        Localize("ui.stronghold.wave_format", "波次 {0}/{1} · {2}"),
                        waveIndex + 1,
                        totalWaves,
                        activeStronghold.GetWaveDisplayName(waveIndex));
                }

                if (remainingText != null)
                {
                    if (plannedTotal > 0)
                    {
                        remainingText.text = string.Format(
                            Localize("ui.stronghold.remaining_ratio_format", "剩余 {0}/{1}"),
                            remaining,
                            plannedTotal);
                    }
                    else
                    {
                        remainingText.text = string.Format(
                            Localize("ui.stronghold.remaining_format", "剩余 {0}"),
                            remaining);
                    }
                }
            }

            if (stateText != null)
            {
                stateText.text = string.IsNullOrEmpty(statusMessage) ? "" : statusMessage;
            }
        }

        private void HandleStrongholdStarted(StrongholdController stronghold)
        {
            ShowStatus(Localize("ui.stronghold.status.started", "据点战斗开始"));
        }

        private void HandleWaveStarted(StrongholdController stronghold, int waveIndex)
        {
            ShowStatus(string.Format(Localize("ui.stronghold.status.wave_started", "波次 {0} 开始"), waveIndex + 1));
        }

        private void HandleWaveCompleted(StrongholdController stronghold, int waveIndex)
        {
            ShowStatus(string.Format(Localize("ui.stronghold.status.wave_completed", "波次 {0} 完成"), waveIndex + 1));
        }

        private void HandleStrongholdCompleted(StrongholdController stronghold)
        {
            ShowStatus(Localize("ui.stronghold.status.cleared", "据点清除"));
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusTimer = statusMessageDuration;
        }

        private void OnGUI()
        {
            if (!ShouldUseFallbackOverlay())
            {
                return;
            }

            bool hasStronghold = activeStronghold != null && activeStronghold.IsRunning;
            if (!hasStronghold && !showWhenInactive)
            {
                return;
            }

            EnsureFallbackStyles();

            Rect panelRect = new Rect(fallbackPosition.x, fallbackPosition.y, fallbackWidth, 122f);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label(Localize("ui.stronghold.title", "据点推进"), fallbackTitleStyle);

            if (hasStronghold && activeStronghold.TryGetWaveStatus(out int waveIndex, out int totalWaves, out int remaining, out int plannedTotal))
            {
                GUILayout.Label(
                    string.Format(
                        Localize("ui.stronghold.wave_format", "波次 {0}/{1} · {2}"),
                        waveIndex + 1,
                        totalWaves,
                        activeStronghold.GetWaveDisplayName(waveIndex)),
                    fallbackBodyStyle);
                if (plannedTotal > 0)
                {
                    GUILayout.Label(
                        string.Format(Localize("ui.stronghold.remaining_ratio_format", "剩余 {0}/{1}"), remaining, plannedTotal),
                        fallbackBodyStyle);
                }
                else
                {
                    GUILayout.Label(
                        string.Format(Localize("ui.stronghold.remaining_format", "剩余 {0}"), remaining),
                        fallbackBodyStyle);
                }
            }
            else
            {
                GUILayout.Label(Localize("ui.stronghold.info", "信息"));
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Space(4f);
                GUILayout.Label(statusMessage, fallbackBodyStyle);
            }

            GUILayout.EndArea();
        }

        private bool ShouldUseFallbackOverlay()
        {
            if (!useFallbackOverlay)
            {
                return false;
            }

            return titleText == null || waveText == null || remainingText == null || stateText == null;
        }

        private void EnsureFallbackStyles()
        {
            if (fallbackTitleStyle == null)
            {
                fallbackTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fallbackFontSize + 1,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (fallbackBodyStyle == null)
            {
                fallbackBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fallbackFontSize,
                    wordWrap = true,
                    normal = { textColor = new Color(0.9f, 0.95f, 1f, 1f) }
                };
            }
        }

        private void EnsureSequenceController()
        {
            if (sequenceController == null)
            {
                sequenceController = FindObjectOfType<StrongholdSequenceController>();
            }
        }

        private void LogStartupStatus()
        {
            if (!logStartupStatus || startupLogged)
            {
                return;
            }

            startupLogged = true;
            bool fallback = ShouldUseFallbackOverlay();
            Debug.Log($"[UI_StrongholdWavePanel] Startup | sequence={(sequenceController != null)} canvasGroup={(canvasGroup != null)} title={(titleText != null)} wave={(waveText != null)} remaining={(remainingText != null)} state={(stateText != null)} fallback={fallback}");
        }

        private static string Localize(string key, string fallback)
        {
            LocalizationService service = LocalizationService.Instance;
            if (service != null)
            {
                return service.Get(key, fallback);
            }

            return fallback;
        }
    }
}
