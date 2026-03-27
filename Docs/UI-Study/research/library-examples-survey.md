# 라이브러리 공식 예제 + 실전 패턴 조사 종합

- **작성일**: 2026-03-28
- **카테고리**: integration
- **상태**: 조사완료

---

## 조사 범위

5개 병렬 에이전트로 조사:
1. VContainer 공식 문서/예제
2. R3 공식 예제 + 커뮤니티 패턴
3. UnityScreenNavigator + uPalette
4. UniTask 고급 UI 패턴
5. UGUI 실전 게임 UI 패턴

## 핵심 발견 요약

### VContainer — 미구현 필수 패턴
| 패턴 | 중요도 | 출처 |
|---|---|---|
| Project Root LifetimeScope (VContainerSettings) | 필수 | 공식 문서 |
| EnqueueParent 멀티씬 계층 | 필수 | 공식 문서 |
| RegisterFactory (Func<TParam,T>) | 필수 | 공식 문서 |
| IAsyncStartable | 높음 | UniTask 통합 문서 |
| ITickable 게임 루프 | 높음 | EntryPoint 문서 |
| Source Generator | 높음 | 최적화 문서 |
| CreateChildFromPrefab 팝업 | 중간 | code-first 문서 |

### R3 — 미구현 필수 패턴
| 패턴 | 중요도 |
|---|---|
| ReactiveCommand + BindTo + CanExecute | 필수 |
| CombineLatest 파생 상태 | 필수 |
| SerializableReactiveProperty Inspector | 필수 |
| ObservableTracker 구독 누수 디버깅 | 필수 |
| Merge 다중 입력 통합 | 높음 |
| EveryValueChanged 폴링 | 중간 |
| Zip/ZipLatest 단계 동기화 | 중간 |

### UnityScreenNavigator — 미구현 필수 패턴
| 패턴 | 중요도 |
|---|---|
| AddLifecycleEvent Presenter 연결 | 필수 |
| Page<TView> 제네릭 베이스 | 필수 |
| WillPushEnter 프리로드 | 필수 |
| 중첩 SheetContainer 탭 | 높음 |
| AddressableAssetLoader | 높음 |
| TransitionAnimationBehaviour + PartnerRect | 중간 |
| IPageContainerCallbackReceiver | 낮음 |

### UniTask — 미구현 고급 패턴
| 패턴 | 중요도 |
|---|---|
| WhenAny + CancelAfterSlim 타임아웃 | 필수 |
| Channel<T> 이벤트 버스 | 높음 |
| AsyncReactiveProperty.BindTo | 높음 |
| IUniTaskAsyncEnumerable | 중간 |
| UNITASK_DOTWEEN_SUPPORT 디파인 | 필수 (설정) |
| SuppressCancellationThrow | 중간 |
| Preserve 다중 await | 낮음 |

### UGUI 실전 패턴 — 미구현
| 패턴 | 우선순위 |
|---|---|
| Canvas 5-레이어 구조 확립 | 필수 (최우선) |
| 로딩 화면 + IProgress | 필수 |
| 토스트 알림 큐 | 높음 |
| 체력바 리액티브 애니메이션 | 높음 |
| 인벤토리 드래그앤드롭 | 중간 |
| 플로팅 데미지 텍스트 | 중간 |
| 설정 메뉴 (PlayerPrefs) | 중간 |
| 툴팁 시스템 | 중간 |
| 컨텍스트 메뉴 | 낮음 |

## 참고 레포지토리
- [mackysoft/VContainer-Examples](https://github.com/mackysoft/VContainer-Examples)
- [annulusgames/ReactiveInputSystem](https://github.com/annulusgames/ReactiveInputSystem)
- [annulusgames/UGUIAnimationSamples](https://github.com/annulusgames/UGUIAnimationSamples)
- [NorthTH/Unity-MVP-with-Vcontainer](https://github.com/NorthTH/Unity-MVP-with-Vcontainer)
- [JoanStinson/UnityUIOptimizationTool](https://github.com/JoanStinson/UnityUIOptimizationTool)
