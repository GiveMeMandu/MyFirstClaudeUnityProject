# 게임 개발 시장 조사 방법론 종합 보고서

- **작성일**: 2026-04-01
- **카테고리**: practice / market-research
- **대상 장르**: 턴제 기지 경영 + 타워 디펜스 혼합 장르
- **레퍼런스 게임**: Frostpunk, Thronefall, They Are Billions, Reigns, Civ5
- **상태**: 조사완료

---

## 1. 요약 (Executive Summary)

인디 게임 시장 조사의 핵심은 **데이터 기반 장르 포지셔닝**과 **경쟁 게임 분석**을 통한 시장 갭 식별이다. 2024년 Steam에는 약 18,900개의 게임이 출시되었으며, 상위 13%만이 의미 있는 수익($11K+)을 달성한다. 소규모 인디 팀에게는 SteamDB, SteamSpy, Gamalytic 같은 무료 도구만으로도 충분한 시장 인텔리전스를 수집할 수 있다. "턴제 기지 경영 + 타워 디펜스" 혼합 장르는 기존 분류에 명확히 존재하지 않는 시장 갭 영역으로, 신중히 포지셔닝하면 차별화 기회가 있다.

---

## 2. 시장 조사 방법론 (Pre-Development Research)

### 2.1 전문 스튜디오의 리서치 프로세스

프로페셔널 스튜디오는 아이디어 단계부터 출시 후까지 다음 단계를 반복한다:

```
[콘셉트] → 시장 가능성 검증 → [기획] → 경쟁 분석 → [개발] → 플레이어 조사 → [출시]
         ↑_______________________________________피드백 루프_________________________↑
```

**핵심 원칙**: 리서치는 "일회성 이벤트"가 아니라 지속적인 활동이다.

### 2.2 TAM/SAM/SOM 프레임워크

게임 개발에 적용한 TAM/SAM/SOM 분석:

| 레벨 | 정의 | 예시 (우리 게임) |
|------|------|-----------------|
| **TAM** (Total Addressable Market) | 전체 PC 전략 게임 시장 | PC 전략 게임 플레이어 전체 (~수억 명) |
| **SAM** (Serviceable Addressable Market) | Steam 기반 턴제/기지건설 장르 | SteamDB 기준 관련 태그 게임의 플레이어 풀 |
| **SOM** (Serviceable Obtainable Market) | 실제 획득 가능한 시장 | 초기 목표 1-5만 판매 |

**글로벌 게임 시장 규모 (Newzoo 2025)**: $188.8B, 플레이어 수 36억 명
**인디 게임 Steam 수익 점유율**: 전체 Steam 수익의 약 48%

### 2.3 시장 조사의 GDD 연계

시장 조사 결과가 GDD에 반영되는 지점:

```
시장 조사 발견 → GDD 반영 위치
─────────────────────────────────────────────
경쟁 게임 불만 리뷰 분석 → 핵심 기능/차별점 섹션
플레이어 페르소나 → 타겟 유저 섹션
장르 포화도 분석 → 마켓 포지셔닝 섹션
가격대 분석 → 비즈니스 모델 섹션
비슷한 게임 세션 길이 데이터 → 게임 플로우/세션 설계 섹션
리뷰 키워드 분석 → 튜토리얼/UX 설계 섹션
```

### 2.4 베스트 프랙티스 (GDC 인사이트)

- **GDC 2025 State of the Industry**: 인디 개발자의 50%가 자기 자금 조달
- **핵심 교훈**: "새로운 게임이 새 플레이어를 만들지 않고, 기존 게임에서 시간을 빼앗는다"
- **권장 접근법**: 조기에 Steam 스토어 페이지를 열어 위시리스트를 수집하고, 시장 반응을 데이터로 검증한 후 개발에 투자

---

## 3. 데이터 분석 도구 상세 가이드

### 3.1 SteamDB (steamdb.info) — 무료

**역할**: Steam의 모든 것을 추적하는 종합 데이터베이스

