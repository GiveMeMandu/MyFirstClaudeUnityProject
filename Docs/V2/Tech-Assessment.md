# 기술 타당성 평가 보고서

- **작성일**: 2026-04-03
- **버전**: v1.1
- **최종 수정일**: 2026-04-03
- **상태**: draft (리뷰 반영)
- **담당**: technical-assessor
- **참조 문서**: Vision.md (v2.6), WaveDefense.md (v0.3), Construction.md (v0.2), Workforce.md (v0.2), Exploration.md (v0.2), Economy-Model.md (v1.2)
- **평가 기준**: 1~2인 인디 개발팀, Unity 6000.x, PC/Steam 타겟

### 변경 이력

| 버전 | 날짜 | 변경 내용 |
|---|---|---|
| v1.0 | 2026-04-03 | 초안 작성 |
| v1.1 | 2026-04-03 | design-critic 리뷰 반영: (1) 프레임 시간 분배 worst-case 병기, (2) 밤 전투 중 ECS->UI 실시간 데이터 흐름 정의, (3) 세이브/로드 직렬화 방식 추가, (4) DOTS 학습 기간 비관 시나리오 추가, (5) 적 종류 수 WaveDefense.md 정합, (6) 최소 테스트 전략 추가, (7) 에디터 도구 우선순위 명확화 |

---

## 1. 기술 스택 평가

### 1.1 현재 프로젝트 환경

| 항목 | 현재 상태 | 비고 |
|---|---|---|
| **엔진** | Unity 6000.x (URP 17.4.0) | Steam PC 타겟에 적합 |
| **DOTS 패키지** | `com.unity.entities` 1.0.0, `com.unity.entities.graphics` 1.0.0 | 설치 완료, V1 ECS 코드 존재 |
| **렌더 파이프라인** | URP (Universal Render Pipeline) | 중저사양 PC 최적화에 적합 |
| **입력 시스템** | New Input System 1.19.0 | RTS 마이크로 입력에 적합 |
| **AI/내비게이션** | `com.unity.ai.navigation` 2.0.11 | NavMesh 기반. 플로우 필드 별도 구현 필요 |
| **UI** | UGUI 2.0.0 + UI Toolkit 모듈 포함 | Vision 방향: 런타임 UI Toolkit + DOTween |

### 1.2 기존 구현체 분석 (V1 Vertical Slice)

V1에서 10개 시스템의 Vertical Slice가 구현되었다. 기술적으로 주목할 점:

| 영역 | 구현 수준 | V2 재사용 가능성 |
|---|---|---|
| **ECS 전투 시스템** | 기초 ECS 컴포넌트/시스템 5종 구현 (EnemyMovement, TowerAttack, EnemyCombat, WaveSpawn, HealthBar) | **높음** -- V2 ECS 아키텍처의 출발점으로 활용 가능 |
| **건설 시스템** | MonoBehaviour 기반 (BuildingManager, BuildingSlot, BuildingData 등) | **중간** -- 데이터 모델 재사용, 로직 리팩터 필요 (V2 업그레이드 분기 구조 변경) |
| **탐사/원정** | MonoBehaviour 기반 | **중간** -- V2 노드 그래프 직선 이동 모델로 변경 필요 |
| **인력 시스템** | MonoBehaviour 기반 | **낮음** -- V2에서 소켓 모델로 완전 재설계 |
| **턴 관리** | DayNightController | **높음** -- 턴/페이즈 전환 로직 재사용 가능 |

**핵심 판단**: 기존 ECS 코드는 `ISystem` + `BurstCompile` 패턴을 올바르게 사용하고 있어 구조적 기반이 건전하다. 다만 현재 `EnemyMovementSystem`이 매 프레임 NativeArray를 할당/해제하는 패턴은 3,000개체 규모에서 GC 압박이 될 수 있어 최적화 대상이다.

### 1.3 추천 기술 스택 (V2)

| 계층 | 기술 | 근거 |
|---|---|---|
| **밤 전투 시뮬레이션** | Unity DOTS (ECS + Burst + Jobs) | 3,000개체 30fps 목표 달성의 유일한 현실적 방안 |
| **낮 페이즈 시스템** | MonoBehaviour + ScriptableObject | 건설/인력/탐사/경제는 턴 단위 이산 연산. ECS 불필요 |
| **경로 탐색** | 커스텀 플로우 필드 (ECS + Jobs) | 다수 적의 일괄 경로 탐색. `com.unity.ai.navigation`은 개별 에이전트 기반이라 500+ 동시 탐색에 부적합 |
| **렌더링** | URP + Entities Graphics + GPU Instancing | 수천 개체 동일 메시 렌더링에 GPU Instancing 필수 |
| **UI (런타임)** | UI Toolkit + DOTween | Vision 7.3 방침. 경량 스택. UGUI 대비 성능 우위 (배치 감소) |
| **데이터** | ScriptableObject (정적 데이터) + ECS ComponentData (런타임 전투) | 하이브리드 데이터 접근 |
| **오디오** | Unity Audio (기본) | 인디 규모에서 별도 미들웨어 불필요. 필요 시 FMOD/Wwise는 P2 |
| **빌드/CI** | Unity Cloud Build 또는 GitHub Actions + GameCI | 1~2인 팀에서 CI 파이프라인 초기 구축 비용을 최소화 |

---

## 2. 시스템별 타당성 평가

### 2.1 종합표

| 시스템 | 등급 | 핵심 리스크 | 권장 기술 | 비고 |
|---|---|---|---|---|
| **웨이브 방어** | **Yellow** | DOTS 학습 곡선, 3,000개체 성능, 플로우 필드 구현 | ECS + Burst + Jobs + 커스텀 플로우 필드 | 프로젝트 최대 기술 리스크. 기존 ECS 코드가 출발점 |
| **건설** | **Green** | 낮음. 업그레이드 분기 UI 복잡도 | MonoBehaviour + ScriptableObject | 가장 안전한 시스템. V1 코드 기반 리팩터 |
| **인력 관리** | **Green** | 소켓 배치 UI/UX 직관성 | MonoBehaviour + ScriptableObject | 5~15명 규모라 성능 이슈 없음. UI 설계가 관건 |
| **탐사/원정** | **Green** | 콘텐츠 볼륨 (기술 리스크 아닌 제작 리스크) | MonoBehaviour + ScriptableObject | 노드 그래프 20~40노드. 기술적 단순 |
| **경제 시스템** | **Green** | 밸런스 검증 (기술 리스크 아닌 설계 리스크) | 순수 C# 데이터 모델 | 이산 연산. 성능 고려 불필요 |
| **턴 관리** | **Green** | 낮/밤 전환 시 DOTS World 활성/비활성 | MonoBehaviour (페이즈 오케스트레이터) | 기존 DayNightController 확장 |
| **적 AI** | **Yellow** | 10~15종 적의 행동 패턴 차별화 + ECS 내 상태 머신 | ECS + Burst (상태 기반 AI) | 복잡도 중간. 우선순위 기반 단순 AI라 관리 가능 |
| **분대 RTS 마이크로** | **Yellow** | 하이브리드 입력(MonoBehaviour) + ECS 분대 이동 브릿징 | MonoBehaviour(입력/선택) + ECS(이동/전투) | 하이브리드 경계가 버그 온상. 명확한 인터페이스 필요 |
| **기술 트리** | **Green** | DAG 데이터 에디터 도구 필요 | ScriptableObject 기반 DAG | 25~44 노드. 기술적 단순 |
| **인카운터/이벤트** | **Green** | 콘텐츠 양 (기술 리스크 아닌 제작 리스크) | ScriptableObject + 이벤트 데이터 | 텍스트 + 선택지 UI |

