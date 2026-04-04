using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ProjectSun.V2.Defense.ECS
{
    // ================================================================
    //  Step 1: 비용 필드 초기화
    // ================================================================

    /// <summary>모든 셀을 기본 비용(1)으로 초기화. IJobParallelFor로 병렬.</summary>
    [BurstCompile]
    public struct InitCostFieldJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<byte> CostField;

        public void Execute(int index)
        {
            CostField[index] = 1;
        }
    }

    // ================================================================
    //  Step 2: 장애물 마킹
    // ================================================================

    /// <summary>
    /// 건물/방벽 위치를 그리드 셀에 매핑하여 비용 255(통행불가)로 설정.
    /// 건물당 1 실행 → 장애물 수(~25개) 병렬.
    ///
    /// 지상 필드: 건물+방벽 모두 장애물.
    /// 공중 필드: 건물만 장애물 (방벽은 통과 가능).
    /// </summary>
    [BurstCompile]
    public struct MarkObstaclesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float> Radii;
        [ReadOnly] public NativeArray<bool> IsWall;

        public int GridWidth;
        public int GridHeight;
        public float CellSize;
        public float InvCellSize;
        public float3 WorldOrigin;

        [NativeDisableParallelForRestriction] public NativeArray<byte> GroundCost;
        [NativeDisableParallelForRestriction] public NativeArray<byte> AirCost;

        public void Execute(int i)
        {
            float3 pos = Positions[i];
            float r = Radii[i];
            bool wall = IsWall[i];

            int minX = math.max(0, (int)math.floor((pos.x - r - WorldOrigin.x) * InvCellSize));
            int maxX = math.min(GridWidth - 1, (int)math.floor((pos.x + r - WorldOrigin.x) * InvCellSize));
            int minZ = math.max(0, (int)math.floor((pos.z - r - WorldOrigin.z) * InvCellSize));
            int maxZ = math.min(GridHeight - 1, (int)math.floor((pos.z + r - WorldOrigin.z) * InvCellSize));

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int idx = z * GridWidth + x;
                    GroundCost[idx] = 255;

                    // Air: 방벽은 통과 가능
                    if (!wall)
                    {
                        AirCost[idx] = 255;
                    }
                }
            }
        }
    }

    // ================================================================
    //  Step 3: 통합 필드 계산 (BFS + 비용 전파)
    // ================================================================

    /// <summary>
    /// 목표 셀들에서 BFS로 비용을 전파하여 IntegrationField를 계산.
    ///
    /// IJob(단일 스레드) — BFS는 본질적으로 순차적이나,
    /// 100x100 = 10K 셀 기준 Burst 컴파일 시 ~0.3ms로 충분.
    ///
    /// 대각선 이동 비용: 직선 비용 x 1.4 (14/10 정수 근사).
    /// </summary>
    [BurstCompile]
    public struct ComputeIntegrationFieldJob : IJob
    {
        public NativeArray<ushort> IntegrationField;
        [ReadOnly] public NativeArray<byte> CostField;
        [ReadOnly] public NativeArray<int> GoalIndices;
        public int GoalCount;
        public int Width;
        public int Height;

        public void Execute()
        {
            int total = Width * Height;

            // 모든 셀을 미도달(MaxValue)로 초기화
            for (int i = 0; i < total; i++)
                IntegrationField[i] = ushort.MaxValue;

            var queue = new NativeQueue<int>(Allocator.Temp);

            // 목표 셀 시드
            for (int g = 0; g < GoalCount; g++)
            {
                int idx = GoalIndices[g];
                if (idx >= 0 && idx < total)
                {
                    IntegrationField[idx] = 0;
                    queue.Enqueue(idx);
                }
            }

            // BFS 비용 전파
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int cx = cur % Width;
                int cz = cur / Width;
                ushort curCost = IntegrationField[cur];

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = cx + NeighborDX(dir);
                    int nz = cz + NeighborDZ(dir);

                    if (nx < 0 || nx >= Width || nz < 0 || nz >= Height) continue;

                    int nIdx = nz * Width + nx;
                    byte cellCost = CostField[nIdx];
                    if (cellCost == 255) continue; // 장애물

                    // 대각선(홀수 dir) 비용 보정: x1.4
                    ushort moveCost = (dir & 1) != 0
                        ? (ushort)(cellCost * 14 / 10)
                        : cellCost;

                    ushort newCost = (ushort)(curCost + moveCost);

                    if (newCost < IntegrationField[nIdx])
                    {
                        IntegrationField[nIdx] = newCost;
                        queue.Enqueue(nIdx);
                    }
                }
            }

            queue.Dispose();
        }

        // 방향별 이웃 오프셋 (Burst에서 static array 불가하므로 switch)
        static int NeighborDX(int dir)
        {
            switch (dir)
            {
                case 0: return  0; // N
                case 1: return  1; // NE
                case 2: return  1; // E
                case 3: return  1; // SE
                case 4: return  0; // S
                case 5: return -1; // SW
                case 6: return -1; // W
                case 7: return -1; // NW
                default: return 0;
            }
        }

        static int NeighborDZ(int dir)
        {
            switch (dir)
            {
                case 0: return  1; // N
                case 1: return  1; // NE
                case 2: return  0; // E
                case 3: return -1; // SE
                case 4: return -1; // S
                case 5: return -1; // SW
                case 6: return  0; // W
                case 7: return  1; // NW
                default: return 0;
            }
        }
    }

    // ================================================================
    //  Step 4: 방향 필드 생성 (병렬)
    // ================================================================

    /// <summary>
    /// 각 셀에서 이웃 8개의 IntegrationField 값을 비교,
    /// 가장 낮은 비용 이웃 방향을 DirectionField에 기록.
    ///
    /// IJobParallelFor — 10K셀 기준 ~0.1ms.
    /// </summary>
    [BurstCompile]
    public struct ComputeDirectionFieldJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ushort> IntegrationField;
        [ReadOnly] public NativeArray<byte> CostField;
        [WriteOnly] public NativeArray<byte> DirectionField;
        public int Width;
        public int Height;

        public void Execute(int index)
        {
            if (CostField[index] == 255)
            {
                DirectionField[index] = FlowDir.None;
                return;
            }

            ushort myCost = IntegrationField[index];

            if (myCost == 0)
            {
                DirectionField[index] = FlowDir.Goal;
                return;
            }

            if (myCost == ushort.MaxValue)
            {
                DirectionField[index] = FlowDir.None;
                return;
            }

            int cx = index % Width;
            int cz = index / Width;
            ushort bestCost = myCost;
            byte bestDir = FlowDir.None;

            for (byte dir = 0; dir < 8; dir++)
            {
                int nx = cx + NeighborDX(dir);
                int nz = cz + NeighborDZ(dir);

                if (nx < 0 || nx >= Width || nz < 0 || nz >= Height) continue;

                ushort nCost = IntegrationField[nz * Width + nx];
                if (nCost < bestCost)
                {
                    bestCost = nCost;
                    bestDir = dir;
                }
            }

            DirectionField[index] = bestDir;
        }

        static int NeighborDX(int dir)
        {
            switch (dir)
            {
                case 0: return  0;
                case 1: return  1;
                case 2: return  1;
                case 3: return  1;
                case 4: return  0;
                case 5: return -1;
                case 6: return -1;
                case 7: return -1;
                default: return 0;
            }
        }

        static int NeighborDZ(int dir)
        {
            switch (dir)
            {
                case 0: return  1;
                case 1: return  1;
                case 2: return  0;
                case 3: return -1;
                case 4: return -1;
                case 5: return -1;
                case 6: return  0;
                case 7: return  1;
                default: return 0;
            }
        }
    }
}
