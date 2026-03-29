# Thronefall (Grizzly Games) — 기술 스택 & 개발 방식 분석

- **작성일**: 2026-03-29
- **카테고리**: practice
- **상태**: 조사완료 (공개 정보 한계)

---

## 1. 요약

Thronefall은 Jonas Tyroller + Paul Schnepf 2인팀(Grizzly Games)이 Unity + C#으로 제작한 미니멀 전략 게임.
고급 아키텍처 프레임워크(DOTS, ECS, DI, Reactive) 없이 표준 MonoBehaviour 기반으로 약 180 작업일 만에 얼리 액세스 출시, 1년 만에 100만 장 판매 달성.
핵심 교훈: 인디 게임에서 복잡한 기술 스택은 불필요하며 "배송 가능한 단순함"이 더 중요하다.

---

## 2. 확인된 기술 스택

| 항목 | 내용 | 출처 |
|------|------|------|
| 게임 엔진 | Unity (버전 미공개) | Pragmatic Engineer 인터뷰 |
| 언어 | C# | 인터뷰 복수 |
| 3D 모델링 | Blender | Pragmatic Engineer 인터뷰 |
| 경로 탐색 | A* Pathfinding Project (Asset Store 유료) | Pragmatic Engineer 인터뷰 |
| IDE | Visual Studio 또는 유사 | 추정 |
| 렌더링 파이프라인 | 미공개 (URP 추정 — 아웃라인 렌더링 활용) | 게임 시각적 특징 분석 |
| UI 시스템 | 미공개 (UGUI 추정 — Unity 표준) | 미확인 |
| 테스트 | 없음 | Pragmatic Engineer 인터뷰 |
| 코드 리뷰 | 없음 | Pragmatic Engineer 인터뷰 |
| 버전 관리 | Git (추정, main 직접 push) | Pragmatic Engineer 인터뷰 |
| AI 보조 | ChatGPT (스켈레톤 코드, 셰이더 번역) | Pragmatic Engineer 인터뷰 |
| 태스크 관리 | Miro (디지털 화이트보드) | 인디게임 인터뷰 |
| 소통 | Discord (일일 통화) | 인디게임 인터뷰 |

---

## 3. 아키텍처 특징

### 3.1 씬 = 레벨

- 씬이 프로젝트 구조의 뼈대 — Thronefall에서 씬은 곧 레벨
- 씬 내 GameObject에 MonoBehaviour 스크립트 부착하는 표준 Unity 패턴

### 3.2 MonoBehaviour 중심

- DI 프레임워크(VContainer, Zenject) 미사용
- DOTS/ECS 미사용
- Reactive 프레임워크(UniRx, R3) 미사용 (추정)
- "스파게티 코드"도 출시에 지장 없다는 실용주의 철학

### 3.3 A* 경로 탐색 커스터마이징

- Asset Store의 A* Pathfinding Project 구매 후 대폭 커스터마이징
- 유닛이 노드 위에 정확히 이동하지 않으므로 후처리(post-processing) 적용
- 개발 과정에서 가장 기술적으로 까다로운 부분으로 언급

### 3.4 게임플레이 프로토타이핑 방법론

- 모든 레벨 1-2일 내 프로토타입 완성 목표
- 아트 없이 단순 도형으로 게임플레이 프로토 먼저
- 이후 로직 없는 아트 프로토 별도
- 2년 프로젝트 기준 2개월 프로토타이핑 권장

---

## 4. 팀 구성 및 개발 문화

| 항목 | 내용 |
|------|------|
| 팀 규모 | 2인 (Jonas Tyroller — 기획/프로그래밍, Paul Schnepf — 기획/아트) |
| 작업 방식 | 100% 원격 (베를린 ↔ 바이에른) |
| 소통 | Discord 일일 통화 |
| 커뮤니티 관리 | Sacha Torikian (Discord/Steam 포럼) |
| 개발 기간 | 180 작업일 → 얼리 액세스, 이후 1년 이상 콘텐츠 추가 |
| 단위 테스트 | 없음 ("인디 게임은 한 번 배송하면 끝, 불필요") |
| 코드 리뷰 | 없음 |
| 분기 전략 | main 직접 push (2인팀에서 충분히 작동) |

---

## 5. 디자인 철학 — "빼기의 예술"

Jonas Tyroller가 강조한 핵심 원칙:

> "If you feel almost ashamed about how tiny your game is, you're scoping correctly."

- 전략 게임에서 불필요한 좌절 요소 제거
- 복잡성을 한꺼번에 투척하지 않고 **기존 시스템에 점진적으로 레이어링**
- 건물이 매일 무료 재건되는 설계 — 여러 디자인 목표를 동시에 달성하는 최소 솔루션

