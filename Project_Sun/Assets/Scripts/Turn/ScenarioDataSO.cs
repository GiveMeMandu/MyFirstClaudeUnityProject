using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Turn
{
    /// <summary>
    /// 턴별 인카운터 설정
    /// </summary>
    [Serializable]
    public struct TurnEncounterConfig
    {
        [Tooltip("이 설정이 적용되는 턴 번호 (1부터 시작)")]
        [Min(1)]
        public int turnNumber;

        [Tooltip("이 턴의 인카운터 유형")]
        public EncounterType encounterType;

        [Tooltip("랜덤 인카운터일 경우 사용할 확률 테이블 (null이면 기본값)")]
        public EncounterDataSO encounterData;
    }

    /// <summary>
    /// 시나리오 전체 정의.
    /// 총 턴 수, 턴별 인카운터 유형, 기본 인카운터 확률 등을 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "NewScenario", menuName = "ProjectSun/Turn/Scenario Data")]
    public class ScenarioDataSO : ScriptableObject
    {
        [Header("시나리오 기본 설정")]
        public string scenarioName;

        [Tooltip("총 턴 수 (이 턴까지 생존하면 클리어)")]
        [Min(1)]
        public int totalTurns = 20;

        [Header("기본 인카운터 설정")]
        [Tooltip("턴별 설정이 없는 턴에 적용되는 기본 인카운터 데이터")]
        public EncounterDataSO defaultEncounterData;

        [Header("턴별 인카운터 설정")]
        [Tooltip("특정 턴에 대한 인카운터 오버라이드 (고정 전투 등)")]
        public List<TurnEncounterConfig> turnConfigs = new();

        /// <summary>
        /// 주어진 턴의 인카운터 유형과 데이터를 반환
        /// </summary>
        public (EncounterType type, EncounterDataSO data) GetTurnEncounter(int turnNumber)
        {
            // 턴별 설정 우선
            foreach (var config in turnConfigs)
            {
                if (config.turnNumber == turnNumber)
                {
                    return (config.encounterType, config.encounterData ?? defaultEncounterData);
                }
            }

            // 기본: 랜덤 인카운터
            return (EncounterType.RandomEncounter, defaultEncounterData);
        }

        /// <summary>
        /// 기본 시나리오 자동 생성 (테스트용)
        /// </summary>
        public void GenerateDefaultScenario(EncounterDataSO defaultEncounter)
        {
            scenarioName = "Test Scenario";
            totalTurns = 15;
            defaultEncounterData = defaultEncounter;

            turnConfigs.Clear();

            // 턴 3, 6, 9, 12, 15에 고정 전투
            int[] battleTurns = { 3, 6, 9, 12, 15 };
            foreach (int t in battleTurns)
            {
                turnConfigs.Add(new TurnEncounterConfig
                {
                    turnNumber = t,
                    encounterType = EncounterType.FixedBattle
                });
            }
        }
    }
}
