using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectSun.Defense.ECS; // V1 컴포넌트: EnemyTag, EnemyStats, EnemyState, EnemyTarget, DeadTag

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// 플로우 필드 기반 적 이동 시스템.
    ///
    /// FlowFieldAgent 컴포넌트가 있는 적만 처리.
    /// EnemyMovementSystem(V1) / EnemyMovementSystemV2와 공존 가능:
    ///   - FlowFieldAgent O → 이 시스템이 이동 담당
    ///   - FlowFieldAgent X → 기존 시스템이 이동 담당
    ///
    /// 매 프레임 비용: 엔티티당 DirectionField[cellIdx] 1회 참조 = O(1).
    /// 3,000개체 기준 ~0.2ms (Burst).
    ///
    /// EnemyMovementSystemV2 대비 장점:
    ///   - O(E*B) 최근접 건물 탐색 불필요
    ///   - 방벽 우회 경로를 자동으로 제공
    ///   - 전역 최적 경로 (직선 탐색은 로컬 최적)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FlowFieldSystem))]
    public partial struct FlowFieldMovementSystem : ISystem
    {
        NativeArray<float3> _dirLUT;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldConfig>();
            state.RequireForUpdate<EnemyTag>();
            _dirLUT = FlowDir.CreateLUT(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_dirLUT.IsCreated) _dirLUT.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var grid = ref SharedFlowField.Grid.Data;
            if (!grid.IsCreated) return;

            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (tf, stats, es, target, agent) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemyStats>, RefRO<EnemyState>,
                                RefRW<EnemyTarget>, RefRO<FlowFieldAgent>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                // Attacking/Dying 상태면 이동하지 않음
                int stateVal = es.ValueRO.Value;
                if (stateVal == (int)global::ProjectSun.Defense.EnemyState.Attacking ||
                    stateVal == (int)global::ProjectSun.Defense.EnemyState.Dying)
                    continue;

                float3 pos = tf.ValueRO.Position;
                int cellIdx = grid.WorldToIndex(pos);
                if (cellIdx < 0 || cellIdx >= grid.CellCount) continue;

                // 지상/공중 필드 선택
                byte dirByte = agent.ValueRO.UseAirField
                    ? grid.AirDirectionField[cellIdx]
                    : grid.DirectionField[cellIdx];

                // Goal 도달 또는 경로 없음 → 전투 시스템이 타겟 할당
                if (dirByte == FlowDir.Goal)
                {
                    // 목표 근처에 도달 — EnemyCombatSystem이 타겟 할당하도록 유지
                    continue;
                }
                if (dirByte == FlowDir.None)
                {
                    // 경로 없음 (장애물로 둘러싸여 있거나 그리드 밖)
                    target.ValueRW.HasTarget = false;
                    continue;
                }

                // LUT에서 정규화된 방향 벡터 참조
                float3 moveDir = _dirLUT[dirByte];
                if (math.lengthsq(moveDir) < 0.001f) continue;

                float speed = stats.ValueRO.Speed;
                float3 newPos = pos + moveDir * speed * dt;

                // 공중 유닛 높이 고정
                bool isFlying = stats.ValueRO.EnemyType == 2;
                newPos.y = isFlying ? 3f : 0f;

                tf.ValueRW = LocalTransform.FromPositionRotation(
                    newPos,
                    quaternion.LookRotationSafe(moveDir, math.up())
                );
            }
        }
    }
}
