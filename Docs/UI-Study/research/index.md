# UI-Study 리서치 인덱스

## 리서치 목록

| 번호 | 주제 | 카테고리 | 작성일 | 상태 |
|------|------|----------|--------|------|
| 001 | [UGUI UI 아키텍처 패턴 비교](ugui-ui-architecture-patterns.md) | pattern | 2026-03-27 | 조사완료 |
| 002 | [UniTask UI 비동기 패턴](unitask-ui-research.md) | library | 2026-03-27 | 조사완료 |
| 003 | [R3 리액티브 라이브러리](r3-reactive-library.md) | library | 2026-03-27 | 조사완료 |
| 004 | [VContainer + UGUI 통합](vcontainer-ugui-integration.md) | integration | 2026-03-27 | 조사완료 |
| 005 | [기술 스택 결정 종합](tech-stack-decisions.md) | integration | 2026-03-27 | 확정 |
| 006 | [라이브러리 공식 예제 + 실전 패턴 조사](library-examples-survey.md) | integration | 2026-03-28 | 조사완료 |
| 007 | [UGUI 코드 기반 레이아웃 (code-first)](ugui-programmatic-layout.md) | practice | 2026-03-28 | 조사완료 |
| 008 | [UGUI Tooltip 위치 계산 시스템](ugui-tooltip-positioning.md) | practice | 2026-03-28 | 조사완료 |
| 009 | [게임 UI 패턴 12종 — HUD/인벤토리/대화/팝업 등](game-ui-patterns.md) | pattern | 2026-03-28 | 조사완료 |
| 010 | [UGUI 고급 UI 애니메이션 패턴](ugui-animation-patterns.md) | animation | 2026-03-28 | 조사완료 |
| 011 | [UGUI ScrollView 풀링 & 버추얼 스크롤](ugui-scrollview-pooling.md) | practice | 2026-03-28 | 조사완료 |
| 012 | [UGUI 성능 최적화 기법 종합](ugui-performance-optimization.md) | performance | 2026-03-28 | 조사완료 |
| 013 | [R3 UI 데이터 바인딩 패턴](r3-ui-binding-patterns.md) | library / practice | 2026-03-28 | 조사완료 |
| 014 | [UGUI 반응형/적응형 디자인 패턴](ugui-responsive-design.md) | practice | 2026-03-28 | 조사완료 |
| 015 | [UGUI World Space UI 패턴](ugui-world-space-ui.md) | pattern / practice | 2026-03-28 | 조사완료 |
| 016 | [UGUI Drag & Drop 구현 패턴](ugui-drag-drop-patterns.md) | practice | 2026-03-28 | 조사완료 |
| 017 | [UGUI 고급 HUD 패턴 — Radial Menu / Minimap / Loading Screen](ugui-advanced-hud-patterns.md) | practice / pattern | 2026-03-28 | 조사완료 |
| 018 | [UGUI 패널 슬라이드 트랜지션 — 클리핑, 캐러셀, 깜빡임 방지](ugui-panel-slide-transition.md) | practice | 2026-03-28 | 조사완료 |
| 019 | [UGUI 인벤토리 그리드 + 디테일 패널 레이아웃](ugui-inventory-grid-detail-panel-layout.md) | practice | 2026-03-28 | 조사완료 |

---

## 카테고리별

### pattern
- [001 - UGUI UI 아키텍처 패턴 비교](ugui-ui-architecture-patterns.md) — MVP, MVVM, MVC, Flux/Redux 비교
- [009 - 게임 UI 패턴 12종](game-ui-patterns.md) — HUD/인벤토리/대화/팝업/탭/로딩/설정/월드UI/래디얼/튜토리얼/리더보드/샵

### library
- [002 - UniTask UI 비동기 패턴](unitask-ui-research.md) — 다이얼로그 await, R3/VContainer 통합
- [003 - R3 리액티브 라이브러리](r3-reactive-library.md) — UniRx 후속, API 차이, Unity 기능
- [013 - R3 UI 데이터 바인딩 패턴](r3-ui-binding-patterns.md) — ReactiveProperty/Command/Triggers/SubscribeAwait 실전 패턴 10종

### integration
- [004 - VContainer + UGUI 통합](vcontainer-ugui-integration.md) — DI 스코핑, 프리팹 주입, 자식 스코프
- [005 - 기술 스택 결정 종합](tech-stack-decisions.md) — 12개 영역 최종 결정