### 2.2 상세 평가

#### 웨이브 방어 시스템 (Yellow)

**등급 근거**: 프로젝트의 핵심 기술 도전이자 차별화 요소. 3,000개체 실시간 시뮬레이션은 DOTS 없이 달성 불가능하며, DOTS 자체가 1~2인 팀에게 상당한 학습 부담을 안긴다.

**리스크 상세**:

1. **플로우 필드 구현 (리스크: 중간)**: `com.unity.ai.navigation`의 NavMeshAgent는 에이전트별 경로 계산 방식이라 500+ 동시 사용 시 프레임 드롭이 불가피하다. GDD에서 "기본: 플로우 필드, 특수 적: 개별 경로"로 명시했는데, 플로우 필드는 Unity 빌트인이 아니므로 커스텀 구현이 필요하다. 그리드 기반 플로우 필드 + Jobs 병렬 계산이 현실적 접근이다.

2. **ECS-MonoBehaviour 하이브리드 경계 (리스크: 중간)**: 밤 전투(ECS)와 낮 시스템(MonoBehaviour) 간 데이터 교환이 매 페이즈 전환마다 발생한다. `DefenseBuildingStats`, `SquadDeployed`, `BuildingDamageReport` 등의 인터페이스 계약(WaveDefense.md 섹션 12.1)이 잘 정의되어 있어 설계 리스크는 낮으나, 실제 구현에서 ECS Entity와 MonoBehaviour GameObject 간 참조 관리가 복잡해질 수 있다.

3. **Entities Graphics 렌더링 (리스크: 낮음~중간)**: `com.unity.entities.graphics` 1.0.0이 이미 설치되어 있다. 3,000개체의 동일/유사 메시를 GPU Instancing으로 렌더링하는 것은 Entities Graphics의 핵심 기능이다. 다만 적 유형별 다른 메시/애니메이션 사용 시 배치가 깨질 수 있어, 유형별 메시 종류를 최소화하는 아트 전략이 필요하다.

**권장 접근법**:
- 프로토타입 1순위: 3,000개체 이동 + 플로우 필드 + 타워 공격만으로 30fps 달성 검증
- 기존 `EnemyMovementSystem`을 플로우 필드 기반으로 리팩터
- NativeArray 매 프레임 할당 패턴을 `SystemState.GetEntityQuery` + Persistent Allocation으로 교체

#### 건설 시스템 (Green)

**등급 근거**: 고정 슬롯 + 고정 건물 설계가 기술적 복잡도를 극도로 낮춘다. 자유 배치 건설이 아니므로 그리드 충돌 검사, 배치 미리보기 레이캐스팅 등이 불필요하다. 10종 + 본부, 업그레이드 A/B 분기 후 직선 강화 구조는 ScriptableObject 데이터 주도 설계로 깔끔하게 구현 가능하다.

**기술적 주의점**:
- 슬롯 해금 체인(선행 건설/연구/탐사)이 그래프 의존성을 형성하므로, 해금 조건 평가 로직에 순환 의존 방지 검증이 필요하다.
- 건물 손상/수리 상태 머신(Locked -> Unlocked -> UnderConstruction -> Active <-> Damaged)은 V1 코드의 확장으로 구현 가능하다.

#### 인력 관리 시스템 (Green)

**등급 근거**: 5~15명의 소수 정예 모델은 성능 이슈가 전혀 없다. 시민 속성(이름, 적성, 패시브, 숙련도) 조합 계산은 턴 단위 이산 연산으로 O(N*M) (N=시민 수, M=건물 수)이며 N이 15, M이 25 수준이라 연산량이 미미하다.

**기술적 주의점**:
- 소켓 배치 UI가 시스템의 핵심 경험이므로, 드래그앤드롭 UI 품질이 관건이다. UI Toolkit의 드래그앤드롭 지원을 확인해야 한다 (UI Toolkit 2025 기준 Drag-and-Drop 이벤트 지원 확인됨).
- 숙련도 성장/하락 계산은 턴 종료 시 일괄 처리로 단순화할 수 있다.

#### 탐사/원정 시스템 (Green)

**등급 근거**: 노드 그래프 20~40 노드, 직선 이동 모델은 기술적으로 가장 단순한 시스템이다. 노드 데이터 스키마가 GDD에 잘 정의되어 있어 ScriptableObject로 직접 매핑 가능하다.

**기술적 주의점**:
- 탐사 지도 UI (노드 그래프 시각화 + 안개 해제 연출)가 주요 구현 작업이다.
- P0-full이므로 프로토타입 단계에서는 3~5노드 스텁으로 충분하다.

#### 경제 시스템 (Green)

**등급 근거**: 순수 데이터 모델이다. 자원 3종(기초/고급/유물), Faucet/Sink 구조, 저장 캡, 인플레이션 방지 메커니즘 모두 정수/부동소수점 연산이다. 성능 이슈가 발생할 여지가 전혀 없다.

**기술적 주의점**:
- 자원 흐름의 디버깅/밸런싱을 위한 에디터 도구(턴별 수입/지출 로그)를 초기부터 구축하면 밸런싱 이터레이션이 대폭 빨라진다.
- Economy-Model.md의 턴별 자원 흐름 시퀀스를 자동 시뮬레이션하는 스프레드시트 또는 에디터 윈도우 권장.

#### 분대 RTS 마이크로 (Yellow)

**등급 근거**: MonoBehaviour(입력 처리, 분대 선택 UI, 카메라 제어) + ECS(분대 NPC 이동, 전투 시뮬레이션)의 하이브리드 영역이 이 시스템의 핵심 난이도다.

**리스크 상세**:
- 플레이어 입력(New Input System) -> MonoBehaviour 분대 선택 -> ECS Entity 명령 전달 경로에서 1프레임 지연이 발생할 수 있다. GDD의 "명령 입력 후 0.1초 이내 반응" 요구사항은 60fps 기준 6프레임이므로 충분히 달성 가능하나, 입력-반응 파이프라인을 초기에 검증해야 한다.
- 일시정지 중 명령 입력 구현: `Time.timeScale = 0` 시 ECS World의 `SystemAPI.Time.DeltaTime`도 0이 되므로 분대 이동이 멈추지만, 명령 대기열에 쌓아두는 패턴이 필요하다.

