using UnityEngine;

namespace ThirdPersonController
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class EnemyProjectile : MonoBehaviour, IPoolable
    {
        public int damage = 10;
        public float knockback = 2f;
        public float speed = 12f;
        public float lifetime = 4f;
        public bool destroyOnHit = true;

        [Header("Status Effects")]
        public bool applySlow = false;
        public float slowMultiplier = 0.6f;
        public float slowDuration = 2f;
        public bool applyDamageReduction = false;
        public float damageReduction = 0.2f;
        public float damageReductionDuration = 2f;

        private Rigidbody body;
        private Collider triggerCollider;
        private Transform owner;
        private float timer;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;

            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                DespawnSelf();
            }
        }

        public void Launch(Vector3 direction, Transform ownerTransform)
        {
            owner = ownerTransform;
            timer = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = transform.forward;
            }

            body.velocity = direction.normalized * speed;
        }

        public void OnSpawned()
        {
            timer = 0f;
            owner = null;

            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }
        }

        public void OnDespawned()
        {
            timer = 0f;
            owner = null;

            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.isTrigger)
            {
                return;
            }

            if (owner != null && other.transform != null && other.transform.IsChildOf(owner))
            {
                return;
            }

            bool damaged = false;
            if (other.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage, owner != null ? owner.position : transform.position, knockback);
                ApplyStatus(playerHealth, other);
                damaged = true;
            }
            else if (other.TryGetComponent<DefenseTarget>(out DefenseTarget defenseTarget))
            {
                defenseTarget.TakeDamage(damage, owner != null ? owner.position : transform.position, knockback);
                damaged = true;
            }

            if (destroyOnHit || damaged)
            {
                DespawnSelf();
            }
        }

        private void ApplyStatus(PlayerHealth playerHealth, Collider target)
        {
            if (playerHealth == null)
            {
                return;
            }

            if (applyDamageReduction)
            {
                playerHealth.ApplyDamageReduction(damageReduction, damageReductionDuration);
            }

            if (applySlow && target != null)
            {
                if (target.TryGetComponent<PlayerMovement>(out PlayerMovement movement))
                {
                    movement.ApplyMoveSlow(slowMultiplier, slowDuration);
                }
                else if (target.transform != null && target.transform.parent != null
                    && target.transform.parent.TryGetComponent<PlayerMovement>(out PlayerMovement parentMovement))
                {
                    parentMovement.ApplyMoveSlow(slowMultiplier, slowDuration);
                }
            }
        }

        private void DespawnSelf()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            ObjectPoolManager.Despawn(gameObject);
        }
    }
}
