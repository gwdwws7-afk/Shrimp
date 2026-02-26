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
                titleText.text = "LEVEL COMPLETE!";
            }
            
            if (expText != null)
            {
                expText.text = $"+{expEarned} EXP";
                expText.transform.localScale = Vector3.zero;
            }
            
            if (pearlsText != null)
            {
                pearlsText.text = $"+{pearlsEarned} Pearls";
                pearlsText.transform.localScale = Vector3.zero;
            }
            
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(timeTaken / 60f);
                int seconds = Mathf.FloorToInt(timeTaken % 60f);
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
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
                titleText.text = "LEVEL FAILED";
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
    }
}