**주요 기능**:
- **Player Charts**: 라이프타임 동시 접속자 수 추이 (경쟁 게임 건강도 파악)
- **Price History**: 전 지역 가격 변동 이력 (가격 전략 벤치마킹)
- **Update History**: 게임 업데이트 빈도 및 내용 (개발사 활동성 파악)
- **Related Apps**: 유사 게임 자동 추천 (숨겨진 경쟁자 발견)
- **Followers**: Steam 팔로워 추이 (마케팅 모멘텀 파악)
- **Tags Explorer**: 태그별 게임 목록 + 리뷰 수 (서브장르 전체 현황)
- **Most Wishlisted**: 실시간 위시리스트 상위 게임 (시장 수요 신호)
- **DAU Leaderboard**: 일간 활성 사용자 순위 (현재 인기 파악)

**인디 개발자 활용 방법**:
1. 레퍼런스 게임 앱 ID를 SteamDB에서 검색
2. `Charts` 탭 → CCU 그래프로 출시 후 생명주기 파악
3. `Update History`로 얼마나 자주, 어떤 패치를 내는지 확인
4. `Related Apps`로 숨어있는 경쟁자 파악
5. `Tags` 탭에서 해당 게임이 사용한 태그 전체 확인

**URL**: https://steamdb.info/

### 3.2 Steam Charts (steamcharts.com) — 무료

**역할**: 플레이어 수 트렌드 시각화

**핵심 데이터**: 월별/일별 평균 플레이어 수, 최고 동시 접속자 수

**활용법**:
- 경쟁 게임의 출시 직후 vs 6개월 후 플레이어 수 비교로 리텐션 추정
- 계절적 패턴 파악 (세일 시즌 스파이크 등)
- 장르 전체의 트렌드 방향성 (성장/유지/쇠퇴) 파악

**URL**: https://steamcharts.com/

### 3.3 SteamSpy (steamspy.com) — 무료 (제한적)

**역할**: 게임 소유자 수 및 장르별 통계

**주의사항**: 2018년 Valve의 프라이버시 정책 변경 후 정확도 저하. 현재는 넓은 범위 추정치만 제공 (예: "200,000~500,000 소유자")

**유용한 기능**:
- 장르/태그별 게임 목록 및 소유자 수 추정
- `Genre Stats` 페이지: 장르별 총 소유자 수, 평균 플레이 시간
- 무료 API로 대규모 데이터 수집 가능

**URLs**:
- 메인: https://steamspy.com/
- API: https://steamspy.com/api.php
- 인디 장르: https://steamspy.com/genre/Indie

### 3.4 Gamalytic (gamalytic.com) — 무료/유료

**역할**: 매출 추정 + 게임 성과 종합 분석

**주요 기능**:
- 개별 게임 추정 판매량 및 수익
- 플레이어 수 데이터
- Steam Analytics 섹션: 장르/태그별 수익성 분석
- 시장 조사 가이드 블로그 포함

**Thronefall 데이터 예시**:
- 추정 판매량: 315,000 카피 (2023년 10월 기준)
- 추정 매출: ~$1.5M USD
- 가격: $7 (Early Access 출시가)

**URL**: https://gamalytic.com/

### 3.5 VGInsights (app.sensortower.com/vgi) — 유료 (~$20/월)

**역할**: 가장 포괄적인 Steam 시장 인텔리전스 플랫폼 (2024년 SensorTower에 인수)

**주요 기능**:
- 게임당 50개+ 데이터포인트 (150,000개+ 게임 커버리지)
- 정밀한 가격 분석 (연도별 수익, 가격대별 분포)
- Global Indie Games Market Report 발간 (연간 무료 공개)
- 수익 추정 방법론 문서화

**2024 글로벌 인디 게임 리포트 활용**:
- URL: https://app.sensortower.com/vgi/assets/reports/VGI_Global_Indie_Games_Market_Report_2024.pdf

### 3.6 Game Data Crunch (gamedatacrunch.com) — 무료

**역할**: 성과 지표 + 리뷰 종합 분석

**특징**:
- 멀티소스 검증 (여러 데이터 소스 교차 확인)
- 24시간마다 업데이트
- 장르/테마 순위

### 3.7 Steam 250 (steam250.com) — 무료

**역할**: 태그별/장르별 최고 평점 게임 랭킹

**활용법**: `steam250.com/tag/<태그명>` 형식으로 특정 태그 최고 게임 확인
- 예: https://steam250.com/tag/base_building
- 예: https://steam250.com/tag/tower_defense

