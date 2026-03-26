# 시대 전환/메타 진행 시스템

- **작성일**: 2026-03-26
- **상태**: 기획 완료
- **slug**: era-transition-system

---

## 1. 개요

### 목적
개별 기지 세션을 넘어선 장기 목표와 내러티브 규모감을 제공하는 메타 진행 시스템. 플레이어는 시대 내 기지를 하나씩 클리어하며 옴니버스 서사를 경험하고, 모든 기지 클리어 시 다음 시대로 전환된다. 게임플레이 상태는 기지 간 완전히 독립되며, 연결은 순수 내러티브로만 이루어진다.

### 핵심 경험
- **세션을 넘어선 목표**: "이 기지를 클리어하면 다음 이야기가 열린다"는 동기 부여
- **완전한 독립 세션**: 매 기지마다 0에서 시작하는 신선함, 캐리오버 없는 공정한 도전
- **옴니버스 내러티브**: 기지별 독립 서사이지만 시대 내에서 이야기가 연결되는 발견의 재미
- **규모감 있는 진행**: 시대 전환 시 기술 수준/환경이 변화하며 게임이 다른 층위로 진입

---

## 2. 시스템 설계

### 핵심 메커니즘

#### 2.1 계층 구조

```
[게임 전체]
  ├── [시대 1: 여명기]
  │     ├── [기지 1-1: 첫 번째 정착지] (튜토리얼)
  │     ├── [기지 1-2: 광야의 전초기지]
  │     └── [기지 1-3: 폭풍의 요새]
  ├── [시대 2: 확장기]
  │     ├── [기지 2-1: ...]
  │     ├── [기지 2-2: ...]
  │     └── [기지 2-3: ...]
  └── [시대 3~5: ...]
        └── ...
```

- 시대 3~5개, 시대당 기지 3~5개
- MVP: 1시대, 기지 1~2개로 축소 검증

#### 2.2 시대 데이터

| 필드 | 타입 | 설명 |
|---|---|---|
| eraId | string | 시대 고유 ID (예: "era_01_dawn") |
| eraName | string | 시대 이름 (예: "여명기") |
| eraDescription | string | 시대 배경 설명 텍스트 |
| eraTheme | EraTheme (enum) | 시각적/청각적 테마 (환경, 색감, BGM 등) |
| bases | List\<BaseDataSO\> | 포함된 기지 목록 (순서 = 플레이 순서) |
| unlockCondition | EraUnlockCondition | 해금 조건 (이전 시대 올클리어) |
| transitionCutsceneId | string | 시대 전환 시 재생할 컷신/텍스트 ID |

#### 2.3 기지 데이터

| 필드 | 타입 | 설명 |
|---|---|---|
| baseId | string | 기지 고유 ID (예: "base_01_01") |
| baseName | string | 기지 이름 |
| baseDescription | string | 기지 설명 (선택 화면용) |
| totalTurns | int | 시나리오 총 턴 수 |
| clearCondition | ClearCondition | 클리어 조건 (N턴 생존 / 특정 목표 달성) |
| initialResources | ResourceSet | 초기 자원 (기초/고급/방어) |
| initialWorkers | int | 초기 인력 수 |
| slotLayout | SlotLayoutSO | 건설 슬롯 배치 데이터 |
| wallExpansionData | WallExpansionDataSO | 방벽 확장 데이터 |
| waveTable | WaveTableSO | 웨이브 구성 |
| encounterTable | EncounterTableSO | 턴별 인카운터 구성 |
| techTree | TechTreeSO | 기지 고유 기술 트리 |
| introNarrativeId | string | 기지 시작 시 인트로 텍스트 ID |
| outroNarrativeId | string | 기지 클리어 시 엔딩 텍스트 ID |
| specialRules | List\<SpecialRuleSO\> | 시나리오 특수 규칙 (선택적) |

#### 2.4 기지 독립성 원칙
- **완전 리셋**: 기지 시작 시 자원/인력/건물/연구 모든 상태 초기화
- **캐리오버 없음**: 이전 기지의 게임플레이 상태가 다음 기지에 영향 없음
- **내러티브만 연결**: 기지 클리어 엔딩 텍스트가 다음 기지 인트로에 복선/암시 제공
- **기지별 고유 경험**: 각 기지마다 다른 슬롯 배치, 기술 트리, 웨이브 구성, 특수 규칙

