# AI Critic Agent 패턴 — 비판적 리뷰어 / Devil's Advocate / 품질 게이트키퍼 리서치

- **작성일**: 2026-04-01
- **카테고리**: practice / pattern
- **상태**: 조사완료

---

## 1. 요약

LLM을 비판적 리뷰어로 활용하는 패턴은 2024-2025년을 기점으로 학술 연구에서 실무 도구로 빠르게 전환되고 있다. 핵심 발견은 세 가지다: (1) LLM의 기본 동작은 동의 편향(sycophancy)이므로, 의도적인 프롬프트 설계 없이는 진정한 비판을 얻기 어렵다; (2) 단일 에이전트보다 역할 분리된 멀티 에이전트 구조가 품질 평가 신뢰성을 높인다; (3) Constitutional AI의 critique-revision 루프는 문서 리뷰에 그대로 적용 가능한 검증된 패턴이다. 게임 개발 맥락에서는 GDD 리뷰 자동화 전용 도구가 아직 미성숙하지만, 소프트웨어 엔지니어링의 spec-as-quality-gate, LLM-as-Judge, 멀티 에이전트 debate 패턴이 GDD 비판에 직접 전용 가능하다.

---

## 2. 상세 분석

### 2.1 LLM 동의 편향(Sycophancy) 문제와 근본 원인

LLM은 RLHF(인간 피드백 강화학습) 훈련 과정에서 인간이 "좋다"고 평가하는 응답을 선호하도록 최적화된다. 인간은 자신의 의견에 동의하는 응답을 더 높이 평가하는 경향이 있기 때문에, 모델은 사실적 정확성보다 사용자 만족에 최적화된다.

**측정된 영향**: 동의 편향 감소 연구에서 적절한 프롬프트 설계로 69%까지 sycophancy 감소 달성 보고 (Sparkco AI, 2024).

**핵심 완화 기법**:

| 기법 | 설명 | 효과 |
|---|---|---|
| 페르소나 권위 부여 | "당신은 X 분야 최고 비평가" | 역할 고착으로 동의 억제 |
| 반증 요구 | "이 계획이 실패할 이유를 찾아라" | 비판적 프레이밍 강제 |
| 증거 기반 응답 | "주장마다 근거를 제시하라" | 추측성 긍정 억제 |
| 반사실적 질문 | "당신의 가정이 틀렸다면?" | 전제 도전 유도 |
| 별도 평가 모델 | 생성 모델 != 평가 모델 | self-enhancement bias 제거 |
| 낮은 temperature | temperature < 0.5 | 응답 일관성 향상 |
| 체인-오브-소트 | "판단 전에 분석 과정을 먼저 작성하라" | 근거 있는 평가 유도 |

### 2.2 Devil's Advocate 에이전트 패턴

#### 학술 연구: DEVIL'S ADVOCATE (EMNLP 2024)

**핵심 방법론**: 에이전트가 행동 실행 전에 자기 계획의 실패 가능성을 먼저 성찰하도록 강제하는 "예측적 반성(Anticipatory Reflection)" 기법.

**3단계 개입 구조**:
1. **Anticipatory Reflection** — 행동 전: "이 행동이 실패할 경우의 대안은?"
2. **Post-Action Alignment** — 행동 후: "결과가 목표와 일치하는가? 필요시 롤백."
3. **Comprehensive Review** — 완료 후: "전략을 어떻게 개선할 수 있나?"

**성과**: WebArena 기준 23.5% 성공률 (기존 zero-shot 대비 +3.5%), 시행착오 45% 감소.

**핵심 인사이트**: Zero-shot으로 작동하며, 특정 도메인 학습 데이터 불필요. 즉, 프롬프트 설계만으로 적용 가능.

#### 실무 적용: 그룹 의사결정의 Devil's Advocate (IUI 2024)

