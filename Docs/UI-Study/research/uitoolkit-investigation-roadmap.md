# UI Toolkit 전수조사 로드맵

- **작성일**: 2026-03-29
- **카테고리**: integration
- **상태**: 진행중

---

## 1. 배경

UGUI 기반 UI Study 20개 리서치 완료 후, AI(Claude)가 UI Toolkit의 UXML/USS를 HTML/CSS처럼 인식하여
훨씬 정확한 UI 코드를 생성한다는 발견에 따라 UI Toolkit 전수조사를 실시한다.

### 호환성 사전 조사 결과

- R3, VContainer, UniTask 모두 UI Toolkit **네이티브 미지원**
- Thronefall(2인 인디, 100만 장) 사례: DI/Reactive 없이 표준 MonoBehaviour로 성공
- 기존 스택(VContainer+R3+MV(R)P)은 소규모 팀에 오버스펙 판단

### 경량 스택 방향

| 역할 | 채택 | 비고 |
|------|------|------|
| UI 프레임워크 | UI Toolkit | UXML/USS, AI 친화 |
| 비동기 | UniTask | 유지 (규모 무관 필수) |
| 애니메이션 | DOTween + CSS Transition | 간단→CSS, 복잡→DOTween |
| 아키텍처 | 심플 MVP (DI 없이) | [SerializeField] + 수동 연결 |
| 상태 관리 | C# event/Action | 복잡 바인딩만 R3 부분 도입 |

---

## 2. 조사 문서 목록

| # | 파일명 | 분대 | Phase | 상태 |
|---|--------|------|-------|------|
| 021 | uitoolkit-fundamentals.md | A | 1 | 대기 |
| 022 | uitoolkit-uxml-uss-deep-dive.md | A | 1 | 대기 |
| 023 | uitoolkit-ai-generation-patterns.md | B | 1 | 대기 |
| 024 | uitoolkit-lightweight-architecture.md | C | 2 | 대기 |
| 025 | uitoolkit-unitask-dotween-patterns.md | C | 2 | 대기 |
| 026 | uitoolkit-runtime-game-ui-patterns.md | D | 2 | 대기 |
| 027 | uitoolkit-performance-and-limits.md | D | 2 | 대기 |
| 028 | uitoolkit-vs-ugui-decision-matrix.md | 정리 | 3 | 대기 |

---

## 3. 실행 순서

```
Phase 0: 사전 준비 (에이전트 수정, 로드맵)
Phase 1: A분대(021,022) + B분대(023) 병렬
Phase 2: C분대(024,025) + D분대(026,027) 병렬 — Phase 1 A분대 완료 후
Phase 3: 정리팀(028) + 전체 문서 정규화 — Phase 2 완료 후
Phase 4: 품질 검증 + 갭 보완
Phase 5: 학습 계획 06-uitoolkit-lightweight.md 생성
```

---

## 4. 핵심 질문

> "DI/Reactive 없이 UI Toolkit + UniTask + DOTween만으로 기지 경영 게임 UI를 얼마나 커버할 수 있는가?"
