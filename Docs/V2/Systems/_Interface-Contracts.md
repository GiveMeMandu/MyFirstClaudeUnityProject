# 시스템 간 인터페이스 계약

- **최종 수정일**: 2026-04-02 (Exploration GDD 반영)
- **담당**: system-designer-v2
- **상태**: 진행 중 (시스템 GDD 추가 시 갱신)

이 문서는 모든 시스템 GDD의 인터페이스 계약을 한곳에 모아 관리한다. 각 행은 두 시스템 간의 데이터 흐름 하나를 나타낸다.

---

## 계약 목록

| 시스템 A | 시스템 B | 방향 | 데이터 | 트리거 | 계약 설명 | 출처 GDD |
|---|---|---|---|---|---|---|
| 건설 | 인력 관리 | A -> B | `SlotActivated(slotId, buildingType)` | 건설 완료 시 | 인력 시스템에 새 소켓 슬롯 개방 통보 | Construction, Workforce |
| 인력 관리 | 건설 | A -> B | `SocketBonusApplied(slotId, bonusType, value)` | 소켓 배치/해제 시 | 건물 효과에 소켓 보너스 반영 | Construction, Workforce |
| 건설 | 인력 관리 | A -> B | `SlotDamaged(slotId)` | 건물 손상 시 | 해당 소켓 시민을 미배치로 강제 전환 | Construction, Workforce |
| 건설 | 인력 관리 | A -> B | `SlotRepaired(slotId)` | 건물 수리 완료 시 | 소켓 재배치 가능 통보 | Construction, Workforce |
| 건설 | 웨이브 방어 | A -> B | `DefenseBuildingStats(slotId, attackPower, range, hp)` | 밤 전투 시작 시 | 방어 건물 스탯을 웨이브 시스템에 전달 | Construction, WaveDefense |
| 웨이브 방어 | 건설 | A -> B | `BuildingDamageReport(slotIds[])` | 밤 전투 종료 시 | 손상된 건물 목록 수신, 손상 상태 전환 | Construction, WaveDefense |
| 건설 | 경제 | A -> B | `ResourceProduction(resourceType, amount)` | 매 턴 시작 시 | 활성 생산 건물의 자원 생산량 전달 | Construction |
| 경제 | 건설 | A -> B | `ResourceConsumed(resourceType, amount)` | 건설/업그레이드/수리 시 | 자원 소비 요청 | Construction |
| 기술 트리 | 건설 | A -> B | `ResearchUnlock(unlockedSlotIds[], unlockedBranches[])` | 연구 완료 시 | 해금된 슬롯/분기 정보 수신 | Construction |
| 탐사/원정 | 건설 | A -> B | `ExplorationUnlock(unlockedSlotIds[], unlockedBranches[])` | 탐사 발견 시 | 해금된 슬롯/분기 정보 수신 | Construction |
| 인력 관리 | 웨이브 방어 | A -> B | `SquadDeployed(squadId, combatPower, size, abilities[])` | 밤 전투 시작 시 | 분대 스탯을 웨이브 시스템에 전달 | Workforce, WaveDefense |
| 웨이브 방어 | 인력 관리 | A -> B | `CombatResult(injuries[], combatExpGained[])` | 밤 전투 종료 시 | 부상자 목록 + 전투 숙련도 경험치 | Workforce, WaveDefense |
| 인력 관리 | 탐사/원정 | A -> B | `ExpeditionDispatched(citizenIds[], targetNode)` | 원정 파견 시 | 파견된 시민 ID와 목표 노드 | Workforce |
| 탐사/원정 | 인력 관리 | A -> B | `ExpeditionReturned(citizenIds[], rewards[], injuries[], events[])` | 원정 귀환 시 | 귀환 시민, 보상, 부상 정보, 이벤트 결과 | Workforce, Exploration |
| 탐사/원정 | 인력 관리 | A -> B | `SurvivorRescued(citizenData)` | 생존자 구출 시 | 신규 시민 데이터 전달 | Workforce |
| 인력 관리 | 경제 | A -> B | `BonusProduction(buildingId, resourceType, bonusAmount)` | 매 턴 생산 정산 시 | 소켓 보너스 추가 생산량 | Workforce |
| 경제 | 인력 관리 | A -> B | `BonfireInvestment(investmentSize)` | 모닥불 투자 시 | 투자 규모에 따른 증원 프로세스 시작 | Workforce |
| 인력 관리 | 경제 | A -> B | `InjuryCost(citizenId, costAmount)` | 부상 치료 시 | 기초 자원 소비 | Workforce |
| 웨이브 방어 | 경제 | A -> B | `DefenseResult(damageRatio, damagedBuildings[], rewardTier)` | 밤 전투 종료 시 | 방어 결과(판정 등급) + 보상 자원 정산 트리거. 수리비 계산 시작 | WaveDefense |
| 경제 | 웨이브 방어 | A -> B | `WaveModifier(strengthMultiplier)` | 적응형 웨이브 약화 발동 시 | 연속 대규모 피해 시 다음 웨이브 강도 -15~20% 보정 | WaveDefense |
| 탐사/원정 | 웨이브 방어 | A -> B | `ScoutInfo(detailLevel, enemyComposition)` | 웨이브 미리보기 시 | 탐사 수준에 따른 적 정보 상세도 결정 (기본/상세/정밀) | WaveDefense, Exploration |
| 탐사/원정 | 경제 | A -> B | `ExplorationReward(basicResource, advancedResource, relicResource)` | 원정 귀환 시 | 탐사 자원 보상 정산 | Exploration |
| 건설 | 탐사/원정 | A -> B | `OutpostBuilt(outpostLevel)` | 전초 기지 건설/업그레이드 시 | 동시 파견 팀 수 + 탐사 속도 보너스 전달 | Exploration, Construction |
