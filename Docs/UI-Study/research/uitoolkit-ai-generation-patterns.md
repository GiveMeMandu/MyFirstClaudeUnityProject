# AI-Assisted UI 개발 — Unity UI Toolkit 생성 패턴

- **작성일**: 2026-03-29
- **카테고리**: practice
- **상태**: 조사완료

---

## 1. 요약

Unity UI Toolkit은 UXML(HTML과 유사)과 USS(CSS 서브셋)를 텍스트 파일로 분리하기 때문에,
방대한 HTML/CSS 학습 데이터를 보유한 LLM이 구조와 스타일을 직접 생성·파싱·diff할 수 있다.
반면 UGUI는 GameObjects + Inspector 값 조합이므로 AI가 추론하기 어렵고,
생성된 코드를 에디터 없이 검증하기 어렵다.
결론적으로 AI 보조 프로토타이핑 파이프라인에서는 UI Toolkit이 UGUI 대비 명확한 구조적 우위를 지닌다.
단, USS는 표준 CSS의 서브셋이므로 AI가 유효하지 않은 CSS 속성을 생성하는 위험이 있으며,
이를 사전에 인지하고 검증 단계를 포함하는 것이 필수다.

---

## 2. 상세 분석

### 2.1 AI가 UI Toolkit을 UGUI보다 잘 처리하는 이유

#### 포맷의 구조적 차이

| 항목 | UGUI | UI Toolkit |
|------|------|-----------|
| 레이아웃 정의 | GameObjects + RectTransform (Inspector) | UXML (텍스트 파일) |
| 스타일 정의 | MonoBehaviour 코드 or Inspector | USS (텍스트 파일) |
| 로직 | C# MonoBehaviour | C# MonoBehaviour |
| AI 생성 가능성 | 낮음 (Inspector 값 직접 설정 불가) | 높음 (파일 내용 직접 생성) |
| 버전 관리 | 어려움 (.unity/.prefab 바이너리 유사) | 용이 (텍스트 diff 가능) |
| 생성 결과 검증 | 에디터에서만 확인 가능 | 텍스트 파싱으로도 검증 가능 |

#### LLM 학습 데이터 관점

- **UXML**: XML 문법 기반 → 인터넷에 수십억 줄의 HTML/XML 학습 데이터 존재
- **USS**: CSS 서브셋 문법 → CSS/SCSS/Less 학습 데이터가 GitHub, MDN, Stack Overflow에 풍부
- **Flexbox 레이아웃**: Unity의 레이아웃 엔진은 Yoga(Facebook)를 기반으로 CSS Flexbox 서브셋을 구현
  → 웹 개발 레퍼런스와 거의 동일하게 동작하므로 AI 생성 코드의 정확도가 높음
