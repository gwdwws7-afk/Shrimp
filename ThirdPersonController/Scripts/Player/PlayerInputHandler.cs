using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 玩家输入处理器 - 使用传统 Input 系统
    /// 无需安装 Input System 包
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";
        public string mouseXAxis = "Mouse X";
        public string mouseYAxis = "Mouse Y";
        
        [Header("Key Bindings")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode sprintKey = KeyCode.LeftShift;
        public KeyCode crouchKey = KeyCode.LeftControl;
        public KeyCode attackKey = KeyCode.Mouse0;
        public KeyCode interactKey = KeyCode.E;

        [Header("Cursor Settings")]
        public bool lockCursor = true;

        // 输入状态属性
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintPressed { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool InteractPressed { get; private set; }

        private void Start()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            // 读取移动输入
            float horizontal = Input.GetAxisRaw(horizontalAxis);
            float vertical = Input.GetAxisRaw(verticalAxis);
            MoveInput = new Vector2(horizontal, vertical).normalized;

            // 读取视角输入
            float mouseX = Input.GetAxis(mouseXAxis);
            float mouseY = Input.GetAxis(mouseYAxis);
            LookInput = new Vector2(mouseX, mouseY);

            // 读取按键状态
            JumpPressed = Input.GetKeyDown(jumpKey);
            JumpHeld = Input.GetKey(jumpKey);
            SprintPressed = Input.GetKey(sprintKey);
            CrouchPressed = Input.GetKey(crouchKey);
            AttackPressed = Input.GetKeyDown(attackKey);
            InteractPressed = Input.GetKeyDown(interactKey);

            // 处理光标锁定
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleCursorLock();
            }
        }

        private void ToggleCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
