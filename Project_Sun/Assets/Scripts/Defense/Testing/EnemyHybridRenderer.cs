using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectSun.Defense.ECS;
using UnityEngine;

namespace ProjectSun.Defense.Testing
{
    /// <summary>
    /// ECS 적 Entity를 GameObject 풀로 시각화하는 하이브리드 렌더러.
    /// PoC용 — 프로덕션에서는 Entities Graphics 사용.
    /// </summary>
    public class EnemyHybridRenderer : MonoBehaviour
    {
        [Header("렌더링 설정")]
        [SerializeField] private int maxVisualEnemies = 1500;
        [SerializeField] private Mesh enemyMesh;
        [SerializeField] private Material basicEnemyMaterial;
        [SerializeField] private Material heavyEnemyMaterial;
        [SerializeField] private Material flyingEnemyMaterial;

        [Header("크기 설정")]
        [SerializeField] private float basicScale = 0.5f;
        [SerializeField] private float heavyScale = 1.2f;
        [SerializeField] private float flyingScale = 0.4f;

        // GPU Instancing용 매트릭스 배열
        private Matrix4x4[] basicMatrices;
        private Matrix4x4[] heavyMatrices;
        private Matrix4x4[] flyingMatrices;
        private int basicCount;
        private int heavyCount;
        private int flyingCount;

        private EntityManager entityManager;
        private const int BatchSize = 1023; // DrawMeshInstanced 최대 배치 크기

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
                entityManager = world.EntityManager;

            if (enemyMesh == null)
                enemyMesh = CreateDefaultMesh();

            basicMatrices = new Matrix4x4[maxVisualEnemies];
            heavyMatrices = new Matrix4x4[maxVisualEnemies];
            flyingMatrices = new Matrix4x4[maxVisualEnemies];
        }

        private void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            CollectEnemyTransforms();
            DrawEnemies();
        }

        private void CollectEnemyTransforms()
        {
            basicCount = 0;
            heavyCount = 0;
            flyingCount = 0;

            var query = entityManager.CreateEntityQuery(
                typeof(EnemyTag), typeof(EnemyStats), typeof(LocalTransform));

            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (entityManager.HasComponent<DeadTag>(entities[i])) continue;

                var stats = entityManager.GetComponentData<EnemyStats>(entities[i]);
                var localTransform = entityManager.GetComponentData<LocalTransform>(entities[i]);

                var pos = new Vector3(localTransform.Position.x, localTransform.Position.y, localTransform.Position.z);
                var rot = localTransform.Rotation;
                var rotation = new Quaternion(rot.value.x, rot.value.y, rot.value.z, rot.value.w);

                switch (stats.EnemyType)
                {
                    case 0: // Basic
                        if (basicCount < maxVisualEnemies)
                        {
                            basicMatrices[basicCount] = Matrix4x4.TRS(pos, rotation, Vector3.one * basicScale);
                            basicCount++;
                        }
                        break;
                    case 1: // Heavy
                        if (heavyCount < maxVisualEnemies)
                        {
                            heavyMatrices[heavyCount] = Matrix4x4.TRS(pos, rotation, Vector3.one * heavyScale);
                            heavyCount++;
                        }
                        break;
                    case 2: // Flying
                        if (flyingCount < maxVisualEnemies)
                        {
                            flyingMatrices[flyingCount] = Matrix4x4.TRS(pos, rotation, Vector3.one * flyingScale);
                            flyingCount++;
                        }
                        break;
                }
            }

            entities.Dispose();
        }

        private void DrawEnemies()
        {
            DrawBatched(basicMatrices, basicCount, basicEnemyMaterial);
            DrawBatched(heavyMatrices, heavyCount, heavyEnemyMaterial);
            DrawBatched(flyingMatrices, flyingCount, flyingEnemyMaterial);
        }

        private void DrawBatched(Matrix4x4[] matrices, int count, Material material)
        {
            if (count == 0 || material == null || enemyMesh == null) return;

            for (int offset = 0; offset < count; offset += BatchSize)
            {
                int batchCount = Mathf.Min(BatchSize, count - offset);

                // DrawMeshInstanced는 최대 1023개 배치 가능
                var batch = new Matrix4x4[batchCount];
                System.Array.Copy(matrices, offset, batch, 0, batchCount);

                Graphics.DrawMeshInstanced(enemyMesh, 0, material, batch, batchCount);
            }
        }

        private Mesh CreateDefaultMesh()
        {
            // 기본 큐브 메쉬를 절차적으로 생성
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            return mesh;
        }
    }
}