- **분리된 관심사**: 구조(UXML) / 스타일(USS) / 로직(C#)의 3파일 분리가 AI가 각 역할을 명확히
  인식하고 생성하는 데 유리하게 작용

#### UGUI의 AI 생성 한계

UGUI로 동일한 UI를 만들려면:

```csharp
// AI가 생성해야 하는 코드 — 에디터 Inspector 없이는 결과 확인 불가
var panel = new GameObject("ResourcePanel");
panel.AddComponent<RectTransform>();
panel.AddComponent<Image>();
// ... 수십 줄의 RectTransform 좌표 계산 ...
```

에디터 없이 실행하면 RectTransform 앵커·피벗·오프셋의 조합이 의도와 다르게 나타날 가능성이 높다.
(참조: [doc 007 — UGUI 코드 기반 레이아웃](ugui-programmatic-layout.md))

반면 UI Toolkit은 텍스트 파일만 드롭하면 바로 렌더링된다.

---

### 2.2 UXML 생성 패턴

#### 효과적인 프롬프트 작성 원칙

AI가 고품질 UXML을 생성하려면 프롬프트에 다음 요소가 포함되어야 한다.

1. **화면 목적**: "리소스 패널", "인벤토리 그리드", "설정 화면"
2. **구성 요소 목록**: 포함할 레이블/버튼/슬라이더 나열
3. **레이아웃 방향**: 수평/수직, 그리드 열 수
4. **BEM 네이밍 힌트**: 클래스명 스키마 제공 (예: `resource-panel__row`)
5. **USS 파일명 명시**: `<Style src="ResourcePanel.uss"/>` 참조를 포함하도록

**권장 프롬프트 구조:**

```
Unity UI Toolkit UXML 파일을 생성해 주세요.
파일명: ResourcePanel.uxml
USS 참조: ResourcePanel.uss

요소:
- 최상위 VisualElement (name="resource-panel", class="resource-panel")
- 내부에 3개의 리소스 행 (gold, wood, food)
- 각 행: 아이콘용 VisualElement + Label(이름) + Label(수량)
- 하단 "수집하기" Button

UXML 네임스페이스 포함, name 속성으로 C# 쿼리 가능하게 작성해 주세요.
```

#### AI가 잘 생성하는 UXML 구조

| 패턴 | 성공률 | 비고 |
|------|--------|------|
| 세로 리스트 (항목 목록) | 높음 | `flex-direction: column` 직관적 |
| 수평 네비게이션 바 | 높음 | `flex-direction: row` + `justify-content` |
| 카드 그리드 | 높음 | `flex-wrap: wrap` + 퍼센트 너비 |
| 폼 레이아웃 (라벨+입력) | 높음 | HTML form 패턴과 동일 |
| 헤더/바디/푸터 구조 | 높음 | 시맨틱 웹 패턴 직접 매핑 |
| 탭 패널 | 중간 | TabView 요소 타입명 혼동 가능 |
| 복잡한 중첩 템플릿 | 낮음 | `<Template>` 참조 경로 오류 빈번 |

#### 예제 1 — 리소스 패널 (Gold / Wood / Food)

**프롬프트**: "Create a Unity UI Toolkit UXML for a resource panel showing gold, wood, and food counters with icons. Include USS reference."

**예상 AI 출력:**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xsi:noNamespaceSchemaLocation="../../../UIElementsSchema/UIElements.xsd"
         editor-extension-mode="False">
    <ui:Style src="ResourcePanel.uss"/>
    <ui:VisualElement name="resource-panel" class="resource-panel">
        <ui:VisualElement name="resource-panel__header" class="resource-panel__header">
            <ui:Label text="자원" name="resource-panel__title"
                      class="resource-panel__title"/>
        </ui:VisualElement>
        <ui:VisualElement name="resource-panel__body" class="resource-panel__body">
            <!-- Gold Row -->
            <ui:VisualElement name="resource-row--gold" class="resource-row">
                <ui:VisualElement name="icon-gold" class="resource-row__icon resource-row__icon--gold"/>
                <ui:Label text="골드" name="label-gold-name" class="resource-row__name"/>
                <ui:Label text="0" name="label-gold-value" class="resource-row__value"/>
            </ui:VisualElement>
            <!-- Wood Row -->
            <ui:VisualElement name="resource-row--wood" class="resource-row">
                <ui:VisualElement name="icon-wood" class="resource-row__icon resource-row__icon--wood"/>
                <ui:Label text="목재" name="label-wood-name" class="resource-row__name"/>
                <ui:Label text="0" name="label-wood-value" class="resource-row__value"/>
            </ui:VisualElement>
            <!-- Food Row -->
            <ui:VisualElement name="resource-row--food" class="resource-row">
                <ui:VisualElement name="icon-food" class="resource-row__icon resource-row__icon--food"/>
                <ui:Label text="식량" name="label-food-name" class="resource-row__name"/>
                <ui:Label text="0" name="label-food-value" class="resource-row__value"/>
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:Button text="수집하기" name="btn-collect" class="resource-panel__btn-collect"/>
    </ui:VisualElement>
</ui:UXML>
```

#### 예제 2 — 인벤토리 그리드 (4열)

**프롬프트**: "Create a Unity UI Toolkit UXML for a 4-column inventory grid with a scrollable item area. Each slot has an icon and quantity label."

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <ui:Style src="InventoryGrid.uss"/>
    <ui:VisualElement name="inventory-root" class="inventory-root">
        <ui:VisualElement name="inventory-header" class="inventory-header">
            <ui:Label text="인벤토리" class="inventory-header__title"/>
            <ui:Label text="0 / 32" name="label-capacity" class="inventory-header__capacity"/>
        </ui:VisualElement>
        <ui:ScrollView name="inventory-scroll" class="inventory-scroll"
                       horizontal-scroller-visibility="Hidden"
                       vertical-scroller-visibility="Auto">
            <ui:VisualElement name="inventory-grid" class="inventory-grid">
                <!-- 슬롯은 C#에서 동적 생성 — 아래는 단일 슬롯 템플릿 예시 -->
                <ui:VisualElement name="slot-0" class="inventory-slot">
                    <ui:VisualElement name="slot-0__icon" class="inventory-slot__icon"/>
                    <ui:Label text="" name="slot-0__qty" class="inventory-slot__qty"/>
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:ScrollView>
        <ui:VisualElement name="item-detail" class="item-detail">
            <ui:Label text="아이템을 선택하세요" name="detail-name" class="item-detail__name"/>
            <ui:Label text="" name="detail-desc" class="item-detail__desc"/>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

#### UXML 생성 시 AI 한계

- **Template 참조 오류**: `<Template src="..."/>` + `<Instance template="..."/>` 경로를 잘못 지정
- **네임스페이스 혼동**: `UnityEditor.UIElements` 요소를 런타임 UXML에 삽입 (에디터 전용 요소)
- **존재하지 않는 요소**: `<ui:GridView>` 같이 실제로 없는 요소명 생성
- **name vs class 혼동**: C# 쿼리용 `name`과 스타일용 `class` 역할을 바꿔 사용
- **xsi 스키마 경로**: IDE 자동완성을 위한 스키마 경로를 잘못 생성할 수 있음

---

### 2.3 USS 생성 패턴

#### 효과적인 USS 프롬프트

```
Unity UI Toolkit USS 파일을 생성해 주세요.
파일명: ResourcePanel.uss
테마: 다크, 판타지 RPG 느낌
색상 팔레트: 배경 #1A1A2E, 강조 #E94560, 중립 #16213E
CSS 변수(--custom-property)로 색상 정의 후 재사용
중요: USS는 CSS 서브셋입니다. 다음은 지원되지 않으므로 사용 금지:
  - gap, grid, calc(), media queries, z-index
  - em/rem 단위 (px 사용)
  - ::before, ::after 가상 요소
  - linear-gradient()
```

#### AI가 잘 생성하는 USS 패턴

**다크 테마 설정 패널:**

```css
/* ResourcePanel.uss */

/* === 디자인 토큰 === */
:root {
    --panel-bg: #1A1A2E;
    --panel-border: #E94560;
    --panel-text: #E0E0E0;
    --panel-text-muted: #888888;
    --panel-accent: #E94560;
    --panel-row-hover: #16213E;
    --panel-btn-bg: #E94560;
    --panel-btn-text: #FFFFFF;
    --spacing-xs: 4px;
    --spacing-sm: 8px;
    --spacing-md: 16px;
    --spacing-lg: 24px;
    --radius-sm: 4px;
    --radius-md: 8px;
}

/* === 패널 컨테이너 === */
.resource-panel {
    background-color: var(--panel-bg);
    border-width: 1px;
    border-color: var(--panel-border);
    border-radius: var(--radius-md);
    padding: var(--spacing-md);
    min-width: 200px;
}

.resource-panel__header {
    border-bottom-width: 1px;
    border-bottom-color: var(--panel-border);
    padding-bottom: var(--spacing-sm);
    margin-bottom: var(--spacing-md);
}

.resource-panel__title {
    color: var(--panel-text);
    font-size: 14px;
    -unity-font-style: bold;
}

.resource-panel__body {
    flex-direction: column;
}

/* === 리소스 행 === */
.resource-row {
    flex-direction: row;
    align-items: center;
    padding: var(--spacing-xs) var(--spacing-sm);
    border-radius: var(--radius-sm);
    margin-bottom: 2px;
    transition: background-color 0.15s ease;
}

.resource-row:hover {
    background-color: var(--panel-row-hover);
}

.resource-row__icon {
    width: 20px;
    height: 20px;
    margin-right: var(--spacing-sm);
    -unity-background-scale-mode: scale-to-fit;
}

.resource-row__name {
    color: var(--panel-text-muted);
    font-size: 12px;
    flex-grow: 1;
}

.resource-row__value {
    color: var(--panel-text);
    font-size: 13px;
    -unity-font-style: bold;
    -unity-text-align: middle-right;
    min-width: 50px;
}

/* === 수집 버튼 === */
.resource-panel__btn-collect {
    background-color: var(--panel-btn-bg);
    color: var(--panel-btn-text);
    border-width: 0;
    border-radius: var(--radius-sm);
    padding: var(--spacing-sm) var(--spacing-md);
    margin-top: var(--spacing-md);
    font-size: 12px;
    -unity-font-style: bold;
    transition: opacity 0.1s ease;
}

.resource-panel__btn-collect:hover {
    opacity: 0.85;
}

.resource-panel__btn-collect:active {
    opacity: 0.7;
    translate: 0 1px;
}
```

**버튼 상태 (Hover / Active / Disabled):**

```css
/* ButtonStates.uss */
.game-btn {
    background-color: #2C3E50;
    color: #ECF0F1;
    border-width: 2px;
    border-color: #3498DB;
    border-radius: 6px;
    padding: 10px 20px;
    font-size: 14px;
    -unity-font-style: bold;
    transition: background-color 0.1s ease, scale 0.1s ease;
}

.game-btn:hover {
    background-color: #3498DB;
    border-color: #5DADE2;
}

.game-btn:active {
    background-color: #1F618D;
    scale: 0.97;
}

.game-btn:disabled {
    background-color: #1A252F;
    color: #566573;
    border-color: #2C3E50;
    opacity: 0.5;
}
```

#### AI가 생성하는 USS의 공통 오류

| 잘못된 생성 예 | 실제 USS 대체 방법 |
|---------------|------------------|
| `gap: 8px;` | 자식에 `margin-bottom: 8px;` 적용 |
| `display: grid;` | `flex-direction: row; flex-wrap: wrap;` |
| `z-index: 10;` | 지원 안 됨 — 요소 순서(sibling index)로 제어 |
| `calc(100% - 20px)` | 지원 안 됨 — `flex-grow: 1; margin: 10px;` 로 대체 |
| `@media (max-width: 768px)` | 지원 안 됨 — C#에서 Runtime에서 클래스 교체 |
| `linear-gradient(...)` | 지원 안 됨 — 텍스처 PNG 임포트 또는 중첩 배경 |
| `::before`, `::after` | 지원 안 됨 — VisualElement 자식으로 대체 |
| `em`, `rem` 단위 | `px` 단위만 사용 |
| `box-shadow:` | 지원 안 됨 — 동일한 색상의 뒤쪽 VisualElement로 시뮬레이션 |
| `font-family: 'Roboto'` | `-unity-font-definition: url(...)` 사용 |
| `text-align: center` | `-unity-text-align: middle-center` |

#### Unity 전용 USS 속성 (AI가 자주 놓치는 항목)

```css
/* AI 프롬프트에 이 속성들 예시를 포함하면 정확도 향상 */
.example {
    -unity-font-style: bold;                    /* italic, bold, bold-and-italic */
    -unity-text-align: middle-center;           /* 9-position grid */
    -unity-text-overflow-position: end;         /* start, middle, end */
    -unity-background-scale-mode: scale-to-fit; /* stretch-to-fill, scale-and-crop */
    -unity-background-image-tint-color: #FF0000;
    -unity-slice-left: 10;                      /* 9-slice (px, 비율 아님) */
    -unity-slice-right: 10;
    -unity-slice-top: 10;
    -unity-slice-bottom: 10;
    -unity-overflow-clip-box: content-box;      /* padding-box (기본) */
}
```

---

### 2.4 UXML + USS + C# 통합 워크플로

#### 풀 워크플로: 자연어 → 3파일 생성

```
1. PROMPT: "리소스 패널 UI가 필요합니다. 골드/목재/식량 수치를 보여주고
            수집 버튼이 있는 다크 테마 패널입니다."

2. AI 출력 #1: ResourcePanel.uxml (구조)
3. AI 출력 #2: ResourcePanel.uss  (스타일)
4. AI 출력 #3: ResourcePanelController.cs (로직)
```

#### AI 생성 C# 컨트롤러 패턴

```csharp
// ResourcePanelController.cs — AI 생성 표준 패턴
using UnityEngine;
using UnityEngine.UIElements;

public class ResourcePanelController : MonoBehaviour
{
    // --- Inspector 참조 ---
    [SerializeField] UIDocument _document;

    // --- Cached Element References ---
    Label _labelGoldValue;
    Label _labelWoodValue;
    Label _labelFoodValue;
    Button _btnCollect;

    // --- Lifecycle ---
    void Awake()
    {
        if (_document == null)
            _document = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        var root = _document.rootVisualElement;

        // UQuery로 요소 캐싱 (UXML name 속성 기반)
        _labelGoldValue = root.Q<Label>("label-gold-value");
        _labelWoodValue = root.Q<Label>("label-wood-value");
        _labelFoodValue = root.Q<Label>("label-food-value");
        _btnCollect     = root.Q<Button>("btn-collect");

        // 이벤트 등록
        _btnCollect?.RegisterCallback<ClickEvent>(OnCollectClicked);
    }

    void OnDisable()
    {
        // 이벤트 해제 (메모리 누수 방지)
        _btnCollect?.UnregisterCallback<ClickEvent>(OnCollectClicked);
    }

    // --- Public API ---
    public void UpdateResources(int gold, int wood, int food)
    {
        if (_labelGoldValue != null) _labelGoldValue.text = gold.ToString("N0");
        if (_labelWoodValue != null) _labelWoodValue.text = wood.ToString("N0");
        if (_labelFoodValue != null) _labelFoodValue.text = food.ToString("N0");
    }

    // --- Private ---
    void OnCollectClicked(ClickEvent evt)
    {
        Debug.Log("Collect clicked");
    }
}
```

#### 반복 개선(Iterative Refinement) 프롬프트 패턴

초안 생성 이후 AI에게 점진적으로 개선을 요청하는 방식:

```
1단계: "ResourcePanel.uxml 초안을 만들어 주세요"
2단계: "수집 버튼 아래에 '다음 수집까지 00:30' 카운트다운 레이블을 추가해 주세요"
3단계: "USS에서 수집 버튼이 비활성화 상태일 때 회색으로 변하는 :disabled 스타일 추가"
4단계: "C# 컨트롤러에 UniTask 기반 카운트다운 로직을 추가해 주세요"
```

이 방식은 한번에 전체를 생성하는 것보다 각 단계의 오류를 격리하기 쉽다.

#### UGUI 코드 우선 방식과의 비교

doc 007(ugui-programmatic-layout.md)의 UGUI code-first 접근 방식과 비교:

```
UGUI code-first 생성 예 (AI 출력):
var panel = new GameObject("ResourcePanel");
var rt = panel.AddComponent<RectTransform>();
rt.anchorMin = new Vector2(0, 1);  // ← AI가 자주 틀리는 부분
rt.anchorMax = new Vector2(0, 1);
rt.anchoredPosition = new Vector2(10, -10);
rt.sizeDelta = new Vector2(200, 300);
// ... 수십 줄 이어짐 ...
```

이 코드는 에디터에서 실행해보기 전까지 올바른지 판단하기 어렵다.
AI도 앵커/피벗/오프셋 조합을 자주 틀리며, 결과물이 화면 밖에 렌더링될 수 있다.

---

### 2.5 AI 프로토타이핑 파이프라인

#### 실전 6단계 워크플로

```
STEP 1: 화면 설명 (자연어)
         ↓
STEP 2: AI → UXML 생성 (구조)
         ↓
STEP 3: AI → USS 생성 (스타일) [USS 제약 프롬프트 필수]
         ↓
STEP 4: AI → C# 컨트롤러 생성 (로직)
         ↓
STEP 5: 파일을 Unity 프로젝트에 복사
         + UIDocument 컴포넌트에 UXML 할당
         + PanelSettings 확인
         ↓
STEP 6: 반복 피드백 (빨간 에러 → AI에게 전달 → 수정 요청)
```

#### 각 단계별 실제 소요 시간 추정

| 단계 | 수동 개발 | AI 보조 | 절감 |
|------|-----------|---------|------|
| UXML 레이아웃 구조 작성 | 30분 | 5분 | ~83% |
| USS 스타일 시트 작성 | 45분 | 8분 | ~82% |
| C# 컨트롤러 보일러플레이트 | 20분 | 3분 | ~85% |
| 첫 번째 패스 오류 수정 | 0분 | 15분 | -15분 (추가 발생) |
| **총계 (간단한 패널 기준)** | **95분** | **31분** | **~67% 절감** |

주의: AI 첫 번째 패스는 "80–90% 수준"이다.
나머지 10–20%는 USS 속성 오류 수정, name 속성 검증, C# Null 확인이 대부분이다.

#### 첫 번째 패스 AI 출력의 공통 수정 항목

1. **USS에서 `gap:` 제거** → 각 자식의 `margin-bottom`으로 교체
2. **UXML에서 editor-only 요소 제거** (예: `<uie:PropertyField>`)
3. **C#에서 `OnEnable`/`OnDisable` 패턴 확인** (일부 AI가 `Awake`에 이벤트 등록)
4. **`Q<>()` 반환값 null 체크** (name 오타로 null 반환 가능)
5. **`-unity-text-align` 값 수정** (AI가 CSS의 `text-align: center` 그대로 생성할 때)

---

### 2.6 한계와 주의사항

#### USS ≠ CSS: 주요 차이 정리

USS는 CSS에서 영감을 받았지만, LLM이 CSS 지식으로 생성하면 런타임 에러 없이 무시되는
속성이 다수 존재한다 (Unity는 무효 속성을 콘솔 경고 없이 무시하는 경우가 있음).

**현재(Unity 6 기준) 지원하지 않는 CSS 기능:**

| CSS 기능 | 지원 여부 | 비고 |
|----------|-----------|------|
| `gap` / `column-gap` / `row-gap` | 미지원 | 로드맵에 있음 |
| CSS Grid (`display: grid`) | 미지원 | flex-wrap 사용 |
| `z-index` | 미지원 | sibling order로 제어 |
| `calc()` | 미지원 | flex-grow 비율로 대체 |
| `@media` 미디어 쿼리 | 미지원 | C# 클래스 교체로 대체 |
| `em` / `rem` 단위 | 미지원 | px 고정 |
| `vh` / `vw` 단위 | 미지원 | px 또는 flex |
| `linear-gradient()` | 미지원 | 텍스처 임포트 |
| `::before` / `::after` | 미지원 | VisualElement 자식 |
| `box-shadow` | 미지원 | 중첩 컨테이너 |
| `:first-child` / `:last-child` | 미지원 | C# 클래스 추가 |
| `:nth-child()` | 미지원 | C# 인덱스 기반 |
| `transform: rotate()` | 부분 지원 | `rotate` 속성 별도 존재 |

#### 에디터 전용 API를 런타임에 사용하는 문제

```csharp
// AI가 잘못 생성하는 패턴 — 에디터 전용
using UnityEditor.UIElements;

var propertyField = new PropertyField(serializedProperty); // 에디터 only!
root.Add(propertyField);
```

런타임 빌드에서는 `UnityEditor` 네임스페이스가 제거되어 빌드 실패.
AI에게 명시적으로 "runtime UI, no UnityEditor namespace" 를 지정해야 한다.

#### UXML 템플릿 참조 오류

```xml
<!-- AI가 생성하는 잘못된 패턴 -->
<ui:Template name="ItemSlot" src="../Prefabs/ItemSlot.uxml"/>
<ui:Instance template="ItemSlot" />

<!-- 올바른 패턴: Assets/ 경로 기준 또는 동일 폴더 상대 경로 -->
<ui:Template name="ItemSlot" src="project://database/Assets/_Study/ItemSlot.uxml"/>
```

#### 복잡한 레이아웃 엣지 케이스

- **flexbox만 지원**: CSS Grid가 없어 복잡한 2D 그리드는 `flex-wrap: wrap`으로 시뮬레이션
  → 열 수가 고정되지 않을 수 있으므로 C#에서 너비를 계산하여 직접 지정 필요
- **퍼센트 너비 오작동**: 부모가 고정 크기가 아닐 때 `width: 25%`가 의도대로 작동 안 할 수 있음
- **`overflow: hidden` 과 `border-radius`**: 자식 클리핑이 CSS와 다르게 동작할 수 있음

#### AI 생성을 사용하지 말아야 할 경우

| 케이스 | 이유 |
|--------|------|
| 커스텀 렌더러 (Mesh API) | USS로 표현 불가, C# 전문 지식 필요 |
| 복잡한 타임라인 애니메이션 | UI Toolkit에 Timeline 통합 없음 |
| World Space UI | UI Toolkit은 Screen Space 전용 (현재) |
| 프로덕션급 퍼포먼스 최적화 | AI 생성 코드는 캐싱 부재 등 문제 있음 |
| 동적 셰이더/파티클 UI | USS Material 속성은 매우 제한적 |

---

### 2.7 UGUI Code-First vs UI Toolkit AI-Generation 비교

#### 동일한 UI를 두 방식으로: 리소스 패널

**UGUI Code-First (doc 007 방식):**

```csharp
// ResourcePanelBuilder.cs — UGUI 순수 코드 생성
// 장점: 에디터 없이도 완전히 동작
// 단점: 많은 상용구 코드, AI 생성 오류율 높음

static GameObject BuildResourcePanel(Transform parent)
{
    var panel = UIFactory.CreatePanel("ResourcePanel", parent,
        new Vector2(200, 300), new Vector2(10, -10));

    var vlg = panel.AddComponent<VerticalLayoutGroup>();
    vlg.padding = new RectOffset(10, 10, 10, 10);
    vlg.spacing = 5;
    vlg.childForceExpandWidth = true;
    vlg.childForceExpandHeight = false;

    BuildResourceRow("골드", panel.transform);
    BuildResourceRow("목재", panel.transform);
    BuildResourceRow("식량", panel.transform);
    // ... 계속 ...
    return panel;
}
```

**UI Toolkit AI-Generation:**

```
입력: "Create a resource panel UXML with gold/wood/food rows and a collect button"
출력: ResourcePanel.uxml (30줄) + ResourcePanel.uss (80줄) + 컨트롤러.cs (50줄)
시간: 약 5분 (AI 생성) + 15분 (검증/수정) = 20분
```

#### 비교표

| 기준 | UGUI Code-First | UI Toolkit AI-Gen |
|------|-----------------|-------------------|
| 초기 생성 속도 | 느림 (코드 작성) | 빠름 (AI 생성) |
| AI 생성 정확도 | 낮음 (좌표 계산 오류) | 중-높음 (텍스트 파싱 가능) |
| 수정 용이성 | 코드 변경 후 재컴파일 | UXML/USS 텍스트 수정 즉시 반영 |
| 디자이너 협업 | 어려움 (코드 전용) | 용이 (UXML/USS는 코드 지식 낮아도 됨) |
| 런타임 성능 | 비슷 | 배칭 우위 (단일 draw call) |
| 버전 관리 | 텍스트 diff 가능 | 텍스트 diff 가능 |
| 학습 곡선 | Unity 경험자엔 친숙 | 웹 경험자엔 친숙 |
| 애니메이션 | DOTween 완전 지원 | CSS transition만 (DOTween 미지원) |
| World Space UI | 지원 | 미지원 |

---

### 2.8 실제 사례 및 커뮤니티 경험

#### 공식 사례: Unity App UI — Claude Code 플러그인

Unity의 공식 패키지 `com.unity.dt.app-ui`(버전 2.2.0+)는 Claude Code 전용 플러그인을 내장한다.
이것이 LLM + UI Toolkit 통합의 공식 레퍼런스다.

```bash
# 플러그인 로드 방법
claude --plugin-dir ./Packages/com.unity.dt.app-ui/Plugins~
```

**제공 스킬:**

| 스킬 | 역할 |
|------|------|
| `app-ui` | UXML/USS 컴포넌트 일반 생성 |
| `app-ui-navigation` | NavGraph, NavHost, NavController |
| `app-ui-redux` | Redux 상태 관리 패턴 |
| `app-ui-mvvm` | MVVM 아키텍처 + DI 패턴 |
| `app-ui-theming` | USS 변수, 다크/라이트 모드 |

**예시 프롬프트 (공식):**

```
"Create a settings screen with a back button and navigation"
"Build a custom dark theme with blue palette and touch-friendly spacing"
"Set up Redux store for user authentication with async actions"
```

#### 커뮤니티 발견 도구: Rosalina

GitHub 오픈소스 도구로 UXML에서 C# 바인딩 코드를 자동 생성한다.
AI가 UXML을 생성하면 Rosalina가 C# 코드를 자동으로 만드는 조합이 효과적이다.

```
워크플로: AI → UXML 생성 → Rosalina → C# 바인딩 자동 생성
```

```csharp
// Rosalina 출력 예 (UXML의 name 속성 기반)
// 파일: ResourcePanel.g.cs (자동 생성, 수정 금지)
public partial class ResourcePanel
{
    public Label LabelGoldValue { get; private set; }
    public Label LabelWoodValue { get; private set; }
    public Button BtnCollect { get; private set; }

    public void InitializeDocument()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        LabelGoldValue = root.Q<Label>("label-gold-value");
        LabelWoodValue = root.Q<Label>("label-wood-value");
        BtnCollect     = root.Q<Button>("btn-collect");
    }
}

