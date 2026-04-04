# M0 S0-1 Code Review
Last Updated: 2026-04-04 (수정 완료: 2026-04-04)

## 수정 이력
| 이슈 | 파일 | 상태 |
|---|---|---|
| C-01 BuildingDamageBuffer IBufferElementData 전환 | BuildingComponents.cs, EnemyCombatSystemV2.cs, EnemyCombatSystem.cs, BattleManager.cs, ECSBenchmarkController.cs | FIXED |
| C-02 EnemyDeathSystem ECB 패턴 | EnemyCombatSystemV2.cs, EnemyCombatSystem.cs | FIXED |
| C-03 UI enum 중복 → Data Layer 통합 | CitizenCardController.cs, BuildingCardController.cs, TechSpikeTestScreen.cs | FIXED |
| I-01 FlowFieldSystem [BurstCompile] 분리 | FlowFieldSystem.cs | FIXED |
| I-02 SquadCommandQueue Domain Reload 대응 | SquadCommandQueue.cs | FIXED |
| I-03 SaveManager 예외 처리 | SaveManager.cs | FIXED |
| I-04 FlowFieldAgent 벤치마크 연결 | ECSBenchmarkController.cs | FIXED |
| I-05 SquadInputController EntityQuery 캐싱 | SquadInputController.cs | FIXED |
| I-06 ResourceState.Spend() 가드 | ResourceState.cs | FIXED |
| **컴파일 검증** | — | **PASS (에러 0건)** |

---

## Summary

