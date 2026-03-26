using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSun.Construction;
using ProjectSun.Resource;
using ProjectSun.Turn;
using ProjectSun.Workforce;
using ProjectSun.WallExpansion;
using ProjectSun.WallExpansion.Testing;

/// <summary>
/// 방벽 확장 시스템 테스트 씬 자동 생성.
/// 건설 슬롯 + 자원 + 턴 + 방벽 확장을 포함한 통합 테스트.
/// </summary>
public static class WallExpansionTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/WallExpansionTest.unity";
    private const string DataPath = "Assets/Data";
    private const string WallDataPath = "Assets/Data/WallExpansion";
    private const string ConstructionDataPath = "Assets/Data/Construction";
    private const string MaterialPath = "Assets/Materials/Testing";
    private const float SlotSpacing = 5f;

    // 초기 슬롯 (Lv.0 — 처음부터 사용 가능)
    private static readonly Vector3[] InitialSlotPositions =
    {
        new(0, 0, 0),       // HQ
        new(-SlotSpacing, 0, 0),
        new(SlotSpacing, 0, 0),
    };

    // Lv.1 확장 슬롯
    private static readonly Vector3[] Level1SlotPositions =
    {
        new(-SlotSpacing * 2, 0, 0),
        new(SlotSpacing * 2, 0, 0),
        new(0, 0, SlotSpacing),
    };

    // Lv.2 확장 슬롯
    private static readonly Vector3[] Level2SlotPositions =
    {
        new(-SlotSpacing, 0, SlotSpacing),
        new(SlotSpacing, 0, SlotSpacing),
        new(0, 0, SlotSpacing * 2),
    };

    // Lv.3 확장 슬롯
    private static readonly Vector3[] Level3SlotPositions =
    {
        new(-SlotSpacing * 2, 0, SlotSpacing),
        new(SlotSpacing * 2, 0, SlotSpacing),
    };

    [MenuItem("ProjectSun/Create Wall Expansion Test Scene")]
    public static void CreateTestScene()
    {
        EnsureDirectories();

        Material groundMat = CreateOrLoadMaterial("Ground_WE", new Color(0.45f, 0.65f, 0.35f));
        Material slotMat = CreateOrLoadMaterial("Slot_WE", new Color(1f, 1f, 1f, 0.3f));
        Material hiddenSlotMat = CreateOrLoadMaterial("HiddenSlot_WE", new Color(0.3f, 0.3f, 0.3f, 0.2f));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5, 1, 5);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // Camera
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 20, -15);
            cam.transform.rotation = Quaternion.Euler(55, 0, 0);
        }

        // Light
        var light = GameObject.Find("Directional Light");
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // ── Managers ──
        var managersGO = new GameObject("--- Managers ---");

        // TurnManager (minimal)
        var turnGO = new GameObject("TurnManager");
        turnGO.transform.SetParent(managersGO.transform);
        var turnManager = turnGO.AddComponent<TurnManager>();

        // ResourceManager
        var resGO = new GameObject("ResourceManager");
        resGO.transform.SetParent(managersGO.transform);
        var resourceManager = resGO.AddComponent<ResourceManager>();

        // BuildingManager
        var buildGO = new GameObject("BuildingManager");
        buildGO.transform.SetParent(managersGO.transform);
        var buildingManager = buildGO.AddComponent<BuildingManager>();

        // WorkforceManager
        var workGO = new GameObject("WorkforceManager");
        workGO.transform.SetParent(managersGO.transform);
        var workforceManager = workGO.AddComponent<WorkforceManager>();

        // WallExpansionManager
        var wallGO = new GameObject("WallExpansionManager");
        wallGO.transform.SetParent(managersGO.transform);
        var wallManager = wallGO.AddComponent<WallExpansionManager>();

        // FeatureUnlockManager
        var unlockGO = new GameObject("FeatureUnlockManager");
        unlockGO.transform.SetParent(managersGO.transform);
        var unlockManager = unlockGO.AddComponent<FeatureUnlockManager>();

        // DefenseRangeController
        var defRangeGO = new GameObject("DefenseRangeController");
        defRangeGO.transform.SetParent(managersGO.transform);
        var defRangeController = defRangeGO.AddComponent<DefenseRangeController>();

        // ── WallExpansionData SO ──
        var expansionData = CreateExpansionDataSO();

        // ── Building Slots ──
        var slotsParent = new GameObject("--- Slots ---");

        // Initial slots (visible)
        var initialSlots = CreateSlotGroup("Initial Slots (Lv.0)", InitialSlotPositions, slotMat, slotsParent.transform, false);

        // Level 1 slots (hidden)
        var lv1Slots = CreateSlotGroup("Level 1 Slots", Level1SlotPositions, hiddenSlotMat, slotsParent.transform, true);
        var lv1Group = lv1Slots.AddComponent<WallExpansionSlotGroup>();
        SetPrivateField(lv1Group, "unlockLevel", 1);
        SetPrivateField(lv1Group, "slots", GetBuildingSlots(lv1Slots));

        // Level 2 slots (hidden)
        var lv2Slots = CreateSlotGroup("Level 2 Slots", Level2SlotPositions, hiddenSlotMat, slotsParent.transform, true);
        var lv2Group = lv2Slots.AddComponent<WallExpansionSlotGroup>();
        SetPrivateField(lv2Group, "unlockLevel", 2);
        SetPrivateField(lv2Group, "slots", GetBuildingSlots(lv2Slots));

        // Level 3 slots (hidden)
        var lv3Slots = CreateSlotGroup("Level 3 Slots", Level3SlotPositions, hiddenSlotMat, slotsParent.transform, true);
        var lv3Group = lv3Slots.AddComponent<WallExpansionSlotGroup>();
        SetPrivateField(lv3Group, "unlockLevel", 3);
        SetPrivateField(lv3Group, "slots", GetBuildingSlots(lv3Slots));

        // ── Wire up references ──
        SetPrivateField(wallManager, "expansionData", expansionData);
        SetPrivateField(wallManager, "resourceManager", resourceManager);
        SetPrivateField(wallManager, "buildingManager", buildingManager);
        SetPrivateField(wallManager, "turnManager", turnManager);
        SetPrivateField(wallManager, "slotGroups", new List<WallExpansionSlotGroup> { lv1Group, lv2Group, lv3Group });

        SetPrivateField(defRangeController, "wallExpansionManager", wallManager);

        // ── Spawn Points ──
        var spawnParent = new GameObject("--- Spawn Points ---");
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            float radius = 22f;
            var sp = new GameObject($"SpawnPoint_{i}");
            sp.transform.SetParent(spawnParent.transform);
            sp.transform.position = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            sp.SetActive(i < 2); // 초기에는 2개만 활성
        }

        // ── Test UI ──
        var uiGO = new GameObject("WallExpansionTestUI");
        var testUI = uiGO.AddComponent<WallExpansionTestUI>();
        SetPrivateField(testUI, "wallExpansionManager", wallManager);
        SetPrivateField(testUI, "resourceManager", resourceManager);
        SetPrivateField(testUI, "turnManager", turnManager);
        SetPrivateField(testUI, "featureUnlockManager", unlockManager);
        SetPrivateField(testUI, "defenseRangeController", defRangeController);

        // ── Save Scene ──
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"[WallExpansionTestSceneBuilder] 테스트 씬 생성 완료: {ScenePath}");
        Debug.Log("  초기 슬롯: 3개 / Lv.1: +3 / Lv.2: +3 / Lv.3: +2");
        Debug.Log("  플레이 후 IMGUI 패널에서 '확장' 버튼으로 테스트");
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder(DataPath))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(WallDataPath))
            AssetDatabase.CreateFolder(DataPath, "WallExpansion");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialPath))
            AssetDatabase.CreateFolder("Assets/Materials", "Testing");
    }

    private static Material CreateOrLoadMaterial(string name, Color color)
    {
        string path = $"{MaterialPath}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static WallExpansionDataSO CreateExpansionDataSO()
    {
        string path = $"{WallDataPath}/TestExpansionData.asset";
        var existing = AssetDatabase.LoadAssetAtPath<WallExpansionDataSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<WallExpansionDataSO>();
        so.GenerateDefault();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    private static GameObject CreateSlotGroup(string groupName, Vector3[] positions, Material mat, Transform parent, bool hidden)
    {
        var group = new GameObject(groupName);
        group.transform.SetParent(parent);

        for (int i = 0; i < positions.Length; i++)
        {
            var slotGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slotGO.name = $"Slot_{groupName}_{i}";
            slotGO.transform.SetParent(group.transform);
            slotGO.transform.position = positions[i];
            slotGO.transform.localScale = new Vector3(2f, 0.1f, 2f);
            slotGO.GetComponent<Renderer>().sharedMaterial = mat;

            var slot = slotGO.AddComponent<BuildingSlot>();
            if (hidden)
            {
                SetPrivateField(slot, "initialState", BuildingSlotState.Hidden);
            }
        }

        return group;
    }

    private static List<BuildingSlot> GetBuildingSlots(GameObject parent)
    {
        var list = new List<BuildingSlot>();
        foreach (Transform child in parent.transform)
        {
            var slot = child.GetComponent<BuildingSlot>();
            if (slot != null) list.Add(slot);
        }
        return list;
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
    }
}
