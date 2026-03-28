#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Page;
using UIStudy.Assets.LifetimeScopes;
using UIStudy.Assets.Views;
using UIStudy.Navigation.LifetimeScopes;
using UIStudy.Navigation.Pages;
using UIStudy.Theme.LifetimeScopes;
using UIStudy.Theme.Views;

namespace UIStudy.Editor
{
    public static class StudySceneBuilders
    {
        // ================================================================
        //  Menu Items
        // ================================================================

        [MenuItem("UI Study/Build All Module Scenes")]
        public static void BuildAll()
        {
            Build04ThemeScene();
            Build03AssetDemoScene();
            Build03AtlasGalleryScene();
            Build02NavigationScene();
            Debug.Log("[StudySceneBuilders] All scenes built.");
        }

        [MenuItem("UI Study/Build 04 - Theme Scene")]
        public static void Build04ThemeScene()
        {
            var scenePath = "Assets/_Study/04-Theme-Accessibility/Scenes/01-ThemeDemo.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var titleTMP = AddTMPText(canvas.transform, "Title", "Theme & Accessibility Demo", 32f,
                TextAlignmentOptions.Center);
            var titleRT = Anchor(titleTMP.rectTransform, AnchorPreset.TopStretch, new Vector2(0, -10),
                new Vector2(0, -60));

            // === Controls Panel (중앙 상단) ===
            var controls = AddPanel(canvas.transform, "Controls", new Color(0.15f, 0.15f, 0.15f, 0.6f));
            var controlsRT = Anchor(controls.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(20, -70), new Vector2(-20, -250));
            var vlg = controls.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(20, 20, 15, 15);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Theme row
            var themeRow = AddHorizontalRow(controls.transform, "ThemeRow", 40f);
            var cycleBtn = AddButton(themeRow.transform, "CycleThemeButton", "Cycle Theme",
                new Vector2(180, 36));
            var themeLabel = AddTMPText(themeRow.transform, "ThemeLabel", "Light", 22f,
                TextAlignmentOptions.MidlineLeft);
            var themeLabelLE = themeLabel.gameObject.AddComponent<LayoutElement>();
            themeLabelLE.flexibleWidth = 1;

            // Font Scale row
            var fontRow = AddHorizontalRow(controls.transform, "FontScaleRow", 40f);
            var fontRowLabel = AddTMPText(fontRow.transform, "Label", "Font Scale", 18f,
                TextAlignmentOptions.MidlineLeft);
            var fontRowLabelLE = fontRowLabel.gameObject.AddComponent<LayoutElement>();
            fontRowLabelLE.preferredWidth = 100;
            var slider = AddSlider(fontRow.transform, "FontScaleSlider", 0.75f, 2f, 1f);
            var sliderLE = slider.gameObject.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth = 1;
            sliderLE.preferredHeight = 30;
            var fontScaleLabel = AddTMPText(fontRow.transform, "FontScaleLabel", "1.0x", 18f,
                TextAlignmentOptions.MidlineRight);
            var fontScaleLabelLE = fontScaleLabel.gameObject.AddComponent<LayoutElement>();
            fontScaleLabelLE.preferredWidth = 60;

            // Color Blind row
            var cbRow = AddHorizontalRow(controls.transform, "ColorBlindRow", 40f);
            var cbToggle = AddButton(cbRow.transform, "ColorBlindToggle", "Color Blind Mode",
                new Vector2(220, 36));
            var cbLabel = AddTMPText(cbRow.transform, "ColorBlindLabel", "OFF", 22f,
                TextAlignmentOptions.MidlineLeft);
            var cbLabelLE = cbLabel.gameObject.AddComponent<LayoutElement>();
            cbLabelLE.flexibleWidth = 1;