---

## 3. DOTS 타당성 심층 분석

### 3.1 DOTS 적용 범위 권장

**결론: 하이브리드 ECS (밤 전투만 DOTS, 나머지 MonoBehaviour)**

| 영역 | DOTS 여부 | 근거 |
|---|---|---|
| 적 이동/AI | **DOTS** | 수천 개체 동시 이동. DOTS 필수 |
| 타워 자동공격 | **DOTS** | 10+ 타워의 타겟 선택 + 대미지 계산. Burst 최적화 대상 |
| 투사체 시뮬레이션 | **DOTS** | 동시 다수 투사체. 엔티티 풀링으로 할당/해제 최소화 |
| 분대 NPC 이동/전투 | **DOTS** | 분대당 5~10 NPC x 3~5분대 = 15~50 NPC. 적과 동일 시뮬레이션 공간 |
| 플로우 필드 계산 | **Jobs + Burst** | 그리드 기반 플로우 필드는 Jobs 병렬 처리에 이상적 |
| 건설/인력/탐사/경제 | **MonoBehaviour** | 턴 단위 이산 연산. ECS 불필요. 개발 속도가 더 중요 |
| UI | **MonoBehaviour (UI Toolkit)** | UI는 DOTS 영역 밖 |
| 턴/페이즈 관리 | **MonoBehaviour** | 게임 흐름 오케스트레이션은 MonoBehaviour가 자연스러움 |

### 3.2 ECS 학습 곡선 평가 (1~2인 팀)

| 항목 | 예상 소요 | 비고 |
|---|---|---|
| **ECS 기초** (Entity, Component, System, Query) | 완료 | V1에서 이미 구현. `ISystem` + `BurstCompile` 사용 확인 |
| **Burst 최적화** | 1~2주 | V1 코드에서 이미 `[BurstCompile]` 사용 중. 세부 최적화(SIMD, SOA 레이아웃) 학습 필요 |
| **Jobs 시스템** | 2~3주 | 플로우 필드 구현에 `IJobParallelFor`, `NativeArray` 활용 필요 |
| **Entities Graphics** | 1~2주 | GPU Instancing 설정, Material Override 등 |
| **플로우 필드 커스텀 구현** | 3~4주 | DOTS와 별개의 알고리즘 구현. 그리드 분해 + 비용 전파 + 방향 벡터 계산 |
| **ECS-MonoBehaviour 브릿지** | 2~3주 | Managed/Unmanaged 경계, EntityManager 접근 패턴 |
| **총 예상** | **8~14주** (풀타임 1인 기준) | 기존 ECS 경험이 있으므로 기초 단계 건너뜀 |

**학습 기간 신뢰 구간**:

| 시나리오 | 기간 | 전제 조건 |
|---|---|---|
| 낙관 | 8~10주 | V1 ECS 패턴을 빠르게 확장 가능. 플로우 필드 오픈소스 참조 가능 |
| 중립 | 12~16주 | Burst/Jobs 세부 최적화에서 예상치 못한 시행착오. 플로우 필드 커스텀 구현 난항 |
| 비관 | 18~22주 | V1 ECS 5종 시스템이 "기초 완료"라 부르기에 부족. ECS-MB 브릿지에서 설계 재작업 발생 |

> **주의**: 프로토타입 총 일정 14~22주(섹션 8.2)의 하한(14주)이 학습 기간 중립 시나리오(16주)보다 짧다. 학습 기간이 중립 이상으로 지연되면 프로토타입 일정이 즉시 영향받는다. 이에 대한 완화 방안으로, DOTS 학습과 낮 페이즈 MonoBehaviour 시스템 개발을 병렬화한다: 학습 기간 중 낮 시스템(건설/인력/경제)을 별도 작업 트리에서 진행하여, DOTS 학습이 지연되더라도 낮 시스템 개발이 정체되지 않도록 한다. 또한 2주 단위 체크포인트(스파이크 1 완료, 플로우 필드 PoC, 브릿지 PoC)를 설정하여 조기 폴백 판단을 가능하게 한다.

### 3.3 DOTS 실패 시 폴백 시나리오

Vision.md 7.3.1에서 정의한 폴백 플랜을 기술적으로 구체화한다.

**폴백 1단계: 엔티티 수 감소 + 시각적 보정**

| 항목 | 스펙 |
|---|---|
| 실제 엔티티 | 500~1,000 |
| 시각적 보정 | 군중 셰이더(Billboard/Sprite 군중) + 파티클 시스템으로 물량감 연출 |
| 기술 요구사항 | 커스텀 셰이더 (Shader Graph 기반 군중 렌더링), 파티클 시스템 대규모 이미터 |
| 구현 난이도 | 중간. 군중 셰이더 구현 경험이 필요하나, 레퍼런스가 풍부함 |
| 리스크 | "가짜 적"과 "진짜 적"의 시각적 괴리. 플레이어가 눈치채면 몰입감 하락 |

**폴백 2단계: 턴 기반 전투 해상도 변경**

| 항목 | 스펙 |
|---|---|
| 전투 방식 | 실시간 시뮬레이션 -> 턴 단위 결과 계산 + 결과 요약 연출 (The Last Spell 방식) |
| 기술 요구사항 | DOTS 완전 제거. MonoBehaviour 기반 전투 결과 연산 + 연출 시스템 |
| 구현 난이도 | 중간. 코드 대규모 재작성이 필요하나 기술적 리스크는 낮음 |
| 리스크 | 핵심 재미("물량 카타르시스") 손상. 비전 문서 재검토 필수 |

**폴백 1단계 자체의 기술 리스크**: 군중 셰이더 구현 경험이 필요하다고 인정했으므로, 폴백 1단계 자체가 실패할 가능성도 존재한다. 이 경우 1단계 시도에 소요된 시간(추정 2~3주)이 매몰 비용이 된다. 이를 완화하기 위해, 기술 스파이크 1 진행과 병렬로 군중 셰이더/빌보드 군중의 소규모 PoC(2~3일)를 실행하여, 1단계 폴백의 실현 가능성을 조기 확인한다. PoC가 실패하면 폴백 2단계(턴 기반)로 직행하는 것이 시간 효율적이다.

**폴백 발동 기준**: 프로토타입 단계에서 GTX 1060 + Ryzen 5 2600 환경에서 2,000개체 30fps 미달 시 폴백 1단계 발동. 1,000개체에서도 30fps 미달 시 폴백 2단계 검토.

---

## 4. 성능 예산

### 4.1 목표 하드웨어

