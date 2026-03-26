using System;
using System.Collections;
using System.Collections.Generic;
using ProjectSun.Construction;
using ProjectSun.Defense;
using ProjectSun.Encounter;
using ProjectSun.Exploration;
using ProjectSun.Policy;
using ProjectSun.Resource;
using ProjectSun.Workforce;
using UnityEngine;

namespace ProjectSun.Turn
{
    /// <summary>
    /// 턴 시스템 중앙 오케스트레이터.
    /// 낮→밤→다음 낮 루프를 관리하고, 모든 시스템의 턴 처리를 조율.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        [Header("시나리오")]
        [SerializeField] private ScenarioDataSO scenarioData;

        [Header("연동")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private WorkforceManager workforceManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private EncounterManager encounterManager;
        [SerializeField] private BuffManager buffManager;
        [SerializeField] private ExplorationManager explorationManager;
        [SerializeField] private PolicyManager policyManager;
        [SerializeField] private ScreenFader screenFader;
        [SerializeField] private ToastMessage toastMessage;

        [Header("페이드 설정")]
        [SerializeField] private float fadeToNightDuration = 1.5f;
        [SerializeField] private float fadeToDayDuration = 2.0f;
        [SerializeField] private float nightAmbientIntensity = 0.3f;
        [SerializeField] private float nightSkipDelay = 1.5f;

        [Header("런타임 상태")]
        [SerializeField] private int currentTurn = 1;
        [SerializeField] private TurnPhase currentPhase = TurnPhase.DayPhase;

        public int CurrentTurn => currentTurn;
        public TurnPhase CurrentPhase => currentPhase;
        public int TotalTurns => scenarioData != null ? scenarioData.TotalTurns : 20;
        public bool IsProcessing => currentPhase != TurnPhase.DayPhase && currentPhase != TurnPhase.GameOver;

        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<int> OnTurnChanged;
        public event Action<GameOverReason> OnGameOver;

        private bool battleInProgress;
        private BattleStatisticsData lastBattleStats;

        private void Start()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleEnded += HandleBattleEnded;
                battleManager.OnBattleStateChanged += HandleBattleStateChanged;
            }

