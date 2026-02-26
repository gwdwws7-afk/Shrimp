using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ThirdPersonController
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct FlowFieldMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<FlowFieldGrid>(out var grid))
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<FlowFieldTarget>(out var target))
            {
                return;
            }

            DynamicBuffer<FlowFieldCell> cells = SystemAPI.GetSingletonBuffer<FlowFieldCell>();
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, agent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FlowFieldAgent>>())
            {
                float3 position = transform.ValueRO.Position;
                float3 toTarget = target.Position - position;
                toTarget.y = 0f;
                if (math.lengthsq(toTarget) <= agent.ValueRO.StoppingDistance * agent.ValueRO.StoppingDistance)
                {
                    continue;
                }

                int2 cell = WorldToCell(grid, position);
                int index = cell.y * grid.Size.x + cell.x;
                if (index < 0 || index >= cells.Length)
                {
                    continue;
                }

                float2 direction = cells[index].Direction;
                if (math.lengthsq(direction) < 0.001f)
                {
                    continue;
                }

                float3 move = new float3(direction.x, 0f, direction.y) * agent.ValueRO.MoveSpeed * deltaTime;
                transform.ValueRW.Position += move;
            }
        }

        private static int2 WorldToCell(FlowFieldGrid grid, float3 position)
        {
            float3 local = position - grid.Origin;
            int x = (int)math.floor(local.x / grid.CellSize);
            int y = (int)math.floor(local.z / grid.CellSize);
            return new int2(math.clamp(x, 0, grid.Size.x - 1), math.clamp(y, 0, grid.Size.y - 1));
        }
    }
}