// 수동 작성 부분 (partial class)
public partial class ResourcePanel : MonoBehaviour
{
    void Start()
    {
        InitializeDocument();
        BtnCollect.clicked += () => Debug.Log("Collect!");
    }
}
```

설치: `com.eastylabs.rosalina` (OpenUPM 또는 Git URL)

#### UXLora — AI 전용 UXML/USS 생성 도구

2026년 Q1 출시 예정(조사 시점 기준)인 SaaS 도구로,
게임 UI 설명(장르, 톤, 색상 팔레트)을 입력하면 Unity-ready UXML/USS 파일을 직접 생성한다.
HUD, 인벤토리, 메뉴, 상점 화면 등을 지원하며 Figma 파일로도 내보내기가 가능하다.

#### Unity MCP 생태계

Claude Code, Cursor, Windsurf 등의 AI IDE에서 Unity Editor를 직접 제어하는 MCP 서버들이
활발히 개발되고 있다. UXML/USS 파일의 읽기/쓰기/리스트 기능을 포함한다.

- `CoplayDev/unity-mcp`: UXML/USS 파일 관리, 씬 제어
- `CoderGamester/mcp-unity`: Claude Code, Cursor 통합 전용
- `IvanMurzak/Unity-MCP`: C# 메서드를 MCP 툴로 등록 가능

#### 커뮤니티 합의 (2025 기준)

Unity Discussions의 "UI Toolkit with LLMs" 스레드 및 관련 토론에서:

- AI는 간단한 UI Toolkit 레이아웃과 스타일의 "80–90%를 맞춘다"
- USS vs CSS 차이로 인한 수정이 가장 빈번한 작업
- UXML은 AI 생성 품질이 높은 반면, C# 바인딩은 Null 안전성 등 추가 검토 필요
- 웹 개발 경험자가 Unity를 처음 접할 때 UI Toolkit을 선택하는 주요 이유가
  "AI 도구가 CSS/HTML과 같은 방식으로 다뤄주기 때문"이라는 의견 다수

---

## 3. 베스트 프랙티스

### DO (권장)

- **USS 제약 목록을 프롬프트에 항상 포함**: `gap`, `calc()`, `z-index` 금지 명시
- **런타임 전용 UI임을 명시**: "runtime UI, no UnityEditor namespace"
- **BEM 네이밍 요청**: `block__element--modifier` 패턴으로 일관성 유지
- **name 속성 지정 요청**: C# `Q<>()` 쿼리를 위해 모든 상호작용 요소에 `name` 필수
- **디자인 토큰(`:root` 변수) 먼저 생성**: 색상·간격·반지름 변수 선언 후 컴포넌트 스타일 적용
- **단계별 파일 분리 요청**: UXML → USS → C# 순서로 개별 생성 요청
- **Rosalina 활용**: AI가 UXML을 생성하면 Rosalina로 C# 바인딩 자동화
- **OnEnable/OnDisable 패턴 사용**: 이벤트 등록·해제를 대칭으로 처리하도록 명시
- **항상 Q<>() 반환값 null 체크**: AI 생성 코드에서 자주 누락됨

### DON'T (금지)

- **무수정으로 AI 생성 USS 그대로 사용 금지**: 반드시 unsupported 속성 검증
- **에디터 전용 요소를 런타임 UXML에 포함 금지**: `<uie:PropertyField>`, `<uie:ObjectField>` 등
- **`inline style`을 UXML 속성으로 대량 작성 금지**: 메모리 비용 증가 (USS 클래스 우선)
- **Unity `--unity-*` 속성 명시 없이 CSS `font-style`, `text-align` 등 직접 사용 금지**
- **`gap` 속성 사용 금지**: 자식 margin으로 대체
- **AI가 생성한 UXML 템플릿 경로를 검증 없이 사용 금지**

### CONSIDER (상황별)

- **ListView vs 동적 슬롯 생성**: 대량 아이템(50+)은 ListView(내장 가상화), 소수는 C# 동적 Add
- **MCP Unity 서버 도입 고려**: AI IDE에서 에디터를 직접 제어하면 복사-붙여넣기 제거 가능
- **Rosalina 도입 시 partial class 설계**: AI UXML 변경 시 `.g.cs`가 재생성되므로 수동 코드는
  항상 별도 partial class 파일에 작성
- **USS 변수 C#에서 런타임 변경**: `SetProperty()` API로 테마 전환 가능 (제한적)

---

## 4. 예제 코드

### 4.1 완성 예제: 인벤토리 그리드 (UXML + USS + C#)

**InventoryGrid.uxml**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <ui:Style src="InventoryGrid.uss"/>
    <ui:VisualElement name="inventory-root" class="inventory-root">

        <!-- 헤더 -->
        <ui:VisualElement name="inventory-header" class="inventory-header">
            <ui:Label text="인벤토리" class="inventory-header__title"/>
            <ui:Label text="0 / 32" name="label-capacity"
                      class="inventory-header__capacity"/>
        </ui:VisualElement>

        <!-- 그리드 영역 -->
        <ui:ScrollView name="inventory-scroll"
                       class="inventory-scroll"
                       horizontal-scroller-visibility="Hidden"
                       vertical-scroller-visibility="Auto">
            <ui:VisualElement name="inventory-grid" class="inventory-grid"/>
        </ui:ScrollView>

        <!-- 디테일 패널 -->
        <ui:VisualElement name="item-detail" class="item-detail">
            <ui:Label text="—" name="detail-name" class="item-detail__name"/>
            <ui:Label text="아이템을 선택하세요" name="detail-desc"
                      class="item-detail__desc"/>
            <ui:Button text="사용" name="btn-use" class="item-detail__btn"/>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

**InventoryGrid.uss**

```css
/* InventoryGrid.uss */
:root {
    --inv-bg: #0D1117;
    --inv-header-bg: #161B22;
    --inv-border: #30363D;
    --inv-accent: #58A6FF;
    --inv-text: #C9D1D9;
    --inv-text-muted: #8B949E;
    --inv-slot-bg: #161B22;
    --inv-slot-hover: #21262D;
    --inv-slot-selected: #1F6FEB;
    --radius: 6px;
    --slot-size: 64px;
}