GPT-3.5-turbo 기반 Devil's Advocate 에이전트 구현 연구 결과:
- 에이전트가 비판 질문과 코멘트를 생성하도록 지시받을 때, "AI 권고에 대한 적절한 신뢰 개발"에 도움을 준다고 인식됨
- 비판 모드 활성화 방법: 에이전트에게 "반대 의견을 제시하는 보조자 역할"로 명시적 지시

#### Don't Just Translate, Agitate (2025): XAI의 Devil's Advocate

AI 설명(Explainable AI)의 강점과 약점에 도전하는 LLM 기반 devil's advocate 실험:
- 목적: AI 설명을 검증하는 것이 아니라, 사용자가 AI를 맹목적으로 신뢰하지 않도록 도발
- 결론: LLM devil's advocate는 유용한 협력자로 인식되며, AI에 대한 비판적 사고를 촉진

### 2.3 멀티 에이전트 Debate / Review 시스템

#### Multi-Agent Debate (MAD) 구조

```
[주제/문서] → [Agent A: 지지 논거] ──┐
                                      ├→ [심판 에이전트] → [합의/판정]
             [Agent B: 반대 논거] ──┘
```

**ChatEval (2024)**: 다수의 에이전트가 자율적으로 텍스트 품질을 논의하고 평가. 인간 평가와의 상관관계에서 단일 에이전트보다 우수.

**A-HMAD (2025)**: 적응형 이종 멀티 에이전트 Debate. Devil's Advocate 역할 에이전트가 상대방 논거를 열거하고 공격. 교육적 추론 품질 향상 입증.

**MAD 한계 (MLRC 2024)**: 단순 self-consistency나 앙상블보다 항상 우월하지는 않음. 하이퍼파라미터 튜닝이 중요. 요건:
- 에이전트 간 역할 명확 분리
- 심판 에이전트(Judge)의 평가 기준 명시
- 합의 실패 시 처리 규칙 정의

#### 9-에이전트 병렬 코드 리뷰 (실무 사례, 2026)

Claude Code를 활용한 9개 병렬 서브에이전트 코드 리뷰 시스템:

| 에이전트 | 역할 |
|---|---|
| Test Runner | 관련 테스트 실행, 실패 상세 보고 |
| Linter & Static Analysis | 타입 오류, 미해결 참조 |
| Code Reviewer | 영향/노력 기준 상위 5개 개선점 |
| Security Reviewer | 인젝션, 인증, 시크릿 노출 |
| Quality & Style Reviewer | 복잡도, 중복, 컨벤션 |
| Test Quality Reviewer | 커버리지 ROI, 동작 vs 구현 테스트 |
| Performance Reviewer | N+1 쿼리, 블로킹, 메모리 누수 |
| Dependency & Deployment Safety | 의존성, 마이그레이션, 관측가능성 |
| Simplification & Maintainability | "더 단순할 수 있는가?" 검토 |

**출력**: Critical > High > Medium > Low 분류 → 3가지 판정(병합 가능 / 주의 필요 / 수정 필요).

### 2.4 Constitutional AI의 Critique-Revision 루프

Anthropic의 Constitutional AI(2022)에서 문서 리뷰에 직접 적용 가능한 패턴:

**기본 구조**:
```
[원본 문서/계획]
    ↓
[Critique Prompt] → "이 계획의 문제점을 찾아라: [평가 원칙]"
    ↓
[Critique 생성]
    ↓
[Revision Prompt] → "비판을 반영하여 계획을 개선하라"
    ↓
[개선된 문서]
    ↓ (반복 가능)
```

**실제 CAI 예시 프롬프트 패턴**:
```
Critic: [문서]의 [특정 측면]을 읽고, 
[평가 원칙]에 비추어 가장 강한 반론을 작성하라.

Revision: 위 비판을 반영하여 [문서]의 [해당 섹션]을 개선하라.
단, 비판이 타당한 경우에만 수정하고, 근거 없는 비판은 거부하라.
```

**핵심 원칙**: 비판과 수정을 동일한 파이프라인에서 반복 적용 가능. 매 반복마다 다른 평가 원칙 적용.

### 2.5 LLM-as-Judge 평가 루브릭 시스템