### 3.8 Steambase (steambase.io) — 무료

**역할**: Steam 게임 통합 스탯 + 차트

**특징**: SteamDB 대비 UI가 직관적, Player Score 자체 산출

### 3.9 GameDiscoverCo Newsletter (newsletter.gamediscover.co) — 무료/유료

**역할**: Simon Carless의 주간 Steam 시장 심층 분석

**필독 기사 유형**:
- PC 신작 시장의 '형태' 분석 (수익 분포 데이터)
- 게임별 성공 사례 딥다이브
- 위시리스트 전환율 현황

**2024년 핵심 데이터** (GameDiscoverCo):
| 수익 순위 | 연간 총수익 (세전) |
|-----------|-------------------|
| #25 신작 | $33.5M |
| #50 신작 | $12.2M |
| #100 신작 | $5.0M |
| #250 신작 | $1.43M |
| #500 신작 | $457K |
| #1,000 신작 | $118K |
| #2,500 신작 (상위 13%) | ~$11K |

### 3.10 모바일 특화 도구 (PC 인디에는 참고용)

| 도구 | URL | 가격 | 특징 |
|------|-----|------|------|
| AppMagic | appmagic.rocks | $380/월+ | 14M 앱, 500+ 태그, D1-D360 리텐션 |
| Sensor Tower | sensortower.com | $25K-$40K/년 | 가장 포괄적, 대형 스튜디오용 |
| Data.ai (App Annie) | data.ai | 기업용 | 앱스토어 + 구글플레이 종합 |
| GameRefinery | gamerefinery.com | 기업용 | 100K+ 모바일 게임 피처 분석 |

**소규모 인디 팀 권장**: PC 게임이라면 모바일 도구는 시장 트렌드 참고용으로만 사용

---

## 4. 경쟁 분석 프레임워크

### 4.1 경쟁사 분류

```
직접 경쟁자 (Direct): 동일 장르, 동일 타겟 오디언스
  → They Are Billions, Thronefall, Frostpunk 2 (비슷한 메커니즘)

간접 경쟁자 (Indirect): 다른 장르이나 같은 플레이어 풀
  → Civ6, RimWorld, Oxygen Not Included (전략/경영 게이머)

영감 게임 (Aspirational): 완전히 다른 장르이나 디자인 참고
  → Reigns (간결한 의사결정), Hades (반복 플레이)
```

### 4.2 분석 데이터 수집 체크리스트

각 경쟁 게임에 대해 수집할 데이터:

- [ ] **기본 정보**: 출시일, 가격, Early Access 여부, 팀 규모
- [ ] **리뷰 수**: (× 30~50 = 추정 판매량, Boxleiter Method)
- [ ] **리뷰 비율**: 긍정/부정 % (게임 품질 지표)
- [ ] **CCU**: 최대 동시 접속자 수 (SteamDB/SteamCharts)
- [ ] **현재 CCU**: 1년 후 플레이어 수 (리텐션 지표)
- [ ] **위시리스트 추이**: SteamDB Followers 그래프
- [ ] **업데이트 빈도**: SteamDB Update History
- [ ] **태그 목록**: 게임이 사용한 Steam 태그 전체
- [ ] **부정 리뷰 키워드**: 가장 많이 언급되는 불만사항
- [ ] **긍정 리뷰 키워드**: 가장 많이 칭찬받는 요소
- [ ] **가격 변동**: 주요 할인 시점과 폭

### 4.3 매출 추정 방법 (Boxleiter Method)

```
추정 판매량 = Steam 리뷰 수 × Boxleiter 배수

Boxleiter 배수 기준:
  - 인디/내러티브: 20~30x
  - 전략/경영: 30~50x
  - 액션/FPS: 50~80x (리뷰를 덜 작성)

추정 매출 = 추정 판매량 × 출시 가격 × 0.7 (Steam 30% 수수료)
           × 0.85 (환불율 약 15% 가정)

※ 범위로 표현: 계산값의 0.5x ~ 1.5x
```

