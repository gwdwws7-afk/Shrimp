using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ThirdPersonController
{
    public class FlowFieldDebugGizmos : MonoBehaviour
    {
        [Header("Display")]
        public bool drawInPlayMode = true;
        public bool drawInEditMode = false;
        public bool drawDirections = true;
        public bool drawGridBounds = true;
        public int maxCellsToDraw = 5000;

        [Header("Style")]
        public float arrowScale = 0.45f;
        public float arrowHeadLength = 0.2f;
        public float arrowHeadWidth = 0.12f;
        public float yOffset = 0.05f;
        public Color directionColor = new Color(0.2f, 0.9f, 0.9f, 0.9f);
        public Color boundsColor = new Color(0.1f, 0.4f, 0.6f, 0.8f);

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying && !drawInEditMode)
            {
                return;
            }

            if (Application.isPlaying && !drawInPlayMode)
            {
                return;
            }

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FlowFieldGrid>(), ComponentType.ReadOnly<FlowFieldCell>());
            if (!query.TryGetSingletonEntity<FlowFieldGrid>(out Entity gridEntity))
            {
                return;
            }

            FlowFieldGrid grid = entityManager.GetComponentData<FlowFieldGrid>(gridEntity);
            DynamicBuffer<FlowFieldCell> cells = entityManager.GetBuffer<FlowFieldCell>(gridEntity);

            int total = grid.Size.x * grid.Size.y;
            if (total <= 0 || cells.Length < total)
            {
                return;
            }

            if (drawGridBounds)
            {
                DrawBounds(grid);
            }

            if (!drawDirections)
            {
                return;
            }

            int step = 1;
            if (maxCellsToDraw > 0 && total > maxCellsToDraw)
            {
                float ratio = (float)total / maxCellsToDraw;
                step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(ratio)));
            }

            float cellSize = grid.CellSize;
            Vector3 origin = new Vector3(grid.Origin.x, grid.Origin.y + yOffset, grid.Origin.z);

            Gizmos.color = directionColor;
            for (int y = 0; y < grid.Size.y; y += step)
            {
                for (int x = 0; x < grid.Size.x; x += step)
                {
                    int index = y * grid.Size.x + x;
                    if (index < 0 || index >= cells.Length)
                    {
                        continue;
                    }

                    FlowFieldCell cell = cells[index];
                    float2 dir2 = cell.Direction;
                    if (math.lengthsq(dir2) < 0.001f)
                    {
                        continue;
                    }

                    Vector3 direction = new Vector3(dir2.x, 0f, dir2.y).normalized;
                    Vector3 center = origin + new Vector3((x + 0.5f) * cellSize, 0f, (y + 0.5f) * cellSize);
                    float arrowLength = cellSize * arrowScale;
                    Vector3 end = center + direction * arrowLength;

                    Gizmos.DrawLine(center, end);
                    DrawArrowHead(end, direction, cellSize * arrowHeadLength, cellSize * arrowHeadWidth);
                }
            }
        }

        private void DrawArrowHead(Vector3 end, Vector3 direction, float length, float width)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector3 right = new Vector3(-direction.z, 0f, direction.x);
            Vector3 back = -direction * length;
            Vector3 headA = end + back + right * width;
            Vector3 headB = end + back - right * width;

            Gizmos.DrawLine(end, headA);
            Gizmos.DrawLine(end, headB);
        }

        private void DrawBounds(FlowFieldGrid grid)
        {
            Vector3 origin = new Vector3(grid.Origin.x, grid.Origin.y + yOffset, grid.Origin.z);
            Vector3 size = new Vector3(grid.Size.x * grid.CellSize, 0f, grid.Size.y * grid.CellSize);
            Vector3 a = origin;
            Vector3 b = origin + new Vector3(size.x, 0f, 0f);
            Vector3 c = origin + new Vector3(size.x, 0f, size.z);
            Vector3 d = origin + new Vector3(0f, 0f, size.z);

            Gizmos.color = boundsColor;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
    }
}
