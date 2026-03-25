using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Turn
{
    /// <summary>
    /// 랜덤 인카운터 이벤트 정의
    /// </summary>
    [Serializable]
    public struct EncounterEvent
    {
        public string eventName;
        [TextArea(1, 3)]
        public string description;
        [Tooltip("이벤트 발생 가중치 (높을수록 자주 발생)")]
        [Min(0f)]
        public float weight;
        [Tooltip("이 이벤트가 전투를 트리거하는지")]
        public bool triggersBattle;
    }

    /// <summary>
    /// 인카운터 확률 테이블 정의
    /// </summary>
    [CreateAssetMenu(fileName = "NewEncounterData", menuName = "ProjectSun/Turn/Encounter Data")]
    public class EncounterDataSO : ScriptableObject
    {
        [Header("랜덤 인카운터 확률")]
        [Tooltip("전투 발생 확률 (0~1)")]
        [Range(0f, 1f)]
        public float battleChance = 0.3f;

        [Tooltip("이벤트 발생 확률 (0~1). 나머지 = 아무 일 없음")]
        [Range(0f, 1f)]
        public float eventChance = 0.25f;

        [Header("이벤트 풀")]
        public List<EncounterEvent> eventPool = new();

        /// <summary>
        /// 랜덤 인카운터 결과 결정
        /// </summary>
        public RandomEncounterResult Roll()
        {
            float roll = UnityEngine.Random.value;
            if (roll < battleChance)
                return RandomEncounterResult.Battle;
            if (roll < battleChance + eventChance)
                return RandomEncounterResult.Event;
            return RandomEncounterResult.Nothing;
        }

        /// <summary>
        /// 이벤트 풀에서 가중치 기반 랜덤 이벤트 선택
        /// </summary>
        public EncounterEvent? PickRandomEvent()
        {
            if (eventPool.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var e in eventPool)
                totalWeight += e.weight;

            if (totalWeight <= 0f) return eventPool[0];

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var e in eventPool)
            {
                cumulative += e.weight;
                if (roll <= cumulative)
                    return e;
            }

            return eventPool[eventPool.Count - 1];
        }
    }
}