문서/계획 품질을 수치화하는 방법:

**평가 프롬프트 기본 구조**:
```
[평가 대상 텍스트]를 다음 기준으로 평가하라:

기준: [평가 축]
- 5점: [명확한 5점 기준 서술]
- 3점: [명확한 3점 기준 서술]
- 1점: [명확한 1점 기준 서술]

평가 과정을 먼저 설명하고, 마지막에 JSON으로 점수를 반환하라:
{"score": N, "reasoning": "...", "critical_issues": [...]}
```

**신뢰도 향상 기법**:
- Binary/3-tier 척도 선호 (5점 척도보다 일관성 높음)
- 평가 축 하나당 독립적인 Judge 사용
- 여러 독립 평가 후 다수결
- Few-shot 예시로 Judge 캘리브레이션
- Temperature < 0.5로 설정

**LLM-RUBRIC (ACL 2024)**: 다차원 캘리브레이션된 루브릭 평가 프레임워크. 각 평가 축의 점수 의미를 명확히 정의하면 Judge 신뢰도 크게 향상.

### 2.6 Steelmanning: 반대 논거 최강화

**Steelmanning**은 상대방의 논거를 가장 강한 형태로 재구성하는 기법:

```
입력: [계획 또는 설계]
지시: 이 계획에 반대하는 사람이 제시할 수 있는 
     가장 강력하고 설득력 있는 논거를 3개 생성하라.
     각 논거는 실제 데이터나 논리에 기반해야 하며,
     추측은 추측으로 표시하라.
     (허위 사실 생성 금지)
```

**경고 — Evidence Laundering**: Steelmanning 시 LLM이 그럴듯한 허위 사실을 생성할 위험. 반드시 "검증되지 않은 주장은 추정으로 표시하라"는 지시 포함 필요.

**Steelman.cloud 플랫폼**: 3라운드 adversarial persona 도전, 최강 반론 생성 → Decision Record 출력. 실제 구현 사례.

### 2.7 Pre-Mortem 분석 패턴

**기본 전제**: "이 프로젝트/계획이 6개월 후 실패했다. 왜 실패했는가?"

**AI Pre-Mortem 프롬프트 구조 (6단계)**:
```
역할: 시니어 프로덕트 매니저로서 Pre-Mortem 분석을 진행한다.

1. [소개] 이 분석의 목적을 팀에게 설명하라.

2. [실패 시뮬레이션] 이 프로젝트가 실패했다고 가정하라.
   실패의 주요 원인을 최소 5가지 나열하라.

3. [리스크 심문] 다음 영역별 질문을 던져라:
   - 기술적 리스크
   - 사용자 수용성
   - 경쟁 환경
   - 팀/리소스
   - 타이밍

4. [시나리오 계획] 최악의 시나리오와 현실적 경로를 탐색하라.

5. [완화책] 식별된 각 리스크에 대한 실행 가능한 대책을 제안하라.

6. [균형] 분석이 편향되지 않도록 유지하라.
```

**효과**: 사전 소급(prospective hindsight)으로 미래 실패 원인 식별 능력 30% 향상 (Brookings 연구).

**Xebia 자동화 Pre-Mortem**: AI가 리스크 시나리오를 자동 생성하고 완화책을 제안하는 시스템 구현 사례.

### 2.8 게임 디자인 문서(GDD) 비판 프레임워크

GDD 전용 critique AI 도구는 아직 미성숙하나, 다음 프레임워크가 유효하다.

#### GDD 품질 평가 축 (게임 개발 커뮤니티 기반)

```
1. 명확성 (Clarity)
   - 타겟 오디언스가 명확한가?
   - 핵심 재미 (fun factor)가 한 문장으로 설명 가능한가?
   - 모호한 표현 없이 구체적인가?

2. 완전성 (Completeness)  
   - 코어 루프가 명시되어 있는가?
   - 실패 조건이 정의되어 있는가?
   - 경제/밸런스 시스템이 설명되어 있는가?

3. 일관성 (Consistency)
   - 디자인 필러와 메카닉이 충돌하지 않는가?
   - 초기 비전과 현재 문서가 일치하는가?

4. 실현 가능성 (Feasibility)
   - 팀 규모/기간 대비 스코프가 현실적인가?
   - 스코프 크립 징후가 있는가?

5. 차별성 (Differentiation)
   - 이 게임만의 특별한 점은 무엇인가?
   - 기존 게임과 어떻게 다른가?
```

