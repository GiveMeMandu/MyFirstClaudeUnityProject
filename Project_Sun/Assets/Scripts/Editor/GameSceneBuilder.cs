using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Defense;
using ProjectSun.V2.Defense.Bridge;

/// <summary>
/// V2 통합 게임 씬 자동 빌더.
/// GameDirector + 단일 GameUIController + Bridge Layer를 한 씬에 배치.
/// Menu: Window > Project Sun > Build Game Scene
/// </summary>
public static class GameSceneBuilder
{
    const string ScenePath = "Assets/Scenes/V2_Game.unity";
    const string PanelSettingsPath = "Assets/UI/PanelSettings/DefaultPanelSettings.asset";
    const string GameUIPath = "Assets/UI/UXML/Screens/GameUI.uxml";

    [MenuItem("Window/Project Sun/Build Game Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Panel Settings
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);

        // ── 1. GameDirector ──
        var directorGO = new GameObject("GameDirector");
        var director = directorGO.AddComponent<GameDirector>();
        var phaseManager = directorGO.AddComponent<PhaseManager>();
        var timeScale = directorGO.AddComponent<TimeScaleController>();
        var gameOver = directorGO.AddComponent<GameOverManager>();
        var autoSave = directorGO.AddComponent<AutoSaveHandler>();
        var flowLogger = directorGO.AddComponent<ResourceFlowLogger>();
        var explorationBridge = directorGO.AddComponent<ExplorationBridge>();
        var encounterBridge = directorGO.AddComponent<EncounterBridge>();
        var techTreeBridge = directorGO.AddComponent<TechTreeBridge>();
        var policyBridge = directorGO.AddComponent<PolicyBridge>();

        // Wire core refs
        SetField(director, "phaseManager", phaseManager);
        SetField(director, "timeScaleController", timeScale);
        SetField(director, "gameOverManager", gameOver);
        SetField(director, "autoSaveHandler", autoSave);
        SetField(director, "resourceFlowLogger", flowLogger);
        SetField(director, "explorationBridge", explorationBridge);
        SetField(director, "encounterBridge", encounterBridge);
        SetField(director, "techTreeBridge", techTreeBridge);
        SetField(director, "policyBridge", policyBridge);
        SetField(autoSave, "phaseManager", phaseManager);
        SetField(gameOver, "battleUIBridge", null); // wired below

        // ── 2. Bridge Layer ──
        var bridgeGO = new GameObject("BridgeLayer");
        var battleInit = bridgeGO.AddComponent<BattleInitializer>();
        var resultCollector = bridgeGO.AddComponent<BattleResultCollector>();
        var uiBridge = bridgeGO.AddComponent<BattleUIBridge>();

        var battleScene = bridgeGO.AddComponent<BattleSceneSetup>();
        bridgeGO.AddComponent<ProjectSun.Defense.ECS.ECSSystemBootstrap>();

        SetField(director, "battleInitializer", battleInit);
        SetField(director, "resultCollector", resultCollector);
        SetField(director, "battleUIBridge", uiBridge);
        SetField(director, "battleSceneSetup", battleScene);
        SetField(gameOver, "battleUIBridge", uiBridge);
        SetField(battleScene, "gameDirector", director);
        SetField(battleScene, "battleUIBridge", uiBridge);

        // ── 3. AudioManager ──
        var audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();

        // ── 4. Single GameUI (replaces 8 individual UI presenters) ──
        var uiGO = new GameObject("GameUI");
        var uiDoc = uiGO.AddComponent<UIDocument>();
        var uiController = uiGO.AddComponent<GameUIController>();

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameUIPath);
        if (uxml != null) uiDoc.visualTreeAsset = uxml;
        else Debug.LogWarning($"[GameSceneBuilder] UXML not found: {GameUIPath}");

        if (panelSettings != null) uiDoc.panelSettings = panelSettings;

        // Wire UIDocument to controller
        SetField(uiController, "uiDocument", uiDoc);

        // Wire references to GameUIController
        SetField(uiController, "director", director);
        SetField(uiController, "battleUIBridge", uiBridge);
        SetField(uiController, "timeScaleController", timeScale);
        SetField(uiController, "gameOverManager", gameOver);
        SetField(uiController, "explorationBridge", explorationBridge);
        SetField(uiController, "encounterBridge", encounterBridge);
        SetField(uiController, "techTreeBridge", techTreeBridge);
        SetField(uiController, "policyBridge", policyBridge);

        // Wire controller to director
        SetField(director, "uiController", uiController);

        // ── 5. Camera ──
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 40, -30);
            cam.transform.rotation = Quaternion.Euler(55, 0, 0);
            cam.fieldOfView = 50f;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // ── 6. Light ──
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ── Save Scene ──
        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[GameSceneBuilder] Scene saved: {ScenePath} — Play 버튼으로 확인하세요.");
    }

    static void SetField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var type = target.GetType();
        var field = type.GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(target, value);
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            var folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
