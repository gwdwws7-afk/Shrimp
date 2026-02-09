using UnityEngine;

namespace ThirdPersonController
{
    /// <summary>
    /// 相机设置助手 - 自动配置第三人称相机
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
            // 如果没有指定目标，自动查找
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogError("[CameraSetupHelper] 未找到玩家对象！请设置 Player 标签或手动指定目标。");
                    return;
                }
                playerTarget = player.transform;
            }

            // 获取或添加 PlayerCamera 组件
            PlayerCamera playerCamera = GetComponent<PlayerCamera>();
            if (playerCamera == null)
            {
                playerCamera = gameObject.AddComponent<PlayerCamera>();
            }

            // 配置参数
            playerCamera.target = playerTarget;
            playerCamera.offset = offset;
            playerCamera.mouseSensitivity = mouseSensitivity;
            playerCamera.defaultDistance = defaultDistance;
            playerCamera.lockCursor = true;

            Debug.Log($"[CameraSetupHelper] 相机已配置完成，目标: {playerTarget.name}");
        }
    }
}
