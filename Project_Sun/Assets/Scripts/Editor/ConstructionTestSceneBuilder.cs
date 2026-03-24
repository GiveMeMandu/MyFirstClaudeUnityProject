using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ProjectSun.Construction;
using ProjectSun.Construction.Testing;

/// <summary>
/// Editor utility that creates a complete construction test scene with
/// ground plane, quarter-view camera, building slots in hex layout, and test UI.
/// </summary>
public static class ConstructionTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ConstructionTest.unity";
    private const string DataPath = "Assets/Data/Construction";
    private const string MaterialPath = "Assets/Materials/Testing";
    private const float SlotSpacing = 4f;

    // Hex layout offsets for 7 slots (center + 6 surrounding)
    private static readonly Vector3[] SlotPositions =
    {
        new(0, 0, 0),            // 0: Center (HQ)
        new(-SlotSpacing, 0, SlotSpacing * 0.6f),   // 1: Top-left
        new(SlotSpacing, 0, SlotSpacing * 0.6f),     // 2: Top-right
        new(-SlotSpacing * 1.2f, 0, 0),              // 3: Left
        new(SlotSpacing * 1.2f, 0, 0),               // 4: Right
        new(-SlotSpacing, 0, -SlotSpacing * 0.6f),   // 5: Bottom-left
        new(SlotSpacing, 0, -SlotSpacing * 0.6f),    // 6: Bottom-right
    };

    private struct SlotConfig
    {
        public string name;
        public BuildingCategory category;
        public bool isHQ;
        public BuildingSlotState initialState;
        public float maxHP;
        public int constructionTurns;
        public float defenseResourceCost;
    }

    private static readonly SlotConfig[] SlotConfigs =
    {
        new() { name = "Headquarters",    category = BuildingCategory.Administration, isHQ = true,  initialState = BuildingSlotState.Active, maxHP = 200, constructionTurns = 1 },
        new() { name = "Farm",            category = BuildingCategory.Resource,       isHQ = false, initialState = BuildingSlotState.Hidden, maxHP = 80,  constructionTurns = 2 },
        new() { name = "Wall",            category = BuildingCategory.Wall,           isHQ = false, initialState = BuildingSlotState.Hidden, maxHP = 150, constructionTurns = 1 },
        new() { name = "Scout Post",      category = BuildingCategory.Exploration,    isHQ = false, initialState = BuildingSlotState.Hidden, maxHP = 60,  constructionTurns = 2 },
        new() { name = "Arrow Tower",     category = BuildingCategory.Defense,        isHQ = false, initialState = BuildingSlotState.Hidden, maxHP = 100, constructionTurns = 3, defenseResourceCost = 10f },
        new() { name = "Admin Office",    category = BuildingCategory.Administration, isHQ = false, initialState = BuildingSlotState.Hidden, maxHP = 80,  constructionTurns = 2 },
        new() { name = "Research Lab",    category = BuildingCategory.Research,       isHQ = false, initialState = BuildingSlotState.Hidden, maxHP = 70,  constructionTurns = 3 },
    };

    [MenuItem("ProjectSun/Create Construction Test Scene")]
    public static void CreateTestScene()
    {
        CreateTestSceneInternal();
    }

    public static void CreateTestSceneInternal()
    {
        EnsureDirectories();

        // Create materials
        Material groundMat = CreateOrLoadMaterial("Ground", new Color(0.45f, 0.65f, 0.35f));
        Material slotMat = CreateOrLoadMaterial("SlotIndicator", new Color(1f, 1f, 1f, 0.3f));
        Material buildingMat = CreateOrLoadMaterial("Building", Color.white);

        // Create BuildingData assets
        BuildingData[] buildingDatas = CreateBuildingDataAssets();

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ground
        CreateGround(groundMat);

        // Camera (quarter-view)
        CreateCamera();

        // Light
        CreateLight();

        // Building slots
        var slots = new List<BuildingSlot>();
        for (int i = 0; i < SlotConfigs.Length; i++)
        {
            var slotGO = CreateBuildingSlot(i, buildingDatas[i], slotMat, buildingMat);
            slots.Add(slotGO.GetComponent<BuildingSlot>());
        }

        // Set up adjacency: all surrounding slots are adjacent to center
        SetupAdjacency(slots);

        // BuildingManager
        var managerGO = new GameObject("BuildingManager");
        var manager = managerGO.AddComponent<BuildingManager>();
        SetPrivateField(manager, "allSlots", new List<BuildingSlot>(slots));

        // HQ starts as Active with health initialized
        var hqSlot = slots[0];
        var hqHealth = hqSlot.GetComponent<BuildingHealth>();
        if (hqHealth != null)
            hqHealth.Initialize(SlotConfigs[0].maxHP, 25f);

        // Test Controller
        var controllerGO = new GameObject("TestController");
        var controller = controllerGO.AddComponent<ConstructionTestController>();
        SetPrivateField(controller, "buildingManager", manager);
        SetPrivateField(controller, "mainCamera", Camera.main);

        // Save scene
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Construction test scene created at {ScenePath}");
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(DataPath))
            AssetDatabase.CreateFolder("Assets/Data", "Construction");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialPath))
            AssetDatabase.CreateFolder("Assets/Materials", "Testing");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static Material CreateOrLoadMaterial(string name, Color color)
    {
        string path = $"{MaterialPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            return existing;
        }

        // Find URP Lit shader
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.color = color;

        // Enable transparency for slot indicator
        if (color.a < 1f)
        {
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static BuildingData[] CreateBuildingDataAssets()
    {
        var datas = new BuildingData[SlotConfigs.Length];

        for (int i = 0; i < SlotConfigs.Length; i++)
        {
            var cfg = SlotConfigs[i];
            string assetPath = $"{DataPath}/{cfg.name.Replace(" ", "")}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<BuildingData>(assetPath);
            if (existing != null)
            {
                datas[i] = existing;
                continue;
            }

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = cfg.name;
            data.category = cfg.category;
            data.isHeadquarters = cfg.isHQ;
            data.constructionTurns = cfg.constructionTurns;
            data.maxHP = cfg.maxHP;
            data.autoRepairRate = 25f;
            data.repairTurns = 2;
            data.baseWorkerSlots = 2;
            data.maxConstructionWorkers = 2;
            data.defenseResourceCostPerSlot = cfg.defenseResourceCost;
            data.tier = 1;

            AssetDatabase.CreateAsset(data, assetPath);
            datas[i] = data;
        }

        return datas;
    }

    private static void CreateGround(Material mat)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
        ground.isStatic = true;
    }

    private static void CreateCamera()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);

        // Quarter-view angle
        camGO.transform.position = new Vector3(0, 15, -15);
        camGO.transform.rotation = Quaternion.Euler(45, 0, 0);

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

        // Slot indicator (flat cylinder as disc)
        var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "SlotIndicator";
        indicator.transform.SetParent(root.transform, false);
        indicator.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
        indicator.transform.localPosition = new Vector3(0, 0.025f, 0);
        var indicatorRenderer = indicator.GetComponent<MeshRenderer>();
        indicatorRenderer.sharedMaterial = slotMat;

        // Building model (cube)
        var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "BuildingModel";
        building.transform.SetParent(root.transform, false);
        building.transform.localPosition = new Vector3(0, 0.6f, 0);
        building.transform.localScale = Vector3.one;
        var buildingRenderer = building.GetComponent<MeshRenderer>();
        buildingRenderer.sharedMaterial = buildingMat;
        building.SetActive(false);

        // State label (TextMesh)
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0, 2.2f, 0);
        // Face camera (billboard) - rotate to face quarter-view camera
        labelGO.transform.rotation = Quaternion.Euler(45, 0, 0);
        var textMesh = labelGO.AddComponent<TextMesh>();
        textMesh.text = cfg.name;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.15f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        // Add components
        var slot = root.AddComponent<BuildingSlot>();
        root.AddComponent<BuildingHealth>();
        root.AddComponent<BuildingVFX>();

        var visual = root.AddComponent<BuildingSlotVisual>();

        // Set serialized fields via reflection
        SetPrivateField(slot, "assignedBuilding", data);
        SetPrivateField(slot, "state", cfg.initialState);

        SetPrivateField(visual, "slotIndicator", indicatorRenderer);
        SetPrivateField(visual, "buildingModel", buildingRenderer);
        SetPrivateField(visual, "stateLabel", textMesh);

        // Add collider to root for raycast selection
        var boxCollider = root.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, 0.5f, 0);
        boxCollider.size = new Vector3(2.5f, 1.5f, 2.5f);

        return root;
    }

    private static void SetupAdjacency(List<BuildingSlot> slots)
    {
        // Center (0) is adjacent to all surrounding (1-6)
        var centerAdjacent = new List<BuildingSlot> { slots[1], slots[2], slots[3], slots[4], slots[5], slots[6] };
        SetPrivateField(slots[0], "adjacentSlots", centerAdjacent);

        // Each surrounding slot is adjacent to center and its neighbors
        // Layout: 1=TL, 2=TR, 3=L, 4=R, 5=BL, 6=BR
        int[][] neighborMap = {
            new[] { 0, 3, 2 },    // 1 (TL): center, left, top-right
            new[] { 0, 1, 4 },    // 2 (TR): center, top-left, right
            new[] { 0, 1, 5 },    // 3 (L): center, top-left, bottom-left
            new[] { 0, 2, 6 },    // 4 (R): center, top-right, bottom-right
            new[] { 0, 3, 6 },    // 5 (BL): center, left, bottom-right
            new[] { 0, 5, 4 },    // 6 (BR): center, bottom-left, right
        };

        for (int i = 0; i < neighborMap.Length; i++)
        {
            var adjacent = new List<BuildingSlot>();
            foreach (int idx in neighborMap[i])
                adjacent.Add(slots[idx]);
            SetPrivateField(slots[i + 1], "adjacentSlots", adjacent);
        }
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
