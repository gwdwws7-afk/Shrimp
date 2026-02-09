using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 跳跃调试工具 - 实时调整跳跃参数
    /// </summary>
    public class JumpDebugger : MonoBehaviour
    {
        [Header("实时调整（运行时有效）")]
        [Range(1f, 5f)]
        [Tooltip("跳跃高度")]
        public float jumpHeight = 2f;
        
        [Range(1f, 10f)]
        [Tooltip("下落重力倍率（越大下落越快）")]
        public float fallMultiplier = 5f;
        
        [Range(-30f, -5f)]
        [Tooltip("最大下落速度（负数）")]
        public float maxFallSpeed = -20f;
        
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = true;
        
        private PlayerMovement movement;
        private Rigidbody rb;
        
        private void Start()
        {
            movement = GetComponent<PlayerMovement>();
            rb = GetComponent<Rigidbody>();
        }
        
        private void Update()
        {
            if (movement != null)
            {
                // 实时更新参数
                movement.jumpHeight = jumpHeight;
                movement.fallMultiplier = fallMultiplier;
                movement.maxFallSpeed = maxFallSpeed;
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            // 在屏幕左上角显示调试信息
            GUI.Box(new Rect(10, 10, 250, 150), "跳跃调试");
            
            if (rb != null)
            {
                GUI.Label(new Rect(20, 40, 230, 20), $"Y轴速度: {rb.velocity.y:F2}");
                GUI.Label(new Rect(20, 65, 230, 20), $"是否在地面: {movement.IsGrounded}");
                GUI.Label(new Rect(20, 90, 230, 20), $"下落倍率: {fallMultiplier:F1}x");
                GUI.Label(new Rect(20, 115, 230, 20), $"跳跃高度: {jumpHeight:F1}m");
            }
            
            // 快捷按钮
            if (GUI.Button(new Rect(10, 170, 120, 30), "下落更快"))
            {
                fallMultiplier = Mathf.Min(fallMultiplier + 1f, 10f);
            }
            
            if (GUI.Button(new Rect(140, 170, 120, 30), "下落更慢"))
            {
                fallMultiplier = Mathf.Max(fallMultiplier - 1f, 1f);
            }
        }
    }
}
