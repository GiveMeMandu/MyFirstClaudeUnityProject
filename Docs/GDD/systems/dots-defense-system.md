# DOTS 방어 시스템

- **작성일**: 2026-03-25
- **상태**: 기획 완료
- **slug**: dots-defense-system

---

## 1. 개요

### 목적
밤 페이즈에서 대규모 적 물량이 기지를 공격하는 웨이브 방어 시스템. Unity DOTS(ECS + Job System + Burst)를 활용하여 1000+ 유닛의 동시 렌더링/이동/전투를 구현한다. 낮 동안의 준비(건설, 인력 배치)가 밤의 생존을 결정하는 핵심 피드백 루프를 제공한다.

### 핵심 경험
- **물량에 압도되는 긴장감**: 수백~수천 적이 밀려오는 시각적 압박감이 메인 감정
- **처절한 과정**: 대부분의 밤 전투는 여유롭지 않으며, 자원과 인력이 부족한 상태에서 버티는 느낌
- **최후의 카타르시스**: 비축 자원을 쏟아부어 완벽한 방어를 구축했을 때의 뿌듯함 (빈번하지 않아야 가치 있음)
- **성장의 시각적 체감**: 방어 체계가 강화될수록 전투 장면이 눈에 띄게 달라져야 함

### PoC 목표
> **DOTS가 이 프로젝트에서 구현 및 적용 가능한지 기술 검증**이 최우선 목표.
> 1000 유닛 동시 스폰/이동/전투를 목표로 성능 테스트를 수행한다.

---

## 2. 시스템 설계

### 핵심 메커니즘

#### 2.1 웨이브 스폰
- 밤 페이즈 시작 시 전투 이벤트 발생
- 지정된 스폰 포인트에서 **시간 간격**을 두고 적 유닛이 웨이브 형태로 스폰
- 웨이브 구성(적 종류, 수량, 간격)은 **ScriptableObject**로 정의
- SO 기본 생성 시 턴 수 기반 **자동 스케일링** 적용 (기획자 편의)

#### 2.2 적 유닛 종류 (PoC: 3종)

| 유형 | 특성 | 크기 | 체력 | 이동속도 | 비고 |
|---|---|---|---|---|---|
| 기본 적 | 표준 지상 유닛 | 소 | 낮음 | 보통 | 물량의 주력 |
| 대형 적 | 느리지만 강인한 유닛 | 대 | 높음 | 느림 | 방어선 압박 |
| 공중 적 | 공중 이동 유닛 | 소 | 보통 | 보통 | 방벽 무시, 통과 |

#### 2.3 적 AI (단순)
1. 스폰 후 **가장 가까운 건물** 방향으로 이동
2. 공격 범위 도달 시 해당 건물 **공격**
3. 건물 파괴 시 **다음으로 가까운 건물**로 이동
4. 공중 유닛은 **방벽을 무시**하고 통과

#### 2.4 방어 타워 (2차 태스크)
- 건설 시스템의 방어 건물이 자동으로 적을 공격
- 스탯: 사거리, 공격 속도, 데미지
- 공중 유닛 공격 가능/불가능 건물 **구분 존재**

#### 2.5 전투 종료 조건

| 조건 | 결과 |
|---|---|
| 모든 적 처치 | **승리** — 밤 페이즈 종료, 통계 표시 후 다음 낮으로 |
| 본부 HP = 0 | **패배** — 게임오버 |

#### 2.6 전투 통계
밤 전투 종료 시 통계창 표시:
- 처치한 적 수 (종류별)
- 피해 입은 건물 목록 및 피해 수치
- (추후) 추가 통계 확장 가능

### 데이터 흐름

```
[입력]                              [처리]                           [출력]
─────────────────                 ─────────────────               ─────────────────
밤 시작 트리거           →     WaveSpawnSystem           →     적 Entity 생성
웨이브 SO 데이터         →     (ECB로 스폰)              →

적 Entity 위치/상태      →     EnemyMovementSystem       →     적 위치 업데이트
건물 위치 데이터         →     (가장 가까운 건물 탐색)      →

적 공격 범위 도달        →     EnemyCombatSystem         →     건물 HP 감소
건물 HP 상태            →     BuildingDamageSystem       →     건물 손상/파괴 이벤트

방어 타워 데이터(2차)    →     TowerAttackSystem         →     적 HP 감소
적 HP = 0              →     EnemyDeathSystem          →     사망 이펙트 + Entity 제거

전투 종료 조건 확인      →     BattleResultSystem        →     통계 집계 → UI 표시
```

### 상태 다이어그램

