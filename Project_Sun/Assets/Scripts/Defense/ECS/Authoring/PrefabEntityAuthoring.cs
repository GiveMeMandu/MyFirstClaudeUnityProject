using Unity.Entities;
using UnityEngine;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// ECS 프리팹 참조 컴포넌트.
    /// 적 종류별 베이크된 프리팹 Entity를 BattleManager가 찾을 수 있게 함.
    /// </summary>
    public struct PrefabEntityReference : IComponentData
    {
        public Entity PrefabEntity;
        public int EnemyType;
    }

    /// <summary>
    /// SubScene에 배치하여 적 프리팹을 ECS Entity 참조로 변환.
    /// 각 적 종류별 하나씩 배치.
    /// </summary>
    public class PrefabEntityAuthoring : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public EnemyType enemyType;
    }

    public class PrefabEntityBaker : Baker<PrefabEntityAuthoring>
    {
        public override void Bake(PrefabEntityAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PrefabEntityReference
            {
                PrefabEntity = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                EnemyType = (int)authoring.enemyType
            });
        }
    }
}
