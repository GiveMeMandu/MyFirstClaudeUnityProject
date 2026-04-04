using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Data;
using Debug = UnityEngine.Debug;

namespace ProjectSun.V2.Defense.Bridge
{
    /// <summary>
    /// 밤→낮 브릿지: ECS World에서 전투 결과를 수집하여 WaveResult로 반환.
    /// Interface Contract: CombatResult, BuildingDamageReport 이행.
    /// </summary>
    public class BattleResultCollector : MonoBehaviour
    {
        /// <summary>결과 수집 소요 시간(ms).</summary>
        public long CollectTimeMs { get; private set; }

        /// <summary>
        /// ECS World에서 전투 결과를 수집하고 GameState에 반영.
        /// </summary>
        public WaveResult CollectResults(GameState gameState)
        {
            var sw = Stopwatch.StartNew();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[BattleResultCollector] ECS World not available");
                return null;
            }

            var em = world.EntityManager;
            var result = new WaveResult
            {
                turnNumber = gameState.currentTurn,
                damagedBuildingSlotIds = new List<string>()
            };

            // 건물 HP 수집 → BuildingRuntimeState 갱신 (BuildingDamageReport 계약)
            CollectBuildingResults(em, gameState, result);

            // 적 처치 통계
            CollectEnemyResults(em, result);

            // 분대 피해 → 시민 부상 (CombatResult 계약)
            CollectSquadResults(em, gameState);

            // 방어 등급 판정
            result.isPerfectDefense = result.damagedBuildingSlotIds.Count == 0;

            // 보상 (등급에 따라)
            float defenseRatio = result.enemiesTotal > 0
                ? (float)result.enemiesDefeated / result.enemiesTotal
                : 1f;
            result.basicReward = Mathf.RoundToInt(defenseRatio * 20f);
            result.advancedReward = result.isPerfectDefense ? 5 : 0;

            sw.Stop();
            CollectTimeMs = sw.ElapsedMilliseconds;

            Debug.Log($"[BattleResultCollector] Results collected in {CollectTimeMs}ms — " +
                      $"Killed: {result.enemiesDefeated}/{result.enemiesTotal}, " +
                      $"Damaged buildings: {result.damagedBuildingSlotIds.Count}, " +
                      $"Perfect: {result.isPerfectDefense}");

            return result;
        }

        void CollectBuildingResults(EntityManager em, GameState gameState, WaveResult result)
        {
            var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingTag>(),
                ComponentType.ReadOnly<BuildingData>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var data = em.GetComponentData<BuildingData>(entities[i]);
                int slotIndex = data.SlotIndex;

                if (slotIndex < 0 || slotIndex >= gameState.buildings.Count)
                    continue;

                var buildingState = gameState.buildings[slotIndex];
                float previousHP = buildingState.currentHP;
                float currentHP = data.CurrentHP;

                // HP 변동 반영 (라운드트립 핵심)
                buildingState.currentHP = Mathf.RoundToInt(currentHP);

                // 손상 판정
                if (currentHP < previousHP)
                {
                    result.damagedBuildingSlotIds.Add(buildingState.slotId);

                    if (buildingState.state == BuildingSlotStateV2.Active)
                        buildingState.state = BuildingSlotStateV2.Damaged;
                }
            }
        }

        void CollectEnemyResults(EntityManager em, WaveResult result)
        {
            // BattleStatistics 싱글턴에서 읽기
            var statsQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BattleStatistics>());
            if (statsQuery.CalculateEntityCount() > 0)
            {
                using var statsEntities = statsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                var stats = em.GetComponentData<BattleStatistics>(statsEntities[0]);
                result.enemiesDefeated = stats.TotalEnemiesKilled;
                result.enemiesTotal = stats.TotalEnemiesSpawned;
            }
            else
            {
                // BattleStatistics가 없으면 직접 카운트
                var allQuery = em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());
                var aliveQuery = em.CreateEntityQuery(
                    new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                        .WithAll<EnemyTag>()
                        .WithNone<DeadTag>());

                int total = allQuery.CalculateEntityCount();
                int alive = aliveQuery.CalculateEntityCount();
                result.enemiesTotal = total;
                result.enemiesDefeated = total - alive;
            }
        }

        void CollectSquadResults(EntityManager em, GameState gameState)
        {
            var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<ProjectSun.V2.Defense.ECS.SquadTag>(),
                ComponentType.ReadOnly<ProjectSun.V2.Defense.ECS.SquadId>(),
                ComponentType.ReadOnly<ProjectSun.V2.Defense.ECS.SquadStats>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            int injuredCount = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var stats = em.GetComponentData<ProjectSun.V2.Defense.ECS.SquadStats>(entities[i]);

                // 분대 HP가 50% 미만이면 부상 판정
                if (stats.CurrentHP < stats.MaxHP * 0.5f)
                    injuredCount++;
            }

            if (injuredCount > 0)
                Debug.Log($"[BattleResultCollector] {injuredCount} squads took heavy casualties");
        }
    }
}
