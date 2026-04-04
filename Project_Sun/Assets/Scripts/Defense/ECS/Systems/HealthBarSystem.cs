using Unity.Burst;
using Unity.Entities;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 체력바 표시 타이머를 관리하는 시스템.
    /// 피격 시 HealthBarTimer를 2초로 설정하고, 매 프레임 감소.
    /// MonoBehaviour 측에서 Timer > 0인 Entity의 체력바를 렌더링.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyDeathSystemV2))]
    public partial struct HealthBarSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (stats, healthBar) in
                SystemAPI.Query<RefRO<EnemyStats>, RefRW<HealthBarTimer>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                // 체력이 줄었으면 타이머 리셋 (CurrentHP < MaxHP)
                if (stats.ValueRO.CurrentHP < stats.ValueRO.MaxHP && healthBar.ValueRO.RemainingTime <= 0f)
                {
                    healthBar.ValueRW.RemainingTime = 2f;
                }

                // 타이머 감소
                if (healthBar.ValueRO.RemainingTime > 0f)
                {
                    healthBar.ValueRW.RemainingTime -= deltaTime;
                }
            }
        }
    }
}
