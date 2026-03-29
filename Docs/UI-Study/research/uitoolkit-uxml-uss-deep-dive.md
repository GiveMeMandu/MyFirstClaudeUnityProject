# UI Toolkit UXML/USS 심층 분석

- **작성일**: 2026-03-29
- **카테고리**: technology
- **상태**: 조사완료

---

## 1. 요약

UXML은 HTML과 유사한 XML 기반 UI 구조 정의 언어로, 두 네임스페이스(UnityEngine.UIElements 런타임 / UnityEditor.UIElements 에디터 전용)를 구분하여 사용한다. USS는 CSS 서브셋에 Unity 전용 `-unity-` 접두사 속성을 추가한 스타일시트로, 선택자 특수성 규칙이 CSS와 동일하게 적용된다. USS 전환(transition)은 웹 CSS와 동일한 문법을 지원하며 pseudo-class와 조합해 코드 없는 상태 애니메이션을 구현할 수 있다. UQuery는 DOM의 querySelector 패턴을 Unity에서 구현한 것으로, 성능을 위해 쿼리 결과를 초기화 시점에 캐싱해야 한다.

---

## 2. 상세 분석

### 2.1 UXML 기본 구조와 문법

**루트 요소와 네임스페이스**

```xml
<?xml version="1.0" encoding="utf-8"?>
<engine:UXML
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:engine="UnityEngine.UIElements"
    xmlns:editor="UnityEditor.UIElements"
    xsi:noNamespaceSchemaLocation="../../UIElementsSchema/UIElements.xsd"
>
    <!-- 런타임 요소: engine: 접두사 사용 -->
    <engine:Button text="Click Me" name="my-button"/>
</engine:UXML>
```

**네임스페이스 단순화 (권장 패턴)**

`xmlns="UnityEngine.UIElements"` 를 기본 네임스페이스로 설정하면 접두사 없이 사용 가능:

```xml
<?xml version="1.0" encoding="utf-8"?>
<UXML xmlns="UnityEngine.UIElements"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <!-- 접두사 없이 직접 사용 -->
    <VisualElement name="root" class="container">
        <Label text="Hello World" name="title"/>
        <Button text="Start" name="start-btn"/>
    </VisualElement>
</UXML>
```

**VisualElement 기본 속성**

| 속성 | 타입 | 역할 |
|------|------|------|
| `name` | string | UQuery용 고유 식별자 (#name) |
| `class` | string (공백 구분) | USS 클래스 (.class) |
| `style` | 인라인 CSS | 직접 스타일 적용 |
| `picking-mode` | Position \| Ignore | 마우스 이벤트 수신 여부 |
| `tabindex` | int | 키보드 탭 순서 |
| `focusable` | bool | 포커스 가능 여부 |
| `tooltip` | string | 호버 시 툴팁 텍스트 |
| `view-data-key` | string | 상태 직렬화 키 |

**주요 컨트롤 속성**

```xml
<!-- Label -->
<Label text="Score: 0" name="score-label"/>

<!-- Button -->
<Button text="Confirm" name="confirm-btn"/>

<!-- Toggle -->
<Toggle label="Enable Sound" value="true" name="sound-toggle"/>

<!-- TextField -->
<TextField label="Player Name" value="" max-length="20" name="name-input"/>

<!-- Slider (float) -->
<Slider label="Volume" low-value="0" high-value="1" value="0.8" name="volume-slider"/>

<!-- SliderInt (int) -->
<SliderInt label="Difficulty" low-value="1" high-value="5" value="3" name="diff-slider"/>

<!-- DropdownField -->
<DropdownField label="Resolution" choices="1920x1080,1280x720,960x540"
               index="0" name="res-dropdown"/>

<!-- ProgressBar -->
<ProgressBar value="0.6" title="Loading..." name="progress"/>

<!-- ScrollView -->
<ScrollView name="item-list" mode="Vertical">
    <!-- 스크롤 가능한 콘텐츠 -->
</ScrollView>
```

**USS 파일 연결**

```xml
<UXML xmlns="UnityEngine.UIElements">
    <!-- 스타일시트 연결 -->
    <Style src="project://database/Assets/UI/Styles/MainMenu.uss"/>
    <!-- 또는 상대 경로 -->
    <Style src="../Styles/MainMenu.uss"/>

    <VisualElement class="container">
        <Label text="Title" class="title-text"/>
    </VisualElement>
</UXML>
```

### 2.2 UXML 템플릿과 인스턴스

템플릿은 UXML을 재사용 컴포넌트처럼 활용하는 핵심 패턴이다.

**기본 템플릿 정의 (ItemRow.uxml)**

```xml
<!-- Assets/UI/Templates/ItemRow.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="item-row" class="item-row">
        <VisualElement name="item-icon" class="item-icon"/>
        <Label name="item-name" class="item-name" text="Item Name"/>
        <Label name="item-count" class="item-count" text="x1"/>
        <Button name="item-action" class="item-action" text="Use"/>
    </VisualElement>
</UXML>
```

**템플릿 임포트 및 인스턴스 생성**

```xml
<!-- Assets/UI/InventoryPanel.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <!-- 템플릿 선언 -->
    <Template src="project://database/Assets/UI/Templates/ItemRow.uxml"
              name="ItemRow"/>

    <ScrollView name="inventory-scroll">
        <!-- 인스턴스 생성 — 다른 이름으로 여러 개 -->
        <Instance template="ItemRow" name="item-slot-0"/>
        <Instance template="ItemRow" name="item-slot-1"/>
        <Instance template="ItemRow" name="item-slot-2"/>
    </ScrollView>
</UXML>
```

**AttributeOverrides — 인스턴스별 값 커스터마이징**

```xml
<UXML xmlns="UnityEngine.UIElements">
    <Template src="PlayerCard.uxml" name="PlayerCard"/>

    <Instance template="PlayerCard" name="player-alice">
        <!-- element-name: 수정할 자식 요소의 name 속성 -->
        <AttributeOverrides element-name="player-name-label" text="Alice"/>
        <AttributeOverrides element-name="player-level-label" text="Lv.42"/>
    </Instance>

    <Instance template="PlayerCard" name="player-bob">
        <AttributeOverrides element-name="player-name-label" text="Bob"/>
        <AttributeOverrides element-name="player-level-label" text="Lv.15"/>
    </Instance>
</UXML>
```

AttributeOverrides 제약사항:
- `class`, `name`, `style` 속성은 오버라이드 불가
- `binding-path` 지원하지만 데이터 바인딩 동작은 안 함
- 중첩 오버라이드 시 얕은(상위) 오버라이드가 우선

**content-container — 자식 삽입 지점 지정**

```xml
<!-- GroupBox.uxml — content-container로 자식 위치 지정 -->
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="group-box" class="group-box">
        <Label name="group-title" class="group-title" text="Group"/>
        <!-- content-container 마크된 요소에 자식이 삽입됨 -->
        <VisualElement name="group-content"
                       content-container="true"
                       class="group-content"/>
    </VisualElement>
</UXML>

<!-- 사용 시: Label이 group-content 안에 들어감 -->
<Instance template="GroupBox" name="my-group">
    <Label text="Child 1"/>
    <Label text="Child 2"/>
</Instance>
```

**C#에서 UXML 인스턴스화**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VisualTreeAsset _itemRowTemplate; // Inspector에서 드래그

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        var container = root.Q<VisualElement>("item-container");

        // 방법 1: VisualTreeAsset.Instantiate()
        var instance = _itemRowTemplate.Instantiate();
        instance.Q<Label>("item-name").text = "Health Potion";
        container.Add(instance);

        // 방법 2: CloneTree() — 기존 부모에 직접 복제
        _itemRowTemplate.CloneTree(container);
    }
}
```

**Resources에서 UXML 로드**

```csharp
// Resources/UI/ItemRow.uxml 에 위치한 경우
var asset = Resources.Load<VisualTreeAsset>("UI/ItemRow");
var instance = asset.Instantiate();
```

### 2.3 USS 속성 레퍼런스

**박스 모델 / 레이아웃**

```css
/* 크기 */
width: 200px;
height: 100px;
min-width: 50px;
max-width: 500px;
min-height: 20px;
max-height: 300px;
width: 50%;          /* 부모 기준 퍼센트 */
width: auto;         /* 내용에 맞게 자동 */