**예시 계산 — They Are Billions**:
- 리뷰 수: ~50,000개
- Boxleiter 배수: 40x (전략 장르)
- 추정 판매량: 2,000,000 카피 (실제 알려진 값: 900,000+ 카피, 일치 범위 내)
- 출시가: $29.99 → 추정 매출: ~$40M~60M (Early Access 포함 누적)

### 4.4 리뷰 감성 분석

**무료 방법**:
1. Steam 스토어 "Most Helpful" 부정 리뷰 상위 20개 읽기
2. 반복되는 키워드 수동 집계
3. "not recommended"의 공통 이유 → 우리 게임에서 해결할 기회

**도구 활용**:
- **SteamReviewAI** (steamreviewai.com): AI 기반 리뷰 패턴 분석
- **IMPRESS Review Word Cloud**: 태그 클라우드 생성

**분석할 질문**:
- 플레이어가 가장 좋아하는 것은?
- 가장 많이 요구하는 추가 기능은?
- 어떤 이유로 환불/부정 리뷰를 남기는가?
- 얼마나 플레이하고 리뷰를 남기는가?

### 4.5 장르 포화도 분석

**Steam 태그 기반 분석**:
1. SteamDB → Tags 검색 → 해당 태그의 전체 게임 수 확인
2. 연도별 출시 수로 시장 성장/포화 트렌드 파악
3. 리뷰 없는 게임 비율 = 시장 실패율 지표

**기지건설 + 타워디펜스 + 턴제 조합 분석**:
```
Steam 태그 조합 검색 예시:
- "Base Building" + "Tower Defense": 약 200~300개 게임 → 포화되지 않음
- "Base Building" + "Turn-Based Strategy": 약 100~150개 게임 → 틈새 영역
- "Tower Defense" + "Turn-Based": 약 50~80개 게임 → 비교적 비어있음
```

---

## 5. 플레이어/오디언스 리서치

### 5.1 Quantic Foundry 플레이어 동기 모델

Quantic Foundry는 125만 명 이상의 게이머 데이터를 분석하여 6개 동기 그룹, 12개 요인으로 구성된 모델을 개발:

| 동기 그룹 | 세부 요인 | 전략/경영 게임 관련성 |
|-----------|-----------|----------------------|
| **Action** | Excitement, Destruction | 중간 (타워 디펜스 요소에 해당) |
| **Social** | Competition, Community | 낮음 (싱글플레이어 게임) |
| **Mastery** | Challenge, Strategy | **매우 높음** (핵심 동기) |
| **Achievement** | Completion, Power | **높음** (기지 완성, 성장) |
| **Creativity** | Fantasy, Design | 높음 (기지 건설 자유도) |
| **Immersion** | Story, Discovery | 중간 (분위기/세계관) |

**우리 게임 타겟 플레이어 프로필**:
- 주요 동기: **Mastery (Strategy)** + **Achievement (Completion)**
- 연령대: 주로 25~40대 (Frostpunk 플레이어 통계 기반)
- 성별: 남성 비율 높음 (~70%), 여성도 의미 있는 비율
- 플레이 패턴: 세션당 1~3시간, 총 30~100시간 플레이 기대

**참고 URL**: https://quanticfoundry.com/gamer-motivation-model/

### 5.2 Bartle 플레이어 유형과 게임 설계

전략/경영 게임에서 각 Bartle 유형별 설계 고려사항:

| 유형 | 특징 | 우리 게임 대응 요소 |
|------|------|---------------------|
| **Achiever** | 목표 달성, 수집 | 미션/도전과제/별점 평가 시스템 |
| **Explorer** | 발견, 실험 | 다양한 전략/빌드 경로, 숨겨진 메커니즘 |
| **Socializer** | 공유, 커뮤니티 | 스크린샷 공유, 리더보드 (선택적) |
| **Killer** | 경쟁, 지배 | 하드코어 모드, 높은 난이도 옵션 |

### 5.3 커뮤니티 리서치 방법

**Reddit 기반 리서치**:

리서치 대상 서브레딧:
- r/gaming (일반)
- r/indiegaming (인디 게임)
- r/pcgaming (PC 게임)
- r/strategy (전략 게임)
- r/4Xgaming (4X 게임)
- r/towerdefense (타워 디펜스)
- r/citybuilder (도시 건설)
- r/rts (RTS)

