#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UIStudy.R3Advanced.Views;
using UIStudy.R3Advanced.LifetimeScopes;
using UIStudy.Animation.Views;
using UIStudy.Animation.LifetimeScopes;
using UIStudy.GameUI.Views;
using UIStudy.GameUI.LifetimeScopes;
using static UIStudy.Editor.StudySceneBuilders;

namespace UIStudy.Editor
{
    /// <summary>
    /// Scene builders for modules 06 (R3 Advanced Binding), 07 (Animation Patterns),
    /// and 08 (Game UI Patterns).
    /// </summary>
    public static class StudySceneBuilders_06_08
    {
        // ================================================================
        //  Menu Items
        // ================================================================

        [MenuItem("UI Study/Build 06 - R3 Advanced Scenes")]
        public static void Build06All()
        {
            Build06_01_ReactiveCommand();
            Build06_02_DebounceSearch();
            Build06_03_TwoWayBinding();
            Debug.Log("[StudySceneBuilders] Module 06 — all scenes built.");
        }

        [MenuItem("UI Study/Build 07 - Animation Scenes")]
        public static void Build07All()
        {
            Build07_01_ButtonEffects();
            Build07_02_StaggerList();
            Build07_03_PanelTransition();
            Debug.Log("[StudySceneBuilders] Module 07 — all scenes built.");
        }

        [MenuItem("UI Study/Build 08 - Game UI Scenes")]
        public static void Build08All()
        {
            Build08_01_DamageNumbers();
            Build08_02_DialogTypewriter();
            Build08_03_InventoryGrid();
            Debug.Log("[StudySceneBuilders] Module 08 — all scenes built.");
        }

        [MenuItem("UI Study/Build 06-08 All New Scenes")]
        public static void BuildAll_06_08()
        {
            Build06All();
            Build07All();
            Build08All();
            Debug.Log("[StudySceneBuilders] Modules 06-08 — ALL scenes built.");
        }

        // ================================================================
        //  Module 06: R3-Advanced-Binding
        // ================================================================

        public static void Build06_01_ReactiveCommand()
        {
            var scenePath = "Assets/_Study/06-R3-Advanced-Binding/Scenes/01-ReactiveCommand.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "06-01 ReactiveCommand", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Gold / Price display (middle) ===
            var infoPanel = AddPanel(canvas.transform, "InfoPanel", new Color(0.15f, 0.15f, 0.15f, 0.6f));
            var infoPanelRT = Anchor(infoPanel.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(20, -60), new Vector2(-20, -180));
            var infoVLG = infoPanel.AddComponent<VerticalLayoutGroup>();
            infoVLG.spacing = 10f;
            infoVLG.padding = new RectOffset(20, 20, 15, 15);
            infoVLG.childAlignment = TextAnchor.UpperCenter;
            infoVLG.childControlWidth = true;
            infoVLG.childControlHeight = true;
            infoVLG.childForceExpandWidth = true;
            infoVLG.childForceExpandHeight = false;

            var goldText = AddTMPText(infoPanel.transform, "GoldText", "Gold: 100", 24f,
                TextAlignmentOptions.Center, new Color(1f, 0.85f, 0f));
            goldText.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;

            var priceText = AddTMPText(infoPanel.transform, "PriceText", "Price: 30", 22f,
                TextAlignmentOptions.Center);
            priceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            // === Status text ===
            var statusText = AddTMPText(canvas.transform, "StatusText", "Ready", 20f,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            Anchor(statusText.rectTransform, AnchorPreset.BottomStretch,
                new Vector2(20, 80), new Vector2(-20, 110));

            // === Buttons (bottom) ===
            var btnRow = AddHorizontalRow(canvas.transform, "ButtonRow", 50f);
            Anchor(btnRow.GetComponent<RectTransform>(), AnchorPreset.BottomStretch,
                new Vector2(40, 20), new Vector2(-40, 70));
            btnRow.GetComponent<HorizontalLayoutGroup>().spacing = 20;

            var buyButton = AddButton(btnRow.transform, "BuyButton", "Buy", new Vector2(0, 44));
            buyButton.AddComponent<LayoutElement>().flexibleWidth = 1;
            var addGoldButton = AddButton(btnRow.transform, "AddGoldButton", "+10 Gold", new Vector2(0, 44));
            addGoldButton.AddComponent<LayoutElement>().flexibleWidth = 1;

            // === PurchaseView ===
            var viewGO = new GameObject("PurchaseView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<PurchaseView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_buyButton").objectReferenceValue = buyButton.GetComponent<Button>();
            viewSO.FindProperty("_addGoldButton").objectReferenceValue = addGoldButton.GetComponent<Button>();
            viewSO.FindProperty("_goldText").objectReferenceValue = goldText;
            viewSO.FindProperty("_priceText").objectReferenceValue = priceText;
            viewSO.FindProperty("_statusText").objectReferenceValue = statusText;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("ReactiveCommandLifetimeScope");
            var scope = scopeGO.AddComponent<ReactiveCommandLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_view").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[06-01] Saved: {scenePath}");
        }

