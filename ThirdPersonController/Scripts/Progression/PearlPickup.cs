using UnityEngine;

namespace ThirdPersonController
{
    [RequireComponent(typeof(Collider))]
    public class PearlPickup : MonoBehaviour
    {
        [Header("Pickup")]
        public PearlItem pearl;
        public PearlInventory inventory;
        public float pickupDelay = 0.15f;

        [Header("Presentation")]
        public float rotateSpeed = 90f;
        public float bobHeight = 0.2f;
        public float bobSpeed = 2f;
        public AudioClip pickupSound;
        public GameObject pickupVfx;

        private Vector3 basePosition;
        private float spawnTime;
        private bool collected;

        private void Awake()
        {
            basePosition = transform.position;
            spawnTime = Time.time;

            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            float bobOffset = Mathf.Sin((Time.time + basePosition.x) * bobSpeed) * bobHeight;
            transform.position = basePosition + Vector3.up * bobOffset;
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        public void Initialize(PearlItem item, PearlInventory targetInventory)
        {
            pearl = item;
            inventory = targetInventory;
            basePosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
            {
                return;
            }

            if (Time.time - spawnTime < pickupDelay)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            PearlInventory targetInventory = inventory;
            if (targetInventory == null)
            {
                targetInventory = other.GetComponent<PearlInventory>();
            }
            if (targetInventory == null)
            {
                targetInventory = FindObjectOfType<PearlInventory>();
            }

            if (targetInventory == null || pearl == null)
            {
                return;
            }

            if (!targetInventory.AddPearl(pearl))
            {
                return;
            }

            collected = true;
            GameEvents.ShowMessage($"Pearl acquired: {pearl.pearlName}", 2f);

            if (pickupVfx != null)
            {
                EffectPoolManager.SpawnEffect(pickupVfx, transform.position, Quaternion.identity, 1.2f);
            }

            if (pickupSound != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(pickupSound, transform.position);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
            }

            Destroy(gameObject);
        }

        private bool IsPlayer(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            if (collider.CompareTag("Player"))
            {
                return true;
            }

            return collider.GetComponent<PlayerCombat>() != null;
        }
    }
}