/* 여백 */
margin: 10px;                     /* 4방향 동일 */
margin: 10px 20px;                /* 상하 | 좌우 */
margin: 10px 20px 5px 15px;       /* 상 우 하 좌 */
margin-top: 10px;
margin-right: 20px;
margin-bottom: 10px;
margin-left: 20px;
margin: auto;                     /* 중앙 정렬 */

/* 패딩 */
padding: 8px 16px;
padding-top: 8px;

/* 테두리 너비 */
border-width: 2px;
border-top-width: 1px;
border-right-width: 1px;

/* 테두리 반경 */
border-radius: 8px;
border-top-left-radius: 12px;
border-bottom-right-radius: 4px;

/* 테두리 색 */
border-color: #333333;
border-top-color: rgba(255, 255, 255, 0.5);
```

**Flexbox 레이아웃**

```css
/* 방향 */
flex-direction: column;          /* 기본값: 세로 */
flex-direction: row;             /* 가로 */
flex-direction: row-reverse;
flex-direction: column-reverse;

/* 줄 바꿈 */
flex-wrap: nowrap;               /* 기본값 */
flex-wrap: wrap;
flex-wrap: wrap-reverse;

/* 주축 정렬 */
justify-content: flex-start;    /* 기본값 */
justify-content: flex-end;
justify-content: center;
justify-content: space-between;
justify-content: space-around;

/* 교차축 정렬 */
align-items: stretch;            /* 기본값 */
align-items: flex-start;
align-items: flex-end;
align-items: center;

/* 개별 요소 교차축 정렬 */
align-self: auto;
align-self: flex-start;
align-self: center;

/* 크기 유연성 */
flex-grow: 1;                    /* 남는 공간 비율로 차지 */
flex-shrink: 1;                  /* 공간 부족 시 축소 비율 */
flex-basis: auto;                /* 기본 크기 */
flex: 1;                         /* grow:1 shrink:1 basis:0 단축 */
flex: 0 0 auto;                  /* 크기 고정 */

/* 위치 모드 */
position: relative;              /* 기본값: 레이아웃 흐름 참여 */
position: absolute;              /* 레이아웃에서 제외, 부모 기준 배치 */
left: 10px;
top: 20px;
right: 0;
bottom: 0;
```

**배경과 이미지**

```css
background-color: #1a1a2e;
background-color: rgb(26, 26, 46);
background-color: rgba(0, 0, 0, 0.8);

/* 배경 이미지 */
background-image: url("project://database/Assets/UI/Textures/bg.png");
background-image: resource("UI/Textures/bg");   /* Resources 폴더 */

/* Unity 전용: 배경 이미지 스케일 모드 */
-unity-background-scale-mode: stretch-to-fill;
-unity-background-scale-mode: scale-and-crop;
-unity-background-scale-mode: scale-to-fit;

/* 배경 틴트 */
-unity-background-image-tint-color: #ff8800;

/* 9-슬라이스 (Sprite 테두리 사용) */
-unity-slice-left: 10;
-unity-slice-right: 10;
-unity-slice-top: 10;
-unity-slice-bottom: 10;

/* 배경 위치/크기 (Unity 6) */
background-size: cover;
background-size: contain;
background-position: center;
```

**텍스트 / 폰트**

```css
/* 표준 CSS 텍스트 */
color: #ffffff;
color: inherit;                  /* 부모 색상 상속 */
font-size: 16px;
font-size: 1.2em;                /* 부모 기준 상대 크기 (제한적) */

/* Unity 전용 텍스트 속성 (- 모두 부모에서 상속됨) */
-unity-font: url("project://database/Assets/Fonts/Roboto-Regular.ttf");
-unity-font-definition: url("project://database/Assets/Fonts/Roboto SDF.asset");
-unity-font-style: normal;
-unity-font-style: italic;
-unity-font-style: bold;
-unity-font-style: bold-and-italic;

/* 텍스트 정렬 (9방향) */
-unity-text-align: upper-left;
-unity-text-align: upper-center;
-unity-text-align: upper-right;
-unity-text-align: middle-left;
-unity-text-align: middle-center;   /* 수평 수직 중앙 */
-unity-text-align: middle-right;
-unity-text-align: lower-left;
-unity-text-align: lower-center;
-unity-text-align: lower-right;

/* 텍스트 외곽선 */
-unity-text-outline-width: 1px;
-unity-text-outline-color: #000000;
-unity-text-outline: 1px #000000;   /* 단축 */

/* 텍스트 오버플로 */
overflow: hidden;
text-overflow: ellipsis;            /* CSS와 동일 */
-unity-text-overflow-position: end;

/* 텍스트 자동 크기 조정 */
-unity-text-auto-size: best-fit 10px 32px;  /* min max */

/* 자간/단어 간격 */
letter-spacing: 1px;
word-spacing: 4px;
-unity-paragraph-spacing: 8px;
```

**표시/가시성/불투명도**

```css
display: flex;              /* 기본값: 표시 + 레이아웃 참여 */
display: none;              /* 숨김 + 레이아웃에서 제거 */