#### 밤 전투 전체 흐름
```
[낮 페이즈]
     │ 밤 시작 트리거 (PoC: 버튼)
     ▼
[전투 준비]
     │ 건물 데이터 → DOTS Entity 변환 (브릿지)
     ▼
[웨이브 진행] ◀──── 다음 웨이브 시작
     │ 적 스폰 → 이동 → 공격
     │
     ├─ 모든 적 처치 + 남은 웨이브 없음 → [승리] → 통계 → [낮 페이즈]
     └─ 본부 HP = 0 → [패배] → 게임오버
```

#### 적 유닛 상태
```
[Spawning] → [Moving] → [Attacking] → 건물 파괴 → [Moving] (다음 타겟)
                              │
                         HP = 0
                              │
                         [Dying] → 사망 이펙트 → Entity 제거
```

---

## 3. 구현 명세

### 아키텍처: MonoBehaviour ↔ DOTS 브릿지

```
┌─────────────────────────────┐     ┌─────────────────────────────┐
│      MonoBehaviour 세계       │     │        DOTS(ECS) 세계        │
│                             │     │                             │
│  BuildingSlot               │ ──→ │  BuildingEntity             │
│  BuildingManager            │     │  (위치, HP, 타워 스탯)        │
│  UI (통계, 체력바)            │ ←── │                             │
│  카메라 컨트롤               │     │  EnemyEntity                │
│  밤 시작 버튼               │ ──→ │  WaveSpawnSystem            │
│                             │     │  EnemyMovementSystem        │
│                             │ ←── │  BattleResultSystem         │
└─────────────────────────────┘     └─────────────────────────────┘

전환 시점:
  밤 시작 → MonoBehaviour 건물 데이터를 DOTS Entity로 변환
  밤 종료 → DOTS 전투 결과를 MonoBehaviour 쪽에 반영 (건물 피해량 등)
```

### 필요한 컴포넌트

#### DOTS (ECS) 컴포넌트

| 컴포넌트 | 역할 | 스크립트 위치 |
|---|---|---|
| EnemyData | 적 스탯 (HP, 속도, 공격력, 유형) | Project_Sun/Assets/Scripts/Defense/ECS/Components/ |
| EnemyMovement | 이동 상태, 현재 타겟 | Project_Sun/Assets/Scripts/Defense/ECS/Components/ |
| BuildingTarget | 건물 Entity 참조 (공격 대상) | Project_Sun/Assets/Scripts/Defense/ECS/Components/ |
| WaveSpawner | 웨이브 스폰 설정 데이터 | Project_Sun/Assets/Scripts/Defense/ECS/Components/ |
| HealthBar | 체력바 표시 타이머 (2초) | Project_Sun/Assets/Scripts/Defense/ECS/Components/ |
| DeathEffect | 사망 시 이펙트 트리거 | Project_Sun/Assets/Scripts/Defense/ECS/Components/ |

#### DOTS (ECS) 시스템

| 시스템 | 역할 | 스크립트 위치 |
|---|---|---|
| WaveSpawnSystem | 웨이브 타이밍에 따라 적 Entity 스폰 | Project_Sun/Assets/Scripts/Defense/ECS/Systems/ |
| EnemyMovementSystem | 적을 가장 가까운 건물로 이동 | Project_Sun/Assets/Scripts/Defense/ECS/Systems/ |
| EnemyCombatSystem | 적의 건물 공격 처리 | Project_Sun/Assets/Scripts/Defense/ECS/Systems/ |
| EnemyDeathSystem | HP 0인 적 처리 + 사망 이펙트 | Project_Sun/Assets/Scripts/Defense/ECS/Systems/ |
| TowerAttackSystem | 방어 타워의 적 공격 (2차 태스크) | Project_Sun/Assets/Scripts/Defense/ECS/Systems/ |
| BattleResultSystem | 전투 종료 조건 확인, 통계 집계 | Project_Sun/Assets/Scripts/Defense/ECS/Systems/ |
| HealthBarSystem | 피격 시 체력바 2초간 표시 | Project_Sun/Assets/Scripts/Defense/ECS/Systems/ |

#### MonoBehaviour 컴포넌트

| 컴포넌트 | 역할 | 스크립트 위치 |
|---|---|---|
| BattleManager | 밤 전투 전체 흐름 관리, DOTS 브릿지 | Project_Sun/Assets/Scripts/Defense/ |
| BattleCameraController | 탑다운/자유 카메라 전환, 줌 | Project_Sun/Assets/Scripts/Defense/ |
| BattleUIManager | 전투 통계, 배속 조절, 밤 시작 버튼 | Project_Sun/Assets/Scripts/UI/ |
| WaveDataSO | 웨이브 구성 ScriptableObject | Project_Sun/Assets/Scripts/Defense/Data/ |
| EnemyDataSO | 적 유닛 스탯 ScriptableObject | Project_Sun/Assets/Scripts/Defense/Data/ |

