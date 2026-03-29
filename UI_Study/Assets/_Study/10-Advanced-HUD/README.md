# 10-Advanced-HUD

Advanced HUD UI patterns: Radial Menu, Loading Screen, Resource HUD.

## Tech Stack
- MV(R)P + VContainer + R3 + UniTask + DOTween

## Scenes

### 01-RadialMenu
8-item radial menu with circular button layout (Atan2 positioning), DOTween scale open/close animation, selection highlight, and center text display.

- **Model**: `RadialMenuModel` -- SelectedIndex (ReactiveProperty), 8 menu items
- **View**: `RadialMenuView` -- Circle layout, highlight, toggle animation
- **Presenter**: `RadialMenuPresenter` -- Button click -> model -> view binding

### 02-LoadingScreen
Full-screen loading overlay with progress bar, task text, cycling tips, and spinning icon. Fake async loading service with 5 steps.

- **Model**: `LoadingModel` -- Progress, CurrentTask, IsLoading
- **View**: `LoadingScreenView` -- Overlay with fade, progress bar, spinner
- **View**: `LoadingDemoView` -- Start button + status text
- **Service**: `FakeLoadingService` -- 5-step UniTask simulation
- **Presenter**: `LoadingScreenPresenter` -- Wires service -> model -> views

### 03-ResourceHUD
Top bar with 5 resources (Gold, Wood, Stone, Food, Population). Animated counter transitions with DOTween, color flash on change, population bar.

- **Model**: `ResourceHUDModel` -- 5 ReactiveProperty<int> resources
- **View**: `ResourceBarView` -- Single resource: icon + value + optional bar
- **View**: `ResourceHUDView` -- Container for 5 ResourceBarView instances
- **Presenter**: `ResourceHUDPresenter` -- Model binding + +/- button wiring

## Scene Setup Guide

Each scene requires:
1. Canvas (Screen Space - Overlay, CanvasScaler: Scale With Screen Size 1920x1080)
2. EventSystem (with new Input System UI Input Module)
3. LifetimeScope component on a root GameObject

### 01-RadialMenu Scene Setup
1. Create LifetimeScope GO with `RadialMenuLifetimeScope`
2. Create Canvas > "RadialMenu" with `RadialMenuView`
3. Under RadialMenu: "MenuContainer" (RectTransform, center anchor)
4. Under MenuContainer: 8 Buttons (each with Image + child TMP text)
5. Under MenuContainer: "CenterText" (TMP)
6. Outside MenuContainer: "ToggleButton" (Button + TMP child "Open Menu")
7. Wire all references in RadialMenuView inspector

### 02-LoadingScreen Scene Setup
1. Create LifetimeScope GO with `LoadingScreenLifetimeScope`
2. Create Canvas > "DemoPanel" with `LoadingDemoView` (Button + status TMP)
3. Create Canvas > "LoadingOverlay" with `LoadingScreenView`:
   - OverlayRoot: full-screen dark Image + CanvasGroup
   - ProgressBar: Background Image + Foreground Image (Filled, Horizontal)
   - PercentageText, CurrentTaskText, TipsText (TMP)
   - Spinner: TMP text with RectTransform for rotation
4. Wire all references in both view inspectors

### 03-ResourceHUD Scene Setup
1. Create LifetimeScope GO with `ResourceHUDLifetimeScope`
2. Create Canvas > "ResourceHUD" with `ResourceHUDView` + HorizontalLayoutGroup
3. Under ResourceHUD: 5x "ResourceBar" prefab instances with `ResourceBarView`:
   - IconText (TMP), ValueText (TMP)
   - BarContainer (optional, for population): Background Image + Fill Image (Filled)
   - AddButton (+), SubtractButton (-)
4. Wire all references in ResourceHUDView and each ResourceBarView
