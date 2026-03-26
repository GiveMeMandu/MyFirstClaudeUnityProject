# tech-tree-system 태스크

## Sub-Features

### SF-01: 데이터 모델 (열거형, 구조체, SO) [M]
- **상태**: DONE
- **의존**: -
- **파일**: TechTreeEnums.cs, TechNodeEffect.cs, TechNodeSO.cs, TechTreeCategorySO.cs, TechTreeDataSO.cs
- **커밋**: —
- [ ] TechNodeState enum (Locked, Available, InProgress, Paused, Completed)
- [ ] TechEffectType enum (BuildingUpgrade, SlotReveal, StatBonus, BuildingSlotAdd, FeatureUnlock)
- [ ] TechNodeEffect struct (효과 종류, 대상, 값)
- [ ] TechNodeSO (노드 정의 — 비용, 필요 진행도, 선행 노드, 효과)
- [ ] TechTreeCategorySO (카테고리별 노드 목록)
- [ ] TechTreeDataSO (전체 트리 = 카테고리 목록)
- [ ] Unity 컴파일 통과

### SF-02: 핵심 매니저 (연구 상태, 진행도, 완료 처리) [L]
- **상태**: IN_PROGRESS
- **의존**: SF-01
- **파일**: TechTreeManager.cs
- **커밋**: —
- [ ] 노드 상태 관리 (Dictionary<TechNodeSO, TechNodeState>)
- [ ] 노드별 진행도 추적 (Dictionary<TechNodeSO, float>)
- [ ] StartResearch() — 비용 검증/차감, 상태 전환
- [ ] SwitchResearch() — 현재 진행도 보존, 새 연구 시작
- [ ] ProcessResearchProgress() — 인력 수 × 인당 생산량
- [ ] CheckCompletion() — 완료 시 효과 적용 + 후속 노드 해금
- [ ] 연구 건물 상태 확인 (BuildingManager 연동)
- [ ] Unity 컴파일 통과

### SF-03: 턴 시스템 연동 [S]
- **상태**: TODO
- **의존**: SF-02
- **파일**: TechTreeManager.cs (확장)
- **커밋**: —
- [ ] TurnManager.OnPhaseChanged 구독
- [ ] DayEnd 시 진행도 증가
- [ ] DayStart 시 완료 체크 + 알림 이벤트 발행
- [ ] Unity 컴파일 통과

### SF-04: UI + 테스트 씬 + SO 에셋 [L]
- **상태**: TODO
- **의존**: SF-03
- **파일**: TechTreeUI.cs, TechTreeTestSceneBuilder.cs, SO 에셋
- **커밋**: —
- [ ] IMGUI 기반 트리 노드 표시 (카테고리 탭)
- [ ] 노드 상태별 색상/스타일
- [ ] 연구 시작/전환 버튼
- [ ] 진행도 바 표시
- [ ] 선행 연구 미충족 시 툴팁
- [ ] TechTreeTestSceneBuilder (테스트 씬 자동 생성)
- [ ] PoC SO 에셋 (5~8개 노드)
- [ ] Unity 컴파일 통과

## 완료 이력
| SF | 커밋 해시 | 날짜 |
|---|---|---|
| SF-01 | f076204 | 2026-03-26 |
