using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// Channel&lt;T&gt; 기반 이벤트 버스.
    /// 여러 시스템이 Publish하고, 여러 UI가 Subscribe하는 pub/sub 패턴.
    /// Publish()로 Connect()한 후 .Subscribe()로 다중 구독 가능.
    /// </summary>
    public class TurnEventBroker<T> : IDisposable
    {
        private readonly Channel<T> _channel;
        private readonly IConnectableUniTaskAsyncEnumerable<T> _multicast;
        private readonly IDisposable _connection;

        public TurnEventBroker()
        {
            _channel = Channel.CreateSingleConsumerUnbounded<T>();
            _multicast = _channel.Reader.ReadAllAsync().Publish();
            _connection = _multicast.Connect();
        }

        /// <summary>
        /// 이벤트 발행 — 어떤 시스템에서든 호출 가능.
        /// </summary>
        public void Publish(T value) => _channel.Writer.TryWrite(value);

        /// <summary>
        /// 구독 — 여러 UI Presenter가 동시 구독 가능.
        /// </summary>
        public IUniTaskAsyncEnumerable<T> Subscribe() => _multicast;

        public void Dispose()
        {
            _channel.Writer.TryComplete();
            _connection.Dispose();
        }
    }

    /// <summary>
    /// 턴 이벤트 데이터.
    /// </summary>
    public enum TurnEventType { ResourceGained, BuildingCompleted, BattleResult, TurnEnd }

    public struct TurnEvent
    {
        public TurnEventType Type;
        public string Message;
        public int Amount;
    }
}
