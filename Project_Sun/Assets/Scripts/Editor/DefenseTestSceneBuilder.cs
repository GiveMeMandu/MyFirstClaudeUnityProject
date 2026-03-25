using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ProjectSun.Construction;
using ProjectSun.Construction.Testing;
using ProjectSun.Defense;
using ProjectSun.Defense.Testing;

/// <summary>
/// 방어 시스템 PoC 테스트 씬을 자동 생성하는 에디터 유틸리티.
/// 건설 시스템 씬을 기반으로 스폰 포인트, 전투 매니저, 적 렌더러를 추가.
/// </summary>
public static class DefenseTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/DefenseTest.unity";
    private const string DataPath = "Assets/Data/Defense";
    private const string MaterialPath = "Assets/Materials/Testing";
    private const string ConstructionDataPath = "Assets/Data/Construction";
    private const float SlotSpacing = 4f;

    // 건물 배치 (중앙 본부 + 주변)
    private static readonly Vector3[] SlotPositions =
    {
        new(0, 0, 0),                                   // 0: HQ (중앙)
        new(-SlotSpacing, 0, SlotSpacing * 0.6f),       // 1: Farm
        new(SlotSpacing, 0, SlotSpacing * 0.6f),        // 2: Arrow Tower
        new(-SlotSpacing * 1.2f, 0, 0),                 // 3: Wall
        new(SlotSpacing * 1.2f, 0, 0),                  // 4: Wall2
        new(-SlotSpacing, 0, -SlotSpacing * 0.6f),      // 5: Resource
        new(SlotSpacing, 0, -SlotSpacing * 0.6f),       // 6: Arrow Tower 2
    };

    private struct SlotConfig
    {
        public string name;
        public BuildingCategory category;
        public bool isHQ;
        public float maxHP;
    }

    private static readonly SlotConfig[] SlotConfigs =
    {
        new() { name = "Headquarters",  category = BuildingCategory.Administration, isHQ = true,  maxHP = 300 },
        new() { name = "Farm",          category = BuildingCategory.Resource,       isHQ = false, maxHP = 80 },
        new() { name = "Arrow Tower",   category = BuildingCategory.Defense,        isHQ = false, maxHP = 120 },
        new() { name = "Wall",          category = BuildingCategory.Wall,           isHQ = false, maxHP = 200 },
        new() { name = "Wall East",     category = BuildingCategory.Wall,           isHQ = false, maxHP = 200 },
        new() { name = "Mine",          category = BuildingCategory.Resource,       isHQ = false, maxHP = 80 },
        new() { name = "Arrow Tower 2", category = BuildingCategory.Defense,        isHQ = false, maxHP = 120 },
    };

    // 스폰 포인트 위치 (기지 외곽 4방향)
    private static readonly Vector3[] SpawnPositions =
    {
        new(0, 0, 20),    // 북
        new(0, 0, -20),   // 남
        new(20, 0, 0),    // 동
        new(-20, 0, 0),   // 서
    };

    [MenuItem("ProjectSun/Create Defense Test Scene")]
    public static void CreateTestScene()
    {
        EnsureDirectories();

        // 머티리얼 생성
        Material groundMat = CreateOrLoadMaterial("Ground", new Color(0.35f, 0.45f, 0.3f));
        Material slotMat = CreateOrLoadMaterial("SlotIndicator", new Color(1f, 1f, 1f, 0.3f));
        Material buildingMat = CreateOrLoadMaterial("Building", Color.white);
        Material basicEnemyMat = CreateOrLoadMaterial("EnemyBasic", new Color(0.9f, 0.2f, 0.2f), true);
        Material heavyEnemyMat = CreateOrLoadMaterial("EnemyHeavy", new Color(0.5f, 0.1f, 0.1f), true);
        Material flyingEnemyMat = CreateOrLoadMaterial("EnemyFlying", new Color(0.9f, 0.5f, 0.1f), true);
        Material spawnPointMat = CreateOrLoadMaterial("SpawnPoint", new Color(1f, 0f, 0f, 0.4f));

        // 적 데이터 SO 생성
        var enemyDatas = CreateEnemyDataAssets();

        // 웨이브 데이터 SO 생성
        var waveData = CreateWaveDataAsset(enemyDatas);

        // 건물 데이터 SO 로드/생성
        BuildingData[] buildingDatas = CreateBuildingDataAssets();

        // 새 씬 생성
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 환경
        CreateGround(groundMat);
        CreateCamera();
        CreateLight();

        // 건물 슬롯 (모두 Active 상태로 시작 — 밤 전투 테스트용)
        var slots = new List<BuildingSlot>();
        for (int i = 0; i < SlotConfigs.Length; i++)
        {
            var slotGO = CreateBuildingSlot(i, buildingDatas[i], slotMat, buildingMat);
            slots.Add(slotGO.GetComponent<BuildingSlot>());
        }

        SetupAdjacency(slots);

        // BuildingManager
        var managerGO = new GameObject("BuildingManager");
        var manager = managerGO.AddComponent<BuildingManager>();
        SetPrivateField(manager, "allSlots", new List<BuildingSlot>(slots));

        // 스폰 포인트
        var spawnPointTransforms = CreateSpawnPoints(spawnPointMat);

        // BattleManager
        var battleManagerGO = new GameObject("BattleManager");
        var battleManager = battleManagerGO.AddComponent<BattleManager>();
        SetPrivateField(battleManager, "waveData", waveData);
        SetPrivateField(battleManager, "buildingManager", manager);
        SetPrivateField(battleManager, "spawnPoints", spawnPointTransforms);

        // BattleCameraController (메인 카메라에 추가)
        var cam = Camera.main;
        var cameraController = cam.gameObject.AddComponent<BattleCameraController>();

        // EnemyHybridRenderer
        var rendererGO = new GameObject("EnemyHybridRenderer");
        var hybridRenderer = rendererGO.AddComponent<EnemyHybridRenderer>();
        SetPrivateField(hybridRenderer, "basicEnemyMaterial", basicEnemyMat);
        SetPrivateField(hybridRenderer, "heavyEnemyMaterial", heavyEnemyMat);
        SetPrivateField(hybridRenderer, "flyingEnemyMaterial", flyingEnemyMat);

        // DefenseTestController
        var controllerGO = new GameObject("DefenseTestController");
        var testController = controllerGO.AddComponent<DefenseTestController>();
        SetPrivateField(testController, "battleManager", battleManager);
        SetPrivateField(testController, "cameraController", cameraController);

        // 건물 HP 초기화 (모두 Active 상태)
        for (int i = 0; i < slots.Count; i++)
        {
            var health = slots[i].GetComponent<BuildingHealth>();
            if (health != null)
                health.Initialize(SlotConfigs[i].maxHP, 25f);
        }

        // 씬 저장
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Defense test scene created at {ScenePath}");
        Debug.Log("Press 'Start Night Battle' button to test DOTS enemy spawning.");
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(DataPath))
            AssetDatabase.CreateFolder("Assets/Data", "Defense");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialPath))
            AssetDatabase.CreateFolder("Assets/Materials", "Testing");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder(ConstructionDataPath))
            AssetDatabase.CreateFolder("Assets/Data", "Construction");
    }

    private static EnemyDataSO[] CreateEnemyDataAssets()
    {
        var datas = new EnemyDataSO[3];

        // Basic
        datas[0] = CreateOrLoadEnemySO("BasicEnemy", EnemyType.Basic, 30f, 3f, 5f, 2f, 1f, 0.5f);
        // Heavy
        datas[1] = CreateOrLoadEnemySO("HeavyEnemy", EnemyType.Heavy, 150f, 1.5f, 15f, 2f, 1.5f, 1.2f);
        // Flying
        datas[2] = CreateOrLoadEnemySO("FlyingEnemy", EnemyType.Flying, 50f, 3f, 8f, 2f, 1f, 0.4f);

        return datas;
    }

    private static EnemyDataSO CreateOrLoadEnemySO(string name, EnemyType type,
        float hp, float speed, float damage, float range, float interval, float scale)
    {
        string path = $"{DataPath}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<EnemyDataSO>();
        so.enemyName = name;
        so.enemyType = type;
        so.hp = hp;
        so.speed = speed;
        so.damage = damage;
        so.attackRange = range;
        so.attackInterval = interval;
        so.scale = scale;

        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    private static WaveDataSO CreateWaveDataAsset(EnemyDataSO[] enemyDatas)
    {
        string path = $"{DataPath}/TestWaveData.asset";
        var existing = AssetDatabase.LoadAssetAtPath<WaveDataSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<WaveDataSO>();
        so.GenerateDefaultWaves(enemyDatas[0], enemyDatas[1], enemyDatas[2]);

        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    private static BuildingData[] CreateBuildingDataAssets()
    {
        var datas = new BuildingData[SlotConfigs.Length];
        for (int i = 0; i < SlotConfigs.Length; i++)
        {
            var cfg = SlotConfigs[i];
            string safeName = cfg.name.Replace(" ", "");
            string path = $"{ConstructionDataPath}/{safeName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (existing != null)
            {
                datas[i] = existing;
                continue;
            }

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = cfg.name;
            data.category = cfg.category;
            data.isHeadquarters = cfg.isHQ;
            data.constructionTurns = 1;
            data.maxHP = cfg.maxHP;
            data.autoRepairRate = 25f;
            data.repairTurns = 2;
            data.baseWorkerSlots = 1;
            data.maxConstructionWorkers = 1;
            data.tier = 1;

            AssetDatabase.CreateAsset(data, path);
            datas[i] = data;
        }
        return datas;
    }

    private static List<Transform> CreateSpawnPoints(Material mat)
    {
        var parent = new GameObject("SpawnPoints");
        var transforms = new List<Transform>();

        for (int i = 0; i < SpawnPositions.Length; i++)
        {
            var sp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sp.name = $"SpawnPoint_{i}";
            sp.transform.SetParent(parent.transform, false);
            sp.transform.position = SpawnPositions[i];
            sp.transform.localScale = new Vector3(3f, 0.1f, 3f);
            sp.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 콜라이더 제거 (시각적 표시만)
            Object.DestroyImmediate(sp.GetComponent<Collider>());

            transforms.Add(sp.transform);
        }

        return transforms;
    }

    private static void CreateGround(Material mat)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
        ground.isStatic = true;
    }

    private static void CreateCamera()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = false;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

        // 탑다운 뷰 위치
        camGO.transform.position = new Vector3(0, 25, -15);
        camGO.transform.rotation = Quaternion.Euler(60, 0, 0);

        camGO.AddComponent<AudioListener>();
    }

    private static void CreateLight()
    {
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.7f, 0.75f, 0.9f); // 밤 분위기
        light.intensity = 0.8f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    private static GameObject CreateBuildingSlot(int index, BuildingData data, Material slotMat, Material buildingMat)
    {
        var cfg = SlotConfigs[index];
        var root = new GameObject($"Slot_{cfg.name.Replace(" ", "_")}");
        root.transform.position = SlotPositions[index];

        // 슬롯 표시 (디스크)
        var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "SlotIndicator";
        indicator.transform.SetParent(root.transform, false);
        indicator.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
        indicator.transform.localPosition = new Vector3(0, 0.025f, 0);
        indicator.GetComponent<MeshRenderer>().sharedMaterial = slotMat;

        // 건물 모델 (큐브, 카테고리별 높이 다르게)
        float height = cfg.category switch
        {
            BuildingCategory.Administration => cfg.isHQ ? 2f : 1.2f,
            BuildingCategory.Defense => 1.8f,
            BuildingCategory.Wall => 1.5f,
            _ => 1f
        };

        var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "BuildingModel";
        building.transform.SetParent(root.transform, false);
        building.transform.localPosition = new Vector3(0, height / 2f, 0);
        building.transform.localScale = new Vector3(1.5f, height, 1.5f);
        building.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;

        // 라벨
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0, height + 0.5f, 0);
        labelGO.transform.rotation = Quaternion.Euler(45, 0, 0);
        var textMesh = labelGO.AddComponent<TextMesh>();
        textMesh.text = cfg.name;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.15f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        // 컴포넌트
        var slot = root.AddComponent<BuildingSlot>();
        root.AddComponent<BuildingHealth>();

        var visual = root.AddComponent<BuildingSlotVisual>();

        // 직렬화 필드 설정
        SetPrivateField(slot, "assignedBuilding", data);
        SetPrivateField(slot, "state", BuildingSlotState.Active); // 모든 건물 Active 상태

        SetPrivateField(visual, "slotIndicator", indicator.GetComponent<MeshRenderer>());
        SetPrivateField(visual, "buildingModel", building.GetComponent<MeshRenderer>());
        SetPrivateField(visual, "stateLabel", textMesh);

        // 콜라이더
        var boxCollider = root.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, height / 2f, 0);
        boxCollider.size = new Vector3(2.5f, height + 0.5f, 2.5f);

        // 건물 카테고리별 색상 적용
        var mr = building.GetComponent<MeshRenderer>();
        var mat = new Material(mr.sharedMaterial);
        mat.color = cfg.category switch
        {
            BuildingCategory.Resource => new Color(0.2f, 0.8f, 0.3f),
            BuildingCategory.Defense => new Color(0.9f, 0.2f, 0.2f),
            BuildingCategory.Research => new Color(0.3f, 0.5f, 1f),
            BuildingCategory.Administration => cfg.isHQ ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.3f, 0.9f),
            BuildingCategory.Wall => new Color(0.5f, 0.5f, 0.5f),
            _ => Color.white
        };
        mr.sharedMaterial = mat;

        return root;
    }

    private static void SetupAdjacency(List<BuildingSlot> slots)
    {
        var centerAdjacent = new List<BuildingSlot>();
        for (int i = 1; i < slots.Count; i++)
            centerAdjacent.Add(slots[i]);
        SetPrivateField(slots[0], "adjacentSlots", centerAdjacent);
    }

    private static Material CreateOrLoadMaterial(string name, Color color, bool enableInstancing = false)
    {
        string path = $"{MaterialPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            if (enableInstancing) existing.enableInstancing = true;
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.color = color;
        if (enableInstancing) mat.enableInstancing = true;

        if (color.a < 1f)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var type = obj.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }
            type = type.BaseType;
        }
        Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
    }
}