#### WINQ 프레임워크 (Stanford 디자인 스쿨 기반)

게임 프로토타입 피드백을 위한 구조화된 방법:

| 카테고리 | 질문 |
|---|---|
| **W**hat Works | 효과적인 부분과 그 이유 |
| **I**mprovement | 개선이 필요한 부분과 예시 |
| **N**ew Ideas | 테마/메카닉/구조에 맞는 새로운 아이디어 |
| **Q**uestions | 불명확하거나 추가 설명이 필요한 부분 |

#### 디자인 필러(Design Pillar) 기반 검증 체크리스트

게임의 핵심 경험(3-5개 필러)에 각 기능/시스템을 대조:

```
검증 질문:
1. 이 기능은 [필러 1]을 강화하는가? 아니면 약화하는가?
2. 이 기능은 [필러 2]와 모순되지 않는가?
3. 필러에 기여하지 않는 기능은 제거할 근거가 있는가?
```

Bungie, Naughty Dog 등 스튜디오가 내부 리뷰에서 사용하는 방식.

### 2.9 Claude Code 서브에이전트 품질 게이트 구현 패턴

#### 에이전트 파일 구조 (`.claude/agents/`)

```
.claude/
  agents/
    design-critic.md      # GDD/기획서 비판 에이전트
    hr-manager.md         # 에이전트 성과 평가 에이전트
    plan-reviewer.md      # 계획 검토 에이전트
    spec-validator.md     # 스펙 검증 에이전트
```

#### 각 에이전트 파일 기본 구조

```markdown
---
name: design-critic
description: GDD 및 시스템 기획서를 비판적으로 리뷰하는 에이전트
tools: Read, Glob, Grep
---

당신은 [역할 정의]...

[단일 책임 목표]
[입력 명세]
[출력 명세]
[품질 게이트 기준]
[핸드오프 규칙]
```

#### 3단계 품질 게이트 파이프라인

```
[기획서 작성] → QG-1(스펙 검토) → [아키텍처 설계] → QG-2(ADR 검증) → [구현] → QG-3(코드 리뷰)
                     ↑ 실패 시 반환                        ↑ 실패 시 반환         ↑ 실패 시 반환
```

**도구 권한 원칙** (PubNub 베스트 프랙티스):
- 기획/비판 에이전트: `Read, Glob, Grep` (읽기 전용)
- 아키텍트 에이전트: `Read, Glob, Grep, Bash(검증 명령어만)`
- 구현 에이전트: `Read, Write, Edit, Bash`
- 도구를 명시하지 않으면 모든 도구 상속됨 → 명시적 화이트리스트 필수

#### 멀티 모델 리뷰 패턴 (A/B)

**Pattern A — MCP 브릿지**: 외부 LLM(GPT-5 등)을 MCP 서버로 연결해 cross-model 평가.

```
서브에이전트 → MCP 툴 호출 → 외부 LLM → 판정(pass/warn/block) → 큐 상태 업데이트
```

**Pattern B — Hook Script**: 커밋/스테이지 이벤트에서 스크립트가 외부 LLM API 호출.

#### 스펙-게이트 아키텍처 (Specification as Quality Gate, 2025)

핵심 인사이트: "생성 에이전트와 리뷰 에이전트가 같은 훈련 데이터를 공유하면 상관 에러가 발생한다 — 두 에이전트 모두 같은 맹점을 가진다."

해결책:
1. **실행 가능한 스펙 우선** (BDD 시나리오, 체크리스트)
2. **결정론적 검증 파이프라인** (스펙 → 자동 테스트)
3. **AI 리뷰는 잔여 결함에만** (구조적/아키텍처 관심사)

