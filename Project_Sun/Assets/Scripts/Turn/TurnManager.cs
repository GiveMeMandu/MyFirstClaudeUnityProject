using System;
using System.Collections;
using ProjectSun.Construction;
using ProjectSun.Defense;
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
            StartCoroutine(ProcessTurnTransition());
        }

        private IEnumerator ProcessTurnTransition()
        {
            // ── 낮 종료 처리 ──
            SetPhase(TurnPhase.DayEnd);

            // 자원 생산 (추후 구현)
            // 손상 건물 자동 회복은 BuildingManager.ProcessTurn()에 포함되어 있으나
            // 건설 진행도와 분리하여 여기서 회복만 먼저 처리
            ProcessDayEndEffects();

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

            yield return new WaitForSecondsRealtime(0.2f);

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

            battleInProgress = true;
            lastBattleStats = null;
            battleManager.StartBattle();

            // 전투 종료 대기
            yield return new WaitUntil(() => !battleInProgress);

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
        /// 낮 종료 시 처리: 손상 건물 자동 회복 (건설 진행도는 제외)
        /// </summary>
        private void ProcessDayEndEffects()
        {
            // 자원 생산 (추후 자원 시스템 연동)
            // 손상 자동 회복은 ProcessTurn()에 포함 — 다음 낮 시작에서 호출
        }

        /// <summary>
        /// 다음 낮 시작 시 처리: 건설 진행도 증가, 완성 건물 활성화
        /// </summary>
        private void ProcessDayStartEffects()
        {
            if (buildingManager != null)
            {
                buildingManager.ProcessTurn();
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
