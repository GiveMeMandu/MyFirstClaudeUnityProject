# exploration-system Context

- **Last Updated**: 2026-03-26
- **Branch**: feature/exploration-system
- **Base**: main

## 핵심 파일

### 신규 생성
- `Project_Sun/Assets/Scripts/Exploration/ExplorationEnums.cs` — 열거형 (노드유형, 안개상태, 원정대상태)
- `Project_Sun/Assets/Scripts/Exploration/ExplorationNodeSO.cs` — 개별 노드 SO (유형, 보상, 힌트)
- `Project_Sun/Assets/Scripts/Exploration/ExplorationMapSO.cs` — 맵 SO (노드 그래프, 간선, 시작점)
- `Project_Sun/Assets/Scripts/Exploration/ExplorationManager.cs` — 탐사 매니저 (맵/안개/이동/이벤트큐)
- `Project_Sun/Assets/Scripts/Exploration/ExpeditionTeam.cs` — 원정대 런타임 데이터
- `Project_Sun/Assets/Scripts/Exploration/Testing/ExplorationUI.cs` — IMGUI 전체화면 노드맵 UI
- `Project_Sun/Assets/Scripts/Editor/ExplorationTestSceneBuilder.cs` — SO 에셋 + 씬 빌더

### 수정된 파일
- `Project_Sun/Assets/Scripts/Workforce/WorkforceEnums.cs` — Expedition 슬롯 타입 추가
- `Project_Sun/Assets/Scripts/Workforce/WorkforceManager.cs` — 원정대 인력 배치/회수/부상 메서드
- `Project_Sun/Assets/Scripts/Turn/TurnManager.cs` — ExplorationManager 연동, 도착 이벤트 처리
- `Project_Sun/Assets/Scripts/Editor/TurnTestSceneBuilder.cs` — 탐사 시스템 통합

## 결정사항

1. **노드 그래프 구조**: ExplorationMapSO에 노드 + 간선을 직접 정의. 정규화 좌표(0~1)로 UI 위치 관리.
2. **안개 3단계**: Hidden → Hinted (유형 아이콘만) → Revealed (완전 공개/방문 완료)
3. **이동 타이밍**: 낮에 목적지 지정 → 턴 종료 시 이동 → 다음 낮 시작 시 도착 이벤트 순차 처리
4. **귀환 경로**: BFS 최단 경로. 방문 완료 노드도 경유 가능 (이벤트 재발생 없음)
5. **인력 연동**: WorkforceManager에 expeditionWorkers 추적 변수 추가. AssignedCount에 포함.
6. **건설 연동**: BuildingManager 이벤트 구독으로 탐사 건물 활성 수 자동 갱신 → maxTeams 결정

## 의존성

- 인력 시스템 (WorkforceManager) — 원정대 인력 배치/회수
- 건설 시스템 (BuildingManager) — 탐사 건물 수 추적
- 턴 시스템 (TurnManager) — 이동 처리 + 도착 이벤트 호출
- 인카운터 시스템 (EncounterManager) — 인카운터 노드 이벤트 전달
- 자원 시스템 (ResourceManager) — 자원 보상 지급

## 알려진 제한/TODO

- Unity 컴파일 미검증 (Unity Editor 미연결 상태에서 구현)
- 정찰 노드: 실제 웨이브 정보 연동 미구현 (토스트만 표시)
- 기술 노드: 기술 트리 시스템 미구현 (토스트만 표시)
- 원정대원 능력치/적성 → 인카운터 성공 확률 영향 미구현
- 이동 중 목적지 변경 불가 (PoC 제한)
- 재방문 가능 노드(상점 등) 플래그는 있으나 실제 동작 미구현