        public static void Build06_02_DebounceSearch()
        {
            var scenePath = "Assets/_Study/06-R3-Advanced-Binding/Scenes/02-DebounceSearch.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "06-02 Debounce Search", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Search InputField ===
            var inputFieldGO = CreateTMPInputField(canvas.transform, "SearchField", "Type to search...");
            var inputFieldRT = inputFieldGO.GetComponent<RectTransform>();
            inputFieldRT.anchorMin = new Vector2(0, 1);
            inputFieldRT.anchorMax = new Vector2(1, 1);
            inputFieldRT.anchoredPosition = new Vector2(0, -75);
            inputFieldRT.sizeDelta = new Vector2(-40, 40);

            // === Status + Count row ===
            var statusRow = AddHorizontalRow(canvas.transform, "StatusRow", 30f);
            var statusRowRT = Anchor(statusRow.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(20, -100), new Vector2(-20, -135));
            statusRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;

            var statusText = AddTMPText(statusRow.transform, "StatusText", "Idle", 18f,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.7f));
            statusText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var resultCountText = AddTMPText(statusRow.transform, "ResultCountText", "0 results", 18f,
                TextAlignmentOptions.MidlineRight);
            resultCountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;

            // === 8 result text rows ===
            var resultsPanel = AddPanel(canvas.transform, "ResultsPanel", new Color(0.12f, 0.12f, 0.18f, 0.5f));
            var resultsPanelRT = Anchor(resultsPanel.GetComponent<RectTransform>(), AnchorPreset.StretchAll,
                new Vector2(20, -145), new Vector2(-20, -20));
            var resultsVLG = resultsPanel.AddComponent<VerticalLayoutGroup>();
            resultsVLG.spacing = 4f;
            resultsVLG.padding = new RectOffset(15, 15, 10, 10);
            resultsVLG.childAlignment = TextAnchor.UpperLeft;
            resultsVLG.childControlWidth = true;
            resultsVLG.childControlHeight = true;
            resultsVLG.childForceExpandWidth = true;
            resultsVLG.childForceExpandHeight = false;

            var resultSlots = new TextMeshProUGUI[8];
            for (int i = 0; i < 8; i++)
            {
                var slot = AddTMPText(resultsPanel.transform, $"ResultSlot_{i}", $"Result {i + 1}", 18f,
                    TextAlignmentOptions.MidlineLeft);
                slot.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
                slot.gameObject.SetActive(false);
                resultSlots[i] = slot;
            }

            // === SearchView ===
            var viewGO = new GameObject("SearchView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<SearchView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_searchField").objectReferenceValue = inputFieldGO.GetComponent<TMP_InputField>();
            viewSO.FindProperty("_resultCountText").objectReferenceValue = resultCountText;
            viewSO.FindProperty("_statusText").objectReferenceValue = statusText;