### 핵심 변수

| 변수 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| enemyHP | float | 기본:30, 대형:150, 공중:50 | 적 체력 |
| enemySpeed | float | 기본:3.0, 대형:1.5, 공중:3.0 | 적 이동 속도 |
| enemyDamage | float | 기본:5, 대형:15, 공중:8 | 적 공격력 |
| enemyAttackRange | float | 2.0 | 공격 시작 거리 |
| enemyAttackInterval | float | 1.0 | 공격 간격 (초) |
| waveInterval | float | 10.0 | 웨이브 간 대기 시간 (초) |
| spawnInterval | float | 0.1 | 웨이브 내 개별 스폰 간격 (초) |
| healthBarDuration | float | 2.0 | 피격 시 체력바 표시 시간 (초) |
| timeScale | float | 1.0 | 전투 배속 (1x / 2x) |
| towerRange | float | 8.0 | 방어 타워 사거리 (2차) |
| towerDamage | float | 10.0 | 방어 타워 데미지 (2차) |
| towerAttackSpeed | float | 1.0 | 방어 타워 공격 속도 (2차) |
| canTargetAir | bool | false | 공중 유닛 공격 가능 여부 (2차) |

### 연동 시스템

| 연동 대상 | 인터페이스 | 방향 | PoC 포함 |
|---|---|---|---|
| **건설 시스템** | 밤 시작 시 BuildingSlot 데이터 → DOTS Entity 변환. 밤 종료 시 건물 피해량 반영 | 양방향 | 최소 브릿지만 |
| **턴 시스템** | 낮→밤 전환 트리거 | 턴 → 방어 | PoC: 버튼으로 대체 |
| **자원 시스템** | 방어 타워 운영 자원 소모 | 방어 → 자원 | PoC: 미포함 |
| **카메라** | 탑다운/자유 전환, 줌인/줌아웃 | UI → 카메라 | 포함 |
| **UI** | 전투 통계, 배속 조절, 체력바 토글 | 방어 → UI | 포함 |

### 필요 패키지 (Unity 2022 LTS)

| 패키지 | UPM 이름 | 용도 |
|---|---|---|
| Entities | `com.unity.entities` | 코어 ECS |
| Entities Graphics | `com.unity.entities.graphics` | ECS Entity 렌더링 |
| Unity Physics | `com.unity.physics` | 충돌 감지 (사거리, 공격 범위) |
| Burst | `com.unity.burst` | 네이티브 컴파일 (성능) |
| Collections | `com.unity.collections` | NativeArray, NativeList 등 |
| Mathematics | `com.unity.mathematics` | Burst 호환 수학 타입 |

---

## 4. 밸런스 가이드

### 조정 가능 파라미터

**WaveDataSO** (웨이브 구성):

| 파라미터 | 조정 목적 | 권장 범위 |
|---|---|---|
| waveCount | 밤당 웨이브 횟수 | 3~8 |
| enemiesPerWave | 웨이브당 적 수 | 50~300 |
| waveInterval | 웨이브 간 유예 시간 | 5~15초 |
| spawnInterval | 스폰 밀도 | 0.05~0.5초 |
| enemyTypeRatio | 적 종류 비율 (기본:대형:공중) | 기본 70:20:10 |
| difficultyScalePerTurn | 턴당 난이도 스케일링 | 1.1x~1.3x |

**EnemyDataSO** (적 유닛 스탯):

| 파라미터 | 조정 목적 | 권장 범위 |
|---|---|---|
| hp | 적 내구도 | 기본 20~50, 대형 100~300 |
| speed | 적 이동 속도 | 1.0~5.0 |
| damage | 건물 피해량 | 3~20 |
| attackRange | 공격 시작 거리 | 1.0~3.0 |
| attackInterval | 공격 주기 | 0.5~2.0초 |

### 밸런스 기준

- **물량 압박**: 기본 적만으로도 방어선이 위협받아야 함. 여유로운 느낌 금지
- **대형 적 위협**: 1~2마리가 방어선 돌파 시 건물 1개 파괴 가능해야 함
- **공중 적 전략**: 방벽 무시이므로 반드시 대공 방어 배치 필요 (배치 안 하면 본부 직접 위협)
- **자동 스케일링**: 턴 진행에 따라 적 수/스탯이 점진적으로 증가하되, 급격한 난이도 점프 방지
- **PoC 기준**: 1000 유닛 동시 존재 시 60fps 유지 (최소 30fps)

---

## 5. 엣지 케이스

