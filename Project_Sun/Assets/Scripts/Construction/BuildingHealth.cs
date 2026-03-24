using System;
using UnityEngine;

namespace ProjectSun.Construction
{
    public class BuildingHealth : MonoBehaviour
    {
        [SerializeField] private float currentHP;
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float autoRepairRate = 25f;

        public float CurrentHP => currentHP;
        public float MaxHP => maxHP;
        public float HPRatio => maxHP > 0 ? currentHP / maxHP : 0f;
        public bool IsDestroyed => currentHP <= 0f;
        public bool IsDamaged => currentHP > 0f && currentHP < maxHP;
        public bool IsFullHealth => currentHP >= maxHP;

        public event Action OnDestroyed;
        public event Action OnRepaired;
        public event Action<float> OnDamaged;

        public void Initialize(float max, float repairRate)
        {
            maxHP = max;
            autoRepairRate = repairRate;
            currentHP = maxHP;
        }

        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            float previous = currentHP;
            currentHP = Mathf.Max(0f, currentHP - damage);
            OnDamaged?.Invoke(damage);

            if (currentHP <= 0f && previous > 0f)
            {
                OnDestroyed?.Invoke();
            }
        }

        /// <summary>
        /// Apply auto repair at day start. Only when damaged (HP > 0).
        /// </summary>
        public void ApplyAutoRepair()
        {
            if (IsDestroyed || IsFullHealth) return;

            currentHP = Mathf.Min(maxHP, currentHP + autoRepairRate);

            if (IsFullHealth)
            {
                OnRepaired?.Invoke();
            }
        }

        /// <summary>
        /// Full restore after repair completion.
        /// </summary>
        public void FullRestore()
        {
            currentHP = maxHP;
            OnRepaired?.Invoke();
        }
    }
}
