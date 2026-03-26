using Unity.Entities;
using UnityEngine;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// 스폰 포인트를 정의하는 오소링 컴포넌트.
    /// 맵 외곽에 배치하여 적 스폰 위치로 사용.
    /// </summary>
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        [Tooltip("스폰 포인트 인덱스 (여러 스폰 포인트 구분용)")]
        public int spawnPointIndex;
    }

    public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpawnPoint { Index = authoring.spawnPointIndex });
        }
    }
}
