using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 快速为玩家对象补齐运行所需的基础组件与关键引用。
    /// </summary>
    [ExecuteInEditMode]
    public class PlayerAutoSetup : MonoBehaviour
    {
        [ContextMenu("自动设置玩家")]
        public void AutoSetup()
        {
            Debug.Log("[PlayerAutoSetup] 开始自动配置玩家组件。");

            SetupRigidbody();
            SetupCollider();
            SetupGroundCheck();
            SetupPlayerMovement();

            gameObject.tag = "Player";
            Debug.Log("[PlayerAutoSetup] 自动配置完成。");
        }

        private void SetupRigidbody()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                Debug.Log("[PlayerAutoSetup] 已添加 Rigidbody。");
            }

            rb.mass = 1f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            Debug.Log("[PlayerAutoSetup] Rigidbody 参数配置完成。");
        }

        private void SetupCollider()
        {
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = gameObject.AddComponent<CapsuleCollider>();
                Debug.Log("[PlayerAutoSetup] 已添加 CapsuleCollider。");
            }

            capsule.radius = 0.3f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.direction = 1;
            Debug.Log("[PlayerAutoSetup] CapsuleCollider 参数配置完成。");
        }

        private void SetupGroundCheck()
        {
            Transform groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                GameObject groundCheckObject = new GameObject("GroundCheck");
                groundCheckObject.transform.SetParent(transform);
                groundCheckObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                groundCheck = groundCheckObject.transform;
                Debug.Log("[PlayerAutoSetup] 已创建 GroundCheck 子节点。");
            }

            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement == null)
            {
                return;
            }

            var groundCheckField = typeof(PlayerMovement).GetField(
                "groundCheck",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (groundCheckField != null)
            {
                groundCheckField.SetValue(movement, groundCheck);
                Debug.Log("[PlayerAutoSetup] 已写入 PlayerMovement.groundCheck。");
            }

            var groundLayerField = typeof(PlayerMovement).GetField(
                "groundLayer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (groundLayerField != null)
            {
                groundLayerField.SetValue(movement, LayerMask.GetMask("Ground"));
                Debug.Log("[PlayerAutoSetup] 已写入 PlayerMovement.groundLayer。");
            }
        }

        private void SetupPlayerMovement()
        {
            if (GetComponent<PlayerMovement>() == null)
            {
                gameObject.AddComponent<PlayerMovement>();
                Debug.Log("[PlayerAutoSetup] 已添加 PlayerMovement。");
            }

            if (GetComponent<PlayerInputHandler>() == null)
            {
                gameObject.AddComponent<PlayerInputHandler>();
                Debug.Log("[PlayerAutoSetup] 已添加 PlayerInputHandler。");
            }
        }
    }
}
