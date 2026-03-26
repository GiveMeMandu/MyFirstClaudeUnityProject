# dots-defense-system Context

- **Last Updated**: 2026-03-25
- **Branch**: feature/TASK-002-dots-defense-system
- **Task ID**: TASK-002
- **Type**: feature
- **Base**: develop

## 핵심 파일

### 데이터 모델
- `Project_Sun/Assets/Scripts/Defense/DefenseEnums.cs` — EnemyType, BattleState, EnemyState
- `Project_Sun/Assets/Scripts/Defense/Data/EnemyDataSO.cs` — 적 유닛 스탯 SO
- `Project_Sun/Assets/Scripts/Defense/Data/WaveDataSO.cs` — 웨이브 구성 SO (자동 스케일링)

### ECS Components
- `Project_Sun/Assets/Scripts/Defense/ECS/Components/EnemyComponents.cs` — EnemyTag, EnemyStats, EnemyState, EnemyTarget, AttackTimer, HealthBarTimer, DeadTag
- `Project_Sun/Assets/Scripts/Defense/ECS/Components/BuildingComponents.cs` — BuildingTag, BuildingData, BuildingDamageBuffer
- `Project_Sun/Assets/Scripts/Defense/ECS/Components/WaveComponents.cs` — WaveManager, SpawnGroup, SpawnPoint, BattleStatistics

### ECS Authoring
- `Project_Sun/Assets/Scripts/Defense/ECS/Authoring/EnemyPrefabAuthoring.cs` — 적 프리팹 → Entity Baker
- `Project_Sun/Assets/Scripts/Defense/ECS/Authoring/EnemySpawnerAuthoring.cs` — 스폰 포인트 Baker
- `Project_Sun/Assets/Scripts/Defense/ECS/Authoring/PrefabEntityAuthoring.cs` — 프리팹 Entity 참조 Baker

### ECS Systems
- `Project_Sun/Assets/Scripts/Defense/ECS/Systems/WaveSpawnSystem.cs` — 적 스폰 (ISystem + Burst)
- `Project_Sun/Assets/Scripts/Defense/ECS/Systems/EnemyMovementSystem.cs` — 적 이동 (가장 가까운 건물)
- `Project_Sun/Assets/Scripts/Defense/ECS/Systems/EnemyCombatSystem.cs` — 적 공격 + 사망 처리
- `Project_Sun/Assets/Scripts/Defense/ECS/Systems/HealthBarSystem.cs` — 체력바 타이머

### MonoBehaviour
- `Project_Sun/Assets/Scripts/Defense/BattleManager.cs` — 전투 흐름 관리, DOTS 브릿지
- `Project_Sun/Assets/Scripts/Defense/BattleCameraController.cs` — 탑다운/자유 카메라
- `Project_Sun/Assets/Scripts/Defense/BattleUIManager.cs` — 전투 UI
- `Project_Sun/Assets/Scripts/Defense/EnemyVisualManager.cs` — 체력바/사망 이펙트 풀링

## 결정사항

1. **Unity 6000.4 (6.4)에서 Entities는 코어 패키지** — manifest.json에 `"com.unity.entities": "1.0.0"` 으로 참조 (실제 내장 버전 6.4.0)
2. **낮=MonoBehaviour / 밤=DOTS 분리 아키텍처** — 전환 시점에 BattleManager가 데이터 브릿지
3. **적 아키타입 최소화** — 적 종류(Basic/Heavy/Flying)는 EnemyStats.EnemyType 필드로 구분, 별도 태그 컴포넌트 미사용
4. **ISystem + BurstCompile** — 모든 ECS 시스템에 적용
5. **오브젝트 풀링** — 체력바, 사망 이펙트 모두 MonoBehaviour 측에서 풀링

## 의존성

- `ProjectSun.Construction` — BuildingManager, BuildingSlot, BuildingHealth, BuildingData (건물 데이터 브릿지)
- `com.unity.entities` 6.4.0 (Unity 6.4 내장)
- `com.unity.entities.graphics` 6.4.0 (Unity 6.4 내장)
- Input System (`com.unity.inputsystem`) — 카메라 조작
- TextMeshPro — UI 텍스트

## 알려진 제한/TODO

- SubScene 및 적 프리팹 Entity 설정 필요 (씬 작업)
- 방어 타워 공격 시스템은 2차 태스크로 분리
- 테스트 씬 구성 필요 (스폰 포인트, 건물 배치, UI Canvas)
- 자원 소모 시스템 미연동 (PoC 범위 외)
- 턴 시스템 미연동 (밤 시작 버튼으로 대체)