            SetPhase(TurnPhase.DayPhase);
        }

        /// <summary>
        /// 턴 종료 버튼에서 호출. 낮→밤 전환 시작.
        /// </summary>
        public void EndTurn()
        {
            if (currentPhase != TurnPhase.DayPhase) return;

            // 방어 자원 부족 체크 (턴 종료 차단)
            if (resourceManager != null && !resourceManager.HasEnoughDefenseResource())
            {
                int cost = resourceManager.CalculateDefenseResourceCost();
                if (toastMessage != null)
                {
                    toastMessage.Show("방어 자원 부족",
                        $"방어 자원이 부족합니다.\n필요: {cost} / 보유: {resourceManager.DefenseResource}\n방어 인력을 조정해주세요.");
                }
                return; // 턴 종료 차단
            }

            // 미배치 인력 경고 (토스트로 표시, 진행은 허용)
            if (workforceManager != null && workforceManager.HasIdleWorkers && toastMessage != null)
            {
                toastMessage.Show("미배치 인력 경고",
                    $"배치되지 않은 인력이 {workforceManager.IdleCount}명 있습니다.\n턴을 종료하시겠습니까?");
                StartCoroutine(WaitForWarningThenProcess());
                return;
            }

            StartCoroutine(ProcessTurnTransition());
        }

        private IEnumerator WaitForWarningThenProcess()
        {
            yield return new WaitUntil(() => !toastMessage.IsVisible);
            StartCoroutine(ProcessTurnTransition());
        }

        private IEnumerator ProcessTurnTransition()
        {
            // ── 낮 종료 처리 ──
            SetPhase(TurnPhase.DayEnd);

            lastProductionSummary = null;
            ProcessDayEndEffects();

            // 원정대 이동 처리 (턴 종료 시)
            if (explorationManager != null)
            {
                explorationManager.ProcessTurnMovement();
            }

            // 생산 결과 토스트
            if (!string.IsNullOrEmpty(lastProductionSummary) && toastMessage != null)
            {
                toastMessage.Show("자원 생산", lastProductionSummary);
                yield return new WaitUntil(() => !toastMessage.IsVisible);
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // ── 낮→밤 페이드 ──
            SetPhase(TurnPhase.FadeToNight);

            if (screenFader != null)
            {
                yield return screenFader.FadeToNight(fadeToNightDuration, nightAmbientIntensity);
            }
            else
            {
                yield return new WaitForSecondsRealtime(fadeToNightDuration);
            }

            // ── 밤 페이즈: 인카운터 결정 ──
            SetPhase(TurnPhase.NightPhase);

            yield return ProcessNightEncounter();

            // ── 밤 종료 ──
            SetPhase(TurnPhase.NightEnd);

            // 전투 결과 통계 토스트
            if (lastBattleStats != null)
            {
                string statsText = $"처치: {lastBattleStats.TotalKilled}/{lastBattleStats.TotalSpawned}\n" +
                                   $"건물 피해: {lastBattleStats.TotalDamageToBuildings:F0}";
                if (toastMessage != null)
                {
                    toastMessage.Show("전투 결과", statsText);
                    yield return new WaitUntil(() => !toastMessage.IsVisible);
                }
                lastBattleStats = null;
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // ── 밤→낮 페이드 ──
            SetPhase(TurnPhase.FadeToDay);

            if (screenFader != null)
            {
                yield return screenFader.FadeToDay(fadeToDayDuration);
            }
            else
            {
                yield return new WaitForSecondsRealtime(fadeToDayDuration);
            }

            // ── 턴 증가 + 클리어 체크 ──
            currentTurn++;
            OnTurnChanged?.Invoke(currentTurn);

            if (currentTurn > TotalTurns)
            {
                SetPhase(TurnPhase.GameOver);
                OnGameOver?.Invoke(GameOverReason.Victory);
                yield break;
            }

            // ── 다음 낮 시작 처리 ──
            SetPhase(TurnPhase.DayStart);
            ProcessDayStartEffects();

            // ── 정책 해금 체크 (낮 시작 시) ──
            if (policyManager != null)
            {
                policyManager.OnNewTurn(currentTurn);
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // ── 탐사 도착 이벤트 처리 (낮 시작 시) ──
            if (explorationManager != null)
            {
                yield return ProcessExplorationArrivals();
            }

            // ── 일상 인카운터 (낮 시작 시) ──
            if (encounterManager != null && encounterManager.TryTriggerDailyEncounter())
            {
                yield return new WaitUntil(() => !encounterManager.IsWaitingForChoice);
            }

            // ── 낮 페이즈 복귀 ──
            SetPhase(TurnPhase.DayPhase);
        }

        private IEnumerator ProcessNightEncounter()
        {
            if (scenarioData == null)
            {
                yield return new WaitForSecondsRealtime(nightSkipDelay);
                yield break;
            }

            var (encounterType, encounterData) = scenarioData.GetTurnEncounter(currentTurn);

            switch (encounterType)
            {
                case EncounterType.FixedBattle:
                    yield return ProcessBattle();
                    break;

                case EncounterType.RandomEncounter:
                    yield return ProcessRandomEncounter(encounterData);
                    break;

                case EncounterType.None:
                    if (toastMessage != null)
                    {
                        toastMessage.Show("평화로운 밤", "아무 일도 일어나지 않았다.");
                        yield return new WaitUntil(() => !toastMessage.IsVisible);
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(nightSkipDelay);
                    }
                    break;
            }
        }

        private IEnumerator ProcessRandomEncounter(EncounterDataSO encounterData)
        {
            if (encounterData == null)
            {
                yield return new WaitForSecondsRealtime(nightSkipDelay);
                yield break;
            }

            var result = encounterData.Roll();

            switch (result)
            {
                case RandomEncounterResult.Battle:
                    if (toastMessage != null)
                    {
                        toastMessage.Show("적 습격!", "적의 공격이 시작됩니다!");
                        yield return new WaitUntil(() => !toastMessage.IsVisible);
                    }
                    yield return ProcessBattle();
                    break;

                case RandomEncounterResult.Event:
                    var evt = encounterData.PickRandomEvent();
                    if (evt.HasValue)
                    {
                        if (toastMessage != null)
                        {
                            toastMessage.Show(evt.Value.eventName, evt.Value.description);
                            yield return new WaitUntil(() => !toastMessage.IsVisible);
                        }

                        if (evt.Value.triggersBattle)
                        {
                            yield return ProcessBattle();
                        }
                    }
                    break;

                case RandomEncounterResult.Nothing:
                    if (toastMessage != null)
                    {
                        toastMessage.Show("평화로운 밤", "아무 일도 일어나지 않았다.");
                        yield return new WaitUntil(() => !toastMessage.IsVisible);
                    }
                    else
                    {
                        yield return new WaitForSecondsRealtime(nightSkipDelay);
                    }
                    break;
            }
        }

        private IEnumerator ProcessBattle()
        {
            if (battleManager == null) yield break;

            // 전투 시작 시 방어 자원 차감
            resourceManager?.DeductDefenseResource();

            battleInProgress = true;
            lastBattleStats = null;
            battleManager.StartBattle();

            // 전투 종료 대기
            yield return new WaitUntil(() => !battleInProgress);

            // 전투 보상 (PoC: 고정 보상)
            if (lastBattleStats != null && lastBattleStats.TotalKilled > 0)
            {
                int basicReward = lastBattleStats.TotalKilled * 2;
                int advancedReward = lastBattleStats.TotalKilled / 5;
                resourceManager?.GrantBattleReward(basicReward, advancedReward);
            }

            // 다음 전투를 위해 Idle 상태로 리셋
            battleManager.ResetToIdle();
        }

        private void HandleBattleEnded(BattleStatisticsData stats)
        {
            lastBattleStats = stats;
            battleInProgress = false;
        }

        private void HandleBattleStateChanged(BattleState state)
        {
            if (state == BattleState.Defeat)
            {
                battleInProgress = false;
                StopAllCoroutines();
                SetPhase(TurnPhase.GameOver);
                OnGameOver?.Invoke(GameOverReason.Defeat);
            }
        }

        /// <summary>
        /// 탐사 도착 이벤트를 순차 처리
        /// </summary>
        private IEnumerator ProcessExplorationArrivals()
        {
            while (explorationManager.HasPendingArrivals)
            {
                var arrival = explorationManager.DequeueArrival();
                if (!arrival.HasValue) break;

                var nodeData = explorationManager.GetNodeData(arrival.Value.nodeIndex);
                if (nodeData == null) continue;

                switch (nodeData.nodeType)
                {
                    case Exploration.ExplorationNodeType.Resource:
                        // 자원 보상 지급
                        if (resourceManager != null)
                        {
                            var lines = new List<string>();
                            foreach (var reward in nodeData.resourceRewards)
                            {
                                var resType = ParseResourceType(reward.resourceId);
                                resourceManager.AddResource(resType, reward.amount);
                                lines.Add($"{resType}: +{reward.amount}");
                            }
                            if (toastMessage != null && lines.Count > 0)
                            {
                                toastMessage.Show($"탐사 발견: {nodeData.nodeName}",
                                    string.Join("\n", lines));
                                yield return new WaitUntil(() => !toastMessage.IsVisible);
                            }
                        }
                        break;

                    case Exploration.ExplorationNodeType.Recon:
                        // 정찰 정보 토스트 (추후 웨이브 정보 연동)
                        if (toastMessage != null)
                        {
                            toastMessage.Show($"정찰 정보: {nodeData.nodeName}",
                                $"{nodeData.reconTurnsAhead}턴 후 웨이브 정보를 획득했습니다.\n{nodeData.description}");
                            yield return new WaitUntil(() => !toastMessage.IsVisible);
                        }
                        break;

                    case Exploration.ExplorationNodeType.Encounter:
                        // 인카운터 발생
                        if (encounterManager != null && nodeData.encounterDefinition != null)
                        {
                            encounterManager.ShowEncounter(nodeData.encounterDefinition);
                            yield return new WaitUntil(() => !encounterManager.IsWaitingForChoice);
                        }
                        break;

                    case Exploration.ExplorationNodeType.Tech:
                        // 기술 해금 토스트 (추후 연동)
                        if (toastMessage != null)
                        {
                            toastMessage.Show($"기술 발견: {nodeData.nodeName}",
                                $"기술 해금 아이템을 획득했습니다.\n{nodeData.description}");
                            yield return new WaitUntil(() => !toastMessage.IsVisible);
                        }
                        break;
                }
            }
        }

        private Resource.ResourceType ParseResourceType(string id)
        {
            if (string.IsNullOrEmpty(id)) return Resource.ResourceType.Basic;
            return id.ToLower() switch
            {
                "basic" => Resource.ResourceType.Basic,
                "advanced" => Resource.ResourceType.Advanced,
                "defense" => Resource.ResourceType.Defense,
                _ => Resource.ResourceType.Basic
            };
        }

        /// <summary>
        /// 낮 종료 시 처리: 자원 생산 → 손상 건물 자동 회복
        /// </summary>
        private void ProcessDayEndEffects()
        {
            // 자원 생산 (인력 배치된 자원 건물)
            if (resourceManager != null)
            {
                var produced = resourceManager.ProcessProduction();

                // 생산 결과 토스트
                if (toastMessage != null)
                {
                    var lines = new List<string>();
                    foreach (var kvp in produced)
                    {
                        if (kvp.Value > 0)
                            lines.Add($"{kvp.Key}: +{kvp.Value}");
                    }
                    if (lines.Count > 0)
                    {
                        lastProductionSummary = string.Join("\n", lines);
                    }
                }
            }

            // 손상 건물 자동 회복 (턴 종료 시, 밤 전환 전)
            if (buildingManager != null)
            {
                buildingManager.ProcessAutoRepair();
            }
        }

        private string lastProductionSummary;

        /// <summary>
        /// 다음 낮 시작 시 처리: 건설 진행도 증가, 부상 회복, 버프 턴 감소
        /// </summary>
        private void ProcessDayStartEffects()
        {
            if (buildingManager != null)
            {
                buildingManager.ProcessTurn();
            }

            if (workforceManager != null)
            {
                workforceManager.ProcessHealingTurn();
            }

            if (buffManager != null)
            {
                buffManager.ProcessTurn();
            }
        }

        private void SetPhase(TurnPhase phase)
        {
            currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleEnded -= HandleBattleEnded;
                battleManager.OnBattleStateChanged -= HandleBattleStateChanged;
            }
        }
    }
}
