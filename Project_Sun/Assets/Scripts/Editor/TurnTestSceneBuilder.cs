using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using ProjectSun.Construction;
using ProjectSun.Construction.Testing;
using ProjectSun.Defense;
using ProjectSun.Defense.Testing;
using ProjectSun.Turn;
using ProjectSun.Turn.Testing;
using ProjectSun.Encounter;
using ProjectSun.Encounter.Testing;
using ProjectSun.Resource;
using ProjectSun.Resource.Testing;
using ProjectSun.Workforce;
using ProjectSun.Workforce.Testing;

/// <summary>
/// 턴 시스템 통합 테스트 씬 자동 생성.
/// 건설 + 방어 + 턴 시스템을 모두 포함한 전체 루프 테스트.
/// </summary>
public static class TurnTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TurnTest.unity";
    private const string DataPath = "Assets/Data";
    private const string TurnDataPath = "Assets/Data/Turn";
    private const string DefenseDataPath = "Assets/Data/Defense";
    private const string ConstructionDataPath = "Assets/Data/Construction";
    private const string MaterialPath = "Assets/Materials/Testing";
    private const float SlotSpacing = 4f;

    private static readonly Vector3[] SlotPositions =
    {
        new(0, 0, 0),
        new(-SlotSpacing, 0, SlotSpacing * 0.6f),
        new(SlotSpacing, 0, SlotSpacing * 0.6f),
        new(-SlotSpacing * 1.2f, 0, 0),
        new(SlotSpacing * 1.2f, 0, 0),
    };

    private struct SlotConfig
    {
        public string name;
        public BuildingCategory category;
        public bool isHQ;
        public float maxHP;
        public float towerRange;
        public float towerDamage;
        public float towerAttackSpeed;
        public bool towerCanTargetAir;
    }

    private static readonly SlotConfig[] SlotConfigs =
    {
        new() { name = "Headquarters",  category = BuildingCategory.Administration, isHQ = true,  maxHP = 300 },
        new() { name = "Farm",          category = BuildingCategory.Resource,       isHQ = false, maxHP = 80 },
        new() { name = "Arrow Tower",   category = BuildingCategory.Defense,        isHQ = false, maxHP = 120, towerRange = 10f, towerDamage = 8f, towerAttackSpeed = 2f, towerCanTargetAir = true },
        new() { name = "Wall",          category = BuildingCategory.Wall,           isHQ = false, maxHP = 200 },
        new() { name = "Arrow Tower 2", category = BuildingCategory.Defense,        isHQ = false, maxHP = 120, towerRange = 12f, towerDamage = 12f, towerAttackSpeed = 1f, towerCanTargetAir = false },
    };

    private static readonly Vector3[] SpawnPositions =
    {
        new(0, 0, 20),
        new(0, 0, -20),
        new(20, 0, 0),
        new(-20, 0, 0),
    };

    [MenuItem("ProjectSun/Create Turn Test Scene")]
    public static void CreateTestScene()
    {
        EnsureDirectories();

        // 머티리얼
        Material groundMat = CreateOrLoadMaterial("Ground", new Color(0.45f, 0.65f, 0.35f));
        Material slotMat = CreateOrLoadMaterial("SlotIndicator", new Color(1f, 1f, 1f, 0.3f));
        Material buildingMat = CreateOrLoadMaterial("Building", Color.white);
        Material basicEnemyMat = CreateOrLoadMaterial("EnemyBasic", new Color(0.9f, 0.2f, 0.2f), true);
        Material heavyEnemyMat = CreateOrLoadMaterial("EnemyHeavy", new Color(0.5f, 0.1f, 0.1f), true);
        Material flyingEnemyMat = CreateOrLoadMaterial("EnemyFlying", new Color(0.9f, 0.5f, 0.1f), true);
        Material spawnPointMat = CreateOrLoadMaterial("SpawnPoint", new Color(1f, 0f, 0f, 0.4f));

        // SO 에셋
        var encounterData = CreateEncounterData();
        var scenarioData = CreateScenarioData(encounterData);
        var waveData = LoadOrCreateWaveData();
        BuildingData[] buildingDatas = CreateBuildingDataAssets();

        // 씬
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGround(groundMat);
        CreateCamera();
        CreateLight();

        // 건물 슬롯
        var slots = new List<BuildingSlot>();
        for (int i = 0; i < SlotConfigs.Length; i++)
        {
            var slotGO = CreateBuildingSlot(i, buildingDatas[i], slotMat, buildingMat);
            slots.Add(slotGO.GetComponent<BuildingSlot>());
        }

        // BuildingManager
        var managerGO = new GameObject("BuildingManager");
        var manager = managerGO.AddComponent<BuildingManager>();
        SetField(manager, "allSlots", new List<BuildingSlot>(slots));

        // 스폰 포인트
        var spawnTransforms = CreateSpawnPoints(spawnPointMat);

        // BattleManager
        var battleGO = new GameObject("BattleManager");
        var battleMgr = battleGO.AddComponent<BattleManager>();
        SetField(battleMgr, "waveData", waveData);
        SetField(battleMgr, "buildingManager", manager);
        SetField(battleMgr, "spawnPoints", spawnTransforms);

        // BattleCameraController
        var cam = Camera.main;
        var camCtrl = cam.gameObject.AddComponent<BattleCameraController>();

        // EnemyHybridRenderer
        var rendererGO = new GameObject("EnemyHybridRenderer");
        var hybridRenderer = rendererGO.AddComponent<EnemyHybridRenderer>();
        SetField(hybridRenderer, "basicEnemyMaterial", basicEnemyMat);
        SetField(hybridRenderer, "heavyEnemyMaterial", heavyEnemyMat);
        SetField(hybridRenderer, "flyingEnemyMaterial", flyingEnemyMat);

        // TowerProjectileRenderer
        new GameObject("TowerProjectileRenderer").AddComponent<TowerProjectileRenderer>();

        // ScreenFader (Canvas)
        var faderGO = new GameObject("ScreenFader");
        var faderCanvas = faderGO.AddComponent<Canvas>();
        faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        faderCanvas.sortingOrder = 999;
        faderGO.AddComponent<CanvasScaler>();
        var fader = faderGO.AddComponent<ScreenFader>();

        // ToastMessage
        var toastGO = new GameObject("ToastMessage");
        var toast = toastGO.AddComponent<ToastMessage>();

        // ResourceManager
        var resourceGO = new GameObject("ResourceManager");
        var resourceMgr = resourceGO.AddComponent<ResourceManager>();
        SetField(resourceMgr, "buildingManager", manager);

        // ResourceUI
        var resourceUIGO = new GameObject("ResourceUI");
        var resourceUI = resourceUIGO.AddComponent<ResourceUI>();
        SetField(resourceUI, "resourceManager", resourceMgr);

        // BuildingManager에 ResourceManager 연결
        SetField(manager, "resourceManager", resourceMgr);

        // WorkforceManager
        var workforceGO = new GameObject("WorkforceManager");
        var workforceMgr = workforceGO.AddComponent<WorkforceManager>();
        SetField(workforceMgr, "buildingManager", manager);
        SetField(workforceMgr, "totalWorkers", 4);

        // ResourceManager에 WorkforceManager 연결
        SetField(resourceMgr, "workforceManager", workforceMgr);

        // EncounterManager + BuffManager
        var encounterPoolSO = CreateEncounterPool();
        var buffGO = new GameObject("BuffManager");
        var buffMgr = buffGO.AddComponent<BuffManager>();

        var encounterGO = new GameObject("EncounterManager");
        var encounterMgr = encounterGO.AddComponent<EncounterManager>();
        SetField(encounterMgr, "encounterPool", encounterPoolSO);
        SetField(encounterMgr, "resourceManager", resourceMgr);
        SetField(encounterMgr, "workforceManager", workforceMgr);
        SetField(encounterMgr, "buildingManager", manager);
        SetField(encounterMgr, "buffManager", buffMgr);

        // EncounterUI + BuffUI
        var encounterUIGO = new GameObject("EncounterUI");
        var encUI = encounterUIGO.AddComponent<EncounterUI>();
        SetField(encUI, "encounterManager", encounterMgr);

        var buffUIGO = new GameObject("BuffUI");
        var bUI = buffUIGO.AddComponent<BuffUI>();
        SetField(bUI, "buffManager", buffMgr);

        // TurnManager
        var turnGO = new GameObject("TurnManager");
        var turnMgr = turnGO.AddComponent<TurnManager>();
        SetField(turnMgr, "scenarioData", scenarioData);
        SetField(turnMgr, "buildingManager", manager);
        SetField(turnMgr, "battleManager", battleMgr);
        SetField(turnMgr, "workforceManager", workforceMgr);
        SetField(turnMgr, "resourceManager", resourceMgr);
        SetField(turnMgr, "encounterManager", encounterMgr);
        SetField(turnMgr, "buffManager", buffMgr);
        SetField(turnMgr, "screenFader", fader);
        SetField(turnMgr, "toastMessage", toast);

        // BattleManager에 WorkforceManager 연결
        SetField(battleMgr, "workforceManager", workforceMgr);

        // TurnTestController
        var turnCtrlGO = new GameObject("TurnTestController");
        var turnCtrl = turnCtrlGO.AddComponent<TurnTestController>();
        SetField(turnCtrl, "turnManager", turnMgr);
        SetField(turnCtrl, "buildingManager", manager);
        SetField(turnCtrl, "mainCamera", cam);

        // WorkforceTestController
        var workforceCtrlGO = new GameObject("WorkforceTestController");
        var workforceCtrl = workforceCtrlGO.AddComponent<WorkforceTestController>();
        SetField(workforceCtrl, "workforceManager", workforceMgr);
        SetField(workforceCtrl, "buildingManager", manager);
        SetField(workforceCtrl, "turnManager", turnMgr);
        SetField(workforceCtrl, "mainCamera", cam);

        // HP 초기화
        for (int i = 0; i < slots.Count; i++)
        {
            var health = slots[i].GetComponent<BuildingHealth>();
            if (health != null)
                health.Initialize(SlotConfigs[i].maxHP, 25f);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Turn test scene created at {ScenePath}");

        Debug.Log($"Turn test scene created at {ScenePath}");
        Debug.Log("Press 'End Turn (Night)' to start the turn loop.");
    }

    private const string EncounterDataPath = "Assets/Data/Encounter";

    private static void EnsureDirectories()
    {
        string[] dirs = { "Assets/Data", TurnDataPath, DefenseDataPath, ConstructionDataPath,
                          EncounterDataPath, "Assets/Materials", MaterialPath, "Assets/Scenes" };
        foreach (var dir in dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Split('/');
                string parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string child = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(child))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = child;
                }
            }
        }
    }

    private static EncounterDataSO CreateEncounterData()
    {
        string path = $"{TurnDataPath}/DefaultEncounter.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EncounterDataSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<EncounterDataSO>();
        so.battleChance = 0.3f;
        so.eventChance = 0.25f;
        so.eventPool = new()
        {
            new() { eventName = "방랑 상인", description = "방랑 상인이 기지를 방문했다. 좋은 거래가 될 것 같다.", weight = 3f },
            new() { eventName = "폭풍 경고", description = "거센 폭풍이 다가오고 있다. 건물들이 약간의 피해를 입을 수 있다.", weight = 2f },
            new() { eventName = "피난민 도착", description = "피난민 일행이 기지에 도착했다. 인력이 증가했다.", weight = 2f },
            new() { eventName = "적 정찰병 발견", description = "적 정찰병이 기지 근처에서 목격되었다!", weight = 1f, triggersBattle = true },
        };

        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    private static ScenarioDataSO CreateScenarioData(EncounterDataSO encounter)
    {
        string path = $"{TurnDataPath}/TestScenario.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ScenarioDataSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<ScenarioDataSO>();
        so.GenerateDefaultScenario(encounter);

        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    private static WaveDataSO LoadOrCreateWaveData()
    {
        string path = $"{DefenseDataPath}/TestWaveData.asset";
        var existing = AssetDatabase.LoadAssetAtPath<WaveDataSO>(path);
        if (existing != null) return existing;
        return null; // 이전에 생성되어 있어야 함
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
                // 타워 스탯 업데이트
                if (cfg.category == BuildingCategory.Defense)
                {
                    existing.towerRange = cfg.towerRange;
                    existing.towerDamage = cfg.towerDamage;
                    existing.towerAttackSpeed = cfg.towerAttackSpeed;
                    existing.towerCanTargetAir = cfg.towerCanTargetAir;
                    EditorUtility.SetDirty(existing);
                }
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

            if (cfg.category == BuildingCategory.Defense)
            {
                data.towerRange = cfg.towerRange;
                data.towerDamage = cfg.towerDamage;
                data.towerAttackSpeed = cfg.towerAttackSpeed;
                data.towerCanTargetAir = cfg.towerCanTargetAir;
            }

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
        cam.backgroundColor = new Color(0.4f, 0.6f, 0.9f); // 낮 하늘색
        camGO.transform.position = new Vector3(0, 25, -15);
        camGO.transform.rotation = Quaternion.Euler(60, 0, 0);
        camGO.AddComponent<AudioListener>();
    }

    private static void CreateLight()
    {
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    private static GameObject CreateBuildingSlot(int index, BuildingData data, Material slotMat, Material buildingMat)
    {
        var cfg = SlotConfigs[index];
        var root = new GameObject($"Slot_{cfg.name.Replace(" ", "_")}");
        root.transform.position = SlotPositions[index];

        var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "SlotIndicator";
        indicator.transform.SetParent(root.transform, false);
        indicator.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
        indicator.transform.localPosition = new Vector3(0, 0.025f, 0);
        indicator.GetComponent<MeshRenderer>().sharedMaterial = slotMat;

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

        var bMat = new Material(buildingMat);
        bMat.color = cfg.category switch
        {
            BuildingCategory.Resource => new Color(0.2f, 0.8f, 0.3f),
            BuildingCategory.Defense => new Color(0.9f, 0.2f, 0.2f),
            BuildingCategory.Wall => new Color(0.5f, 0.5f, 0.5f),
            BuildingCategory.Administration => cfg.isHQ ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.3f, 0.9f),
            _ => Color.white
        };
        building.GetComponent<MeshRenderer>().sharedMaterial = bMat;

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

        var slot = root.AddComponent<BuildingSlot>();
        root.AddComponent<BuildingHealth>();
        var visual = root.AddComponent<BuildingSlotVisual>();

        SetField(slot, "assignedBuilding", data);
        SetField(slot, "state", BuildingSlotState.Active);
        SetField(visual, "slotIndicator", indicator.GetComponent<MeshRenderer>());
        SetField(visual, "buildingModel", building.GetComponent<MeshRenderer>());
        SetField(visual, "stateLabel", textMesh);

        var boxCollider = root.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, height / 2f, 0);
        boxCollider.size = new Vector3(2.5f, height + 0.5f, 2.5f);

        return root;
    }

    private static Material CreateOrLoadMaterial(string name, Color color, bool enableInstancing = false)
    {
        string path = $"{MaterialPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (enableInstancing) existing.enableInstancing = true;
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        if (enableInstancing) mat.enableInstancing = true;

        if (color.a < 1f)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void SetField(object obj, string fieldName, object value)
    {
        var type = obj.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            if (field != null) { field.SetValue(obj, value); return; }
            type = type.BaseType;
        }
        Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
    }

    private static EncounterPoolSO CreateEncounterPool()
    {
        string poolPath = $"{EncounterDataPath}/TestEncounterPool.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EncounterPoolSO>(poolPath);
        if (existing != null) return existing;

        // 일상 인카운터 생성
        var daily1 = CreateEncounterDef("D-001", "정찰 보고", "정찰병이 인근에서 보급품을 발견했습니다.",
            EncounterCategory.Daily,
            new EncounterChoice { choiceText = "회수하라", effects = new() { new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "basic", resourceAmount = 10 } } },
            new EncounterChoice { choiceText = "무시하라", effects = new() });

        var daily2 = CreateEncounterDef("D-002", "축제 요청", "주민들이 축제를 열고 싶다고 합니다.",
            EncounterCategory.Daily,
            new EncounterChoice { choiceText = "허가", effects = new() { new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "basic", resourceAmount = -8 } } },
            new EncounterChoice { choiceText = "불허", effects = new() });

        var daily3 = CreateEncounterDef("D-003", "생산 호황", "오늘 생산 시설의 효율이 좋습니다!",
            EncounterCategory.Daily,
            new EncounterChoice { choiceText = "더 돌려!", effects = new() { new ChoiceEffect { effectType = EffectType.Buff, buffType = BuffType.ProductionBonus, buffValue = 0.3f, buffDuration = 3 } } },
            new EncounterChoice { choiceText = "무리하지 말자", effects = new() { new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "basic", resourceAmount = 5 } } });

        var daily4 = CreateEncounterDef("D-004", "의문의 물자", "기지 앞에 정체불명의 물자가 놓여있습니다.",
            EncounterCategory.Daily,
            new EncounterChoice { choiceText = "사용한다", effects = new() { new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "defense", resourceAmount = 5 }, new ChoiceEffect { effectType = EffectType.WorkerInjury, workerAmount = 1 } } },
            new EncounterChoice { choiceText = "버린다", effects = new() });

        // 중요 인카운터 생성
        var major1 = CreateEncounterDef("M-001", "생존자 발견", "폐허에서 생존자 집단을 발견했습니다.",
            EncounterCategory.Major,
            new EncounterChoice { choiceText = "식량 나눔", effects = new() { new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "basic", resourceAmount = -10 }, new ChoiceEffect { effectType = EffectType.WorkerChange, workerAmount = 2 } } },
            new EncounterChoice { choiceText = "교역 제안", costResourceId = "advanced", costAmount = 15, effects = new() { new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "basic", resourceAmount = 30 } } },
            new EncounterChoice { choiceText = "전투 태세", requiredBuildingName = "Arrow Tower", effects = new() { new ChoiceEffect { effectType = EffectType.Buff, buffType = BuffType.AttackBonus, buffValue = 0.25f, buffDuration = 3 }, new ChoiceEffect { effectType = EffectType.WorkerChange, workerAmount = 1 } } });

        // 풀 생성
        var pool = ScriptableObject.CreateInstance<EncounterPoolSO>();
        pool.dailyEncounters = new() { daily1, daily2, daily3, daily4 };
        pool.majorEncounters = new() { major1 };

        AssetDatabase.CreateAsset(pool, poolPath);
        return pool;
    }

    private static EncounterDefinitionSO CreateEncounterDef(string id, string name, string desc,
        EncounterCategory category, params EncounterChoice[] choices)
    {
        string path = $"{EncounterDataPath}/{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
        so.encounterName = name;
        so.description = desc;
        so.category = category;
        so.choices = new List<EncounterChoice>(choices);
        so.weight = 1f;

        AssetDatabase.CreateAsset(so, path);
        return so;
    }
}