| 항목 | 최소 사양 (GDD 기준) | 권장 사양 |
|---|---|---|
| GPU | GTX 1060 6GB | RTX 2060 |
| CPU | Ryzen 5 2600 (6C/12T) | Ryzen 5 5600X |
| RAM | 8GB | 16GB |
| 저장 장치 | HDD | SSD |
| OS | Windows 10 64-bit | Windows 10/11 64-bit |

근거: Steam Hardware Survey 중앙값 (WaveDefense.md 섹션 11.1). GTX 1060은 2026년 기준 하위 20% 수준이나 Steam 전략 게임 유저의 중저사양 타겟으로 합리적이다.

### 4.2 프레임율 예산

| 페이즈 | 목표 FPS | 근거 |
|---|---|---|
| **낮 페이즈** | 60fps | 턴제이므로 성능 부하 낮음. 60fps 안정 유지 |
| **밤 전투 (초반, ~200개체)** | 60fps | 개체 수 적음. 60fps 유지 가능 |
| **밤 전투 (중반, ~800개체)** | 60fps | DOTS + Burst 최적화로 60fps 유지 목표 |
| **밤 전투 (후반, ~2,500개체)** | 30fps (최소) | GTX 1060 기준 30fps가 현실적 최저선 |
| **Final Wave (3,000개체)** | 30fps (최소) | GDD 명시 기준. 권장 사양에서 45~60fps 목표 |

### 4.3 프레임 시간 분배 (밤 전투, 33.3ms/30fps 기준)

> **주의**: 아래 수치는 기술 스파이크 이전의 **검증 전 추정치**이다. Best-case는 Burst 최적화 + GPU Instancing이 이상적으로 작동하는 전제이며, Worst-case는 문서 내에서 이미 인정한 리스크 요인(NativeArray GC 압박, 배치 깨짐, 쿼리 미최적화 등)을 반영한다. 기술 스파이크 1 완료 후 실측치로 교체한다.

| 항목 | Best-case (ms) | Worst-case (ms) | 주요 악화 요인 |
|---|---|---|---|
| **적 이동 + AI** | 4~6 | 8~12 | NativeArray 매 프레임 할당(GC), 플로우 필드 재계산 비용, 3,000개체 AI 상태 전이 |
| **타워 공격 + 타겟 선택** | 2~3 | 4~6 | O(T*E) 탐색 비용 (T=25타워, E=3,000적). 공간 분할 미적용 시 폭발 |
| **투사체 시뮬레이션** | 1~2 | 2~3 | 투사체 동시 100+ 스파이크 시. 풀링 미적용 시 할당 비용 |
| **분대 NPC** | 1~2 | 2~3 | 적과의 전투 상호작용 연산. 규모가 작아 악화 폭 제한적 |
| **물리/충돌** | 2~3 | 5~8 | ECS 거리 검사 O(N^2) 미최적화 시. 공간 해싱 없으면 3,000 x 3,000 쌍 폭발 |
| **렌더링** | 10~14 | 16~22 | 적 유형별 메시 차이로 GPU Instancing 배치 깨짐, LOD 미적용 |
| **UI 갱신** | 1~2 | 2~3 | ECS->UI 실시간 읽기 부하 (섹션 6.5 참조) |
| **기타 (오디오, 파티클 등)** | 2~3 | 3~4 | 대규모 파티클 이미터 |
| **여유** | 3~5 | - | Worst-case에서는 여유 없음 |
| **합계 (best)** | ~33ms | - | 30fps 프레임 예산 내 |
| **합계 (worst)** | - | ~61ms | **30fps 초과. 최적화 필수** |

**Worst-case 해석**: worst-case 합계가 33ms를 크게 초과한다는 것은 "모든 것이 동시에 최악"인 극단 시나리오이며 실제로는 발생 확률이 낮다. 그러나 개별 항목이 worst-case에 도달할 가능성은 충분하며, 특히 **물리/충돌**(공간 분할 미적용)과 **렌더링**(배치 깨짐)은 최적화 없이는 worst-case에 가까워질 수 있다. 기술 스파이크 1에서 아래 항목을 최우선 프로파일링한다:

1. 공간 분할 자료구조 (Spatial Hashing / Grid Partitioning) 적용 전후 물리/충돌 비용 비교
2. 적 유형별 메시를 3~4개 기본 메시 + Material Override로 제한 시 GPU Instancing 배치 효율
3. NativeArray Persistent Allocation 전환 후 GC 스파이크 해소 여부

### 4.4 메모리 예산

| 항목 | 예산 | 근거 |
|---|---|---|
| **총 메모리** | 2GB 이하 (8GB RAM 타겟) | OS + 백그라운드 앱 고려. 게임 2GB 이하 |
| **텍스처** | 512MB~768MB | URP, 2D 스프라이트/3D 저폴리 에셋 |
| **메시/지오메트리** | 128MB~256MB | 건물 10종 + 적 10~15종 + 분대 + 맵 지형 |
| **ECS 엔티티 데이터** | 64MB~128MB | 3,000개체 x ~256 bytes/entity = ~768KB (매우 작음). 여유분은 NativeArray 등 |
| **오디오** | 64MB~128MB | 압축 오디오, 스트리밍 |
| **UI** | 32MB~64MB | UI Toolkit 스타일시트 + 텍스처 |
| **플로우 필드 그리드** | 16MB~32MB | 맵 크기 의존. 200x200 그리드 x 8bytes = ~320KB. 다중 필드 캐싱 포함 |
| **여유** | 256MB+ | GC, 임시 할당, 에디터 오버헤드 |

### 4.5 로딩 시간

| 항목 | 목표 | 근거 |
|---|---|---|
| **초기 로딩** | 10초 이내 (SSD), 20초 이내 (HDD) | 스플래시 + 메인 메뉴 로딩 |
| **기지 세션 로딩** | 5초 이내 (SSD), 10초 이내 (HDD) | 맵 + 초기 건물 + UI 로딩. Addressables 사용 시 |
| **낮/밤 전환** | 즉시 (0.5초 이내) | 전환 연출(페이드) 중 ECS World 준비. 로딩 화면 없음 |

---

## 5. 기술 리스크 레지스터

