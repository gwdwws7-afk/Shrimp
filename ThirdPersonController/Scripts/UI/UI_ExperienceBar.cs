using UnityEngine;
using UnityEngine.UI;

namespace ThirdPersonController
{
    public class UI_ExperienceBar : MonoBehaviour
    {
        [Header("UI References")]
        public Slider expSlider;
        public Text levelText;
        public Text expText;

        [Header("Animation")]
        public bool useSmoothFill = true;
        public float fillSpeed = 6f;

        public PlayerExperienceSystem experienceSystem;

        private float targetFill = 0f;
        private float currentFill = 0f;

        private void Start()
        {
            if (experienceSystem == null)
            {
                experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            }

            Refresh(true);
        }

        private void Update()
        {
            if (experienceSystem == null)
            {
                return;
            }

            Refresh(false);

            if (useSmoothFill && expSlider != null)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * fillSpeed);
                expSlider.value = currentFill;
            }
        }

        private void Refresh(bool force)
        {
            int expToNext = Mathf.Max(1, experienceSystem.ExpToNext);
            targetFill = Mathf.Clamp01((float)experienceSystem.currentExp / expToNext);

            if (expSlider != null && (!useSmoothFill || force))
            {
                expSlider.value = targetFill;
                currentFill = targetFill;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv {experienceSystem.level}";
            }

            if (expText != null)
            {
                expText.text = $"{experienceSystem.currentExp}/{expToNext}";
            }
        }
    }
}