결함 분류:
- 유형 A: 스펙 가능하지만 미작성 → AI 리뷰 대상
- 유형 B: 경제적으로 스펙 작성 불가 → AI 리뷰 대상
- 유형 C: 실행 전 스펙 불가 → 런타임 발견
- 유형 D: 구조/아키텍처 → AI 리뷰 핵심 대상
- 유형 E: 스펙 자체의 결함 → AI 리뷰로 발견 가능

---

## 3. 베스트 프랙티스

### DO (권장)

- [ ] 비판 에이전트에게 명시적 역할 권위를 부여하라 ("최고 게임 디자인 비평가")
- [ ] 비판 → 수정 루프를 파이프라인으로 구성하라 (Constitutional AI 패턴)
- [ ] 평가 축을 하나당 독립된 에이전트/프롬프트로 분리하라
- [ ] "이 계획이 실패한다면 왜인가?"를 항상 포함하라 (Pre-Mortem)
- [ ] 비판 에이전트는 읽기 전용 도구만 부여하라
- [ ] 평가 루브릭에 각 점수의 구체적 의미를 기술하라
- [ ] 생성 모델과 평가 모델을 분리하라 (correlated error 방지)
- [ ] 체인-오브-소트로 판단 근거를 먼저 작성하게 하라

### DON'T (금지)

- [ ] 열린 질문("이 계획 어때?")으로 비판을 요청하지 말라 → 동의 편향 유발
- [ ] 비판 에이전트에게 Write/Edit 권한을 부여하지 말라
- [ ] Steelmanning 시 사실 검증 없이 그대로 신뢰하지 말라 (Evidence Laundering)
- [ ] 평가 기준 없이 "좋은지 나쁜지 말해줘"라고 묻지 말라
- [ ] 동일 모델이 생성과 평가를 모두 담당하게 하지 말라

### CONSIDER (상황별)

- [ ] 멀티 에이전트 debate는 중요 결정에만 적용 (비용 대비 효과 검토)
- [ ] GDD 리뷰 시 WINQ 프레임워크로 피드백 구조화
- [ ] 5점 척도보다 Binary/3-tier 척도가 신뢰성 높음
- [ ] 인간 리뷰어를 완전히 대체하지 말고, AI를 "사전 필터"로 활용

---

## 4. 호환성

| 항목 | 버전/상태 | 비고 |
|---|---|---|
| Claude Code 서브에이전트 | 2025년 공식 지원 | `.claude/agents/` 디렉토리 |
| Constitutional AI 패턴 | Anthropic 2022, 실무 적용 2024+ | 프롬프트 레벨 구현 가능 |
| MAD (Multi-Agent Debate) | 연구 단계, 실무 도입 시작 | 비용 대비 효과 검토 필요 |
| LLM-as-Judge | 2024년 주류화 | G-Eval, Prometheus 등 |
| Devil's Advocate Agent | EMNLP 2024 발표 | Zero-shot, 프롬프트만으로 구현 |
| Pre-Mortem AI | 2024-2025 실무 도입 | Xebia 자동화 사례 존재 |

---

## 5. 예제 코드

### 5.1 기본 Devil's Advocate 프롬프트 템플릿

```
[시스템 프롬프트]
당신은 [도메인]의 경험 많은 비평가입니다.
당신의 역할은 설계 문서를 검토하고 가장 강력한 반론을 제시하는 것입니다.
동의하거나 칭찬하는 것이 아니라, 잠재적 실패 지점을 발견하는 것이 목표입니다.

[사용자 프롬프트]
다음 [GDD/기획서/계획]을 검토하고:
1. 이것이 실패할 수 있는 3가지 핵심 이유를 나열하라
2. 각 이유에 대해 반증 사례가 있다면 제시하라
3. 가장 치명적인 리스크 1가지를 강조하라
4. 수정을 제안하되, 비판이 타당한 경우에만 하라

[문서 내용]
{document}
```

