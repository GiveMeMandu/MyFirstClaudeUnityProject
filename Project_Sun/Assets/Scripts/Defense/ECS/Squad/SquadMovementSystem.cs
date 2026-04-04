using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectSun.Defense.ECS; // EnemyTag, EnemyStats, DeadTag

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// 분대 이동/전투 시스템.
    ///
    /// SquadCommand.Type에 따라:
    ///   Move: 목표 위치로 직선 이동. 적과 교전하지 않음.
    ///   AttackMove: 목표 위치로 이동하며 사거리 내 적 공격.
    ///   HoldPosition: 이동하지 않음. 사거리 내 적만 공격.
    ///   Idle: 초기 배치 위치에서 자동 교전 (HoldPosition과 동일 동작).
    ///
    /// 분대 수: 3~5. 성능 영향 미미. Burst 적용으로 안전 마진 확보.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SquadMovementSystem : ISystem
    {
        // 적 위치 캐시 (분대 전투용)
        NativeList<float3> _enemyPositions;
        NativeList<Entity> _enemyEntities;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SquadTag>();
            _enemyPositions = new NativeList<float3>(256, Allocator.Persistent);
            _enemyEntities = new NativeList<Entity>(256, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_enemyPositions.IsCreated) _enemyPositions.Dispose();
            if (_enemyEntities.IsCreated) _enemyEntities.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // 일시정지 중(dt=0)이면 이동/전투 처리 스킵
            // (명령 적재는 SquadCommandSystem에서 이미 처리됨)
            if (dt <= 0f) return;

            // 적 위치 수집 (분대 공격 타겟 선택용)
            _enemyPositions.Clear();
            _enemyEntities.Clear();

            foreach (var (tf, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                _enemyPositions.Add(tf.ValueRO.Position);
                _enemyEntities.Add(entity);
            }

            // 각 분대 처리
            foreach (var (tf, stats, cmd, atkTimer) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<SquadStats>, RefRO<SquadCommand>, RefRW<SquadAttackTimer>>()
                    .WithAll<SquadTag>())
            {
                float3 pos = tf.ValueRO.Position;
                var cmdType = cmd.ValueRO.Type;
                float3 targetPos = cmd.ValueRO.TargetPosition;

                // === 이동 처리 ===
                bool shouldMove = false;
                float3 moveDir = float3.zero;

                if (cmdType == SquadCommandType.Move || cmdType == SquadCommandType.AttackMove)
                {
                    float distSq = math.distancesq(pos, targetPos);
                    float arriveThreshold = 1f; // 1m 이내 도착 판정

                    if (distSq > arriveThreshold * arriveThreshold)
                    {
                        moveDir = math.normalize(targetPos - pos);
                        shouldMove = true;
                    }
                }
                // HoldPosition, Idle: 이동 없음

                if (shouldMove)
                {
                    float3 newPos = pos + moveDir * stats.ValueRO.MoveSpeed * dt;
                    newPos.y = 0f;
                    tf.ValueRW = LocalTransform.FromPositionRotation(
                        newPos,
                        quaternion.LookRotationSafe(moveDir, math.up())
                    );
                    pos = newPos; // 이동 후 위치로 갱신
                }

                // === 전투 처리 ===
                // Move 상태에서는 교전하지 않음
                if (cmdType == SquadCommandType.Move) continue;

                // AttackMove, HoldPosition, Idle: 사거리 내 적 공격
                atkTimer.ValueRW.TimeSinceLastAttack += dt;

                float attackInterval = stats.ValueRO.AttackSpeed > 0f
                    ? 1f / stats.ValueRO.AttackSpeed
                    : 1f;

                if (atkTimer.ValueRO.TimeSinceLastAttack < attackInterval) continue;

                // 사거리 내 가장 가까운 적 탐색
                float rangeSq = stats.ValueRO.AttackRange * stats.ValueRO.AttackRange;
                float closestDistSq = float.MaxValue;
                int closestIdx = -1;

                for (int i = 0; i < _enemyPositions.Length; i++)
                {
                    float dSq = math.distancesq(pos, _enemyPositions[i]);
                    if (dSq <= rangeSq && dSq < closestDistSq)
                    {
                        closestDistSq = dSq;
                        closestIdx = i;
                    }
                }

                if (closestIdx >= 0)
                {
                    atkTimer.ValueRW.TimeSinceLastAttack = 0f;

                    // 적 HP 감소
                    if (SystemAPI.HasComponent<EnemyStats>(_enemyEntities[closestIdx]))
                    {
                        var enemyStats = SystemAPI.GetComponentRW<EnemyStats>(_enemyEntities[closestIdx]);
                        enemyStats.ValueRW.CurrentHP -= stats.ValueRO.CombatPower;
                    }

                    // 체력바 표시
                    if (SystemAPI.HasComponent<HealthBarTimer>(_enemyEntities[closestIdx]))
                    {
                        var hb = SystemAPI.GetComponentRW<HealthBarTimer>(_enemyEntities[closestIdx]);
                        hb.ValueRW.RemainingTime = 2f;
                    }
                }
            }
        }
    }
}