**검색 방법**:
1. `site:reddit.com "They Are Billions" "wish"` 같은 문구로 불만/요청 검색
2. 각 서브레딧의 "Most Upvoted" 게시물에서 패턴 파악
3. "What do you want to see in a [장르] game?" 유형 스레드 수집

**Discord 커뮤니티 리서치**:
- Frostpunk, They Are Billions 공식 Discord 서버 참가
- 플레이어들이 자주 언급하는 기능/불만 관찰
- "Feature Request" 채널에서 수요 파악

### 5.4 플레이어 페르소나 개발 템플릿

```
페르소나 이름: [예: "전략가 철민"]

기본 정보:
  - 나이: 28세
  - 직업: IT 직군 / 직장인
  - 게임 경력: 10년+, 주로 PC 전략 게임

게임 플레이 패턴:
  - 하루 플레이 시간: 1~2시간 (퇴근 후)
  - 주당 플레이 일수: 3~5일
  - 선호 장르: 전략, 경영, 로그라이크

보유 게임 목록:
  - Frostpunk, Civ6, RimWorld, They Are Billions
  
동기:
  - 복잡한 시스템 최적화에서 오는 지적 만족
  - "완벽한 빌드" 달성의 성취감
  - 어려운 도전을 극복했을 때의 카타르시스

불만/페인포인트:
  - 학습 곡선이 너무 가파른 게임
  - 튜토리얼이 없거나 설명이 부족한 게임
  - 세이브/로드가 불편한 게임
  - 모바일 게임처럼 느껴지는 인터페이스

구매 결정 요인:
  - 친구 추천 또는 유튜버 리뷰
  - Steam 위시리스트 할인 알림
  - 데모 플레이 후 구매
```

---

## 6. 산업 트렌드 분석

### 6.1 신뢰할 수 있는 산업 보고서

| 기관 | 보고서 | 주기 | 비용 | URL |
|------|--------|------|------|-----|
| **Newzoo** | Global Games Market Report | 연간 | 무료 (요약본) | newzoo.com |
| **VGInsights** | Global PC/Indie Games Market Report | 연간 | 무료 | app.sensortower.com/vgi |
| **GDC** | State of the Game Industry | 연간 | 무료 | reg.gdconf.com |
| **GameDiscoverCo** | 주간 Steam 분석 | 주간 | 무료/유료 | newsletter.gamediscover.co |
| **How To Market A Game** | 벤치마크 데이터 | 수시 | 무료 | howtomarketagame.com/benchmarks |
| **GameDeveloper.com** | 산업 기사/GDC 콘텐츠 | 상시 | 무료 | gamedeveloper.com |

### 6.2 장르 트렌드 추적 방법

**Steam 기반 트렌드 추적**:
1. SteamDB Tags 페이지 → 태그별 게임 수 증가 추이
2. Steam Charts → 장르별 게임의 플레이어 수 트렌드
3. Steam Next Fest 참가작 모니터링 → 6개월 후 성과 추적

**2024-2025년 주요 트렌드**:
- **Early Access 일반화**: 인디 스튜디오의 기본 전략으로 정착
- **로그라이크 하이브리드**: 전략 게임 + 로그라이크 조합이 계속 성장
- **"미니멀리스트" 디자인**: Thronefall의 성공이 증명한 단순 + 깊이 전략
- **Steam 포화**: 연간 19,000개 게임 출시 → 마케팅 중요도 증가
- **가격 전략 재고**: $40+ 게임이 상위 250위 진입 확률 62%로 저가 대비 압도적

**$20-$30 가격대가 인디 전략 게임의 황금 구간**: 상위 250위 진입률 19.2%

### 6.3 Steam 태그 트렌드 모니터링

**실시간 트렌드 확인 방법**:
1. Steam 스토어 → "인기 신작" / "베스트셀러" 페이지 주기적 확인
2. SteamDB → "Most played games" / "Upcoming games" 섹션
3. Gamalytic → "Steam Analytics" → 장르별 수익성 순위

---

## 7. 우리 게임 (턴제 기지 경영 + 타워 디펜스) 특화 분석

### 7.1 시장 포지셔닝 맵

