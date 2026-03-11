using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 统一管理 HUD、面板栈、飘字对象池与全局 UI 事件。
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("设置")]
        public GameObject hudPanel;           // 常驻战斗 HUD 面板
        public GameObject pausePanel; // UI 引用，用于驱动界面表现与信息同步。
        public GameObject gameOverPanel; // UI 引用，用于驱动界面表现与信息同步。
        public GameObject victoryPanel; // UI 引用，用于驱动界面表现与信息同步。

        [Header("设置")]
        public UI_HPBar hpBar;
        public UI_StaminaBar staminaBar;
        public UI_MusouBar musouBar;
        public UI_ExperienceBar experienceBar;
        public UI_StrongholdWavePanel strongholdWavePanel;
        public UI_ComboCounter comboCounter;
        public UI_SkillBar skillBar;

        [Header("伤害数字")]
        public Transform damageTextParent; // UI 引用，用于驱动界面表现与信息同步。
        public GameObject damageTextPrefab; // UI 引用，用于驱动界面表现与信息同步。
        [Header("设置")]
        public int initialDamageTextPoolSize = 24;
        public int maxDamageTextPoolSize = 96;

        // 当前暂停状态，驱动时间缩放与输入锁定逻辑。
        private bool isPaused = false;
        private Stack<GameObject> uiStack = new Stack<GameObject>(); // UI 面板栈管理，用于维持信息可读性与界面层次。
        private readonly Queue<UI_DamageText> damageTextPool = new Queue<UI_DamageText>();
        private readonly HashSet<UI_DamageText> activeDamageTexts = new HashSet<UI_DamageText>();
        private bool damageTextPoolInitialized = false;

        private string toastMessage = string.Empty;
        private float toastTimer = 0f;
        private float toastDuration = 0f;

        // 暂停状态变化广播（true = 暂停，false = 恢复）。
        public System.Action<bool> OnPauseStateChanged;

        protected override void OnAwake()
        {
            base.OnAwake();

            // 初始化 UI 默认显隐状态与对象池。
            InitializeUI();

// 订阅全局 UI 事件，用于模块解耦并同步关键节点。
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            // 开局默认展示 HUD。
            ShowHUD(true);

            // 非战斗常驻面板默认隐藏。
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);

            InitializeDamageTextPool();
        }

        private void SubscribeToEvents()
        {
// 监听全局暂停事件，用于模块解耦并同步关键节点。
            GameEvents.OnGamePaused += OnGamePaused;

// 监听玩家死亡事件，用于模块解耦并同步关键节点。
            GameEvents.OnPlayerDeath += OnPlayerDeath;

            // 监听关卡结算事件（胜利/失败）。
            GameEvents.OnGameOver += OnGameOver;

// 监听伤害飘字事件，用于模块解耦并同步关键节点。
            GameEvents.OnShowDamageText += ShowDamageText;

            // 监听短提示消息事件。
            GameEvents.OnShowMessage += ShowMessage;
        }

        protected override void OnDestroy()
        {
            // 反注册事件，避免对象销毁后被继续回调。
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnGameOver -= OnGameOver;
            GameEvents.OnShowDamageText -= ShowDamageText;
            GameEvents.OnShowMessage -= ShowMessage;

            ClearDamageTextPool();

            base.OnDestroy();
        }

        #region 面板控制

        /// <summary>
        /// 执行 Show HUD 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void ShowHUD(bool show)
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(show);
            }
        }

        /// <summary>
        /// 切换Pause，在双态之间做一致性变更。
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
        /// 暂停游戏并打开暂停面板。
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

            // 暂停时释放鼠标，便于操作菜单。
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnPauseStateChanged?.Invoke(true);
            GameEvents.GamePaused(true);
        }

        /// <summary>
        /// 恢复游戏并关闭栈顶面板。
        /// </summary>
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            // 恢复时关闭栈中所有已打开面板。
            while (uiStack.Count > 0)
            {
                GameObject panel = uiStack.Pop();
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }

            // 恢复锁定鼠标并隐藏光标，回到战斗输入。
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnPauseStateChanged?.Invoke(false);
            GameEvents.GamePaused(false);
        }

        /// <summary>
        /// 执行 Close Current Panel 相关逻辑，并保证模块状态与外部调用约定一致。
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

            // 关闭后若无面板且仍处于暂停，则自动恢复游戏。
            if (uiStack.Count == 0 && isPaused)
            {
                ResumeGame();
            }
        }

        /// <summary>
        /// 打开并入栈 UI 面板。
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

        #region 事件回调

        private void OnGamePaused(bool paused)
        {
            // 暂停状态已在 PauseGame/ResumeGame 内部处理，这里保留兼容入口。
        }

        private void OnPlayerDeath()
        {
            ShowHUD(false);

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            // 死亡后释放鼠标，便于操作结算界面。
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
        /// 显示世界坐标伤害数字。
        /// </summary>
        private void ShowDamageText(int damage, Vector3 worldPosition, bool isCritical)
        {
            if (damageTextPrefab == null || damageTextParent == null) return;

            UI_DamageText damageText = GetDamageTextFromPool();
            if (damageText != null)
            {
                activeDamageTexts.Add(damageText);
                damageText.gameObject.SetActive(true);
                damageText.transform.SetAsLastSibling();
                damageText.Initialize(damage, worldPosition, isCritical, ReturnDamageTextToPool);
            }
        }

        /// <summary>
        /// 外部调用的伤害数字接口。
        /// </summary>
        public void ShowDamage(int damage, Vector3 worldPosition, bool isCritical = false)
        {
            ShowDamageText(damage, worldPosition, isCritical);
        }

        #endregion

        #region 顶部提示

        /// <summary>
        /// 执行 Show Message 相关逻辑，并保证模块状态与外部调用约定一致。
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

        #region 对象池
        private void InitializeDamageTextPool()
        {
            if (damageTextPoolInitialized)
            {
                return;
            }

            damageTextPoolInitialized = true;

            if (damageTextPrefab == null || damageTextParent == null)
            {
                return;
            }

            int initialCount = Mathf.Max(0, initialDamageTextPoolSize);
            for (int i = 0; i < initialCount; i++)
            {
                UI_DamageText item = CreateDamageTextItem();
                if (item == null)
                {
                    break;
                }

                damageTextPool.Enqueue(item);
            }
        }

        private UI_DamageText GetDamageTextFromPool()
        {
            if (!damageTextPoolInitialized)
            {
                InitializeDamageTextPool();
            }

            while (damageTextPool.Count > 0)
            {
                UI_DamageText pooled = damageTextPool.Dequeue();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            int maxPool = Mathf.Max(0, maxDamageTextPoolSize);
            int totalTracked = damageTextPool.Count + activeDamageTexts.Count;
            if (maxPool == 0 || totalTracked < maxPool)
            {
                return CreateDamageTextItem();
            }

            return null;
        }

        private UI_DamageText CreateDamageTextItem()
        {
            if (damageTextPrefab == null || damageTextParent == null)
            {
                return null;
            }

            GameObject damageTextObj = Instantiate(damageTextPrefab, damageTextParent);
            UI_DamageText damageText = damageTextObj.GetComponent<UI_DamageText>();
            if (damageText == null)
            {
                Destroy(damageTextObj);
                return null;
            }

            damageTextObj.SetActive(false);
            return damageText;
        }

        private void ReturnDamageTextToPool(UI_DamageText damageText)
        {
            if (damageText == null)
            {
                return;
            }

            activeDamageTexts.Remove(damageText);
            if (!damageTextPoolInitialized)
            {
                Destroy(damageText.gameObject);
                return;
            }

            int maxPool = Mathf.Max(0, maxDamageTextPoolSize);
            int totalTracked = damageTextPool.Count + activeDamageTexts.Count;
            if (maxPool > 0 && totalTracked >= maxPool)
            {
                Destroy(damageText.gameObject);
                return;
            }

            damageTextPool.Enqueue(damageText);
        }

        private void ClearDamageTextPool()
        {
            if (activeDamageTexts.Count > 0)
            {
                List<UI_DamageText> activeSnapshot = new List<UI_DamageText>(activeDamageTexts);
                activeDamageTexts.Clear();
                for (int i = 0; i < activeSnapshot.Count; i++)
                {
                    if (activeSnapshot[i] != null)
                    {
                        Destroy(activeSnapshot[i].gameObject);
                    }
                }
            }

            while (damageTextPool.Count > 0)
            {
                UI_DamageText pooled = damageTextPool.Dequeue();
                if (pooled != null)
                {
                    Destroy(pooled.gameObject);
                }
            }
        }

        #endregion

        #region 外部接口

        /// <summary>
        /// 更新HPBar，保持显示与运行数据一致。
        /// </summary>
        public void UpdateHPBar(float current, float max)
        {
            if (hpBar != null)
            {
                hpBar.UpdateHP(current, max);
            }
        }

        /// <summary>
        /// 更新Stamina Bar，保持显示与运行数据一致。
        /// </summary>
        public void UpdateStaminaBar(float current, float max)
        {
            if (staminaBar != null)
            {
                staminaBar.UpdateStamina(current, max);
            }
        }

        /// <summary>
        /// 更新Combo，保持显示与运行数据一致。
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
