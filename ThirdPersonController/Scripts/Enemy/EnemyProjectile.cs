using UnityEngine;

namespace ThirdPersonController
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class EnemyProjectile : MonoBehaviour
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
        private Transform owner;
        private float timer;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;

            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
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

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.isTrigger)
            {
                return;
            }

            if (owner != null && other.transform == owner)
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
                Destroy(gameObject);
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
    }
}
