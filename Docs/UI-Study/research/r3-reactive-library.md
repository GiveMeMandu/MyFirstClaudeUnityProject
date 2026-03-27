# R3 리액티브 라이브러리 리서치 리포트

- **작성일**: 2026-03-27
- **카테고리**: library
- **상태**: 조사완료

---

## 1. 요약

R3는 UniRx의 공식 3세대 후속으로, 같은 작성자(neuecc/Cysharp)가 개발. v1.3.0 (2025.02) 활발히 유지. UniRx는 사실상 중단. R3는 에러 시 구독 유지, 성능 개선, 멀티플랫폼 지원이 핵심 차이점.

## 2. 상세 분석

### 2.1 UniRx → R3 진화

| 세대 | 이름 | 범위 |
|---|---|---|
| 1세대 | Rx.NET | Microsoft 공식 |
| 2세대 | UniRx | Unity 전용 |
| 3세대 | R3 | Unity + Godot + WPF + MAUI 등 |

### 2.2 핵심 API 차이

```csharp
// === 에러 처리 ===
// UniRx: OnError → 구독 종료 (위험!)
// R3: OnErrorResume → 구독 유지
observable.Subscribe(
    onNext: x => Process(x),
    onErrorResume: ex => Debug.LogException(ex), // 구독 안 죽음
    onCompleted: result => { }
);

// === 오퍼레이터 이름 변경 ===
// UniRx        → R3
// Throttle     → Debounce
// Buffer       → Chunk
// StartWith    → Prepend
// Sample       → ThrottleLast
```

### 2.3 Unity 전용 기능 (R3.Unity)

```csharp
// 프레임 기반 오퍼레이터
Observable.EveryUpdate()
Observable.IntervalFrame(5)
someObservable.DelayFrame(3)
someObservable.DebounceFrame(2)

// Inspector 노출
[SerializeField] SerializableReactiveProperty<int> health = new(100);

// 구독 해제
observable.Subscribe(x => { }).AddTo(this); // destroyCancellationToken 연동

// 디버깅
ObservableTracker.EnableTracking = true; // Window > Observable Tracker
```

### 2.4 Disposal 패턴 (성능 순)

```csharp
// 1. 고정 개수 (최고 성능) — 최대 8개
var d = Disposable.Combine(d1, d2, d3);

// 2. 빌더 패턴 (권장)
DisposableBag bag = new();
sub1.AddTo(ref bag);
sub2.AddTo(ref bag);

// 3. CompositeDisposable (스레드 안전, 가장 느림)
CompositeDisposable composite = new();
```

## 3. 베스트 프랙티스

### DO (권장)
- IInitializable.Initialize()에서 Subscribe
- AddTo(this) 또는 AddTo(ref disposableBag)로 수명 관리
- ObservableTracker 개발 중 활성화
- ReactiveProperty 외부 노출 시 ReadOnlyReactiveProperty 사용

### DON'T (금지)
- VContainer Construct()에서 Subscribe (데드락)
- OnError 무시 (R3는 OnErrorResume으로 대체)
- AsSystemObservable().ToUniTask() on hot observable (미완료 문제)

### CONSIDER (상황별)
- SerializableReactiveProperty는 Inspector 노출 필요 시에만 (약간 추가 메모리)
- Odin Inspector 사용 시 SerializableReactiveProperty 충돌 주의

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 2022+ 권장 | destroyCancellationToken 활용 |
| VContainer | 1.17+ | IStartable/IDisposable 패턴 동일 |
| UniTask | 2.5+ | SubscribeAwait 지원 |
| NuGet | 2.5M 다운로드 | 활발한 채택 |

## 5. 설치

```
// UPM Git URL
https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity
```

NuGetForUnity로 코어 R3 패키지도 필요할 수 있음.

## 6. 참고 자료

1. [Cysharp/R3 GitHub](https://github.com/Cysharp/R3)
2. [R3 README](https://github.com/Cysharp/R3/blob/main/README.md)
3. [neuecc Medium — R3 발표](https://neuecc.medium.com/r3-a-new-modern-reimplementation-of-reactive-extensions-for-c-cf29abcc5826)
4. [VContainer + UniRx Integration](https://vcontainer.hadashikick.jp/integrations/unirx)
