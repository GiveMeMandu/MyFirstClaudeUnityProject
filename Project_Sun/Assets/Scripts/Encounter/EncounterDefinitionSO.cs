using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Encounter
{
    /// <summary>
    /// 선택지의 개별 효과
    /// </summary>
    [Serializable]
    public struct ChoiceEffect
    {
        public EffectType effectType;

        [Header("자원 변동 (ResourceChange)")]
        [Tooltip("basic, advanced, defense")]
        public string resourceId;
        public int resourceAmount;

        [Header("인력 변동 (WorkerChange/WorkerInjury)")]
        public int workerAmount;

        [Header("버프 (Buff)")]
        public BuffType buffType;
        [Tooltip("버프 효과량 (예: 0.2 = 20%)")]
        public float buffValue;
        [Tooltip("버프 지속 턴 수")]
        public int buffDuration;
    }

    /// <summary>
    /// 하나의 선택지
    /// </summary>
    [Serializable]
    public struct EncounterChoice
    {
        public string choiceText;

        [Header("비용 (부족 시 선택 불가)")]
        [Tooltip("필요 자원 ID (빈 문자열이면 비용 없음)")]
        public string costResourceId;
        public int costAmount;

        [Header("조건 (미충족 시 선택 불가)")]
        [Tooltip("필요 건물 이름 (빈 문자열이면 조건 없음)")]
        public string requiredBuildingName;

        [Header("효과")]
        public List<ChoiceEffect> effects;
    }

    /// <summary>
    /// 개별 인카운터 정의 SO
    /// </summary>
    [CreateAssetMenu(fileName = "NewEncounter", menuName = "ProjectSun/Encounter/Encounter Definition")]
    public class EncounterDefinitionSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string encounterName;
        [TextArea(2, 5)]
        public string description;
        public EncounterCategory category;

        [Header("선택지")]
        [Tooltip("일상: 2개, 중요: 3개")]
        public List<EncounterChoice> choices = new();

        [Header("풀 가중치")]
        [Min(0.1f)]
        public float weight = 1f;
    }
}
