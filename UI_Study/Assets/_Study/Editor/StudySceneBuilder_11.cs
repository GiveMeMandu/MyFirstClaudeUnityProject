#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UIStudy.InGameUI.LifetimeScopes;
using UIStudy.InGameUI.Views;
using static UIStudy.Editor.StudySceneBuilders;

namespace UIStudy.Editor
{
    public static class StudySceneBuilder_11
    {
        [MenuItem("UI Study/Build 11 - InGame WorldUI Scene")]
        public static void Build11_QuarterViewBuildings()
        {
            var scenePath = "Assets/_Study/11-InGame-WorldUI/Scenes/01-QuarterViewBuildings.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ─── EventSystem ───
            AddEventSystem();

            // ─── Directional Light ───
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // ─── Quarter-View Camera ───
            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.18f, 0.22f);
            camGO.transform.position = new Vector3(10, 12, -10);
            camGO.transform.rotation = Quaternion.Euler(35, -45, 0);

            // ─── Ground Plane ───
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2, 1, 2); // 20x20 units
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.25f, 0.35f, 0.2f); // grass green
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // ─── Buildings (6 cubes) ───
            var buildingConfigs = new[]
            {
                ("Goldmine",   new Vector3(-3, 0.75f, 2),    new Color(0.9f, 0.75f, 0.2f), new Vector3(1.5f, 1.5f, 1.5f)),
                ("Lumber Mill", new Vector3(2, 0.6f, 4),     new Color(0.55f, 0.35f, 0.15f), new Vector3(1.8f, 1.2f, 1.2f)),
                ("Quarry",     new Vector3(5, 0.5f, -1),     new Color(0.5f, 0.5f, 0.55f), new Vector3(1.4f, 1.0f, 1.6f)),
                ("Farm",       new Vector3(-1, 0.4f, -3),    new Color(0.3f, 0.65f, 0.2f), new Vector3(2.0f, 0.8f, 1.5f)),
                ("Treasury",   new Vector3(1, 0.9f, 1),      new Color(0.85f, 0.6f, 0.1f), new Vector3(1.2f, 1.8f, 1.2f)),
                ("Warehouse",  new Vector3(-4, 0.6f, -1.5f), new Color(0.4f, 0.3f, 0.25f), new Vector3(1.6f, 1.2f, 1.8f)),
            };

            var buildingViews = new BuildingView[buildingConfigs.Length];
            for (int i = 0; i < buildingConfigs.Length; i++)
            {
                var (bName, pos, color, scale) = buildingConfigs[i];
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = bName;
                cube.transform.position = pos;
                cube.transform.localScale = scale;

                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                cube.GetComponent<MeshRenderer>().sharedMaterial = mat;

                // BoxCollider is auto-added by CreatePrimitive
                var bv = cube.AddComponent<BuildingView>();
                buildingViews[i] = bv;
            }

            // ─── Screen Space Overlay Canvas (for UI) ───
            var canvas = AddCanvas("UICanvas");

            // Title
            var title = AddTMPText(canvas.transform, "Title", "Quarter-View Building Demo", 24f,
                TextAlignmentOptions.Center);
            var titleRT = title.rectTransform;
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.anchoredPosition = new Vector2(0, -20);
            titleRT.sizeDelta = new Vector2(0, 40);

            // Instruction
            var instruction = AddTMPText(canvas.transform, "Instruction",
                "Click a building to collect resources", 16f, TextAlignmentOptions.Center,
                new Color(0.7f, 0.7f, 0.7f));
            var instrRT = instruction.rectTransform;
            instrRT.anchorMin = new Vector2(0, 1);
            instrRT.anchorMax = new Vector2(1, 1);
            instrRT.anchoredPosition = new Vector2(0, -50);
            instrRT.sizeDelta = new Vector2(0, 30);

            // Resource counters (bottom-left)
            var resourcePanel = new GameObject("ResourcePanel", typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            resourcePanel.transform.SetParent(canvas.transform, false);
            var rpRT = resourcePanel.GetComponent<RectTransform>();
            rpRT.anchorMin = new Vector2(0, 0);
            rpRT.anchorMax = new Vector2(0, 0);
            rpRT.pivot = new Vector2(0, 0);
            rpRT.anchoredPosition = new Vector2(15, 15);
            rpRT.sizeDelta = new Vector2(200, 120);
            var rpVLG = resourcePanel.GetComponent<VerticalLayoutGroup>();
            rpVLG.spacing = 4;
            rpVLG.childAlignment = TextAnchor.LowerLeft;
            rpVLG.childControlWidth = true;
            rpVLG.childControlHeight = false;
            rpVLG.childForceExpandWidth = true;
            rpVLG.childForceExpandHeight = false;

            // Add a semi-transparent background
            var rpBG = resourcePanel.AddComponent<Image>();
            rpBG.color = new Color(0, 0, 0, 0.5f);
            rpBG.raycastTarget = false;

            var goldText = AddTMPText(resourcePanel.transform, "GoldText", "Gold: 0", 18f,
                TextAlignmentOptions.MidlineLeft, new Color(1f, 0.85f, 0.2f));
            goldText.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;
            var woodText = AddTMPText(resourcePanel.transform, "WoodText", "Wood: 0", 18f,
                TextAlignmentOptions.MidlineLeft, new Color(0.6f, 0.4f, 0.2f));
            woodText.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;
            var stoneText = AddTMPText(resourcePanel.transform, "StoneText", "Stone: 0", 18f,
                TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.65f, 0.7f));
            stoneText.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;
            var foodText = AddTMPText(resourcePanel.transform, "FoodText", "Food: 0", 18f,
                TextAlignmentOptions.MidlineLeft, new Color(0.3f, 0.8f, 0.2f));
            foodText.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;

            // Floating container (stretches full canvas for positioning)
            var floatingContainer = new GameObject("FloatingContainer", typeof(RectTransform));
            floatingContainer.transform.SetParent(canvas.transform, false);
            StretchFill(floatingContainer.GetComponent<RectTransform>());

            // Floating resource prefab template (hidden in scene)
            var prefabGO = new GameObject("FloatingResourceTemplate",
                typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
            prefabGO.transform.SetParent(floatingContainer.transform, false);
            var prefabRT = prefabGO.GetComponent<RectTransform>();
            prefabRT.sizeDelta = new Vector2(200, 40);
            var prefabTMP = prefabGO.GetComponent<TextMeshProUGUI>();
            prefabTMP.fontSize = 22;
            prefabTMP.alignment = TextAlignmentOptions.Center;
            prefabTMP.fontStyle = FontStyles.Bold;
            prefabTMP.raycastTarget = false;
            var prefabView = prefabGO.AddComponent<FloatingResourceView>();
            var prefabViewSO = new SerializedObject(prefabView);
            prefabViewSO.FindProperty("_text").objectReferenceValue = prefabTMP;
            prefabViewSO.ApplyModifiedPropertiesWithoutUndo();
            prefabGO.SetActive(false);

            // ─── InGameUIDemoView ───
            var demoViewGO = new GameObject("InGameUIDemoView");
            demoViewGO.transform.SetParent(canvas.transform, false);
            var demoView = demoViewGO.AddComponent<InGameUIDemoView>();
            var demoViewSO = new SerializedObject(demoView);
            demoViewSO.FindProperty("_titleText").objectReferenceValue = title;
            demoViewSO.FindProperty("_instructionText").objectReferenceValue = instruction;
            demoViewSO.FindProperty("_goldText").objectReferenceValue = goldText;
            demoViewSO.FindProperty("_woodText").objectReferenceValue = woodText;
            demoViewSO.FindProperty("_stoneText").objectReferenceValue = stoneText;
            demoViewSO.FindProperty("_foodText").objectReferenceValue = foodText;
            demoViewSO.FindProperty("_floatingContainer").objectReferenceValue =
                floatingContainer.GetComponent<RectTransform>();
            demoViewSO.ApplyModifiedPropertiesWithoutUndo();

            // ─── LifetimeScope ───
            var scopeGO = new GameObject("InGameUILifetimeScope");
            var scope = scopeGO.AddComponent<InGameUILifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_demoView").objectReferenceValue = demoView;
            scopeSO.FindProperty("_floatingPrefab").objectReferenceValue = prefabView;
            scopeSO.FindProperty("_floatingContainer").objectReferenceValue =
                floatingContainer.GetComponent<RectTransform>();
            scopeSO.FindProperty("_mainCamera").objectReferenceValue = cam;

            // Buildings array
            var buildingsProp = scopeSO.FindProperty("_buildings");
            buildingsProp.arraySize = buildingViews.Length;
            for (int i = 0; i < buildingViews.Length; i++)
                buildingsProp.GetArrayElementAtIndex(i).objectReferenceValue = buildingViews[i];

            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[11-01] Saved: {scenePath}");
        }
    }
}
#endif