#### 2.5 클리어 조건

| 조건 타입 | 설명 | 예시 |
|---|---|---|
| SurviveTurns | N턴 생존 | "20턴 동안 본부를 지켜라" |
| BuildTarget | 특정 건물 건설 완료 | "연구소를 건설하고 Lv.3까지 업그레이드" |
| ResourceTarget | 자원 N 이상 축적 | "고급 자원 200 확보" |
| WallLevel | 방벽 레벨 N 달성 | "방벽을 Lv.3까지 확장" |
| Composite | 복합 조건 (AND/OR) | "20턴 생존 AND 방벽 Lv.2 이상" |

- `ClearConditionSO`에 조건 타입과 목표값을 설정
- 턴 종료 시 `ClearConditionChecker`가 조건 달성 여부 확인

#### 2.6 게임 흐름

```
[타이틀 화면]
       │
       ▼
[시대 선택 화면]
  현재 해금된 시대 표시
  클리어한 시대에 완료 마크
       │ 시대 선택
       ▼
[기지 선택 화면]
  시대 내 기지 목록 (순서대로)
  각 기지: 이름, 설명, 클리어 상태
  잠긴 기지: 이전 기지 클리어 필요
       │ 기지 선택
       ▼
[인트로 내러티브]
  기지 배경 설명 + 목표 제시
       │ 확인
       ▼
[기지 플레이]
  (기존 턴 루프: 낮→밤→낮→...)
       │ 클리어 조건 달성
       ▼
[아웃트로 내러티브]
  결과 텍스트 + 다음 기지로의 복선
       │ 확인
       ▼
[기지 선택 화면으로 복귀]
  (or 마지막 기지였으면 시대 전환 연출)
```

#### 2.7 시대 전환

```
[시대 N 마지막 기지 클리어]
       │
       ▼
[시대 전환 연출]
  ① 시대 N 총괄 텍스트 (성과 요약)
  ② 시대 전환 컷신/일러스트
  ③ 시대 N+1 소개 텍스트 (새 환경/위협 소개)
       │
       ▼
[시대 N+1 기지 선택 화면]
```

#### 2.8 패배 처리
- 기지 플레이 중 패배(본부 파괴) 시 → 결과 화면 → 기지 선택 화면 복귀
- 동일 기지 재도전 가능 (게임플레이 상태 완전 리셋)
- 패배 횟수는 기록하지 않음 (SaveData에 클리어 여부만 저장)

### 데이터 흐름

```
[입력]                              [처리]                           [출력]
─────────────────                 ─────────────────               ─────────────────
시대 선택 (플레이어)      →     MetaProgressManager        →     기지 목록 로딩
기지 선택 (플레이어)      →     BaseSessionManager         →     기지 초기화 + 인트로
클리어 조건 달성          →     ClearConditionChecker      →     아웃트로 + 기지 클리어 기록
시대 내 전체 클리어       →     MetaProgressManager        →     시대 전환 연출 + 다음 시대 해금
SaveData 로드            →     MetaProgressManager        →     시대/기지 클리어 상태 복원
```

---

## 3. 구현 명세

### 필요한 컴포넌트

| 컴포넌트 | 역할 | 스크립트 위치 |
|---|---|---|
| MetaProgressManager | 시대/기지 클리어 상태 관리, 해금 조건 판정 | Project_Sun/Assets/Scripts/Meta/ |
| EraDataSO | 시대 정적 데이터 (이름, 기지 목록, 해금 조건) | Project_Sun/Assets/Scripts/Meta/ |
| BaseDataSO | 기지 정적 데이터 (모든 시나리오 설정 포함) | Project_Sun/Assets/Scripts/Meta/ |
| ClearConditionSO | 클리어 조건 정의 (타입, 목표값) | Project_Sun/Assets/Scripts/Meta/ |
| ClearConditionChecker | 턴 종료 시 클리어 조건 달성 여부 확인 | Project_Sun/Assets/Scripts/Meta/ |
| BaseSessionManager | 기지 세션 초기화/종료 처리 (자원/인력/건물 리셋) | Project_Sun/Assets/Scripts/Meta/ |
| NarrativeManager | 인트로/아웃트로/시대 전환 텍스트 표시 | Project_Sun/Assets/Scripts/Meta/ |
| MetaSaveData | 시대/기지 클리어 상태 직렬화 | Project_Sun/Assets/Scripts/Meta/ |
| EraSelectUI | 시대 선택 화면 UI | Project_Sun/Assets/Scripts/UI/ |
| BaseSelectUI | 기지 선택 화면 UI | Project_Sun/Assets/Scripts/UI/ |

