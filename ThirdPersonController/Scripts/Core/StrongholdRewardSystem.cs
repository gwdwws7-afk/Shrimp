using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class StrongholdRewardSystem : MonoBehaviour
    {
        [Header("Targets")]
        public StrongholdSequenceController sequenceController;
        public List<StrongholdController> strongholds = new List<StrongholdController>();

        [Header("Rewards")]
        public int expOnWaveComplete = 25;
        public int expOnStrongholdClear = 80;
        public int talentPointsOnClear = 1;
        public bool grantPearlOnClear = true;

        [Header("Pearl Drops")]
        public PearlDatabase pearlDatabase;
        public PearlInventory inventory;
        public GameObject pickupPrefab;
        public float spawnHeightOffset = 0.35f;
        public float scatterRadius = 0.4f;

        [Header("Feedback")]
        public bool showMessages = true;

        private PlayerExperienceSystem experienceSystem;

        private void Awake()
        {
            if (sequenceController == null)
            {
                sequenceController = FindObjectOfType<StrongholdSequenceController>();
            }

            if (inventory == null)
            {
                inventory = FindObjectOfType<PearlInventory>();
            }

            if (experienceSystem == null)
            {
                experienceSystem = FindObjectOfType<PlayerExperienceSystem>();
            }

            if (strongholds == null || strongholds.Count == 0)
            {
                StrongholdController[] found = FindObjectsOfType<StrongholdController>();
                strongholds = new List<StrongholdController>(found);
            }
        }

        private void OnEnable()
        {
            BindStrongholds(true);
        }

        private void OnDisable()
        {
            BindStrongholds(false);
        }

        private void BindStrongholds(bool bind)
        {
            if (strongholds == null)
            {
                return;
            }

            for (int i = 0; i < strongholds.Count; i++)
            {
                StrongholdController stronghold = strongholds[i];
                if (stronghold == null)
                {
                    continue;
                }

                if (bind)
                {
                    stronghold.OnWaveCompleted += HandleWaveCompleted;
                    stronghold.OnStrongholdCompleted += HandleStrongholdCompleted;
                }
                else
                {
                    stronghold.OnWaveCompleted -= HandleWaveCompleted;
                    stronghold.OnStrongholdCompleted -= HandleStrongholdCompleted;
                }
            }
        }

        private void HandleWaveCompleted(StrongholdController stronghold, int waveIndex)
        {
            if (expOnWaveComplete > 0 && experienceSystem != null)
            {
                experienceSystem.GrantExperience(expOnWaveComplete);
            }
        }

        private void HandleStrongholdCompleted(StrongholdController stronghold)
        {
            if (expOnStrongholdClear > 0 && experienceSystem != null)
            {
                experienceSystem.GrantExperience(expOnStrongholdClear);
            }

            if (talentPointsOnClear > 0)
            {
                TalentTree talentTree = FindObjectOfType<TalentTree>();
                if (talentTree != null)
                {
                    talentTree.availablePoints += talentPointsOnClear;
                    talentTree.NotifyChanged();
                }
            }

            if (grantPearlOnClear)
            {
                SpawnPearlPickup(stronghold != null ? stronghold.transform.position : transform.position);
            }

            if (showMessages)
            {
                GameEvents.ShowMessage("据点奖励已发放", 2f);
            }
        }

        private void SpawnPearlPickup(Vector3 position)
        {
            PearlItem pearl = PickRandomPearl();
            if (pearl == null)
            {
                return;
            }

            Vector3 spawnPosition = position + Vector3.up * spawnHeightOffset;
            if (scatterRadius > 0f)
            {
                Vector2 scatter = Random.insideUnitCircle * scatterRadius;
                spawnPosition += new Vector3(scatter.x, 0f, scatter.y);
            }

            GameObject pickupObject = null;
            if (pickupPrefab != null)
            {
                pickupObject = Object.Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                pickupObject = CreateRuntimePickup(spawnPosition);
            }

            if (pickupObject == null)
            {
                if (inventory != null)
                {
                    inventory.AddPearl(pearl);
                }
                GameEvents.ShowMessage($"Pearl acquired: {pearl.pearlName}", 2f);
                return;
            }

            PearlPickup pickup = pickupObject.GetComponent<PearlPickup>();
            if (pickup == null)
            {
                pickup = pickupObject.AddComponent<PearlPickup>();
            }

            pickup.Initialize(pearl, inventory);
        }

        private PearlItem PickRandomPearl()
        {
            if (pearlDatabase == null || pearlDatabase.pearls == null || pearlDatabase.pearls.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, pearlDatabase.pearls.Count);
            return pearlDatabase.pearls[index];
        }

        private GameObject CreateRuntimePickup(Vector3 position)
        {
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupObject.name = "PearlPickup";
            pickupObject.transform.position = position;
            pickupObject.transform.localScale = Vector3.one * 0.35f;

            SphereCollider collider = pickupObject.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Rigidbody rb = pickupObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return pickupObject;
        }
    }
}
