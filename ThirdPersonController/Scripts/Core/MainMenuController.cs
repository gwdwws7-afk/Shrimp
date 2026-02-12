using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Flow")]
        public string startSceneName = "SampleScene";
        public bool loadSaveOnStart = true;

        [Header("Input")]
        public KeyCode startKey = KeyCode.Return;
        public KeyCode quitKey = KeyCode.Escape;

        [Header("UI")]
        public string titleText = "Abyss Warriors";
        public string subtitleText = "Press Enter to Start";
        public bool showSubtitle = true;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetupStyles();
        }

        private void Update()
        {
            if (Input.GetKeyDown(startKey))
            {
                StartGame();
            }

            if (Input.GetKeyDown(quitKey))
            {
                QuitGame();
            }
        }

        private void OnGUI()
        {
            if (titleStyle == null)
            {
                SetupStyles();
            }

            float panelWidth = 420f;
            float panelHeight = 260f;
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Box(panelRect, string.Empty);

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(10f);
            GUILayout.Label(titleText, titleStyle);

            if (showSubtitle)
            {
                GUILayout.Space(8f);
                GUILayout.Label(subtitleText, subtitleStyle);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Start Game", buttonStyle))
            {
                StartGame();
            }

            if (GUILayout.Button("Quit", buttonStyle))
            {
                QuitGame();
            }

            GUILayout.Space(10f);
            GUILayout.EndArea();
        }

        private void StartGame()
        {
            if (loadSaveOnStart && SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }

            SceneManager.LoadScene(startSceneName);
        }

        private void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            Debug.Log("QuitGame called (Editor)");
#endif
        }

        private void SetupStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.7f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fixedHeight = 42f
            };
        }
    }
}
