using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ThirdPersonController
{
    public class UI_LevelComplete : MonoBehaviour
    {
        [Header("References")]
        public GameObject panel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI expText;
        public TextMeshProUGUI pearlsText;
        public TextMeshProUGUI timeText;
        public Button continueButton;
        public Button retryButton;
        
        [Header("Animation")]
        public float appearDuration = 0.5f;
        public float delayBetweenItems = 0.2f;
        
        private System.Action onContinueCallback;
        private System.Action onRetryCallback;
        
        private void Start()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            
            Hide();
        }
        
        public void Show(int expEarned, int pearlsEarned, float timeTaken, System.Action onContinue, System.Action onRetry = null)
        {
            onContinueCallback = onContinue;
            onRetryCallback = onRetry;
            
            if (titleText != null)
            {
                titleText.text = Localize("ui.level.complete_title", "LEVEL COMPLETE!");
            }
            
            if (expText != null)
            {
                expText.text = string.Format(Localize("ui.level.exp_format", "+{0} EXP"), expEarned);
                expText.transform.localScale = Vector3.zero;
            }
            
            if (pearlsText != null)
            {
                pearlsText.text = string.Format(Localize("ui.level.pearls_format", "+{0} Pearls"), pearlsEarned);
                pearlsText.transform.localScale = Vector3.zero;
            }
            
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(timeTaken / 60f);
                int seconds = Mathf.FloorToInt(timeTaken % 60f);
                timeText.text = string.Format(Localize("ui.level.time_format", "Time: {0:00}:{1:00}"), minutes, seconds);
                timeText.transform.localScale = Vector3.zero;
            }
            
            panel.SetActive(true);
            
            AnimateIn();
        }
        
        private void AnimateIn()
        {
            if (expText != null)
            {
                expText.transform.DOScale(1f, appearDuration).SetDelay(delayBetweenItems * 0);
            }
            
            if (pearlsText != null)
            {
                pearlsText.transform.DOScale(1f, appearDuration).SetDelay(delayBetweenItems * 1);
            }
            
            if (timeText != null)
            {
                timeText.transform.DOScale(1f, appearDuration).SetDelay(delayBetweenItems * 2);
            }
        }
        
        private void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        private void OnContinueClicked()
        {
            onContinueCallback?.Invoke();
        }
        
        private void OnRetryClicked()
        {
            onRetryCallback?.Invoke();
        }
        
        public void ShowFailed(System.Action onContinue)
        {
            onContinueCallback = onContinue;
            
            if (titleText != null)
            {
                titleText.text = Localize("ui.level.failed_title", "LEVEL FAILED");
                titleText.color = Color.red;
            }
            
            if (expText != null)
            {
                expText.text = "";
            }
            
            if (pearlsText != null)
            {
                pearlsText.text = "";
            }
            
            panel.SetActive(true);
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
