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

        public StrongholdSequenceController sequenceController;

        private StrongholdController activeStronghold;
        private float statusTimer;
        private string statusMessage;

        private void OnEnable()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (sequenceController == null)
            {
                sequenceController = FindObjectOfType<StrongholdSequenceController>();
            }

            RefreshActiveStronghold();
        }

        private void OnDisable()
        {
            BindStronghold(null);
        }

        private void Update()
        {
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
                titleText.text = "据点推进";
            }

            if (activeStronghold.TryGetWaveStatus(out int waveIndex, out int totalWaves, out int remaining, out int plannedTotal))
            {
                if (waveText != null)
                {
                    waveText.text = $"波次 {waveIndex + 1}/{totalWaves} · {activeStronghold.GetWaveDisplayName(waveIndex)}";
                }

                if (remainingText != null)
                {
                    if (plannedTotal > 0)
                    {
                        remainingText.text = $"剩余 {remaining}/{plannedTotal}";
                    }
                    else
                    {
                        remainingText.text = $"剩余 {remaining}";
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
            ShowStatus("据点战开始");
        }

        private void HandleWaveStarted(StrongholdController stronghold, int waveIndex)
        {
            ShowStatus($"第 {waveIndex + 1} 波来袭");
        }

        private void HandleWaveCompleted(StrongholdController stronghold, int waveIndex)
        {
            ShowStatus($"第 {waveIndex + 1} 波清除");
        }

        private void HandleStrongholdCompleted(StrongholdController stronghold)
        {
            ShowStatus("据点清除");
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusTimer = statusMessageDuration;
        }
    }
}
