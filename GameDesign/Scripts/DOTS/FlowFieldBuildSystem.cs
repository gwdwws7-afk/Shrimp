using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ThirdPersonController
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct FlowFieldBuildSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<FlowFieldGrid>(out var gridRW))
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<FlowFieldTarget>(out var target))
            {
                return;
            }

            FlowFieldGrid grid = gridRW.ValueRO;
            if (grid.Size.x <= 0 || grid.Size.y <= 0)
            {
                return;
            }

            int2 targetCell = WorldToCell(grid, target.Position);
            bool rebuild = grid.NeedsRebuild != 0 || !targetCell.Equals(grid.LastTargetCell);
            if (!rebuild)
            {
                return;
            }

            DynamicBuffer<FlowFieldCell> cells = SystemAPI.GetSingletonBuffer<FlowFieldCell>();
            int total = grid.Size.x * grid.Size.y;
            if (cells.Length != total)
            {
                cells.ResizeUninitialized(total);
                for (int i = 0; i < total; i++)
                {
                    cells[i] = new FlowFieldCell
                    {
                        Cost = 1,
                        BestCost = int.MaxValue,
                        Direction = float2.zero
                    };
                }
            }

            for (int i = 0; i < cells.Length; i++)
            {
                FlowFieldCell cell = cells[i];
                if (cell.Cost == 0)
                {
                    cell.Cost = 1;
                }
                cell.BestCost = int.MaxValue;
                cell.Direction = float2.zero;
                cells[i] = cell;
            }

            if (!IsValidCell(grid, targetCell))
            {
                return;
            }

            int targetIndex = IndexOf(grid, targetCell);
            FlowFieldCell targetData = cells[targetIndex];
            targetData.BestCost = 0;
            cells[targetIndex] = targetData;

            using (NativeQueue<int2> open = new NativeQueue<int2>(Allocator.Temp))
            {
                open.Enqueue(targetCell);
                while (open.TryDequeue(out int2 current))
                {
                    int currentIndex = IndexOf(grid, current);
                    int currentCost = cells[currentIndex].BestCost;

                    for (int i = 0; i < 4; i++)
                    {
                        int2 neighbor = current + NeighborOffset(i);
                        if (!IsValidCell(grid, neighbor))
                        {
                            continue;
                        }

                        int neighborIndex = IndexOf(grid, neighbor);
                        FlowFieldCell neighborData = cells[neighborIndex];
                        int newCost = currentCost + neighborData.Cost;
                        if (newCost < neighborData.BestCost)
                        {
                            neighborData.BestCost = newCost;
                            cells[neighborIndex] = neighborData;
                            open.Enqueue(neighbor);
                        }
                    }
                }
            }

            for (int y = 0; y < grid.Size.y; y++)
            {
                for (int x = 0; x < grid.Size.x; x++)
                {
                    int2 cell = new int2(x, y);
                    int index = IndexOf(grid, cell);
                    FlowFieldCell cellData = cells[index];
                    if (cellData.BestCost == int.MaxValue)
                    {
                        cellData.Direction = float2.zero;
                        cells[index] = cellData;
                        continue;
                    }

                    int bestCost = cellData.BestCost;
                    float2 bestDirection = float2.zero;
                    for (int i = 0; i < 4; i++)
                    {
                        int2 neighbor = cell + NeighborOffset(i);
                        if (!IsValidCell(grid, neighbor))
                        {
                            continue;
                        }

                        int neighborIndex = IndexOf(grid, neighbor);
                        int neighborCost = cells[neighborIndex].BestCost;
                        if (neighborCost < bestCost)
                        {
                            bestCost = neighborCost;
                            bestDirection = math.normalize(new float2(neighbor.x - cell.x, neighbor.y - cell.y));
                        }
                    }

                    cellData.Direction = bestDirection;
                    cells[index] = cellData;
                }
            }

            grid.LastTargetCell = targetCell;
            grid.NeedsRebuild = 0;
            gridRW.ValueRW = grid;
        }

        private static int IndexOf(FlowFieldGrid grid, int2 cell)
        {
            return cell.y * grid.Size.x + cell.x;
        }

        private static bool IsValidCell(FlowFieldGrid grid, int2 cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < grid.Size.x && cell.y < grid.Size.y;
        }

        private static int2 NeighborOffset(int index)
        {
            switch (index)
            {
                case 0: return new int2(1, 0);
                case 1: return new int2(-1, 0);
                case 2: return new int2(0, 1);
                default: return new int2(0, -1);
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