| ID | 리스크 | 발생 확률 | 영향도 | 심각도 | 완화 방안 | 상태 |
|---|---|---|---|---|---|---|
| TR-01 | **DOTS 학습 곡선으로 개발 일정 지연** | 중간 | 높음 | **높음** | (1) 기존 V1 ECS 코드 기반 점진적 확장, (2) DOTS를 밤 전투에만 한정, (3) 8~14주 학습 기간을 WBS에 명시 반영 | 미조치 |
| TR-02 | **3,000개체 30fps 미달** | 중간 | 높음 | **높음** | (1) 프로토타입 기술 스파이크 1순위, (2) 폴백 1단계(500~1,000 + 군중 셰이더), (3) 폴백 2단계(턴 기반 결과), (4) 권장 사양 상향 | 미조치 |
| TR-03 | **플로우 필드 구현 복잡도 과소평가** | 중간 | 중간 | **중간** | (1) 오픈소스 플로우 필드 구현체 조사 (Unity DOTS 커뮤니티), (2) 단순 그리드 기반 구현으로 시작, (3) 폴백: A* + 경로 캐싱 (100~200개체 규모) | 미조치 |
| TR-04 | **ECS-MonoBehaviour 브릿지 버그/복잡도** | 중간 | 중간 | **중간** | (1) 명확한 인터페이스 계약 (WaveDefense.md 섹션 12.1 기반), (2) 페이즈 전환 시 데이터 스냅샷 패턴, (3) 브릿지 레이어 단위 테스트 (섹션 5.1 참조) | 미조치 |
| TR-05 | **UI Toolkit 성숙도 부족 (드래그앤드롭, 애니메이션)** | 낮음 | 중간 | **낮음~중간** | (1) Unity 6000.x의 UI Toolkit은 충분히 성숙, (2) DOTween 연동으로 애니메이션 보완, (3) 폴백: UGUI (이미 프로젝트에 포함) | 미조치 |
| TR-06 | **1~2인 팀의 병렬 작업 병목** | 높음 | 중간 | **높음** | (1) Git worktree 활용 (이미 파이프라인 구축), (2) 시스템 경계 명확화로 독립 개발 가능, (3) 클로드 코드 에이전트 병렬 활용 | 미조치 |
| TR-07 | **콘텐츠 볼륨 부족 (이벤트 100+종, 노드 맵)** | 높음 | 중간 | **높음** | (1) P1 시스템(이벤트, 기술 트리)은 EA 출시 전 제작, (2) 이벤트 템플릿 기반 대량 생산, (3) 데이터 주도 설계로 코드 수정 없이 콘텐츠 추가 | 미조치 |
| TR-08 | **Entities Graphics 렌더링 한계 (적 유형별 메시 차이)** | 낮음 | 중간 | **낮음** | (1) 적 10~15종을 3~4개 기본 메시 + Material Override로 차별화, (2) LOD 시스템 적용, (3) 먼 거리 적은 빌보드/스프라이트 전환 | 미조치 |
| TR-09 | **밤 전투 시 GC 스파이크 (프레임 드롭)** | 중간 | 중간 | **중간** | (1) NativeArray Persistent Allocation, (2) Entity 풀링 (사전 Instantiate), (3) Profiler 상시 모니터링, (4) Incremental GC 활성화 | 미조치 |
| TR-10 | **과도한 아키텍처 설계로 개발 정체** | 중간 | 높음 | **높음** | (1) 포스트모템 교훈: "완벽한 아키텍처보다 작동하는 프로토타입", (2) YAGNI 원칙, (3) 시스템 간 느슨한 결합이면 충분, (4) 리팩터는 프로토타입 검증 후 | 미조치 |

### 5.1 최소 테스트 전략

1~2인 팀에서 전면적 TDD는 비현실적이나, DOTS 시스템은 디버깅이 일반 MonoBehaviour보다 현저히 어렵다. 아래는 최소한의 테스트 자동화 방침이다.

| 영역 | 테스트 방식 | 도구 | 우선순위 |
|---|---|---|---|
| **Bridge Layer** (BattleInitializer, BattleResultCollector, BattleUIBridge) | 단위 테스트: MB 데이터 -> ECS Entity 변환 정합성, ECS 결과 -> MB 데이터 역변환 정합성 | Unity Test Framework (EditMode) | P0-core |
| **ECS 시스템** (EnemyMovement, TowerAttack, FlowField) | 통합 테스트: ECS World 생성 -> 엔티티 투입 -> N프레임 시뮬레이션 -> 결과 검증 | Unity Test Framework (PlayMode) + ECS Test Utilities | P0-core |
| **성능 회귀** | 성능 테스트: 3,000개체 시뮬레이션 프레임 시간 측정. CI에서 자동 실행하여 회귀 감지 | Unity Performance Testing Extension | P0-full |
| **낮 페이즈 시스템** (건설/인력/경제) | 로직 테스트: 순수 C# 데이터 모델의 입출력 검증 | Unity Test Framework (EditMode) | P1 |

**원칙**: "Bridge Layer와 ECS 시스템은 반드시 테스트한다. 나머지는 버그 발생 시 회귀 테스트를 추가한다."

---

## 6. 아키텍처 제안

### 6.1 전체 구조: 하이브리드 레이어드 아키텍처

```
+--------------------------------------------------+
|                    Game Director                   |
|     (턴 관리, 페이즈 전환, 게임 상태 오케스트레이션)      |
+--------------------------------------------------+
         |                              |
         v                              v
+-------------------+    +---------------------------+
|   Day Phase       |    |     Night Phase            |
|   (MonoBehaviour) |    |     (DOTS ECS)             |
+-------------------+    +---------------------------+
| - Construction    |    | - EnemyMovementSystem     |
| - Workforce       |    | - TowerAttackSystem       |
| - Exploration     |    | - EnemyCombatSystem       |
| - Economy         |    | - ProjectileSystem        |
| - TechTree        |    | - SquadMovementSystem     |
| - Policy          |    | - FlowFieldSystem         |
| - Encounter       |    | - WaveSpawnSystem         |
+-------------------+    +---------------------------+
         |                              |
         v                              v
+--------------------------------------------------+
|              Bridge Layer (데이터 교환)               |
|   페이즈 전환 시 MonoBehaviour <-> ECS 데이터 동기화    |
+--------------------------------------------------+
         |
         v
+--------------------------------------------------+
|            Shared Data Layer                       |
|   ScriptableObject (정적 데이터: 건물, 적, 웨이브)     |
|   GameState (런타임 상태: 자원, 인력, 슬롯)            |
+--------------------------------------------------+
         |
         v
+--------------------------------------------------+
|                 UI Layer                           |
|   UI Toolkit + DOTween (런타임 UI)                  |
+--------------------------------------------------+
```

### 6.2 핵심 설계 원칙

1. **DOTS 경계를 밤 전투로 엄격히 제한**: 낮 페이즈의 어떤 시스템도 ECS를 직접 사용하지 않는다. 이것이 학습 부담과 디버깅 복잡도를 최소화하는 핵심 결정이다.

2. **Bridge Layer를 단일 지점으로 집중**: 낮->밤 전환 시 `BattleInitializer`가 MonoBehaviour 데이터를 ECS Entity로 변환하고, 밤->낮 전환 시 `BattleResultCollector`가 ECS 결과를 MonoBehaviour 데이터로 수집하며, 밤 전투 진행 중에는 `BattleUIBridge`가 ECS 데이터를 UI용으로 단방향 읽기한다(섹션 6.5). 이 세 클래스만이 양쪽 세계를 넘나든다.

3. **데이터 주도 설계**: 건물/적/웨이브/노드 정의는 모두 ScriptableObject로 관리한다. 코드 수정 없이 콘텐츠를 추가/수정할 수 있어야 한다. 이것은 밸런싱 이터레이션 속도를 결정한다.

