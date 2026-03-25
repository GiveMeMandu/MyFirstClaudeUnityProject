using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectSun.Defense.ECS;
using UnityEngine;

namespace ProjectSun.Defense.Testing
{
    /// <summary>
    /// 타워 공격을 시각화하는 PoC 렌더러.
    /// 타워가 공격할 때 타워→적 방향으로 짧은 라인을 표시.
    /// </summary>
    public class TowerProjectileRenderer : MonoBehaviour
    {
        [Header("시각화 설정")]
        [SerializeField] private Color lineColor = new(1f, 1f, 0.2f, 0.8f);
        [SerializeField] private float lineDuration = 0.1f;
        [SerializeField] private Material lineMaterial;

        private struct AttackLine
        {
            public Vector3 From;
            public Vector3 To;
            public float Timer;
        }

        private AttackLine[] activeLines;
        private int lineCount;
        private const int MaxLines = 64;

        private EntityManager entityManager;

        private void Start()
        {
            activeLines = new AttackLine[MaxLines];

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
                entityManager = world.EntityManager;

            if (lineMaterial == null)
            {
                // 기본 Unlit 라인 머티리얼
                lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                lineMaterial.SetInt("_ZWrite", 0);
                lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
        }

        private void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            CollectAttackEvents();
            UpdateTimers();
        }

        private void CollectAttackEvents()
        {
            // 공격 중인 타워와 가장 가까운 적 사이에 라인 표시
            // TowerAttackTimer가 0에 가까운 타워 = 방금 공격한 타워

            int enemyCount = 0;
            foreach (var _ in SystemAPI_Substitute_EnemyCount()) { enemyCount++; }
            if (enemyCount == 0) return;

            var enemyPositions = new NativeList<float3>(Allocator.Temp);
            var enemyTypes = new NativeList<int>(Allocator.Temp);

            var query = entityManager.CreateEntityQuery(
                typeof(EnemyTag), typeof(EnemyStats), typeof(LocalTransform));
            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (entityManager.HasComponent<DeadTag>(entities[i])) continue;
                var t = entityManager.GetComponentData<LocalTransform>(entities[i]);
                var s = entityManager.GetComponentData<EnemyStats>(entities[i]);
                enemyPositions.Add(t.Position);
                enemyTypes.Add(s.EnemyType);
            }
            entities.Dispose();

            // 타워 쿼리
            var towerQuery = entityManager.CreateEntityQuery(
                typeof(TowerTag), typeof(TowerStats), typeof(TowerAttackTimer),
                typeof(LocalTransform), typeof(ECS.BuildingData));
            var towerEntities = towerQuery.ToEntityArray(Allocator.Temp);

            for (int t = 0; t < towerEntities.Length; t++)
            {
                var timer = entityManager.GetComponentData<TowerAttackTimer>(towerEntities[t]);
                var bData = entityManager.GetComponentData<ECS.BuildingData>(towerEntities[t]);

                // 파괴된 건물 무시
                if (bData.CurrentHP <= 0f) continue;

                var towerStats = entityManager.GetComponentData<TowerStats>(towerEntities[t]);

                // 비활성 타워 무시 (인력 미배치)
                if (towerStats.AttackSpeed <= 0f || towerStats.Damage <= 0f) continue;

                // 방금 공격한 타워만 (타이머가 deltaTime 미만)
                if (timer.TimeSinceLastAttack > Time.deltaTime * 2f) continue;

                var towerPos = entityManager.GetComponentData<LocalTransform>(towerEntities[t]).Position;
                float rangeSq = towerStats.Range * towerStats.Range;

                // 가장 가까운 적 찾기
                float closestDist = float.MaxValue;
                int closestIdx = -1;
                for (int e = 0; e < enemyPositions.Length; e++)
                {
                    if (!towerStats.CanTargetAir && enemyTypes[e] == 2) continue;
                    float d = math.distancesq(towerPos, enemyPositions[e]);
                    if (d <= rangeSq && d < closestDist)
                    {
                        closestDist = d;
                        closestIdx = e;
                    }
                }

                if (closestIdx >= 0 && lineCount < MaxLines)
                {
                    activeLines[lineCount] = new AttackLine
                    {
                        From = new Vector3(towerPos.x, towerPos.y + 1.5f, towerPos.z),
                        To = new Vector3(enemyPositions[closestIdx].x, enemyPositions[closestIdx].y + 0.5f, enemyPositions[closestIdx].z),
                        Timer = lineDuration
                    };
                    lineCount++;
                }
            }

            towerEntities.Dispose();
            enemyPositions.Dispose();
            enemyTypes.Dispose();
        }

        // EntityManager 쿼리 대용 (적 수 세기)
        private System.Collections.Generic.IEnumerable<int> SystemAPI_Substitute_EnemyCount()
        {
            var q = entityManager.CreateEntityQuery(typeof(EnemyTag));
            int count = q.CalculateEntityCount();
            for (int i = 0; i < count; i++) yield return i;
        }

        private void UpdateTimers()
        {
            int writeIdx = 0;
            for (int i = 0; i < lineCount; i++)
            {
                activeLines[i].Timer -= Time.deltaTime;
                if (activeLines[i].Timer > 0f)
                {
                    activeLines[writeIdx] = activeLines[i];
                    writeIdx++;
                }
            }
            lineCount = writeIdx;
        }

        private void OnRenderObject()
        {
            if (lineCount == 0 || lineMaterial == null) return;

            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(lineColor);

            for (int i = 0; i < lineCount; i++)
            {
                GL.Vertex(activeLines[i].From);
                GL.Vertex(activeLines[i].To);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