            // === Preview Panel ===
            var previewBG = AddPanel(canvas.transform, "PreviewPanel", new Color(0.95f, 0.95f, 0.92f));
            var previewRT = Anchor(previewBG.GetComponent<RectTransform>(), AnchorPreset.StretchAll,
                new Vector2(20, -260), new Vector2(-20, -20));
            var previewVLG = previewBG.AddComponent<VerticalLayoutGroup>();
            previewVLG.spacing = 10f;
            previewVLG.padding = new RectOffset(30, 30, 25, 25);
            previewVLG.childAlignment = TextAnchor.UpperLeft;
            previewVLG.childControlWidth = true;
            previewVLG.childControlHeight = false;
            previewVLG.childForceExpandWidth = true;
            previewVLG.childForceExpandHeight = false;

            var previewTitle = AddTMPText(previewBG.transform, "PreviewTitle", "Preview Title", 28f,
                TextAlignmentOptions.TopLeft, Color.black);
            var previewTitleLE = previewTitle.gameObject.AddComponent<LayoutElement>();
            previewTitleLE.preferredHeight = 40;
            var previewBody = AddTMPText(previewBG.transform, "PreviewBody",
                "This is sample body text to demonstrate theme color changes.\nFont scaling will affect all text elements.",
                18f, TextAlignmentOptions.TopLeft, new Color(0.3f, 0.3f, 0.3f));
            var previewBodyLE = previewBody.gameObject.AddComponent<LayoutElement>();
            previewBodyLE.preferredHeight = 80;
            var accentBar = AddImage(previewBG.transform, "AccentBar", new Color(0.2f, 0.5f, 0.9f),
                new Vector2(0, 6));
            var accentLE = accentBar.gameObject.AddComponent<LayoutElement>();
            accentLE.preferredHeight = 6;

            // === ThemeDemoView component ===
            var viewGO = new GameObject("ThemeDemoView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<ThemeDemoView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_cycleThemeButton").objectReferenceValue = cycleBtn.GetComponent<Button>();
            viewSO.FindProperty("_themeLabel").objectReferenceValue = themeLabel;
            viewSO.FindProperty("_fontScaleSlider").objectReferenceValue = slider;
            viewSO.FindProperty("_fontScaleLabel").objectReferenceValue = fontScaleLabel;
            viewSO.FindProperty("_colorBlindToggle").objectReferenceValue = cbToggle.GetComponent<Button>();
            viewSO.FindProperty("_colorBlindLabel").objectReferenceValue = cbLabel;
            viewSO.FindProperty("_backgroundPanel").objectReferenceValue = previewBG.GetComponent<Image>();
            viewSO.FindProperty("_previewTitle").objectReferenceValue = previewTitle;
            viewSO.FindProperty("_previewBody").objectReferenceValue = previewBody;
            viewSO.FindProperty("_accentBar").objectReferenceValue = accentBar;

            // _allTexts array
            var allTexts = new Object[]
            {
                titleTMP, themeLabel, fontRowLabel, fontScaleLabel, cbLabel, previewTitle, previewBody
            };
            var allTextsProp = viewSO.FindProperty("_allTexts");
            allTextsProp.arraySize = allTexts.Length;
            for (int i = 0; i < allTexts.Length; i++)
                allTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = allTexts[i];
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("ThemeDemoLifetimeScope");
            var scope = scopeGO.AddComponent<ThemeDemoLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_themeDemoView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[04] Saved: {scenePath}");
        }

