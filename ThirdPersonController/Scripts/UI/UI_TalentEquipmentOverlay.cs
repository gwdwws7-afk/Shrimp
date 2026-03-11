using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class UI_TalentEquipmentOverlay : MonoBehaviour
    {
        [Header("References")]
        public TalentTree talentTree;
        public PearlInventory inventory;
        public PearlEquipment equipment;

        [Header("Input")]
        public KeyCode toggleKey = KeyCode.T;
        public bool pauseGameWhenOpen = true;
        public bool allowToggle = true;
        public PlayerInputHandler inputHandler;

        private bool isOpen;
        private int selectedSlot;
        private Vector2 talentScroll;
        private Vector2 inventoryScroll;

        private void Awake()
        {
            if (talentTree == null)
            {
                talentTree = FindObjectOfType<TalentTree>();
            }

            if (inventory == null)
            {
                inventory = FindObjectOfType<PearlInventory>();
            }

            if (equipment == null)
            {
                equipment = FindObjectOfType<PearlEquipment>();
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<PlayerInputHandler>();
            }
        }

        private void Update()
        {
            if (!allowToggle)
            {
                return;
            }

            bool togglePressed = inputHandler != null
                ? inputHandler.WasUnifiedKeyPressedThisFrame(toggleKey)
                : PlayerInputHandler.ReadUnifiedKeyDown(toggleKey);

            if (togglePressed)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            SetOpen(!isOpen, pauseGameWhenOpen);
        }

        public void SetOpen(bool open, bool controlTimeAndCursor)
        {
            isOpen = open;
            if (controlTimeAndCursor)
            {
                Time.timeScale = isOpen ? 0f : 1f;
                Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = isOpen;
            }
        }

        public void DrawEmbedded(Rect panelRect, bool showFooter)
        {
            GUILayout.BeginArea(panelRect);
            DrawPanelContents(showFooter);
            GUILayout.EndArea();
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            float width = 820f;
            float height = 520f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, string.Empty);

            GUILayout.BeginArea(panel);
            DrawPanelContents(true);
            GUILayout.EndArea();
        }

        private void DrawPanelContents(bool showFooter)
        {
            GUILayout.Space(6f);
            GUILayout.Label("成长", HeaderStyle());
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            DrawEquipmentPanel();
            GUILayout.Space(12f);
            DrawTalentPanel();
            GUILayout.EndHorizontal();

            if (showFooter)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("按 T 关闭", SmallStyle());
            }
        }

        private void DrawEquipmentPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(360));
            GUILayout.Label("装备", SectionStyle());

            if (equipment == null)
            {
                GUILayout.Label("未找到装备系统。");
                GUILayout.EndVertical();
                return;
            }

            equipment.EnsureSlotCount();
            for (int i = 0; i < equipment.equippedPearls.Count; i++)
            {
                PearlItem pearl = equipment.equippedPearls[i];
                string name = pearl != null ? pearl.pearlName : "(Empty)";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"槽位 {i + 1}: {name}", GUILayout.Width(220));
                if (GUILayout.Button("选择", GUILayout.Width(60)))
                {
                    selectedSlot = i;
                }

                if (pearl != null && GUILayout.Button("卸下", GUILayout.Width(70)))
                {
                    equipment.Unequip(i);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            GUILayout.Label("背包");

            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(300));
            if (inventory == null || inventory.ownedPearls == null || inventory.ownedPearls.Count == 0)
            {
                GUILayout.Label("暂无珍珠.");
            }
            else
            {
                for (int i = 0; i < inventory.ownedPearls.Count; i++)
                {
                    PearlItem pearl = inventory.ownedPearls[i];
                    if (pearl == null)
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pearl.pearlName, GUILayout.Width(200));
                    if (GUILayout.Button("装备", GUILayout.Width(80)))
                    {
                        equipment.Equip(pearl, selectedSlot);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void DrawTalentPanel()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("天赋");

            if (talentTree == null || talentTree.data == null)
            {
                GUILayout.Label("未找到天赋树数据.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label($"可用点数: {talentTree.availablePoints}");

            talentScroll = GUILayout.BeginScrollView(talentScroll, GUILayout.Height(420));
            DrawTalentBranch("进攻", TalentBranch.Offense);
            DrawTalentBranch("控场", TalentBranch.Control);
            DrawTalentBranch("生存", TalentBranch.Survival);
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void DrawTalentBranch(string title, TalentBranch branch)
        {
            GUILayout.Space(6f);
            GUILayout.Label(title, SectionStyle());

            List<TalentNodeData> nodes = talentTree.data.nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                TalentNodeData node = nodes[i];
                if (node == null || node.branch != branch)
                {
                    continue;
                }

                bool unlocked = talentTree.IsUnlocked(node.id);
                string status = unlocked ? "Unlocked" : "Locked";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{node.title} [{status}]", GUILayout.Width(240));

                if (!unlocked && talentTree.CanUnlock(node.id))
                {
                    if (GUILayout.Button("解锁", GUILayout.Width(80)))
                    {
                        talentTree.Unlock(node.id);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private GUIStyle HeaderStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }

        private GUIStyle SectionStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 1f, 1f, 0.8f) }
            };
        }

        private GUIStyle SmallStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
            };
        }
    }
}
