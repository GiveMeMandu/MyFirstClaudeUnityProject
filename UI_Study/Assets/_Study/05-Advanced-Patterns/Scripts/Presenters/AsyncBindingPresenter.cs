using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using TMPro;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Advanced.Presenters
{
    /// <summary>
    /// AsyncReactiveProperty — UniTask의 리액티브 프로퍼티.
    /// R3 ReactiveProperty와 달리 async LINQ 스트림으로 동작.
    /// BindTo로 TMP 텍스트에 직접 바인딩 가능.
    /// </summary>
    public class AsyncBindingPresenter : IStartable, IDisposable
    {
        private readonly AsyncReactiveProperty<int> _score = new(0);
        private readonly AsyncReactiveProperty<string> _status = new("Ready");

        // View 참조는 VContainer로 주입
        private readonly AsyncBindingView _view;

        public AsyncBindingPresenter(AsyncBindingView view)
        {
            _view = view;
        }

        public void Start()
        {
            var ct = _view.destroyCancellationToken;

            // BindTo — AsyncReactiveProperty를 TMP에 직접 바인딩
            _score
                .Select(s => $"Score: {s}")
                .BindTo(_view.ScoreText, ct);

            _status.BindTo(_view.StatusText, ct);

            // WaitAsync — 특정 조건까지 대기
            WaitForHighScoreAsync(ct).Forget();
        }

        private async UniTaskVoid WaitForHighScoreAsync(System.Threading.CancellationToken ct)
        {
            // score가 100 이상이 될 때까지 대기
            await _score.Where(s => s >= 100).FirstAsync(ct);
            _status.Value = "High Score Reached!";
            Debug.Log("[AsyncBinding] Score reached 100!");
        }

        public void AddScore(int amount) => _score.Value += amount;

        public void Dispose()
        {
            _score.Dispose();
            _status.Dispose();
        }
    }

    /// <summary>
    /// AsyncBinding View.
    /// </summary>
    public class AsyncBindingView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _statusText;

        public TextMeshProUGUI ScoreText => _scoreText;
        public TextMeshProUGUI StatusText => _statusText;
    }
}
