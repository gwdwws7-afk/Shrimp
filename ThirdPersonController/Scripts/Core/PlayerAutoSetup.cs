using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 自动设置玩家组件 - 一键修复常见问题
    /// </summary>
    [ExecuteInEditMode]
    public class PlayerAutoSetup : MonoBehaviour
    {
        [ContextMenu("自动设置玩家")]
        public void AutoSetup()
        {
            Debug.Log("[PlayerAutoSetup] 开始自动设置玩家...");

            // 1. 检查并设置 Rigidbody
            SetupRigidbody();

            // 2. 检查并设置 Collider
            SetupCollider();

            // 3. 检查并设置 GroundCheck
            SetupGroundCheck();

            // 4. 检查 PlayerMovement 设置
            SetupPlayerMovement();

            // 5. 设置标签
            gameObject.tag = "Player";

            Debug.Log("[PlayerAutoSetup] 设置完成！");
        }

        private void SetupRigidbody()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                Debug.Log("[PlayerAutoSetup] 添加了 Rigidbody");
            }

            rb.mass = 1f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            Debug.Log("[PlayerAutoSetup] Rigidbody 配置完成");
        }

        private void SetupCollider()
        {
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = gameObject.AddComponent<CapsuleCollider>();
                Debug.Log("[PlayerAutoSetup] 添加了 CapsuleCollider");
            }

            capsule.radius = 0.3f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0, 0.9f, 0);
            capsule.direction = 1; // Y轴

            Debug.Log("[PlayerAutoSetup] CapsuleCollider 配置完成");
        }

        private void SetupGroundCheck()
        {
            // 查找是否已有 GroundCheck
            Transform groundCheck = transform.Find("GroundCheck");
            
            if (groundCheck == null)
            {
                // 创建 GroundCheck 物体
                GameObject gc = new GameObject("GroundCheck");
                gc.transform.SetParent(transform);
                gc.transform.localPosition = new Vector3(0, 0.05f, 0); // 脚底位置
                groundCheck = gc.transform;
                Debug.Log("[PlayerAutoSetup] 创建了 GroundCheck 子物体");
            }

            // 设置 PlayerMovement
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null)
            {
                // 使用反射设置私有字段
                var groundCheckField = typeof(PlayerMovement).GetField("groundCheck", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (groundCheckField != null)
                {
                    groundCheckField.SetValue(movement, groundCheck);
                    Debug.Log("[PlayerAutoSetup] GroundCheck 已赋值");
                }

                // 设置地面层（默认第6层）
                var groundLayerField = typeof(PlayerMovement).GetField("groundLayer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (groundLayerField != null)
                {
                    groundLayerField.SetValue(movement, LayerMask.GetMask("Ground"));
                    Debug.Log("[PlayerAutoSetup] GroundLayer 已设置为 Ground 层");
                }
            }
        }

        private void SetupPlayerMovement()
        {
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement == null)
            {
                movement = gameObject.AddComponent<PlayerMovement>();
                Debug.Log("[PlayerAutoSetup] 添加了 PlayerMovement");
            }

            // 检查其他必要组件
            if (GetComponent<PlayerInputHandler>() == null)
            {
                gameObject.AddComponent<PlayerInputHandler>();
                Debug.Log("[PlayerAutoSetup] 添加了 PlayerInputHandler");
            }
        }
    }
}