| 상황 | 처리 방법 |
|---|---|
| 적이 공격하던 건물이 파괴됨 | 다음으로 가까운 건물 탐색 후 이동 |
| 모든 방어 건물이 파괴됨 | 남은 건물(본부) 방향으로 이동 |
| 공중 유닛이 방벽에 도달 | 방벽 무시하고 통과, 내부 건물 공격 |
| 대공 방어 미배치 시 공중 유닛 | 공중 유닛이 자유롭게 본부까지 도달 (전략적 페널티) |
| 모든 적 처치 전 웨이브 종료 | 남은 적이 모두 처치될 때까지 밤 지속 |
| 밤 중 게임 종료/재접속 | PoC: 무시 (전투 상태 저장 미구현) |
| 적 스폰 위치에 건물이 있을 경우 | 스폰 포인트는 기지 외곽에 고정 배치, 건물과 겹치지 않도록 설계 |
| 1000+ 유닛 동시 사망 | ECB(EntityCommandBuffer)로 일괄 처리, 이펙트는 풀링 |
| 배속 변경 중 전투 | Time.timeScale 조정, DOTS 시스템은 deltaTime 기반이므로 자동 반영 |
| 체력바 토글 | 전역 플래그로 체력바 시스템 활성/비활성 전환 |

---

## 6. 참고 자료

### 레퍼런스 게임

| 자료 | 참고 포인트 |
|---|---|
| **They Are Billions** | 대규모 물량의 시각적 압박감, 방어선 돌파의 긴장감 |
| **Thronefall** | 기본 방어전 흐름, 간결한 전투 피드백, 탑다운 시점 |

### DOTS 기술 참고

| 자료 | 용도 |
|---|---|
| **[EntityComponentSystemSamples](https://github.com/Unity-Technologies/EntityComponentSystemSamples)** | Unity 공식 DOTS 샘플. Entities 101, Physics 101, Graphics 샘플 |
| **[Unite24 ConnectingTheDOTS](https://github.com/Unity-Technologies/Unite24ConnectingTheDOTS)** | Unite 2024 DOTS 게임 데모. 실제 프로젝트 통합 구조 참고 |
| **[EmrhnFyz/DOTS-RTS](https://github.com/EmrhnFyz/DOTS-RTS)** | 가장 유사한 커뮤니티 레퍼런스. Flow field 이동, 웨이브 스폰, 타워 타겟팅, ECS 체력/데미지 구현 |
| **[Latios Framework / Kinemation](https://github.com/Dreaming381/Latios-Framework)** | 1000+ 유닛 애니메이션 솔루션 (공식 DOTS 애니메이션 미성숙 시 대안) |
| **[Code Monkey DOTS 강좌](https://unitycodemonkey.com/video.php?v=1gSnTlUjs-s)** | Entities 1.x 기반 무료 6시간 + 유료 17시간 강좌 |
| **[Entities 1.0 공식 문서](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)** | Baker, ISystem vs SystemBase, ECB 등 핵심 API 문서 |

### 핵심 기술 노트

- **ISystem + Burst 우선**: SystemBase 대신 ISystem 사용. OnUpdate에 `[BurstCompile]` 적용 가능
- **IJobEntity**: 적 유닛 처리에 사용. IJobChunk보다 간결
- **EntityCommandBuffer.ParallelWriter**: 스폰/제거 시 사용, 동기 포인트 최소화
- **적 아키타입 최소화**: 모든 적을 가능한 한 동일 아키타입으로 유지 (상태는 enum 필드로)
- **물리 엔진 대신 직접 이동**: 1000+ 유닛에 Unity Physics 리지드바디 사용 금지. 직접 위치 업데이트
- **Baked Mesh Animation**: PoC 애니메이션은 텍스처 기반 베이크 방식 권장 (CPU 스키닝 비용 0)

---

## 7. 태스크 분리 계획

| 태스크 | 범위 | 의존성 |
|---|---|---|
| **PoC 1: DOTS 기본 세팅** | 패키지 설치, Subscene 설정, 기본 Entity 스폰 테스트 | 없음 |
| **PoC 2: 적 스폰 & 이동** | WaveSpawnSystem, EnemyMovementSystem, 1000 유닛 성능 테스트 | PoC 1 |
| **PoC 3: 적 전투 & 건물 피해** | EnemyCombatSystem, 건설 시스템 브릿지, 건물 HP 감소 | PoC 2 |
| **PoC 4: 전투 UI** | 카메라(탑다운/자유), 배속, 체력바, 통계, 밤 시작 버튼 | PoC 2 |
| **PoC 5: 사망 이펙트 & 폴리시** | 사망 이펙트, 체력바 토글, 시각적 피드백 | PoC 3 |
| **2차: 방어 타워** | TowerAttackSystem, 대공 구분, 건설 시스템 방어 건물 연동 | PoC 3 |
