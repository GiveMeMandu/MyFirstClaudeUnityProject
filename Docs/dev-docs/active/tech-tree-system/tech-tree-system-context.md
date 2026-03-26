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

## 아키텍처 결정
- (구현 후 기록)

## 알려진 제한/TODO
- 탐사 시스템 완성 후 노드 해금 연동 추가 필요
- 인력 능력치 반영은 인력 시스템 구현 후 추가