.inventory-root {
    flex-direction: column;
    background-color: var(--inv-bg);
    border-radius: var(--radius);
    border-width: 1px;
    border-color: var(--inv-border);
    min-width: 300px;
    max-width: 400px;
}

.inventory-header {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    background-color: var(--inv-header-bg);
    padding: 8px 12px;
    border-bottom-width: 1px;
    border-bottom-color: var(--inv-border);
    border-top-left-radius: var(--radius);
    border-top-right-radius: var(--radius);
}

.inventory-header__title {
    color: var(--inv-text);
    font-size: 13px;
    -unity-font-style: bold;
}

.inventory-header__capacity {
    color: var(--inv-text-muted);
    font-size: 11px;
}

.inventory-scroll {
    flex-grow: 1;
    max-height: 260px;
}

.inventory-grid {
    flex-direction: row;
    flex-wrap: wrap;
    padding: 8px;
}

/* 인벤토리 슬롯 (C#에서 동적 생성) */
.inventory-slot {
    width: var(--slot-size);
    height: var(--slot-size);
    background-color: var(--inv-slot-bg);
    border-width: 1px;
    border-color: var(--inv-border);
    border-radius: 4px;
    margin: 2px;
    align-items: center;
    justify-content: flex-end;
    transition: background-color 0.1s ease;
}

