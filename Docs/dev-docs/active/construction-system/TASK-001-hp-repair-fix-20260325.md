# TASK-001: 체력 회복/수리 버그 수정

- **날짜**: 2026-03-25
- **브랜치**: feature/TASK-001-construction-system
- **상태**: 완료

## 버그 내용

1. 턴이 지나도 체력이 회복되지 않음
2. 수리 기능 미동작

## 원인

BuildingSlot이 `health.OnDamaged` 이벤트를 구독하지 않아서 `Active → Damaged` 상태 전이가 발생하지 않음.
- Damaged 상태에 진입하지 못하니 ProcessTurn()의 auto-repair 분기도 실행 안 됨
- Destroyed 상태에는 정상 도달하지만, Damaged 경유 없이 바로 파괴됨

## 수정

- `BuildingSlot.Awake()`: `health.OnDamaged += HandleDamaged` 구독 추가
- `HandleDamaged(float)` 메서드 추가: Active 상태에서 피해 시 Damaged로 전이
- `OnDestroy()`: 구독 해제 추가
