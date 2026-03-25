using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 적 프리팹에 붙여서 ECS Entity로 베이크.
    /// SubScene 안의 적 프리팹 원본에 부착.
    /// </summary>
    public class EnemyPrefabAuthoring : MonoBehaviour
    {
        public EnemyType enemyType;
        public float hp = 30f;
        public float speed = 3f;
        public float damage = 5f;
        public float attackRange = 2f;
        public float attackInterval = 1f;
    }

    public class EnemyPrefabBaker : Baker<EnemyPrefabAuthoring>
    {
        public override void Bake(EnemyPrefabAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyTag());

            AddComponent(entity, new EnemyStats
            {
                MaxHP = authoring.hp,
                CurrentHP = authoring.hp,
                Speed = authoring.speed,
                Damage = authoring.damage,
                AttackRange = authoring.attackRange,
                AttackInterval = authoring.attackInterval,
                EnemyType = (int)authoring.enemyType
            });

            AddComponent(entity, new EnemyState { Value = (int)Defense.EnemyState.Moving });

            AddComponent(entity, new EnemyTarget
            {
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                HasTarget = false
            });

            AddComponent(entity, new AttackTimer { TimeSinceLastAttack = 0f });

            AddComponent(entity, new HealthBarTimer { RemainingTime = 0f });
        }
    }
}
