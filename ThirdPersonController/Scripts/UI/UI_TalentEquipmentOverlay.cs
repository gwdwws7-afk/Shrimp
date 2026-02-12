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
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            isOpen = !isOpen;
            if (pauseGameWhenOpen)
            {
                Time.timeScale = isOpen ? 0f : 1f;
            }

            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpen;
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
            GUILayout.Space(6f);
            GUILayout.Label("Progression", HeaderStyle());
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            DrawEquipmentPanel();
            GUILayout.Space(12f);
            DrawTalentPanel();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.Label("Press T to close", SmallStyle());
            GUILayout.EndArea();
        }

        private void DrawEquipmentPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(360));
            GUILayout.Label("Equipment", SectionStyle());

            if (equipment == null)
            {
                GUILayout.Label("No equipment component.");
                GUILayout.EndVertical();
                return;
            }

            equipment.EnsureSlotCount();
            for (int i = 0; i < equipment.equippedPearls.Count; i++)
            {
                PearlItem pearl = equipment.equippedPearls[i];
                string name = pearl != null ? pearl.pearlName : "(Empty)";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Slot {i + 1}: {name}", GUILayout.Width(220));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    selectedSlot = i;
                }

                if (pearl != null && GUILayout.Button("Unequip", GUILayout.Width(70)))
                {
                    equipment.Unequip(i);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            GUILayout.Label($"Inventory (Select slot {selectedSlot + 1})", SectionStyle());

            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(300));
            if (inventory == null || inventory.ownedPearls == null || inventory.ownedPearls.Count == 0)
            {
                GUILayout.Label("No pearls collected.");
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
                    if (GUILayout.Button("Equip", GUILayout.Width(80)))
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
            GUILayout.Label("Talent Tree", SectionStyle());

            if (talentTree == null || talentTree.data == null)
            {
                GUILayout.Label("No talent tree data.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label($"Available Points: {talentTree.availablePoints}");

            talentScroll = GUILayout.BeginScrollView(talentScroll, GUILayout.Height(420));
            DrawTalentBranch("Offense", TalentBranch.Offense);
            DrawTalentBranch("Control", TalentBranch.Control);
            DrawTalentBranch("Survival", TalentBranch.Survival);
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
                    if (GUILayout.Button("Unlock", GUILayout.Width(80)))
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
