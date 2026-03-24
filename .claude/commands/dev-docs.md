---
description: 태스크에 대한 전략적 계획과 구조화된 개발 문서 생성
argument-hint: 계획할 작업 설명 (예: "플레이어 이동 시스템 구현", "씬 전환 리팩토링")
---

You are an elite strategic planning specialist. Create a comprehensive, actionable plan for: $ARGUMENTS

## Instructions

1. **Analyze the request** and determine the scope of planning needed
2. **Examine relevant files** in the codebase to understand current state
3. **Create a structured plan** with:
   - Executive Summary (요약)
   - Current State Analysis (현재 상태)
   - Proposed Future State (목표 상태)
   - Implementation Phases (구현 단계)
   - Detailed Tasks (세부 태스크, 명확한 완료 기준 포함)
   - Risk Assessment (위험 요소 및 완화 방법)
   - Success Metrics (성공 지표)

4. **Task Breakdown Structure**:
   - 각 주요 섹션은 단계(phase) 또는 컴포넌트를 나타냄
   - 태스크에 번호 및 우선순위 부여
   - 각 태스크에 명확한 완료 기준 포함
   - 태스크 간 의존성 명시
   - 작업량 추정 (S/M/L/XL)

5. **Create task management structure**:
   - 디렉토리 생성: `Docs/dev-docs/active/[task-name]/`
   - 세 파일 생성:
     - `[task-name]-plan.md` — 전략적 계획 전문
     - `[task-name]-context.md` — 핵심 파일, 결정사항, 의존성
     - `[task-name]-tasks.md` — 체크리스트 형식의 진행 추적
   - 각 파일 상단에 "Last Updated: YYYY-MM-DD" 포함

## Quality Standards
- 계획은 모든 필요한 컨텍스트를 자체적으로 포함해야 함
- 명확하고 실행 가능한 언어 사용
- 관련 기술 세부사항 포함
- 잠재적 위험 및 엣지 케이스 고려

## Context References
- `CLAUDE.md` — 프로젝트 구조 및 Unity CLI 참조
- `Docs/GDD/` — 게임 기획 문서 참조
- `Project_Sun/Assets/` — 현재 구현 상태 파악

**Note**: 플랜 모드 종료 후 구체적인 계획이 확정됐을 때 이 커맨드를 사용하세요. 컨텍스트 리셋 후에도 살아남는 영구적인 태스크 구조를 생성합니다.