### 5.2 GDD 비판 에이전트 정의 (Claude Code 서브에이전트)

```markdown
---
name: design-critic
description: |
  게임 디자인 문서(GDD), 시스템 기획서, 피처 계획을 비판적으로 리뷰합니다.
  /ui-research, /game-design 명령 이후 또는 명시적으로 호출 시 활성화됩니다.
  읽기 전용 에이전트 — 문서를 수정하지 않습니다.
tools: Read, Glob, Grep
---

당신은 10년 경력의 시니어 게임 디자이너이자 제작 PD입니다.
수백 개의 GDD를 검토한 경험이 있으며, 스코프 크립과 재미 없는 메카닉을 즉시 식별합니다.

## 리뷰 의무

문서를 받으면 반드시 다음 5개 축으로 평가하라:

### 1. 명확성 (Clarity) [1-5점]
- 한 문장으로 핵심 재미를 설명할 수 있는가?
- 모호한 표현("적절한", "충분한" 등)이 있는가?

### 2. 완전성 (Completeness) [1-5점]
- 코어 루프가 명시되어 있는가?
- 실패 조건과 패배 상태가 정의되어 있는가?
- 경제/밸런스 시스템이 설명되어 있는가?

### 3. 일관성 (Consistency) [1-5점]
- 디자인 필러와 각 메카닉이 충돌하지 않는가?
- 문서 내 상충하는 설명이 있는가?

### 4. 실현 가능성 (Feasibility) [1-5점]
- 팀 규모/타임라인 대비 스코프가 현실적인가?
- 스코프 크립 징후("나중에 추가할 수 있다")가 있는가?

### 5. 차별성 (Differentiation) [1-5점]
- 이 게임만의 특별한 점이 명확한가?
- "X + Y" 조합으로 설명되는 파생 게임처럼 느껴지지 않는가?

## Pre-Mortem 필수 포함

"이 게임이 출시 6개월 후 실패했다면 왜인가?" 시나리오로 리스크 3가지 식별.

## 출력 형식

```json
{
  "overall_score": N,
  "dimension_scores": {
    "clarity": N,
    "completeness": N,
    "consistency": N,
    "feasibility": N,
    "differentiation": N
  },
  "critical_issues": ["치명적 문제 목록"],
  "improvements": ["우선순위 개선 항목"],
  "premortem_risks": ["실패 시나리오 3개"],
  "verdict": "APPROVED | NEEDS_REVISION | REJECTED"
}
```

판정 기준:
- APPROVED: 모든 축 4점 이상, critical_issues 없음
- NEEDS_REVISION: 평균 3점 이상, 수정 가능한 이슈
- REJECTED: 하나 이상의 축 2점 이하, 또는 치명적 구조 문제
```

### 5.3 HR 매니저 에이전트 정의 (에이전트 성과 평가)

```markdown
---
name: hr-manager
description: |
  다른 AI 에이전트의 산출물 품질을 평가하고 성과를 추적합니다.
  에이전트 작업 완료 후 호출되어 품질 게이트 통과 여부를 판정합니다.
tools: Read, Glob, Grep
---

당신은 엄격하지만 공정한 QA 매니저입니다.
에이전트가 제출한 산출물을 검토하고, 기준 충족 여부를 명확히 판정합니다.

## 평가 절차

1. 산출물 유형 파악 (코드, 문서, 기획서, 리서치 보고서)
2. 해당 유형의 완료 기준(Definition of Done) 적용
3. 결함 분류: Critical / Major / Minor
4. 판정 및 피드백 제공

## 판정 출력

```json
{
  "agent_name": "...",
  "task": "...",
  "verdict": "PASS | CONDITIONAL_PASS | FAIL",
  "critical_defects": [],
  "major_defects": [],
  "minor_defects": [],
  "score": N,
  "required_actions": []
}
```
```

### 5.4 Constitutional AI Critique-Revision 루프 (Python 의사코드)

