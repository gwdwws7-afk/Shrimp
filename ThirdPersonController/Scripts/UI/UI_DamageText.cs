using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ThirdPersonController
{
    /// <summary>
    /// 伤害数字UI - 浮动显示伤害值
    /// </summary>
    public class UI_DamageText : MonoBehaviour
    {
        [Header("UI引用")]
        public Text damageText;
        public CanvasGroup canvasGroup;
        
        [Header("颜色设置")]
        public Color normalColor = Color.white;
        public Color criticalColor = new Color(1f, 0.5f, 0f);  // 暴击橙色
        public Color playerDamageColor = Color.red;            // 玩家受伤红色
        
        [Header("动画设置")]
        public float floatSpeed = 2f;        // 上升速度
        public float fadeDelay = 0.5f;       // 延迟多久开始淡出
        public float fadeDuration = 0.5f;    // 淡出持续时间
        public float moveRange = 50f;        // 随机移动范围
        
        [Header("暴击设置")]
        public float criticalScale = 1.5f;   // 暴击放大倍数
        public float shakeAmount = 10f;      // 暴击震动幅度
        
        private Camera mainCamera;
        private RectTransform rectTransform;
        private Vector3 worldPosition;
        private bool isInitialized = false;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
            
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
        
        /// <summary>
        /// 初始化伤害数字
        /// </summary>
        public void Initialize(int damage, Vector3 worldPos, bool isCritical = false)
        {
            worldPosition = worldPos;
            isInitialized = true;
            
            // 设置文本
            if (damageText != null)
            {
                damageText.text = damage.ToString();
                damageText.color = isCritical ? criticalColor : normalColor;
                
                // 暴击效果
                if (isCritical)
                {
                    damageText.fontSize = Mathf.RoundToInt(damageText.fontSize * criticalScale);
                    damageText.transform.DOShakePosition(0.3f, shakeAmount, 10, 90);
                }
            }
            
            // 随机偏移
            Vector2 randomOffset = new Vector2(
                Random.Range(-moveRange, moveRange),
                Random.Range(0, moveRange * 0.5f)
            );
            
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += randomOffset;
            }
            
            // 开始动画
            StartCoroutine(AnimateDamageText());
        }
        
        private void Update()
        {
            if (!isInitialized || mainCamera == null) return;
            
            // 跟随世界位置
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
            
            if (rectTransform != null)
            {
                rectTransform.position = screenPos;
            }
        }
        
        private System.Collections.IEnumerator AnimateDamageText()
        {
            // 上升动画
            float elapsed = 0f;
            Vector3 startPos = rectTransform != null ? rectTransform.position : transform.position;
            
            while (elapsed < fadeDelay)
            {
                if (rectTransform != null)
                {
                    rectTransform.position += Vector3.up * floatSpeed * Time.deltaTime;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 淡出动画
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, fadeDuration);
            }
            else if (damageText != null)
            {
                damageText.DOFade(0f, fadeDuration);
            }
            
            yield return new WaitForSeconds(fadeDuration);
            
            // 销毁
            Destroy(gameObject);
        }
    }
}
