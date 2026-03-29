namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 8: 복잡성 판단 매트릭스 — 기술 도입 결정 가이드.
    ///
    /// ┌──────────────────────────────────────────────────────────────────┐
    /// │                    복잡성 판단 의사결정 트리                       │
    /// ├──────────────────────────────────────────────────────────────────┤
    /// │                                                                  │
    /// │  Q1: 이벤트가 단순 1:1 (버튼→액션) 인가?                          │
    /// │      YES → C# event로 충분                                       │
    /// │      NO  → Q2로                                                  │
    /// │                                                                  │
    /// │  Q2: 시간 기반 조작이 필요한가? (디바운스, 스로틀, 딜레이)           │
    /// │      YES → R3 부분 도입 (Observable.FromEvent + 연산자)           │
    /// │      NO  → Q3로                                                  │
    /// │                                                                  │
    /// │  Q3: 여러 이벤트를 조합해야 하는가? (CombineLatest, Merge, Zip)   │
    /// │      YES → R3 부분 도입                                          │
    /// │      NO  → Q4로                                                  │
    /// │                                                                  │
    /// │  Q4: 상태가 여러 곳에서 공유되어야 하는가?                         │
    /// │      YES → ReactiveProperty 도입 고려                            │
    /// │      NO  → C# event 유지                                        │
    /// │                                                                  │
    /// ├──────────────────────────────────────────────────────────────────┤
    /// │                                                                  │
    /// │  【C# event만으로 충분한 경우】                                    │
    /// │  - 버튼 클릭 → 단일 액션                                         │
    /// │  - 토글/슬라이더 → 단일 값 변경                                   │
    /// │  - 리스트 선택 → 상세 표시                                        │
    /// │  - Apply/Cancel 패턴                                             │
    /// │  ✓ Step 1-7 전부 가능                                            │
    /// │                                                                  │
    /// │  【R3를 추가해야 하는 경우】                                       │
    /// │  - 검색 입력 디바운스 (Debounce)                                  │
    /// │  - 버튼 연타 방지 (ThrottleFirst)                                │
    /// │  - 여러 필터 조합 (CombineLatest)                                │
    /// │  - 복수 이벤트 스트림 병합 (Merge)                                │
    /// │  - 자동 저장 (Buffer + Throttle)                                  │
    /// │  ✓ Step 8: Debounce, ThrottleFirst만 부분 도입                   │
    /// │                                                                  │
    /// │  【VContainer를 추가해야 하는 경우】                               │
    /// │  - 15개 이상 화면 (의존성 수동 관리 한계)                          │
    /// │  - 공유 서비스 (Auth, Analytics, Config)                          │
    /// │  - 화면 간 데이터 공유 (Scoped Lifetime)                          │
    /// │  - 테스트 용이성 (Mock 주입)                                      │
    /// │  ✓ 이 학습에서는 도입하지 않음 (Step 1-8 범위 밖)                 │
    /// │                                                                  │
    /// └──────────────────────────────────────────────────────────────────┘
    /// </summary>
    public static class ComplexityEvaluator
    {
        /// <summary>
        /// 코드 줄 수 비교: 수동 디바운스 vs R3 Debounce.
        /// </summary>
        public static string GetDebounceComparison()
        {
            return @"
=== Manual Debounce (C# event + UniTask) ===
Lines: ~30 (CTS 생성/취소/Dispose + async method + try/catch)
Risk:  Race condition, CTS leak, OperationCanceledException handling

=== R3 Debounce ===
Lines: 3 (FromEvent → Debounce → Subscribe)
Risk:  None (자동 취소, 자동 구독 해제 via CompositeDisposable)

=== Verdict ===
시간 기반 이벤트 조작이 필요하면 R3를 부분 도입하라.
불필요한 복잡성은 피하되, 정당한 복잡성은 수용하라.";
        }

        /// <summary>
        /// 기술 스택 도입 순서 권장.
        /// </summary>
        public static string GetAdoptionOrder()
        {
            return @"
1. C# events + plain classes (모든 프로젝트의 시작)
2. + UniTask (async/await 필요 시)
3. + R3 부분 도입 (Debounce/Throttle/Combine 필요 시)
4. + R3 ReactiveProperty (다수 구독자, 상태 공유 필요 시)
5. + VContainer (15+ 화면, 공유 서비스, 테스트 필요 시)

각 단계는 이전 단계가 한계에 도달했을 때만 추가.
'혹시 필요할까봐' 미리 도입하지 않는다.";
        }
    }
}
