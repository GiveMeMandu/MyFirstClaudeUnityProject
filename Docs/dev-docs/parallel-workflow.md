# Parallel Development Workflow

Git worktree를 활용한 병렬 시스템 개발 가이드.

## 전제 조건

- 모든 대상 시스템의 GDD가 완성되어 있을 것
- develop 브랜치가 안정 상태일 것
- Unity Editor가 메인 프로젝트에 연결되어 있을 것

## Phase 1: GDD 완성

`/system-design` 커맨드로 모든 대상 시스템의 기획서를 작성합니다.

```
/system-design 탐사/원정 시스템
/system-design 인카운터/이벤트 시스템
/system-design 기술 트리 시스템
...
```

출력: `Docs/GDD/systems/<slug>.md`

## Phase 2: Worktree 생성

```bash
# 일괄 생성
/worktree init exploration-system encounter-system tech-tree-system

# 또는 개별 생성
/worktree create exploration-system
```

각 worktree는 `.worktrees/<slug>/`에 생성되며, `feature/<slug>` 브랜치를 가집니다.

## Phase 3: 병렬 구현

### 방법 A: Subagent 활용 (권장)

Claude Code의 Agent 도구에서 `isolation: "worktree"`를 사용하여 subagent가 독립 worktree에서 작업:

```
Agent(isolation: "worktree", prompt: "implement exploration-system from GDD...")
Agent(isolation: "worktree", prompt: "implement encounter-system from GDD...")
```

### 방법 B: 순차 구현

각 worktree로 이동하며 순차적으로 구현:

```bash
cd .worktrees/exploration-system
# 구현 작업...
cd .worktrees/encounter-system
# 구현 작업...
```

### 방법 C: 멀티 터미널

여러 터미널에서 각 worktree에 Claude Code 세션을 열어 동시 작업:

```bash
# Terminal 1
cd .worktrees/exploration-system && claude

# Terminal 2
cd .worktrees/encounter-system && claude
```

## Phase 4: 순차 컴파일 검증

Unity Editor는 한 프로젝트만 열 수 있으므로, 메인 worktree에서 순차 검증합니다:

```bash
/worktree verify exploration-system encounter-system tech-tree-system
```

이 스크립트는:
1. develop에서 임시 integration 브랜치 생성
2. 각 feature 브랜치를 순차 merge
3. merge마다 Unity 컴파일 체크
4. 충돌 또는 에러 시 해당 브랜치 식별
5. 결과 리포트 출력
6. integration 브랜치 자동 삭제

## Phase 5: PR 생성 + 머지

검증 통과한 브랜치마다 개별 PR:

```bash
git checkout feature/exploration-system
/task-pr
```

## Phase 6: 정리

```bash
/worktree cleanup --all
/task-cleanup
```

## 주의사항

### Unity Editor 제약
- 한 PC에서 Unity Editor는 1개만 열 수 있음
- worktree는 코드 편집용, 컴파일 검증은 메인 worktree에서 순차 실행

### PS-NNN 카운터
- 모든 worktree가 `.git/ps-counter`를 공유 (git common dir)
- mkdir 기반 lock으로 동시 커밋 시 충돌 방지

### 파일 충돌 최소화
- 독립적인 시스템을 각기 다른 worktree에서 작업
- GameManager 등 공유 파일 수정이 필요하면 순차 작업 권장
- 충돌 발생 시 `worktree-compile-verify.sh`가 감지
