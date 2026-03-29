#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UIStudy.DragDrop.Views;
using UIStudy.DragDrop.LifetimeScopes;
using UIStudy.AdvancedHUD.Views;
using UIStudy.AdvancedHUD.LifetimeScopes;
using static UIStudy.Editor.StudySceneBuilders;

namespace UIStudy.Editor
{
    /// <summary>
    /// Scene builders for modules 09 (Drag And Drop) and 10 (Advanced HUD).
    /// </summary>
    public static class StudySceneBuilders_09_10
    {
        // ================================================================
        //  Menu Items
        // ================================================================

        [MenuItem("UI Study/Build 09 - Drag And Drop Scenes")]
        public static void Build09All()
        {
            Build09_01_BasicDragDrop();
            Build09_02_SortableList();
            Build09_03_GridSlotSwap();
            Debug.Log("[StudySceneBuilders] Module 09 — all scenes built.");
        }

        [MenuItem("UI Study/Build 10 - Advanced HUD Scenes")]
        public static void Build10All()
        {
            Build10_01_RadialMenu();
            Build10_02_LoadingScreen();
            Build10_03_ResourceHUD();
            Debug.Log("[StudySceneBuilders] Module 10 — all scenes built.");
        }

        [MenuItem("UI Study/Build 09-10 All New Scenes")]
        public static void BuildAll_09_10()
        {
            Build09All();
            Build10All();
            Debug.Log("[StudySceneBuilders] Modules 09-10 — ALL scenes built.");
        }

        // ================================================================
        //  Module 09: Drag-And-Drop
        // ================================================================

