---
description: Git worktree 관리 (병렬 시스템 개발)
argument-hint: <create|list|verify|cleanup> [args...] (예: create exploration-system)
---

Git worktree를 활용한 병렬 시스템 개발을 관리합니다.

Arguments: $ARGUMENTS

## Instructions

1. **$ARGUMENTS 파싱** — 첫 번째 단어가 서브커맨드:

### create <slug> [base-branch]
단일 worktree 생성:
```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/worktree-create.sh" <slug> [base-branch]
```

### init <slug1> <slug2> ...
여러 worktree 일괄 생성:
```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/worktree-init-all.sh" <slug1> <slug2> ...
```

### verify <slug1> [slug2...]
순차 컴파일 검증 (Unity Editor 필요):
```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/worktree-compile-verify.sh" <slug1> [slug2...]
```

### cleanup <slug> | --all | --list
worktree 정리:
```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/worktree-cleanup.sh" <slug|--all|--list>
```

## 병렬 개발 워크플로우

1. GDD 기획서 전부 완성
2. `/worktree init slug1 slug2 slug3` — worktree 일괄 생성
3. 각 worktree에서 독립 구현 (subagent 또는 순차)
4. `/worktree verify slug1 slug2 slug3` — 순차 컴파일 검증
5. 각 브랜치에서 PR 생성
6. `/worktree cleanup --all` — 정리

## Examples
- `/worktree create exploration-system`
- `/worktree init exploration-system encounter-system tech-tree-system`
- `/worktree verify exploration-system encounter-system`
- `/worktree cleanup exploration-system`
- `/worktree cleanup --list`
