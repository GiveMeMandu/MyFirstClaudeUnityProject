using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Encounter
{
    [Serializable]
    public class ActiveBuff
    {
        public BuffType Type;
        public float Value;
        public int RemainingTurns;
        public string SourceName;
    }

    /// <summary>
    /// 활성 버프 관리. 턴 만료 처리, 중복 시 갱신.
    /// </summary>
    public class BuffManager : MonoBehaviour
    {
        [SerializeField] private List<ActiveBuff> activeBuffs = new();

        public IReadOnlyList<ActiveBuff> ActiveBuffs => activeBuffs;

        public event Action OnBuffsChanged;

        /// <summary>
        /// 버프 추가. 같은 종류가 이미 있으면 더 강한 것으로 갱신.
        /// </summary>
        public void AddBuff(BuffType type, float value, int duration, string source)
        {
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i].Type == type)
                {
                    // 더 강한 값으로 갱신
                    if (value >= activeBuffs[i].Value)
                    {
                        activeBuffs[i].Value = value;
                        activeBuffs[i].RemainingTurns = duration;
                        activeBuffs[i].SourceName = source;
                    }
                    OnBuffsChanged?.Invoke();
                    return;
                }
            }

            activeBuffs.Add(new ActiveBuff
            {
                Type = type,
                Value = value,
                RemainingTurns = duration,
                SourceName = source
            });
            OnBuffsChanged?.Invoke();
        }

        /// <summary>
        /// 턴 시작 시 호출: 모든 버프 턴 감소, 만료 제거.
        /// </summary>
        public void ProcessTurn()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].RemainingTurns--;
                if (activeBuffs[i].RemainingTurns <= 0)
                {
                    activeBuffs.RemoveAt(i);
                }
            }
            OnBuffsChanged?.Invoke();
        }

        /// <summary>
        /// 특정 버프의 현재 값 반환 (없으면 0)
        /// </summary>
        public float GetBuffValue(BuffType type)
        {
            foreach (var buff in activeBuffs)
            {
                if (buff.Type == type) return buff.Value;
            }
            return 0f;
        }

        /// <summary>
        /// 특정 버프가 활성 중인지
        /// </summary>
        public bool HasBuff(BuffType type)
        {
            foreach (var buff in activeBuffs)
            {
                if (buff.Type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// 생산력 보너스 배율 (1.0 = 보너스 없음, 1.2 = 20% 증가)
        /// </summary>
        public float ProductionMultiplier => 1f + GetBuffValue(BuffType.ProductionBonus);

        /// <summary>
        /// 공격력 보너스 배율
        /// </summary>
        public float AttackMultiplier => 1f + GetBuffValue(BuffType.AttackBonus);

        /// <summary>
        /// 방어 자원 소모 배율 (1.0 = 정상, 0.5 = 절반)
        /// </summary>
        public float DefenseResourceMultiplier =>
            HasBuff(BuffType.DefenseResourceHalf) ? 0.5f : 1f;
    }
}
