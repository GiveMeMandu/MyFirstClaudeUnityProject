using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectSun.Defense.ECS; // V1 컴포넌트 참조 (BuildingTag, BuildingData 등)

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// SharedStatic으로 FlowFieldGrid를 시스템 간 공유.
    /// FlowFieldSystem이 Write, FlowFieldMovementSystem이 Read.
    /// </summary>
    public static class SharedFlowField
    {
        public static readonly SharedStatic<FlowFieldGrid> Grid =
            SharedStatic<FlowFieldGrid>.GetOrCreate<SharedFlowFieldCtx, FlowFieldGrid>();

        // SharedStatic requires unique context type
        struct SharedFlowFieldCtx { }
    }

    /// <summary>
    /// 플로우 필드 계산 시스템.
    ///
    /// 재계산 조건: FlowFieldConfig.NeedsRecompute == true
    ///   - 밤 전투 시작 시 (FlowFieldSetup이 true로 설정)
    ///   - 건물 파괴 시 (외부 시스템이 true로 설정)
    ///
    /// 파이프라인 (200x200 그리드, Burst+Jobs 기준 < 1ms):
    ///   1. InitCostFieldJob (IJobParallelFor) — 모든 셀 비용 1
    ///   2. MarkObstaclesJob (IJobParallelFor) — 건물/방벽 위치에 255
    ///   3. ComputeIntegrationFieldJob (IJob) — BFS 비용 전파
    ///   4. ComputeDirectionFieldJob (IJobParallelFor) — 8방향 최적 방향
    ///   지상+공중 각각 → 총 6 Jobs
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FlowFieldMovementSystem))]
    public partial struct FlowFieldSystem : ISystem
    {
        NativeArray<int> _goalIndices;
        bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldConfig>();
        }

        public void OnDestroy(ref SystemState state)
        {
            ref var grid = ref SharedFlowField.Grid.Data;
            if (grid.IsCreated) grid.Dispose();
            if (_goalIndices.IsCreated) _goalIndices.Dispose();
        }

        // I-01: [BurstCompile] 제거 — 첫 프레임 초기화 시 Persistent NativeArray 할당이 필요.
        // Burst는 관리 코드(new FlowFieldGrid Persistent 할당)를 컴파일하지 못한다.
        // Job 스케줄 자체는 Burst이므로 성능에 실질적 영향 없음.
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingletonRW<FlowFieldConfig>();
            ref var grid = ref SharedFlowField.Grid.Data;

            if (!_initialized)
            {
                // Persistent 할당 — OnUpdate에서 처리하는 이유:
                // FlowFieldConfig 싱글턴은 FlowFieldSetup.Start()에서 생성되므로
                // OnCreate 시점에는 존재하지 않음. 첫 프레임에서 1회만 실행.
                grid = new FlowFieldGrid(
                    config.ValueRO.GridWidth,
                    config.ValueRO.GridHeight,
                    config.ValueRO.CellSize,
                    config.ValueRO.WorldOrigin,
                    Allocator.Persistent
                );
                _goalIndices = new NativeArray<int>(32, Allocator.Persistent);
                _initialized = true;
                config.ValueRW.NeedsRecompute = true;
            }

            if (!config.ValueRO.NeedsRecompute) return;

            config.ValueRW.NeedsRecompute = false;
            config.ValueRW.LastComputedFrame = (int)(SystemAPI.Time.ElapsedTime * 60);

            int totalCells = grid.CellCount;

            // === Step 1: Init cost fields (병렬) ===
            var initGround = new InitCostFieldJob { CostField = grid.CostField };
            var initAir = new InitCostFieldJob { CostField = grid.AirCostField };
            var initHandle = JobHandle.CombineDependencies(
                initGround.Schedule(totalCells, 256, state.Dependency),
                initAir.Schedule(totalCells, 256, state.Dependency)
            );

            // === Step 2: 장애물 수집 + 마킹 ===
            var positions = new NativeList<float3>(32, Allocator.TempJob);
            var radii = new NativeList<float>(32, Allocator.TempJob);
            var isWall = new NativeList<bool>(32, Allocator.TempJob);
            var goals = new NativeList<float3>(8, Allocator.TempJob);

            foreach (var (tf, bd) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<BuildingData>>()
                    .WithAll<BuildingTag>())
            {
                if (bd.ValueRO.CurrentHP <= 0f) continue;

                positions.Add(tf.ValueRO.Position);
                radii.Add(grid.CellSize * 0.8f);
                isWall.Add(bd.ValueRO.IsWall);

                // 비방벽 건물(본부 포함)이 적의 목표
                if (!bd.ValueRO.IsWall)
                    goals.Add(tf.ValueRO.Position);
            }

            if (positions.Length > 0)
            {
                var markJob = new MarkObstaclesJob
                {
                    Positions = positions.AsArray(),
                    Radii = radii.AsArray(),
                    IsWall = isWall.AsArray(),
                    GridWidth = grid.Width,
                    GridHeight = grid.Height,
                    CellSize = grid.CellSize,
                    InvCellSize = grid.InvCellSize,
                    WorldOrigin = grid.WorldOrigin,
                    GroundCost = grid.CostField,
                    AirCost = grid.AirCostField
                };
                initHandle = markJob.Schedule(positions.Length, 4, initHandle);
            }

            initHandle.Complete();

            // === Step 3: 목표 인덱스 변환 ===
            int goalCount = 0;
            if (_goalIndices.Length < goals.Length)
            {
                _goalIndices.Dispose();
                _goalIndices = new NativeArray<int>(math.max(goals.Length, 8), Allocator.Persistent);
            }

            for (int i = 0; i < goals.Length; i++)
            {
                int idx = grid.WorldToIndex(goals[i]);
                if (idx < 0 || idx >= totalCells) continue;

                // 목표 셀이 장애물이면 인접 통과 가능 셀 탐색
                if (grid.CostField[idx] == 255)
                    idx = FindPassableNeighbor(ref grid, idx);

                if (idx >= 0)
                {
                    _goalIndices[goalCount] = idx;
                    goalCount++;
                }
            }

            // === Step 4: 통합 필드 계산 (지상/공중 병렬) ===
            var intGround = new ComputeIntegrationFieldJob
            {
                IntegrationField = grid.IntegrationField,
                CostField = grid.CostField,
                GoalIndices = _goalIndices,
                GoalCount = goalCount,
                Width = grid.Width,
                Height = grid.Height
            };
            var intAir = new ComputeIntegrationFieldJob
            {
                IntegrationField = grid.AirIntegrationField,
                CostField = grid.AirCostField,
                GoalIndices = _goalIndices,
                GoalCount = goalCount,
                Width = grid.Width,
                Height = grid.Height
            };
            var intHandle = JobHandle.CombineDependencies(
                intGround.Schedule(),
                intAir.Schedule()
            );
            intHandle.Complete();

            // === Step 5: 방향 필드 계산 (병렬) ===
            var dirGround = new ComputeDirectionFieldJob
            {
                IntegrationField = grid.IntegrationField,
                CostField = grid.CostField,
                DirectionField = grid.DirectionField,
                Width = grid.Width,
                Height = grid.Height
            };
            var dirAir = new ComputeDirectionFieldJob
            {
                IntegrationField = grid.AirIntegrationField,
                CostField = grid.AirCostField,
                DirectionField = grid.AirDirectionField,
                Width = grid.Width,
                Height = grid.Height
            };
            var dirHandle = JobHandle.CombineDependencies(
                dirGround.Schedule(totalCells, 256),
                dirAir.Schedule(totalCells, 256)
            );
            dirHandle.Complete();

            positions.Dispose();
            radii.Dispose();
            isWall.Dispose();
            goals.Dispose();

            state.Dependency = default;
        }

        /// <summary>장애물 셀의 인접 통과 가능 셀 탐색 (반경 3까지).</summary>
        static int FindPassableNeighbor(ref FlowFieldGrid grid, int centerIdx)
        {
            int2 c = grid.IndexToCoord(centerIdx);
            for (int r = 1; r <= 3; r++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (math.abs(dx) != r && math.abs(dz) != r) continue;
                        int nx = c.x + dx;
                        int nz = c.y + dz;
                        if (!grid.InBounds(nx, nz)) continue;
                        int nIdx = grid.CoordToIndex(nx, nz);
                        if (grid.CostField[nIdx] != 255) return nIdx;
                    }
                }
            }
            return -1;
        }
    }
}