        [MenuItem("UI Study/Build 03 - Asset Demo Scene")]
        public static void Build03AssetDemoScene()
        {
            var scenePath = "Assets/_Study/03-Addressables-Assets/Scenes/01-AssetDemo.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "Addressables - Asset Demo", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch, new Vector2(0, -10), new Vector2(0, -50));

            // === Display Area ===
            var displayImage = AddImage(canvas.transform, "DisplayImage", new Color(0.2f, 0.2f, 0.2f, 0.5f),
                new Vector2(256, 256));
            var displayRT = displayImage.rectTransform;
            displayRT.anchorMin = new Vector2(0.5f, 0.5f);
            displayRT.anchorMax = new Vector2(0.5f, 0.5f);
            displayRT.anchoredPosition = new Vector2(0, 40);
            displayImage.preserveAspect = true;

            // === Status ===
            var statusText = AddTMPText(canvas.transform, "StatusText", "Ready", 20f,
                TextAlignmentOptions.Center);
            var statusRT = Anchor(statusText.rectTransform, AnchorPreset.BottomStretch,
                new Vector2(20, 80), new Vector2(-20, 120));

            // === Buttons ===
            var btnRow = AddHorizontalRow(canvas.transform, "ButtonRow", 50f);
            var btnRowRT = Anchor(btnRow.GetComponent<RectTransform>(), AnchorPreset.BottomStretch,
                new Vector2(40, 20), new Vector2(-40, 70));
            btnRow.GetComponent<HorizontalLayoutGroup>().spacing = 20;
            var loadBtn = AddButton(btnRow.transform, "LoadButton", "Load", new Vector2(0, 44));
            loadBtn.AddComponent<LayoutElement>().flexibleWidth = 1;
            var releaseBtn = AddButton(btnRow.transform, "ReleaseButton", "Release", new Vector2(0, 44));
            releaseBtn.AddComponent<LayoutElement>().flexibleWidth = 1;

            // === AssetDemoView ===
            var viewGO = new GameObject("AssetDemoView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<AssetDemoView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_displayImage").objectReferenceValue = displayImage;
            viewSO.FindProperty("_loadButton").objectReferenceValue = loadBtn.GetComponent<Button>();
            viewSO.FindProperty("_releaseButton").objectReferenceValue = releaseBtn.GetComponent<Button>();
            viewSO.FindProperty("_statusText").objectReferenceValue = statusText;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("AssetDemoLifetimeScope");
            var scope = scopeGO.AddComponent<AssetDemoLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_assetDemoView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[03a] Saved: {scenePath}");
        }

