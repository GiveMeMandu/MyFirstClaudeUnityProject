# tech-tree-system 컨텍스트

## 기획서
- Docs/GDD/systems/tech-tree-system.md

## 브랜치
- feature/tech-tree-system

## 연동 시스템
- BuildingManager: Research 카테고리 건물 상태/인력 조회, requiresResearch 플래그
- ResourceManager: CanAfford() / SpendCosts() 비용 처리
- TurnManager: OnPhaseChanged 이벤트 (DayEnd 진행도, DayStart 완료 처리)
- WorkforceManager: GetBuildingTotalWorkers() 인력 수

## 구현 파일
- `Project_Sun/Assets/Scripts/TechTree/TechTreeEnums.cs` — 열거형 (TechNodeState, TechEffectType, TechCategory)
- `Project_Sun/Assets/Scripts/TechTree/TechNodeEffect.cs` — 효과 구조체
- `Project_Sun/Assets/Scripts/TechTree/Data/TechNodeSO.cs` — 노드 SO
- `Project_Sun/Assets/Scripts/TechTree/Data/TechTreeCategorySO.cs` — 카테고리 SO
- `Project_Sun/Assets/Scripts/TechTree/Data/TechTreeDataSO.cs` — 전체 트리 SO
- `Project_Sun/Assets/Scripts/TechTree/TechTreeManager.cs` — 핵심 매니저
- `Project_Sun/Assets/Scripts/TechTree/Testing/TechTreeUI.cs` — IMGUI UI
- `Project_Sun/Assets/Scripts/Editor/TechTreeTestSceneBuilder.cs` — 테스트 씬 빌더

## 아키텍처 결정
- SF-02와 SF-03 통합: 턴 연동이 매니저의 핵심 기능이므로 HandlePhaseChanged로 일체화
- 노드별 진행도 보존: Dictionary<TechNodeSO, float>로 전환 시 진행도 유지
- 효과 적용은 Debug.Log 레벨 (연동 시스템별 실제 적용은 해당 시스템 구현 시 연결)
- BuildingManager/WorkforceManager null 시 테스트 기본값 반환 (standalone 동작 가능)

## 알려진 제한/TODO
- 탐사 시스템 완성 후 노드 해금 연동 추가 필요
- 인력 능력치 반영은 인력 시스템 구현 후 추가
