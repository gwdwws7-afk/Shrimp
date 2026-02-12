using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// UI管理器 - 单例模式
    /// 管理所有UI面板的显示/隐藏和交互
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("HUD面板")]
        public GameObject hudPanel;           // 主HUD（战斗中显示）
        public GameObject pausePanel;         // 暂停菜单
        public GameObject gameOverPanel;      // 游戏结束
        public GameObject victoryPanel;       // 胜利界面
        
        [Header("UI组件")]
        public UI_HPBar hpBar;
        public UI_StaminaBar staminaBar;
        public UI_MusouBar musouBar;
        public UI_ComboCounter comboCounter;
        public UI_SkillBar skillBar;
        
        [Header("浮动元素")]
        public Transform damageTextParent;    // 伤害数字父物体
        public GameObject damageTextPrefab;   // 伤害数字预制体
        
        // UI状态
        private bool isPaused = false;
        private Stack<GameObject> uiStack = new Stack<GameObject>();  // UI层级栈

        private string toastMessage = string.Empty;
        private float toastTimer = 0f;
        private float toastDuration = 0f;
        
        // 事件
        public System.Action<bool> OnPauseStateChanged;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            
            // 初始化所有UI
            InitializeUI();
            
            // 订阅事件
            SubscribeToEvents();
        }
        
        private void InitializeUI()
        {
            // 确保HUD显示
            ShowHUD(true);
            
            // 隐藏其他面板
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }
        
        private void SubscribeToEvents()
        {
            // 游戏暂停事件
            GameEvents.OnGamePaused += OnGamePaused;
            
            // 玩家死亡
            GameEvents.OnPlayerDeath += OnPlayerDeath;
            
            // 游戏结束
            GameEvents.OnGameOver += OnGameOver;
            
            // 显示伤害数字
            GameEvents.OnShowDamageText += ShowDamageText;
            
            // 显示消息
            GameEvents.OnShowMessage += ShowMessage;
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // 取消订阅
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnGameOver -= OnGameOver;
            GameEvents.OnShowDamageText -= ShowDamageText;
            GameEvents.OnShowMessage -= ShowMessage;
        }
        
        #region 面板控制
        
        /// <summary>
        /// 显示/隐藏HUD
        /// </summary>
        public void ShowHUD(bool show)
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(show);
            }
        }
        
        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            isPaused = !isPaused;
            
            if (isPaused)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                uiStack.Push(pausePanel);
            }
            
            // 解锁光标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            OnPauseStateChanged?.Invoke(true);
            GameEvents.GamePaused(true);
        }
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            
            // 关闭所有弹出的面板
            while (uiStack.Count > 0)
            {
                GameObject panel = uiStack.Pop();
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
            
            // 锁定光标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            OnPauseStateChanged?.Invoke(false);
            GameEvents.GamePaused(false);
        }
        
        /// <summary>
        /// 关闭当前面板（返回上一层）
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (uiStack.Count > 0)
            {
                GameObject panel = uiStack.Pop();
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
            
            // 如果栈空了，恢复游戏
            if (uiStack.Count == 0 && isPaused)
            {
                ResumeGame();
            }
        }
        
        /// <summary>
        /// 打开面板
        /// </summary>
        public void OpenPanel(GameObject panel)
        {
            if (panel != null)
            {
                panel.SetActive(true);
                uiStack.Push(panel);
            }
        }
        
        #endregion
        
        #region 事件处理
        
        private void OnGamePaused(bool paused)
        {
            // 可由外部调用触发
        }
        
        private void OnPlayerDeath()
        {
            ShowHUD(false);
            
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            
            // 解锁光标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void OnGameOver(bool isVictory)
        {
            ShowHUD(false);
            
            if (isVictory && victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
            else if (!isVictory && gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        #endregion
        
        #region 伤害数字
        
        /// <summary>
        /// 显示伤害数字
        /// </summary>
        private void ShowDamageText(int damage, Vector3 worldPosition, bool isCritical)
        {
            if (damageTextPrefab == null || damageTextParent == null) return;
            
            // 创建伤害数字
            GameObject damageTextObj = Instantiate(damageTextPrefab, damageTextParent);
            UI_DamageText damageText = damageTextObj.GetComponent<UI_DamageText>();
            
            if (damageText != null)
            {
                damageText.Initialize(damage, worldPosition, isCritical);
            }
        }
        
        /// <summary>
        /// 显示伤害数字（便捷方法）
        /// </summary>
        public void ShowDamage(int damage, Vector3 worldPosition, bool isCritical = false)
        {
            ShowDamageText(damage, worldPosition, isCritical);
        }
        
        #endregion
        
        #region 消息提示
        
        /// <summary>
        /// 显示消息
        /// </summary>
        private void ShowMessage(string message, float duration)
        {
            toastMessage = message;
            toastDuration = Mathf.Max(0.1f, duration);
            toastTimer = toastDuration;
        }

        private void OnGUI()
        {
            if (toastTimer <= 0f || string.IsNullOrEmpty(toastMessage))
            {
                return;
            }

            toastTimer -= Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(toastTimer / toastDuration);
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 1f, 1f, alpha) }
            };

            Rect rect = new Rect(0, 20, Screen.width, 30);
            GUI.Label(rect, toastMessage, style);
        }
        
        #endregion
        
        #region 更新HUD
        
        /// <summary>
        /// 更新血条
        /// </summary>
        public void UpdateHPBar(float current, float max)
        {
            if (hpBar != null)
            {
                hpBar.UpdateHP(current, max);
            }
        }
        
        /// <summary>
        /// 更新耐力条
        /// </summary>
        public void UpdateStaminaBar(float current, float max)
        {
            if (staminaBar != null)
            {
                staminaBar.UpdateStamina(current, max);
            }
        }
        
        /// <summary>
        /// 更新连击
        /// </summary>
        public void UpdateCombo(int combo)
        {
            if (comboCounter != null)
            {
                comboCounter.UpdateCombo(combo);
            }
        }
        
        #endregion
        
        #region 属性
        
        public bool IsPaused => isPaused;
        
        #endregion
    }
}