---

## 6. 비즈니스 성과

| 지표 | 수치 |
|------|------|
| 얼리 액세스 출시 | 2023년 8월 2일 |
| 정식 출시 | 2024년 10월 11일 |
| 판매량 (1년차) | 약 100만 장 |
| 가격 | $12.99 |
| 플랫폼 | Steam (SteamDeck 최적화) |
| 마케팅 | Jonas Tyroller YouTube 채널 (개발 여정 공유) |

---

## 7. 우리 프로젝트에 대한 함의

### 7.1 기술 스택 복잡도에 대한 현실 점검

Thronefall의 성공은 **고급 아키텍처 없이도 최고 수준의 게임을 만들 수 있음**을 증명한다.
단, 이는 2인 소규모 팀, 장르 특성(씬=레벨), 단일 배송 상품에 적합한 선택이다.

### 7.2 우리 스택(VContainer+R3+UniTask)의 정당성

현재 UI_Study의 VContainer+R3+UniTask 스택은 **학습 목적**과 **확장성**에 맞게 설계되었다.
Thronefall식 접근은 빠른 프로토타이핑에는 유효하지만:

- 장기 유지보수성 낮음
- 팀 확장 시 협업 어려움
- 복잡한 상태 관리 시 어려움 (기지 경영 게임은 Thronefall보다 복잡한 상태 필요)

### 7.3 채택 가능한 요소

| Thronefall 교훈 | 우리 프로젝트 적용 방안 |
|----------------|----------------------|
| 프로토타입 우선 | 새 시스템은 단순 MonoBehaviour로 먼저 작동 확인 후 아키텍처 정리 |
| 과도한 엔지니어링 지양 | VContainer 스코프를 복잡하게 중첩하지 말 것 |
| A* 같은 검증된 솔루션 활용 | 커스텀 구현 전 Asset Store 검토 |
| 단순 코드도 배송 가능 | 완벽한 아키텍처보다 동작하는 게임 우선 |

---

## 8. 공개 정보 한계

Thronefall의 기술 스택에 대한 공개 정보는 제한적이다:
- **확인 불가**: Unity 버전, 렌더링 파이프라인, UI 시스템, 구체적 패키지 목록
- **GDC 발표**: 검색 결과에서 GDC/CEDEC 발표 없음 확인
- **코드 공개**: 없음
- **정보 출처 한계**: Pragmatic Engineer 팟캐스트, Game Developer 인터뷰, indiegames.wtf 인터뷰가 주요 소스

더 깊은 기술 분석을 원한다면:
1. Jonas Tyroller YouTube 채널에서 개발 유튜브 영상 직접 분석
2. Portside Game Assembly에서 Jonas Tyroller 발표 자료 확인
3. Steam 토론 게시판의 기술 질문 스레드 탐색

---

## 9. 참고 자료

1. [Building a best-selling game with a tiny team — Pragmatic Engineer (Jonas Tyroller)](https://newsletter.pragmaticengineer.com/p/thronefall)
2. [Mastering minimalism and layering complexity with Thronefall — Game Developer](https://www.gamedeveloper.com/design/mastering-minimalism-and-layering-complexity-with-strategy-game-thronefall)
3. [Thronefall interview — indiegames.wtf](https://indiegames.wtf/interviews/thronefall/)
4. [Thronefall Wikipedia](https://en.wikipedia.org/wiki/Thronefall)
5. [Jonas Tyroller Twitter/X](https://x.com/JonasTyroller)
6. [openindie Podcast — Thronefall / Grizzly Games / Paul Schnepf](https://creators.spotify.com/pod/profile/openindie/episodes/30---Thronefall---Grizzly-Games---Paul-Schnepf---Germany-e2dc1g5)
7. [Deep dive: how Thronefall went minimal to hit 300k sales](https://newsletter.gamediscover.co/p/deep-dive-how-thronefall-went-minimal)
8. [Portside Game Assembly — Jonas Tyroller Speaker Profile](https://portsideassembly.com/speakers/jonas-tyroller/)

## 10. 미해결 질문

- [ ] Thronefall의 Unity 버전 및 렌더링 파이프라인 확인 (URP/HDRP/Built-in)
- [ ] Thronefall UI 시스템 (UGUI vs UI Toolkit) 확인
- [ ] ScriptableObject 활용 여부 및 규모 확인
- [ ] Jonas Tyroller YouTube 개발 영상에서 코드 스니펫 분석 가능 여부
