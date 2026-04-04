using UnityEngine;

namespace ProjectSun.V2.Data
{
    [CreateAssetMenu(menuName = "ProjectSun/V2/Data/Enemy")]
    public class EnemyDataSOV2 : ScriptableObject
    {
        [Header("Identification")]
        public string enemyId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Classification")]
        public EnemyTier tier;
        public EnemyAttackRange attackRange;
        public EnemyMoveSpeed moveSpeedClass;
        public EnemyHealthClass healthClass;
        public EnemyAttackType attackType;

        [Header("Stats")]
        [Tooltip("Base HP (Larva = 30 as reference)")]
        [Min(1f)]
        public float hp = 30f;

        [Tooltip("Movement speed in units/sec (Larva = 3.0 as reference)")]
        [Min(0.1f)]
        public float moveSpeed = 3f;

        [Tooltip("Damage per hit")]
        [Min(0f)]
        public float attackDamage = 5f;

        [Tooltip("Attack range in world units")]
        [Min(0.1f)]
        public float attackRangeValue = 2f;

        [Tooltip("Seconds between attacks")]
        [Min(0.1f)]
        public float attackInterval = 1f;

        [Header("AI Behavior")]
        public EnemyTargetPriority targetPriority = EnemyTargetPriority.Nearest;

        [Header("Special Abilities")]
        public EnemySpecialAbility specialAbility;

        [Header("Visual (placeholder)")]
        [Min(0.1f)]
        public float scale = 1f;
    }
}
