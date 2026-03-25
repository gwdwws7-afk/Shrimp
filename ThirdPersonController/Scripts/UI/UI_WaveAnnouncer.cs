using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ThirdPersonController
{
    public class UI_WaveAnnouncer : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI waveTitleText;
        public TextMeshProUGUI waveNumberText;
        public CanvasGroup canvasGroup;
        
        [Header("Settings")]
        public float displayDuration = 3f;
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 0.5f;
        
        [Header("Colors")]
        public Color normalWaveColor = Color.white;
        public Color eliteWaveColor = new Color(1f, 0.5f, 0f);
        public Color bossWaveColor = Color.red;
        
        private void OnEnable()
        {
            StrongholdController[] controllers = FindObjectsOfType<StrongholdController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i].OnWaveStarted += HandleWaveStarted;
            }
        }
        
        private void OnDisable()
        {
            StrongholdController[] controllers = FindObjectsOfType<StrongholdController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i].OnWaveStarted -= HandleWaveStarted;
            }
        }
        
        private void HandleWaveStarted(StrongholdController controller, int waveIndex)
        {
            ShowWaveAnnouncement(controller, waveIndex);
        }
        
        public void ShowWaveAnnouncement(StrongholdController controller, int waveIndex)
        {
            if (controller == null || waveIndex < 0 || waveIndex >= controller.waves.Count)
            {
                return;
            }
            
            StrongholdWave wave = controller.waves[waveIndex];
            
            if (waveTitleText != null)
            {
                if (!string.IsNullOrWhiteSpace(wave.name))
                {
                    waveTitleText.text = wave.name;
                }
                else
                {
                    waveTitleText.text = string.Format(
                        Localize("ui.wave.announcer.default_wave_title_format", "Wave {0}"),
                        waveIndex + 1);
                }
            }
            
            if (waveNumberText != null)
            {
                waveNumberText.text = string.Format(
                    Localize("ui.wave.announcer.wave_format", "WAVE {0}"),
                    waveIndex + 1);
            }
            
            Color waveColor = normalWaveColor;
            
            if (wave.eliteTrigger != null && wave.eliteTrigger.enabled)
            {
                waveColor = eliteWaveColor;
            }
            
            if (waveTitleText != null)
            {
                waveTitleText.color = waveColor;
            }
            
            if (waveNumberText != null)
            {
                waveNumberText.color = waveColor;
            }
            
            Show();
        }
        
        public void ShowBossAnnouncement(string bossName)
        {
            if (waveTitleText != null)
            {
                waveTitleText.text = string.IsNullOrWhiteSpace(bossName)
                    ? Localize("ui.wave.announcer.boss_default", "Boss Encounter")
                    : bossName;
                waveTitleText.color = bossWaveColor;
            }
            
            if (waveNumberText != null)
            {
                waveNumberText.text = Localize("ui.wave.announcer.boss_tag", "BOSS");
                waveNumberText.color = bossWaveColor;
            }
            
            Show();
        }
        
        private void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeInDuration);
                
                DOVirtual.DelayedCall(displayDuration, () =>
                {
                    canvasGroup.DOFade(0f, fadeOutDuration);
                });
            }
        }
        
        public void ShowCustomAnnouncement(string title, string subtitle, Color color)
        {
            if (waveTitleText != null)
            {
                waveTitleText.text = title;
                waveTitleText.color = color;
            }
            
            if (waveNumberText != null)
            {
                waveNumberText.text = subtitle;
                waveNumberText.color = color;
            }
            
            Show();
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
