using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonController
{
    public class LevelFlowController : MonoBehaviour
    {
        [Header("Scene Flow")]
        public string mainMenuSceneName = "MainMenu";
        public int levelId = 1;

        [Header("Input")]
        public KeyCode menuKey = KeyCode.Escape;

        [Header("UI")]
        public string levelTitle = "Sample Level";
        public bool showOverlay = true;

        [Header("Lighting")]
        public bool ensureLightingOnStart = true;
        public float fallbackLightIntensity = 1f;

        private bool menuOpen;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;

        private void Start()
        {
            GameEvents.LevelStarted(levelId);
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.currentLevel = levelId;
            }

            if (ensureLightingOnStart)
            {
                EnsureLighting();
            }
            SetupStyles();
        }

        private void Update()
        {
            if (Input.GetKeyDown(menuKey))
            {
                ToggleMenu();
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || !menuOpen)
            {
                return;
            }

            if (titleStyle == null)
            {
                SetupStyles();
            }

            float panelWidth = 360f;
            float panelHeight = 220f;
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Box(panelRect, string.Empty);
            GUILayout.BeginArea(panelRect);
            GUILayout.Space(10f);
            GUILayout.Label(levelTitle, titleStyle);
            GUILayout.Space(20f);

            if (GUILayout.Button("Resume", buttonStyle))
            {
                CloseMenu();
            }

            if (GUILayout.Button("Exit to Main Menu", buttonStyle))
            {
                ExitToMenu();
            }

            GUILayout.EndArea();
        }

        private void ToggleMenu()
        {
            if (menuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        private void OpenMenu()
        {
            menuOpen = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CloseMenu()
        {
            menuOpen = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ExitToMenu()
        {
            menuOpen = false;
            Time.timeScale = 1f;
            GameEvents.LevelCompleted(levelId);
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void SetupStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fixedHeight = 40f
            };
        }

        private void EnsureLighting()
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null && lights[i].type == LightType.Directional)
                    {
                        sun = lights[i];
                        break;
                    }
                }
            }

            if (sun == null)
            {
                GameObject lightObj = new GameObject("Directional Light (Auto)");
                sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = fallbackLightIntensity;
                lightObj.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            }

            if (sun != null)
            {
                sun.enabled = true;
                RenderSettings.sun = sun;
            }

            if (RenderSettings.skybox == null)
            {
                RenderSettings.skybox = Resources.GetBuiltinResource<Material>("Default-Skybox.mat");
            }

            DynamicGI.UpdateEnvironment();
        }
    }
}
