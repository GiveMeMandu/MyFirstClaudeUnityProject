# policy-system Context

- **Last Updated**: 2026-03-26
- **Branch**: feature/policy-system
- **Type**: feature
- **Base**: develop

## 핵심 파일

- `Project_Sun/Assets/Scripts/Policy/PolicyEnums.cs` — 열거형 (카테고리, 노드 상태, 효과 유형)
- `Project_Sun/Assets/Scripts/Policy/PolicyNodeSO.cs` — 개별 정책 노드 SO + PolicyEffect 구조체
- `Project_Sun/Assets/Scripts/Policy/PolicyTreeSO.cs` — 카테고리별 정책 트리 SO
- `Project_Sun/Assets/Scripts/Policy/PolicyManager.cs` — 상태 관리, 해금, 제정, 분기 잠김
- `Project_Sun/Assets/Scripts/Policy/PolicyEffectResolver.cs` — 활성 효과 집계, 외부 시스템 수정자 API
- `Project_Sun/Assets/Scripts/Policy/Testing/PolicyUI.cs` — IMGUI 카테고리별 노드 UI + 확인 팝업
- `Project_Sun/Assets/Scripts/Policy/Testing/PolicyTestController.cs` — 턴 시뮬레이션 테스트
- `Project_Sun/Assets/Scripts/Editor/PolicyTestSceneBuilder.cs` — 씬 + SO 에셋 자동 생성
- `Project_Sun/Assets/Scripts/Turn/TurnManager.cs` — 수정: PolicyManager 연동 추가

## 결정사항

- 원형 3등분 UI (내정/탐사/방어) — 카테고리별 독립 정책 트리
- 이진 분기 노드는 카테고리당 1개 (PoC 총 3개), 잠김은 후속 노드까지 전파
- 턴 수 기반 해금 + 선행 노드 제정 AND 조건
- 정책 제정 무료 — 비가역성이 비용
- 효과 중첩은 가산 (Additive)
- PolicyEffect 구조체에 isPercentage 플래그로 % vs 절대값 구분
- PoC: 3카테고리 × 4노드 = 12노드 (카테고리당 분기 1쌍 포함)

## 알려진 제한/TODO

- Unity Editor 컴파일 검증 미완 (Editor 미연결)
- 희망/불만 게이지 시스템 미구현 — HopeInstant/DiscontentPerTurn 효과는 API만 존재
- 기술 트리 연동 미구현 — TechUnlock 효과 타입 추후 추가 필요
- 저장/로드 직렬화 미구현
- 원형 UI는 현재 IMGUI 리스트 뷰 — 실제 원형 레이아웃은 프로덕션 UI 단계에서 구현

## 의존성

- Turn System (TurnManager — 낮 시작 시 해금 체크)
- Resource System (ResourceManager — 자원 수정자 API 제공)
- Workforce System (WorkforceManager — 인력 수정자 API 제공)
- Encounter System (EncounterManager — 인카운터 수정자 API 제공)
