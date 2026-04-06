using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSun.V2.Core;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Defense
{
    /// <summary>
    /// 전투 씬 시각화. MonoBehaviour 기반 프로토타입 시뮬레이션.
    /// ECS 시스템 자동 등록 문제를 우회하여 직접 적 스폰/이동/전투 처리.
    /// 바닥 + 건물 + 적 프리미티브 렌더링 + 전투 자동 종료.
    /// </summary>
    public class BattleSceneSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameDirector gameDirector;
        [SerializeField] Bridge.BattleUIBridge battleUIBridge;

        [Header("Battle Settings")]
        [SerializeField] float minBattleDuration = 3f;
        [SerializeField] float spawnRadius = 45f;
        [SerializeField] float enemySpeed = 5f;

        // 시각 오브젝트
        GameObject _ground;
        List<BuildingVisual> _buildings = new();
        List<EnemyVisual> _enemies = new();
        Material _matEnemy, _matBuilding, _matTower, _matGround, _matHQ;

        // 전투 상태
        bool _battleActive;
        float _battleTimer;
        float _spawnTimer;
        int _spawnedCount;
        int _totalToSpawn;
        int _killedCount;
        GameState _gameState;

        public int TotalSpawned => _spawnedCount;
        public int AliveCount => _enemies.FindAll(e => e.alive).Count;
        public int KilledCount => _killedCount;

        public void SetupBattleScene(GameState gameState)
        {
            CleanupScene();
            EnsureMaterials();
            _gameState = gameState;

            // 적 수 = 턴 기반 스케일링
            int turn = Mathf.Max(1, gameState.currentTurn);
            _totalToSpawn = Mathf.RoundToInt(10 * Mathf.Pow(1.2f, turn - 1));
            _spawnedCount = 0;
            _killedCount = 0;
            _spawnTimer = 0f;

            // 바닥
            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.name = "BattleGround";
            _ground.transform.localScale = new Vector3(10f, 1f, 10f);
            _ground.GetComponent<Renderer>().material = _matGround;
            Destroy(_ground.GetComponent<Collider>());

            // 건물
            for (int i = 0; i < gameState.buildings.Count; i++)
            {
                var building = gameState.buildings[i];
                if (building.state != BuildingSlotStateV2.Active &&
                    building.state != BuildingSlotStateV2.Damaged) continue;

                float angle = (float)i / Mathf.Max(gameState.buildings.Count, 1) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 15f, 0, Mathf.Sin(angle) * 15f);

                bool isHQ = building.buildingId == "headquarters";
                bool isTower = building.buildingId != null && building.buildingId.Contains("tower");

                var go = GameObject.CreatePrimitive(isHQ ? PrimitiveType.Cube : PrimitiveType.Cylinder);
                go.name = $"Building_{building.slotId}";
                go.transform.position = pos + Vector3.up * (isHQ ? 1.5f : 1f);
                go.transform.localScale = isHQ ? Vector3.one * 3f : isTower ? new Vector3(1, 2, 1) : Vector3.one * 1.5f;
                go.GetComponent<Renderer>().material = isHQ ? _matHQ : isTower ? _matTower : _matBuilding;
                Destroy(go.GetComponent<Collider>());

                _buildings.Add(new BuildingVisual
                {
                    go = go, position = pos, hp = building.currentHP,
                    maxHP = building.maxHP, isHQ = isHQ, isTower = isTower,
                    slotIndex = i, attackTimer = 0f,
                    attackRange = isTower ? 15f : 0f,
                    attackDamage = isTower ? 10f : 0f
                });
            }

            _battleActive = true;
            _battleTimer = 0f;

            Debug.Log($"[BattleScene] Setup: {_buildings.Count} buildings, {_totalToSpawn} enemies to spawn");
        }

        void Update()
        {
            if (!_battleActive) return;
            _battleTimer += Time.deltaTime;

            SpawnEnemies();
            MoveEnemies();
            TowerAttack();
            CheckBattleEnd();
        }

        void SpawnEnemies()
        {
            if (_spawnedCount >= _totalToSpawn) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < 0.3f) return;
            _spawnTimer = 0f;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * spawnRadius, 0.3f, Mathf.Sin(angle) * spawnRadius);

            float hpScale = Mathf.Pow(1.1f, _gameState.currentTurn - 1);
            float hp = 30f * hpScale;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"Enemy_{_spawnedCount}";
            sphere.transform.position = pos;
            sphere.transform.localScale = Vector3.one * 0.6f;
            sphere.GetComponent<Renderer>().material = _matEnemy;
            Destroy(sphere.GetComponent<Collider>());

            _enemies.Add(new EnemyVisual
            {
                go = sphere, hp = hp, maxHP = hp, speed = enemySpeed,
                damage = 5f * hpScale, alive = true, attackTimer = 0f
            });

            _spawnedCount++;
        }

        void MoveEnemies()
        {
            if (_buildings.Count == 0) return;

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                if (!enemy.alive) continue;

                // 가장 가까운 건물 찾기
                float closestDist = float.MaxValue;
                int closestIdx = -1;
                for (int j = 0; j < _buildings.Count; j++)
                {
                    if (_buildings[j].hp <= 0) continue;
                    float dist = Vector3.Distance(enemy.go.transform.position, _buildings[j].position);
                    if (dist < closestDist) { closestDist = dist; closestIdx = j; }
                }

                if (closestIdx < 0) continue;

                if (closestDist > 2f)
                {
                    // 이동
                    Vector3 dir = (_buildings[closestIdx].position - enemy.go.transform.position).normalized;
                    enemy.go.transform.position += dir * enemy.speed * Time.deltaTime;
                }
                else
                {
                    // 공격
                    enemy.attackTimer += Time.deltaTime;
                    if (enemy.attackTimer >= 1f)
                    {
                        enemy.attackTimer = 0f;
                        var bld = _buildings[closestIdx];
                        bld.hp -= enemy.damage;
                        _buildings[closestIdx] = bld;

                        // GameState 반영
                        if (bld.slotIndex < _gameState.buildings.Count)
                            _gameState.buildings[bld.slotIndex].currentHP = Mathf.RoundToInt(bld.hp);
                    }
                }

                // HP에 따라 크기 변동
                float ratio = enemy.maxHP > 0 ? enemy.hp / enemy.maxHP : 0;
                enemy.go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.6f, ratio);
                _enemies[i] = enemy;
            }
        }

        void TowerAttack()
        {
            for (int t = 0; t < _buildings.Count; t++)
            {
                var tower = _buildings[t];
                if (!tower.isTower || tower.hp <= 0 || tower.attackRange <= 0) continue;

                tower.attackTimer += Time.deltaTime;
                if (tower.attackTimer < 0.5f) { _buildings[t] = tower; continue; }
                tower.attackTimer = 0f;

                // 가장 가까운 적 공격
                float closestDist = float.MaxValue;
                int closestIdx = -1;
                for (int e = 0; e < _enemies.Count; e++)
                {
                    if (!_enemies[e].alive) continue;
                    float dist = Vector3.Distance(tower.position, _enemies[e].go.transform.position);
                    if (dist <= tower.attackRange && dist < closestDist)
                    { closestDist = dist; closestIdx = e; }
                }

                if (closestIdx >= 0)
                {
                    var enemy = _enemies[closestIdx];
                    enemy.hp -= tower.attackDamage;
                    if (enemy.hp <= 0)
                    {
                        enemy.alive = false;
                        // 사망 VFX: 스케일 펀치 + 색상 페이드
                        StartCoroutine(DeathVFX(enemy.go));
                        _killedCount++;
                    }
                    _enemies[closestIdx] = enemy;
                }

                _buildings[t] = tower;
            }
        }

        void CheckBattleEnd()
        {
            if (_battleTimer < minBattleDuration) return;
            if (_spawnedCount < _totalToSpawn) return;

            bool allDead = _enemies.TrueForAll(e => !e.alive);
            bool hqDestroyed = _buildings.Exists(b => b.isHQ && b.hp <= 0);

            if (allDead || hqDestroyed)
            {
                _battleActive = false;
                Debug.Log($"[BattleScene] End: {(allDead ? "All dead" : "HQ destroyed")} " +
                          $"killed={_killedCount}/{_spawnedCount} time={_battleTimer:F1}s");
                gameDirector?.EndNight();
            }
        }

        public void CleanupScene()
        {
            _battleActive = false;
            if (_ground != null) Destroy(_ground);
            foreach (var b in _buildings) if (b.go != null) Destroy(b.go);
            _buildings.Clear();
            foreach (var e in _enemies) if (e.go != null) Destroy(e.go);
            _enemies.Clear();
        }

        void EnsureMaterials()
        {
            if (_matEnemy != null) return;
            _matEnemy = CreateMat(new Color(0.9f, 0.2f, 0.2f));
            _matBuilding = CreateMat(new Color(0.5f, 0.5f, 0.6f));
            _matTower = CreateMat(new Color(0.3f, 0.6f, 0.9f));
            _matGround = CreateMat(new Color(0.15f, 0.12f, 0.1f));
            _matHQ = CreateMat(new Color(0.9f, 0.8f, 0.3f));
        }

        static Material CreateMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) { var m = new Material(shader); m.SetColor("_BaseColor", color); return m; }
            shader = Shader.Find("Standard");
            return new Material(shader) { color = color };
        }

        IEnumerator DeathVFX(GameObject go)
        {
            if (go == null) yield break;
            var renderer = go.GetComponent<Renderer>();
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = go.transform.localScale;

            while (elapsed < duration && go != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // 팽창 후 수축
                float scale = t < 0.3f ? Mathf.Lerp(1f, 1.5f, t / 0.3f) : Mathf.Lerp(1.5f, 0f, (t - 0.3f) / 0.7f);
                go.transform.localScale = startScale * scale;
                if (renderer != null)
                    renderer.material.SetColor("_BaseColor", Color.Lerp(new Color(0.9f, 0.2f, 0.2f), Color.white, t));
                yield return null;
            }

            if (go != null) go.SetActive(false);
        }

        void OnDestroy() => CleanupScene();

        struct BuildingVisual
        {
            public GameObject go; public Vector3 position;
            public float hp, maxHP; public bool isHQ, isTower;
            public int slotIndex; public float attackTimer, attackRange, attackDamage;
        }

        struct EnemyVisual
        {
            public GameObject go; public float hp, maxHP, speed, damage;
            public bool alive; public float attackTimer;
        }
    }
}
