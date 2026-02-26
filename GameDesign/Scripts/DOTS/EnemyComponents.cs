using Unity.Entities;
using Unity.Mathematics;

namespace ThirdPersonController
{
    public struct EnemyTag : IComponentData
    {
    }

    public struct EnemyHealthData : IComponentData
    {
        public int Current;
        public int Max;
    }

    public struct EnemyMoveSpeed : IComponentData
    {
        public float Value;
    }

    public struct EnemyAttackDamage : IComponentData
    {
        public int Value;
    }

    public struct FlowFieldAgent : IComponentData
    {
        public float MoveSpeed;
        public float StoppingDistance;
    }

    public struct FlowFieldTarget : IComponentData
    {
        public float3 Position;
    }

    public struct FlowFieldGrid : IComponentData
    {
        public int2 Size;
        public float CellSize;
        public float3 Origin;
        public int2 LastTargetCell;
        public byte NeedsRebuild;
    }

    public struct FlowFieldCell : IBufferElementData
    {
        public byte Cost;
        public int BestCost;
        public float2 Direction;
    }
}