### 핵심 변수

| 변수 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| currentEraIndex | int | 0 | 현재 선택된 시대 인덱스 |
| currentBaseIndex | int | 0 | 현재 선택된 기지 인덱스 |
| eraList | List\<EraDataSO\> | - | 전체 시대 목록 |
| clearedBases | Dictionary\<string, bool\> | {} | 기지 클리어 상태 (baseId → 클리어 여부) |
| clearedEras | Dictionary\<string, bool\> | {} | 시대 클리어 상태 (eraId → 클리어 여부) |
| highestUnlockedEra | int | 0 | 해금된 최고 시대 인덱스 |
| narrativeSpeed | float | 0.05 | 내러티브 텍스트 표시 속도 (초/글자) |
| eraTransitionDuration | float | 3.0 | 시대 전환 연출 시간 (초) |

### 연동 시스템

| 연동 대상 | 인터페이스 | 타이밍 | PoC 포함 |
|---|---|---|---|
| **턴 시스템** | `TurnManager` 초기화 시 BaseDataSO의 totalTurns 주입 | 기지 시작 시 | O |
| **자원 시스템** | `ResourceManager` 초기화 시 BaseDataSO의 initialResources 주입 | 기지 시작 시 | O |
| **인력 시스템** | `WorkforceManager` 초기화 시 BaseDataSO의 initialWorkers 주입 | 기지 시작 시 | O |
| **건설 시스템** | `BuildingManager` 초기화 시 BaseDataSO의 slotLayout 주입 | 기지 시작 시 | O |
| **방벽 확장** | `WallExpansionManager` 초기화 시 BaseDataSO의 wallExpansionData 주입 | 기지 시작 시 | O |
| **방어 시스템** | `BattleManager` 초기화 시 BaseDataSO의 waveTable 주입 | 기지 시작 시 | O |
| **기술 트리** | BaseDataSO의 techTree 로딩 | 기지 시작 시 | X (추후) |
| **저장 시스템** | MetaSaveData 직렬화/역직렬화 | 기지 클리어/앱 종료 시 | O |

### 구현 단위 (Sub-Features)

| ID | 구현 단위 | 포함 스크립트 | 의존 단위 | 크기 |
|---|---|---|---|---|
| SF-01 | 데이터 모델 | EraDataSO.cs, BaseDataSO.cs, ClearConditionSO.cs, MetaEnums.cs | - | M |
| SF-02 | MetaProgressManager 코어 | MetaProgressManager.cs (시대/기지 상태 관리, 해금 판정) | SF-01 | M |
| SF-03 | BaseSessionManager | BaseSessionManager.cs (기지 초기화, 각 시스템에 데이터 주입) | SF-01 | L |
| SF-04 | ClearConditionChecker | ClearConditionChecker.cs (턴 종료 시 조건 확인) | SF-01, SF-03 | M |
| SF-05 | 내러티브 표시 | NarrativeManager.cs (인트로/아웃트로/시대 전환 텍스트) | SF-02 | M |
| SF-06 | MetaSaveData | MetaSaveData.cs (JSON 직렬화, PlayerPrefs or 파일) | SF-02 | S |
| SF-07 | 시대/기지 선택 UI + 테스트 | EraSelectUI.cs, BaseSelectUI.cs | SF-02~06 | L |

#### 의존성 그래프

```
SF-01 ──→ SF-02 ──→ SF-05
      ──→ SF-03 ──→ SF-04
SF-02 ──→ SF-06
SF-02~06 ──→ SF-07
```