        public static void Build09_01_BasicDragDrop()
        {
            var scenePath = "Assets/_Study/09-Drag-And-Drop/Scenes/01-BasicDragDrop.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "09-01 Basic Drag & Drop", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Draggable Items Row (middle) ===
            var itemsRow = AddHorizontalRow(canvas.transform, "DraggableItemsRow", 100f);
            var itemsRowRT = Anchor(itemsRow.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(40, -80), new Vector2(-40, -200));
            itemsRow.GetComponent<HorizontalLayoutGroup>().spacing = 20;
            itemsRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            var itemNames = new[] { "Sword", "Shield", "Potion" };
            var itemColors = new[]
            {
                new Color(0.8f, 0.3f, 0.3f, 0.9f), // red
                new Color(0.3f, 0.5f, 0.8f, 0.9f), // blue
                new Color(0.3f, 0.8f, 0.4f, 0.9f), // green
            };
            var draggableItems = new DraggableItemView[3];

            for (int i = 0; i < 3; i++)
            {
                // DraggableItemView: Image + CanvasGroup + TMP label
                var itemGO = new GameObject($"Draggable_{itemNames[i]}",
                    typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                itemGO.transform.SetParent(itemsRow.transform, false);
                var itemImg = itemGO.GetComponent<Image>();
                itemImg.color = itemColors[i];
                itemGO.AddComponent<LayoutElement>().preferredWidth = 100;

                var itemLabel = AddTMPText(itemGO.transform, "Label", itemNames[i], 18f,
                    TextAlignmentOptions.Center);
                StretchFill(itemLabel.rectTransform);

                var draggable = itemGO.AddComponent<DraggableItemView>();
                var draggableSO = new SerializedObject(draggable);
                draggableSO.FindProperty("_image").objectReferenceValue = itemImg;
                draggableSO.FindProperty("_label").objectReferenceValue = itemLabel;
                draggableSO.FindProperty("_canvasGroup").objectReferenceValue = itemGO.GetComponent<CanvasGroup>();
                draggableSO.ApplyModifiedPropertiesWithoutUndo();

                draggableItems[i] = draggable;
            }

            // === Drop Zones Row (below items) ===
            var dropRow = AddHorizontalRow(canvas.transform, "DropZonesRow", 120f);
            var dropRowRT = Anchor(dropRow.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(60, -220), new Vector2(-60, -360));
            dropRow.GetComponent<HorizontalLayoutGroup>().spacing = 30;
            dropRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            // Equip drop zone
            var equipZone = CreateDropZone(dropRow.transform, "EquipZone", "Equip");
            // Discard drop zone
            var discardZone = CreateDropZone(dropRow.transform, "DiscardZone", "Discard");

            // === Status text ===
            var statusText = AddTMPText(canvas.transform, "StatusText", "Drag items to a drop zone", 20f,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            Anchor(statusText.rectTransform, AnchorPreset.BottomStretch,
                new Vector2(20, 20), new Vector2(-20, 60));

            // === BasicDragDropDemoView ===
            var viewGO = new GameObject("BasicDragDropDemoView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<BasicDragDropDemoView>();
            var viewSO = new SerializedObject(view);

            // Wire _draggableItems array
            var draggablesProp = viewSO.FindProperty("_draggableItems");
            draggablesProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                draggablesProp.GetArrayElementAtIndex(i).objectReferenceValue = draggableItems[i];

            viewSO.FindProperty("_equipZone").objectReferenceValue = equipZone;
            viewSO.FindProperty("_discardZone").objectReferenceValue = discardZone;
            viewSO.FindProperty("_statusText").objectReferenceValue = statusText;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("BasicDragDropLifetimeScope");
            var scope = scopeGO.AddComponent<BasicDragDropLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_demoView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[09-01] Saved: {scenePath}");
        }

        public static void Build09_02_SortableList()
        {
            var scenePath = "Assets/_Study/09-Drag-And-Drop/Scenes/02-SortableList.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "09-02 Sortable List", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === List Container (VerticalLayoutGroup) ===
            var listPanel = AddPanel(canvas.transform, "ListContainer",
                new Color(0.12f, 0.12f, 0.18f, 0.5f));
            var listPanelRT = Anchor(listPanel.GetComponent<RectTransform>(), AnchorPreset.StretchAll,
                new Vector2(20, -60), new Vector2(-20, -60));
            var listVLG = listPanel.AddComponent<VerticalLayoutGroup>();
            listVLG.spacing = 4f;
            listVLG.padding = new RectOffset(10, 10, 10, 10);
            listVLG.childAlignment = TextAnchor.UpperLeft;
            listVLG.childControlWidth = true;
            listVLG.childControlHeight = true;
            listVLG.childForceExpandWidth = true;
            listVLG.childForceExpandHeight = false;

            // === 6 Sortable Items ===
            var barColors = new[]
            {
                new Color(0.2f, 0.2f, 0.25f, 0.9f),
                new Color(0.25f, 0.25f, 0.3f, 0.9f),
                new Color(0.2f, 0.2f, 0.25f, 0.9f),
                new Color(0.25f, 0.25f, 0.3f, 0.9f),
                new Color(0.2f, 0.2f, 0.25f, 0.9f),
                new Color(0.25f, 0.25f, 0.3f, 0.9f),
            };
            var sortableItems = new SortableItemView[6];

            for (int i = 0; i < 6; i++)
            {
                // Each item: HorizontalLayoutGroup with drag handle + label
                var itemGO = new GameObject($"SortableItem_{i}",
                    typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                itemGO.transform.SetParent(listPanel.transform, false);
                var itemBg = itemGO.GetComponent<Image>();
                itemBg.color = barColors[i];
                var itemLE = itemGO.AddComponent<LayoutElement>();
                itemLE.preferredHeight = 50;

                var itemHLG = itemGO.AddComponent<HorizontalLayoutGroup>();
                itemHLG.spacing = 10f;
                itemHLG.padding = new RectOffset(10, 10, 5, 5);
                itemHLG.childAlignment = TextAnchor.MiddleLeft;
                itemHLG.childControlWidth = true;
                itemHLG.childControlHeight = true;
                itemHLG.childForceExpandWidth = false;
                itemHLG.childForceExpandHeight = true;

                // Drag handle icon
                var handleText = AddTMPText(itemGO.transform, "HandleIcon", "::", 22f,
                    TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                handleText.gameObject.AddComponent<LayoutElement>().preferredWidth = 30;

                // Label
                var label = AddTMPText(itemGO.transform, "Label", $"Task {i + 1}", 20f,
                    TextAlignmentOptions.MidlineLeft);
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                // Add SortableItemView
                var sortable = itemGO.AddComponent<SortableItemView>();
                var sortableSO = new SerializedObject(sortable);
                sortableSO.FindProperty("_background").objectReferenceValue = itemBg;
                sortableSO.FindProperty("_handleIcon").objectReferenceValue = handleText;
                sortableSO.FindProperty("_label").objectReferenceValue = label;
                sortableSO.FindProperty("_canvasGroup").objectReferenceValue = itemGO.GetComponent<CanvasGroup>();
                sortableSO.ApplyModifiedPropertiesWithoutUndo();

                sortableItems[i] = sortable;
            }

            // === Insertion Indicator Line (thin bar, hidden by default) ===
            var insertionLine = AddImage(listPanel.transform, "InsertionLine",
                new Color(1f, 0.8f, 0.2f, 1f), new Vector2(0, 3));
            var insertionLineRT = insertionLine.rectTransform;
            insertionLineRT.anchorMin = new Vector2(0, 0.5f);
            insertionLineRT.anchorMax = new Vector2(1, 0.5f);
            insertionLineRT.sizeDelta = new Vector2(-20, 3);
            insertionLine.gameObject.SetActive(false);

            // === Status text ===
            var statusText = AddTMPText(canvas.transform, "StatusText", "Drag items to reorder", 18f,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            Anchor(statusText.rectTransform, AnchorPreset.BottomStretch,
                new Vector2(20, 20), new Vector2(-20, 55));

            // === SortableListView ===
            var viewGO = new GameObject("SortableListView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<SortableListView>();
            var viewSO = new SerializedObject(view);

            viewSO.FindProperty("_listContainer").objectReferenceValue = listPanel.GetComponent<RectTransform>();

            // Wire _items array
            var itemsProp = viewSO.FindProperty("_items");
            itemsProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = sortableItems[i];

            viewSO.FindProperty("_insertionLine").objectReferenceValue = insertionLineRT;
            viewSO.FindProperty("_statusText").objectReferenceValue = statusText;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("SortableListLifetimeScope");
            var scope = scopeGO.AddComponent<SortableListLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_listView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[09-02] Saved: {scenePath}");
        }

        public static void Build09_03_GridSlotSwap()
        {
            var scenePath = "Assets/_Study/09-Drag-And-Drop/Scenes/03-GridSlotSwap.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "09-03 Grid Slot Swap", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === 3x3 Grid Container ===
            var gridPanel = AddPanel(canvas.transform, "GridPanel", new Color(0.1f, 0.1f, 0.15f, 0.5f));
            var gridPanelRT = gridPanel.GetComponent<RectTransform>();
            gridPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
            gridPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
            // 3 cells x 100 + 2 spacing x 10 + padding = 340x340
            gridPanelRT.sizeDelta = new Vector2(340, 340);
            gridPanelRT.anchoredPosition = new Vector2(0, 20);

            var gridLayout = gridPanel.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(100, 100);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.padding = new RectOffset(5, 5, 5, 5);

            // === 9 Swap Slots ===
            // 5 filled ("A"-"E"), 4 empty
            var slotContents = new[] { "A", "B", "C", "D", "E", "", "", "", "" };
            var swapSlots = new SwapSlotView[9];

            for (int i = 0; i < 9; i++)
            {
                bool isEmpty = string.IsNullOrEmpty(slotContents[i]);
                var slotBgColor = isEmpty
                    ? new Color(0.15f, 0.15f, 0.15f, 0.6f)
                    : new Color(0.25f, 0.35f, 0.5f, 0.9f);

                var slotGO = new GameObject($"Slot_{i}",
                    typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                slotGO.transform.SetParent(gridPanel.transform, false);
                var slotBg = slotGO.GetComponent<Image>();
                slotBg.color = slotBgColor;

                // Icon text
                var iconText = AddTMPText(slotGO.transform, "IconText",
                    isEmpty ? "" : slotContents[i], 28f, TextAlignmentOptions.Center);
                StretchFill(iconText.rectTransform);

                // Highlight overlay (initially hidden)
                var highlight = AddImage(slotGO.transform, "Highlight",
                    new Color(0.4f, 0.5f, 0.3f, 0.5f), Vector2.zero);
                StretchFill(highlight.rectTransform);
                highlight.gameObject.SetActive(false);

                // Add SwapSlotView
                var slot = slotGO.AddComponent<SwapSlotView>();
                var slotSO = new SerializedObject(slot);
                slotSO.FindProperty("_background").objectReferenceValue = slotBg;
                slotSO.FindProperty("_iconText").objectReferenceValue = iconText;
                slotSO.FindProperty("_highlight").objectReferenceValue = highlight;
                slotSO.FindProperty("_canvasGroup").objectReferenceValue = slotGO.GetComponent<CanvasGroup>();
                slotSO.ApplyModifiedPropertiesWithoutUndo();

                swapSlots[i] = slot;
            }

            // === Status text ===
            var statusText = AddTMPText(canvas.transform, "StatusText",
                "Drag to swap slots", 20f,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            Anchor(statusText.rectTransform, AnchorPreset.BottomStretch,
                new Vector2(20, 20), new Vector2(-20, 60));

            // === GridSwapView ===
            var viewGO = new GameObject("GridSwapView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<GridSwapView>();
            var viewSO = new SerializedObject(view);

            // Wire _slots array
            var slotsProp = viewSO.FindProperty("_slots");
            slotsProp.arraySize = 9;
            for (int i = 0; i < 9; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = swapSlots[i];

            viewSO.FindProperty("_statusText").objectReferenceValue = statusText;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("GridSwapLifetimeScope");
            var scope = scopeGO.AddComponent<GridSwapLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_gridView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[09-03] Saved: {scenePath}");
        }

        // ================================================================
        //  Module 10: Advanced-HUD
        // ================================================================

        public static void Build10_01_RadialMenu()
        {
            var scenePath = "Assets/_Study/10-Advanced-HUD/Scenes/01-RadialMenu.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Toggle Button (top) ===
            var toggleBtn = AddButton(canvas.transform, "ToggleButton", "Open Menu", new Vector2(180, 44));
            var toggleBtnRT = toggleBtn.GetComponent<RectTransform>();
            toggleBtnRT.anchorMin = new Vector2(0.5f, 1);
            toggleBtnRT.anchorMax = new Vector2(0.5f, 1);
            toggleBtnRT.anchoredPosition = new Vector2(0, -40);
            var toggleBtnLabel = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            // === Center selection text ===
            var centerText = AddTMPText(canvas.transform, "CenterText", "Select an item", 22f,
                TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
            var centerTextRT = centerText.rectTransform;
            centerTextRT.anchorMin = new Vector2(0.5f, 0.5f);
            centerTextRT.anchorMax = new Vector2(0.5f, 0.5f);
            centerTextRT.sizeDelta = new Vector2(200, 40);
            centerTextRT.anchoredPosition = Vector2.zero;

            // === Menu Container (holds 8 buttons, positioned around center) ===
            var menuContainerGO = new GameObject("MenuContainer", typeof(RectTransform));
            menuContainerGO.transform.SetParent(canvas.transform, false);
            var menuContainerRT = menuContainerGO.GetComponent<RectTransform>();
            menuContainerRT.anchorMin = new Vector2(0.5f, 0.5f);
            menuContainerRT.anchorMax = new Vector2(0.5f, 0.5f);
            menuContainerRT.sizeDelta = new Vector2(400, 400);
            menuContainerRT.anchoredPosition = Vector2.zero;

            // 8 menu item buttons (centered; View.Awake will reposition in a circle)
            var menuLabels = new[] { "Attack", "Defend", "Magic", "Items", "Move", "Wait", "Flee", "Status" };
            var menuButtons = new Button[8];
            var menuLabelTMPs = new TextMeshProUGUI[8];

            for (int i = 0; i < 8; i++)
            {
                var btnGO = AddButton(menuContainerGO.transform, $"MenuBtn_{i}", menuLabels[i],
                    new Vector2(70, 70));
                var btnRT = btnGO.GetComponent<RectTransform>();
                // Position all at center; RadialMenuView.Awake will reposition
                btnRT.anchorMin = new Vector2(0.5f, 0.5f);
                btnRT.anchorMax = new Vector2(0.5f, 0.5f);
                btnRT.anchoredPosition = Vector2.zero;

                // Style as circle-ish button
                btnGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                menuButtons[i] = btnGO.GetComponent<Button>();
                menuLabelTMPs[i] = btnGO.GetComponentInChildren<TextMeshProUGUI>();
            }

            // === RadialMenuView ===
            var viewGO = new GameObject("RadialMenuView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<RadialMenuView>();
            var viewSO = new SerializedObject(view);

            viewSO.FindProperty("_menuContainer").objectReferenceValue = menuContainerRT;

            // Wire _menuButtons array
            var btnsProp = viewSO.FindProperty("_menuButtons");
            btnsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
                btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = menuButtons[i];

            // Wire _menuLabels array
            var labelsProp = viewSO.FindProperty("_menuLabels");
            labelsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = menuLabelTMPs[i];

            viewSO.FindProperty("_centerText").objectReferenceValue = centerText;
            viewSO.FindProperty("_toggleButton").objectReferenceValue = toggleBtn.GetComponent<Button>();
            viewSO.FindProperty("_toggleButtonText").objectReferenceValue = toggleBtnLabel;
            viewSO.FindProperty("_radius").floatValue = 150f;
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("RadialMenuLifetimeScope");
            var scope = scopeGO.AddComponent<RadialMenuLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_radialMenuView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[10-01] Saved: {scenePath}");
        }

        public static void Build10_02_LoadingScreen()
        {
            var scenePath = "Assets/_Study/10-Advanced-HUD/Scenes/02-LoadingScreen.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Demo Controls (center) ===
            var demoPanel = AddPanel(canvas.transform, "DemoPanel", new Color(0.15f, 0.15f, 0.15f, 0.6f));
            var demoPanelRT = demoPanel.GetComponent<RectTransform>();
            demoPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
            demoPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
            demoPanelRT.sizeDelta = new Vector2(300, 150);
            demoPanelRT.anchoredPosition = Vector2.zero;

            var demoVLG = demoPanel.AddComponent<VerticalLayoutGroup>();
            demoVLG.spacing = 12f;
            demoVLG.padding = new RectOffset(20, 20, 20, 20);
            demoVLG.childAlignment = TextAnchor.MiddleCenter;
            demoVLG.childControlWidth = true;
            demoVLG.childControlHeight = true;
            demoVLG.childForceExpandWidth = true;
            demoVLG.childForceExpandHeight = false;

            var startBtn = AddButton(demoPanel.transform, "StartLoadingButton", "Start Loading",
                new Vector2(0, 44));
            startBtn.AddComponent<LayoutElement>().preferredHeight = 44;

            var demoStatusText = AddTMPText(demoPanel.transform, "DemoStatusText", "Ready", 18f,
                TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            demoStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            // === Loading Overlay (full screen, dark, initially hidden) ===
            var overlayGO = new GameObject("LoadingOverlay",
                typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            overlayGO.transform.SetParent(canvas.transform, false);
            StretchFill(overlayGO.GetComponent<RectTransform>());
            var overlayImg = overlayGO.GetComponent<Image>();
            overlayImg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
            overlayImg.raycastTarget = true;
            var overlayCanvasGroup = overlayGO.GetComponent<CanvasGroup>();

            var overlayVLG = overlayGO.AddComponent<VerticalLayoutGroup>();
            overlayVLG.spacing = 15f;
            overlayVLG.padding = new RectOffset(60, 60, 100, 60);
            overlayVLG.childAlignment = TextAnchor.MiddleCenter;
            overlayVLG.childControlWidth = true;
            overlayVLG.childControlHeight = true;
            overlayVLG.childForceExpandWidth = true;
            overlayVLG.childForceExpandHeight = false;

            // Spinner (a simple rotating square)
            var spinnerGO = new GameObject("Spinner", typeof(RectTransform), typeof(Image));
            spinnerGO.transform.SetParent(overlayGO.transform, false);
            var spinnerImg = spinnerGO.GetComponent<Image>();
            spinnerImg.color = new Color(0.4f, 0.7f, 1f, 1f);
            spinnerImg.raycastTarget = false;
            var spinnerLE = spinnerGO.AddComponent<LayoutElement>();
            spinnerLE.preferredWidth = 40;
            spinnerLE.preferredHeight = 40;

            // Current task text
            var taskText = AddTMPText(overlayGO.transform, "TaskText", "Loading...", 20f,
                TextAlignmentOptions.Center);
            taskText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            // Progress bar background
            var progressBg = AddPanel(overlayGO.transform, "ProgressBarBG",
                new Color(0.2f, 0.2f, 0.2f, 0.8f));
            var progressBgLE = progressBg.AddComponent<LayoutElement>();
            progressBgLE.preferredHeight = 20;

            // Progress bar fill (inside background)
            var progressFill = AddImage(progressBg.transform, "ProgressFill",
                new Color(0.3f, 0.7f, 1f, 1f), Vector2.zero);
            var progressFillRT = progressFill.rectTransform;
            progressFillRT.anchorMin = Vector2.zero;
            progressFillRT.anchorMax = new Vector2(0, 1);
            progressFillRT.offsetMin = Vector2.zero;
            progressFillRT.offsetMax = Vector2.zero;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;
            // For Filled mode without sprite, use simple stretch instead
            progressFillRT.anchorMax = new Vector2(1, 1);
            StretchFill(progressFillRT);
            progressFill.type = Image.Type.Simple;
            // We'll let the View control fillAmount via DOFillAmount.
            // Use Filled type properly:
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;

            // Percentage text
            var percentText = AddTMPText(overlayGO.transform, "PercentageText", "0%", 24f,
                TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.9f));
            percentText.gameObject.AddComponent<LayoutElement>().preferredHeight = 35;

            // Tips text
            var tipsText = AddTMPText(overlayGO.transform, "TipsText",
                "Tip: Press F1 for help at any time.", 16f,
                TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            tipsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
            tipsText.fontStyle = FontStyles.Italic;

            // Initially hide overlay
            overlayGO.SetActive(false);

            // === LoadingDemoView ===
            var demoViewGO = new GameObject("LoadingDemoView");
            demoViewGO.transform.SetParent(canvas.transform, false);
            var demoView = demoViewGO.AddComponent<LoadingDemoView>();
            var demoViewSO = new SerializedObject(demoView);
            demoViewSO.FindProperty("_startLoadingButton").objectReferenceValue =
                startBtn.GetComponent<Button>();
            demoViewSO.FindProperty("_statusText").objectReferenceValue = demoStatusText;
            demoViewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LoadingScreenView ===
            var loadViewGO = new GameObject("LoadingScreenView");
            loadViewGO.transform.SetParent(canvas.transform, false);
            var loadView = loadViewGO.AddComponent<LoadingScreenView>();
            var loadViewSO = new SerializedObject(loadView);
            loadViewSO.FindProperty("_overlayCanvasGroup").objectReferenceValue = overlayCanvasGroup;
            loadViewSO.FindProperty("_overlayRoot").objectReferenceValue = overlayGO;
            loadViewSO.FindProperty("_progressBarFill").objectReferenceValue = progressFill;
            loadViewSO.FindProperty("_percentageText").objectReferenceValue = percentText;
            loadViewSO.FindProperty("_currentTaskText").objectReferenceValue = taskText;
            loadViewSO.FindProperty("_tipsText").objectReferenceValue = tipsText;
            loadViewSO.FindProperty("_spinnerTransform").objectReferenceValue =
                spinnerGO.GetComponent<RectTransform>();
            loadViewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("LoadingScreenLifetimeScope");
            var scope = scopeGO.AddComponent<LoadingScreenLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_loadingScreenView").objectReferenceValue = loadView;
            scopeSO.FindProperty("_loadingDemoView").objectReferenceValue = demoView;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[10-02] Saved: {scenePath}");
        }

        public static void Build10_03_ResourceHUD()
        {
            var scenePath = "Assets/_Study/10-Advanced-HUD/Scenes/03-ResourceHUD.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddEventSystem();
            var canvas = AddCanvas("Canvas");

            // === Title ===
            var title = AddTMPText(canvas.transform, "Title", "10-03 Resource HUD", 28f,
                TextAlignmentOptions.Center);
            Anchor(title.rectTransform, AnchorPreset.TopStretch,
                new Vector2(0, -10), new Vector2(0, -50));

            // === Top Bar: 5 resource displays in a horizontal row ===
            var topBar = AddHorizontalRow(canvas.transform, "ResourceBar", 130f);
            var topBarRT = Anchor(topBar.GetComponent<RectTransform>(), AnchorPreset.TopStretch,
                new Vector2(10, -55), new Vector2(-10, -195));
            topBar.GetComponent<HorizontalLayoutGroup>().spacing = 8;
            topBar.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            topBar.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
            topBar.GetComponent<HorizontalLayoutGroup>().childControlHeight = true;
            topBar.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            topBar.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            // Resource configs: name, icon, hasBar
            var resources = new[]
            {
                ("Gold", "G", false),
                ("Wood", "W", false),
                ("Stone", "S", false),
                ("Food", "F", false),
                ("Population", "P", true),
            };

            var resourceBars = new ResourceBarView[5];
            for (int i = 0; i < 5; i++)
            {
                var (resName, icon, hasBar) = resources[i];
                var resView = CreateResourceBar(topBar.transform, resName, icon, hasBar);
                resourceBars[i] = resView;
            }

            // === ResourceHUDView ===
            var viewGO = new GameObject("ResourceHUDView");
            viewGO.transform.SetParent(canvas.transform, false);
            var view = viewGO.AddComponent<ResourceHUDView>();
            var viewSO = new SerializedObject(view);
            viewSO.FindProperty("_goldBar").objectReferenceValue = resourceBars[0];
            viewSO.FindProperty("_woodBar").objectReferenceValue = resourceBars[1];
            viewSO.FindProperty("_stoneBar").objectReferenceValue = resourceBars[2];
            viewSO.FindProperty("_foodBar").objectReferenceValue = resourceBars[3];
            viewSO.FindProperty("_populationBar").objectReferenceValue = resourceBars[4];
            viewSO.ApplyModifiedPropertiesWithoutUndo();

            // === LifetimeScope ===
            var scopeGO = new GameObject("ResourceHUDLifetimeScope");
            var scope = scopeGO.AddComponent<ResourceHUDLifetimeScope>();
            var scopeSO = new SerializedObject(scope);
            scopeSO.FindProperty("_resourceHUDView").objectReferenceValue = view;
            scopeSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureDir(scenePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[10-03] Saved: {scenePath}");
        }

        // ================================================================
        //  Private Helpers
        // ================================================================

        /// <summary>
        /// Creates a DropZoneView with background, label, and highlight.
        /// </summary>
        private static DropZoneView CreateDropZone(Transform parent, string name, string label)
        {
            var zoneGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            zoneGO.transform.SetParent(parent, false);
            var zoneBg = zoneGO.GetComponent<Image>();
            zoneBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            zoneGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Label
            var zoneLabel = AddTMPText(zoneGO.transform, "Label", label, 22f,
                TextAlignmentOptions.Center);
            StretchFill(zoneLabel.rectTransform);

            // Highlight overlay (initially hidden)
            var highlight = AddImage(zoneGO.transform, "Highlight",
                new Color(0.3f, 0.5f, 0.3f, 0.4f), Vector2.zero);
            StretchFill(highlight.rectTransform);
            highlight.gameObject.SetActive(false);

            // Add DropZoneView component
            var zone = zoneGO.AddComponent<DropZoneView>();
            var zoneSO = new SerializedObject(zone);
            zoneSO.FindProperty("_background").objectReferenceValue = zoneBg;
            zoneSO.FindProperty("_label").objectReferenceValue = zoneLabel;
            zoneSO.FindProperty("_highlight").objectReferenceValue = highlight;
            zoneSO.ApplyModifiedPropertiesWithoutUndo();

            return zone;
        }

        /// <summary>
        /// Creates a single ResourceBarView with icon, value, optional bar, and +/- buttons.
        /// </summary>
        private static ResourceBarView CreateResourceBar(Transform parent, string name, string icon,
            bool hasBar)
        {
            var rootGO = new GameObject($"Res_{name}", typeof(RectTransform));
            rootGO.transform.SetParent(parent, false);

            var rootVLG = rootGO.AddComponent<VerticalLayoutGroup>();
            rootVLG.spacing = 4f;
            rootVLG.padding = new RectOffset(5, 5, 5, 5);
            rootVLG.childAlignment = TextAnchor.UpperCenter;
            rootVLG.childControlWidth = true;
            rootVLG.childControlHeight = true;
            rootVLG.childForceExpandWidth = true;
            rootVLG.childForceExpandHeight = false;

            // Icon text
            var iconText = AddTMPText(rootGO.transform, "IconText", icon, 24f,
                TextAlignmentOptions.Center, new Color(1f, 0.85f, 0f));
            iconText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            // Value text
            var valueText = AddTMPText(rootGO.transform, "ValueText", "0", 20f,
                TextAlignmentOptions.Center);
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 25;

            // Optional bar (for population)
            GameObject barContainer = null;
            Image barFill = null;
            if (hasBar)
            {
                barContainer = AddPanel(rootGO.transform, "BarContainer",
                    new Color(0.2f, 0.2f, 0.2f, 0.8f));
                var barContainerLE = barContainer.AddComponent<LayoutElement>();
                barContainerLE.preferredHeight = 8;

                barFill = AddImage(barContainer.transform, "BarFill",
                    new Color(0.3f, 0.8f, 0.4f, 1f), Vector2.zero);
                StretchFill(barFill.rectTransform);
                barFill.type = Image.Type.Filled;
                barFill.fillMethod = Image.FillMethod.Horizontal;
                barFill.fillOrigin = 0;
                barFill.fillAmount = 0.5f;
            }

            // +/- buttons row
            var btnRow = AddHorizontalRow(rootGO.transform, "Buttons", 30f);
            btnRow.GetComponent<HorizontalLayoutGroup>().spacing = 4;
            btnRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            var addBtn = AddButton(btnRow.transform, "AddButton", "+", new Vector2(30, 28));
            addBtn.AddComponent<LayoutElement>().flexibleWidth = 1;
            var subBtn = AddButton(btnRow.transform, "SubButton", "-", new Vector2(30, 28));
            subBtn.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Add ResourceBarView component
            var resView = rootGO.AddComponent<ResourceBarView>();
            var resSO = new SerializedObject(resView);
            resSO.FindProperty("_iconText").objectReferenceValue = iconText;
            resSO.FindProperty("_valueText").objectReferenceValue = valueText;
            resSO.FindProperty("_addButton").objectReferenceValue = addBtn.GetComponent<Button>();
            resSO.FindProperty("_subtractButton").objectReferenceValue = subBtn.GetComponent<Button>();

            if (hasBar)
            {
                resSO.FindProperty("_barFill").objectReferenceValue = barFill;
                resSO.FindProperty("_barContainer").objectReferenceValue = barContainer;
            }

            resSO.ApplyModifiedPropertiesWithoutUndo();

            return resView;
        }
    }
}
#endif
