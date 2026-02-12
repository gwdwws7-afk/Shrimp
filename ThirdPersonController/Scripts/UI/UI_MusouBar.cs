using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    public class UI_MusouBar : MonoBehaviour
    {
        [Header("UI References")]
        public Slider musouSlider;
        public Image fillImage;
        public Text labelText;
        public Text readyText;

        [Header("Colors")]
        public Color normalColor = new Color(0.2f, 0.6f, 1f);
        public Color readyColor = new Color(1f, 0.8f, 0.2f);
        public Color activeColor = new Color(1f, 0.5f, 0.2f);
        public Color fatigueColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("Animation")]
        public bool useSmoothFill = true;
        public float fillSpeed = 8f;
        public bool pulseReady = true;
        public float pulseSpeed = 2f;
        public float readyMinAlpha = 0.35f;

        private float targetFill = 0f;
        private float currentFill = 0f;
        private bool isReady;
        private bool isActive;
        private bool isFatigued;

        private void Start()
        {
            GameEvents.OnMusouChanged += OnMusouChanged;
            GameEvents.OnMusouStateChanged += OnMusouStateChanged;
            GameEvents.OnMusouFatigueStateChanged += OnMusouFatigueStateChanged;

            InitializeState();
        }

        private void OnDestroy()
        {
            GameEvents.OnMusouChanged -= OnMusouChanged;
            GameEvents.OnMusouStateChanged -= OnMusouStateChanged;
            GameEvents.OnMusouFatigueStateChanged -= OnMusouFatigueStateChanged;
        }

        private void Update()
        {
            if (useSmoothFill && musouSlider != null)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * fillSpeed);
                musouSlider.value = currentFill;
            }

            UpdateReadyPulse();
        }

        private void InitializeState()
        {
            PlayerMusouSystem musou = FindObjectOfType<PlayerMusouSystem>();
            if (musou != null)
            {
                ApplyMusouValue(musou.currentMusou, musou.maxMusou);
                isActive = musou.IsActive;
                isFatigued = musou.IsFatigued;
            }
            else
            {
                ApplyMusouValue(0f, 1f);
            }

            UpdateFillColor();
            UpdateReadyVisibility();
        }

        private void ApplyMusouValue(float current, float max)
        {
            float normalized = max <= 0f ? 0f : Mathf.Clamp01(current / max);
            targetFill = normalized;

            if (musouSlider != null)
            {
                if (!useSmoothFill)
                {
                    musouSlider.value = normalized;
                    currentFill = normalized;
                }
                else if (currentFill <= 0f)
                {
                    musouSlider.value = normalized;
                    currentFill = normalized;
                }
            }

            isReady = max > 0f && current >= max;
        }

        private void UpdateReadyPulse()
        {
            if (readyText == null)
            {
                return;
            }

            if (!readyText.gameObject.activeSelf)
            {
                return;
            }

            if (!pulseReady)
            {
                SetReadyAlpha(1f);
                return;
            }

            float alpha = Mathf.Lerp(readyMinAlpha, 1f, Mathf.PingPong(Time.time * pulseSpeed, 1f));
            SetReadyAlpha(alpha);
        }

        private void SetReadyAlpha(float alpha)
        {
            if (readyText == null)
            {
                return;
            }

            Color color = readyText.color;
            color.a = alpha;
            readyText.color = color;
        }

        private void UpdateFillColor()
        {
            if (fillImage == null)
            {
                return;
            }

            if (isActive)
            {
                fillImage.color = activeColor;
                return;
            }

            if (isFatigued)
            {
                fillImage.color = fatigueColor;
                return;
            }

            fillImage.color = isReady ? readyColor : normalColor;
        }

        private void UpdateReadyVisibility()
        {
            if (readyText == null)
            {
                return;
            }

            bool shouldShow = isReady && !isActive;
            readyText.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                SetReadyAlpha(0f);
            }
        }

        private void OnMusouChanged(float current, float max)
        {
            ApplyMusouValue(current, max);
            UpdateFillColor();
            UpdateReadyVisibility();
        }

        private void OnMusouStateChanged(bool active)
        {
            isActive = active;
            UpdateFillColor();
            UpdateReadyVisibility();
        }

        private void OnMusouFatigueStateChanged(bool active)
        {
            isFatigued = active;
            UpdateFillColor();
        }
    }
}
