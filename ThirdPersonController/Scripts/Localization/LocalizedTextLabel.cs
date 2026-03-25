using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ThirdPersonController
{
    [DisallowMultipleComponent]
    public class LocalizedTextLabel : MonoBehaviour
    {
        public string key;
        [TextArea] public string fallbackText;
        public Text uiText;
        public TMP_Text tmpText;

        private void Awake()
        {
            if (uiText == null)
            {
                uiText = GetComponent<Text>();
            }

            if (tmpText == null)
            {
                tmpText = GetComponent<TMP_Text>();
            }
        }

        private void OnEnable()
        {
            LocalizationService service = LocalizationService.Instance;
            if (service != null)
            {
                service.OnLanguageChanged += HandleLanguageChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            LocalizationService service = LocalizationService.Instance;
            if (service != null)
            {
                service.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        public void Refresh()
        {
            string text = fallbackText;
            LocalizationService service = LocalizationService.Instance;
            if (service != null)
            {
                text = service.Get(key, fallbackText);
            }

            if (uiText != null)
            {
                uiText.text = text;
            }

            if (tmpText != null)
            {
                tmpText.text = text;
            }
        }

        private void HandleLanguageChanged(LocalizationLanguage _)
        {
            Refresh();
        }
    }
}
