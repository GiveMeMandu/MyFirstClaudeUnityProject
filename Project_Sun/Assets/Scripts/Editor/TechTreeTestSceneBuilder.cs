using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSun.Construction;
using ProjectSun.Resource;
using ProjectSun.Turn;
using ProjectSun.Workforce;
using ProjectSun.TechTree;
using ProjectSun.TechTree.Testing;
using ProjectSun.Turn.Testing;
using ProjectSun.Workforce.Testing;

/// <summary>
/// 기술 트리 시스템 테스트 씬 자동 생성.
/// 연구 건물 + TechTreeManager + UI를 포함한 테스트 환경.
/// </summary>
public static class TechTreeTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TechTreeTest.unity";
    private const string DataPath = "Assets/Data";
    private const string TechTreeDataPath = "Assets/Data/TechTree";
    private const string ConstructionDataPath = "Assets/Data/Construction";
    private const string MaterialPath = "Assets/Materials/Testing";

    [MenuItem("ProjectSun/Create TechTree Test Scene")]
    public static void CreateTestScene()
    {
        EnsureDirectories();

        // 머티리얼
        Material groundMat = CreateOrLoadMaterial("Ground", new Color(0.45f, 0.65f, 0.35f));

        // SO 에셋
        var techTreeData = CreateTechTreeData();
        var researchBuildingData = CreateResearchBuildingData();

        // 씬
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGround(groundMat);
        CreateCamera();
        CreateLight();

        // 연구 건물 슬롯
        var researchSlotGO = CreateResearchBuildingSlot(researchBuildingData);
        var researchSlot = researchSlotGO.GetComponent<BuildingSlot>();

        // BuildingManager
        var managerGO = new GameObject("BuildingManager");
        var buildingMgr = managerGO.AddComponent<BuildingManager>();
        SetField(buildingMgr, "allSlots", new List<BuildingSlot> { researchSlot });

        // ResourceManager
        var resourceGO = new GameObject("ResourceManager");
        var resourceMgr = resourceGO.AddComponent<ResourceManager>();
        SetField(resourceMgr, "buildingManager", buildingMgr);

        // WorkforceManager
        var workforceGO = new GameObject("WorkforceManager");
        var workforceMgr = workforceGO.AddComponent<WorkforceManager>();
        SetField(workforceMgr, "buildingManager", buildingMgr);
        SetField(workforceMgr, "totalWorkers", 3);

        // ResourceManager에 WorkforceManager 연결
        SetField(resourceMgr, "workforceManager", workforceMgr);

        // BuildingManager에 ResourceManager 연결
        SetField(buildingMgr, "resourceManager", resourceMgr);

        // TurnManager (간소화)
        var turnGO = new GameObject("TurnManager");
        var turnMgr = turnGO.AddComponent<TurnManager>();
        SetField(turnMgr, "buildingManager", buildingMgr);
        SetField(turnMgr, "workforceManager", workforceMgr);
        SetField(turnMgr, "resourceManager", resourceMgr);

        // ScenarioData 필요 — 기존 에셋 로드 또는 최소 생성
        var scenarioData = LoadOrCreateMinimalScenario();
        SetField(turnMgr, "scenarioData", scenarioData);

        // ScreenFader (TurnManager 필요)
        var faderGO = new GameObject("ScreenFader");
        var faderCanvas = faderGO.AddComponent<Canvas>();
        faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        faderCanvas.sortingOrder = 999;
        faderGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        var fader = faderGO.AddComponent<ScreenFader>();
        SetField(turnMgr, "screenFader", fader);

        // ToastMessage
        var toastGO = new GameObject("ToastMessage");
        var toast = toastGO.AddComponent<ToastMessage>();
        SetField(turnMgr, "toastMessage", toast);

        // TechTreeManager
        var techTreeGO = new GameObject("TechTreeManager");
        var techTreeMgr = techTreeGO.AddComponent<TechTreeManager>();
        SetField(techTreeMgr, "techTreeData", techTreeData);
        SetField(techTreeMgr, "buildingManager", buildingMgr);
        SetField(techTreeMgr, "resourceManager", resourceMgr);
        SetField(techTreeMgr, "turnManager", turnMgr);
        SetField(techTreeMgr, "workforceManager", workforceMgr);

        // TechTreeUI
        var techTreeUIGO = new GameObject("TechTreeUI");
        var techTreeUI = techTreeUIGO.AddComponent<TechTreeUI>();
        SetField(techTreeUI, "techTreeManager", techTreeMgr);

        // ResourceUI
        var resourceUIGO = new GameObject("ResourceUI");
        var resourceUI = resourceUIGO.AddComponent<ProjectSun.Resource.Testing.ResourceUI>();
        SetField(resourceUI, "resourceManager", resourceMgr);

        // TurnTestController (턴 종료 버튼 + 턴/페이즈 표시)
        var cam = Camera.main;
        var turnCtrlGO = new GameObject("TurnTestController");
        var turnCtrl = turnCtrlGO.AddComponent<TurnTestController>();
        SetField(turnCtrl, "turnManager", turnMgr);
        SetField(turnCtrl, "buildingManager", buildingMgr);
        SetField(turnCtrl, "mainCamera", cam);

        // WorkforceTestController (인력 배치 UI)
        var workforceCtrlGO = new GameObject("WorkforceTestController");
        var workforceCtrl = workforceCtrlGO.AddComponent<WorkforceTestController>();
        SetField(workforceCtrl, "workforceManager", workforceMgr);
        SetField(workforceCtrl, "buildingManager", buildingMgr);
        SetField(workforceCtrl, "turnManager", turnMgr);
        SetField(workforceCtrl, "mainCamera", cam);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"TechTree test scene created at {ScenePath}");
        Debug.Log("1) 연구 건물에 인력 배치 필요 (WorkforceManager)");
        Debug.Log("2) 화면 우측 상단 'Research' 버튼으로 트리 UI 열기");
        Debug.Log("3) 노드 클릭으로 연구 시작 → 턴 종료 시 진행");
    }

    // ── SO 에셋 생성 ────────────────────────────────────────

    private static TechTreeDataSO CreateTechTreeData()
    {
        string path = $"{TechTreeDataPath}/PoC_TechTree.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TechTreeDataSO>(path);
        if (existing != null) return existing;

        // 노드 생성
        var nodeEconomy1 = CreateTechNode("EconBasic", "효율적 경작", "자원 건물 생산량 +20%",
            TechCategory.Economy, 2, new List<ResourceCost> { new() { resourceId = "basic", amount = 10 } },
            new Vector2(0, 0),
            new List<TechNodeEffect> { new() { effectType = TechEffectType.StatBonus, targetId = "Farm", value = 20f, description = "Farm 생산량 +20%" } });

        var nodeEconomy2 = CreateTechNode("EconAdv", "개량 채굴", "고급 자원 생산량 +20%",
            TechCategory.Economy, 3, new List<ResourceCost> { new() { resourceId = "basic", amount = 15 } },
            new Vector2(1, 0),
            new List<TechNodeEffect> { new() { effectType = TechEffectType.StatBonus, targetId = "Mine", value = 20f, description = "Mine 생산량 +20%" } });

        var nodeEconomy3 = CreateTechNode("EconMaster", "관개 시스템", "모든 자원 건물 생산량 +15%",
            TechCategory.Economy, 4, new List<ResourceCost> { new() { resourceId = "basic", amount = 20 }, new() { resourceId = "advanced", amount = 10 } },
            new Vector2(0.5f, 1),
            new List<TechNodeEffect> { new() { effectType = TechEffectType.StatBonus, targetId = "AllResource", value = 15f, description = "모든 자원 건물 +15%" } },
            new List<TechNodeSO> { nodeEconomy1, nodeEconomy2 });

        var nodeDefense1 = CreateTechNode("DefBasic", "강화 화살", "타워 데미지 +25%",
            TechCategory.Defense, 2, new List<ResourceCost> { new() { resourceId = "basic", amount = 12 } },
            new Vector2(0, 0),
            new List<TechNodeEffect> { new() { effectType = TechEffectType.StatBonus, targetId = "ArrowTower", value = 25f, description = "Arrow Tower 데미지 +25%" } });

        var nodeDefense2 = CreateTechNode("DefWall", "강화 성벽", "방벽 HP +50%",
            TechCategory.Defense, 2, new List<ResourceCost> { new() { resourceId = "basic", amount = 10 } },
            new Vector2(1, 0),
            new List<TechNodeEffect> { new() { effectType = TechEffectType.StatBonus, targetId = "Wall", value = 50f, description = "Wall HP +50%" } });

        var nodeDefense3 = CreateTechNode("DefSlot", "추가 방어 거점", "숨겨진 방어 슬롯 공개",
            TechCategory.Defense, 3, new List<ResourceCost> { new() { resourceId = "basic", amount = 15 }, new() { resourceId = "advanced", amount = 8 } },
            new Vector2(0.5f, 1),
            new List<TechNodeEffect> { new() { effectType = TechEffectType.SlotReveal, targetId = "DefenseSlot_Hidden1", value = 1f, description = "숨겨진 방어 슬롯 공개" } },
            new List<TechNodeSO> { nodeDefense1 });

        var nodeUtility1 = CreateTechNode("UtilBuild", "건설 효율", "건설 인력 슬롯 +1",
            TechCategory.Utility, 2, new List<ResourceCost> { new() { resourceId = "basic", amount = 10 } },
            new Vector2(0, 0),
            new List<TechNodeEffect> { new() { effectType = TechEffectType.BuildingSlotAdd, targetId = "Construction", value = 1f, description = "건설 인력 슬롯 +1" } });

        // 카테고리 생성
        var catEconomy = CreateCategory("Economy", "경제", TechCategory.Economy, new List<TechNodeSO> { nodeEconomy1, nodeEconomy2, nodeEconomy3 });
        var catDefense = CreateCategory("Defense", "방어", TechCategory.Defense, new List<TechNodeSO> { nodeDefense1, nodeDefense2, nodeDefense3 });
        var catUtility = CreateCategory("Utility", "건설/지원", TechCategory.Utility, new List<TechNodeSO> { nodeUtility1 });

        // 트리 생성
        var tree = ScriptableObject.CreateInstance<TechTreeDataSO>();
        tree.treeName = "PoC 기술 트리";
        tree.description = "PoC용 범용 기술 트리 (7개 노드)";
        tree.researchPointsPerWorker = 1f;
        tree.categories = new List<TechTreeCategorySO> { catEconomy, catDefense, catUtility };

        AssetDatabase.CreateAsset(tree, path);
        return tree;
    }

    private static TechNodeSO CreateTechNode(string id, string nodeName, string description,
        TechCategory category, int requiredPoints, List<ResourceCost> cost, Vector2 position,
        List<TechNodeEffect> effects, List<TechNodeSO> prerequisites = null)
    {
        string path = $"{TechTreeDataPath}/Node_{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TechNodeSO>(path);
        if (existing != null) return existing;

        var node = ScriptableObject.CreateInstance<TechNodeSO>();
        node.nodeName = nodeName;
        node.description = description;
        node.category = category;
        node.requiredResearchPoints = requiredPoints;
        node.researchCost = cost ?? new List<ResourceCost>();
        node.nodePosition = position;
        node.effects = effects ?? new List<TechNodeEffect>();
        node.prerequisites = prerequisites ?? new List<TechNodeSO>();

        AssetDatabase.CreateAsset(node, path);
        return node;
    }

    private static TechTreeCategorySO CreateCategory(string id, string categoryName, TechCategory category, List<TechNodeSO> nodes)
    {
        string path = $"{TechTreeDataPath}/Category_{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TechTreeCategorySO>(path);
        if (existing != null) return existing;

        var cat = ScriptableObject.CreateInstance<TechTreeCategorySO>();
        cat.categoryName = categoryName;
        cat.category = category;
        cat.nodes = nodes;

        AssetDatabase.CreateAsset(cat, path);
        return cat;
    }

    private static BuildingData CreateResearchBuildingData()
    {
        string path = $"{ConstructionDataPath}/ResearchLab.asset";
        var existing = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<BuildingData>();
        data.buildingName = "Research Lab";
        data.description = "기술 연구를 수행하는 건물";
        data.category = BuildingCategory.Research;
        data.isHeadquarters = false;
        data.constructionTurns = 1;
        data.maxHP = 100f;
        data.autoRepairRate = 25f;
        data.repairTurns = 2;
        data.baseWorkerSlots = 2;
        data.maxConstructionWorkers = 1;
        data.tier = 1;

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static ScenarioDataSO LoadOrCreateMinimalScenario()
    {
        // 기존 시나리오 재사용
        string existingPath = "Assets/Data/Turn/TestScenario.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ScenarioDataSO>(existingPath);
        if (existing != null) return existing;

        // 없으면 최소 생성
        string path = $"{TechTreeDataPath}/MinimalScenario.asset";
        var scenario = AssetDatabase.LoadAssetAtPath<ScenarioDataSO>(path);
        if (scenario != null) return scenario;

        scenario = ScriptableObject.CreateInstance<ScenarioDataSO>();
        AssetDatabase.CreateAsset(scenario, path);
        return scenario;
    }

    // ── 씬 오브젝트 생성 ────────────────────────────────────

    private static GameObject CreateResearchBuildingSlot(BuildingData data)
    {
        var root = new GameObject("Slot_ResearchLab");
        root.transform.position = Vector3.zero;

        var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "BuildingModel";
        building.transform.SetParent(root.transform, false);
        building.transform.localPosition = new Vector3(0, 0.75f, 0);
        building.transform.localScale = new Vector3(2f, 1.5f, 2f);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.5f, 0.9f); // 연구소 = 파란색
        building.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0, 2f, 0);
        labelGO.transform.rotation = Quaternion.Euler(45, 0, 0);
        var textMesh = labelGO.AddComponent<TextMesh>();
        textMesh.text = "Research Lab";
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.15f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        var slot = root.AddComponent<BuildingSlot>();
        root.AddComponent<BuildingHealth>();

        SetField(slot, "assignedBuilding", data);
        SetField(slot, "state", BuildingSlotState.Active);

        return root;
    }

    private static void CreateGround(Material mat)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
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
        cam.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
        camGO.transform.position = new Vector3(0, 15, -10);
        camGO.transform.rotation = Quaternion.Euler(55, 0, 0);
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

    // ── 유틸리티 ────────────────────────────────────────────

    private static void EnsureDirectories()
    {
        string[] dirs = { DataPath, TechTreeDataPath, ConstructionDataPath, "Assets/Materials", MaterialPath, "Assets/Scenes" };
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

    private static Material CreateOrLoadMaterial(string name, Color color)
    {
        string path = $"{MaterialPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
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
}