visibility: visible;        /* 기본값 */
visibility: hidden;         /* 숨김, 레이아웃 공간은 유지 */

opacity: 1.0;               /* 완전 불투명 (기본) */
opacity: 0.5;               /* 반투명 */
opacity: 0.0;               /* 완전 투명 */

overflow: visible;          /* 기본값: 자식이 넘쳐도 표시 */
overflow: hidden;           /* 경계 밖 잘라냄 */
-unity-overflow-clip-box: padding-box;  /* 클리핑 기준 */
-unity-overflow-clip-box: content-box;
```

**변환과 필터**

```css
/* 변환 (UI Toolkit 2022.2+) */
translate: 10px 20px;
scale: 1.2 1.2;
rotate: 45deg;
transform-origin: center center;
transform-origin: 50% 50%;

/* 그림자 필터 (Unity 6) */
filter: drop-shadow(2px 4px 6px rgba(0,0,0,0.5));
```

**색상 값 형식**

```css
/* 16진수 */
color: #ffffff;      /* 흰색 */
color: #ff000080;    /* 반투명 빨강 (RRGGBBAA) */

/* rgb/rgba 함수 */
color: rgb(255, 128, 0);
color: rgba(255, 128, 0, 0.8);

/* USS 변수 (Unity 6.1+) */
:root {
    --primary-color: #4a90d9;
    --font-size-large: 24px;
}
.title {
    color: var(--primary-color);
    font-size: var(--font-size-large);
}
```

**길이 단위**

| 단위 | 의미 | 사용 예 |
|------|------|---------|
| `px` | 픽셀 (절대) | `width: 200px` |
| `%` | 부모 기준 퍼센트 | `width: 50%` |
| `auto` | 자동 (내용/레이아웃 결정) | `margin: auto` |
| `vw` | 뷰포트 너비 % | 제한적 지원 |
| `vh` | 뷰포트 높이 % | 제한적 지원 |

### 2.4 USS 선택자 (Selectors)

**선택자 유형**

```css
/* 1. 타입 선택자 — C# 클래스명으로 매칭 */
Button { background-color: #2244aa; }
Label  { color: white; }
VisualElement { margin: 4px; }

/* 2. 클래스 선택자 — class 속성 값으로 매칭 */
.primary-btn  { background-color: #4a90d9; }
.danger-btn   { background-color: #e74c3c; }
.item-row     { height: 48px; }

/* 3. 이름 선택자 — name 속성 값으로 매칭 (#) */
#health-bar   { width: 200px; height: 16px; }
#score-label  { font-size: 24px; }

/* 4. 전역 선택자 — 모든 요소 */
* { margin: 0; padding: 0; }
```

**복합 선택자**

```css
/* 자손 선택자 (공백) — 모든 하위 요소 */
.container Label { color: #cccccc; }

/* 자식 선택자 (>) — 직계 자식만 */
.item-row > Label { font-size: 14px; }

/* 다중 선택자 (,) — 여러 요소에 동일 스타일 */
Button, Toggle, Slider { margin-bottom: 8px; }

/* 타입 + 클래스 복합 */
Button.primary-btn { font-size: 16px; }

/* 클래스 + 클래스 복합 (두 클래스 모두 가진 요소) */
.item-row.selected { background-color: #334466; }
```

**Pseudo-class (의사 클래스)**

```css
/* :hover — 마우스 커서가 위에 있을 때 */
Button:hover {
    background-color: #5ba0e9;
    scale: 1.05 1.05;
    transition-property: scale;
    transition-duration: 0.1s;
}

/* :active — 상호작용 중 (버튼 누르는 중) */
Button:active {
    background-color: #3a7bc8;
    translate: 0 2px;
}

/* :inactive — 상호작용 종료 후 */
Button:inactive {
    translate: 0 0;
}

/* :focus — 포커스 받은 상태 */
TextField:focus {
    border-color: #4a90d9;
    border-width: 2px;
}

/* :disabled — 비활성화 상태 */
Button:disabled {
    opacity: 0.4;
    cursor: arrow;
}

/* :enabled — 활성화 상태 */
Button:enabled {
    cursor: link;
}

/* :checked — 체크된 상태 (Toggle, RadioButton) */
Toggle:checked { color: #4a90d9; }
Toggle:checked > .unity-toggle__checkmark { background-color: #4a90d9; }

/* :root — 스타일시트가 적용된 루트 요소 */
:root {
    -unity-font-definition: url("project://database/Assets/Fonts/Roboto SDF.asset");
    font-size: 16px;
    color: #ffffff;
}

/* 복합 pseudo-class (AND 조건) */
Toggle:checked:hover {
    background-color: rgba(74, 144, 217, 0.2);
}

Button:disabled:hover {
    /* 비활성화된 버튼에 hover 없음 */
    background-color: inherit;
}
```

**선택자 특수성(Specificity) 규칙**

우선순위 높음 → 낮음:

```
1. C# 직접 스타일 (element.style.xxx = ...)   — 최고 우선순위
2. UXML 인라인 스타일 (style="...")           — 두 번째
3. 이름 선택자 (#name)                        — 세 번째
4. 클래스 선택자 (.class)                     — 네 번째
5. 타입 선택자 (Button, Label)                — 다섯 번째
6. 전역 선택자 (*)                            — 가장 낮음
7. 상속된 스타일                              — 최저
```

동일 특수성 시:
- 같은 USS 파일: 파일 뒤쪽 선택자가 우선
- 다른 USS 파일: 요소 계층에서 더 깊이 적용된 쪽이 우선

**USS는 `!important`를 지원하지 않는다.**

### 2.5 USS 전환(Transitions)

**기본 전환 문법**

```css
.my-button {
    background-color: #2244aa;
    scale: 1 1;
    opacity: 1;

    /* 단일 속성 전환 */
    transition-property: background-color;
    transition-duration: 0.2s;
    transition-timing-function: ease-out;
    transition-delay: 0s;

    /* 단축 표기 */
    transition: background-color 0.2s ease-out 0s;
}

/* hover 상태로 전환 */
.my-button:hover {
    background-color: #4466cc;
}
```

**다중 속성 전환**

```css
.panel {
    opacity: 1;
    translate: 0 0;
    scale: 1 1;

    /* 쉼표로 구분하여 여러 속성 전환 */
    transition-property: opacity, translate, scale;
    transition-duration: 0.3s, 0.3s, 0.2s;
    transition-timing-function: ease-out, ease-out, ease-in-out;
    transition-delay: 0s, 0s, 0.05s;
}

/* all — 모든 속성에 동일 전환 적용 */
.simple-fade {
    opacity: 1;
    transition: all 0.3s ease;
}
```

**Timing Function 옵션**

```css
transition-timing-function: linear;
transition-timing-function: ease;
transition-timing-function: ease-in;
transition-timing-function: ease-out;
transition-timing-function: ease-in-out;

/* 고급 커브 */
transition-timing-function: ease-in-sine;
transition-timing-function: ease-out-sine;
transition-timing-function: ease-in-out-sine;
transition-timing-function: ease-in-cubic;
transition-timing-function: ease-out-cubic;
transition-timing-function: ease-in-out-cubic;
transition-timing-function: ease-in-quint;
transition-timing-function: ease-out-quint;
transition-timing-function: ease-in-out-quint;
transition-timing-function: ease-in-circ;
transition-timing-function: ease-out-circ;
transition-timing-function: ease-in-out-circ;
transition-timing-function: ease-in-elastic;
transition-timing-function: ease-out-elastic;
transition-timing-function: ease-in-back;
transition-timing-function: ease-out-back;
transition-timing-function: ease-in-bounce;
transition-timing-function: ease-out-bounce;
```

**애니메이션 가능 속성 분류**

| 분류 | 속성 | 동작 |
|------|------|------|
| 완전 애니메이션 | opacity, color, background-color, width, height, margin, padding, border-color, border-width, border-radius, translate, rotate, scale, transform-origin, left, top, right, bottom, flex-grow, flex-shrink, flex-basis | 부드러운 보간 |
| 이산(Discrete) | display, visibility, flex-direction, position, background-image, -unity-background-scale-mode | 즉각 변경 |
| 비애니메이션 | transition 속성 자체 | 전환 없음 |

**퍼포먼스 팁: Transform 속성 우선 사용**

```css
/* 권장 — transform은 재레이아웃 없이 GPU에서 처리 */
.fast-anim {
    transition: translate 0.3s ease, scale 0.3s ease, opacity 0.3s ease;
}
.fast-anim:hover {
    translate: 0 -4px;
    scale: 1.02 1.02;
    opacity: 0.9;
}

/* 주의 — width/height 변경은 매 프레임 레이아웃 재계산 */
.slow-anim {
    transition: width 0.3s ease;  /* 성능 영향 있음 */
}
```

**전환 이벤트 (C#)**

```csharp
var element = root.Q<VisualElement>("my-panel");

// 전환 시작 시
element.RegisterCallback<TransitionStartEvent>(evt =>
{
    Debug.Log($"Transition started: {evt.stylePropertyNames}");
});

// 전환 완료 시
element.RegisterCallback<TransitionEndEvent>(evt =>
{
    Debug.Log($"Transition ended: {evt.stylePropertyNames}");
});

// 전환 취소 시
element.RegisterCallback<TransitionCancelEvent>(evt =>
{
    Debug.Log("Transition cancelled");
});
```

**씬 첫 프레임 전환 주의사항**

```css
/* 문제: 씬 로드 첫 프레임에는 이전 상태가 없어 전환이 발생하지 않음 */
/* 해결: 초기값을 명시적으로 설정 */
.slide-panel {
    left: -300px;  /* auto가 아닌 명시적 값 필수 */
    transition: left 0.4s ease-out;
}
.slide-panel--open {
    left: 0;
}
```

**실용 전환 패턴**

```css
/* 1. 페이드 인/아웃 */
.fade-panel {
    opacity: 0;
    transition: opacity 0.3s ease;
}
.fade-panel--visible { opacity: 1; }

/* 2. 슬라이드 인 (위에서) */
.slide-from-top {
    translate: 0 -100%;
    transition: translate 0.4s ease-out;
}
.slide-from-top--open { translate: 0 0; }

/* 3. 스케일 팝업 */
.popup {
    scale: 0.8 0.8;
    opacity: 0;
    transition: scale 0.2s ease-out, opacity 0.2s ease;
}
.popup--open {
    scale: 1 1;
    opacity: 1;
}

/* 4. 버튼 호버 리프트 */
.game-button {
    translate: 0 0;
    transition: translate 0.1s ease-out, background-color 0.15s ease;
}
.game-button:hover {
    translate: 0 -3px;
    background-color: #5ba0e9;
}
.game-button:active {
    translate: 0 1px;
}
```

### 2.6 UQuery API

UQuery는 DOM의 querySelector/querySelectorAll 패턴을 Unity에서 구현한 것이다.

**Q<T>() — 단일 요소 조회**

```csharp
// Q() = Query<T>().First() 의 단축 표기
var root = document.rootVisualElement;

// 이름으로 조회 (#name)
var btn = root.Q<Button>("start-button");         // 이름 + 타입
var lbl = root.Q("score-label") as Label;         // 이름만 (타입 캐스트 필요)
var ve  = root.Q<VisualElement>("my-container");

// 클래스로 조회
var first = root.Q(className: "item-row");
var typed = root.Q<VisualElement>(className: "item-row");

// 복합 조건
var specific = root.Q<Button>(name: "confirm", className: "primary-btn");
```

**Query<T>() — 다중 요소 조회**

```csharp
// 모든 Button 조회
var allButtons = root.Query<Button>().ToList();

// 특정 클래스의 모든 요소
var allRows = root.Query(className: "item-row").ToList();

// 메서드 체이닝
var result = root.Query<Button>()
                 .Where(b => b.enabledSelf)  // 활성화된 버튼만
                 .First();

// 인덱스 접근
var thirdButton = root.Query<Button>().AtIndex(2);

// ForEach 적용
root.Query<Label>(className: "stat-value").ForEach(label =>
{
    label.text = "0";
});

// QueryState (List 할당 없음 — 성능 최적화)
var queryState = root.Query<Button>(className: "action-btn");
foreach (var btn in queryState)
{
    btn.SetEnabled(false);
}
```

**계층적 쿼리 (중첩 탐색)**

```csharp
// 특정 컨테이너 내부에서만 쿼리
var panel = root.Q<VisualElement>("inventory-panel");
var items = panel.Query<VisualElement>(className: "item-slot").ToList();

// 부모 탐색 (UQuery 미지원 — 수동 순회 필요)
var element = root.Q("some-element");
VisualElement parent = element.parent;
while (parent != null)
{
    if (parent.ClassListContains("target-class"))
        break;
    parent = parent.parent;
}
```

**성능 최적화 패턴**

```csharp
// 잘못된 패턴 — Update마다 쿼리
private void Update()
{
    // 매 프레임 쿼리 = 성능 낭비
    var label = root.Q<Label>("score");
    label.text = score.ToString();
}

// 올바른 패턴 — OnEnable에서 캐싱
private Label _scoreLabel;
private Button _pauseButton;

private void OnEnable()
{
    var root = GetComponent<UIDocument>().rootVisualElement;
    // 한 번만 쿼리하여 필드에 저장
    _scoreLabel  = root.Q<Label>("score");
    _pauseButton = root.Q<Button>("pause");
}

private void UpdateScore(int newScore)
{
    _scoreLabel.text = newScore.ToString(); // 캐시된 참조 사용
}

// List 할당 없는 QueryState 열거
private void ResetAllValues()
{
    var query = _root.Query<Label>(className: "stat-value");
    // ToList() 없이 직접 열거 — 힙 할당 최소화
    foreach (var label in query)
        label.text = "-";
}
```

### 2.7 커스텀 VisualElement

**Unity 6 방식 — UxmlElement + UxmlAttribute**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

// [UxmlElement] 속성 + partial 클래스 필수
[UxmlElement]
public partial class ResourceBar : VisualElement
{
    // UXML 속성으로 노출
    [UxmlAttribute("label-text")]
    public string labelText { get; set; } = "Resource";

    [UxmlAttribute("max-value")]
    public float maxValue { get; set; } = 100f;

    [UxmlAttribute("current-value")]
    public float currentValue { get; set; } = 100f;

    [UxmlAttribute("bar-color")]
    public Color barColor { get; set; } = Color.green;

    // 내부 요소
    private Label _label;
    private VisualElement _fillBar;

    public ResourceBar()
    {
        // 클래스 추가 (USS 스타일링용)
        AddToClassList("resource-bar");

        // 내부 구조 생성
        _label = new Label();
        _label.AddToClassList("resource-bar__label");

        var track = new VisualElement();
        track.AddToClassList("resource-bar__track");

        _fillBar = new VisualElement();
        _fillBar.AddToClassList("resource-bar__fill");

        track.Add(_fillBar);
        Add(_label);
        Add(track);

        // 라이프사이클 이벤트
        RegisterCallback<AttachToPanelEvent>(_ => Refresh());
    }

    // 외부에서 값 변경 시 호출
    public void SetValue(float current, float max)
    {
        currentValue = current;
        maxValue = max;
        Refresh();
    }

    private void Refresh()
    {
        _label.text = labelText;
        float ratio = maxValue > 0 ? currentValue / maxValue : 0f;
        _fillBar.style.width = Length.Percent(ratio * 100f);
        _fillBar.style.backgroundColor = barColor;

        // 상태별 USS 클래스
        _fillBar.EnableInClassList("resource-bar__fill--low",      ratio < 0.3f);
        _fillBar.EnableInClassList("resource-bar__fill--critical", ratio < 0.15f);
    }
}
```

UXML에서 사용:
```xml
<UXML xmlns="UnityEngine.UIElements">
    <!-- 커스텀 요소 — 등록된 이름으로 사용 -->
    <ResourceBar label-text="HP" max-value="100" current-value="75"
                 bar-color="#00cc44" name="hp-bar"/>
    <ResourceBar label-text="MP" max-value="50" current-value="30"
                 bar-color="#4488ff" name="mp-bar"/>
</UXML>
```

**generateVisualContent — 커스텀 그래픽**

```csharp
[UxmlElement]
public partial class RadialProgressBar : VisualElement
{
    [UxmlAttribute]
    public float progress { get; set; } = 0.75f; // 0~1

    [UxmlAttribute]
    public float lineWidth { get; set; } = 8f;

    [UxmlAttribute]
    public Color backgroundColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 1f);

    [UxmlAttribute]
    public Color fillColor { get; set; } = Color.cyan;

    public RadialProgressBar()
    {
        // generateVisualContent 델리게이트 등록
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        float cx = contentRect.width * 0.5f;
        float cy = contentRect.height * 0.5f;
        float radius = Mathf.Min(cx, cy) - lineWidth;

        var painter = mgc.painter2D;

        // 배경 원 (트랙)
        painter.strokeColor = backgroundColor;
        painter.lineWidth = lineWidth;
        painter.lineCap = LineCap.Round;
        painter.BeginPath();
        painter.Arc(new Vector2(cx, cy), radius, 0f, 360f);
        painter.Stroke();

        // 진행 호
        float endAngle = -90f + (progress * 360f);
        painter.strokeColor = fillColor;
        painter.lineWidth = lineWidth;
        painter.BeginPath();
        painter.Arc(new Vector2(cx, cy), radius, -90f, endAngle);
        painter.Stroke();
    }

    // 값 변경 후 수동으로 재그리기 요청
    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        MarkDirtyRepaint(); // generateVisualContent 재호출 트리거
    }
}
```

**레거시 방식 — UxmlFactory + UxmlTraits (Unity 2021~2023)**

```csharp
// Unity 6 이전 방식 — 참고용
public class OldStyleElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<OldStyleElement, UxmlTraits> {}

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlStringAttributeDescription _title = new() { name = "title", defaultValue = "Default" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            ((OldStyleElement)ve).title = _title.GetValueFromBag(bag, cc);
        }
    }

    public string title { get; set; }
}
```

### 2.8 USS 고급 패턴

**BEM 방식 클래스 네이밍 (권장)**

```css
/* Block */
.health-bar { }

/* Element (Block__Element) */
.health-bar__track { }
.health-bar__fill { }
.health-bar__label { }

/* Modifier (Block--Modifier) */
.health-bar--low { }
.health-bar--critical { }
.health-bar__fill--low { background-color: #ff8800; }
.health-bar__fill--critical { background-color: #ff2222; }
```

**USS 변수 (CSS Custom Properties, Unity 6.1+)**

```css
/* UI Builder에서 편집 가능한 변수 정의 */
:root {
    /* 색상 팔레트 */
    --color-primary: #4a90d9;
    --color-danger: #e74c3c;
    --color-success: #27ae60;
    --color-bg-dark: #1a1a2e;
    --color-bg-panel: #16213e;
    --color-text: #eaeaea;
    --color-text-muted: #888888;

    /* 간격 체계 */
    --spacing-xs: 4px;
    --spacing-sm: 8px;
    --spacing-md: 16px;
    --spacing-lg: 24px;
    --spacing-xl: 32px;

    /* 반경 */
    --border-radius-sm: 4px;
    --border-radius-md: 8px;
    --border-radius-lg: 16px;
}

.panel {
    background-color: var(--color-bg-panel);
    padding: var(--spacing-md);
    border-radius: var(--border-radius-md);
}

.btn-primary {
    background-color: var(--color-primary);
    padding: var(--spacing-sm) var(--spacing-md);
}
```

**Unity 내장 컨트롤 하위 요소 스타일링**

Unity 빌트인 컨트롤의 내부 요소는 `.unity-` 접두사 클래스를 갖는다:

```css
/* Button 내부 텍스트 */
.unity-button__text { font-size: 14px; }

/* Toggle 체크박스 */
.unity-toggle__checkmark { background-color: #4a90d9; }
.unity-toggle__text      { color: white; }

/* Slider 트랙 */
.unity-base-slider__tracker   { background-color: #333; }
.unity-base-slider__dragger   { background-color: #4a90d9; }

/* TextField 입력 영역 */
.unity-text-field__input       { background-color: #2a2a2a; }
.unity-text-element--inner     { color: white; }

/* ListView 아이템 */
.unity-list-view__item          { height: 40px; }
.unity-list-view__item--selected { background-color: #334466; }

/* ScrollView 스크롤바 */
.unity-scroll-view__vertical-scroller   { }
.unity-base-slider__drag-container      { }
```

---

## 3. 베스트 프랙티스

### DO (권장)

- **UXML으로 구조 정의, USS로 스타일, C#으로 로직 분리**: 관심사 분리 원칙 준수
- **:root 선택자로 전역 폰트/색상 설정**: 일관된 테마 적용
- **AddToClassList/RemoveFromClassList로 상태 전환**: USS transition 자동 활성화
- **UQuery 결과 OnEnable에서 캐싱**: Update 등 반복 호출 부위에서 재조회 금지
- **QueryState 사용으로 List 할당 최소화**: `.ToList()` 대신 직접 foreach 열거
- **transform 속성으로 전환 애니메이션**: width/height 전환보다 레이아웃 재계산 없음
- **BEM 네이밍 컨벤션 채택**: `.block__element--modifier` 패턴으로 충돌 방지
- **USS 파일을 용도별로 분리**: `base.uss` (전역 변수), `components.uss` (컴포넌트), `views.uss` (화면별)
- **Unity 6 UxmlElement + partial class 패턴**: 커스텀 컨트롤은 신규 방식으로 작성
- **전환 초기값을 auto가 아닌 명시적 px로 설정**: 첫 프레임 전환 오작동 방지

### DON'T (금지)

- **C# 직접 style 설정과 USS 혼용 금지**: C# 스타일은 USS를 항상 덮어씀
- **:hover 선택자를 수많은 자손을 가진 요소에 적용 금지**: 매 마우스 이동마다 전체 하위 계층 무효화
- **Update에서 Q<T>() 반복 조회 금지**: 초기화 때 캐싱 후 캐시 사용
- **generateVisualContent 콜백 내부에서 VisualElement 속성 변경 금지**: 무한 재그리기 루프
- **에디터 전용 USS 속성을 런타임 USS에 사용 금지**: 런타임 빌드에서 무시됨
- **CSS grid 레이아웃 시도 금지**: UI Toolkit은 Flexbox만 지원, grid 없음
- **VisualElement.style에 % 값과 전환 동시 사용 주의**: 일부 경우 예상치 못한 동작

### CONSIDER (상황별)

- **filter: drop-shadow 주의**: Unity 6 기능, 모든 플랫폼에서 성능 검증 필요
- **transition: all 사용 자제**: 불필요한 속성까지 감시, 명시적 속성 지정이 더 효율적
- **USS 변수(--var) 활용**: Unity 6.1+에서 테마 시스템 구축 시 유용
- **9-슬라이스(-unity-slice-*)**: 버튼/패널 배경 텍스처에 활용, Sprite의 Border 설정 필요
- **MarkDirtyRepaint()**: generateVisualContent 사용 요소의 값 변경 후 명시적 호출 필요

---

## 4. UGUI 대비 매핑표 (UXML/USS 관점)

| UGUI 개념 | UXML/USS 대응 | 코드/문법 |
|-----------|--------------|----------|
| Prefab (UI용) | UXML + Template/Instance | `<Template src="Foo.uxml"/>` + `<Instance template="Foo"/>` |
| Prefab 변수 노출 | UxmlAttribute | `[UxmlAttribute] public string title { get; set; }` |
| Inspector SerializeField | UxmlAttribute | UXML 속성으로 에디터에서 설정 |
| SetActive(false) | `display: none` | CSS와 동일 |
| CanvasGroup.alpha | `opacity: 0` | `transition: opacity 0.3s ease` |
| Animator (UI) | USS transition | `:hover`, `AddToClassList`로 트리거 |
| Image.color tint | `-unity-background-image-tint-color` | `#ff8800` 등 |
| Image (Sliced) | `-unity-slice-left/right/top/bottom` | 정수값 (픽셀) |
| Text.fontSize | `font-size: 24px` | 표준 CSS |
| Text.font | `-unity-font` 또는 `-unity-font-definition` | SDF 폰트에는 `-unity-font-definition` 사용 |
| Text.alignment | `-unity-text-align: middle-center` | 9방향 값 |
| Text.color | `color: #ffffff` | 표준 CSS, 상속됨 |
| Outline (Text) | `-unity-text-outline: 1px #000` | Unity 전용 |
| VerticalLayoutGroup | `flex-direction: column` | 기본값이므로 생략 가능 |
| HorizontalLayoutGroup | `flex-direction: row` | |
| GridLayoutGroup | 없음 (flex-wrap으로 근사) | CSS grid 미지원 |
| LayoutGroup.spacing | `margin` 또는 `gap` | 자식 margin으로 간격 조정 |
| ContentSizeFitter | 자동 (기본 동작) | 별도 설정 불필요 |
| LayoutElement.minWidth | `min-width: 100px` | 표준 CSS |
| LayoutElement.flexibleWidth | `flex-grow: 1` | |
| RectTransform.anchoredPosition | `position: absolute; left: x; top: y` | |
| RectTransform.sizeDelta | `width: Wpx; height: Hpx` | |
| RectTransform.anchorMin/Max | `position: relative` + flex | anchor 개념 없음 |
| scrollRect.normalizedPosition | ScrollView.scrollOffset | C# API |
| GetComponent<Text>().text = | label.text = | UQuery로 찾은 후 설정 |
| onClick.AddListener | clicked += 또는 RegisterCallback<ClickEvent> | |
| onValueChanged.AddListener | RegisterValueChangedCallback | Toggle, Slider, TextField 등 |

---

## 5. 예제 코드

### 기본 사용법

**완전한 UXML 화면 예제 (HUD)**

```xml
<?xml version="1.0" encoding="utf-8"?>
<UXML xmlns="UnityEngine.UIElements">
    <Style src="project://database/Assets/UI/Styles/HUD.uss"/>

    <VisualElement name="hud-root" class="hud">

        <!-- 상단 자원 바 -->
        <VisualElement name="resource-bar" class="resource-bar">
            <VisualElement class="resource-group">
                <VisualElement class="resource-icon resource-icon--gold"/>
                <Label name="gold-label" class="resource-value" text="0"/>
            </VisualElement>
            <VisualElement class="resource-group">
                <VisualElement class="resource-icon resource-icon--food"/>
                <Label name="food-label" class="resource-value" text="0"/>
            </VisualElement>
        </VisualElement>

        <!-- 하단 액션 버튼 -->
        <VisualElement name="action-bar" class="action-bar">
            <Button name="build-btn" class="action-btn action-btn--build" text="Build"/>
            <Button name="recruit-btn" class="action-btn action-btn--recruit" text="Recruit"/>
            <Button name="research-btn" class="action-btn action-btn--research" text="Research"/>
        </VisualElement>

    </VisualElement>
</UXML>
```

**대응 USS 파일 (HUD.uss)**

```css
/* HUD.uss */

.hud {
    width: 100%;
    height: 100%;
    position: absolute;
    top: 0;
    left: 0;
    pointer-events: none; /* HUD는 클릭 통과 */
}

/* 자원 바 */
.resource-bar {
    flex-direction: row;
    justify-content: center;
    align-items: center;
    position: absolute;
    top: 16px;
    left: 0;
    right: 0;
    pointer-events: none;
}

.resource-group {
    flex-direction: row;
    align-items: center;
    background-color: rgba(0, 0, 0, 0.7);
    border-radius: 20px;
    padding: 6px 16px;
    margin: 0 8px;
}

.resource-icon {
    width: 24px;
    height: 24px;
    margin-right: 8px;
    -unity-background-scale-mode: scale-to-fit;
}

.resource-icon--gold {
    background-image: resource("UI/Icons/gold");
    -unity-background-image-tint-color: #ffd700;
}

.resource-icon--food {
    background-image: resource("UI/Icons/food");
    -unity-background-image-tint-color: #90ee90;
}

.resource-value {
    color: #ffffff;
    font-size: 18px;
    -unity-font-style: bold;
    -unity-text-align: middle-left;
}

/* 액션 바 */
.action-bar {
    flex-direction: row;
    justify-content: center;
    position: absolute;
    bottom: 24px;
    left: 0;
    right: 0;
    pointer-events: all; /* 버튼은 클릭 받음 */
}

.action-btn {
    width: 100px;
    height: 50px;
    margin: 0 8px;
    border-radius: 8px;
    border-width: 0;
    color: #ffffff;
    font-size: 14px;
    -unity-font-style: bold;
    cursor: link;
    translate: 0 0;
    transition: translate 0.1s ease-out, background-color 0.15s ease;
}

.action-btn:hover {
    translate: 0 -4px;
}

.action-btn:active {
    translate: 0 2px;
}

.action-btn--build    { background-color: #2980b9; }
.action-btn--build:hover { background-color: #3498db; }

.action-btn--recruit  { background-color: #27ae60; }
.action-btn--recruit:hover { background-color: #2ecc71; }

.action-btn--research { background-color: #8e44ad; }
.action-btn--research:hover { background-color: #9b59b6; }

.action-btn:disabled  { opacity: 0.4; cursor: arrow; translate: 0 0; }
```

### 고급 패턴

**UXML 템플릿 기반 아이템 카드 시스템**

```xml
<!-- Assets/UI/Templates/ItemCard.uxml -->
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="item-card" class="item-card">
        <VisualElement name="item-icon" class="item-card__icon"/>
        <VisualElement class="item-card__info">
            <Label name="item-name"    class="item-card__name"    text="Item"/>
            <Label name="item-desc"    class="item-card__desc"    text="Description"/>
            <Label name="item-rarity"  class="item-card__rarity"  text="Common"/>
        </VisualElement>
        <VisualElement class="item-card__actions">
            <Button name="equip-btn"   class="item-card__btn item-card__btn--equip"   text="Equip"/>
            <Button name="discard-btn" class="item-card__btn item-card__btn--discard" text="Discard"/>
        </VisualElement>
    </VisualElement>
</UXML>
```

```csharp
// 아이템 카드 동적 생성 컨트롤러
public class InventoryController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VisualTreeAsset _itemCardTemplate;

    private VisualElement _container;

    private void OnEnable()
    {
        _container = _document.rootVisualElement.Q<VisualElement>("item-container");
    }

    public void AddItem(ItemData data)
    {
        var card = _itemCardTemplate.Instantiate();

        // 콘텐츠 설정
        card.Q<Label>("item-name").text   = data.name;
        card.Q<Label>("item-desc").text   = data.description;
        card.Q<Label>("item-rarity").text = data.rarity.ToString();

        // 희귀도에 따른 USS 클래스 추가
        var itemCard = card.Q<VisualElement>("item-card");
        itemCard.AddToClassList($"item-card--{data.rarity.ToString().ToLower()}");

        // 아이콘 설정
        var icon = card.Q<VisualElement>("item-icon");
        icon.style.backgroundImage = new StyleBackground(data.icon);

        // 버튼 이벤트
        var equipBtn   = card.Q<Button>("equip-btn");
        var discardBtn = card.Q<Button>("discard-btn");

        // 클로저로 data 캡처 (람다보다 명시적 변수 캡처 권장)
        var capturedData = data;
        equipBtn.clicked   += () => OnEquipItem(capturedData);
        discardBtn.clicked += () => OnDiscardItem(capturedData, card);

        _container.Add(card);
    }

    private void OnEquipItem(ItemData data)   { /* 장착 로직 */ }
    private void OnDiscardItem(ItemData data, VisualElement card)
    {
        card.RemoveFromHierarchy(); // 화면에서 제거
    }
}
```

**USS 트랜지션을 활용한 슬라이딩 패널**

```css
/* SlidingPanel.uss */
.sliding-panel {
    position: absolute;
    bottom: -300px;           /* 화면 밖 초기 위치 — 명시적 px 필수 */
    left: 0;
    right: 0;
    height: 300px;
    background-color: #1a1a2e;
    border-top-left-radius: 16px;
    border-top-right-radius: 16px;
    transition: bottom 0.4s ease-out;
}

.sliding-panel--open {
    bottom: 0;
}

.sliding-panel__handle {
    width: 40px;
    height: 4px;
    background-color: #555;
    border-radius: 2px;
    align-self: center;
    margin-top: 12px;
    margin-bottom: 8px;
}
```

```csharp
public class SlidingPanelController : MonoBehaviour
{
    private VisualElement _panel;
    private bool _isOpen = false;

    private void OnEnable()
    {
        _panel = GetComponent<UIDocument>()
            .rootVisualElement
            .Q<VisualElement>("sliding-panel");

        _panel.Q<Button>("panel-toggle").clicked += TogglePanel;
    }

    public void TogglePanel()
    {
        _isOpen = !_isOpen;
        // USS 클래스 토글 — transition이 자동 실행
        _panel.EnableInClassList("sliding-panel--open", _isOpen);
    }

    public void OpenPanel()  => _panel.AddToClassList("sliding-panel--open");
    public void ClosePanel() => _panel.RemoveFromClassList("sliding-panel--open");
}
```

**ListView 가상화와 UXML 템플릿 결합**

```csharp
public class QuestLogController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VisualTreeAsset _questRowTemplate;

    private ListView _questList;
    private List<QuestData> _quests;

    private void OnEnable()
    {
        _questList = _document.rootVisualElement.Q<ListView>("quest-list");
        _quests    = QuestManager.Instance.GetActiveQuests();

        _questList.itemsSource   = _quests;
        _questList.fixedItemHeight = 64f; // 가상화에 필수

        // 아이템 시각 요소 생성 (재사용됨)
        _questList.makeItem = () => _questRowTemplate.Instantiate();

        // 데이터를 시각 요소에 바인딩 (스크롤 시 재사용됨)
        _questList.bindItem = (element, index) =>
        {
            var quest = _quests[index];
            element.Q<Label>("quest-title").text    = quest.title;
            element.Q<Label>("quest-progress").text =
                $"{quest.currentCount}/{quest.targetCount}";

            element.EnableInClassList("quest-row--completed", quest.isCompleted);
        };

        _questList.selectionType = SelectionType.Single;
        _questList.onSelectionChange += OnQuestSelected;

        _questList.Rebuild();
    }

    private void OnQuestSelected(IEnumerable<object> selectedItems)
    {
        var quest = selectedItems.FirstOrDefault() as QuestData;
        if (quest != null) ShowQuestDetail(quest);
    }

    private void ShowQuestDetail(QuestData quest) { /* 상세 패널 표시 */ }
}
```

---

## 6. UI_Study 적용 계획

### UXML/USS 학습 우선순위

1. **UXML 기본 구조 + USS 연결** (즉시)
   - 네임스페이스, `<Style src>`, 기본 컨트롤 마크업 실습

2. **Flexbox 레이아웃 숙달** (1주차)
   - row/column, justify-content, align-items, flex-grow 조합
   - 반응형 HUD 구성 실습

3. **USS Pseudo-class + Transition** (1주차)
   - `:hover`, `:active`, `:disabled` 상태 스타일
   - translate/scale/opacity 전환으로 버튼 피드백

4. **UXML 템플릿 시스템** (2주차)
   - ItemCard, DialogBox 등 재사용 컴포넌트 설계
   - AttributeOverrides 패턴 실습

5. **커스텀 VisualElement** (2주차)
   - UxmlElement + generateVisualContent 조합
   - HealthBar, ProgressRing 등 게임 UI 특화 컴포넌트

6. **UQuery 최적화** (지속)
   - 모든 예제에서 OnEnable 캐싱 패턴 준수

### 기지 경영 게임 USS 아키텍처 제안

```
Assets/UI/
├── Styles/
│   ├── variables.uss        -- 색상/간격/폰트 변수 (--var)
│   ├── base.uss             -- 기본 요소 스타일 (Button, Label, VisualElement)
│   ├── components.uss       -- 재사용 컴포넌트 (card, bar, badge)
│   ├── hud.uss              -- HUD 전용 스타일
│   ├── panels.uss           -- 패널/팝업 스타일
│   └── animations.uss       -- 전환/애니메이션 정의
├── Templates/
│   ├── ResourceBar.uxml
│   ├── BuildingCard.uxml
│   ├── UnitRow.uxml
│   └── DialogBox.uxml
└── Screens/
    ├── HUD.uxml
    ├── BuildMenu.uxml
    ├── UnitManage.uxml
    └── Settings.uxml
```

---

## 7. 참고 자료

- [UXML 작성 가이드](https://docs.unity3d.com/Manual/UIE-WritingUXMLTemplate.html)
- [UXML 파일 재사용](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-reuse-uxml-files.html)
- [USS 선택자 레퍼런스](https://docs.unity3d.com/Manual/UIE-USS-Selectors.html)
- [USS Pseudo-class 레퍼런스](https://docs.unity3d.com/Manual/UIE-USS-Selectors-Pseudo-Classes.html)
- [USS 선택자 특수성](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-uss-selector-precedence.html)
- [USS 전환(Transitions) 가이드](https://docs.unity3d.com/Manual/UIE-Transitions.html)
- [USS 속성 레퍼런스 전체](https://docs.unity3d.com/Manual/UIE-USS-Properties-Reference.html)
- [USS 공통 속성 (supported properties)](https://docs.unity3d.com/Manual/UIE-USS-SupportedProperties.html)
- [UQuery 가이드](https://docs.unity3d.com/Manual/UIE-UQuery.html)
- [커스텀 컨트롤 생성 (Unity 6)](https://docs.unity3d.com/6000.1/Documentation/Manual/UIE-create-custom-controls.html)
- [generateVisualContent / Painter2D](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-generate-2d-visual-content.html)
- [고급 개발자를 위한 스타일링 베스트 프랙티스](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/styling.html)
- [UXML 소개 (Medium)](https://medium.com/@lemapp09/beginning-game-development-unitys-ui-toolkit-s-uxml-3283f9cab1f7)
- [USS 소개 (Medium)](https://medium.com/@lemapp09/beginning-game-development-unitys-ui-toolkit-s-uss-87e19e7b71e5)
- [USS Transition 발표 스레드](https://forum.unity.com/threads/announcing-uss-transition.1203832/)

---

## 8. 미해결 질문

1. **USS 선택자 `>` 자식 콤비네이터와 공백 자손 콤비네이터의 성능 차이는?** — 복잡한 UI에서 선택자 깊이의 실제 성능 영향 측정 필요
2. **UXML AttributeOverrides가 스타일 클래스에 적용 불가한 대안은?** — USS에서 인스턴스 이름 기반 선택자(`#player1 .icon`)로 처리 가능한지 검증
3. **USS 변수(--var)가 런타임에서 C#으로 읽기/쓰기 가능한가?** — `customStyle`/`CustomStyleProperty` API와의 관계 조사 필요
4. **Painter2D Arc의 각도 기준이 시계 방향인가 반시계 방향인가?** — 레디얼 진행바 구현 시 정확한 각도 방향 확인 필요
5. **ListView의 bindItem에서 클로저 이벤트 등록 시 메모리 누수 패턴?** — unbindItem 콜백에서 이벤트 해제 필요성 검증
6. **USS transition과 DOTween을 동일 속성에 동시 적용 시 충돌 동작?** — 예: opacity를 USS transition과 DOTween 모두 제어하는 경우
7. **UXML의 `<Style>` 태그와 USS 파일 경로 — 에디터 이동 후 자동 업데이트 여부?** — 에셋 경로 변경 시 GUID 기반 참조 또는 수동 경로 갱신 필요 여부