.inventory-slot:hover {
    background-color: var(--inv-slot-hover);
    border-color: var(--inv-accent);
}

.inventory-slot--selected {
    background-color: var(--inv-slot-selected);
    border-color: var(--inv-accent);
}

.inventory-slot__icon {
    position: absolute;
    top: 4px;
    left: 4px;
    right: 4px;
    bottom: 16px;
    -unity-background-scale-mode: scale-to-fit;
}

.inventory-slot__qty {
    font-size: 10px;
    color: var(--inv-text);
    -unity-text-align: lower-right;
    padding-right: 3px;
    padding-bottom: 2px;
}

/* 디테일 패널 */
.item-detail {
    flex-direction: column;
    padding: 10px 12px;
    border-top-width: 1px;
    border-top-color: var(--inv-border);
    min-height: 80px;
}

.item-detail__name {
    color: var(--inv-text);
    font-size: 13px;
    -unity-font-style: bold;
    margin-bottom: 4px;
}

.item-detail__desc {
    color: var(--inv-text-muted);
    font-size: 11px;
    flex-grow: 1;
}

.item-detail__btn {
    background-color: var(--inv-accent);
    color: #0D1117;
    border-width: 0;
    border-radius: 4px;
    padding: 6px 12px;
    font-size: 12px;
    -unity-font-style: bold;
    margin-top: 6px;
    align-self: flex-end;
    transition: opacity 0.1s ease;
}

