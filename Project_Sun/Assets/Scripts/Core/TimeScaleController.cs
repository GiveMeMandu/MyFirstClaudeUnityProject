using System;
using UnityEngine;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 전투 시간 조절. 1x / 2x / 일시정지 (GDD: 3x 제거됨).
    /// ECS는 Time.timeScale을 통해 SystemAPI.Time.DeltaTime이 자동 반영된다.
    /// 일시정지(timeScale=0) 중에도 분대 명령 입력은 가능하다 (GDD 엣지케이스 6).
    ///
    /// WaveDefense.md 2.2, 3.3 참조:
    /// - 속도: 1x / 2x + 일시정지
    /// - 일시정지 중 명령 지연: 0초 (즉시 실행)
    /// - 2x 배속 중 건물 손상 시: 자동 1x 전환 + 경고 (설정으로 비활성화 가능)
    ///
    /// SF-WD-011.
    /// </summary>
    public class TimeScaleController : MonoBehaviour
    {
        /// <summary>
        /// 시간 속도 단계.
        /// GDD: 1x / 2x + 일시정지 (3x는 제거됨, WaveDefense.md 13.2 참조).
        /// </summary>
        public enum TimeSpeed
        {
            Paused = 0,
            Normal = 1,
            Fast = 2
        }

        [Header("Settings")]
        [Tooltip("2x 배속 중 건물 손상 시 자동으로 1x로 전환할지 여부 (GDD 엣지케이스 9)")]
        [SerializeField] bool _autoSlowOnDamage = true;

        TimeSpeed _currentSpeed = TimeSpeed.Normal;
        TimeSpeed _speedBeforePause = TimeSpeed.Normal;

        /// <summary>현재 속도 단계.</summary>
        public TimeSpeed CurrentSpeed => _currentSpeed;

        /// <summary>일시정지 상태인지 여부.</summary>
        public bool IsPaused => _currentSpeed == TimeSpeed.Paused;

        /// <summary>2x 배속 중 건물 손상 자동 감속 설정 (GDD 엣지케이스 9).</summary>
        public bool AutoSlowOnDamage
        {
            get => _autoSlowOnDamage;
            set => _autoSlowOnDamage = value;
        }

        /// <summary>
        /// 속도 변경 시 발행. UI 갱신용.
        /// </summary>
        public event Action<TimeSpeed> OnSpeedChanged;

        /// <summary>
        /// 지정된 속도로 전환.
        /// </summary>
        public void SetSpeed(TimeSpeed speed)
        {
            if (_currentSpeed == speed) return;

            var previous = _currentSpeed;
            _currentSpeed = speed;

            Time.timeScale = speed switch
            {
                TimeSpeed.Paused => 0f,
                TimeSpeed.Normal => 1f,
                TimeSpeed.Fast => 2f,
                _ => 1f
            };

            // Pause 직전 속도를 기억하여 TogglePause 복귀에 사용
            if (speed == TimeSpeed.Paused && previous != TimeSpeed.Paused)
                _speedBeforePause = previous;

            OnSpeedChanged?.Invoke(speed);
        }

        /// <summary>
        /// 일시정지 토글. 해제 시 일시정지 직전 속도로 복귀.
        /// GDD: 일시정지 남용 제한 없음 (WaveDefense.md 엣지케이스 6).
        /// </summary>
        public void TogglePause()
        {
            if (_currentSpeed == TimeSpeed.Paused)
                SetSpeed(_speedBeforePause);
            else
                SetSpeed(TimeSpeed.Paused);
        }

        /// <summary>
        /// Paused -> Normal -> Fast -> Paused 순환.
        /// </summary>
        public void CycleSpeed()
        {
            SetSpeed(_currentSpeed switch
            {
                TimeSpeed.Paused => TimeSpeed.Normal,
                TimeSpeed.Normal => TimeSpeed.Fast,
                TimeSpeed.Fast => TimeSpeed.Paused,
                _ => TimeSpeed.Normal
            });
        }

        /// <summary>
        /// 2x 배속 중 건물 손상 발생 시 호출.
        /// GDD 엣지케이스 9: 자동 1x 전환 + 경고 (설정으로 비활성화 가능).
        /// BattleUIBridge 또는 건물 손상 이벤트 구독자가 호출한다.
        /// </summary>
        public void NotifyBuildingDamaged()
        {
            if (!_autoSlowOnDamage) return;
            if (_currentSpeed != TimeSpeed.Fast) return;

            SetSpeed(TimeSpeed.Normal);
            Debug.Log("[TimeScaleController] Auto-slowed to 1x due to building damage (edge case 9)");
        }

        /// <summary>
        /// 밤 페이즈 시작 시 호출. 기본 속도(1x)로 초기화.
        /// </summary>
        public void ResetToNormal()
        {
            _speedBeforePause = TimeSpeed.Normal;
            SetSpeed(TimeSpeed.Normal);
        }

        void OnDestroy()
        {
            // 씬 전환 등으로 파괴 시 timeScale 복원
            Time.timeScale = 1f;
        }
    }
}
