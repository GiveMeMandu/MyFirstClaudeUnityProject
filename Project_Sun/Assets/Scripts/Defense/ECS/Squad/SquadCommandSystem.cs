using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// 명령 큐 → ECS 컴포넌트 전달 시스템.
    ///
    /// 매 프레임 SquadCommandQueue에서 명령을 꺼내
    /// 해당 분대의 SquadCommand 컴포넌트를 갱신한다.
    ///
    /// Burst 미적용: NativeQueue는 static이라 SharedStatic으로 래핑하지 않는 한
    /// Burst에서 접근 불가. 큐 처리는 분대 수(3~5)만큼이므로 Burst 없이도 0.01ms.
    ///
    /// 일시정지 처리:
    /// - Time.timeScale == 0이어도 이 시스템은 실행됨 (SimulationSystemGroup은 DeltaTime=0으로 실행)
    /// - 명령은 큐에 적재되며, SquadCommand.Type만 갱신
    /// - SquadMovementSystem이 DeltaTime=0이므로 이동은 일어나지 않음
    /// - 재개 시 DeltaTime > 0이 되면 즉시 이동 시작
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SquadMovementSystem))]
    public partial struct SquadCommandSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SquadTag>();
            SquadCommandQueue.Initialize();
        }

        public void OnDestroy(ref SystemState state)
        {
            SquadCommandQueue.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Drain all pending commands
            while (SquadCommandQueue.TryDequeue(out var entry))
            {
                ApplyCommand(ref state, entry);
            }
        }

        void ApplyCommand(ref SystemState state, SquadCommandEntry entry)
        {
            foreach (var (id, cmd) in
                SystemAPI.Query<RefRO<SquadId>, RefRW<SquadCommand>>()
                    .WithAll<SquadTag>())
            {
                // -1 = 전체 분대, 아니면 특정 분대만
                if (entry.SquadId != -1 && id.ValueRO.Value != entry.SquadId)
                    continue;

                cmd.ValueRW.Type = entry.CommandType;
                cmd.ValueRW.TargetPosition = entry.TargetPosition;
                cmd.ValueRW.TargetEntity = Entity.Null;
                cmd.ValueRW.IssuedTime = entry.IssuedTime;

                // 반응 시간 측정 로깅
                double latency = Time.unscaledTimeAsDouble - entry.IssuedTime;
                if (latency > 0.1)
                {
                    Debug.LogWarning($"[Squad] Command latency {latency * 1000:F1}ms exceeds 100ms target (squad {id.ValueRO.Value})");
                }
            }
        }
    }
}