.item-detail__btn:hover { opacity: 0.85; }
.item-detail__btn:active { opacity: 0.7; }
```

**InventoryGridController.cs**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryGridController : MonoBehaviour
{
    [SerializeField] UIDocument _document;

    // Cached references
    VisualElement _grid;
    Label _labelCapacity;
    Label _detailName;
    Label _detailDesc;
    Button _btnUse;

    readonly List<VisualElement> _slots = new();
    int _selectedIndex = -1;

    // ---

    void OnEnable()
    {
        var root = _document.rootVisualElement;

        _grid          = root.Q<VisualElement>("inventory-grid");
        _labelCapacity = root.Q<Label>("label-capacity");
        _detailName    = root.Q<Label>("detail-name");
        _detailDesc    = root.Q<Label>("detail-desc");
        _btnUse        = root.Q<Button>("btn-use");

        _btnUse?.RegisterCallback<ClickEvent>(OnUseClicked);
    }

    void OnDisable()
    {
        _btnUse?.UnregisterCallback<ClickEvent>(OnUseClicked);
        _slots.Clear();
    }

    // --- Public API ---

    public void SetupSlots(int totalSlots, int usedSlots)
    {
        _grid.Clear();
        _slots.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            int index = i; // 클로저 캡처
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");

            var icon = new VisualElement();
            icon.AddToClassList("inventory-slot__icon");

            var qty = new Label { text = "" };
            qty.AddToClassList("inventory-slot__qty");

            slot.Add(icon);
            slot.Add(qty);

            slot.RegisterCallback<ClickEvent>(_ => SelectSlot(index));
            _grid.Add(slot);
            _slots.Add(slot);
        }

        _labelCapacity.text = $"{usedSlots} / {totalSlots}";
    }

    public void SetSlotItem(int index, Sprite icon, int quantity)
    {
        if (index < 0 || index >= _slots.Count) return;
        var slot = _slots[index];
        slot.Q<VisualElement>().style.backgroundImage =
            new StyleBackground(icon);
        slot.Q<Label>().text = quantity > 1 ? quantity.ToString() : "";
    }

    // --- Private ---

    void SelectSlot(int index)
    {
        // 이전 선택 해제
        if (_selectedIndex >= 0 && _selectedIndex < _slots.Count)
            _slots[_selectedIndex].RemoveFromClassList("inventory-slot--selected");

        _selectedIndex = index;
        _slots[index].AddToClassList("inventory-slot--selected");

        // 디테일 패널 업데이트 (실제 데이터는 외부에서 주입)
        _detailName.text = $"슬롯 {index}";
        _detailDesc.text = "아이템 설명이 여기 표시됩니다";
    }

    void OnUseClicked(ClickEvent evt)
    {
        if (_selectedIndex < 0) return;
        Debug.Log($"Use item at slot {_selectedIndex}");
    }
}
```

