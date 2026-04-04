# V2 기획 문서 현황

최종 갱신: 2026-04-04 (WBS v1.1 반영)

## 문서 상태

| # | 문서 | 경로 | 담당 | 상태 | 최종 수정일 | 의존 문서 |
|---|---|---|---|---|---|---|
| 1 | 시장 분석 | Research/Market-Analysis.md | market-research-analyst | approved | 2026-04-01 | — |
| 2 | 경쟁 분석 | Research/Competitive-Analysis.md | market-research-analyst | approved | 2026-04-01 | — |
| 3 | 레퍼런스 게임 분석 | Research/RefGames/*.md | reference-game-analyst | approved | 2026-04-01 | Market-Analysis |
| 4 | 비전 문서 | Vision.md (v2.6) | vision-director | approved | 2026-04-03 | Market-Analysis, RefGames |
| 5 | 경제 모델 | Economy/*.md (v1.2) | economy-designer | approved | 2026-04-03 | Vision |
| 6a | 건설 시스템 GDD | Systems/Construction.md (v0.2) | system-designer-v2 | approved | 2026-04-03 | Vision, Economy |
| 6b | 인력 관리 시스템 GDD | Systems/Workforce.md (v0.2) | system-designer-v2 | approved | 2026-04-03 | Vision, Economy |
| 6c | 웨이브 방어 시스템 GDD | Systems/WaveDefense.md (v0.3) | system-designer-v2 | approved | 2026-04-03 | Vision, Economy |
| 6d | 탐사/원정 시스템 GDD | Systems/Exploration.md (v0.2) | system-designer-v2 | approved | 2026-04-03 | Vision |
| 7 | 밸런스 프레임워크 | Balance.md (v1.1) | balance-designer | approved | 2026-04-03 | Systems, Economy |
| 8 | 콘텐츠/내러티브 | Content/Content.md (v1.2) | content-designer | approved | 2026-04-03 | Vision, Exploration |
| 9 | UX 플로우 | UX/Screen-Flow.md (v1.1) + Wireframes 7종 | ux-flow-designer | approved | 2026-04-03 | Systems |
| 10 | 기술 타당성 | Tech-Assessment.md (v1.1) | technical-assessor | approved | 2026-04-03 | Systems, Vision |
| 11 | WBS/마일스톤 | WBS.md (v1.1) | wbs-planner | approved | 2026-04-04 | All above |

## 상태 범례

- `missing` — 미작성
- `draft` — 초안 작성 완료, 리뷰 전
- `review` — design-critic 리뷰 중
- `revision` — 리뷰 후 수정 중
- `approved` — 리뷰 통과, 확정
