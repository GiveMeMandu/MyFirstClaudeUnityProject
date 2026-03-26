using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectSun.Construction;
using ProjectSun.Encounter;
using ProjectSun.Exploration;
using ProjectSun.Exploration.Testing;
using ProjectSun.Resource;
using ProjectSun.Turn;
using ProjectSun.Workforce;

/// <summary>
/// 탐사 시스템 테스트용 SO 에셋 생성 + 기존 씬에 탐사 시스템 추가.
/// </summary>
public static class ExplorationTestSceneBuilder
{
    private const string ExplorationDataPath = "Assets/Data/Exploration";
    private const string EncounterDataPath = "Assets/Data/Encounter";
    private const string ConstructionDataPath = "Assets/Data/Construction";

    [MenuItem("ProjectSun/Create Exploration Test Data")]
    public static void CreateExplorationTestData()
    {
        EnsureDirectories();
        CreateExplorationMapAsset();
        CreateExplorationBuildingData();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ExplorationTestSceneBuilder] Exploration test data created.");
    }

    [MenuItem("ProjectSun/Add Exploration to Scene")]
    public static void AddExplorationToScene()
    {
        EnsureDirectories();

        var mapSO = CreateExplorationMapAsset();
        CreateExplorationBuildingData();

        // ExplorationManager 찾기 또는 생성
        var explorationMgr = Object.FindObjectOfType<ExplorationManager>();
        if (explorationMgr == null)
        {
            var go = new GameObject("ExplorationManager");
            explorationMgr = go.AddComponent<ExplorationManager>();
        }

        // 연동 설정
        var workforceMgr = Object.FindObjectOfType<WorkforceManager>();
        var buildingMgr = Object.FindObjectOfType<BuildingManager>();
        var turnMgr = Object.FindObjectOfType<TurnManager>();

        SetField(explorationMgr, "mapData", mapSO);
        if (workforceMgr != null)
            SetField(explorationMgr, "workforceManager", workforceMgr);
        if (buildingMgr != null)
            SetField(explorationMgr, "buildingManager", buildingMgr);

        // TurnManager에 ExplorationManager 연결
        if (turnMgr != null)
            SetField(turnMgr, "explorationManager", explorationMgr);

        // ExplorationUI
        var explorationUI = Object.FindObjectOfType<ExplorationUI>();
        if (explorationUI == null)
        {
            var uiGO = new GameObject("ExplorationUI");
            explorationUI = uiGO.AddComponent<ExplorationUI>();
        }
        SetField(explorationUI, "explorationManager", explorationMgr);
        if (workforceMgr != null)
            SetField(explorationUI, "workforceManager", workforceMgr);

        EditorUtility.SetDirty(explorationMgr);
        if (turnMgr != null) EditorUtility.SetDirty(turnMgr);

        AssetDatabase.SaveAssets();
        Debug.Log("[ExplorationTestSceneBuilder] Exploration system added to current scene.");
    }

    public static ExplorationMapSO CreateExplorationMapAsset()
    {
        string mapPath = $"{ExplorationDataPath}/TestExplorationMap.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ExplorationMapSO>(mapPath);
        if (existing != null) return existing;

        // 노드 SO 생성
        var baseNode = CreateNodeSO("Base", "기지", "본부", ExplorationNodeType.Resource, "기지");
        var resourceNode1 = CreateNodeSO("ResourceOutpost", "보급 기지", "소규모 물자 창고를 발견했다.",
            ExplorationNodeType.Resource, "물자?", "basic", 15);
        var resourceNode2 = CreateNodeSO("HiddenCache", "은닉 물자", "고급 물자가 숨겨져 있다.",
            ExplorationNodeType.Resource, "숨겨진 것?", "advanced", 10);
        var reconNode1 = CreateNodeSO("WatchTower", "감시탑", "높은 곳에서 적의 동향을 파악할 수 있다.",
            ExplorationNodeType.Recon, "높은 곳?", reconTurns: 2);
        var reconNode2 = CreateNodeSO("EnemyCamp", "적 야영지", "적의 주둔지를 발견했다. 다음 공격 정보를 알 수 있다.",
            ExplorationNodeType.Recon, "적 흔적?", reconTurns: 1);
        var encounterNode1 = CreateEncounterNodeSO("AbandonedVillage", "폐촌", "사람이 살았던 흔적이 남아있다.",
            "발견물?");
        var encounterNode2 = CreateEncounterNodeSO("MysteriousRuins", "고대 유적", "알 수 없는 구조물이 서 있다.",
            "구조물?");
        var techNode1 = CreateNodeSO("ResearchLab", "연구 시설", "이전 시대의 연구 시설을 발견했다.",
            ExplorationNodeType.Tech, "시설?", techId: "basic_research");
        var resourceNode3 = CreateNodeSO("DefenseCache", "방어 물자", "방어 자원이 쌓여있다.",
            ExplorationNodeType.Resource, "물자?", "defense", 8);
        var resourceNode4 = CreateNodeSO("RichMine", "풍부한 광산", "고급 자원이 대량으로 매장되어 있다.",
            ExplorationNodeType.Resource, "광물?", "advanced", 20);

        // 맵 SO 생성
        var mapSO = ScriptableObject.CreateInstance<ExplorationMapSO>();
        mapSO.mapName = "테스트 탐사 맵";
        mapSO.mapDescription = "PoC 테스트용 10노드 맵";
        mapSO.baseNodeIndex = 0;

        // 노드 배치 (정규화 좌표 0~1)
        mapSO.nodes = new List<MapNodeEntry>
        {
            new() { nodeData = baseNode,       mapPosition = new Vector2(0.5f, 0.85f) },   // 0: 기지 (하단 중앙)
            new() { nodeData = resourceNode1,  mapPosition = new Vector2(0.3f, 0.7f) },    // 1: 보급 기지
            new() { nodeData = reconNode1,     mapPosition = new Vector2(0.7f, 0.7f) },    // 2: 감시탑
            new() { nodeData = encounterNode1, mapPosition = new Vector2(0.15f, 0.5f) },   // 3: 폐촌
            new() { nodeData = resourceNode2,  mapPosition = new Vector2(0.45f, 0.45f) },  // 4: 은닉 물자
            new() { nodeData = reconNode2,     mapPosition = new Vector2(0.75f, 0.45f) },  // 5: 적 야영지
            new() { nodeData = techNode1,      mapPosition = new Vector2(0.25f, 0.25f) },  // 6: 연구 시설
            new() { nodeData = encounterNode2, mapPosition = new Vector2(0.55f, 0.2f) },   // 7: 고대 유적
            new() { nodeData = resourceNode3,  mapPosition = new Vector2(0.85f, 0.25f) },  // 8: 방어 물자
            new() { nodeData = resourceNode4,  mapPosition = new Vector2(0.4f, 0.05f) },   // 9: 풍부한 광산
        };

        // 간선 (연결 + 소요 턴수)
        mapSO.edges = new List<MapEdge>
        {
            new() { nodeIndexA = 0, nodeIndexB = 1, travelTurns = 1 },  // 기지 ↔ 보급기지
            new() { nodeIndexA = 0, nodeIndexB = 2, travelTurns = 1 },  // 기지 ↔ 감시탑
            new() { nodeIndexA = 1, nodeIndexB = 3, travelTurns = 1 },  // 보급기지 ↔ 폐촌
            new() { nodeIndexA = 1, nodeIndexB = 4, travelTurns = 1 },  // 보급기지 ↔ 은닉물자
            new() { nodeIndexA = 2, nodeIndexB = 4, travelTurns = 1 },  // 감시탑 ↔ 은닉물자
            new() { nodeIndexA = 2, nodeIndexB = 5, travelTurns = 1 },  // 감시탑 ↔ 적 야영지
            new() { nodeIndexA = 3, nodeIndexB = 6, travelTurns = 2 },  // 폐촌 ↔ 연구시설 (2턴)
            new() { nodeIndexA = 4, nodeIndexB = 7, travelTurns = 2 },  // 은닉물자 ↔ 고대유적 (2턴)
            new() { nodeIndexA = 5, nodeIndexB = 8, travelTurns = 2 },  // 적야영지 ↔ 방어물자 (2턴)
            new() { nodeIndexA = 6, nodeIndexB = 9, travelTurns = 1 },  // 연구시설 ↔ 풍부한 광산
            new() { nodeIndexA = 7, nodeIndexB = 9, travelTurns = 1 },  // 고대유적 ↔ 풍부한 광산
        };

        AssetDatabase.CreateAsset(mapSO, mapPath);
        return mapSO;
    }

    private static BuildingData CreateExplorationBuildingData()
    {
        string path = $"{ConstructionDataPath}/ScoutPost.asset";
        var existing = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<BuildingData>();
        data.buildingName = "Scout Post";
        data.description = "원정대를 파견할 수 있는 탐사 건물";
        data.category = BuildingCategory.Exploration;
        data.constructionTurns = 1;
        data.maxHP = 80f;
        data.autoRepairRate = 20f;
        data.repairTurns = 2;
        data.baseWorkerSlots = 1;
        data.maxConstructionWorkers = 1;
        data.constructionCost = new List<ResourceCost>
        {
            new() { resourceId = "basic", amount = 30 },
            new() { resourceId = "advanced", amount = 10 }
        };

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static ExplorationNodeSO CreateNodeSO(string fileName, string nodeName, string desc,
        ExplorationNodeType type, string hint,
        string resourceId = null, int resourceAmount = 0,
        int reconTurns = 1, string techId = null)
    {
        string path = $"{ExplorationDataPath}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ExplorationNodeSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<ExplorationNodeSO>();
        so.nodeName = nodeName;
        so.description = desc;
        so.nodeType = type;
        so.hintText = hint;

        if (type == ExplorationNodeType.Resource && !string.IsNullOrEmpty(resourceId))
        {
            so.resourceRewards = new List<NodeResourceReward>
            {
                new() { resourceId = resourceId, amount = resourceAmount }
            };
        }

        if (type == ExplorationNodeType.Recon)
        {
            so.reconTurnsAhead = reconTurns;
        }

        if (type == ExplorationNodeType.Tech && !string.IsNullOrEmpty(techId))
        {
            so.techUnlockId = techId;
        }

        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    private static ExplorationNodeSO CreateEncounterNodeSO(string fileName, string nodeName, string desc, string hint)
    {
        // 인카운터 노드용 인카운터 SO 생성
        string encPath = $"{EncounterDataPath}/Exp_{fileName}.asset";
        var existingEnc = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>(encPath);
        if (existingEnc == null)
        {
            existingEnc = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
            existingEnc.encounterName = $"탐사: {nodeName}";
            existingEnc.description = desc;
            existingEnc.category = EncounterCategory.Major;
            existingEnc.weight = 1f;
            existingEnc.choices = new List<EncounterChoice>
            {
                new()
                {
                    choiceText = "조심스럽게 조사",
                    effects = new()
                    {
                        new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "basic", resourceAmount = 10 }
                    }
                },
                new()
                {
                    choiceText = "자원 투자하여 철저 조사",
                    costResourceId = "advanced",
                    costAmount = 5,
                    effects = new()
                    {
                        new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "basic", resourceAmount = 25 },
                        new ChoiceEffect { effectType = EffectType.ResourceChange, resourceId = "advanced", resourceAmount = 10 }
                    }
                },
                new()
                {
                    choiceText = "무시하고 지나감",
                    effects = new()
                }
            };
            AssetDatabase.CreateAsset(existingEnc, encPath);
        }

        // 노드 SO
        string nodePath = $"{ExplorationDataPath}/{fileName}.asset";
        var existingNode = AssetDatabase.LoadAssetAtPath<ExplorationNodeSO>(nodePath);
        if (existingNode != null) return existingNode;

        var so = ScriptableObject.CreateInstance<ExplorationNodeSO>();
        so.nodeName = nodeName;
        so.description = desc;
        so.nodeType = ExplorationNodeType.Encounter;
        so.hintText = hint;
        so.encounterDefinition = existingEnc;

        AssetDatabase.CreateAsset(so, nodePath);
        return so;
    }

    private static void EnsureDirectories()
    {
        string[] dirs = { "Assets/Data", ExplorationDataPath, EncounterDataPath, ConstructionDataPath };
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