### 4.2 USS 변수 C#에서 런타임 변경 (테마 전환)

```csharp
// USS 커스텀 속성(변수)은 직접 변경 불가 — 클래스 교체 방식 사용
public void SetDarkTheme(VisualElement root)
{
    root.RemoveFromClassList("theme--light");
    root.AddToClassList("theme--dark");
}

// USS에서:
// .theme--dark .resource-panel { background-color: #1A1A2E; }
// .theme--light .resource-panel { background-color: #F5F5F5; }
```

---

## 5. UI_Study 적용 계획

### 권장 학습 순서

이 리서치를 바탕으로 AI 보조 UI Toolkit 학습을 진행할 경우 권장 순서:

| 단계 | 예제 | AI 역할 | 학습 목표 |
|------|------|---------|-----------|
| 1 | 기본 UXML 구조 이해 | AI가 UXML 생성, 수동으로 읽기 | 네임스페이스, 요소 타입 |
| 2 | USS 다크 테마 패널 | AI가 USS 생성 + 오류 수정 실습 | USS vs CSS 차이 체득 |
| 3 | 리소스 HUD (읽기 전용) | AI 3파일 생성 + C# 연결 | UQuery, 이벤트 패턴 |
| 4 | 인벤토리 그리드 (인터랙티브) | AI UXML/USS + 수동 C# | 동적 VisualElement 생성 |
| 5 | 설정 패널 (Slider/Toggle/Dropdown) | AI 전체 생성 + 검증 | 입력 요소, 데이터 바인딩 |
| 6 | MCP 통합 실험 | Claude Code + unity-cli | AI ↔ Editor 자동화 |

