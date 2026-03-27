using R3;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// ObservableTracker — R3의 구독 누수 디버깅 도구.
    /// 개발 중에만 활성화하여 활성 구독 수를 모니터링.
    /// Window > Observable Tracker 에디터 윈도우와 연동.
    ///
    /// 주의: 프로덕션에서는 비활성화할 것 (성능 영향).
    /// </summary>
    public class LeakDetector : IStartable
    {
        public void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ObservableTracker.EnableTracking = true;
            ObservableTracker.EnableStackTrace = true;
            Debug.Log("[LeakDetector] ObservableTracker 활성화 — Window > Observable Tracker에서 확인");
#endif
        }

        /// <summary>
        /// 현재 활성 구독 수를 로그로 출력.
        /// 디버그 버튼이나 치트 키에 연결하여 사용.
        /// </summary>
        public static void LogActiveSubscriptions()
        {
            var count = 0;
            ObservableTracker.ForEachActiveTask(info =>
            {
                count++;
                Debug.Log($"  [{count}] {info.FormattedType} — added at {info.AddTime:F1}s");
            });
            Debug.Log($"[LeakDetector] 활성 구독 수: {count}");
        }
    }
}
