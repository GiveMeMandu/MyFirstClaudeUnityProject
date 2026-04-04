using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Data;
using ProjectSun.V2.Defense.ECS;
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

            // 방어 등급 판정 (GDD: 피해 비율 ~10% 이하 = 완벽 방어)
            float totalMaxHP = 0f;
            float totalDamage = 0f;
            for (int i = 0; i < gameState.buildings.Count; i++)
            {
                var b = gameState.buildings[i];
                totalMaxHP += b.maxHP;
                float damage = b.maxHP - b.currentHP;
                if (damage > 0) totalDamage += damage;
            }
            float damageRatio = totalMaxHP > 0f ? totalDamage / totalMaxHP : 0f;
            result.isPerfectDefense = damageRatio <= 0.1f;

            // 보상 (등급에 따라) — Economy-Model 연동
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

                // HP 하한: 0 이하 방지
                currentHP = math.max(0f, currentHP);

                // HP 변동 반영 (라운드트립 핵심)
                buildingState.currentHP = Mathf.RoundToInt(currentHP);

                // 손상 판정 (BuildingDamageReport 계약)
                if (currentHP < previousHP)
                {
                    result.damagedBuildingSlotIds.Add(buildingState.slotId);

                    // GDD: 건물 HP 0 = 손상 상태 전환 (완전 파괴 없음, 본부 제외)
                    // 본부 HP 0 = 게임오버는 상위 레이어에서 처리
                    if (buildingState.state == BuildingSlotStateV2.Active)
                        buildingState.state = BuildingSlotStateV2.Damaged;
                }

                // 본부 HP 0 체크 — 게임오버 조건 로깅 (실제 게임오버 처리는 상위 시스템 책임)
                if (data.IsHeadquarters && currentHP <= 0f)
                {
                    Debug.LogWarning("[BattleResultCollector] HQ destroyed — game over condition met!");
                    result.headquartersDestroyed = true;
                }
            }
        }

        void CollectEnemyResults(EntityManager em, WaveResult result)
        {
            // BattleStatistics 싱글턴에서 읽기 (우선)
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
                // BattleStatistics가 없으면 직접 카운트 (폴백)
                CollectEnemyResultsFallback(em, result);
            }
        }

        /// <summary>
        /// 분대 ECS 결과를 GameState의 시민 상태에 반영 (CombatResult 계약).
        /// GDD: 분대 HP &lt; 50% = 해당 시민 부상, 분대 HP = 0 = 해당 시민 부상 (전멸).
        /// </summary>
        void CollectSquadResults(EntityManager em, GameState gameState)
        {
            var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<SquadTag>(),
                ComponentType.ReadOnly<SquadId>(),
                ComponentType.ReadOnly<SquadStats>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            // InCombat 상태인 시민 목록을 인덱스로 매핑
            // (BattleInitializer.CreateSquadEntities에서 squadIndex 순서와 동일)
            var combatCitizens = new List<CitizenRuntimeState>();
            for (int i = 0; i < gameState.citizens.Count; i++)
            {
                if (gameState.citizens[i].state == CitizenState.InCombat)
                    combatCitizens.Add(gameState.citizens[i]);
            }

            int injuredCount = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var squadId = em.GetComponentData<SquadId>(entities[i]);
                var stats = em.GetComponentData<SquadStats>(entities[i]);

                // squadId.Value는 BattleInitializer의 squadIndex에 대응
                int citizenIndex = squadId.Value;
                if (citizenIndex < 0 || citizenIndex >= combatCitizens.Count)
                    continue;

                var citizen = combatCitizens[citizenIndex];

                // 분대 HP 50% 미만 → 부상 판정
                if (stats.CurrentHP < stats.MaxHP * 0.5f)
                {
                    citizen.state = CitizenState.Injured;
                    injuredCount++;

                    Debug.Log($"[BattleResultCollector] {citizen.displayName} injured " +
                              $"(squad HP: {stats.CurrentHP:F0}/{stats.MaxHP:F0})");
                }
                else
                {
                    // 전투 종료 후 전투 상태에서 원래 상태로 복귀
                    citizen.state = CitizenState.Idle;
                }
            }

            if (injuredCount > 0)
                Debug.Log($"[BattleResultCollector] {injuredCount} squads took heavy casualties — citizens injured");
        }

        /// <summary>
        /// BattleStatistics가 없을 때의 직접 카운트 폴백.
        /// EntityQueryBuilder 기반 쿼리를 안전하게 처리한다.
        /// </summary>
        void CollectEnemyResultsFallback(EntityManager em, WaveResult result)
        {
            var allQuery = em.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());
            int total = allQuery.CalculateEntityCount();

            var aliveQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Exclude<DeadTag>());
            int alive = aliveQuery.CalculateEntityCount();

            result.enemiesTotal = total;
            result.enemiesDefeated = total - alive;
        }
    }
}
