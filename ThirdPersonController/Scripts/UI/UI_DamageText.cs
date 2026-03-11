using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

namespace ThirdPersonController
{
    /// <summary>
    /// Floating damage text presenter.
    /// Handles text setup, screen-space tracking, and fade-out lifecycle.
    /// </summary>
    public class UI_DamageText : MonoBehaviour
    {
        [Header("References")]
        public Text damageText;
        public CanvasGroup canvasGroup;

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color criticalColor = new Color(1f, 0.5f, 0f);
        public Color playerDamageColor = Color.red;

        [Header("Animation")]
        public float floatSpeed = 2f;
        public float fadeDelay = 0.5f;
        public float fadeDuration = 0.5f;
        public float moveRange = 50f;

        [Header("Critical")]
        public float criticalScale = 1.5f;
        public float shakeAmount = 10f;

        private Camera mainCamera;
        private RectTransform rectTransform;
        private Vector3 worldPosition;
        private bool isInitialized;
        private Coroutine animationCoroutine;
        private Action<UI_DamageText> onCompleted;
        private int baseFontSize;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (damageText != null)
            {
                baseFontSize = damageText.fontSize;
            }
        }

        public void Initialize(int damage, Vector3 worldPos, bool isCritical = false)
        {
            Initialize(damage, worldPos, isCritical, null);
        }

        public void Initialize(int damage, Vector3 worldPos, bool isCritical, Action<UI_DamageText> completedCallback)
        {
            worldPosition = worldPos;
            isInitialized = true;
            IsPlaying = true;
            onCompleted = completedCallback;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            ResetVisualState();

            if (damageText != null)
            {
                damageText.text = damage.ToString();
                damageText.color = isCritical ? criticalColor : normalColor;

                if (isCritical)
                {
                    int sourceSize = baseFontSize > 0 ? baseFontSize : damageText.fontSize;
                    damageText.fontSize = Mathf.RoundToInt(sourceSize * criticalScale);
                    damageText.transform.DOShakePosition(0.3f, shakeAmount, 10, 90);
                }
            }

            Vector2 randomOffset = new Vector2(
                UnityEngine.Random.Range(-moveRange, moveRange),
                UnityEngine.Random.Range(0f, moveRange * 0.5f));

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = randomOffset;
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(AnimateDamageText());
        }

        private void Update()
        {
            if (!isInitialized || mainCamera == null)
            {
                return;
            }

            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
            if (rectTransform != null)
            {
                rectTransform.position = screenPos;
            }
        }

        private System.Collections.IEnumerator AnimateDamageText()
        {
            float elapsed = 0f;
            while (elapsed < fadeDelay)
            {
                if (rectTransform != null)
                {
                    rectTransform.position += Vector3.up * floatSpeed * Time.deltaTime;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, fadeDuration);
            }
            else if (damageText != null)
            {
                damageText.DOFade(0f, fadeDuration);
            }

            yield return new WaitForSeconds(fadeDuration);

            animationCoroutine = null;
            CompletePresentation();
        }

        private void CompletePresentation()
        {
            isInitialized = false;
            IsPlaying = false;

            Action<UI_DamageText> callback = onCompleted;
            onCompleted = null;

            if (callback != null)
            {
                gameObject.SetActive(false);
                callback.Invoke(this);
                return;
            }

            Destroy(gameObject);
        }

        private void ResetVisualState()
        {
            transform.DOKill();

            if (damageText != null)
            {
                damageText.DOKill();
                damageText.color = new Color(damageText.color.r, damageText.color.g, damageText.color.b, 1f);
                if (baseFontSize > 0)
                {
                    damageText.fontSize = baseFontSize;
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
            }
        }

        private void OnDisable()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            transform.DOKill();
            if (damageText != null)
            {
                damageText.DOKill();
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
            }

            IsPlaying = false;
            isInitialized = false;
        }
    }
}