#### 실행 순서
1. SF-01 (데이터 모델)
2. SF-02 (MetaProgressManager 코어)
3. SF-03 (BaseSessionManager) — SF-02와 병렬 가능
4. SF-04 (ClearConditionChecker)
5. SF-05 (내러티브 표시) — SF-04와 병렬 가능
6. SF-06 (MetaSaveData) — SF-04와 병렬 가능
7. SF-07 (시대/기지 선택 UI + 테스트)

---

## 4. 밸런스 가이드

### 조정 가능 파라미터

| 파라미터 | 조정 목적 | 권장 범위 |
|---|---|---|
| 시대당 기지 수 | 시대 볼륨 | 3~5개 (MVP: 1~2개) |
| 기지별 총 턴 수 | 세션 길이 | 15~30턴 |
| 기지별 초기 기초 자원 | 시작 여유도 | 80~150 |
| 기지별 초기 고급 자원 | 초반 선택지 | 0~50 |
| 기지별 초기 방어 자원 | 첫 전투 여유 | 10~30 |
| 기지별 초기 인력 | 시작 인력 | 3~6명 |
| 클리어 조건 난이도 | 기지 난이도 | 생존 턴 수 / 목표 수치 조절 |
| 기지 잠금 방식 | 순서 강제 여부 | 순차 해금 or 선택 가능 (SO 설정) |
| 내러티브 텍스트 속도 | 텍스트 연출 | 0.03~0.08초/글자 |
| 시대 전환 연출 시간 | 전환 체감 | 2.0~5.0초 |

### 밸런스 기준

- **세션 길이**: 기지 1개 = 30~60분 플레이 (15~30턴 기준)
- **난이도 곡선**: 시대 내 첫 기지는 쉽게, 마지막 기지는 어렵게 (초기 자원/웨이브 강도 조절)
- **튜토리얼 기지**: 시대 1 기지 1은 시스템 학습용 — 넉넉한 자원, 약한 웨이브, 단순한 클리어 조건
- **시대 간 난이도 점프**: 시대 전환 시 기본 웨이브 강도/특수 규칙이 한 단계 상승
- **재도전 부담**: 패배 시 처음부터지만, 세션이 30~60분이므로 재도전 심리적 부담 적음

---

## 5. 엣지 케이스

| 상황 | 처리 방법 |
|---|---|
| 클리어한 기지 재플레이 | 허용. 게임플레이 상태 완전 리셋. 재클리어해도 진행 상태 변화 없음 |
| 시대의 중간 기지만 클리어 | 발생 불가 — 기지는 순차 해금 (이전 기지 클리어 필수) |
| 기지 플레이 중 앱 종료 | 기지 내 진행 상태 저장하지 않음. 다음 실행 시 기지 선택 화면에서 시작 |
| SaveData 손상/없음 | 모든 시대/기지 미클리어 상태로 초기화 (시대 1 기지 1부터 시작) |
| 마지막 시대 클리어 | "게임 클리어" 엔딩 연출 → 타이틀 화면 복귀. 자유 재플레이 가능 |
| 기지 데이터에 클리어 조건 없음 | 기본값 사용 (SurviveTurns, totalTurns만큼 생존) |
| 복합 클리어 조건 부분 달성 | UI에 조건별 달성/미달성 표시. 모든 조건 충족 시에만 클리어 |
| 기지 플레이 중 자발적 포기 | "기지 포기" 버튼 → 확인 팝업 → 기지 선택 화면 복귀 (패배 아님, 기록 없음) |

---

## 6. 참고 자료

| 자료 | 참고 포인트 |
|---|---|
| **Frostpunk** | 시나리오 기반 독립 세션, 각 시나리오별 고유 규칙/목표 |
| **Into the Breach** | 완전 독립 세션, 짧은 플레이 시간, 재도전이 부담 없는 구조 |
| **FTL** | 런 기반 독립 세션, 메타 진행은 해금만 (게임플레이 캐리오버 없음) |
| **Docs/GDD/Vision.md** | "옴니버스: 기지별 독립 서사, 시대 내 연결", "완전 독립 세션" 원칙 |
| **턴 시스템 기획서** | ScenarioDataSO 구조, 클리어/패배 판정 흐름 |
| **자원 시스템 기획서** | 초기 자원 설정, ResourceManager 초기화 인터페이스 |
| **방벽 확장 기획서** | WallExpansionDataSO 기지별 주입 구조 |