4. **YAGNI (You Aren't Gonna Need It)**: 포스트모템 교훈(Project D -- "차별화 요소를 찾는데 마일스톤을 할당하지 않음")을 반영하여, 초기에는 최소 기능 구현에 집중하고 아키텍처를 과도하게 설계하지 않는다. 인터페이스 추상화는 시스템 간 경계에서만 적용하고, 시스템 내부는 직접적인 구현을 선호한다.

### 6.3 데이터 흐름 상세

#### 낮 -> 밤 전환 (BattleInitializer)

```
MonoBehaviour 데이터                    ECS Entity 생성
--------------------------------------------------------------
BuildingSlot[]                    -->   Building Entity + Components
  .buildingType, .upgradeLevel          (BuildingTag, BuildingData, 
  .currentHP, .socketBonus              TowerStats, LocalTransform)

SquadData[]                       -->   Squad Entity + Components
  .citizenId, .combatPower              (SquadTag, SquadStats,
  .abilities, .position                 LocalTransform)

WaveDataSO                       -->   WaveSpawner Entity
  .subWaves[], .enemies[]               (WaveData, SpawnTimer)

FlowFieldGrid                    -->   NativeArray<float3>
  .costField, .directionField           (Persistent Allocation)
```

#### 밤 -> 낮 전환 (BattleResultCollector)

```
ECS 결과 수집                           MonoBehaviour 갱신
--------------------------------------------------------------
Building Entity HP                -->   BuildingSlot.currentHP
  (손상된 건물 목록)                      .state = Damaged

Squad Entity 결과                 -->   CitizenData.injuries
  (부상자, 경험치)                        .combatExp += gained

WaveResult                        -->   TurnManager.defenseResult
  (판정 등급, 파괴 비율)                   Economy.processRewards()
```

### 6.4 턴/실시간 하이브리드 구조

```
GameDirector (MonoBehaviour)
  |
  |-- DayPhase (턴제, 무제한 시간)
  |     |-- 자원 정산 (Economy)
  |     |-- 플레이어 의사결정 (건설/인력/탐사/연구)
  |     |-- 원정대 진행 (Exploration)
  |     |-- 인카운터 발생 (Encounter)
  |     |-- "턴 종료" 버튼 클릭
  |     
  |-- PhaseTransition (낮->밤)
  |     |-- BattleInitializer.Convert()  // MB -> ECS
  |     |-- WavePreview UI 표시
  |     |-- "전투 시작" 버튼 클릭
  |     
  |-- NightPhase (실시간)
  |     |-- ECS World 시뮬레이션 (적 이동/전투)
  |     |-- 플레이어 입력 (분대 명령, 일시정지/배속)
  |     |-- 시간 조절: Time.timeScale (1x/2x/0(일시정지))
  |     |-- ECS SystemGroup은 DeltaTime 기반 업데이트
  |     
  |-- PhaseTransition (밤->낮)
  |     |-- BattleResultCollector.Collect()  // ECS -> MB
  |     |-- 결과 판정 UI 표시
  |     |-- 턴 카운터 증가
```

### 6.5 밤 전투 중 ECS -> UI 실시간 데이터 흐름

섹션 6.2의 Bridge Layer 원칙("BattleInitializer와 BattleResultCollector만이 양쪽 세계를 넘나든다")은 **페이즈 전환 시점**의 대량 데이터 교환에 해당한다. 밤 전투 **진행 중**에도 UI는 ECS 데이터를 실시간으로 읽어야 하며(체력바, 미니맵, 전투 통계 등), 이 경로는 Bridge Layer와 별도로 정의한다.

**설계 원칙**: ECS -> UI는 **단방향 읽기 전용**이다. UI가 ECS에 데이터를 쓰는 것은 분대 명령(섹션 6.4의 플레이어 입력)을 통해서만 발생하며, 이는 명령 큐 패턴으로 이미 정의되어 있다.

**아키텍처: BattleUIBridge (읽기 전용 수집기)**

```
ECS World (밤 전투 시뮬레이션)
  |
  |-- [SystemGroup 끝, 매 프레임]
  |     BattleUIDataCollectorSystem (ISystem, Burst 불가 -- Managed 접근)
  |       - EntityQuery로 UI에 필요한 데이터만 수집
  |       - NativeArray -> Managed 구조체 배열로 복사 (프레임당 1회)
  |
  v
BattleUIBridge (MonoBehaviour, 싱글턴)
  |-- BuildingHPList: (slotIndex, currentHP, maxHP)[]
  |-- EnemyCountByType: Dictionary<EnemyType, int>
  |-- WaveProgress: (currentSubWave, totalSubWaves, remainingEnemies)
  |-- SquadStatusList: (squadId, currentHP, position)[]
  |-- CombatStats: (totalKills, totalDamageDealt, totalDamageReceived)
  |
  v
UI Layer (UI Toolkit)
  - 건물 HP 오버레이: BattleUIBridge.BuildingHPList 폴링 (매 프레임)
  - 미니맵 적 표시: BattleUIBridge.EnemyCountByType (0.5초 간격)
  - 웨이브 진행 바: BattleUIBridge.WaveProgress
  - 분대 상태 패널: BattleUIBridge.SquadStatusList
```

**성능 고려사항**:
- `BattleUIDataCollectorSystem`은 SystemGroup 끝에 배치하여 시뮬레이션 연산과 경합하지 않는다.
- UI가 필요한 데이터만 선별적으로 수집한다. 3,000개체 전체가 아닌, 건물(10~25개) + 분대(3~5개) + 집계 통계만 복사하므로 비용은 0.1~0.3ms 수준으로 추정한다.
- 미니맵/통계 등 빈도가 낮은 UI는 매 프레임이 아닌 0.5초 간격으로 갱신하여 부하를 분산한다.
- 체력바 등 엔티티별 월드 스페이스 UI는 Entities Graphics의 `MaterialOverride`로 셰이더 기반 표시를 고려한다 (MonoBehaviour UI 오버헤드 회피).

**Bridge Layer 원칙과의 관계**: `BattleUIBridge`는 Bridge Layer의 세 번째 구성 요소로 추가한다. 기존 두 클래스(`BattleInitializer`, `BattleResultCollector`)가 페이즈 전환 시점의 양방향 교환을 담당하는 반면, `BattleUIBridge`는 전투 진행 중 단방향 읽기만 담당한다. "양쪽 세계를 넘나드는" 클래스를 3개로 제한함으로써 원칙을 유지한다.

### 6.6 세이브/로드 시스템 기술 설계

하이브리드 아키텍처(MonoBehaviour + ECS)에서 게임 상태 직렬화 방식을 정의한다.

**핵심 결정: 낮 페이즈에서만 세이브 가능**

| 항목 | 결정 | 근거 |
|---|---|---|
| **세이브 시점** | 낮 페이즈 중에만 (자동 저장: 매 턴 시작 시) | 밤 전투 중 ECS World 상태 직렬화는 복잡도가 높고, 수천 엔티티의 정확한 스냅샷 복원이 극히 어렵다 |
| **세이브 대상** | MonoBehaviour/GameState 데이터만 | ECS World는 세이브하지 않는다. 밤 전투 결과는 항상 BattleResultCollector를 통해 MonoBehaviour로 수집된 후 저장 |
| **포맷** | JSON (개발 중) -> Binary (출시 시) | 디버깅 용이성 우선. JsonUtility 또는 System.Text.Json 사용 |

**직렬화 대상 데이터 구조**:

```
SaveData
  |-- header: (saveName, turnNumber, timestamp, version)
  |-- gameState:
  |     |-- currentPhase: DayPhase
  |     |-- turnNumber: int
  |     |-- resources: {basic: int, advanced: int, artifact: int}
  |     |-- storageCaps: {basic: int, advanced: int, artifact: int}
  |
  |-- buildings: BuildingSlotSaveData[]
  |     (slotIndex, buildingType, upgradeLevel, currentHP, assignedCitizens[])
  |
  |-- citizens: CitizenSaveData[]
  |     (id, name, aptitude, passive, proficiencyMap, injuries, assignedSlot)
  |
  |-- exploration: ExplorationSaveData
  |     (unlockedNodes[], expeditionState, exploredEdges[])
  |
  |-- techTree: TechTreeSaveData
  |     (unlockedNodeIds[], inProgressNodeId, progressTurns)
  |
  |-- waveHistory: WaveResultSaveData[]
  |     (turnNumber, grade, buildingDamage[], citizenInjuries[])
```

**기술적 고려사항**:
- ScriptableObject의 정적 데이터(건물 정의, 적 정의 등)는 세이브하지 않는다. 게임 버전과 함께 배포되므로 참조 ID만 저장한다.
- 세이브 파일 버전 관리: `version` 필드로 하위 호환성을 처리한다. 마이그레이션 로직은 EA 이후에 구현한다.
- 세이브 슬롯: 3~5개 수동 슬롯 + 자동 저장 1개. 프로토타입에서는 1슬롯으로 시작한다.
- Shared Data Layer(섹션 6.1)의 GameState가 세이브/로드의 중심 허브가 된다. 모든 런타임 상태가 GameState를 경유하므로, 로드 시 GameState만 복원하면 각 시스템이 자신의 상태를 재구성할 수 있다.

---

## 7. 도구 및 라이브러리 권장

### 7.1 필수 패키지 (이미 포함 또는 추가 필요)

| 패키지 | 상태 | 용도 |
|---|---|---|
| `com.unity.entities` 1.0.0 | 설치됨 | ECS 핵심 |
| `com.unity.entities.graphics` 1.0.0 | 설치됨 | ECS 엔티티 렌더링 |
| `com.unity.render-pipelines.universal` 17.4.0 | 설치됨 | URP 렌더링 |
| `com.unity.inputsystem` 1.19.0 | 설치됨 | RTS 입력 처리 |
| `com.unity.ai.navigation` 2.0.11 | 설치됨 | 특수 적 개별 경로 (보조) |

### 7.2 추가 권장 패키지

| 패키지 | 용도 | 우선순위 | 비고 |
|---|---|---|---|
| **DOTween Pro** | UI 애니메이션, 건물 배치 VFX | P0-core | Vision 방침. Asset Store 구매 |
| **Unity Profiler** (빌트인) | 성능 모니터링 | P0-core | 항시 활용 |
| **Addressables** | 에셋 로딩 최적화 | P0-full | 기지별 에셋 분리 로딩 |
| `com.unity.collections` | NativeContainer 확장 | P0-core | Entities와 함께 설치됨 (확인 필요) |

### 7.3 사용하지 않을 것

| 라이브러리 | 제외 이유 |
|---|---|
| **VContainer** | 밤 전투 핵심 시스템은 ECS 기반. 낮 시스템은 규모가 작아 DI 컨테이너 오버헤드 불필요 |
| **R3 (Reactive)** | 이벤트 기반 반응형 프로그래밍은 턴 단위 이산 연산에 과도. 단순 이벤트/콜백으로 충분 |
| **UniTask** | Unity 6000.x의 `Awaitable` 빌트인으로 대체 가능. 추가 의존성 최소화 |
| **Cinemachine** | 밤 전투 카메라는 RTS 탑다운 고정 뷰. 복잡한 카메라 워크 불필요 |
| **FMOD/Wwise** | 1~2인 인디 규모에서 오디오 미들웨어는 과도. Unity Audio 기본으로 시작 |

### 7.4 빌드/CI

| 항목 | 권장 | 비고 |
|---|---|---|
| **버전 관리** | Git + GitHub (현재 사용 중) | Git LFS for 에셋 |
| **CI/CD** | GitHub Actions + GameCI (또는 Unity Cloud Build) | 빌드 자동화, 테스트 자동 실행 |
| **에디터 도구** | 아래 우선순위 참조 | 밸런싱 이터레이션 가속 |
| **프로파일링** | Unity Profiler + `unity-cli profiler` | 프레임 타임 상시 모니터링 |

**에디터 도구 우선순위** (YAGNI 원칙 적용: 밸런싱에 직접 기여하는 것만 프로토타입 단계에서 제작):

| 도구 | 단계 | 근거 |
|---|---|---|
| ScriptableObject Inspector 커스터마이징 (건물/적/웨이브 데이터) | P0-core | 공수 최소(PropertyDrawer 수준). SO 값 변경 즉시 반영되므로 별도 도구 없이 라이브 밸런싱 가능 |
| 웨이브 미리보기 (에디터 윈도우) | P0-full | 웨이브 구성 확인에 필수. SO 데이터 시각화 수준으로 공수 1~2일 |
| 턴별 자원 흐름 로그 (콘솔 출력) | P0-core | Debug.Log 수준 구현. 별도 에디터 윈도우 불필요 |
| 밸런스 시뮬레이터 (에디터 윈도우) | P1 | 공수 높음(1~2주). EA 밸런싱 단계에서 구축. 프로토타입에서는 스프레드시트로 대체 |

---

## 8. 프로토타입 우선순위 (기술 스파이크)

### 8.1 기술 스파이크 순서

| 순서 | 스파이크 | 검증 목표 | 예상 기간 | 성공 기준 | 실패 시 |
|---|---|---|---|---|---|
| **1** | **3,000개체 ECS 성능** | DOTS 기반 대규모 엔티티 시뮬레이션 성능 | 2~3주 | GTX 1060에서 3,000개체 이동 + 타워 공격 30fps | 폴백 1단계 (개체 축소 + 군중 셰이더) |
| **2** | **플로우 필드 경로 탐색** | 500+ 적 동시 경로 탐색 프레임 드롭 없음 | 2~3주 | 3,000개체 플로우 필드 기반 이동에서 추가 프레임 비용 2ms 이내 | A* + 경로 캐싱 (100~200개체) |
| **3** | **분대 RTS 마이크로 입력** | MonoBehaviour 입력 -> ECS 분대 명령 반응성 | 1~2주 | 명령 입력 후 0.1초 이내 반응. 일시정지 중 명령 입력 정상 작동 | 명령 대기열 + 예측 이동 |
| **4** | **낮/밤 페이즈 전환** | ECS World 활성/비활성 + 데이터 교환 | 1~2주 | 전환 시 0.5초 이내. 데이터 손실 없음 | 단일 World + System Enable/Disable |
| **5** | **UI Toolkit 핵심 화면** | 인력 소켓 배치 드래그앤드롭, 건설 패널 | 2~3주 | 드래그앤드롭 자연스러움, 60fps UI 갱신 | UGUI 폴백 |

### 8.2 프로토타입 마일스톤

```
[스파이크 1~2] ---- 4~6주 -----> DOTS 성능 검증 완료
     |                               |
     |                          폴백 판단 게이트
     |                               |
[스파이크 3~4] ---- 2~4주 -----> 하이브리드 전환 검증
     |
[스파이크 5]   ---- 2~3주 -----> UI 핵심 화면 검증
     |
[P0-core 프로토타입] -- 4~6주 --> 낮의 건설/인력 + 밤의 웨이브 방어
     |                            (1기지, 10턴, 최소 콘텐츠)
     |
[플레이테스트] ---- 2주 -------> 핵심 재미 검증
                                  (카타르시스 + 에이전시 + 한 턴만 더)
```

**총 예상**: 기술 스파이크 8~14주 + P0-core 프로토타입 4~6주 + 플레이테스트 2주 = **14~22주 (3.5~5.5개월)**

> **비관 시나리오 주의** (섹션 3.2 참조): DOTS 학습이 비관 시나리오(18~22주)에 도달하면 총 일정은 24~30주(6~7.5개월)로 확장될 수 있다. 이를 완화하기 위해 DOTS 학습 기간 동안 낮 페이즈 시스템(건설/인력/경제)을 별도 worktree에서 병렬 개발한다. 2주 단위 학습 체크포인트에서 진행 상황을 평가하여 조기 폴백 판단을 내린다.

---

## 9. 포스트모템 교훈 반영

### 9.1 Project D 교훈

| 교훈 | 적용 |
|---|---|
| "프로덕트가 시장에서 어떤 비전을 가지고 있는지 명확하지 않음" | Vision.md에서 시장 포지셔닝과 화이트 스페이스를 명확히 정의함. 기술 선택도 이 비전에 종속 |
| "차별화 요소를 찾는데 마일스톤을 할당하지 않음" | 기술 스파이크 1~2순위를 DOTS 성능 검증에 할당. 핵심 차별화(물량 카타르시스)의 기술적 실현 가능성을 가장 먼저 검증 |
| "하드마일스톤에서 프로젝트 비전 확인 필요" | P0-core 프로토타입이 하드마일스톤 역할. 여기서 DOTS 성능 + RTS 마이크로 재미를 모두 검증 |

### 9.2 일반 포스트모템 교훈

| 교훈 | 적용 |
|---|---|
| **과도한 아키텍처 설계** | DOTS를 밤 전투로 한정. 나머지는 MonoBehaviour. "YAGNI" 원칙 적용 |
| **기술 리스크 후순위 검증** | 기술 스파이크를 WBS 최전방에 배치. 가장 불확실한 것을 가장 먼저 검증 |
| **저비용 빠른 개발 기조** | 인디 1~2인 팀에 맞는 최소 기술 스택. DI 컨테이너/리액티브 프레임워크 등 과도한 추상화 배제 |
| **프로토타입 우선** | 완벽한 아키텍처보다 작동하는 프로토타입. 리팩터는 검증 후 |

---

## 10. 권장 사항 요약

### 10.1 즉시 실행 항목 (다음 스프린트)

1. **기술 스파이크 1 착수**: 3,000개체 ECS 이동 + 타워 공격 성능 벤치마크. 기존 `EnemyMovementSystem`을 확장하되, NativeArray 할당 패턴을 Persistent로 전환.
2. **플로우 필드 리서치**: Unity DOTS 커뮤니티/Asset Store에서 플로우 필드 구현체 조사. 없으면 그리드 기반 커스텀 구현 계획 수립.
3. **프로파일링 환경 구축**: GTX 1060 + Ryzen 5 2600 사양의 테스트 환경 확보 또는 에뮬레이션 설정.

### 10.2 아키텍처 결정 사항 (프로토타입 전 확정)

1. **하이브리드 ECS**: 밤 전투만 DOTS, 나머지 MonoBehaviour. 이 결정을 확정하고 팀 전체가 공유.
2. **Bridge Layer 설계**: `BattleInitializer` / `BattleResultCollector` 인터페이스를 WaveDefense.md 섹션 12.1 기반으로 구체화.
3. **데이터 아키텍처**: ScriptableObject 기반 정적 데이터 + GameState 싱글턴(또는 서비스 로케이터) 패턴 확정.

### 10.3 하지 않을 것

1. 낮 페이즈 시스템을 ECS로 구현하지 않는다.
2. DI 컨테이너, 리액티브 프레임워크를 게임 시스템에 도입하지 않는다.
3. 프로토타입 전에 시스템 간 추상화 레이어를 과도하게 설계하지 않는다.
4. 최적화를 프로파일링 없이 추측으로 수행하지 않는다.
5. 멀티플레이어/네트워크 코드를 고려하지 않는다 (싱글플레이어 전용).

---

## 11. 최종 등급

| 전체 기술 타당성 | **Yellow (조건부 실현 가능)** |
|---|---|
| **Green 요소** | 낮 페이즈 전체 시스템(건설/인력/탐사/경제/기술트리) -- 기술적으로 직접적이고 검증된 패턴 |
| **Yellow 요소** | 밤 전투 DOTS 시스템 -- 핵심 차별화 요소이자 최대 기술 리스크. 1~2인 팀의 DOTS 학습 곡선이 관건 |
| **Red 요소** | 없음 -- 설계 문서의 폴백 플랜이 적절히 준비되어 있음 |
| **조건** | 기술 스파이크 1~2에서 3,000개체 30fps가 확인되면 Green 전환. 실패 시 폴백 1단계 적용 후에도 Green 유지 가능 |

**핵심 메시지**: 이 프로젝트의 기술적 실현 가능성은 **DOTS 밤 전투 성능 검증 단 하나에 달려 있다**. 나머지 모든 시스템은 표준 Unity 개발로 충분히 구현 가능하다. 기술 스파이크를 WBS 최전방에 배치하여 불확실성을 가장 먼저 제거하는 것이 프로젝트 성패를 좌우한다.
