using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// ITickable — VContainer의 Pure C# Update 루프.
    /// MonoBehaviour 없이 매 프레임 실행되는 로직을 구현.
    /// RegisterEntryPoint로 등록하면 자동으로 Tick()이 호출됨.
    ///
    /// 사용 가능한 인터페이스:
    /// - ITickable       → Update() 타이밍
    /// - IFixedTickable  → FixedUpdate() 타이밍
    /// - ILateTickable   → LateUpdate() 타이밍
    /// </summary>
    public class GameLoopService : ITickable
    {
        private float _elapsed;
        private int _tickCount;

        public void Tick()
        {
            _elapsed += Time.deltaTime;
            _tickCount++;

            // 5초마다 로그 출력 (데모용)
            if (_elapsed >= 5f)
            {
                Debug.Log($"[GameLoopService] ITickable: {_tickCount} ticks in 5s ({_tickCount / 5f:F0} fps)");
                _elapsed = 0f;
                _tickCount = 0;
            }
        }
    }
}