            var slotsProp = viewSO.FindProperty("_resultSlots");
            slotsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = resultSlots[i];
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("DebounceSearchLifetimeScope");
            var scope = scopeGO.AddComponent<DebounceSearchLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_view").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[06-02] Saved: {scenePath}");
        }

        public static void Build06_03_TwoWayBinding()
        {
            var scenePath = "Assets/_Study/06-R3-Advanced-Binding/Scenes/03-TwoWayBinding.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "06-03 Two-Way Binding", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Content panel ===
            var content = AddPanel(canvas.transform, "Content", new Color(0.15f, 0.15f, 0.15f, 0.6f));
            var contentRT = Anchor(content.GetComponent<RectTransform>(), AnchorPreset.StretchAll,
                new Vector2(20, -60), new Vector2(-20, -20));
            var contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 12f;
            contentVLG.padding = new RectOffset(20, 20, 15, 15);
            contentVLG.childAlignment = TextAnchor.UpperLeft;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = true;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;

            // Name input
            var nameLabel = AddTMPText(content.transform, "NameLabel", "Character Name:", 18f,
                TextAlignmentOptions.MidlineLeft);
            nameLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;

            var nameInput = CreateTMPInputField(content.transform, "NameInput", "Enter name...");
            nameInput.AddComponent<LayoutElement>().preferredHeight = 40;

            // Health slider + label
            var healthRow = AddHorizontalRow(content.transform, "HealthRow", 40f);
            healthRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;
            var healthLabelPrefix = AddTMPText(healthRow.transform, "HealthPrefix", "Health:", 18f,
                TextAlignmentOptions.MidlineLeft);
            healthLabelPrefix.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;
            var healthSlider = AddSlider(healthRow.transform, "HealthSlider", 0f, 100f, 50f);
            healthSlider.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            healthSlider.wholeNumbers = true;
            var healthLabel = AddTMPText(healthRow.transform, "HealthLabel", "50", 18f,
                TextAlignmentOptions.MidlineRight);
            healthLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 50;

            // Attack slider + label
            var attackRow = AddHorizontalRow(content.transform, "AttackRow", 40f);
            attackRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;
            var attackLabelPrefix = AddTMPText(attackRow.transform, "AttackPrefix", "Attack:", 18f,
                TextAlignmentOptions.MidlineLeft);
            attackLabelPrefix.gameObject.AddComponent<LayoutElement>().preferredWidth = 70;
            var attackSlider = AddSlider(attackRow.transform, "AttackSlider", 0f, 50f, 10f);
            attackSlider.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            attackSlider.wholeNumbers = true;
            var attackLabel = AddTMPText(attackRow.transform, "AttackLabel", "10", 18f,
                TextAlignmentOptions.MidlineRight);
            attackLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 50;

            // Preview text at bottom
            var previewText = AddTMPText(content.transform, "PreviewText",
                "Name: Hero | HP: 50 | ATK: 10", 22f, TextAlignmentOptions.Center,
                new Color(0.6f, 0.9f, 1f));
            previewText.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            // === CharacterView ===
            var viewGO = new GameObject("CharacterView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<CharacterView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_nameInput").objectReferenceValue = nameInput.GetComponent<TMP_InputField>();
            viewSO.FindProperty("_healthSlider").objectReferenceValue = healthSlider;
            viewSO.FindProperty("_attackSlider").objectReferenceValue = attackSlider;
            viewSO.FindProperty("_healthLabel").objectReferenceValue = healthLabel;
            viewSO.FindProperty("_attackLabel").objectReferenceValue = attackLabel;
            viewSO.FindProperty("_previewText").objectReferenceValue = previewText;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("TwoWayBindingLifetimeScope");
            var scope = scopeGO.AddComponent<TwoWayBindingLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_view").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[06-03] Saved: {scenePath}");
        }

        // ================================================================
        //  Module 07: Animation-Patterns
        // ================================================================

        public static void Build07_01_ButtonEffects()
        {
            var scenePath = "Assets/_Study/07-Animation-Patterns/Scenes/01-ButtonEffects.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "07-01 Button Effects", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === 2x2 Grid of buttons ===
            var gridPanel = AddPanel(canvas.transform, "GridPanel", new Color(0.12f, 0.12f, 0.18f, 0f));
            var gridPanelRT = Anchor(gridPanel.GetComponent<RectTransform>(), AnchorPreset.StretchAll,
                new Vector2(30, -60), new Vector2(-30, -20));
            var gridLayout = gridPanel.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(200, 120);
            gridLayout.spacing = new Vector2(20, 20);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            // Create 4 button cells, each with button + counter text
            var buttonNames = new[] { "ScalePunch", "ColorFlash", "Shake", "Bounce" };
            var buttonLabels = new[] { "Scale Punch", "Color Flash", "Shake", "Bounce" };
            var buttons = new GameObject[4];
            var countTexts = new TextMeshProUGUI[4];

            for (int i = 0; i < 4; i++)
            {
                // Cell container
                var cell = new GameObject($"Cell_{buttonNames[i]}", typeof(RectTransform),
                    typeof(VerticalLayoutGroup));
                cell.transform.SetParent(gridPanel.transform, false);
                var cellVLG = cell.GetComponent<VerticalLayoutGroup>();
                cellVLG.spacing = 6f;
                cellVLG.childAlignment = TextAnchor.MiddleCenter;
                cellVLG.childControlWidth = true;
                cellVLG.childControlHeight = true;
                cellVLG.childForceExpandWidth = true;
                cellVLG.childForceExpandHeight = false;

                var btn = AddButton(cell.transform, $"{buttonNames[i]}Button", buttonLabels[i],
                    new Vector2(180, 60));
                btn.AddComponent<LayoutElement>().preferredHeight = 60;
                buttons[i] = btn;

                var countText = AddTMPText(cell.transform, $"{buttonNames[i]}CountText", "Clicks: 0", 16f,
                    TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
                countText.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;
                countTexts[i] = countText;
            }

            // === ButtonEffectsView ===
            var viewGO = new GameObject("ButtonEffectsView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<ButtonEffectsView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_scalePunchButton").objectReferenceValue = buttons[0].GetComponent<Button>();
            viewSO.FindProperty("_colorFlashButton").objectReferenceValue = buttons[1].GetComponent<Button>();
            viewSO.FindProperty("_shakeButton").objectReferenceValue = buttons[2].GetComponent<Button>();
            viewSO.FindProperty("_bounceButton").objectReferenceValue = buttons[3].GetComponent<Button>();
            viewSO.FindProperty("_scalePunchCountText").objectReferenceValue = countTexts[0];
            viewSO.FindProperty("_colorFlashCountText").objectReferenceValue = countTexts[1];
            viewSO.FindProperty("_shakeCountText").objectReferenceValue = countTexts[2];
            viewSO.FindProperty("_bounceCountText").objectReferenceValue = countTexts[3];
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("ButtonEffectsLifetimeScope");
            var scope = scopeGO.AddComponent<ButtonEffectsLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_buttonEffectsView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[07-01] Saved: {scenePath}");
        }

        public static void Build07_02_StaggerList()
        {
            var scenePath = "Assets/_Study/07-Animation-Patterns/Scenes/02-StaggerList.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "07-02 Stagger List", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Controls row ===
            var controlsRow = AddHorizontalRow(canvas.transform, "ControlsRow", 44f);
            var controlsRowRT = Anchor(controlsRow.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(20, -55), new Vector2(-20, -100));
            controlsRow.GetComponent<HorizontalLayoutGroup>().spacing = 15;

            var toggleButton = AddButton(controlsRow.transform, "ToggleButton", "Show / Hide",
                new Vector2(160, 40));
            toggleButton.AddComponent<LayoutElement>().preferredWidth = 160;

            var speedLabel = AddTMPText(controlsRow.transform, "SpeedLabel", "Speed:", 18f,
                TextAlignmentOptions.MidlineLeft);
            speedLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;

            var speedSlider = AddSlider(controlsRow.transform, "SpeedSlider", 0.5f, 3f, 1f);
            speedSlider.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // === 8 colored bar items ===
            var itemsPanel = AddPanel(canvas.transform, "ItemsPanel", new Color(0, 0, 0, 0));
            var itemsPanelRT = Anchor(itemsPanel.GetComponent<RectTransform>(), AnchorPreset.StretchAll,
                new Vector2(20, -110), new Vector2(-20, -20));
            var itemsVLG = itemsPanel.AddComponent<VerticalLayoutGroup>();
            itemsVLG.spacing = 6f;
            itemsVLG.padding = new RectOffset(10, 10, 10, 10);
            itemsVLG.childAlignment = TextAnchor.UpperLeft;
            itemsVLG.childControlWidth = true;
            itemsVLG.childControlHeight = true;
            itemsVLG.childForceExpandWidth = true;
            itemsVLG.childForceExpandHeight = false;

            var barColors = new[]
            {
                new Color(0.9f, 0.3f, 0.3f, 0.8f),
                new Color(0.3f, 0.9f, 0.3f, 0.8f),
                new Color(0.3f, 0.3f, 0.9f, 0.8f),
                new Color(0.9f, 0.9f, 0.3f, 0.8f),
                new Color(0.9f, 0.3f, 0.9f, 0.8f),
                new Color(0.3f, 0.9f, 0.9f, 0.8f),
                new Color(0.9f, 0.6f, 0.3f, 0.8f),
                new Color(0.6f, 0.3f, 0.9f, 0.8f),
            };

            var items = new RectTransform[8];
            for (int i = 0; i < 8; i++)
            {
                // Each item: Image background + CanvasGroup + TMP text
                var itemGO = new GameObject($"Item_{i}", typeof(RectTransform), typeof(Image),
                    typeof(CanvasGroup));
                itemGO.transform.SetParent(itemsPanel.transform, false);
                var itemImage = itemGO.GetComponent<Image>();
                itemImage.color = barColors[i];
                itemImage.raycastTarget = false;
                var itemLE = itemGO.AddComponent<LayoutElement>();
                itemLE.preferredHeight = 40;

                var itemText = AddTMPText(itemGO.transform, "Label", $"Item {i + 1}", 18f,
                    TextAlignmentOptions.MidlineLeft);
                var itemTextRT = itemText.rectTransform;
                StretchFill(itemTextRT);
                itemTextRT.offsetMin = new Vector2(15, 0);

                items[i] = itemGO.GetComponent<RectTransform>();
            }

            // === StaggerListView ===
            var viewGO = new GameObject("StaggerListView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<StaggerListView>();
            var viewSO = new SerializedObject(view);

            var itemsProp = viewSO.FindProperty("_items");
            itemsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

            viewSO.FindProperty("_toggleButton").objectReferenceValue = toggleButton.GetComponent<Button>();
            viewSO.FindProperty("_speedSlider").objectReferenceValue = speedSlider;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("StaggerListLifetimeScope");
            var scope = scopeGO.AddComponent<StaggerListLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_staggerListView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[07-02] Saved: {scenePath}");
        }

        public static void Build07_03_PanelTransition()
        {
            var scenePath = "Assets/_Study/07-Animation-Patterns/Scenes/03-PanelTransition.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "07-03 Panel Transition", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === 4 transition buttons in a row ===
            var btnRow = AddHorizontalRow(canvas.transform, "TransitionButtons", 44f);
            var btnRowRT = Anchor(btnRow.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(10, -55), new Vector2(-10, -100));
            btnRow.GetComponent<HorizontalLayoutGroup>().spacing = 8;

            var fadeButton = AddButton(btnRow.transform, "FadeButton", "Fade", new Vector2(0, 40));
            fadeButton.AddComponent<LayoutElement>().flexibleWidth = 1;
            var slideLeftButton = AddButton(btnRow.transform, "SlideLeftButton", "Slide Left", new Vector2(0, 40));
            slideLeftButton.AddComponent<LayoutElement>().flexibleWidth = 1;
            var scalePopButton = AddButton(btnRow.transform, "ScalePopButton", "Scale Pop", new Vector2(0, 40));
            scalePopButton.AddComponent<LayoutElement>().flexibleWidth = 1;
            var flipYButton = AddButton(btnRow.transform, "FlipYButton", "Flip Y", new Vector2(0, 40));
            flipYButton.AddComponent<LayoutElement>().flexibleWidth = 1;

            // === Panel container area ===
            var panelArea = new GameObject("PanelArea", typeof(RectTransform));
            panelArea.transform.SetParent(canvas.transform, false);
            var panelAreaRT = panelArea.GetComponent<RectTransform>();
            Anchor(panelAreaRT, AnchorPreset.StretchAll,
                new Vector2(20, -110), new Vector2(-20, -20));

            // Panel A (visible)
            var panelAGO = new GameObject("PanelA", typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup));
            panelAGO.transform.SetParent(panelArea.transform, false);
            StretchFill(panelAGO.GetComponent<RectTransform>());
            panelAGO.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.7f, 1f);
            panelAGO.GetComponent<Image>().raycastTarget = false;
            var panelAText = AddTMPText(panelAGO.transform, "PanelAText", "Panel A", 36f,
                TextAlignmentOptions.Center);
            StretchFill(panelAText.rectTransform);

            // Panel B (hidden initially)
            var panelBGO = new GameObject("PanelB", typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup));
            panelBGO.transform.SetParent(panelArea.transform, false);
            StretchFill(panelBGO.GetComponent<RectTransform>());
            panelBGO.GetComponent<Image>().color = new Color(0.7f, 0.3f, 0.2f, 1f);
            panelBGO.GetComponent<Image>().raycastTarget = false;
            panelBGO.SetActive(false);
            var panelBText = AddTMPText(panelBGO.transform, "PanelBText", "Panel B", 36f,
                TextAlignmentOptions.Center);
            StretchFill(panelBText.rectTransform);

            // === PanelTransitionView (on PanelArea so GetComponent<RectTransform> works) ===
            var view = panelArea.AddComponent<PanelTransitionView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_panelA").objectReferenceValue = panelAGO.GetComponent<RectTransform>();
            viewSO.FindProperty("_panelB").objectReferenceValue = panelBGO.GetComponent<RectTransform>();
            viewSO.FindProperty("_fadeButton").objectReferenceValue = fadeButton.GetComponent<Button>();
            viewSO.FindProperty("_slideLeftButton").objectReferenceValue = slideLeftButton.GetComponent<Button>();
            viewSO.FindProperty("_scalePopButton").objectReferenceValue = scalePopButton.GetComponent<Button>();
            viewSO.FindProperty("_flipYButton").objectReferenceValue = flipYButton.GetComponent<Button>();
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("PanelTransitionLifetimeScope");
            var scope = scopeGO.AddComponent<PanelTransitionLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_panelTransitionView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[07-03] Saved: {scenePath}");
        }

        // ================================================================
        //  Module 08: Game-UI-Patterns
        // ================================================================

        public static void Build08_01_DamageNumbers()
        {
            var scenePath = "Assets/_Study/08-Game-UI-Patterns/Scenes/01-DamageNumbers.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "08-01 Damage Numbers", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Target area (colored rectangle in center) ===
            var targetImage = AddImage(canvas.transform, "TargetImage",
                new Color(0.3f, 0.15f, 0.15f, 0.8f), new Vector2(200, 200));
            targetImage.raycastTarget = true;
            var targetRT = targetImage.rectTransform;
            targetRT.anchorMin = new Vector2(0.5f, 0.5f);
            targetRT.anchorMax = new Vector2(0.5f, 0.5f);
            targetRT.anchoredPosition = new Vector2(0, 30);
            var targetLabel = AddTMPText(targetImage.transform, "TargetLabel", "TARGET", 24f,
                TextAlignmentOptions.Center);
            StretchFill(targetLabel.rectTransform);

            // === NumberContainer overlay (same area as target, but numbers float above) ===
            var numberContainerGO = new GameObject("NumberContainer", typeof(RectTransform));
            numberContainerGO.transform.SetParent(canvas.transform, false);
            var numberContainerRT = numberContainerGO.GetComponent<RectTransform>();
            StretchFill(numberContainerRT);

            // === DamageNumberView prefab (in-scene template, disabled — pool will clone) ===
            var prefabGO = new GameObject("DamageNumberPrefab", typeof(RectTransform),
                typeof(CanvasGroup));
            prefabGO.transform.SetParent(numberContainerGO.transform, false);
            var prefabRT = prefabGO.GetComponent<RectTransform>();
            prefabRT.sizeDelta = new Vector2(120, 40);
            prefabGO.SetActive(false);

            var prefabText = AddTMPText(prefabGO.transform, "Text", "0", 36f,
                TextAlignmentOptions.Center);
            StretchFill(prefabText.rectTransform);

            var prefabView = prefabGO.AddComponent<DamageNumberView>();
            var prefabViewSO = new SerializedObject(prefabView);
            prefabViewSO.FindProperty("_text").objectReferenceValue = prefabText;
            prefabViewSO.ApplyModifiedPropertiesWithoutUndo();

            // === Pool info text ===
            var poolInfoText = AddTMPText(canvas.transform, "PoolInfoText", "Pool: 0/0", 16f,
                TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            Anchor(poolInfoText.rectTransform, AnchorPreset.BottomStretch,
                new Vector2(20, 80), new Vector2(-20, 105));

            // === 3 buttons at bottom ===
            var btnRow = AddHorizontalRow(canvas.transform, "ButtonRow", 50f);
            Anchor(btnRow.GetComponent<RectTransform>(), AnchorPreset.BottomStretch,
                new Vector2(30, 20), new Vector2(-30, 70));
            btnRow.GetComponent<HorizontalLayoutGroup>().spacing = 15;

            var dealDamageBtn = AddButton(btnRow.transform, "DealDamageButton", "Damage", new Vector2(0, 44));
            dealDamageBtn.AddComponent<LayoutElement>().flexibleWidth = 1;
            var criticalHitBtn = AddButton(btnRow.transform, "CriticalHitButton", "Critical!", new Vector2(0, 44));
            criticalHitBtn.AddComponent<LayoutElement>().flexibleWidth = 1;
            var healBtn = AddButton(btnRow.transform, "HealButton", "Heal", new Vector2(0, 44));
            healBtn.AddComponent<LayoutElement>().flexibleWidth = 1;

            // === DamageNumberDemoView ===
            var demoViewGO = new GameObject("DamageNumberDemoView");
            demoViewGO.transform.SetParent(canvas.transform, false);
            var demoView = demoViewGO.AddComponent<DamageNumberDemoView>();
            var demoViewSO = new SerializedObject(demoView);
            demoViewSO.FindProperty("_dealDamageButton").objectReferenceValue = dealDamageBtn.GetComponent<Button>();
            demoViewSO.FindProperty("_criticalHitButton").objectReferenceValue = criticalHitBtn.GetComponent<Button>();
            demoViewSO.FindProperty("_healButton").objectReferenceValue = healBtn.GetComponent<Button>();
            demoViewSO.FindProperty("_targetImage").objectReferenceValue = targetRT;
            demoViewSO.FindProperty("_numberContainer").objectReferenceValue = numberContainerRT;
            demoViewSO.FindProperty("_poolInfoText").objectReferenceValue = poolInfoText;
            demoViewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("DamageNumberLifetimeScope");
            var scope = scopeGO.AddComponent<DamageNumberLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_demoView").objectReferenceValue = demoView;
            scopeSO.FindProperty("_damageNumberPrefab").objectReferenceValue = prefabView;
            scopeSO.FindProperty("_numberContainer").objectReferenceValue = numberContainerRT;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[08-01] Saved: {scenePath}");
        }

        public static void Build08_02_DialogTypewriter()
        {
            var scenePath = "Assets/_Study/08-Game-UI-Patterns/Scenes/02-DialogTypewriter.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "08-02 Dialog Typewriter", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Start button + status (top area) ===
            var topRow = AddHorizontalRow(canvas.transform, "TopRow", 44f);
            var topRowRT = Anchor(topRow.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(20, -55), new Vector2(-20, -100));
            topRow.GetComponent<HorizontalLayoutGroup>().spacing = 15;

            var startDialogButton = AddButton(topRow.transform, "StartDialogButton", "Start Dialog",
                new Vector2(160, 40));
            startDialogButton.AddComponent<LayoutElement>().preferredWidth = 160;

            var statusText = AddTMPText(topRow.transform, "StatusText", "Ready", 18f,
                TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.7f, 0.7f));
            statusText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // === DialogDemoView ===
            var demoViewGO = new GameObject("DialogDemoView");
            demoViewGO.transform.SetParent(canvas.transform, false);
            var demoView = demoViewGO.AddComponent<DialogDemoView>();
            var demoViewSO = new SerializedObject(demoView);
            demoViewSO.FindProperty("_startDialogButton").objectReferenceValue = startDialogButton.GetComponent<Button>();
            demoViewSO.FindProperty("_statusText").objectReferenceValue = statusText;
            demoViewSO.ApplyModifiedPropertiesWithoutUndo();

            // === Dialog panel (bottom area) ===
            var dialogPanelGO = new GameObject("DialogPanel", typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup));
            dialogPanelGO.transform.SetParent(canvas.transform, false);
            var dialogPanelRT = dialogPanelGO.GetComponent<RectTransform>();
            dialogPanelRT.anchorMin = new Vector2(0, 0);
            dialogPanelRT.anchorMax = new Vector2(1, 0);
            dialogPanelRT.anchoredPosition = new Vector2(0, 120);
            dialogPanelRT.sizeDelta = new Vector2(-30, 200);
            dialogPanelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 0.95f);
            var dialogCG = dialogPanelGO.GetComponent<CanvasGroup>();

            var dialogVLG = dialogPanelGO.AddComponent<VerticalLayoutGroup>();
            dialogVLG.spacing = 8f;
            dialogVLG.padding = new RectOffset(20, 20, 15, 15);
            dialogVLG.childAlignment = TextAnchor.UpperLeft;
            dialogVLG.childControlWidth = true;
            dialogVLG.childControlHeight = true;
            dialogVLG.childForceExpandWidth = true;
            dialogVLG.childForceExpandHeight = false;

            // Speaker name
            var speakerText = AddTMPText(dialogPanelGO.transform, "SpeakerText", "Speaker", 22f,
                TextAlignmentOptions.TopLeft, new Color(1f, 0.85f, 0.3f));
            speakerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            // Body text
            var bodyText = AddTMPText(dialogPanelGO.transform, "BodyText",
                "Dialog text will appear here with typewriter effect...", 18f,
                TextAlignmentOptions.TopLeft);
            var bodyTextLE = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyTextLE.preferredHeight = 80;
            bodyTextLE.flexibleHeight = 1;

            // Bottom row: Skip button + Continue arrow
            var dialogBottomRow = AddHorizontalRow(dialogPanelGO.transform, "DialogBottomRow", 30f);
            dialogBottomRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;
            dialogBottomRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;

            var skipButton = AddButton(dialogBottomRow.transform, "SkipButton", "Skip",
                new Vector2(80, 28));
            skipButton.AddComponent<LayoutElement>().preferredWidth = 80;

            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(dialogBottomRow.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Continue arrow with CanvasGroup
            var continueArrowGO = new GameObject("ContinueArrow", typeof(RectTransform),
                typeof(CanvasGroup));
            continueArrowGO.transform.SetParent(dialogBottomRow.transform, false);
            continueArrowGO.AddComponent<LayoutElement>().preferredWidth = 30;
            var continueArrowCG = continueArrowGO.GetComponent<CanvasGroup>();
            continueArrowCG.alpha = 0f;
            var arrowText = AddTMPText(continueArrowGO.transform, "ArrowText", "\u25BC", 22f,
                TextAlignmentOptions.Center);
            StretchFill(arrowText.rectTransform);

            // === DialogView ===
            var dialogViewGO = new GameObject("DialogView");
            dialogViewGO.transform.SetParent(canvas.transform, false);
            var dialogView = dialogViewGO.AddComponent<DialogView>();
            var dialogViewSO = new SerializedObject(dialogView);
            dialogViewSO.FindProperty("_speakerText").objectReferenceValue = speakerText;
            dialogViewSO.FindProperty("_bodyText").objectReferenceValue = bodyText;
            dialogViewSO.FindProperty("_continueArrow").objectReferenceValue = continueArrowCG;
            dialogViewSO.FindProperty("_skipButton").objectReferenceValue = skipButton.GetComponent<Button>();
            dialogViewSO.FindProperty("_dialogPanel").objectReferenceValue = dialogCG;
            dialogViewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("DialogTypewriterLifetimeScope");
            var scope = scopeGO.AddComponent<DialogTypewriterLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_demoView").objectReferenceValue = demoView;
            scopeSO.FindProperty("_dialogView").objectReferenceValue = dialogView;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[08-02] Saved: {scenePath}");
        }

        public static void Build08_03_InventoryGrid()
        {
            var scenePath = "Assets/_Study/08-Game-UI-Patterns/Scenes/03-InventoryGrid.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title + Gold row ===
            var topRow = AddHorizontalRow(canvas.transform, "TopRow", 40f);
            var topRowRT = Anchor(topRow.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(10, -10), new Vector2(-10, -50));
            topRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;

            var titleText = AddTMPText(topRow.transform, "Title", "08-03 Inventory Grid", 26f,
                TextAlignmentOptions.MidlineLeft);
            titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var goldText = AddTMPText(topRow.transform, "GoldText", "Gold: 1000", 20f,
                TextAlignmentOptions.MidlineRight, new Color(1f, 0.85f, 0f));
            goldText.gameObject.AddComponent<LayoutElement>().preferredWidth = 140;

            // === Main content: Grid (left) + Detail panel (right) ===
            var mainRow = AddHorizontalRow(canvas.transform, "MainRow", 0f);
            var mainRowRT = mainRow.GetComponent<RectTransform>();
            Anchor(mainRowRT, AnchorPreset.StretchAll,
                new Vector2(10, -55), new Vector2(-10, -10));
            var mainRowHLG = mainRow.GetComponent<HorizontalLayoutGroup>();
            mainRowHLG.spacing = 15;
            mainRowHLG.childControlWidth = true;
            mainRowHLG.childControlHeight = true;
            mainRowHLG.childForceExpandWidth = false;
            mainRowHLG.childForceExpandHeight = true;
            // Remove fixed LayoutElement height so it stretches
            var mainRowLE = mainRow.GetComponent<LayoutElement>();
            if (mainRowLE != null) Object.DestroyImmediate(mainRowLE);

            // === Grid container (left side) ===
            var gridContainer = new GameObject("GridContainer", typeof(RectTransform), typeof(Image),
                typeof(GridLayoutGroup));
            gridContainer.transform.SetParent(mainRow.transform, false);
            gridContainer.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            gridContainer.GetComponent<Image>().raycastTarget = false;
            var gridContainerLE = gridContainer.AddComponent<LayoutElement>();
            gridContainerLE.flexibleWidth = 3;
            gridContainerLE.flexibleHeight = 1;

            var gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(80, 80);
            gridLayout.spacing = new Vector2(6, 6);
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            // === 16 inventory slots ===
            var slots = new InventorySlotView[16];
            for (int i = 0; i < 16; i++)
            {
                var slotGO = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image),
                    typeof(Button));
                slotGO.transform.SetParent(gridContainer.transform, false);

                var slotImage = slotGO.GetComponent<Image>();
                slotImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                var slotButton = slotGO.GetComponent<Button>();
                slotButton.targetGraphic = slotImage;

                // Icon text (centered, larger)
                var iconText = AddTMPText(slotGO.transform, "IconText", "", 28f,
                    TextAlignmentOptions.Center);
                var iconTextRT = iconText.rectTransform;
                StretchFill(iconTextRT);
                iconTextRT.offsetMin = new Vector2(2, 15);
                iconTextRT.offsetMax = new Vector2(-2, -2);

                // Count text (bottom-right)
                var countText = AddTMPText(slotGO.transform, "CountText", "", 14f,
                    TextAlignmentOptions.BottomRight, new Color(0.9f, 0.9f, 0.9f));
                var countTextRT = countText.rectTransform;
                StretchFill(countTextRT);
                countTextRT.offsetMin = new Vector2(2, 2);
                countTextRT.offsetMax = new Vector2(-4, -2);

                // Selection highlight (border-like overlay, initially hidden)
                var highlightGO = new GameObject("SelectionHighlight", typeof(RectTransform),
                    typeof(Image));
                highlightGO.transform.SetParent(slotGO.transform, false);
                StretchFill(highlightGO.GetComponent<RectTransform>());
                var highlightImage = highlightGO.GetComponent<Image>();
                highlightImage.color = new Color(1f, 0.85f, 0f, 0.4f);
                highlightImage.raycastTarget = false;
                highlightGO.SetActive(false);

                // InventorySlotView component
                var slotView = slotGO.AddComponent<InventorySlotView>();
                var slotViewSO = new SerializedObject(slotView);
                slotViewSO.FindProperty("_background").objectReferenceValue = slotImage;
                slotViewSO.FindProperty("_iconText").objectReferenceValue = iconText;
                slotViewSO.FindProperty("_countText").objectReferenceValue = countText;
                slotViewSO.FindProperty("_selectionHighlight").objectReferenceValue = highlightGO;
                slotViewSO.FindProperty("_button").objectReferenceValue = slotButton;
                slotViewSO.ApplyModifiedPropertiesWithoutUndo();

                slots[i] = slotView;
            }

            // === Detail panel (right side) ===
            var detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup));
            detailPanel.transform.SetParent(mainRow.transform, false);
            detailPanel.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.8f);
            detailPanel.GetComponent<Image>().raycastTarget = false;
            var detailPanelLE = detailPanel.AddComponent<LayoutElement>();
            detailPanelLE.flexibleWidth = 2;
            detailPanelLE.flexibleHeight = 1;

            var detailVLG = detailPanel.GetComponent<VerticalLayoutGroup>();
            detailVLG.spacing = 10f;
            detailVLG.padding = new RectOffset(15, 15, 15, 15);
            detailVLG.childAlignment = TextAnchor.UpperLeft;
            detailVLG.childControlWidth = true;
            detailVLG.childControlHeight = true;
            detailVLG.childForceExpandWidth = true;
            detailVLG.childForceExpandHeight = false;

            var detailIcon = AddTMPText(detailPanel.transform, "DetailIcon", "", 48f,
                TextAlignmentOptions.Center);
            detailIcon.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;

            var detailName = AddTMPText(detailPanel.transform, "DetailName", "Item Name", 24f,
                TextAlignmentOptions.TopLeft);
            detailName.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;

            var detailRarity = AddTMPText(detailPanel.transform, "DetailRarity", "Common", 18f,
                TextAlignmentOptions.TopLeft, new Color(0.7f, 0.7f, 0.7f));
            detailRarity.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;

            var detailDescription = AddTMPText(detailPanel.transform, "DetailDescription",
                "Select an item to see its details.", 16f,
                TextAlignmentOptions.TopLeft, new Color(0.8f, 0.8f, 0.8f));
            var detailDescLE = detailDescription.gameObject.AddComponent<LayoutElement>();
            detailDescLE.preferredHeight = 60;
            detailDescLE.flexibleHeight = 1;

            detailPanel.SetActive(false);

            // === InventoryGridView ===
            var gridViewGO = new GameObject("InventoryGridView");
            gridViewGO.transform.SetParent(canvas.transform, false);
            var gridView = gridViewGO.AddComponent<InventoryGridView>();
            var gridViewSO = new SerializedObject(gridView);

            var slotsProp = gridViewSO.FindProperty("_slots");
            slotsProp.arraySize = 16;
            for (int i = 0; i < 16; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

            gridViewSO.FindProperty("_detailPanel").objectReferenceValue = detailPanel;
            gridViewSO.FindProperty("_detailName").objectReferenceValue = detailName;
            gridViewSO.FindProperty("_detailRarity").objectReferenceValue = detailRarity;
            gridViewSO.FindProperty("_detailDescription").objectReferenceValue = detailDescription;
            gridViewSO.FindProperty("_detailIcon").objectReferenceValue = detailIcon;
            gridViewSO.FindProperty("_goldText").objectReferenceValue = goldText;
            gridViewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("InventoryGridLifetimeScope");
            var scope = scopeGO.AddComponent<InventoryGridLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_gridView").objectReferenceValue = gridView;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[08-03] Saved: {scenePath}");
        }

        // ================================================================
        //  Local Helpers
        // ================================================================

        /// <summary>
        /// Creates a TMP_InputField with placeholder text using DefaultControls-like approach.
        /// </summary>
        private static GameObject CreateTMPInputField(Transform parent, string name, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Text Area (viewport with RectMask2D)
            var textAreaGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaGO.transform.SetParent(go.transform, false);
            var textAreaRT = textAreaGO.GetComponent<RectTransform>();
            StretchFill(textAreaRT);
            textAreaRT.offsetMin = new Vector2(10, 2);
            textAreaRT.offsetMax = new Vector2(-10, -2);

            // Placeholder text
            var placeholderTMP = AddTMPText(textAreaGO.transform, "Placeholder", placeholder, 18f,
                TextAlignmentOptions.MidlineLeft, new Color(0.5f, 0.5f, 0.5f, 0.7f));
            StretchFill(placeholderTMP.rectTransform);
            placeholderTMP.fontStyle = FontStyles.Italic;

            // Input text
            var inputTMP = AddTMPText(textAreaGO.transform, "Text", "", 18f,
                TextAlignmentOptions.MidlineLeft);
            StretchFill(inputTMP.rectTransform);

            // Wire up TMP_InputField
            var inputField = go.GetComponent<TMP_InputField>();
            inputField.textViewport = textAreaRT;
            inputField.textComponent = inputTMP;
            inputField.placeholder = placeholderTMP;
            inputField.fontAsset = inputTMP.font;
            inputField.pointSize = 18f;

            // Set target graphic for interactivity
            inputField.targetGraphic = bg;

            return go;
        }
    }
}
#endif