```
                    복잡도 (High)
                        │
    RimWorld            │           Civ 5/6
    Dwarf Fortress      │           
                        │    [우리 게임 목표 영역]
─────────────────────────────────────────────── 실시간 ↔ 턴제
    They Are Billions   │           Reigns
    Frostpunk           │
                        │    Thronefall (단순)
                    복잡도 (Low)
```

**목표 포지션**: 중간 복잡도 + 턴제 + 기지경영 + 타워 디펜스 하이브리드
- 이 조합은 명확한 시장 공백 (기존 게임들이 각 요소를 따로 가짐)

### 7.2 관련 Steam 태그 목록

**필수 태그** (해당 플레이어 풀을 끌어오기 위해 반드시 사용):
- `Turn-Based Strategy`
- `Base Building`
- `Tower Defense`
- `Strategy`
- `Management`

**추가 태그** (장르 확장 및 검색 노출):
- `City Builder` (도시 건설 유사성이 있다면)
- `Survival` (자원 관리/위협 요소가 있다면)
- `Roguelite` (무작위 요소가 있다면)
- `Colony Sim` (인구/거주민 관리가 있다면)
- `4X` (Explore/Expand/Exploit/Exterminate 요소가 있다면)
- `Pixel Graphics` (아트 스타일에 따라)
- `Difficult` (높은 난이도 게임이라면)
- `Singleplayer` (멀티플레이가 없다면)

### 7.3 레퍼런스 게임 데이터 요약

| 게임 | 출시년 | 리뷰 수 | 추정 판매량 | 가격 | 주요 태그 |
|------|--------|---------|-------------|------|-----------|
| **They Are Billions** | 2019 | ~50,000 | 900,000+ | $29.99 | Base Building, Tower Defense, Survival, RTS |
| **Frostpunk** | 2018 | ~132,000 | 3,000,000+ | $29.99 | City Builder, Strategy, Survival, Management |
| **Thronefall** | 2023 | ~22,000 | ~315,000 | $7~14 | Tower Defense, Strategy, Base Building, Minimalist |
| **Reigns** | 2016 | ~6,000 | 500,000+ | $2.99 | Card Game, Strategy, Casual |
| **Civilization V** | 2010 | ~185,000 | 8,000,000+ | $29.99 | Turn-Based Strategy, 4X, Historical |

### 7.4 장르 조합 성공 사례

**"혼합 장르 하이브리드" 성공 패턴**:
- **Mindustry**: 기지건설 + 타워 디펜스 → 96% 긍정, 성공 사례 (무료 → $6.99)
- **Cataclismo** (2024): RTS + 타워 디펜스 + 도시건설 → 신규 진입, 긍정적 초반 반응
- **Becastled**: 도시건설 + 타워 디펜스 → 90%+ 긍정
- **Thronefall**: 타워 디펜스 + 미니멀 기지건설 → **300K 판매 성공 케이스**

**성공 공통점**:
1. 명확한 "야간 방어 + 주간 건설" 루프
2. 한 판에 1~2시간 완결 가능한 세션
3. 점진적 난이도 상승 + 의미 있는 선택지
4. "이번 판은 다르게 해봐야지" 하는 리플레이 욕구

### 7.5 "Thronefall 성공 공식" 분석

GameDiscoverCo 딥다이브에서 추출한 핵심 인사이트:

**성공 요인**:
1. **Jonas Tyroller의 177K 구독자 활용**: 개인 브랜드가 초기 마케팅 대체
2. **Steam Next Fest 데모**: 출시 전 검증 + 위시리스트 확보
3. **$7 접근성 가격**: 충동구매 유도, 리뷰 수 빠른 증가
4. **"단순하지만 깊은"**: 복잡한 UI/메뉴 없이 직관적인 플레이
5. **중국 시장 의외 성공**: 무의도적 Simplified Chinese 공략

**우리 게임 적용점**:
- Steam Next Fest 데모 참여는 필수 전략
- $20~$30 가격대 검토 (복잡도 높으면 더 높은 가격 정당화 가능)
- YouTube 개발 일지 콘텐츠 제작으로 커뮤니티 선행 구축

---

## 8. 위시리스트 및 수요 검증

### 8.1 위시리스트의 의미

