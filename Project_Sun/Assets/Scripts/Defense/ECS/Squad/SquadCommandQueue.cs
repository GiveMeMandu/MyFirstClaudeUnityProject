using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// MonoBehaviour → ECS 명령 전달 버퍼.
    ///
    /// MonoBehaviour(SquadInputController)가 Enqueue,
    /// ECS(SquadCommandSystem)가 Dequeue.
    ///
    /// NativeQueue.ParallelWriter로 메인 스레드에서 안전하게 Enqueue 가능.
    /// 일시정지(timeScale=0) 중에도 Enqueue는 정상 작동 — ECS 시스템은
    /// 재개 시 큐의 명령을 즉시 처리.
    /// </summary>
    public static class SquadCommandQueue
    {
        static NativeQueue<SquadCommandEntry> _queue;

        /// <summary>큐 초기화. MonoBehaviour.Awake 또는 시스템 OnCreate에서 1회 호출.</summary>
        public static void Initialize()
        {
            if (_queue.IsCreated) return;
            _queue = new NativeQueue<SquadCommandEntry>(Allocator.Persistent);
        }

        /// <summary>큐 해제. OnDestroy에서 호출.</summary>
        public static void Dispose()
        {
            if (_queue.IsCreated) _queue.Dispose();
        }

        /// <summary>명령 추가. MonoBehaviour에서 호출 (메인 스레드).</summary>
        public static void Enqueue(SquadCommandEntry entry)
        {
            if (!_queue.IsCreated) Initialize();
            _queue.Enqueue(entry);
        }

        /// <summary>큐의 모든 명령을 꺼냄. ECS 시스템에서 호출.</summary>
        public static bool TryDequeue(out SquadCommandEntry entry)
        {
            if (!_queue.IsCreated)
            {
                entry = default;
                return false;
            }
            return _queue.TryDequeue(out entry);
        }

        public static int Count => _queue.IsCreated ? _queue.Count : 0;
    }

    /// <summary>
    /// 명령 큐 엔트리.
    /// MonoBehaviour가 생성하고 ECS 시스템이 소비.
    /// </summary>
    public struct SquadCommandEntry
    {
        /// <summary>대상 분대 ID. -1이면 전체 분대.</summary>
        public int SquadId;
        public SquadCommandType CommandType;
        public float3 TargetPosition;
        /// <summary>명령 발행 시간 (Time.unscaledTime). 반응 지연 측정용.</summary>
        public double IssuedTime;
    }
}