M0 Tech Spike 코드 전체 (~40개 C# 파일)를 검토했다. 컴파일은 성공한 상태이며, 각 에이전트의 레이어 내부 품질은 전반적으로 양호하다. 그러나 에이전트 간 통합 경계에서 구조적 결함이 3건 발견되었고, ECS 코드에서 Burst 안전성 문제와 NativeContainer 패턴 오류가 확인되었다.

---

## Score: 6.5/10

| 항목 | 점수 | 비고 |
|---|---|---|
| Data Layer 품질 | 8/10 | 직렬화 일관성 우수, 네임스페이스 혼선 1건 |
| DOTS/ECS 품질 | 6/10 | Burst 안전성 문제, ECB 패턴 오류, SharedStatic 위험 |
| UI Layer 품질 | 7/10 | 이중 enum 정의 문제, SimpleTween 누수 잠재 |
| 에이전트 간 통합 | 5/10 | 네임스페이스 불일치, Data-ECS 결합 없음 |
| 프로젝트 컨벤션 | 9/10 | New Input System 준수, DI/R3 미사용 |

---

## Critical Issues (must fix)

### [C-01] EnemyCombatSystemV2: `BuildingDamageBuffer`가 실제 IBufferElementData가 아닌 IComponentData

**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Components/BuildingComponents.cs:26`  
**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Systems/EnemyCombatSystemV2.cs:62-65`

`BuildingDamageBuffer`의 이름은 "Buffer"이지만 `IComponentData`로 선언되어 있다. ECS의 `IBufferElementData`와 전혀 다른 타입이다. 현재 구현에서 여러 적이 동일 프레임에 같은 건물을 공격하면 데미지가 마지막 1개 적의 공격만 반영되는 **데이터 레이스** 위험이 있다. `AccumulatedDamage`에 `+=` 연산을 하지만, 동일 엔티티를 여러 이터레이션에서 ReadWrite 접근 시 순서 보장이 없다.

```csharp
// 현재 (잠재적 데이터 레이스)
var damageBuffer = SystemAPI.GetComponentRW<BuildingDamageBuffer>(target.ValueRO.TargetEntity);
damageBuffer.ValueRW.AccumulatedDamage += stats.ValueRO.Damage;
```

수정 방향: `EntityCommandBuffer`로 데미지 이벤트를 별도 엔티티로 기록한 후 합산하거나, `[NativeDisableContainerSafetyRestriction]` + 명시적 Interlocked 사용, 또는 실제 `IBufferElementData`로 변경.

---

### [C-02] EnemyDeathSystemV2: `EntityCommandBuffer(Allocator.Temp)` 잘못된 ECB 패턴

**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Systems/EnemyCombatSystemV2.cs:92`

```csharp
var ecb = new EntityCommandBuffer(Allocator.Temp);
```

`Allocator.Temp`는 Job에서 ECB를 사용할 때 지원되지 않는다. 현재 코드는 `[BurstCompile]` 어트리뷰트가 붙어있는 `OnUpdate` 내에서 관리되는데, Burst 컴파일된 함수 내에서 `Allocator.Temp` ECB는 예외를 발생시킬 수 있다. 또한 `DestroyEntity`와 `AddComponent` 두 개의 루프를 돌리면서 `[BurstCompile]`의 이점이 사라진다.

수정 방향: `BeginSimulationEntityCommandBufferSystem` 또는 `EndSimulationEntityCommandBufferSystem`에서 ECB를 가져와야 한다.

```csharp
// 올바른 패턴
var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
    .CreateCommandBuffer(state.WorldUnmanaged);
```

---

### [C-03] UI Layer의 `CitizenAptitude`, `BuildingCategory` 중복 enum 정의 — Data Layer와 불일치

**파일**: `Project_Sun/Assets/UI/Scripts/Components/CitizenCardController.cs:8`  
**파일**: `Project_Sun/Assets/UI/Scripts/Components/BuildingCardController.cs:8`

UI 레이어가 `ProjectSun.V2.Data` 네임스페이스의 `CitizenAptitude`와 `BuildingCategoryV2`를 사용하지 않고 별도로 로컬 enum을 정의했다.

- `ProjectSun.V2.Data.CitizenAptitude`: `None, Construction, Combat, Research, Exploration` (5개 값)
- `ProjectSun.UI.Components.CitizenAptitude`: `Combat, Construction, Exploration` (3개 값, `None`과 `Research` 누락)

실제 게임에서 `CitizenRuntimeState.aptitude == CitizenAptitude.Research`인 시민을 UI가 표현할 수 없다. 두 타입은 컴파일 레벨에서는 분리되어 있어 경고도 없다.

수정 방향: UI 컴포넌트들이 `ProjectSun.V2.Data` 네임스페이스의 enum을 직접 사용하도록 변경.

---

## Important Issues (should fix)

### [I-01] FlowFieldSystem.OnUpdate: `[BurstCompile]` 표시됐으나 관리 코드 포함

**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Pathfinding/FlowFieldSystem.cs:57-210`

`OnUpdate`에 `[BurstCompile]`이 붙어 있지만, 내부에서:
- `NativeList<float3>` `NativeList<float>` `NativeList<bool>` `NativeList<float3>`를 `Allocator.TempJob`으로 할당
- `SharedFlowField.Grid.Data`에 Persistent allocation을 수행 (초기화 시)
- `FindPassableNeighbor` 내에서 직접 `NativeArray` 인덱스 접근

`Allocator.TempJob`은 Burst에서 허용되나, `_initialized` 분기 내에서 새 `FlowFieldGrid` (6개 NativeArray Persistent 할당)를 직접 하는 것은 Burst 컴파일러가 관리 코드로 판단하면 fallback이 발생할 수 있다. 실제 컴파일 경고 여부를 확인해야 한다.

---

### [I-02] SquadCommandQueue: Static NativeQueue — 도메인 리로드 시 누수

**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Squad/SquadCommandQueue.cs`

`static NativeQueue<SquadCommandEntry> _queue`는 Domain Reload(Play Mode 종료 시 Unity가 스크립트를 다시 로드할 때) 이후 해제되지 않은 상태로 남을 수 있다. Unity에서 Domain Reload가 발생하면 static 필드는 초기화되지만 NativeContainer는 네이티브 메모리라 누수가 발생한다.

수정 방향: `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 어트리뷰트로 Dispose 보장하거나, `AssemblyReloadEvents.beforeAssemblyReload`에 Dispose를 연결.

---

### [I-03] SaveManager: 싱글 슬롯 하드코딩 + 예외 처리 없음

**파일**: `Project_Sun/Assets/Scripts/Core/SaveLoad/SaveManager.cs`

`File.WriteAllText` / `File.ReadAllText`에 예외 처리가 없다. 디스크 공간 부족, 권한 문제, 파일 손상 시 게임이 크래시된다. 또한 `save_slot_0.json` 하드코딩은 멀티슬롯 지원 불가 구조다 (V2 스펙이 멀티슬롯을 요구하는지 확인 필요).

```csharp
// 최소 수정
try { File.WriteAllText(SaveFilePath, json); }
catch (Exception e) { Debug.LogError($"[SaveManager] Save failed: {e.Message}"); return false; }
```

---

### [I-04] EnemyMovementSystemV2: 플로우 필드와 병행 사용 시 충돌 위험

**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Systems/EnemyMovementSystemV2.cs`

V2 코드에는 플로우 필드(`FlowFieldMovementSystem`, `ProjectSun.V2.Defense.ECS`)와 직접 최근접 건물 탐색(`EnemyMovementSystemV2`, `ProjectSun.Defense.ECS`) 두 이동 시스템이 모두 활성화된다. `FlowFieldMovementSystem`은 `FlowFieldAgent` 컴포넌트가 있는 적만 처리하도록 설계되어 있으나, `FlowFieldAgent` 컴포넌트를 실제로 적 엔티티에 추가하는 코드가 벤치마크 컨트롤러에 없다. 즉, 현재 상태에서는 플로우 필드가 실제로 사용되지 않는다.

---

### [I-05] SquadInputController: 매 프레임 EntityQuery 생성

**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Squad/SquadInputController.cs:175,195`

`SelectSquad()`와 `DeselectAll()`에서 매번 `em.CreateEntityQuery()`를 호출한다. EntityQuery 생성은 비용이 있으며, 분대 선택은 사용자 입력마다 발생한다. Query는 필드로 캐시해야 한다.

---

### [I-06] ResourceState.Spend(): 음수 검증 없음

**파일**: `Project_Sun/Assets/Scripts/Core/State/ResourceState.cs:30-36`

`Spend()`는 `CanAfford()` 확인 없이 직접 차감한다. 호출자가 항상 `CanAfford()`를 먼저 확인한다고 가정하지만, 복수 시스템이 동일 턴에 지출을 시도할 경우 자원이 음수가 될 수 있다. 방어 코드를 내부에 포함하는 것이 안전하다.

---

## Minor Suggestions

### [S-01] WaveDataSOV2: `HashSet<AttackDirection>` → 메모리 할당

**파일**: `Project_Sun/Assets/Scripts/Data/WaveDataSOV2.cs:66`

`GetDirectionCount()`는 `new HashSet<AttackDirection>()`을 생성한다. 에디터 도구용이면 허용되지만, 런타임에서 매 턴 호출된다면 GC 압박이 될 수 있다. 서브웨이브 수가 최대 8개이므로 간단한 비트마스크(int)로 대체 가능하다.

---

### [S-02] FlowFieldGrid: `FlowDir.Offsets`가 `static readonly` managed array

**파일**: `Project_Sun/Assets/Scripts/Defense/ECS/Pathfinding/FlowFieldGrid.cs:130`

`public static readonly int2[] Offsets`는 관리 힙 배열이다. Burst Job에서 이 배열을 직접 참조할 수 없다 (현재 Jobs에서는 switch-case로 우회하고 있어 실제 참조하지 않음). 문서화 용도로는 괜찮으나, 실수로 Job 내에서 참조하면 Burst 오류가 발생한다는 주석을 추가하는 것이 좋다.

---

### [S-03] SimpleTween: silent catch 블록

**파일**: `Project_Sun/Assets/Scripts/UI/Scripts/Util/SimpleTween.cs:111,128`

`try { t.Setter(value); } catch { /* element may be gone */ }` 패턴은 오류를 완전히 삼킨다. PoC에서는 허용되지만, 실제 게임 코드에서는 `catch (Exception e)` + 최소 `Debug.LogWarning`이 필요하다.

---

### [S-04] BuildingCardController: 생성자에서 UI 트리를 코드로 구성

**파일**: `Project_Sun/Assets/Scripts/UI/Scripts/Components/BuildingCardController.cs:43-127`

BuildingCardController는 `BuildingCard.uxml`이 있음에도 생성자에서 UI 트리를 완전히 코드로 구성한다. UXML 템플릿을 `VisualTreeAsset.CloneTree()`로 인스턴스화하는 것이 UI Toolkit의 권장 패턴이며, 코드-UXML 중복 유지가 필요 없어진다.

---

### [S-05] EnemyDataSOV2 `attackRange` 필드명 충돌

**파일**: `Project_Sun/Assets/Scripts/Data/EnemyDataSOV2.cs:17,32`

클래스에 `public EnemyAttackRange attackRange` (enum)와 `public float attackRangeValue` (수치) 두 필드가 있다. 이름이 혼란스럽다. `attackRangeValue` → `attackRangeDistance`로 명확화 권장.

---

## Cross-Agent Integration Audit

| 영역 | 상태 | 비고 |
|---|---|---|
| Data Layer enum → ECS 참조 | 미사용 | ECS V2 코드는 `ProjectSun.Defense.ECS` V1 컴포넌트만 사용. `ProjectSun.V2.Data` enum을 ECS에서 전혀 참조하지 않음 |
| Data Layer enum → UI 참조 | 불일치 | UI가 독립 enum 정의. `CitizenAptitude` 값 3개 vs 5개 불일치 (C-03) |
| SaveManager → GameState | 정상 | `GameState` 직렬화 연결 올바름 |
| SORegistry → BuildingDataSO | 정상 | ID 기반 룩업 패턴 준수 |
| ECS V1 → ECS V2 네임스페이스 | 부분적 | V2 시스템들이 V1 컴포넌트(`ProjectSun.Defense.ECS`)를 `using` 으로 참조. 의도적이나 명시적 브릿지 문서 없음 |
| FlowField → EnemyMovement 공존 | 위험 | `FlowFieldAgent` 미할당으로 FlowField 미사용 상태 (I-04) |
| SquadCommandQueue 생명주기 | 취약 | SquadCommandSystem과 SquadInputController 양쪽에서 Initialize() 호출 가능, Dispose 도메인 리로드 취약 (I-02) |

---

## Interface Contract Status

_Interface-Contracts.md 기준, M0에서 구현된 계약의 정합성 확인_

| 계약 | 구현 상태 | 비고 |
|---|---|---|
| `DefenseBuildingStats(slotId, attackPower, range, hp)` 밤 전투 시작 시 전달 | 미구현 | M0 스파이크 범위 밖. `BuildingData.SlotIndex`는 존재하나 전달 파이프라인 없음 |
| `BuildingDamageReport(slotIds[])` 밤 전투 종료 시 | 부분 구현 | `BuildingDamageBuffer.AccumulatedDamage` 필드 존재. MonoBehaviour 수신 측 미구현 |
| `SquadDeployed(squadId, combatPower, size, abilities[])` 밤 전투 시작 시 | 미구현 | Squad ECS 컴포넌트 정의됨. WaveDefense 시스템에서 수신 측 없음 |
| `WaveResult` 경제 정산 | 부분 구현 | `WaveResult` 구조체에 `basicReward`, `advancedReward`, `relicReward` 필드 존재. 실제 경제 시스템 연결 없음 |

---

## DOTS Quality Checklist

| 항목 | 상태 | 비고 |
|---|---|---|
| `[BurstCompile]` 시스템 적용 | 대부분 적용 | EnemyMovementSystemV2, TowerAttackSystemV2, EnemyCombatSystemV2, FlowFieldJobs, FlowFieldMovementSystem 모두 적용 |
| NativeContainer Persistent Dispose | 대부분 안전 | OnDestroy에서 IsCreated 체크 후 Dispose. SpatialHashMap은 Dispose 인터페이스 구현 |
| Managed type in IComponentData | 없음 | 모든 IComponentData는 blittable 타입만 사용 |
| EntityCommandBuffer 패턴 | 오류 있음 | EnemyDeathSystemV2: Allocator.Temp ECB를 [BurstCompile] 내에서 사용 (C-02) |
| SharedStatic 사용 | 잠재 위험 | `SharedFlowField.Grid`가 `FlowFieldGrid` (여러 NativeArray 포함)를 SharedStatic으로 공유. 두 시스템이 동일 프레임에 읽기/쓰기할 경우 동기화 이슈 가능 |
| Job dependency 체인 | 일부 누락 | FlowFieldSystem에서 `state.Dependency = default`로 초기화. 이전 시스템의 dependency를 무시할 수 있음 |
| 매 프레임 NativeContainer 할당 | 해소됨 | V2 시스템들은 Persistent 할당으로 전환됨 (Tech-Assessment.md 지적사항 반영) |

---

## Positive Observations

- **ECS V2 성능 최적화 방향 올바름**: EnemyMovementSystemV2가 V1의 매 프레임 NativeArray 할당 패턴을 Persistent NativeList로 전환한 것은 Tech-Assessment.md 권장사항을 정확히 반영.

- **SpatialHashMap 구현 품질**: O(T*E) → O(T*K) 최적화를 위한 커스텀 SpatialHashMap이 Burst-compatible struct로 깔끔하게 구현되어 있다. Build/QueryRange 패턴도 올바르다.

- **플로우 필드 파이프라인**: InitCost → MarkObstacles → IntegrationField(BFS) → DirectionField(병렬) 4단계 파이프라인이 IJob/IJobParallelFor를 적절히 구분해 사용했다. 지상/공중 듀얼 필드 설계도 GDD 요구사항(Flying 적 벽 통과)을 반영했다.

- **Data Layer 직렬화 일관성**: `GameState`, `ResourceState`, `BuildingRuntimeState`, `CitizenRuntimeState`, `WaveResult` 등 모든 런타임 상태 클래스에 `[Serializable]`이 일관되게 적용되어 있다.

- **SquadCommandQueue 설계 의도**: MonoBehaviour(입력) → NativeQueue → ECS 단방향 명령 전달 패턴은 하이브리드 경계를 관리하는 좋은 접근이다. 일시정지 시 명령 큐잉 동작도 올바르게 주석화되어 있다.

- **UI Toolkit 이벤트 바인딩 대칭성**: TechSpikeTestScreen의 OnEnable/OnDisable에서 이벤트 등록/해제가 대칭적으로 구현되어 있다. DragDropManager도 Dispose() 패턴을 따른다.

- **New Input System 준수**: 검토된 모든 파일에서 `UnityEngine.Input` 참조 없음. SquadInputController가 InputAction을 코드에서 직접 생성하는 방식은 PoC 의존성 최소화 측면에서 적절하다.

---

## Next Steps

1. **C-01, C-02 즉시 수정** (데이터 레이스, ECB 패턴): 다음 ECS 구현 전 반드시 해소해야 함.
2. **C-03 수정**: UI → Data Layer enum 통합은 실제 GameState와 UI 연결 시 버그 원인이 됨.
3. **I-04 확인**: FlowField가 실제로 활성화되지 않는 문제 — 벤치마크에서 FlowFieldAgent 컴포넌트 추가 또는 FlowFieldSetup 씬 배치 필요.
4. **I-02 수정**: 개발 중 Play Mode 반복 실행이 많으므로 Domain Reload 메모리 누수는 실질적 개발 장애가 됨.
