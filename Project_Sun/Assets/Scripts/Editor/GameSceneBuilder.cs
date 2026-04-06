using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Defense;
using ProjectSun.V2.Defense.Bridge;
using ProjectSun.V2.Construction;
using ProjectSun.V2.Workforce;
using ProjectSun.V2.Exploration;
using ProjectSun.V2.UI;

/// <summary>
/// V2 통합 게임 씬 자동 빌더.
/// GameDirector + 모든 UI Presenter + Bridge Layer를 한 씬에 배치.
/// Menu: Window > Project Sun > Build Game Scene
/// </summary>
public static class GameSceneBuilder
{
    const string ScenePath = "Assets/Scenes/V2_Game.unity";
    const string PanelSettingsPath = "Assets/UI/PanelSettings/DefaultPanelSettings.asset";

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

        // Wire core refs
        SetField(director, "phaseManager", phaseManager);
        SetField(director, "timeScaleController", timeScale);
        SetField(director, "gameOverManager", gameOver);
        SetField(director, "autoSaveHandler", autoSave);
        SetField(director, "resourceFlowLogger", flowLogger);
        SetField(director, "explorationBridge", explorationBridge);
        SetField(director, "encounterBridge", encounterBridge);
        SetField(autoSave, "phaseManager", phaseManager);
        SetField(gameOver, "battleUIBridge", null); // wired below

        // ── 2. Bridge Layer ──
        var bridgeGO = new GameObject("BridgeLayer");
        var battleInit = bridgeGO.AddComponent<BattleInitializer>();
        var resultCollector = bridgeGO.AddComponent<BattleResultCollector>();
        var uiBridge = bridgeGO.AddComponent<BattleUIBridge>();

        var battleScene = bridgeGO.AddComponent<BattleSceneSetup>();

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

        // ── 4. UI Presenters (sortOrder: 콘텐츠=0, HUD=100, 오버레이=200) ──
        var menuPresenter = CreatePresenter<MenuScreenPresenter>(
            "UI_Menu", "Assets/UI/UXML/Screens/MenuScreens.uxml", panelSettings, 200);
        var battleHUD = CreatePresenter<BattleHUDPresenter>(
            "UI_BattleHUD", "Assets/UI/UXML/Screens/BattleHUD.uxml", panelSettings, 100);
        var constructionTab = CreatePresenter<ConstructionTabPresenter>(
            "UI_ConstructionTab", "Assets/UI/UXML/Screens/ConstructionTab.uxml", panelSettings, 0);
        var workforceTab = CreatePresenter<WorkforceTabPresenter>(
            "UI_WorkforceTab", "Assets/UI/UXML/Screens/WorkforceTab.uxml", panelSettings, 0);
        var explorationTab = CreatePresenter<ExplorationTabPresenter>(
            "UI_ExplorationTab", "Assets/UI/UXML/Screens/ExplorationTab.uxml", panelSettings, 0);
        var wavePreview = CreatePresenter<WavePreviewPresenter>(
            "UI_WavePreview", "Assets/UI/UXML/Screens/WavePreview.uxml", panelSettings, 200);
        var encounterPopup = CreatePresenter<EncounterPopupPresenter>(
            "UI_EncounterPopup", "Assets/UI/UXML/Screens/EncounterPopup.uxml", panelSettings, 200);

        // Wire presenter refs to director
        SetField(director, "menuPresenter", menuPresenter.GetComponent<MenuScreenPresenter>());
        SetField(director, "battleHUD", battleHUD.GetComponent<BattleHUDPresenter>());
        SetField(director, "constructionTab", constructionTab.GetComponent<ConstructionTabPresenter>());
        SetField(director, "workforceTab", workforceTab.GetComponent<WorkforceTabPresenter>());
        SetField(director, "explorationTab", explorationTab.GetComponent<ExplorationTabPresenter>());
        SetField(director, "wavePreview", wavePreview.GetComponent<WavePreviewPresenter>());
        SetField(director, "encounterPopup", encounterPopup.GetComponent<EncounterPopupPresenter>());

        // Wire sub-refs for presenters
        SetField(battleHUD.GetComponent<BattleHUDPresenter>(), "battleUIBridge", uiBridge);
        SetField(battleHUD.GetComponent<BattleHUDPresenter>(), "timeScaleController", timeScale);
        SetField(battleHUD.GetComponent<BattleHUDPresenter>(), "gameOverManager", gameOver);
        SetField(explorationTab.GetComponent<ExplorationTabPresenter>(), "explorationBridge", explorationBridge);
        SetField(encounterPopup.GetComponent<EncounterPopupPresenter>(), "encounterBridge", encounterBridge);

        // ── 5. Day Tab Controller (UI Toolkit) ──
        var tabPresenter = CreatePresenter<DayTabController>(
            "UI_DayHUD", "Assets/UI/UXML/Screens/DayHUD.uxml", panelSettings, 100);
        var tabCtrl = tabPresenter.GetComponent<DayTabController>();
        SetField(tabCtrl, "director", director);
        SetField(director, "dayTabController", tabCtrl);

        // ── 6. Camera ──
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 40, -30);
            cam.transform.rotation = Quaternion.Euler(55, 0, 0);
            cam.fieldOfView = 50f;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // ── 7. Light ──
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ── Save Scene ──
        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[GameSceneBuilder] Scene saved: {ScenePath}");
        EditorUtility.DisplayDialog("Game Scene Built",
            $"Scene saved to {ScenePath}\n\n" +
            "GameDirector + 6 UI Presenters + Bridge Layer + AudioManager 배치 완료.\n" +
            "Play 버튼을 눌러 메인 메뉴부터 시작하세요.",
            "OK");
    }

    static GameObject CreatePresenter<T>(string name, string uxmlPath, PanelSettings panelSettings, float sortOrder = 0)
        where T : MonoBehaviour
    {
        var go = new GameObject(name);
        var uiDoc = go.AddComponent<UIDocument>();
        go.AddComponent<T>();

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        if (uxml != null) uiDoc.visualTreeAsset = uxml;
        else Debug.LogWarning($"[GameSceneBuilder] UXML not found: {uxmlPath}");

        if (panelSettings != null) uiDoc.panelSettings = panelSettings;
        uiDoc.sortingOrder = sortOrder;

        // Wire UIDocument ref via reflection
        SetField(go.GetComponent<T>(), "uiDocument", uiDoc);

        return go;
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
