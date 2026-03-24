# TASK-001: 건설 시스템 테스트 씬 생성

- **날짜**: 2026-03-24
- **브랜치**: feature/TASK-001-construction-system
- **상태**: 진행중

## 목표

건설 시스템 동작을 확인할 수 있는 테스트 씬 생성. Thronefall 스타일 쿼터뷰 맵에 건물 슬롯 배치.

## 생성 파일

| 파일 | 역할 |
|---|---|
| `Scripts/Construction/Testing/BuildingSlotVisual.cs` | 슬롯 상태별 비주얼 |
| `Scripts/Construction/Testing/ConstructionTestController.cs` | 테스트 UI + 턴 진행 |
| `Scripts/Editor/ConstructionTestSceneBuilder.cs` | 에디터 씬 빌더 |

## 씬 구성

- Ground Plane (30x30)
- 쿼터뷰 Orthographic 카메라
- 7개 BuildingSlot (중앙 HQ + 6개 주변)
- BuildingData SO 7개
- IMGUI 테스트 패널
