using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectSun.Defense.ECS;
using UnityEngine;

namespace ProjectSun.Defense
{
    /// <summary>
    /// 적 유닛의 시각적 피드백 관리 (MonoBehaviour 측).
    /// ECS 데이터를 읽어 체력바와 사망 이펙트를 처리.
    /// </summary>
    public class EnemyVisualManager : MonoBehaviour
    {
        [Header("체력바 설정")]
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private float healthBarOffsetY = 1.5f;
        [SerializeField] private int maxHealthBars = 100;

        [Header("사망 이펙트")]
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private int deathEffectPoolSize = 50;
        [SerializeField] private float deathEffectDuration = 1f;

        [Header("연동")]
        [SerializeField] private BattleUIManager uiManager;

        // 체력바 풀
        private GameObject[] healthBarPool;
        private RectTransform[] healthBarFills;
        private int activeHealthBars;

        // 사망 이펙트 풀
        private GameObject[] deathEffectPool;
        private float[] deathEffectTimers;
        private int nextDeathEffect;

        private EntityManager entityManager;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            InitializePools();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                entityManager = world.EntityManager;
            }
        }

        private void InitializePools()
        {
            // 체력바 풀
            healthBarPool = new GameObject[maxHealthBars];
            healthBarFills = new RectTransform[maxHealthBars];
            for (int i = 0; i < maxHealthBars; i++)
            {
                if (healthBarPrefab != null)
                {
                    healthBarPool[i] = Instantiate(healthBarPrefab, transform);
                    healthBarPool[i].SetActive(false);
                    // 자식에서 fill bar 찾기
                    var fill = healthBarPool[i].transform.Find("Fill");
                    if (fill != null) healthBarFills[i] = fill as RectTransform;
                }
            }

            // 사망 이펙트 풀
            deathEffectPool = new GameObject[deathEffectPoolSize];
            deathEffectTimers = new float[deathEffectPoolSize];
            for (int i = 0; i < deathEffectPoolSize; i++)
            {
                if (deathEffectPrefab != null)
                {
                    deathEffectPool[i] = Instantiate(deathEffectPrefab, transform);
                    deathEffectPool[i].SetActive(false);
                }
                deathEffectTimers[i] = 0f;
            }
        }

        private void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            bool showHealthBars = uiManager != null ? uiManager.HealthBarsEnabled : true;

            if (showHealthBars)
            {
                UpdateHealthBars();
            }
            else
            {
                HideAllHealthBars();
            }

            UpdateDeathEffects();
        }

        private void UpdateHealthBars()
        {
            activeHealthBars = 0;

            // 체력바 표시가 필요한 적 유닛 쿼리
            var query = entityManager.CreateEntityQuery(
                typeof(EnemyTag),
                typeof(EnemyStats),
                typeof(HealthBarTimer),
                typeof(LocalTransform)
            );

            // ComponentType.Exclude 대신 WithNone 사용 불가 (EntityManager 직접 쿼리)
            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length && activeHealthBars < maxHealthBars; i++)
            {
                if (entityManager.HasComponent<DeadTag>(entities[i])) continue;

                var timer = entityManager.GetComponentData<HealthBarTimer>(entities[i]);
                if (timer.RemainingTime <= 0f) continue;

                var stats = entityManager.GetComponentData<EnemyStats>(entities[i]);
                var localTransform = entityManager.GetComponentData<LocalTransform>(entities[i]);

                var worldPos = new Vector3(
                    localTransform.Position.x,
                    localTransform.Position.y + healthBarOffsetY,
                    localTransform.Position.z
                );

                var screenPos = mainCamera.WorldToScreenPoint(worldPos);

                // 카메라 뒤에 있으면 표시하지 않음
                if (screenPos.z < 0f) continue;

                if (healthBarPool[activeHealthBars] != null)
                {
                    healthBarPool[activeHealthBars].SetActive(true);
                    healthBarPool[activeHealthBars].transform.position = screenPos;

                    // 체력바 fill 업데이트
                    float ratio = stats.MaxHP > 0 ? stats.CurrentHP / stats.MaxHP : 0f;
                    if (healthBarFills[activeHealthBars] != null)
                    {
                        healthBarFills[activeHealthBars].localScale = new Vector3(ratio, 1f, 1f);
                    }
                }

                activeHealthBars++;
            }

            entities.Dispose();

            // 남은 체력바 비활성화
            for (int i = activeHealthBars; i < maxHealthBars; i++)
            {
                if (healthBarPool[i] != null)
                    healthBarPool[i].SetActive(false);
            }
        }

        private void HideAllHealthBars()
        {
            for (int i = 0; i < maxHealthBars; i++)
            {
                if (healthBarPool[i] != null)
                    healthBarPool[i].SetActive(false);
            }
        }

        private void UpdateDeathEffects()
        {
            for (int i = 0; i < deathEffectPoolSize; i++)
            {
                if (deathEffectTimers[i] > 0f)
                {
                    deathEffectTimers[i] -= Time.deltaTime;
                    if (deathEffectTimers[i] <= 0f && deathEffectPool[i] != null)
                    {
                        deathEffectPool[i].SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// 사망 이펙트를 특정 위치에 재생 (ECS에서 호출)
        /// </summary>
        public void PlayDeathEffect(Vector3 position)
        {
            if (deathEffectPool[nextDeathEffect] != null)
            {
                deathEffectPool[nextDeathEffect].transform.position = position;
                deathEffectPool[nextDeathEffect].SetActive(true);
                deathEffectTimers[nextDeathEffect] = deathEffectDuration;

                // 파티클 시스템 재시작
                var ps = deathEffectPool[nextDeathEffect].GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
            }

            nextDeathEffect = (nextDeathEffect + 1) % deathEffectPoolSize;
        }
    }
}
