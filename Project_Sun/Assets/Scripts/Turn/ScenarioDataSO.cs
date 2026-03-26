using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Turn
{
    /// <summary>
    /// 턴별 인카운터 설정.
    /// 배열 인덱스 = 턴 번호 - 1 (즉, [0]은 1턴, [1]은 2턴...).
    /// </summary>
    [Serializable]
    public struct TurnEncounterConfig
    {
        [Tooltip("이 턴의 인카운터 유형\n" +
                 "- None: 아무 일 없음\n" +
                 "- FixedBattle: 반드시 전투 발생\n" +
                 "- RandomEncounter: 확률 테이블에 따라 결정")]
        public EncounterType encounterType;

        [Tooltip("RandomEncounter일 경우 사용할 확률 테이블 (null이면 기본값 사용)")]
        public EncounterDataSO encounterData;
    }

    /// <summary>
    /// 시나리오 전체 정의.
    /// turnSchedule 배열로 각 턴의 인카운터를 직접 지정.
    /// 배열의 [0] = 1턴, [1] = 2턴, ..., [N-1] = N턴.
    /// </summary>
    [CreateAssetMenu(fileName = "NewScenario", menuName = "ProjectSun/Turn/Scenario Data")]
    public class ScenarioDataSO : ScriptableObject
    {
        [Header("시나리오 기본 설정")]
        public string scenarioName;

        [Header("기본 인카운터 설정")]
        [Tooltip("turnSchedule에 설정이 없거나 RandomEncounter의 encounterData가 null일 때 사용")]
        public EncounterDataSO defaultEncounterData;

        [Header("턴별 스케줄")]
        [Tooltip("각 턴의 인카운터를 순서대로 정의.\n" +
                 "배열 크기 = 총 턴 수.\n" +
                 "[0] = 1턴, [1] = 2턴, ...")]
        public List<TurnEncounterConfig> turnSchedule = new();

        /// <summary>
        /// 총 턴 수 (배열 크기로 결정)
        /// </summary>
        public int TotalTurns => turnSchedule.Count;

        /// <summary>
        /// 주어진 턴의 인카운터 유형과 데이터를 반환 (1-based)
        /// </summary>
        public (EncounterType type, EncounterDataSO data) GetTurnEncounter(int turnNumber)
        {
            int index = turnNumber - 1;
            if (index >= 0 && index < turnSchedule.Count)
            {
                var config = turnSchedule[index];
                return (config.encounterType, config.encounterData ?? defaultEncounterData);
            }

            // 범위 밖이면 랜덤 인카운터
            return (EncounterType.RandomEncounter, defaultEncounterData);
        }

        /// <summary>
        /// 기본 시나리오 자동 생성 (테스트용, 15턴)
        /// </summary>
        public void GenerateDefaultScenario(EncounterDataSO defaultEncounter)
        {
            scenarioName = "Test Scenario";
            defaultEncounterData = defaultEncounter;

            turnSchedule.Clear();

            // 15턴 스케줄: 턴 3, 6, 9, 12, 15에 고정 전투, 나머지 랜덤
            var battleTurns = new HashSet<int> { 3, 6, 9, 12, 15 };

            for (int turn = 1; turn <= 15; turn++)
            {
                turnSchedule.Add(new TurnEncounterConfig
                {
                    encounterType = battleTurns.Contains(turn)
                        ? EncounterType.FixedBattle
                        : EncounterType.RandomEncounter,
                    encounterData = null // 기본값 사용
                });
            }
        }
    }
}
