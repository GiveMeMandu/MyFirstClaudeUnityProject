using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight.Editor
{
    public static class SceneSetup
    {
        private const string BasePath = "Assets/_Study/12-UIToolkit-Lightweight";
        private const string ScenesPath = BasePath + "/Scenes";
        private const string UxmlPath = BasePath + "/UI/UXML";
        private const string PanelSettingsPath = BasePath + "/UI/DefaultPanelSettings.asset";

        [MenuItem("UIToolkit-Lightweight/Create All Scenes")]
        public static void CreateAllScenes()
        {
            // Ensure Scenes folder exists
            if (!AssetDatabase.IsValidFolder(ScenesPath))
                AssetDatabase.CreateFolder(BasePath, "Scenes");

            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (ps == null)
            {
                Debug.LogError($"PanelSettings not found at {PanelSettingsPath}");
                return;
            }

            CreateScene01(ps);
            CreateScene02(ps);
            CreateScene03(ps);
            CreateScene04(ps);
            CreateScene05(ps);
            CreateScene06(ps);
            CreateScene07(ps);
            CreateScene08(ps);

            Debug.Log("[SceneSetup] All 8 scenes created!");
        }

        private static void CreateScene01(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("ProfileCard");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = LoadUxml("ProfileCard.uxml");
            go.AddComponent<ProfileCardView>().SetDocumentRef(doc);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/01-UIToolkit-Basic.unity");
        }

        private static void CreateScene02(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("ResourcePanel");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = LoadUxml("ResourcePanel.uxml");
            var view = go.AddComponent<ResourcePanelView>();

            var bootstrap = go.AddComponent<ResourceBootstrapper>();
            SetField(view, "_document", doc);
            SetField(bootstrap, "_view", view);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/02-SimpleMVP.unity");
        }

        private static void CreateScene03(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("DialogDemo");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = LoadUxml("ConfirmDialog.uxml");

            var demoView = go.AddComponent<DialogDemoView>();
            var confirmView = go.AddComponent<ConfirmDialogView>();
            var loadingView = go.AddComponent<LoadingScreenView>();
            var bootstrap = go.AddComponent<DialogDemoBootstrapper>();

            SetField(demoView, "_document", doc);
            SetField(confirmView, "_document", doc);
            SetField(loadingView, "_document", doc);
            SetField(bootstrap, "_demoView", demoView);
            SetField(bootstrap, "_confirmDialog", confirmView);
            SetField(bootstrap, "_loadingScreen", loadingView);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/03-AsyncDialog.unity");
        }

        private static void CreateScene04(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("AnimationDemo");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = LoadUxml("AnimationDemo.uxml");

            var panelView = go.AddComponent<AnimatedPanelView>();
            var staggerView = go.AddComponent<StaggerListView>();
            var bootstrap = go.AddComponent<AnimationDemoBootstrapper>();

            SetField(panelView, "_document", doc);
            SetField(staggerView, "_document", doc);
            SetField(bootstrap, "_document", doc);
            SetField(bootstrap, "_panelView", panelView);
            SetField(bootstrap, "_staggerView", staggerView);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/04-AnimationDual.unity");
        }

        private static void CreateScene05(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("BuildingDetail");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = LoadUxml("BuildingDetail.uxml");

            var view = go.AddComponent<BuildingDetailView>();
            var bootstrap = go.AddComponent<BuildingDetailBootstrapper>();

            SetField(view, "_document", doc);
            SetField(bootstrap, "_detailView", view);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/05-AIWorkflow.unity");
        }

        private static void CreateScene06(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // HUD (Sort Order 0)
            var hudGo = new GameObject("ResourceHUD");
            var hudDoc = hudGo.AddComponent<UIDocument>();
            hudDoc.panelSettings = ps;
            hudDoc.visualTreeAsset = LoadUxml("ResourceHUD.uxml");
            hudDoc.sortingOrder = 0;
            var hudView = hudGo.AddComponent<ResourceHudView>();
            SetField(hudView, "_document", hudDoc);

            // Build Menu (Sort Order 10)
            var menuGo = new GameObject("BuildMenu");
            var menuDoc = menuGo.AddComponent<UIDocument>();
            menuDoc.panelSettings = ps;
            menuDoc.visualTreeAsset = LoadUxml("BuildMenu.uxml");
            menuDoc.sortingOrder = 10;
            var menuView = menuGo.AddComponent<BuildMenuView>();
            SetField(menuView, "_document", menuDoc);

            // Tooltip (Sort Order 20)
            var tipGo = new GameObject("Tooltip");
            var tipDoc = tipGo.AddComponent<UIDocument>();
            tipDoc.panelSettings = ps;
            tipDoc.visualTreeAsset = LoadUxml("Tooltip.uxml");
            tipDoc.sortingOrder = 20;
            var tipView = tipGo.AddComponent<TooltipView>();
            SetField(tipView, "_document", tipDoc);

            // Bootstrapper
            var bootGo = new GameObject("GameUIBootstrapper");
            var bootstrap = bootGo.AddComponent<GameUIBootstrapper>();
            SetField(bootstrap, "_hudView", hudView);
            SetField(bootstrap, "_buildMenuView", menuView);
            SetField(bootstrap, "_tooltipView", tipView);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/06-GameUI-HUD-Build.unity");
        }

        private static void CreateScene07(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Inventory
            var invGo = new GameObject("Inventory");
            var invDoc = invGo.AddComponent<UIDocument>();
            invDoc.panelSettings = ps;
            invDoc.visualTreeAsset = LoadUxml("Inventory.uxml");
            var invView = invGo.AddComponent<InventoryView>();
            SetField(invView, "_document", invDoc);

            // Settings
            var setGo = new GameObject("Settings");
            var setDoc = setGo.AddComponent<UIDocument>();
            setDoc.panelSettings = ps;
            setDoc.visualTreeAsset = LoadUxml("Settings.uxml");
            setDoc.sortingOrder = 5;
            var setView = setGo.AddComponent<SettingsView>();
            SetField(setView, "_document", setDoc);

            // Bootstrapper
            var bootGo = new GameObject("InventorySettingsBootstrapper");
            var bootstrap = bootGo.AddComponent<InventorySettingsBootstrapper>();
            SetField(bootstrap, "_inventoryView", invView);
            SetField(bootstrap, "_settingsView", setView);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/07-Inventory-Settings.unity");
        }

        private static void CreateScene08(PanelSettings ps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("ComplexityDemo");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = LoadUxml("SearchPanel.uxml");

            var view = go.AddComponent<SearchPanelView>();
            var bootstrap = go.AddComponent<ComplexityDemoBootstrapper>();

            SetField(view, "_document", doc);
            SetField(bootstrap, "_view", view);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/08-ComplexityDecision.unity");
        }

        private static VisualTreeAsset LoadUxml(string filename)
        {
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UxmlPath}/{filename}");
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogWarning($"Field {fieldName} not found on {target.GetType().Name}");
        }
    }

    // Extension to set UIDocument ref on ProfileCardView (it has its own _document field)
    public static class ProfileCardViewEditorExt
    {
        public static void SetDocumentRef(this ProfileCardView view, UIDocument doc)
        {
            var field = typeof(ProfileCardView).GetField("_document",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(view, doc);
        }
    }
}
