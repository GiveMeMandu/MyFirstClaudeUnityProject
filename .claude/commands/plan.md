---
description: V2 기획 에이전트 팀 오케스트레이션
argument-hint: <subcommand> [args] (예: init, market, vision, system 건설, review Vision.md)
---

V2 기획 파이프라인의 통합 오케스트레이터입니다. 12명의 전문 에이전트 팀을 지휘합니다.

Arguments: $ARGUMENTS

## Instructions

### Phase 0: 서브커맨드 파싱

$ARGUMENTS에서 서브커맨드를 파싱합니다:

| 서브커맨드 | 에이전트 | 설명 |
|---|---|---|
| `init` | market + reference + vision 순차 | Phase 1 전체 (시장→레퍼런스→비전) |
| `market` | market-research-analyst | 시장 분석 |
| `reference [game]` | reference-game-analyst | 레퍼런스 게임 분석 |
| `vision` | vision-director | 비전 문서 인터뷰 |
| `system <name>` | system-designer-v2 | 특정 시스템 GDD |
| `balance [system]` | balance-designer | 밸런스 프레임워크 |
| `content` | content-designer | 콘텐츠/내러티브 |
| `economy` | economy-designer | 경제 모델 |
| `ux [screen]` | ux-flow-designer | UX 플로우 |
| `assess` | technical-assessor | 기술 타당성 |
| `wbs` | wbs-planner | WBS/마일스톤 |
| `review <doc>` | design-critic | 문서 비판적 리뷰 |
| `eval [agent]` | hr-manager | 에이전트 성과 평가 |
| `status` | (직접 처리) | Document-Status.md 조회 |
| `ingest [path]` | (직접 처리) | 외부 문서 추출 |

### Phase 1: 컨텍스트 로딩

서브커맨드에 따라 에이전트를 호출하기 전, 관련 컨텍스트를 준비합니다:
1. `Docs/V2/Planning/Document-Status.md` 확인 (문서 완성 현황)
2. 에이전트가 필요로 하는 상위 문서 존재 여부 확인
3. 상위 문서가 없으면 사용자에게 알리고 선행 작업 안내

### Phase 2: 에이전트 호출

Agent 도구를 사용하여 해당 에이전트를 호출합니다.

**호출 시 필수 전달 사항**:
- 현재 프로젝트 경로: `D:\Unity\MyFirstClaudeUnityProject`
- 산출물 경로: `Docs/V2/` 하위 해당 디렉토리
- 관련 참조 문서 경로 목록
- 사용자 요청 컨텍스트

**init 모드 실행 순서**:
```
1. market-research-analyst → Research/Market-Analysis.md
2. reference-game-analyst → Research/RefGames/*.md (5~10개 게임)
3. vision-director → Vision.md (시장+레퍼런스 결과 참조)
```

### Phase 3: 리뷰 게이트

에이전트 작업 완료 후:
1. 산출물을 사용자에게 보여주기
2. design-critic 리뷰 실행 여부 확인
3. 리뷰 결과에 따라:
   - APPROVED: Document-Status.md를 'approved'로 갱신
   - NEEDS_REVISION: 담당 에이전트에 수정 요청 → 재리뷰 (최대 3회)
   - REJECTED: 사용자와 협의 후 재작성 또는 방향 변경

### Phase 4: 상태 갱신

`Docs/V2/Planning/Document-Status.md`를 갱신합니다:
- 문서 경로, 담당 에이전트, 최종 수정일, 상태, 의존 문서

## status 서브커맨드 처리

`/plan status` 실행 시 Agent 호출 없이 직접 처리:
1. `Docs/V2/Planning/Document-Status.md` 읽기
2. 없으면 전체 V2 디렉토리 스캔하여 현황 표시
3. 각 문서의 상태(draft/review/approved/missing) 표시

## ingest 서브커맨드 처리

`/plan ingest [path]` 실행 시:
1. `python scripts/extract-documents.py` 스크립트 활용
2. 경로 미지정 시 전체 배치 추출 (`--batch all`)
3. 경로 지정 시 단일 문서 추출 (`--single <slug>`)

## 문서 작성 순서 가이드 (탑다운)

```
/plan market      → Step 1: 시장 분석
/plan reference   → Step 2: 레퍼런스 게임 분석
/plan vision      → Step 3: 비전 문서 인터뷰
/plan economy     → Step 4: 경제 모델
/plan system X    → Step 5: 시스템별 GDD (P0→P1→P2)
/plan balance     → Step 6: 밸런스 프레임워크
/plan content     → Step 7: 콘텐츠/내러티브
/plan ux          → Step 8: UX 플로우
/plan assess      → Step 9: 기술 타당성
/plan wbs         → Step 10: WBS/마일스톤
```

모든 문서 확정 전 `/plan review <doc>`으로 design-critic 리뷰 필수.
Phase 완료 시 `/plan eval`로 hr-manager 팀 평가.

## Examples

- `/plan init` — 시장조사+레퍼런스+비전 순차 실행
- `/plan vision` — 비전 문서 인터뷰 시작
- `/plan system 건설` — 건설 시스템 GDD 작성
- `/plan review Vision.md` — 비전 문서 비판적 리뷰
- `/plan eval vision-director` — 비전 디렉터 성과 평가
- `/plan status` — 전체 문서 현황 조회
- `/plan ingest` — D:\Krafton\ 문서 배치 추출
