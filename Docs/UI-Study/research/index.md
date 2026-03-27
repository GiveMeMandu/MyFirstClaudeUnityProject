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

---

## 카테고리별

### pattern
- [001 - UGUI UI 아키텍처 패턴 비교](ugui-ui-architecture-patterns.md) — MVP, MVVM, MVC, Flux/Redux 비교

### library
- [002 - UniTask UI 비동기 패턴](unitask-ui-research.md) — 다이얼로그 await, R3/VContainer 통합
- [003 - R3 리액티브 라이브러리](r3-reactive-library.md) — UniRx 후속, API 차이, Unity 기능

### integration
- [004 - VContainer + UGUI 통합](vcontainer-ugui-integration.md) — DI 스코핑, 프리팹 주입, 자식 스코프
- [005 - 기술 스택 결정 종합](tech-stack-decisions.md) — 12개 영역 최종 결정

### practice
- [007 - UGUI 코드 기반 레이아웃 (code-first)](ugui-programmatic-layout.md) — RectTransform 앵커 치트시트, DefaultControls, LayoutGroup, Editor 스크립트 씬 빌더, unity-cli 검증 루틴
- [008 - UGUI Tooltip 위치 계산 시스템](ugui-tooltip-positioning.md) — RectTransformUtility, Canvas 모드별 카메라, anchoredPosition, 경계 클램핑, ScaleMode, New Input System

---

## 확정 기술 스택

| 핵심 | 보조 |
|---|---|
| MV(R)P + VContainer + R3 + UniTask | UnityScreenNavigator + DOTween + uPalette |
| Addressables + SpriteAtlas V2 | Unity Localization + FancyScrollView |
| New Input System (게임패드 포함) | 폰트 스케일링 + 색약 모드 |