        [MenuItem("UI Study/Build 03 - Atlas Gallery Scene")]
        public static void Build03AtlasGalleryScene()
        {
            var scenePath = "Assets/_Study/03-Addressables-Assets/Scenes/02-AtlasGallery.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "SpriteAtlas Gallery", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch, new Vector2(0, -10), new Vector2(0, -50));

            // === Display Image ===
            var displayImage = AddImage(canvas.transform, "DisplayImage",
                new Color(0.2f, 0.2f, 0.2f, 0.5f), new Vector2(256, 256));
            var displayRT = displayImage.rectTransform;
            displayRT.anchorMin = new Vector2(0.5f, 0.5f);
            displayRT.anchorMax = new Vector2(0.5f, 0.5f);
            displayRT.anchoredPosition = new Vector2(0, 40);
            displayImage.preserveAspect = true;

            // === Name + Index ===
            var nameText = AddTMPText(canvas.transform, "NameText", "(sprite name)", 22f,
                TextAlignmentOptions.Center);
            Anchor(nameText.rectTransform, AnchorPreset.BottomStretch, new Vector2(20, 110), new Vector2(-20, 140));
            var indexText = AddTMPText(canvas.transform, "IndexText", "0 / 0", 18f,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            Anchor(indexText.rectTransform, AnchorPreset.BottomStretch, new Vector2(20, 80), new Vector2(-20, 108));

            // === Buttons ===
            var btnRow = AddHorizontalRow(canvas.transform, "ButtonRow", 50f);
            Anchor(btnRow.GetComponent<RectTransform>(), AnchorPreset.BottomStretch,
                new Vector2(40, 20), new Vector2(-40, 70));
            btnRow.GetComponent<HorizontalLayoutGroup>().spacing = 20;
            var prevBtn = AddButton(btnRow.transform, "PrevButton", "< Prev", new Vector2(0, 44));
            prevBtn.AddComponent<LayoutElement>().flexibleWidth = 1;
            var nextBtn = AddButton(btnRow.transform, "NextButton", "Next >", new Vector2(0, 44));
            nextBtn.AddComponent<LayoutElement>().flexibleWidth = 1;

            // === AtlasGalleryView ===
            var viewGO = new GameObject("AtlasGalleryView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<AtlasGalleryView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_displayImage").objectReferenceValue = displayImage;
            viewSO.FindProperty("_nextButton").objectReferenceValue = nextBtn.GetComponent<Button>();
            viewSO.FindProperty("_prevButton").objectReferenceValue = prevBtn.GetComponent<Button>();
            viewSO.FindProperty("_nameText").objectReferenceValue = nameText;
            viewSO.FindProperty("_indexText").objectReferenceValue = indexText;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("AtlasGalleryLifetimeScope");
            var scope = scopeGO.AddComponent<AtlasGalleryLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_atlasGalleryView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[03b] Saved: {scenePath}");
        }

        [MenuItem("UI Study/Build 02 - Navigation Scene")]
        public static void Build02NavigationScene()
        {
            var basePath = "Assets/_Study/02-Screen-Navigation";
            var scenePath = $"{basePath}/Scenes/01-Navigation.unity";
            var resPath = $"{basePath}/Resources";

            // 1. Create Page prefabs in Resources/
            CreatePagePrefab<MainPageView>($"{resPath}/MainPage.prefab",
                "Main Page", ("_goToDetailButton", "Go to Detail"), ("_titleText", null));
            CreatePagePrefab<DetailPageView>($"{resPath}/DetailPage.prefab",
                "Detail Page",
                ("_goToSettingsButton", "Go to Settings"), ("_backButton", "Back"),
                ("_titleText", null), ("_contentText", null));
            CreatePagePrefab<SettingsPageView>($"{resPath}/SettingsPage.prefab",
                "Settings",
                ("_backButton", "Back"), ("_titleText", null));

            // 2. Build main scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // PageContainer fills the entire Canvas
            // RectMask2D must be added BEFORE PageContainer ([RequireComponent])
            var pageContainerGO = new GameObject("PageContainer",
                typeof(RectTransform), typeof(CanvasGroup), typeof(RectMask2D));
            pageContainerGO.transform.SetParent(canvas.transform, false);
            StretchFill(pageContainerGO.GetComponent<RectTransform>());
            var pageContainer = pageContainerGO.AddComponent<PageContainer>();

            // LifetimeScope
            var scopeGO = new GameObject("NavigationLifetimeScope");
            var scope = scopeGO.AddComponent<NavigationLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_pageContainer").objectReferenceValue = pageContainer;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[02] Saved: {scenePath}");
        }

        // ================================================================
        //  Page Prefab Creator — Module 02
        // ================================================================

