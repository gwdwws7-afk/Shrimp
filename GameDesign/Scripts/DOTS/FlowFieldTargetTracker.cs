using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ThirdPersonController
{
    public class FlowFieldTargetTracker : MonoBehaviour
    {
        public Transform target;

        private EntityManager entityManager;
        private Entity targetEntity = Entity.Null;

        private void Start()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(FlowFieldTarget));
            if (!query.TryGetSingletonEntity<FlowFieldTarget>(out targetEntity))
            {
                targetEntity = entityManager.CreateEntity(typeof(FlowFieldTarget));
            }

            if (target == null)
            {
                TryResolveTarget();
            }
        }

        private void Update()
        {
            if (target == null)
            {
                TryResolveTarget();
            }

            if (entityManager == null || targetEntity == Entity.Null || target == null)
            {
                return;
            }

            FlowFieldTarget data = new FlowFieldTarget
            {
                Position = target.position
            };
            entityManager.SetComponentData(targetEntity, data);
        }

        private void TryResolveTarget()
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null)
            {
                target = movement.transform;
                return;
            }

            PlayerCombat combat = FindObjectOfType<PlayerCombat>();
            if (combat != null)
            {
                target = combat.transform;
            }
        }
    }
}
