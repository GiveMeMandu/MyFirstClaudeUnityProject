# dots-defense-system Tasks

- **Last Updated**: 2026-03-25
- **Branch**: feature/TASK-002-dots-defense-system

## Sub-Features

### SF-01: DOTS 패키지 설치 + 데이터 모델 [M]
- **상태**: DONE
- **의존**: -
- **파일**: manifest.json, EnemyDataSO.cs, WaveDataSO.cs, DefenseEnums.cs, ECS Components
- **커밋**: d0dc2fc
- [x] manifest.json에 DOTS 패키지 추가 (entities 6.4.0, entities.graphics 6.4.0)
- [x] EnemyDataSO (적 종류별 스탯 SO)
- [x] WaveDataSO (웨이브 구성 SO, 자동 스케일링)
- [x] DefenseEnums (EnemyType, BattleState, EnemyState)
- [x] ECS IComponentData 정의 (EnemyStats, EnemyTarget, BuildingData 등)
- [x] Unity 컴파일 통과

### SF-02: 적 스폰 시스템 [L]
- **상태**: DONE
- **의존**: SF-01
- **파일**: EnemyPrefabAuthoring.cs, EnemySpawnerAuthoring.cs, PrefabEntityAuthoring.cs, WaveSpawnSystem.cs, BattleManager.cs
- **커밋**: f19368b
- [x] EnemyPrefabAuthoring + Baker (적 프리팹 → Entity 변환)
- [x] EnemySpawnerAuthoring + Baker (스폰 포인트 설정)
- [x] PrefabEntityAuthoring + Baker (프리팹 Entity 참조)
- [x] WaveSpawnSystem (ISystem, ECB로 적 스폰)
- [x] BattleManager (MonoBehaviour, 전투 라이프사이클 관리, DOTS 브릿지)
- [x] Unity 컴파일 통과

### SF-03: 적 이동 시스템 [M]
- **상태**: DONE
- **의존**: SF-02
- **파일**: EnemyMovementSystem.cs
- **커밋**: 1f45711
- [x] EnemyMovementSystem (ISystem, 가장 가까운 건물로 이동)
- [x] 공중 유닛 방벽 무시 로직
- [x] Unity 컴파일 통과

### SF-04: 적 전투 + 건물 피해 [L]
- **상태**: DONE
- **의존**: SF-03
- **파일**: EnemyCombatSystem.cs (EnemyCombatSystem + EnemyDeathSystem)
- **커밋**: 0f85003
- [x] EnemyCombatSystem (ISystem, 범위 내 건물 공격)
- [x] BuildingDamageBuffer (DOTS→MonoBehaviour 피해 전달)
- [x] EnemyDeathSystem (HP 0 적 제거)
- [x] BattleManager에 승리/패배 판정 + 건물 피해 동기화
- [x] Unity 컴파일 통과

### SF-05: 전투 카메라 + UI [M]
- **상태**: DONE
- **의존**: SF-02
- **파일**: BattleCameraController.cs, BattleUIManager.cs
- **커밋**: 703a3db
- [x] BattleCameraController (탑다운/자유 전환, 줌인/줌아웃)
- [x] BattleUIManager (밤 시작 버튼, 배속 1x/2x, 통계창)
- [x] 전투 통계 데이터 구조 (처치 수, 건물 피해 목록)
- [x] Unity 컴파일 통과

### SF-06: 사망 이펙트 + 체력바 [M]
- **상태**: DONE
- **의존**: SF-04
- **파일**: HealthBarSystem.cs, EnemyVisualManager.cs
- **커밋**: 825a598
- [x] HealthBarSystem (ECS, 피격 시 2초 타이머)
- [x] EnemyVisualManager (MonoBehaviour, 체력바 풀링 + 사망 이펙트 풀링)
- [x] 체력바 토글 가능
- [x] Unity 컴파일 통과

## 완료 이력
| SF | 커밋 해시 | 날짜 |
|---|---|---|
| SF-01 | d0dc2fc | 2026-03-25 |
| SF-02 | f19368b | 2026-03-25 |
| SF-03 | 1f45711 | 2026-03-25 |
| SF-04 | 0f85003 | 2026-03-25 |
| SF-05 | 703a3db | 2026-03-25 |
| SF-06 | 825a598 | 2026-03-25 |
