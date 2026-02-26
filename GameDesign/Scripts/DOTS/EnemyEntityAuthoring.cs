using Unity.Entities;
using UnityEngine;

namespace ThirdPersonController
{
    public class EnemyEntityAuthoring : MonoBehaviour
    {
        public int maxHealth = 100;
        public float moveSpeed = 4f;
        public int attackDamage = 10;
        public float stoppingDistance = 1.2f;
    }

    public class EnemyEntityAuthoringBaker : Baker<EnemyEntityAuthoring>
    {
        public override void Bake(EnemyEntityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EnemyTag>(entity);
            AddComponent(entity, new EnemyHealthData
            {
                Current = authoring.maxHealth,
                Max = authoring.maxHealth
            });
            AddComponent(entity, new EnemyMoveSpeed
            {
                Value = authoring.moveSpeed
            });
            AddComponent(entity, new EnemyAttackDamage
            {
                Value = authoring.attackDamage
            });
            AddComponent(entity, new FlowFieldAgent
            {
                MoveSpeed = authoring.moveSpeed,
                StoppingDistance = authoring.stoppingDistance
            });
        }
    }
}
