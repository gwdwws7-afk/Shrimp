using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ThirdPersonController
{
    public class FlowFieldGridAuthoring : MonoBehaviour
    {
        public int width = 50;
        public int height = 50;
        public float cellSize = 1f;
        public Vector3 origin;
        [Range(1, 10)]
        public int defaultCost = 1;
    }

    public class FlowFieldGridAuthoringBaker : Baker<FlowFieldGridAuthoring>
    {
        public override void Bake(FlowFieldGridAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            int2 size = new int2(Mathf.Max(1, authoring.width), Mathf.Max(1, authoring.height));

            AddComponent(entity, new FlowFieldGrid
            {
                Size = size,
                CellSize = Mathf.Max(0.1f, authoring.cellSize),
                Origin = authoring.origin,
                LastTargetCell = new int2(-1, -1),
                NeedsRebuild = 1
            });

            DynamicBuffer<FlowFieldCell> cells = AddBuffer<FlowFieldCell>(entity);
            int total = size.x * size.y;
            cells.ResizeUninitialized(total);
            for (int i = 0; i < total; i++)
            {
                cells[i] = new FlowFieldCell
                {
                    Cost = (byte)math.clamp(authoring.defaultCost, 1, 10),
                    BestCost = int.MaxValue,
                    Direction = float2.zero
                };
            }
        }
    }
}
