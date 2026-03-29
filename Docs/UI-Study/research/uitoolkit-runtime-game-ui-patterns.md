# UI Toolkit 런타임 게임 UI 패턴

- **작성일**: 2026-03-29
- **카테고리**: pattern
- **상태**: 조사완료

---

## 1. 요약

UI Toolkit을 사용한 게임 UI 구현은 UGUI의 GameObject 기반 접근과 근본적으로 다르다. VisualElement 트리와 Flexbox 레이아웃, USS 클래스 상태 전환, ListView 가상화, TabView 같은 내장 컨트롤을 조합하면 기지 경영 게임의 모든 주요 화면(HUD, 건설 메뉴, 유닛 목록, 기술 트리, 설정, 다이얼로그, 저장/불러오기)을 경량 스택(UI Toolkit + UniTask + DOTween + C# events + simple MVP)으로 구현할 수 있다. UGUI 대비 코드량과 성능 모두 유리한 경우가 많으나, 월드 공간 UI와 복잡한 타임라인 애니메이션은 여전히 UGUI가 강세다.

---

## 2. 상세 분석

### 2.1 Resource HUD (Always Visible)

HUD는 게임의 자원 상태(골드, 목재, 식량, 인구)를 항상 화면 상단에 표시하는 영속 패널이다.

#### UXML 구조

```xml
<!-- ResourceHUD.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="hud-root" class="hud-bar">
        <VisualElement name="resource-gold" class="resource-item">
            <VisualElement class="resource-icon resource-icon--gold"/>
            <Label name="gold-label" class="resource-label" text="0"/>
        </VisualElement>
        <VisualElement name="resource-wood" class="resource-item">
            <VisualElement class="resource-icon resource-icon--wood"/>
            <Label name="wood-label" class="resource-label" text="0"/>
        </VisualElement>
        <VisualElement name="resource-food" class="resource-item">
            <VisualElement class="resource-icon resource-icon--food"/>
            <Label name="food-label" class="resource-label" text="0"/>
        </VisualElement>
        <VisualElement name="resource-pop" class="resource-item">
            <VisualElement class="resource-icon resource-icon--pop"/>
            <Label name="pop-label" class="resource-label" text="0/0"/>
        </VisualElement>
    </VisualElement>
</UXML>
```

#### USS 스타일링

```css
/* ResourceHUD.uss */
.hud-bar {
    flex-direction: row;
    justify-content: flex-start;
    align-items: center;
    background-color: rgba(0, 0, 0, 0.75);
    padding: 6px 12px;
    border-bottom-width: 2px;
    border-bottom-color: rgb(80, 60, 30);
    height: 48px;
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
}

.resource-item {
    flex-direction: row;
    align-items: center;
    margin-right: 24px;
}

.resource-icon {
    width: 28px;
    height: 28px;
    margin-right: 6px;
    -unity-background-scale-mode: scale-to-fit;
}

.resource-icon--gold  { background-image: url("/Icons/icon_gold.png"); }
.resource-icon--wood  { background-image: url("/Icons/icon_wood.png"); }
.resource-icon--food  { background-image: url("/Icons/icon_food.png"); }
.resource-icon--pop   { background-image: url("/Icons/icon_pop.png"); }

.resource-label {
    color: rgb(240, 220, 150);
    font-size: 16px;
    -unity-font-style: bold;
}

/* 값 증가 시 반짝임 효과 */
.resource-label--flash {
    color: rgb(255, 255, 100);
    transition-property: color;
    transition-duration: 0.5s;
    transition-timing-function: ease-out;
}
```

#### C# Presenter (경량 MVP)

```csharp
// ResourceHudPresenter.cs
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using System;

public class ResourceHudPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    // 캐시된 레이블 레퍼런스
    private Label _goldLabel;
    private Label _woodLabel;
    private Label _foodLabel;
    private Label _popLabel;

    // 이전 값 (변화 감지용)
    private int _prevGold, _prevWood, _prevFood, _prevPop, _prevMaxPop;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _goldLabel = root.Q<Label>("gold-label");
        _woodLabel = root.Q<Label>("wood-label");
        _foodLabel = root.Q<Label>("food-label");
        _popLabel  = root.Q<Label>("pop-label");
    }

    // 게임 모델에서 호출 (C# event 기반, 반응형 라이브러리 불필요)
    public void UpdateResources(int gold, int wood, int food, int pop, int maxPop)
    {
        if (gold != _prevGold) { SetLabelWithFlash(_goldLabel, gold.ToString()); _prevGold = gold; }
        if (wood != _prevWood) { SetLabelWithFlash(_woodLabel, wood.ToString()); _prevWood = wood; }
        if (food != _prevFood) { SetLabelWithFlash(_foodLabel, food.ToString()); _prevFood = food; }

        string popText = $"{pop}/{maxPop}";
        if (pop != _prevPop || maxPop != _prevMaxPop)
        {
            _popLabel.text = popText;
            // 인구 한계 접근 시 색상 경고
            _popLabel.EnableInClassList("resource-label--warning", pop >= maxPop * 0.9f);
            _prevPop = pop;
            _prevMaxPop = maxPop;
        }
    }

    private void SetLabelWithFlash(Label label, string value)
    {
        label.text = value;
        FlashLabelAsync(label).Forget();
    }

    // DOTween 없이 USS transition만으로 반짝임 구현
    private async UniTaskVoid FlashLabelAsync(Label label)
    {
        label.AddToClassList("resource-label--flash");
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        label.RemoveFromClassList("resource-label--flash");
    }
}
```

#### UGUI 대비 비교

| 항목 | UGUI 방식 | UI Toolkit 방식 |
|------|-----------|----------------|
| 레이아웃 | HorizontalLayoutGroup + LayoutElement | flex-direction: row (USS) |
| 텍스트 | TextMeshProUGUI | Label |
| 아이콘 | Image + Sprite | VisualElement + background-image USS |
| 반짝임 | DOTween color animation | USS transition (color 속성) |
| 값 업데이트 | `_goldText.text = value.ToString()` | `_goldLabel.text = value` (동일) |
| Canvas Rebuild | 값 변경 시 Canvas dirty 위험 | 개별 요소만 dirty, 부모 영향 없음 |

---

### 2.2 Building Construction Menu

건물 건설 메뉴는 격자 형태의 건물 카드 목록으로, 아이콘·이름·비용·잠금 상태·호버 툴팁을 포함한다.

#### 그리드 레이아웃 (CSS Grid 없이 Flexbox wrap)

UI Toolkit은 CSS Grid를 지원하지 않으므로 `flex-wrap: wrap` + 고정 너비 카드로 그리드를 근사한다.

```css
/* BuildMenu.uss */
.build-menu {
    flex-direction: column;
    width: 380px;
    background-color: rgba(20, 15, 10, 0.92);
    border-radius: 8px;
    padding: 12px;
}

.build-grid {
    flex-direction: row;
    flex-wrap: wrap;          /* 핵심: wrap으로 자동 줄바꿈 그리드 효과 */
    justify-content: flex-start;
}

.building-card {
    width: 100px;
    height: 110px;
    margin: 4px;
    flex-direction: column;
    align-items: center;
    background-color: rgb(45, 35, 20);
    border-radius: 6px;
    border-width: 1px;
    border-color: rgb(80, 65, 40);
    padding: 6px;
    transition-property: background-color, border-color;
    transition-duration: 0.15s;
}

.building-card:hover {
    background-color: rgb(65, 50, 28);
    border-color: rgb(180, 140, 60);
}

.building-card--locked {
    opacity: 0.4;
}

.building-card--affordable {
    border-color: rgb(100, 200, 80);
}

.building-card--too-expensive {
    border-color: rgb(200, 60, 60);
}

.building-icon {
    width: 56px;
    height: 56px;
    -unity-background-scale-mode: scale-to-fit;
}

.building-name {
    font-size: 11px;
    color: rgb(220, 200, 160);
    margin-top: 4px;
    -unity-text-align: middle-center;
}

.building-cost {
    font-size: 10px;
    color: rgb(200, 180, 100);
    -unity-text-align: middle-center;
}
```

#### 건물 카드 동적 생성

```csharp
// BuildMenuPresenter.cs
public class BuildMenuPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VisualTreeAsset _cardTemplate;

    private VisualElement _buildGrid;
    private VisualElement _tooltip;
    private Label _tooltipName, _tooltipDesc, _tooltipCost;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _buildGrid   = root.Q<VisualElement>("build-grid");
        _tooltip     = root.Q<VisualElement>("tooltip-panel");
        _tooltipName = root.Q<Label>("tooltip-name");
        _tooltipDesc = root.Q<Label>("tooltip-desc");
        _tooltipCost = root.Q<Label>("tooltip-cost");

        // 툴팁 초기 숨김
        _tooltip.style.display = DisplayStyle.None;
    }

    public void PopulateBuildings(IReadOnlyList<BuildingData> buildings, ResourceState resources)
    {
        _buildGrid.Clear();

        foreach (var bld in buildings)
        {
            var card = _cardTemplate.Instantiate();
            var cardRoot = card.Q<VisualElement>("card-root");

            card.Q<VisualElement>("building-icon").style.backgroundImage
                = new StyleBackground(bld.Icon);
            card.Q<Label>("building-name").text = bld.DisplayName;
            card.Q<Label>("building-cost").text = $"G:{bld.GoldCost} W:{bld.WoodCost}";

            bool isLocked = !bld.IsUnlocked;
            bool canAfford = resources.CanAfford(bld.GoldCost, bld.WoodCost);

            cardRoot.EnableInClassList("building-card--locked", isLocked);
            cardRoot.EnableInClassList("building-card--affordable", !isLocked && canAfford);
            cardRoot.EnableInClassList("building-card--too-expensive", !isLocked && !canAfford);

            // 호버 툴팁 (USS :hover로 하이라이트, C#으로 툴팁 내용 제어)
            var capturedBld = bld; // 람다 캡처
            cardRoot.RegisterCallback<MouseEnterEvent>(_ => ShowTooltip(capturedBld));
            cardRoot.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());

            if (!isLocked && canAfford)
                cardRoot.RegisterCallback<ClickEvent>(_ => OnBuildingSelected(capturedBld));

            _buildGrid.Add(card);
        }
    }

    private void ShowTooltip(BuildingData bld)
    {
        _tooltipName.text = bld.DisplayName;
        _tooltipDesc.text = bld.Description;
        _tooltipCost.text = $"골드: {bld.GoldCost}  목재: {bld.WoodCost}";
        _tooltip.style.display = DisplayStyle.Flex;
    }

    private void HideTooltip()
    {
        _tooltip.style.display = DisplayStyle.None;
    }

    private void OnBuildingSelected(BuildingData bld)
    {
        BuildingSelected?.Invoke(bld);
    }

    public event Action<BuildingData> BuildingSelected;
}
```

#### UGUI 대비 비교

| 항목 | UGUI (GridLayoutGroup) | UI Toolkit |
|------|------------------------|-----------|
| 그리드 구현 | GridLayoutGroup 컴포넌트 | flex-wrap: wrap (USS) |
| 셀 크기 고정 | cellSize 프로퍼티 | 카드에 width/height 고정 |
| 호버 효과 | EventTrigger + DOTween | USS :hover 가상 클래스 |
| 잠금 표시 | CanvasGroup.alpha | opacity + --locked CSS 클래스 |
| 툴팁 위치 | RectTransform 절대 배치 | position: absolute + top/left |
| 동적 활성화 | SetActive(true/false) | DisplayStyle.Flex/None |

---

### 2.3 Unit Management List

유닛 목록은 대량 데이터(수십~수백 유닛)를 정렬·필터링할 수 있는 스크롤 가능 목록이다. ListView의 가상화가 핵심이다.

#### UXML 구조

```xml
<!-- UnitList.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="unit-list-panel" class="panel">
        <!-- 헤더: 필터 + 정렬 -->
        <VisualElement class="list-toolbar">
            <TextField name="search-field" class="search-field" placeholder="유닛 검색..."/>
            <VisualElement class="sort-buttons">
                <Button name="sort-name"    text="이름" class="sort-btn"/>
                <Button name="sort-level"   text="레벨" class="sort-btn"/>
                <Button name="sort-hp"      text="HP"   class="sort-btn"/>
                <Button name="sort-attack"  text="공격" class="sort-btn"/>
            </VisualElement>
        </VisualElement>

        <!-- 가상화 ListView -->
        <ListView name="unit-listview" class="unit-list"
                  fixed-item-height="64"
                  selection-type="Single"
                  show-alternating-row-backgrounds="ContentOnly"/>

        <!-- 선택 유닛 상세 -->
        <VisualElement name="unit-detail" class="unit-detail"/>
    </VisualElement>
</UXML>
```

#### ListView 가상화 구현

```csharp
// UnitListPresenter.cs
public class UnitListPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VisualTreeAsset _unitRowTemplate;

    private ListView _listView;
    private TextField _searchField;

    private List<UnitData> _allUnits = new();
    private List<UnitData> _filteredUnits = new();
    private string _currentSort = "name";
    private bool _sortAscending = true;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _listView   = root.Q<ListView>("unit-listview");
        _searchField = root.Q<TextField>("search-field");

        // ListView 가상화 설정
        _listView.itemsSource   = _filteredUnits;
        _listView.fixedItemHeight = 64f;

        // makeItem: 재사용 VisualElement 생성 (풀링 역할)
        _listView.makeItem = () =>
        {
            var row = _unitRowTemplate.Instantiate();
            return row;
        };

        // bindItem: 데이터를 재사용 요소에 바인딩 (매 스크롤마다 호출)
        _listView.bindItem = (element, index) =>
        {
            if (index >= _filteredUnits.Count) return;
            var unit = _filteredUnits[index];

            element.Q<Label>("unit-name").text  = unit.Name;
            element.Q<Label>("unit-level").text = $"Lv.{unit.Level}";
            element.Q<Label>("unit-hp").text    = $"HP: {unit.CurrentHp}/{unit.MaxHp}";
            element.Q<Label>("unit-attack").text = $"공격: {unit.Attack}";

            var portrait = element.Q<VisualElement>("unit-portrait");
            portrait.style.backgroundImage = new StyleBackground(unit.Portrait);

            // 부상 상태 표시
            element.EnableInClassList("unit-row--injured", unit.CurrentHp < unit.MaxHp * 0.5f);
        };

        // unbindItem: 재활용 전 클린업 (이벤트 등록 없으면 생략 가능)
        _listView.unbindItem = (element, index) =>
        {
            element.RemoveFromClassList("unit-row--injured");
        };

        // 선택 이벤트
        _listView.selectionChanged += OnUnitSelected;

        // 검색 필터
        _searchField.RegisterValueChangedCallback(evt => FilterAndRefresh(evt.newValue));

        // 정렬 버튼
        root.Q<Button>("sort-name").clicked   += () => SetSort("name");
        root.Q<Button>("sort-level").clicked  += () => SetSort("level");
        root.Q<Button>("sort-hp").clicked     += () => SetSort("hp");
        root.Q<Button>("sort-attack").clicked += () => SetSort("attack");
    }

    private void OnDisable()
    {
        _listView.selectionChanged -= OnUnitSelected;
    }

    public void LoadUnits(IEnumerable<UnitData> units)
    {
        _allUnits = new List<UnitData>(units);
        FilterAndRefresh(_searchField?.value ?? "");
    }

    private void FilterAndRefresh(string query)
    {
        _filteredUnits.Clear();

        if (string.IsNullOrWhiteSpace(query))
            _filteredUnits.AddRange(_allUnits);
        else
        {
            string lower = query.ToLowerInvariant();
            foreach (var u in _allUnits)
                if (u.Name.ToLowerInvariant().Contains(lower))
                    _filteredUnits.Add(u);
        }

        ApplySort();
        // RefreshItems()는 현재 표시 요소만 갱신 (더 빠름)
        // Rebuild()는 전체 재구성 (itemsSource 타입 변경 시 필요)
        _listView.RefreshItems();
    }

    private void SetSort(string key)
    {
        if (_currentSort == key)
            _sortAscending = !_sortAscending; // 같은 키 재클릭 시 방향 토글
        else
        {
            _currentSort   = key;
            _sortAscending = true;
        }
        ApplySort();
        _listView.RefreshItems();
    }

    private void ApplySort()
    {
        _filteredUnits.Sort((a, b) =>
        {
            int cmp = _currentSort switch
            {
                "name"   => string.Compare(a.Name, b.Name, StringComparison.Ordinal),
                "level"  => a.Level.CompareTo(b.Level),
                "hp"     => a.CurrentHp.CompareTo(b.CurrentHp),
                "attack" => a.Attack.CompareTo(b.Attack),
                _        => 0
            };
            return _sortAscending ? cmp : -cmp;
        });
    }

    private void OnUnitSelected(IEnumerable<object> selected)
    {
        var unit = selected.FirstOrDefault() as UnitData;
        if (unit != null) UnitSelected?.Invoke(unit);
    }

    public event Action<UnitData> UnitSelected;
}
```

#### 핵심 ListView API 정리

| API | 역할 | 주의사항 |
|-----|------|---------|
| `makeItem` | 재사용 VisualElement 생성 | 초기화만, 데이터 바인딩 금지 |
| `bindItem(element, index)` | 인덱스의 데이터를 요소에 적용 | 스크롤마다 반복 호출됨 |
| `unbindItem(element, index)` | 재활용 전 클린업 | 이벤트 구독했다면 반드시 해제 |
| `destroyItem(element)` | 풀에서 제거될 때 호출 | 외부 리소스 해제 용도 |
| `fixedItemHeight` | 고정 행 높이 (px) | 가상화 필수 조건 |
| `virtualizationMethod` | FixedHeight 또는 DynamicHeight | 동적 높이는 성능 비용 증가 |
| `RefreshItems()` | 보이는 항목만 rebind | 필터/정렬 후 사용 |
| `Rebuild()` | 전체 재구성 | itemsSource 타입 변경 시만 |
| `selectionChanged` | 선택 변경 이벤트 | `IEnumerable<object>` 반환 |

#### UGUI 대비 비교

| 항목 | UGUI (ScrollRect + 수동 풀링) | UI Toolkit (ListView) |
|------|-------------------------------|----------------------|
| 풀링 | 수동 ObjectPool 구현 필요 | makeItem/bindItem으로 자동화 |
| 정렬 | 데이터 재정렬 후 전체 재생성 | 동일하나 RefreshItems()가 효율적 |
| 필터 | 전체 재생성 or SetActive | 동일하나 내부 풀 재활용 |
| 선택 | EventTrigger 또는 Toggle | 내장 selectionType + selectionChanged |
| 행 높이 | 수동 RectTransform 계산 | fixedItemHeight 설정으로 자동 |
| 코드량 | 풀 관리 ~100줄 추가 | makeItem/bindItem ~20줄 |

---

### 2.4 Settings Screen

설정 화면은 그래픽·오디오·조작 세 카테고리를 TabView로 구분하고, 각종 입력 컨트롤로 설정값을 조작한다.

#### UXML 구조

```xml
<!-- Settings.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="settings-root" class="settings-panel">
        <Label class="panel-title" text="설정"/>

        <TabView name="settings-tabs">
            <Tab label="그래픽" name="tab-graphics">
                <VisualElement class="settings-section">
                    <Label class="section-title" text="화면 품질"/>
                    <VisualElement class="setting-row">
                        <Label text="품질 레벨" class="setting-label"/>
                        <DropdownField name="quality-dropdown" class="setting-control"/>
                    </VisualElement>
                    <VisualElement class="setting-row">
                        <Label text="VSync" class="setting-label"/>
                        <Toggle name="vsync-toggle" class="setting-control"/>
                    </VisualElement>
                    <VisualElement class="setting-row">
                        <Label text="전체화면" class="setting-label"/>
                        <Toggle name="fullscreen-toggle" class="setting-control"/>
                    </VisualElement>
                </VisualElement>
            </Tab>

            <Tab label="오디오" name="tab-audio">
                <VisualElement class="settings-section">
                    <VisualElement class="setting-row">
                        <Label text="마스터 볼륨" class="setting-label"/>
                        <Slider name="master-volume" class="setting-slider"
                                low-value="0" high-value="1" value="1"/>
                        <Label name="master-volume-label" class="setting-value-label" text="100%"/>
                    </VisualElement>
                    <VisualElement class="setting-row">
                        <Label text="BGM 볼륨" class="setting-label"/>
                        <Slider name="bgm-volume" class="setting-slider"
                                low-value="0" high-value="1" value="0.8"/>
                        <Label name="bgm-volume-label" class="setting-value-label" text="80%"/>
                    </VisualElement>
                    <VisualElement class="setting-row">
                        <Label text="효과음" class="setting-label"/>
                        <Slider name="sfx-volume" class="setting-slider"
                                low-value="0" high-value="1" value="1"/>
                        <Label name="sfx-volume-label" class="setting-value-label" text="100%"/>
                    </VisualElement>
                </VisualElement>
            </Tab>

            <Tab label="조작" name="tab-controls">
                <VisualElement class="settings-section">
                    <VisualElement class="setting-row">
                        <Label text="카메라 감도" class="setting-label"/>
                        <Slider name="cam-sensitivity" class="setting-slider"
                                low-value="0.1" high-value="3.0" value="1.0"/>
                    </VisualElement>
                    <VisualElement class="setting-row">
                        <Label text="스크롤 반전" class="setting-label"/>
                        <Toggle name="invert-scroll"/>
                    </VisualElement>
                </VisualElement>
            </Tab>
        </TabView>

        <VisualElement class="settings-footer">
            <Button name="btn-apply"  text="적용" class="btn-primary"/>
            <Button name="btn-cancel" text="취소" class="btn-secondary"/>
            <Button name="btn-reset"  text="기본값 복원" class="btn-danger"/>
        </VisualElement>
    </VisualElement>
</UXML>
```

#### C# 설정 Presenter

```csharp
// SettingsPresenter.cs
public class SettingsPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    // 오디오 슬라이더
    private Slider _masterVolume, _bgmVolume, _sfxVolume;
    private Label _masterLabel, _bgmLabel, _sfxLabel;

    // 그래픽
    private DropdownField _qualityDropdown;
    private Toggle _vsyncToggle, _fullscreenToggle;

    // 조작
    private Slider _camSensitivity;
    private Toggle _invertScroll;

    // 변경 전 설정값 (취소용 스냅샷)
    private SettingsData _snapshot;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _masterVolume = root.Q<Slider>("master-volume");
        _bgmVolume    = root.Q<Slider>("bgm-volume");
        _sfxVolume    = root.Q<Slider>("sfx-volume");
        _masterLabel  = root.Q<Label>("master-volume-label");
        _bgmLabel     = root.Q<Label>("bgm-volume-label");
        _sfxLabel     = root.Q<Label>("sfx-volume-label");

        _qualityDropdown   = root.Q<DropdownField>("quality-dropdown");
        _vsyncToggle       = root.Q<Toggle>("vsync-toggle");
        _fullscreenToggle  = root.Q<Toggle>("fullscreen-toggle");
        _camSensitivity    = root.Q<Slider>("cam-sensitivity");
        _invertScroll      = root.Q<Toggle>("invert-scroll");

        // 드롭다운 옵션 설정
        _qualityDropdown.choices = new List<string>(QualitySettings.names);

        // 현재 값 로드
        LoadCurrentSettings();

        // 슬라이더 값 변경 → 레이블 실시간 업데이트
        _masterVolume.RegisterValueChangedCallback(e =>
            _masterLabel.text = $"{Mathf.RoundToInt(e.newValue * 100)}%");
        _bgmVolume.RegisterValueChangedCallback(e =>
            _bgmLabel.text = $"{Mathf.RoundToInt(e.newValue * 100)}%");
        _sfxVolume.RegisterValueChangedCallback(e =>
            _sfxLabel.text = $"{Mathf.RoundToInt(e.newValue * 100)}%");

        // 푸터 버튼
        root.Q<Button>("btn-apply").clicked  += ApplySettings;
        root.Q<Button>("btn-cancel").clicked += CancelSettings;
        root.Q<Button>("btn-reset").clicked  += ResetToDefaults;
    }

    private void LoadCurrentSettings()
    {
        _snapshot = SettingsData.LoadFromPlayerPrefs();

        _masterVolume.SetValueWithoutNotify(_snapshot.MasterVolume);
        _bgmVolume.SetValueWithoutNotify(_snapshot.BgmVolume);
        _sfxVolume.SetValueWithoutNotify(_snapshot.SfxVolume);
        _qualityDropdown.SetValueWithoutNotify(
            QualitySettings.names[_snapshot.QualityLevel]);
        _vsyncToggle.SetValueWithoutNotify(_snapshot.VSync);
        _fullscreenToggle.SetValueWithoutNotify(_snapshot.Fullscreen);
        _camSensitivity.SetValueWithoutNotify(_snapshot.CamSensitivity);
        _invertScroll.SetValueWithoutNotify(_snapshot.InvertScroll);

        // 레이블 초기화
        _masterLabel.text = $"{Mathf.RoundToInt(_snapshot.MasterVolume * 100)}%";
        _bgmLabel.text    = $"{Mathf.RoundToInt(_snapshot.BgmVolume * 100)}%";
        _sfxLabel.text    = $"{Mathf.RoundToInt(_snapshot.SfxVolume * 100)}%";
    }

    private void ApplySettings()
    {
        var data = new SettingsData
        {
            MasterVolume   = _masterVolume.value,
            BgmVolume      = _bgmVolume.value,
            SfxVolume      = _sfxVolume.value,
            QualityLevel   = _qualityDropdown.index,
            VSync          = _vsyncToggle.value,
            Fullscreen     = _fullscreenToggle.value,
            CamSensitivity = _camSensitivity.value,
            InvertScroll   = _invertScroll.value,
        };

        data.Apply();           // QualitySettings, AudioMixer 등에 반영
        data.SaveToPlayerPrefs();
        _snapshot = data;       // 새 스냅샷 저장
    }

    private void CancelSettings()
    {
        // 스냅샷으로 UI 복원 (SetValueWithoutNotify = 이벤트 발생 없이 설정)
        _masterVolume.SetValueWithoutNotify(_snapshot.MasterVolume);
        _bgmVolume.SetValueWithoutNotify(_snapshot.BgmVolume);
        // ... 나머지 동일
        SettingsClosed?.Invoke();
    }

    private void ResetToDefaults()
    {
        var defaults = SettingsData.Defaults();
        _masterVolume.value = defaults.MasterVolume;
        _bgmVolume.value    = defaults.BgmVolume;
        // ... 나머지
    }

    public event Action SettingsClosed;
}
```

#### 핵심 컨트롤 요약

| 컨트롤 | 값 속성 | 변경 이벤트 | 특징 |
|--------|---------|------------|------|
| `Slider` | `value` (float) | `RegisterValueChangedCallback` | low/highValue로 범위 설정 |
| `SliderInt` | `value` (int) | `RegisterValueChangedCallback` | 정수 전용 |
| `Toggle` | `value` (bool) | `RegisterValueChangedCallback` | 체크박스 |
| `DropdownField` | `value` (string) | `RegisterValueChangedCallback` | `choices` List 필수 |
| `RadioButtonGroup` | `value` (int) | `RegisterValueChangedCallback` | 단일 선택 |
| `ToggleButtonGroup` | `value` (ToggleButtonGroupState) | `RegisterValueChangedCallback` | Unity 6 신규 |
| `TabView` | `selectedTabIndex` (int) | `tab.selected` 이벤트 | Unity 6 신규 |

---

### 2.5 Dialog/Popup System

다이얼로그는 배경 입력을 차단하는 모달 오버레이로 구현한다. UI Toolkit에서는 별도 PanelSettings(높은 Sort Order)를 사용하거나 동일 문서 내 최상위 레이어에 배치한다.

#### 모달 오버레이 패턴

```xml
<!-- DialogOverlay.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <!-- 반투명 배경: pickingMode=Position으로 클릭 차단 -->
    <VisualElement name="modal-backdrop" class="modal-backdrop">
        <!-- 중앙 패널 -->
        <VisualElement name="dialog-panel" class="dialog-panel">
            <Label name="dialog-title" class="dialog-title"/>
            <Label name="dialog-message" class="dialog-message"/>
            <VisualElement class="dialog-buttons">
                <Button name="btn-confirm" class="btn-primary"/>
                <Button name="btn-cancel"  class="btn-secondary"/>
            </VisualElement>
        </VisualElement>
    </VisualElement>
</UXML>
```

```css
/* Dialog.uss */
.modal-backdrop {
    position: absolute;
    left: 0; top: 0; right: 0; bottom: 0;
    background-color: rgba(0, 0, 0, 0.6);
    /* Flexbox로 자식(dialog-panel)을 중앙 배치 */
    align-items: center;
    justify-content: center;
    /* 초기 opacity 0으로 시작 (페이드인 애니메이션용) */
    opacity: 0;
    transition-property: opacity;
    transition-duration: 0.25s;
}

.modal-backdrop--visible {
    opacity: 1;
}

.dialog-panel {
    background-color: rgb(30, 25, 15);
    border-radius: 10px;
    border-width: 2px;
    border-color: rgb(100, 80, 40);
    padding: 24px;
    min-width: 300px;
    max-width: 500px;
    /* 패널 자체도 scale 애니메이션 */
    scale: 0.8 0.8;
    transition-property: scale;
    transition-duration: 0.25s;
    transition-timing-function: ease-out;
}

.modal-backdrop--visible .dialog-panel {
    scale: 1 1;
}

.dialog-buttons {
    flex-direction: row;
    justify-content: flex-end;
    margin-top: 16px;
}
```

#### 입력 차단 메커니즘

UI Toolkit에서 배경 입력 차단은 두 가지 방법으로 구현한다:

1. **pickingMode = Position** (기본값): 반투명 backdrop이 전체 화면을 덮으면 그 뒤의 요소는 클릭 이벤트를 받지 못한다. backdrop의 크기가 화면 전체이고 position: absolute + left/top/right/bottom: 0으로 설정되어 있으면 자동으로 차단된다.

2. **별도 PanelSettings (Sort Order 높음)**: 팝업 전용 UIDocument를 높은 Sort Order의 PanelSettings에 연결하면 별도 패널이 상위에서 이벤트를 먼저 수신한다.

```csharp
// DialogPresenter.cs
public class DialogPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private VisualElement _backdrop;
    private VisualElement _panel;
    private Label _title, _message;
    private Button _confirmBtn, _cancelBtn;

    private Action _onConfirm;
    private Action _onCancel;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _backdrop   = root.Q<VisualElement>("modal-backdrop");
        _panel      = root.Q<VisualElement>("dialog-panel");
        _title      = root.Q<Label>("dialog-title");
        _message    = root.Q<Label>("dialog-message");
        _confirmBtn = root.Q<Button>("btn-confirm");
        _cancelBtn  = root.Q<Button>("btn-cancel");

        _confirmBtn.clicked += OnConfirm;
        _cancelBtn.clicked  += OnCancel;

        // 초기 숨김
        _backdrop.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        _confirmBtn.clicked -= OnConfirm;
        _cancelBtn.clicked  -= OnCancel;
    }

    public void ShowConfirm(string title, string message,
                             string confirmText, string cancelText,
                             Action onConfirm, Action onCancel = null)
    {
        _title.text         = title;
        _message.text       = message;
        _confirmBtn.text    = confirmText;
        _cancelBtn.text     = cancelText;
        _cancelBtn.style.display = cancelText != null ? DisplayStyle.Flex : DisplayStyle.None;

        _onConfirm = onConfirm;
        _onCancel  = onCancel;

        _backdrop.style.display = DisplayStyle.Flex;
        // USS transition으로 페이드인 + 스케일 애니메이션
        // display 변경 후 한 프레임 대기 필요 (즉시 클래스 추가 시 transition 미발동)
        _backdrop.schedule.Execute(() =>
            _backdrop.AddToClassList("modal-backdrop--visible"));
    }

    public void ShowInfo(string title, string message)
    {
        ShowConfirm(title, message, "확인", null, null);
    }

    private async void OnConfirm()
    {
        await HideAsync();
        _onConfirm?.Invoke();
    }

    private async void OnCancel()
    {
        await HideAsync();
        _onCancel?.Invoke();
    }

    private async UniTask HideAsync()
    {
        _backdrop.RemoveFromClassList("modal-backdrop--visible");
        // CSS transition 완료 대기 (250ms)
        await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
        _backdrop.style.display = DisplayStyle.None;
    }
}
```

#### 중요 패턴: display 변경 후 transition 트리거

`DisplayStyle.None`에서 `Flex`로 전환 직후 즉시 CSS 클래스를 추가하면 transition이 발동하지 않는다. 요소가 레이아웃에 실제로 참여하기까지 한 프레임이 필요하기 때문이다. `schedule.Execute()`로 한 프레임 지연 후 클래스를 추가해야 한다.

---

### 2.6 Research/Tech Tree

기술 트리는 노드(기술)와 연결선(선행 조건)으로 구성된 2D 그래프 UI다. UI Toolkit의 절대 위치 배치와 `generateVisualContent` API를 조합한다.

#### 노드 절대 위치 배치

```xml
<!-- TechTree.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <ScrollView name="tech-scroll" class="tech-scroll-view">
        <!-- 전체 트리 컨테이너 (ScrollView 내 고정 크기 캔버스) -->
        <VisualElement name="tech-canvas" class="tech-canvas">
            <!-- 연결선 레이어 (노드보다 먼저 → 뒤에 렌더됨) -->
            <VisualElement name="connection-layer" class="connection-layer"/>
            <!-- 노드 레이어 -->
            <VisualElement name="node-layer" class="node-layer"/>
        </VisualElement>
    </ScrollView>
</UXML>
```

```css
.tech-scroll-view {
    flex-grow: 1;
}

.tech-canvas {
    width: 1600px;    /* 트리 전체 너비 */
    height: 900px;    /* 트리 전체 높이 */
    position: relative;
}

.connection-layer {
    position: absolute;
    left: 0; top: 0; right: 0; bottom: 0;
}

.tech-node {
    position: absolute;   /* 절대 위치로 트리 노드 배치 */
    width: 100px;
    height: 80px;
}

.tech-node--locked    { opacity: 0.4; border-color: rgb(80, 80, 80); }
.tech-node--available { border-color: rgb(200, 160, 60); }
.tech-node--completed { border-color: rgb(60, 200, 80); background-color: rgba(60, 200, 80, 0.15); }
```

#### 연결선 그리기 (generateVisualContent + Painter2D)

```csharp
// TechTreePresenter.cs
public class TechTreePresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VisualTreeAsset _nodeTemplate;

    private VisualElement _connectionLayer;
    private VisualElement _nodeLayer;

    // 노드 위치 맵 (기술 ID → 화면 위치)
    private Dictionary<string, Rect> _nodeRects = new();

    // 연결 목록 (from → to)
    private List<(string From, string To)> _connections = new();

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _connectionLayer = root.Q<VisualElement>("connection-layer");
        _nodeLayer       = root.Q<VisualElement>("node-layer");

        // generateVisualContent로 연결선 그리기
        _connectionLayer.generateVisualContent += DrawConnections;
    }

    private void OnDisable()
    {
        if (_connectionLayer != null)
            _connectionLayer.generateVisualContent -= DrawConnections;
    }

    public void BuildTree(IReadOnlyList<TechNode> techs)
    {
        _nodeLayer.Clear();
        _nodeRects.Clear();
        _connections.Clear();

        foreach (var tech in techs)
        {
            var node = _nodeTemplate.Instantiate();
            node.Q<Label>("tech-name").text = tech.DisplayName;
            node.Q<Label>("tech-cost").text = $"{tech.Cost}RP";

            // 절대 위치 배치
            node.style.position = Position.Absolute;
            node.style.left     = tech.TreePosition.x;
            node.style.top      = tech.TreePosition.y;

            // 상태 클래스
            string stateClass = tech.State switch
            {
                TechState.Locked     => "tech-node--locked",
                TechState.Available  => "tech-node--available",
                TechState.Completed  => "tech-node--completed",
                _                   => ""
            };
            node.AddToClassList(stateClass);

            var capturedTech = tech;
            if (tech.State == TechState.Available)
                node.RegisterCallback<ClickEvent>(_ => OnTechClicked(capturedTech));

            // 노드 Rect 기록 (연결선 좌표 계산용)
            _nodeRects[tech.Id] = new Rect(
                tech.TreePosition.x, tech.TreePosition.y, 100f, 80f);

            _nodeLayer.Add(node);

            // 선행 조건 연결 등록
            foreach (var prereqId in tech.Prerequisites)
                _connections.Add((prereqId, tech.Id));
        }

        // 연결선 레이어 강제 재그리기
        _connectionLayer.MarkDirtyRepaint();
    }

    private void DrawConnections(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;

        foreach (var (fromId, toId) in _connections)
        {
            if (!_nodeRects.TryGetValue(fromId, out var fromRect)) continue;
            if (!_nodeRects.TryGetValue(toId,   out var toRect))   continue;

            // 노드 오른쪽 중심 → 다음 노드 왼쪽 중심으로 선
            var start = new Vector2(fromRect.xMax, fromRect.center.y);
            var end   = new Vector2(toRect.xMin,  toRect.center.y);

            // 베지어 곡선으로 부드러운 연결선
            var cp1 = new Vector2(start.x + 40f, start.y);
            var cp2 = new Vector2(end.x - 40f,   end.y);

            p.strokeColor = new Color(0.5f, 0.4f, 0.2f, 0.8f);
            p.lineWidth   = 2f;
            p.BeginPath();
            p.MoveTo(start);
            p.BezierCurveTo(cp1, cp2, end);
            p.Stroke();
        }
    }

    private void OnTechClicked(TechNode tech)
    {
        TechSelected?.Invoke(tech);
    }

    public event Action<TechNode> TechSelected;
}
```

#### UGUI 대비 비교

| 항목 | UGUI | UI Toolkit |
|------|------|-----------|
| 노드 배치 | RectTransform.anchoredPosition | style.left, style.top (position: absolute) |
| 연결선 | LineRenderer (3D) 또는 UILineRenderer 플러그인 | Painter2D (generateVisualContent) |
| 스크롤 | ScrollRect | ScrollView (내장) |
| 노드 클릭 | Button.onClick | RegisterCallback<ClickEvent> |
| 배경 그리드 | Texture + RawImage | generateVisualContent 또는 USS background-image |

---

### 2.7 Save/Load Screen

저장/불러오기 화면은 저장 슬롯 목록을 ListView로 구현하고, 썸네일·메타데이터·새 저장·불러오기·삭제 기능을 제공한다.

#### 구현 패턴

```csharp
// SaveLoadPresenter.cs
public class SaveLoadPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VisualTreeAsset _slotTemplate;
    [SerializeField] private DialogPresenter _dialog;

    private ListView _slotList;
    private Button _btnLoad, _btnDelete, _btnNewSave;
    private List<SaveSlotData> _slots = new();

    private SaveSlotData _selectedSlot;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _slotList  = root.Q<ListView>("save-slot-list");
        _btnLoad   = root.Q<Button>("btn-load");
        _btnDelete = root.Q<Button>("btn-delete");
        _btnNewSave = root.Q<Button>("btn-new-save");

        _slotList.itemsSource   = _slots;
        _slotList.fixedItemHeight = 80f;
        _slotList.makeItem = () => _slotTemplate.Instantiate();
        _slotList.bindItem = BindSlotItem;

        _slotList.selectionChanged += OnSlotSelected;

        _btnLoad.clicked    += OnLoadClicked;
        _btnDelete.clicked  += OnDeleteClicked;
        _btnNewSave.clicked += OnNewSaveClicked;

        // 초기 버튼 상태
        _btnLoad.SetEnabled(false);
        _btnDelete.SetEnabled(false);

        LoadSlots();
    }

    private void BindSlotItem(VisualElement element, int index)
    {
        var slot = _slots[index];

        element.Q<Label>("slot-name").text    = slot.DisplayName;
        element.Q<Label>("slot-date").text    = slot.SaveTime.ToString("yyyy-MM-dd HH:mm");
        element.Q<Label>("slot-playtime").text = slot.FormattedPlayTime;

        var thumb = element.Q<VisualElement>("slot-thumbnail");
        if (slot.Thumbnail != null)
            thumb.style.backgroundImage = new StyleBackground(slot.Thumbnail);

        element.EnableInClassList("slot--empty", slot.IsEmpty);
    }

    private void OnSlotSelected(IEnumerable<object> selection)
    {
        _selectedSlot = selection.FirstOrDefault() as SaveSlotData;
        bool hasSelection = _selectedSlot != null && !_selectedSlot.IsEmpty;
        _btnLoad.SetEnabled(hasSelection);
        _btnDelete.SetEnabled(hasSelection);
    }

    private void OnDeleteClicked()
    {
        if (_selectedSlot == null) return;
        _dialog.ShowConfirm(
            "저장 삭제",
            $"'{_selectedSlot.DisplayName}'를 삭제하시겠습니까?",
            "삭제", "취소",
            onConfirm: () =>
            {
                SaveSystem.Delete(_selectedSlot.SlotIndex);
                LoadSlots();
            }
        );
    }

    private void LoadSlots()
    {
        _slots.Clear();
        _slots.AddRange(SaveSystem.GetAllSlots());
        _slotList.RefreshItems();
    }

    private void OnLoadClicked()    { SaveSystem.Load(_selectedSlot.SlotIndex); }
    private void OnNewSaveClicked() { SaveSystem.SaveNew(); LoadSlots(); }
}
```

---

### 2.8 비교 매트릭스: UGUI vs UI Toolkit

| UI 패턴 | UGUI 구현 난이도 | UI Toolkit 구현 난이도 | 성능 (UI Toolkit 기준) | 권장 선택 |
|---------|----------------|----------------------|----------------------|----------|
| Resource HUD | 쉬움 | 쉬움 | 동등 ~ 유리 (Canvas rebuild 없음) | UI Toolkit |
| Building Card Grid | 보통 (GridLayoutGroup) | 보통 (flex-wrap) | 동등 | 취향 |
| Unit List (대용량) | 어려움 (수동 풀링) | 쉬움 (ListView) | UI Toolkit 유리 (가상화 내장) | UI Toolkit |
| Settings (TabView) | 보통 | 쉬움 (TabView 내장) | 동등 | UI Toolkit |
| Modal Dialog | 보통 | 보통 | 동등 | 취향 |
| Tech Tree | 어려움 (LineRenderer 등) | 어려움 (Painter2D) | 동등 | UGUI (월드 공간 필요 시) |
| Save/Load | 보통 | 쉬움 (ListView) | UI Toolkit 유리 | UI Toolkit |

---

## 3. 베스트 프랙티스

### DO (권장)

- **OnEnable에서 Q<T>() 쿼리 결과를 필드에 캐시** — Update에서 반복 쿼리는 성능 낭비
- **USS 클래스로 상태 전환** — `AddToClassList`/`RemoveFromClassList`/`EnableInClassList`를 CSS 클래스와 조합
- **ListView.fixedItemHeight 항상 설정** — 가상화가 이 값 없이는 동작하지 않음
- **bindItem에서만 데이터 바인딩** — makeItem은 구조 생성만, 실제 데이터는 bindItem에서
- **모달 backdrop에 position: absolute + left/top/right/bottom: 0** — 전체 화면 클릭 차단
- **display 변경 후 CSS transition은 schedule.Execute로 1프레임 지연** — 즉시 추가 시 transition 미발동
- **generateVisualContent 후 MarkDirtyRepaint()** — 데이터 변경 시 수동 재그리기 트리거 필요
- **기술 트리 노드에 position: absolute + style.left/top** — Flexbox 레이아웃 밖에서 자유 배치

### DON'T (금지)

- **makeItem 내부에서 데이터 바인딩 금지** — 재사용 시 기존 데이터가 남음
- **bindItem 내부에서 이벤트 구독 금지** — 재활용 시 중복 구독, unbindItem에서 해제 필요
- **Update에서 Q<T>() 호출 금지** — 매 프레임 DOM 쿼리는 성능 저하
- **display: none 상태에서 직접 CSS transition 트리거 금지** — 1프레임 지연 필수
- **기술 트리 노드를 모두 Flexbox로 배치 시도 금지** — 자유 배치 불가, absolute 사용
- **generateVisualContent 콜백 내에서 MarkDirtyRepaint() 호출 금지** — 무한 루프

### CONSIDER (상황별)

- **모달 팝업 전용 UIDocument + 높은 Sort Order PanelSettings** — 완전한 레이어 분리가 필요할 때
- **DOTween을 CSS transition 대신 사용** — 복잡한 시퀀스 애니메이션, 이징 정밀 제어 필요 시
- **ListView item-template (UXML 속성)으로 makeItem 대체** — Unity 6에서 더 간결한 방법
- **ToggleButtonGroup** — 설정 화면의 RadioButton 그룹을 Unity 6에서 더 쉽게 구현

---

## 4. UGUI 대비 비교

| 측면 | UGUI 강점 | UI Toolkit 강점 |
|------|-----------|----------------|
| 대규모 목록 | 수동 풀링 구현 필요 | ListView 내장 가상화 |
| 레이아웃 코드 | 적음 (컴포넌트 기반) | 적음 (USS 선언형) |
| 애니메이션 | DOTween/Animator/Timeline 완전 지원 | CSS transition (제한적), DOTween 가능 |
| 월드 공간 UI | 완전 지원 | 미지원 (RenderTexture 우회만) |
| 그리드 레이아웃 | GridLayoutGroup 컴포넌트 | flex-wrap 근사 (CSS Grid 미지원) |
| 커스텀 그래픽 | Graphic 상속 + OnPopulateMesh | generateVisualContent + Painter2D |
| 상태 기반 스타일 | 코드로 Color/Alpha 직접 변경 | USS :hover, .class 선언형 |
| 마스킹 | Mask 컴포넌트 | overflow: hidden |
| 데이터 많은 화면 | Canvas rebuild 비용 | 개별 dirty, 재계산 효율적 |

---

## 5. 예제 코드

### 5.1 ResourceHUD 전체 코드 요약

핵심 패턴: `OnEnable`에서 쿼리 → 캐시 → 공개 메서드로 업데이트 → USS transition 활용

```csharp
// 사용 측 (게임 매니저 등)
_resourceHud.UpdateResources(
    gold: GameState.Gold,
    wood: GameState.Wood,
    food: GameState.Food,
    pop: GameState.Population,
    maxPop: GameState.MaxPopulation
);
```

### 5.2 ListView 필터+정렬 핵심 패턴

```csharp
// 필터 후 정렬 후 갱신 — 3단계 패턴
_filteredList = _allData.Where(Filter).OrderBy(SortKey).ToList();
_listView.itemsSource = _filteredList;
_listView.RefreshItems(); // Rebuild()는 전체 재구성이므로 비용 큼
```

### 5.3 모달 표시/숨김의 display + transition 패턴

```csharp
// 표시: display 먼저, 그 다음 프레임에 class 추가
_backdrop.style.display = DisplayStyle.Flex;
_backdrop.schedule.Execute(() => _backdrop.AddToClassList("--visible"));

// 숨김: class 제거 후 transition 완료 대기, 그 후 display 제거
_backdrop.RemoveFromClassList("--visible");
await UniTask.Delay(TimeSpan.FromSeconds(TRANSITION_DURATION));
_backdrop.style.display = DisplayStyle.None;
```

---

## 6. UI_Study 적용 계획

| 화면 | 구현 우선순위 | 핵심 학습 포인트 |
|------|-------------|----------------|
| Resource HUD | 1순위 | Flexbox row, Label 업데이트, USS transition |
| Building Menu | 2순위 | flex-wrap 그리드, :hover, CSS 클래스 상태 |
| Unit List | 2순위 | ListView 가상화, makeItem/bindItem, 정렬/필터 |
| Settings Screen | 3순위 | TabView, Slider/Toggle/DropdownField, SetValueWithoutNotify |
| Dialog Popup | 3순위 | display+transition 패턴, pickingMode, schedule.Execute |
| Tech Tree | 4순위 | position: absolute, generateVisualContent, Painter2D |
| Save/Load | 4순위 | ListView + 썸네일, Dialog 연동 |

---

## 7. 참고 자료

1. [Unity Manual: ListView](https://docs.unity3d.com/Manual/UIE-uxml-element-ListView.html)
2. [Unity ScriptRef: ListView API](https://docs.unity3d.com/ScriptReference/UIElements.ListView.html)
3. [Unity Manual: TabView (Unity 6)](https://docs.unity3d.com/Manual/UIE-uxml-element-TabView.html)
4. [Unity Manual: USS Animatable Properties](https://docs.unity3d.com/Manual/UIE-USS-Properties-Reference.html)
5. [Unity Manual: TransitionEndEvent](https://docs.unity3d.com/ScriptReference/UIElements.TransitionEndEvent.html)
6. [Unity Manual: Painter2D / generateVisualContent](https://docs.unity3d.com/Manual/UIE-generate-2d-visual-content.html)
7. [Unity Manual: Masking (overflow: hidden)](https://docs.unity3d.com/Manual/UIE-masking.html)
8. [Unity Manual: Event Dispatching / pickingMode](https://docs.unity3d.com/Manual/UIE-Events-Dispatching.html)
9. [Unity Manual: PanelSettings.targetTexture](https://docs.unity3d.com/ScriptReference/UIElements.PanelSettings-targetTexture.html)
10. [loglog.games: UI Toolkit First Steps](https://loglog.games/blog/unity-ui-toolkit-first-steps/)
11. [Angry Shark Studio: UGUI vs UI Toolkit 2025](https://www.angry-shark-studio.com/blog/unity-ui-toolkit-vs-ugui-2025-guide/)
12. [UIToolkitUnityRoyaleRuntimeDemo (GitHub)](https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo)

---

## 8. 미해결 질문

- [ ] **flex-wrap 그리드에서 마지막 행 정렬**: 카드가 `flex-wrap`으로 줄바꿈될 때 마지막 행이 왼쪽 정렬되도록 강제하는 방법 (CSS `align-content: flex-start` 확인 필요)
- [ ] **ListView의 DynamicHeight 모드 성능**: 행 높이가 가변적인 경우 FixedHeight 대비 성능 차이 측정 필요
- [ ] **generateVisualContent와 DOTween 조합**: 연결선 애니메이션(그라데이션 이동 등)을 DOTween으로 구동할 때 MarkDirtyRepaint 호출 빈도 최적화 방법
- [ ] **Tech Tree ScrollView + 핀치 줌**: 모바일에서 기술 트리를 핀치 줌하는 패턴이 UI Toolkit에서 가능한지 조사 필요
- [ ] **TabView selectedTabIndex 저장/복원**: view-data-key 속성 사용 시 PlayerPrefs와 중복 저장 방지 전략
- [ ] **모달에서 ESC 키 처리**: KeyDownEvent를 root에서 받을 때 모달이 열려있는지 상태 확인하는 패턴 정립 필요