```python
def critique_revision_loop(document: str, principles: list[str], max_iterations: int = 3) -> str:
    """
    Constitutional AI 스타일의 critique-revision 루프.
    principles: 각 반복에서 적용할 평가 원칙 목록
    """
    current_doc = document
    
    for i, principle in enumerate(principles[:max_iterations]):
        # 1단계: 비판 생성
        critique_prompt = f"""
        다음 문서를 검토하라:
        {current_doc}
        
        평가 원칙: {principle}
        
        이 원칙에 비추어 문서의 가장 강한 약점을 분석하라.
        비판은 구체적이고 실행 가능해야 하며, 추측은 추측으로 표시하라.
        """
        critique = llm.generate(critique_prompt)
        
        # 2단계: 비판 반영한 수정
        revision_prompt = f"""
        원본 문서:
        {current_doc}
        
        비판:
        {critique}
        
        비판이 타당한 경우에만 문서를 수정하라.
        근거 없는 비판은 명시적으로 거부하라.
        수정된 문서를 반환하라.
        """
        current_doc = llm.generate(revision_prompt)
    
    return current_doc
```

### 5.5 LLM-as-Judge 루브릭 프롬프트 템플릿

```
다음 게임 기획서를 [평가 축]의 관점에서 평가하라.

평가 대상:
{document}

평가 기준 — [평가 축]:
- 5점: [구체적 5점 기준, 예: "코어 루프가 한 문장으로 설명되고, 플레이어 행동-피드백-보상이 명확히 정의됨"]
- 3점: [구체적 3점 기준, 예: "코어 루프가 있으나 피드백 루프가 불명확함"]
- 1점: [구체적 1점 기준, 예: "코어 루프 자체가 정의되지 않음"]

평가 방법:
1. 먼저 문서에서 관련 근거를 인용하라
2. 각 점수 기준과 비교하라
3. 최종 점수와 핵심 근거를 JSON으로 반환하라

출력:
{"score": N, "evidence": "...", "key_issue": "..."}
```

---

## 6. 우리 프로젝트 적용 계획

이 리서치는 `.claude/agents/` 디렉토리의 두 에이전트 설계에 직접 적용된다.

### design-critic 에이전트 설계 방향

- **역할**: GDD, 시스템 기획서, 피처 계획을 비판하는 비판적 리뷰어
- **트리거**: 기획서 작성 완료 후 품질 게이트로 자동 또는 수동 호출
- **핵심 패턴**: Pre-Mortem + WINQ + LLM-as-Judge 루브릭 결합
- **도구 제한**: Read/Glob/Grep (읽기 전용 — 수정 권한 없음)
- **출력**: 구조화된 JSON 판정 (APPROVED / NEEDS_REVISION / REJECTED)

### hr-manager 에이전트 설계 방향

- **역할**: 다른 에이전트의 산출물 품질을 평가하는 메타 평가자
- **트리거**: 에이전트 작업 완료 후 또는 주기적 품질 감사
- **핵심 패턴**: Definition of Done 체크리스트 + Constitutional AI 원칙 적용
- **평가 차원**: 완전성, 정확성, 지시 준수, 실행 가능성
- **도구 제한**: Read/Glob/Grep (읽기 전용)

### 구현 우선순위

1. design-critic 에이전트 — 기획서 리뷰 즉시 필요
2. GDD 리뷰 루브릭 정의 (5개 평가 축 + 점수 기준)
3. Pre-Mortem 프롬프트 통합
4. hr-manager 에이전트 — 에이전트 생태계 성장 후 도입

---

## 7. 참고 자료

