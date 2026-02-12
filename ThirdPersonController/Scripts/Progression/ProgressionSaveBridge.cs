using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class ProgressionSaveBridge : MonoBehaviour
    {
        [Header("References")]
        public PearlDatabase pearlDatabase;
        public PearlInventory inventory;
        public PearlEquipment equipment;
        public TalentTree talentTree;
        public PlayerExperienceSystem experienceSystem;

        [Header("Behavior")]
        public bool applyOnStart = true;
        public bool autoSaveOnChange = true;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<PearlInventory>();
            }

            if (equipment == null)
            {
                equipment = GetComponent<PearlEquipment>();
            }

            if (talentTree == null)
            {
                talentTree = GetComponent<TalentTree>();
            }

            if (experienceSystem == null)
            {
                experienceSystem = GetComponent<PlayerExperienceSystem>();
            }
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged += SaveProgression;
            }

            if (equipment != null)
            {
                equipment.OnEquipmentChanged += SaveProgression;
            }

            if (talentTree != null)
            {
                talentTree.OnTalentChanged += SaveProgression;
            }
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyProgression();
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= SaveProgression;
            }

            if (equipment != null)
            {
                equipment.OnEquipmentChanged -= SaveProgression;
            }

            if (talentTree != null)
            {
                talentTree.OnTalentChanged -= SaveProgression;
            }
        }

        public void ApplyProgression()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            GameData data = SaveManager.Instance.CurrentData;
            bool shouldSaveDefaults = false;

            if (talentTree != null)
            {
                if (data.unlockedTalentNodes != null && data.unlockedTalentNodes.Count > 0)
                {
                    talentTree.unlockedNodes = new List<string>(data.unlockedTalentNodes);
                }
                else
                {
                    shouldSaveDefaults = true;
                }

                if (data.talentPoints >= 0)
                {
                    talentTree.availablePoints = data.talentPoints;
                }

                talentTree.NotifyChanged();
            }

            if (inventory != null && data.ownedPearlIds != null && data.ownedPearlIds.Count > 0)
            {
                inventory.ownedPearls.Clear();
                for (int i = 0; i < data.ownedPearlIds.Count; i++)
                {
                    PearlItem pearl = ResolvePearl(data.ownedPearlIds[i]);
                    if (pearl != null)
                    {
                        inventory.ownedPearls.Add(pearl);
                    }
                }

                inventory.NotifyChanged();
            }
            else if (inventory != null)
            {
                shouldSaveDefaults = true;
            }

            if (equipment != null && data.equippedPearlIds != null && data.equippedPearlIds.Count > 0)
            {
                equipment.EnsureSlotCount();
                int count = Mathf.Min(equipment.equippedPearls.Count, data.equippedPearlIds.Count);
                for (int i = 0; i < count; i++)
                {
                    equipment.equippedPearls[i] = ResolvePearl(data.equippedPearlIds[i]);
                }

                equipment.NotifyChanged();
            }
            else if (equipment != null)
            {
                shouldSaveDefaults = true;
            }

            if (shouldSaveDefaults)
            {
                SaveProgression();
            }

            if (experienceSystem != null)
            {
                experienceSystem.level = Mathf.Max(1, data.playerLevel);
                experienceSystem.currentExp = Mathf.Max(0, data.currentExp);
            }
        }

        public void SaveProgression()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null)
            {
                return;
            }

            GameData data = SaveManager.Instance.CurrentData;

            if (talentTree != null)
            {
                data.talentPoints = talentTree.availablePoints;
                data.unlockedTalentNodes = new List<string>(talentTree.unlockedNodes);
            }

            if (inventory != null)
            {
                data.ownedPearlIds = new List<string>();
                for (int i = 0; i < inventory.ownedPearls.Count; i++)
                {
                    PearlItem pearl = inventory.ownedPearls[i];
                    if (pearl != null)
                    {
                        data.ownedPearlIds.Add(pearl.GetId());
                    }
                }
            }

            if (equipment != null)
            {
                data.equippedPearlIds = new List<string>();
                equipment.EnsureSlotCount();
                for (int i = 0; i < equipment.equippedPearls.Count; i++)
                {
                    PearlItem pearl = equipment.equippedPearls[i];
                    data.equippedPearlIds.Add(pearl != null ? pearl.GetId() : string.Empty);
                }
            }

            if (experienceSystem != null)
            {
                data.playerLevel = experienceSystem.level;
                data.currentExp = experienceSystem.currentExp;
            }

            if (autoSaveOnChange)
            {
                SaveManager.Instance.SaveGame();
            }
        }

        private PearlItem ResolvePearl(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (pearlDatabase != null)
            {
                return pearlDatabase.GetPearlById(id);
            }

            return null;
        }
    }
}
