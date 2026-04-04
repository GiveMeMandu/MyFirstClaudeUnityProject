using System;
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
    /// 밤->낮 브릿지: ECS World에서 전투 결과를 수집하여 WaveResult로 반환.
    /// Interface Contract: CombatResult, BuildingDamageReport, DefenseResult 이행.
    /// </summary>
    public class BattleResultCollector : MonoBehaviour
    {
        // ── 보상 밸런싱 상수 (Economy-Model.md 7.1) ──────────────────────
        // SO로 분리 가능하나, M1에서는 인라인 상수로 시작.
        // 범위 값의 중간값을 기본값으로 사용.

        const int PerfectBasicMin = 10;
        const int PerfectBasicMax = 15;
        const int PerfectAdvancedMin = 4;
        const int PerfectAdvancedMax = 6;
        const float PerfectRelicChance = 0.30f;
        const int PerfectRelicAmount = 1;

        const int MinorBasicMin = 5;
        const int MinorBasicMax = 8;
        const int MinorAdvancedMin = 2;
        const int MinorAdvancedMax = 3;

        const int ModerateBasicMin = 4;
        const int ModerateBasicMax = 6;

        const int MajorBasicMin = 3;
        const int MajorBasicMax = 5;

        // ── 등급 판정 기준 (WaveDefense.md 6.2) ─────────────────────────
        const float PerfectThreshold = 0.10f;  // <= 10%
        const float MinorThreshold = 0.25f;    // <= 25%
        const float ModerateThreshold = 0.50f; // <= 50%

        /// <summary>결과 수집 소요 시간(ms).</summary>
        public long CollectTimeMs { get; private set; }

        /// <summary>
        /// 방어 결과 판정 완료 시 발행.
        /// Interface Contract: DefenseResult(damageRatio, damagedBuildings[], rewardTier).
        /// 경제 시스템이 구독하여 보상 자원 정산 + 수리비 계산을 시작한다.
        /// </summary>
        public event Action<WaveResult> OnDefenseResultPublished;

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

            // 건물 HP 수집 -> BuildingRuntimeState 갱신 (BuildingDamageReport 계약)
            CollectBuildingResults(em, gameState, result);

            // 적 처치 통계
            CollectEnemyResults(em, result);

            // 분대 피해 -> 시민 부상 (CombatResult 계약)
            CollectSquadResults(em, gameState);

            // ── 방어 등급 판정 (SF-WD-010) ────────────────────────────────
            // 피해 비율 계산: 총 피해 HP / 총 최대 HP
            // GDD(WaveDefense.md 6.1)는 "손상 건물 수 / 전체 건물 수"로 정의하나,
            // HP 기반 비율이 더 세밀한 판정을 제공하므로 HP 기반을 사용한다.
            float totalMaxHP = 0f;
            float totalDamage = 0f;
            for (int i = 0; i < gameState.buildings.Count; i++)
            {
                var b = gameState.buildings[i];
                if (b.state == BuildingSlotStateV2.Locked || b.state == BuildingSlotStateV2.Unlocked)
                    continue; // 건설되지 않은 슬롯은 판정에서 제외

                totalMaxHP += b.maxHP;
                float damage = b.maxHP - b.currentHP;
                if (damage > 0f) totalDamage += damage;
            }

            float damageRatio = totalMaxHP > 0f ? totalDamage / totalMaxHP : 0f;
            damageRatio = Mathf.Clamp01(damageRatio);

            result.damageRatio = damageRatio;
            result.grade = CalculateGrade(damageRatio);
            result.isPerfectDefense = result.grade == DefenseResultGrade.PerfectDefense;

            // ── 등급별 보상 계산 (Economy-Model.md 7.1) ───────────────────
            CalculateRewards(result);

            sw.Stop();
            CollectTimeMs = sw.ElapsedMilliseconds;

            Debug.Log($"[BattleResultCollector] Results collected in {CollectTimeMs}ms -- " +
                      $"Killed: {result.enemiesDefeated}/{result.enemiesTotal}, " +
                      $"Damaged buildings: {result.damagedBuildingSlotIds.Count}, " +
                      $"Grade: {result.grade} (damageRatio: {damageRatio:P1}), " +
                      $"Rewards: B{result.basicReward}/A{result.advancedReward}/R{result.relicReward}");

            // Interface Contract: DefenseResult 발행
            OnDefenseResultPublished?.Invoke(result);

            return result;
        }

        /// <summary>
        /// 피해 비율을 4단계 등급으로 변환.
        /// WaveDefense.md 6.2: <=10% 완벽 / <=25% 경미 / <=50% 중간 / >50% 대규모.
        /// </summary>
        static DefenseResultGrade CalculateGrade(float damageRatio)
        {
            if (damageRatio <= PerfectThreshold) return DefenseResultGrade.PerfectDefense;
            if (damageRatio <= MinorThreshold) return DefenseResultGrade.MinorDamage;
            if (damageRatio <= ModerateThreshold) return DefenseResultGrade.ModerateDamage;
            return DefenseResultGrade.MajorDamage;
        }

        /// <summary>
        /// 등급별 보상 계산.
        /// Economy-Model.md 7.1 테이블 기반.
        /// 범위 값 내에서 적 처치 비율(defenseRatio)을 보간하여 보상량을 결정한다.
        /// </summary>
        void CalculateRewards(WaveResult result)
        {
            // 적 처치 비율: 보상 범위 내 보간에 사용
            float defenseRatio = result.enemiesTotal > 0
                ? (float)result.enemiesDefeated / result.enemiesTotal
                : 1f;

            switch (result.grade)
            {
                case DefenseResultGrade.PerfectDefense:
                    result.basicReward = LerpReward(PerfectBasicMin, PerfectBasicMax, defenseRatio);
                    result.advancedReward = LerpReward(PerfectAdvancedMin, PerfectAdvancedMax, defenseRatio);
                    // 유물 확률 판정 (GDD: 30% 확률)
                    result.relicReward = UnityEngine.Random.value < PerfectRelicChance
                        ? PerfectRelicAmount
                        : 0;
                    break;

                case DefenseResultGrade.MinorDamage:
                    result.basicReward = LerpReward(MinorBasicMin, MinorBasicMax, defenseRatio);
                    result.advancedReward = LerpReward(MinorAdvancedMin, MinorAdvancedMax, defenseRatio);
                    result.relicReward = 0;
                    break;

                case DefenseResultGrade.ModerateDamage:
                    result.basicReward = LerpReward(ModerateBasicMin, ModerateBasicMax, defenseRatio);
                    result.advancedReward = 0;
                    result.relicReward = 0;
                    break;

                case DefenseResultGrade.MajorDamage:
                    result.basicReward = LerpReward(MajorBasicMin, MajorBasicMax, defenseRatio);
                    result.advancedReward = 0;
                    result.relicReward = 0;
                    break;
            }
        }

        /// <summary>
        /// min~max 범위 내에서 t(0~1)로 보간하여 정수 보상을 계산.
        /// </summary>
        static int LerpReward(int min, int max, float t)
        {
            return Mathf.RoundToInt(Mathf.Lerp(min, max, Mathf.Clamp01(t)));
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
        /// TODO: combatExpGained 구현 — 전투 참여 시민 proficiencyLevel 증가 (M2)
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
                else if (citizen.state == CitizenState.InCombat)
                {
                    // 전투 종료 후 InCombat → Idle 복귀 (다른 상태는 보존)
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
