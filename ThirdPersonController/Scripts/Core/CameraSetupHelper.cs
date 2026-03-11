using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// CameraSetupHelper 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class CameraSetupHelper : MonoBehaviour
    {
        [Header("目标玩家")]
        public Transform playerTarget;
        
        [Header("相机参数")]
        public Vector3 offset = new Vector3(0, 1.5f, 0);
        public float mouseSensitivity = 3f;
        public float defaultDistance = 5f;

        private void Start()
        {
            SetupCamera();
        }

        private void SetupCamera()
        {
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogError("[CameraSetupHelper] 未找到 Player 标签对象，无法配置相机目标。");
                    return;
                }
                playerTarget = player.transform;
            }

// 按需获取组件引用，减少初始化顺序耦合。
            PlayerCamera playerCamera = GetComponent<PlayerCamera>();
            if (playerCamera == null)
            {
                playerCamera = gameObject.AddComponent<PlayerCamera>();
            }

// 围绕 镜头 执行该步骤，用于保持上下文语义一致。
            playerCamera.target = playerTarget;
            playerCamera.offset = offset;
            playerCamera.mouseSensitivity = mouseSensitivity;
            playerCamera.defaultDistance = defaultDistance;
            playerCamera.lockCursor = true;

            Debug.Log($"[CameraSetupHelper] 相机已配置完成，目标: {playerTarget.name}");
        }
    }
}