위시리스트는 구매 의향 신호이자 Steam 알고리즘의 핵심 지표:
- Steam Next Fest 이전 **2,000+ 위시리스트** 필요 (유의미한 노출을 위해)
- 출시 시 권장 위시리스트: **7,000+** (기본 성공 임계점)
- 홈페이지 피처드 기준: **50,000+** 위시리스트

### 8.2 위시리스트 → 판매 전환율

최신 데이터 (2024-2025, GameDiscoverCo):
- **전체 중간값**: 위시리스트의 약 15% → 출시 첫 주 판매
- **$10 이상 게임**: 위시리스트의 약 10% → 첫 주 판매
- **첫 달 누적**: 위시리스트의 약 27%

**계산 예시**:
```
목표: 첫 달 10,000판매
필요 위시리스트: 10,000 / 0.27 = ~37,000
```

### 8.3 Steam Next Fest 위시리스트 벤치마크

| 성과 등급 | Steam Next Fest 기간 위시리스트 획득 |
|-----------|-------------------------------------|
| Bronze | 6,000 미만 |
| Silver | 6,000~20,000 |
| Gold | 20,000~50,000 |
| Platinum | 50,000~150,000 |
| Diamond | 150,000+ |

---

## 9. 실전 리서치 워크플로우 (소규모 팀용)

### 9.1 아이디어 단계 (개발 전) — 1~2주

```
Step 1: 경쟁 게임 목록 작성 (10~15개)
  → SteamDB 태그 검색으로 찾기
  → Steam 스토어 "More Like This" 활용

Step 2: 각 게임 기본 데이터 수집 (Gamalytic + SteamDB)
  → 리뷰 수, CCU, 추정 판매량, 가격

Step 3: 상위 3개 게임 리뷰 심층 분석
  → 부정 리뷰 100개 읽기
  → 반복 키워드 5~10개 추출

Step 4: 시장 갭 정의
  → "경쟁 게임들이 공통으로 못 하는 것은?"
  → 우리 게임의 차별점 1문장으로 정리

Step 5: 플레이어 페르소나 2개 작성
  → 주요 타겟 + 2차 타겟
```

### 9.2 기획 단계 (GDD 작성 중) — 지속

```
Step 6: 태그 전략 확정
  → 사용할 Steam 태그 5~10개 선정
  → 각 태그의 경쟁 강도 확인

Step 7: 가격 전략 결정
  → 유사 게임 가격대 분포 분석
  → 개발 볼륨 대비 적정 가격 설정

Step 8: 유사 게임 위시리스트 트렌드 추적 (SteamDB Followers)
  → 출시 전후 모멘텀 패턴 분석
```

### 9.3 개발 중 — 분기별

```
Step 9: 경쟁 신작 모니터링
  → Steam 출시 알림 설정 (관련 태그)
  → 새 경쟁 게임 리뷰 추이 추적

Step 10: 커뮤니티 반응 측정
  → Reddit/Discord에서 우리 게임 주제 언급 모니터링
  → Next Fest 데모 후 피드백 분석
```

---

## 10. 무료 리서치 도구 최종 정리

| 우선순위 | 도구 | 용도 | URL |
|----------|------|------|-----|
| ★★★ | **SteamDB** | 경쟁 게임 CCU/태그/업데이트 분석 | steamdb.info |
| ★★★ | **Gamalytic** | 매출 추정 + 장르별 수익성 | gamalytic.com |
| ★★★ | **GameDiscoverCo** | Steam 시장 심층 분석 뉴스레터 | newsletter.gamediscover.co |
| ★★★ | **How To Market A Game** | 위시리스트/마케팅 벤치마크 | howtomarketagame.com |
| ★★☆ | **Steam Charts** | CCU 트렌드 시각화 | steamcharts.com |
| ★★☆ | **SteamSpy** | 장르별 소유자 수 추정 | steamspy.com |
| ★★☆ | **Steam 250** | 태그별 최고 평점 게임 | steam250.com |
| ★★☆ | **Steambase** | 직관적 스탯 대시보드 | steambase.io |
| ★☆☆ | **Google Trends** | 장르 검색 관심도 추이 | trends.google.com |
| ★☆☆ | **Quantic Foundry** | 플레이어 동기 모델 | quanticfoundry.com |
| ★☆☆ | **TwitchTracker** | 스트리밍 노출 분석 | twitchtracker.com |
| 유료 | **VGInsights** | 정밀 수익 분석 (~$20/월) | app.sensortower.com/vgi |