1. [DEVIL'S ADVOCATE: Anticipatory Reflection for LLM Agents (EMNLP 2024)](https://aclanthology.org/2024.findings-emnlp.53/)
2. [Enhancing AI-Assisted Group Decision Making through LLM Devil's Advocate (IUI 2024)](https://mingyin.org/paper/IUI-24/devil.pdf)
3. [Don't Just Translate, Agitate: LLMs as Devil's Advocates for AI Explanations (2025)](https://arxiv.org/html/2504.12424v1)
4. [Improving Factuality and Reasoning through Multiagent Debate (MIT)](https://composable-models.github.io/llm_debate/)
5. [ChatEval: Better LLM-based Evaluators through Multi-Agent Debate](https://openreview.net/forum?id=FQepisCUWu)
6. [Should we be going MAD? MAD Strategies for LLMs (MLRC 2024)](https://proceedings.mlr.press/v235/smit24a.html)
7. [Reducing LLM Sycophancy: 69% Improvement Strategies (Sparkco AI)](https://sparkco.ai/blog/reducing-llm-sycophancy-69-improvement-strategies)
8. [Sycophancy in Large Language Models: Causes and Mitigations](https://arxiv.org/html/2411.15287v1)
9. [Constitutional AI: Harmlessness from AI Feedback (Anthropic)](https://www.anthropic.com/research/constitutional-ai-harmlessness-from-ai-feedback)
10. [The Specification as Quality Gate (2025)](https://arxiv.org/html/2603.25773)
11. [LLM-as-a-Judge Complete Guide (EvidentlyAI)](https://www.evidentlyai.com/llm-guide/llm-as-a-judge)
12. [LLM-RUBRIC: Multidimensional Calibrated Evaluation (ACL 2024)](https://aclanthology.org/2024.acl-long.745.pdf)
13. [9 Parallel AI Agents Code Review (HAMY, 2026)](https://hamy.xyz/blog/2026-02_code-reviews-claude-subagents)
14. [Best Practices for Claude Code Subagents (PubNub)](https://www.pubnub.com/blog/best-practices-for-claude-code-sub-agents/)
15. [awesome-claude-code-subagents: code-reviewer agent](https://github.com/VoltAgent/awesome-claude-code-subagents/blob/main/categories/04-quality-security/code-reviewer.md)
16. [Steelman: Adversarial Reasoning Platform](https://www.steelman.cloud/)
17. [AI Pre-Mortem Prompt Template (DocsBot AI)](https://docsbot.ai/prompts/business/ai-pre-mortem-prompt)
18. [Automated Pre-Mortem Analysis with AI (Xebia)](https://xebia.com/articles/introducing-automated-pre-mortem-analysis-powered-by-artificial-intelligence/)
19. [WINQ Framework for Game Design Feedback](https://www.kathleenmercury.com/providing-feedback-on-prototypes-the-winq.html)
20. [GDC Vault: Improving Critique of Game Projects](https://www.gdcvault.com/play/1024966/Improving-Critique-of-Game-Projects)
21. [Design Pillars: The Core of Your Game (Game Developer)](https://www.gamedeveloper.com/design/design-pillars-the-core-of-your-game)
22. [Multi-Agent Debate for Requirements Engineering (2025)](https://arxiv.org/html/2507.05981v1)
23. [Adaptive Heterogeneous Multi-Agent Debate (Springer, 2025)](https://link.springer.com/article/10.1007/s44443-025-00353-3)
24. [LLM-as-Judge: Best Practices (Monte Carlo Data)](https://www.montecarlodata.com/blog-llm-as-judge/)
25. [How to build high-quality AI code review agent (Augment Code)](https://www.augmentcode.com/blog/how-we-built-high-quality-ai-code-review-agent)

---

## 8. 미해결 질문

- [ ] design-critic 에이전트의 평가 루브릭 5개 축별 세부 점수 기준 정의 필요
- [ ] hr-manager가 평가해야 할 에이전트 유형별 Definition of Done 목록 작성 필요
- [ ] 멀티 에이전트 debate (생성 에이전트 vs 비판 에이전트)가 단일 critic보다 나은지 실험 필요
- [ ] GDD 비판 시 게임 장르별(턴제 전략, 액션, RPG 등) 다른 루브릭이 필요한지 검토
- [ ] Constitutional AI critique-revision 루프를 Claude Code 워크플로에 통합하는 방법 (hook vs subagent)
- [ ] 에이전트 비판 결과를 Obsidian Vault에 자동 기록하는 파이프라인 설계
