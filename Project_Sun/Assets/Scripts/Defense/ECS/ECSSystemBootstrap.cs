using Unity.Entities;
using UnityEngine;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// V2 ECS 시스템 부트스트래퍼.
    /// 자동 등록이 실패하는 경우 수동으로 시스템을 등록한다.
    /// V1 시스템은 비활성화한다.
    /// </summary>
    public class ECSSystemBootstrap : MonoBehaviour
    {
        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[ECSBootstrap] World not available");
                return;
            }

            // V1 비활성화
            TryDisable<EnemyMovementSystem>(world);
            TryDisable<TowerAttackSystem>(world);
            TryDisable<EnemyCombatSystem>(world);
            TryDisable<EnemyDeathSystem>(world);

            // V2 시스템이 SimulationSystemGroup의 업데이트 목록에 있는지 확인
            var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();

            // GetExistingSystem은 World에서 찾지만, SystemGroup에 추가되지 않았을 수 있음
            // 강제로 추가 (이미 있으면 무시됨)
            Debug.Log("[ECSBootstrap] Ensuring V2 systems in SimulationSystemGroup...");

            EnsureSystem<WaveSpawnSystem>(world, simGroup);
            EnsureSystem<EnemyMovementSystemV2>(world, simGroup);
            EnsureSystem<EnemyCombatSystemV2>(world, simGroup);
            EnsureSystem<BuildingDamageApplySystemV2>(world, simGroup);
            EnsureSystem<TowerAttackSystemV2>(world, simGroup);
            EnsureSystem<ProjectileMovementSystem>(world, simGroup);
            EnsureSystem<ProjectileHitSystem>(world, simGroup);
            EnsureSystem<EnemyDeathSystemV2>(world, simGroup);
            EnsureSystem<HealthBarSystem>(world, simGroup);

            // 업데이트 목록 갱신 강제
            try { simGroup.SortSystems(); }
            catch (System.Exception ex) { Debug.LogWarning("[ECSBootstrap] Sort: " + ex.Message); }

            Debug.Log("[ECSBootstrap] 9 V2 systems ensured in update list");
        }

        void EnsureSystem<T>(World world, SimulationSystemGroup group) where T : unmanaged, ISystem
        {
            // 기존 시스템이 있으면 제거 후 재생성
            var existing = world.GetExistingSystem<T>();
            if (existing != default)
            {
                group.RemoveSystemFromUpdateList(existing);
                world.DestroySystem(existing);
            }

            var handle = world.CreateSystem<T>();
            group.AddSystemToUpdateList(handle);
        }

        void TryDisable<T>(World world) where T : unmanaged, ISystem
        {
            var handle = world.GetExistingSystem<T>();
            if (handle != default)
                world.Unmanaged.ResolveSystemStateRef(handle).Enabled = false;
        }
    }
}