---

## 11. 미해결 질문 및 추가 조사 필요 항목

- [ ] 중국 시장 (Steam) 내 한국 인디 게임 수익화 가능성
- [ ] 턴제 기지경영 장르의 정확한 TAM 계산 (스팀스파이 데이터로)
- [ ] Frostpunk 2와의 직접 경쟁 리스크 분석 (규모 차이)
- [ ] Early Access 전략 vs 완성 출시 ROI 비교 (장르 특성상)
- [ ] 한국어/일본어 로컬라이제이션이 수익에 미치는 영향
- [ ] 모바일 포팅 가능성 및 AppMagic 기반 시장 검증

---

## 12. 참고 자료

1. [How to do market research for your indie game - Gamalytic](https://gamalytic.com/blog/dow-to-do-market-research-for-your-indie-game)
2. [10 Free Tools for Video Game Market & Competitor Analysis - IMPRESS](https://impress.games/blog/free-tools-for-video-game-market-competitor-analysis)
3. [A Guide to DIY Market Research for Indie Developers - Game Developer](https://www.gamedeveloper.com/business/a-guide-to-diy-market-research-for-indie-developers)
4. [The shape of the PC new game market in 2024 - GameDiscoverCo](https://newsletter.gamediscover.co/p/the-shape-of-the-pc-new-game-market)
5. [Deep dive: how Thronefall went 'minimal' to hit 300k sales - GameDiscoverCo](https://newsletter.gamediscover.co/p/deep-dive-how-thronefall-went-minimal)
6. [The state of Steam wishlist 'conversions': 2024-2025 - GameDiscoverCo](https://newsletter.gamediscover.co/p/the-state-of-steam-wishlist-conversions)
7. [Benchmarks for selling a game on Steam - How To Market A Game](https://howtomarketagame.com/2022/09/25/benchmarks-for-selling-a-game-on-steam/)
8. [The 2024 Indie Game Landscape - Medium/Shahriar Shahrabi](https://shahriyarshahrabi.medium.com/the-2024-indie-game-landscape-why-luck-plays-a-major-role-in-success-on-steam-c6cbc1868c35)
9. [Using Steam reviews to estimate sales - Game Developer](https://www.gamedeveloper.com/business/using-steam-reviews-to-estimate-sales)
10. [VGI Global Indie Games Market Report 2024](https://app.sensortower.com/vgi/assets/reports/VGI_Global_Indie_Games_Market_Report_2024.pdf)
11. [Gamer Motivation Model - Quantic Foundry](https://quanticfoundry.com/gamer-motivation-model/)
12. [Newzoo Global Games Market Report 2025](https://gamedevreports.substack.com/p/newzoo-the-games-market-in-2025)
13. [Turn-Based Steam Charts Top Revenue - September 2024](https://turnbasedlovers.com/lists/turn-based-steam-charts-top-revenue-games-september-2024/)
14. [Top Base Building Games - Steam 250](https://steam250.com/tag/base_building)
15. [They Are Billions Statistics - LEVVVEL](https://levvvel.com/they-are-billions-statistics/)
16. [3 Ways to Conduct Competitive Analysis for Indie Devs - Marketer Interview](https://marketerinterview.com/3-ways-to-conduct-competitive-analysis-for-indie-game-developers-on-steam/)
17. [Steam Next Fest Wishlist Benchmarks 2025 - How To Market A Game](https://howtomarketagame.com/2025/03/26/benchmarks-how-many-wishlists-can-i-get-from-steam-next-fest/)
18. [How to estimate Steam game revenue - Game Oracle](https://www.game-oracle.com/blog/estimated-revenue)
19. [Indie Game Development and Market Research Importance - Negative Five VC](https://negativefive.vc/game-marketing/indie-game-development-and-the-importance-of-video-game-market-research/)
20. [GDC 2025 State of the Game Industry](https://reg.gdconf.com/state-of-game-industry-2025)
