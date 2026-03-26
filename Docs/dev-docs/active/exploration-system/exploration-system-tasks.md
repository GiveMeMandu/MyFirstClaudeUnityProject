# exploration-system 구현 태스크

- **기획서**: Docs/GDD/systems/exploration-system.md
- **브랜치**: feature/exploration-system
- **시작일**: 2026-03-26

## Sub-Features

### SF-01: 데이터 모델 [M]
- **상태**: DONE
- **의존**: -
- **파일**: ExplorationEnums.cs, ExplorationNodeSO.cs, ExplorationMapSO.cs
- **커밋**: 9be1a7d
- [x] ExplorationEnums (NodeType, FogState, ExpeditionState)
- [x] ExplorationNodeSO (노드 정의: 유형, 보상, 힌트, 재방문 플래그)
- [x] ExplorationMapSO (맵 정의: 노드 목록, 간선, 소요 턴수, 시작 노드)
- [x] Unity 컴파일 통과 (Unity 미연결 — 코드 레벨 검증)

### SF-02: ExplorationManager 코어 [XL]
- **상태**: DONE
- **의존**: SF-01
- **파일**: ExplorationManager.cs, ExpeditionTeam.cs
- **커밋**: 4cc42a1
- [x] ExpeditionTeam (원정대 런타임 데이터: 인원, 위치, 상태, 경로)
- [x] ExplorationManager (맵 초기화, 안개 관리, 노드 상태 관리)
- [x] 이동 처리 (턴 종료 시 원정대 이동, 도착 판정)
- [x] 귀환 처리 (BFS 최단 경로 계산, 기지 도착 시 인력 복귀)
- [x] 도착 이벤트 큐 (보상 대기열 관리)
- [x] Unity 컴파일 통과 (Unity 미연결 — 코드 레벨 검증)

### SF-03: 인력/건설 시스템 연동 [M]
- **상태**: DONE
- **의존**: SF-02
- **파일**: WorkforceManager.cs (수정), WorkforceEnums.cs (수정), ExplorationManager.cs (수정)
- **커밋**: f6bcfa6
- [x] WorkforceManager에 원정대 인력 배치/회수/부상 메서드 추가
- [x] WorkerSlotType에 Expedition 추가
- [x] ExplorationManager에서 탐사 건물 수 → maxTeams 계산
- [x] BuildingManager 탐사 건물 활성/파괴 이벤트 구독
- [x] Unity 컴파일 통과 (Unity 미연결 — 코드 레벨 검증)

### SF-04: 턴/인카운터/자원 연동 [L]
- **상태**: DONE
- **의존**: SF-02, SF-03
- **파일**: TurnManager.cs (수정)
- **커밋**: a4ddd14
- [x] TurnManager에 ExplorationManager 연동 (턴 종료 시 이동 처리 호출)
- [x] 도착 이벤트 순차 처리 (자원 → ResourceManager, 인카운터 → EncounterManager)
- [x] 정찰 노드 정보 토스트 표시 (추후 웨이브 정보 연동 준비)
- [x] Unity 컴파일 통과 (Unity 미연결 — 코드 레벨 검증)

### SF-05: UI + 테스트 데이터 + 테스트 씬 [XL]
- **상태**: DONE
- **의존**: SF-04
- **파일**: ExplorationUI.cs, ExplorationTestSceneBuilder.cs, TurnTestSceneBuilder.cs (수정)
- **커밋**: 20a34bd
- [x] ExplorationUI (IMGUI 전체 화면 노드 맵 + 원정대 관리 패널)
- [x] 10노드 테스트 맵 SO 에셋 (에디터 스크립트에서 자동 생성)
- [x] 각 유형별 노드 SO 에셋 (Resource x4, Recon x2, Encounter x2, Tech x1)
- [x] ExplorationTestSceneBuilder (독립 에디터 스크립트)
- [x] TurnTestSceneBuilder에 탐사 시스템 통합
- [x] Unity 컴파일 통과 (Unity 미연결 — 코드 레벨 검증)

## 완료 이력
| SF | 커밋 해시 | 날짜 |
|---|---|---|
| SF-01 | 9be1a7d | 2026-03-26 |
| SF-02 | 4cc42a1 | 2026-03-26 |
| SF-03 | f6bcfa6 | 2026-03-26 |
| SF-04 | a4ddd14 | 2026-03-26 |
| SF-05 | 20a34bd | 2026-03-26 |