        private static void CreatePagePrefab<T>(string prefabPath, string defaultTitle,
            params (string fieldName, string buttonLabel)[] fields)
            where T : Page
        {
            EnsureDir(prefabPath);

            // Root — stretch fill + CanvasGroup for USN transitions
            var root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath),
                typeof(RectTransform), typeof(CanvasGroup));
            StretchFill(root.GetComponent<RectTransform>());

            // Background
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            StretchFill(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 1f);

            // Content panel (centered vertical layout)
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(root.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0.1f, 0.1f);
            contentRT.anchorMax = new Vector2(0.9f, 0.9f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;
            var contentVLG = content.GetComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 20f;
            contentVLG.padding = new RectOffset(20, 20, 40, 20);
            contentVLG.childAlignment = TextAnchor.UpperCenter;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;

            // View component on root
            var view = root.AddComponent<T>();
            var so = new SerializedObject(view);

            foreach (var (fieldName, btnLabel) in fields)
            {
                var prop = so.FindProperty(fieldName);
                if (prop == null) continue;

                if (fieldName.Contains("Button") || fieldName.Contains("button"))
                {
                    // Create button
                    var btn = CreateUIButton(content.transform, fieldName, btnLabel ?? "Button",
                        new Vector2(0, 44));
                    btn.AddComponent<LayoutElement>().preferredHeight = 44;
                    prop.objectReferenceValue = btn.GetComponent<Button>();
                }
                else if (fieldName.Contains("Text") || fieldName.Contains("title") ||
                         fieldName.Contains("content") || fieldName.Contains("Title") ||
                         fieldName.Contains("Content"))
                {
                    // Create TMP text
                    float fontSize = fieldName.Contains("itle") ? 32f : 20f;
                    var tmp = CreateUITMPText(content.transform, fieldName,
                        fieldName.Contains("itle") ? defaultTitle : "...",
                        fontSize, TextAlignmentOptions.Center);
                    tmp.gameObject.AddComponent<LayoutElement>().preferredHeight =
                        fieldName.Contains("itle") ? 50 : 60;
                    prop.objectReferenceValue = tmp;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[02 prefab] Saved: {prefabPath}");
        }

        // ================================================================
        //  UI Helpers
        // ================================================================

        internal static void AddEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        internal static GameObject AddCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            return go;
        }

        internal static TextMeshProUGUI AddTMPText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        internal static GameObject AddButton(Transform parent, string name, string label, Vector2 size)
        {
            var go = CreateUIButton(parent, name, label, size);
            return go;
        }

        internal static GameObject CreateUIButton(Transform parent, string name, string label, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            StretchFill(textGO.GetComponent<RectTransform>());
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            // Button target graphic
            go.GetComponent<Button>().targetGraphic = img;

            return go;
        }

        internal static Image AddImage(Transform parent, string name, Color color, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        internal static Slider AddSlider(Transform parent, string name, float min, float max, float value)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            // Style the fill area
            var fillImage = go.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = new Color(0.3f, 0.6f, 0.9f);

            return slider;
        }

        internal static GameObject AddPanel(Transform parent, string name, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bgColor;
            go.GetComponent<Image>().raycastTarget = false;
            return go;
        }

        internal static GameObject AddHorizontalRow(Transform parent, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return go;
        }

        internal static TextMeshProUGUI CreateUITMPText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment)
        {
            return AddTMPText(parent, name, text, fontSize, alignment, Color.white);
        }

        // ================================================================
        //  RectTransform Helpers
        // ================================================================

        internal enum AnchorPreset
        {
            TopStretch,
            BottomStretch,
            StretchAll
        }

        internal static RectTransform Anchor(RectTransform rt, AnchorPreset preset,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            switch (preset)
            {
                case AnchorPreset.TopStretch:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    // offsetMin.y = bottom edge from top anchor, offsetMax.y = top edge from top anchor
                    rt.offsetMin = new Vector2(offsetMin.x, 0);
                    rt.offsetMax = new Vector2(offsetMax.x, 0);
                    rt.anchoredPosition = new Vector2(
                        (offsetMin.x + offsetMax.x) * 0.5f,
                        (offsetMin.y + offsetMax.y) * 0.5f);
                    rt.sizeDelta = new Vector2(
                        -(offsetMin.x - offsetMax.x),
                        offsetMin.y - offsetMax.y);
                    break;
                case AnchorPreset.BottomStretch:
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.anchoredPosition = new Vector2(
                        (offsetMin.x + offsetMax.x) * 0.5f,
                        (offsetMin.y + offsetMax.y) * 0.5f);
                    rt.sizeDelta = new Vector2(
                        -(offsetMin.x - offsetMax.x),
                        offsetMax.y - offsetMin.y);
                    break;
                case AnchorPreset.StretchAll:
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(offsetMin.x, -offsetMax.y);
                    rt.offsetMax = new Vector2(offsetMax.x, offsetMin.y);
                    break;
            }

            return rt;
        }

        internal static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void EnsureDir(string assetPath)
        {
            var fullDir = Path.GetDirectoryName(Path.Combine(Application.dataPath, "..",  assetPath));
            if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);
        }
    }
}
#endif