### animation
- [010 - UGUI 고급 UI 애니메이션 패턴](ugui-animation-patterns.md) — DOTween Sequence/이징/프리셋, 화면전환, 마이크로인터랙션, 스태거, 상태머신, USN 트랜지션, 성능최적화, Spine/Lottie, UI파티클, 반응형

### practice
- [007 - UGUI 코드 기반 레이아웃 (code-first)](ugui-programmatic-layout.md) — RectTransform 앵커 치트시트, DefaultControls, LayoutGroup, Editor 스크립트 씬 빌더, unity-cli 검증 루틴
- [018 - UGUI 패널 슬라이드 트랜지션](ugui-panel-slide-transition.md) — RectMask2D vs Mask 클리핑 비교, 캐러셀 계층 구조, 슬라이드 거리 계산(rect.width), 깜빡임 방지(SetActive 타이밍), DOTween Sequence Join 패턴
- [019 - UGUI 인벤토리 그리드 + 디테일 패널 레이아웃](ugui-inventory-grid-detail-panel-layout.md) — 고정/유연 너비 혼합 HLG, GridLayoutGroup 열 수 보장, fixedWidth 계산공식(358px), LayoutElement minWidth/preferredWidth/flexibleWidth 선택 기준, Empty State 패턴, Child Force Expand 주의사항
- [008 - UGUI Tooltip 위치 계산 시스템](ugui-tooltip-positioning.md) — RectTransformUtility, Canvas 모드별 카메라, anchoredPosition, 경계 클램핑, ScaleMode, New Input System
- [011 - UGUI ScrollView 풀링 & 버추얼 스크롤](ugui-scrollview-pooling.md) — LoopScrollRect/FancyScrollView/EnhancedScroller 비교, ObservableCollections.R3 통합, Canvas 성능, LayoutGroup 대안, 무한스크롤/페이지네이션
- [014 - UGUI 반응형/적응형 디자인 패턴](ugui-responsive-design.md) — CanvasScaler 3모드/Match 공식, SafeAreaPanel, AspectRatioFitter, SpriteAtlas Variant, 방향전환(R3), TMP auto-size, DeviceProfile, UIScaleService, DPI-Aware, Unity6 변경사항
- [015 - UGUI World Space UI 패턴](ugui-world-space-ui.md) — World Space Canvas 설정, 빌보딩 3종, HP 바(자식Canvas/GPU Instancing), Screen Space 대안(WorldToScreenPoint), 상호작용 프롬프트, 플로팅 전투 텍스트, 네임 플레이트, 오클루전 레이캐스트, 거리 LOD, 성능 최적화
- [016 - UGUI Drag & Drop 구현 패턴](ugui-drag-drop-patterns.md) — IBeginDragHandler 트리오, Ghost DragLayer, CanvasGroup.blocksRaycasts, 드롭 존 하이라이트, 그리드 슬롯 교환, Sortable List, 카드 Fan 레이아웃, Canvas 좌표 변환, R3 Observable 드래그 체인, 터치/멀티터치
- [017 - UGUI 고급 HUD 패턴](ugui-advanced-hud-patterns.md) — Radial Menu(Atan2/Radial360/R3), Minimap(RenderTexture/orthographic/아이콘 추적/Compass uvRect), Loading Screen(AsyncOperation/Addressables 집계/UniTask IProgress/Lerp/최소 표시 시간/tips R3)

### performance
- [012 - UGUI 성능 최적화 기법 종합](ugui-performance-optimization.md) — Canvas Rebuild 비용/트리거, Canvas 분리 전략, 드로우콜 배칭, Overdraw 감소, LayoutGroup 대안, TMP 최적화, RaycastTarget, SetActive vs CanvasGroup, 오브젝트 풀링, 프로파일러 도구, 대규모 UI, 메모리 최적화

---

## 확정 기술 스택

| 핵심 | 보조 |
|---|---|
| MV(R)P + VContainer + R3 + UniTask | UnityScreenNavigator + DOTween + uPalette |
| Addressables + SpriteAtlas V2 | Unity Localization + FancyScrollView |
| New Input System (게임패드 포함) | 폰트 스케일링 + 색약 모드 |