### 현재 프로젝트 스택과의 관계

현재 UI_Study 스택(MV(R)P + VContainer + R3 + UniTask + DOTween)은
**UGUI 기반**으로 확정되어 있다 (doc 021 참조).

AI 보조 UI Toolkit은 다음 맥락에서 병행 탐구 가치가 있다:

- **빠른 프로토타이핑**: 신규 UI 아이디어를 UGUI로 구현하기 전 레이아웃 검증용
- **미래 대비 학습**: Unity 6에서 UI Toolkit의 런타임 지원이 강화되고 있어
  중장기 전환 가능성을 위한 선행 학습
- **에디터 도구 개발**: UI Toolkit은 에디터 확장에서 이미 표준으로, 에디터 툴 제작 시 즉시 활용 가능

### 위험 요소

- DOTween이 UI Toolkit을 지원하지 않음 (CSS transition만 사용 가능)
- VContainer + R3 통합이 수동 래퍼 필요 (doc 021 상세 설명)
- AI 생성 USS의 gap/calc 오류는 조용히 무시될 수 있어 레이아웃 버그 원인이 됨

---

## 6. 참고 자료

| 자료 | 링크 | 설명 |
|------|------|------|
| Unity 공식 UXML 구조 | [docs.unity3d.com](https://docs.unity3d.com/Manual/UIE-UXML.html) | UXML 태그/속성 레퍼런스 |
| USS 지원 속성 목록 | [docs.unity3d.com](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-USS-SupportedProperties.html) | 지원/미지원 속성 체크 |
| USS 베스트 프랙티스 | [docs.unity3d.com](https://docs.unity3d.com/Manual/UIE-USS-WritingStyleSheets.html) | BEM, inline 피하기 |
| UQuery 레퍼런스 | [docs.unity3d.com](https://docs.unity3d.com/Manual/UIE-UQuery.html) | Q<>(), QueryAll() 사용법 |
| Claude Code — App UI 플러그인 | [docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.2/manual/claude-plugin.html) | 공식 AI 통합 플러그인 |
| Rosalina (UXML → C# 자동생성) | [github.com/Eastrall/Rosalina](https://github.com/Eastrall/Rosalina) | UXML에서 partial class 생성 |
| UI Toolkit with LLMs (토론) | [Unity Discussions](https://discussions.unity.com/t/ui-toolkit-with-llms/1694843) | 커뮤니티 경험 스레드 |
| UI Toolkit vs UGUI 2025 | [Medium — Angry Shark Studio](https://medium.com/@studio.angry.shark/unity-ui-toolkit-vs-ugui-2025-developer-guide-8407312c91ed) | 성능/DX 비교 |
| UI Toolkit 실망 포인트 | [mortoray.com](https://mortoray.com/my-disappointment-with-unity-uitoolkit/) | USS 한계 정직한 평가 |
| FlexBuilder 2024 가이드 | [flexbuilder.ninja](https://flexbuilder.ninja/2024/04/12/2024-guide-to-uitoolkit-for-unity-games/) | 실전 게임 UI 레이아웃 |
| LogLog Games — UI Toolkit 첫걸음 | [loglog.games](https://loglog.games/blog/unity-ui-toolkit-first-steps/) | 런타임 패턴 실전 코드 |
| UXLora (AI → UXML 도구) | [uxlora.app](https://www.uxlora.app/) | AI 전용 UXML/USS 생성 SaaS |
| Unity MCP (CoplayDev) | [github.com/CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | AI IDE → Unity 에디터 제어 |
| USS Custom Properties 가이드 | [docs.unity3d.com](https://docs.unity3d.com/Manual/UIE-USS-CustomProperties.html) | 변수 정의 및 테마 패턴 |
| Unity AI Tools 현황 (2025) | [Unity Discussions](https://discussions.unity.com/t/unity-ai-coding-tools-current-state-june-2025/1664497) | AI 도구 커뮤니티 평가 |
| Unity SKILL.md — UI Toolkit | [github.com/Besty0728](https://github.com/Besty0728/Unity-Skills/blob/main/SkillsForUnity/unity-skills~/skills/uitoolkit/SKILL.md) | AI 스킬 레퍼런스 문서 |

---

## 7. 미해결 질문

1. **USS `gap` 속성 지원 시기**: Unity 6 로드맵에 포함됐으나 정확한 버전 미정.
   지원 시 AI 생성 USS 정확도가 크게 향상될 것으로 예상.

2. **DOTween + UI Toolkit 연동 가능성**: DOTween은 공식적으로 미지원이지만,
   `VisualElement.style` 속성을 DOTween의 `To()` 커스텀 플러그인으로 트위닝하는
   서드파티 시도가 있음 — 안정성/성능 검증 필요.

3. **R3 UI Toolkit 공식 Observable 제공 여부**: R3의 UI Toolkit 전용
   `RegisterCallback` 래퍼가 공식 확장으로 추가될 가능성 — 향후 모니터링 필요.

4. **AI 생성 USS 품질의 모델별 차이**: Claude Sonnet vs GPT-4o vs Gemini Pro 비교
   실험 데이터 없음 — 실제 실험이 필요.

5. **UXLora 출시 후 품질 평가**: Q2 2026 출시 예정 도구로 실제 게임 UI 생성 품질 미검증.

6. **MCP Unity 서버 + UI Toolkit 자동화 안정성**: UXML 파일을 MCP 통해 직접 쓰고
   에디터에서 자동 리임포트하는 워크플로의 안정성 (핫 리로드 타이밍 이슈) 미검증.
