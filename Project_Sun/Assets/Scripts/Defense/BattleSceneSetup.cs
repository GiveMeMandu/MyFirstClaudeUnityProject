using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectSun.Defense.ECS;
using ProjectSun.V2.Core;
using ProjectSun.V2.Data;
using ProjectSun.V2.Defense.Bridge;

namespace ProjectSun.V2.Defense
{
    /// <summary>
    /// 전투 씬 시각화. 바닥/건물 프리미티브 + ECS 엔티티 렌더러.
    /// 매 프레임 ECS 엔티티를 읽어 프리미티브 GameObject로 시각화.
    /// 전투 종료 자동 감지 (모든 적 처치 OR 본부 파괴).
    /// </summary>
    public class BattleSceneSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameDirector gameDirector;
        [SerializeField] BattleUIBridge battleUIBridge;

        [Header("Visual Settings")]
        [SerializeField] Material enemyMaterial;
        [SerializeField] Material buildingMaterial;
        [SerializeField] Material towerMaterial;
        [SerializeField] Material groundMaterial;

        [Header("Auto End")]
        [SerializeField] float minBattleDuration = 3f;

        // 프리미티브 풀
        GameObject _ground;
        List<GameObject> _buildingVisuals = new();
        List<GameObject> _enemyPool = new();
        int _activeEnemyCount;
        float _battleTimer;
        bool _battleActive;

        // 머티리얼 (런타임 생성)
        Material _matEnemy;
        Material _matBuilding;
        Material _matTower;
        Material _matGround;
        Material _matHQ;

        void Awake()
        {
            CreateFallbackMaterials();
        }

        /// <summary>전투 시작 시 호출. 바닥 + 건물 비주얼 생성.</summary>
        public void SetupBattleScene(GameState gameState)
        {
            CleanupScene();

            // 바닥
            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.name = "BattleGround";
            _ground.transform.localScale = new Vector3(10f, 1f, 10f);
            _ground.GetComponent<Renderer>().material = groundMaterial ?? _matGround;
            Destroy(_ground.GetComponent<Collider>());

            // 건물 비주얼
            for (int i = 0; i < gameState.buildings.Count; i++)
            {
                var building = gameState.buildings[i];
                if (building.state != BuildingSlotStateV2.Active &&
                    building.state != BuildingSlotStateV2.Damaged)
                    continue;

                float angle = (float)i / Mathf.Max(gameState.buildings.Count, 1) * Mathf.PI * 2f;
                float radius = 15f;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius, 0,
                    Mathf.Sin(angle) * radius);

                bool isHQ = building.buildingId == "headquarters";
                bool isTower = building.buildingId != null && building.buildingId.Contains("tower");

                var go = GameObject.CreatePrimitive(isHQ ? PrimitiveType.Cube : PrimitiveType.Cylinder);
                go.name = $"Building_{building.slotId}";
                go.transform.position = pos + Vector3.up * (isHQ ? 1.5f : 1f);
                go.transform.localScale = isHQ
                    ? new Vector3(3f, 3f, 3f)
                    : isTower
                        ? new Vector3(1f, 2f, 1f)
                        : new Vector3(1.5f, 1.5f, 1.5f);

                var mat = isHQ ? _matHQ : isTower ? (towerMaterial ?? _matTower) : (buildingMaterial ?? _matBuilding);
                go.GetComponent<Renderer>().material = mat;
                Destroy(go.GetComponent<Collider>());
                _buildingVisuals.Add(go);
            }

            _battleActive = true;
            _battleTimer = 0f;
            _activeEnemyCount = 0;
        }

        void Update()
        {
            if (!_battleActive) return;

            _battleTimer += Time.deltaTime;

            // ECS 엔티티 시각화
            RenderECSEntities();

            // 전투 종료 자동 감지
            if (_battleTimer >= minBattleDuration && battleUIBridge != null)
            {
                bool allDead = battleUIBridge.TotalEnemiesSpawned > 0 &&
                               battleUIBridge.AliveEnemyCount == 0;
                bool hqDestroyed = battleUIBridge.HeadquartersMaxHP > 0 &&
                                   battleUIBridge.HeadquartersHP <= 0;

                if (allDead || hqDestroyed)
                {
                    _battleActive = false;
                    Debug.Log($"[BattleScene] Auto-end: {(allDead ? "All enemies dead" : "HQ destroyed")} at {_battleTimer:F1}s");
                    gameDirector?.EndNight();
                }
            }
        }

        void RenderECSEntities()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;

            // 적 엔티티 수집
            var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<EnemyStats>());

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            int aliveCount = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                if (em.HasComponent<DeadTag>(entities[i])) continue;

                var transform = em.GetComponentData<LocalTransform>(entities[i]);
                var stats = em.GetComponentData<EnemyStats>(entities[i]);

                // 풀에서 가져오거나 생성
                while (_enemyPool.Count <= aliveCount)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = $"Enemy_{_enemyPool.Count}";
                    sphere.transform.localScale = Vector3.one * 0.6f;
                    sphere.GetComponent<Renderer>().material = enemyMaterial ?? _matEnemy;
                    Destroy(sphere.GetComponent<Collider>());
                    sphere.SetActive(false);
                    _enemyPool.Add(sphere);
                }

                var enemyGo = _enemyPool[aliveCount];
                enemyGo.SetActive(true);
                enemyGo.transform.position = new Vector3(
                    transform.Position.x,
                    transform.Position.y + 0.3f,
                    transform.Position.z);

                // HP에 따라 크기 변경
                float hpRatio = stats.MaxHP > 0 ? stats.CurrentHP / stats.MaxHP : 1f;
                float scale = Mathf.Lerp(0.3f, 0.6f, hpRatio);
                enemyGo.transform.localScale = Vector3.one * scale;

                aliveCount++;
            }

            // 초과 풀 비활성화
            for (int i = aliveCount; i < _enemyPool.Count; i++)
            {
                if (_enemyPool[i].activeSelf)
                    _enemyPool[i].SetActive(false);
            }

            _activeEnemyCount = aliveCount;
        }

        /// <summary>씬 정리. 밤 종료 시 호출.</summary>
        public void CleanupScene()
        {
            _battleActive = false;

            if (_ground != null) Destroy(_ground);

            foreach (var go in _buildingVisuals)
                if (go != null) Destroy(go);
            _buildingVisuals.Clear();

            foreach (var go in _enemyPool)
                if (go != null) Destroy(go);
            _enemyPool.Clear();
        }

        void CreateFallbackMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");

            _matEnemy = new Material(shader) { color = new Color(0.9f, 0.2f, 0.2f) };
            _matBuilding = new Material(shader) { color = new Color(0.5f, 0.5f, 0.6f) };
            _matTower = new Material(shader) { color = new Color(0.3f, 0.6f, 0.9f) };
            _matGround = new Material(shader) { color = new Color(0.15f, 0.12f, 0.1f) };
            _matHQ = new Material(shader) { color = new Color(0.9f, 0.8f, 0.3f) };
        }

        void OnDestroy()
        {
            CleanupScene();
        }
    }
}
