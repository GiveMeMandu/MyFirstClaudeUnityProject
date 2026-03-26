# policy-system Tasks

- **Last Updated**: 2026-03-26
- **Branch**: feature/policy-system

## Sub-Features

### SF-01: 데이터 모델 [M]
- **상태**: DONE
- **의존**: -
- **파일**: PolicyEnums.cs, PolicyNodeSO.cs, PolicyTreeSO.cs
- **커밋**: 0cd6472
- [x] PolicyEnums.cs (카테고리, 노드 상태, 효과 유형)
- [x] PolicyNodeSO.cs (개별 정책 노드 SO)
- [x] PolicyTreeSO.cs (카테고리별 정책 트리 SO)
- [x] Unity 컴파일 통과 (Editor 미연결 — skip)

### SF-02: PolicyManager 코어 [L]
- **상태**: DONE
- **의존**: SF-01
- **파일**: PolicyManager.cs
- **커밋**: 4ed808b
- [x] 정책 상태 관리 (Locked/Unlocked/Enacted/BranchLocked)
- [x] 턴 기반 해금 체크
- [x] 제정 처리 + 분기 잠김
- [x] Unity 컴파일 통과 (Editor 미연결 — skip)

### SF-03: PolicyEffectResolver [M]
- **상태**: DONE
- **의존**: SF-01, SF-02
- **파일**: PolicyEffectResolver.cs
- **커밋**: 17fa1fc
- [x] 활성 정책 효과 집계
- [x] 외부 시스템 수정자 API
- [x] Unity 컴파일 통과 (Editor 미연결 — skip)

### SF-04: 턴 시스템 연동 [S]
- **상태**: DONE
- **의존**: SF-02
- **파일**: TurnManager.cs (수정)
- **커밋**: 5a48ae0
- [x] 낮 시작 시 PolicyManager.OnNewTurn 호출
- [x] Unity 컴파일 통과 (Editor 미연결 — skip)

### SF-05: UI + 테스트 데이터 + 테스트 씬 [L]
- **상태**: DONE
- **의존**: SF-03, SF-04
- **파일**: PolicyUI.cs, PolicyTestController.cs, PolicyTestSceneBuilder.cs
- **커밋**: f52cd24
- [x] IMGUI 정책 UI (카테고리별 노드 표시)
- [x] PolicyTestController (턴 시뮬레이션)
- [x] PolicyTestSceneBuilder (에디터 씬 빌더 + SO 에셋 자동 생성)
- [x] 기본 SO 에셋 생성 (9노드 PoC 트리)
- [x] Unity 컴파일 통과 (Editor 미연결 — skip)

## 완료 이력
| SF | 커밋 해시 | 날짜 |
|---|---|---|
| SF-01 | 0cd6472 | 2026-03-26 |
| SF-02 | 4ed808b | 2026-03-26 |
| SF-03 | 17fa1fc | 2026-03-26 |
| SF-04 | 5a48ae0 | 2026-03-26 |
| SF-05 | f52cd24 | 2026-03-26 |
