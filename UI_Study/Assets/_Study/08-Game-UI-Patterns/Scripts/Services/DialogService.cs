using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.GameUI.Models;
using UIStudy.GameUI.Views;

namespace UIStudy.GameUI.Services
{
    /// <summary>
    /// 대화 큐 서비스 — DialogLine 배열을 순차 표시, await 패턴.
    /// </summary>
    public class DialogService : IDisposable
    {
        private readonly DialogView _dialogView;
        private CancellationTokenSource _sessionCts;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public DialogService(DialogView dialogView)
        {
            _dialogView = dialogView;
        }

        /// <summary>
        /// 대화 시퀀스 비동기 실행.
        /// 각 라인: 화자 설정 -> 타이프라이터 -> 계속 화살표 -> 클릭 대기 -> 다음 라인.
        /// </summary>
        public async UniTask ShowDialog(DialogLine[] lines, CancellationToken externalCt = default)
        {
            if (_isPlaying) return;
            _isPlaying = true;

            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            _sessionCts = new CancellationTokenSource();

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _sessionCts.Token, externalCt);

            try
            {
                _dialogView.SetVisible(true);

                for (int i = 0; i < lines.Length; i++)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();
                    var line = lines[i];

                    _dialogView.SetSpeaker(line.Speaker);

                    // 타이프라이터용 CTS — 스킵 버튼으로 취소 가능
                    var typeCts = new CancellationTokenSource();
                    var typeLinked = CancellationTokenSource.CreateLinkedTokenSource(
                        typeCts.Token, linkedCts.Token);

                    // 스킵 버튼 클릭 시 타이핑 취소
                    var skipSub = _dialogView.SkipButton.OnClickAsObservable()
                        .Subscribe(_ => typeCts.Cancel());

                    // 타이프라이터 실행
                    await _dialogView.TypeText(line.Text, line.CharDelay, typeLinked.Token);

                    skipSub.Dispose();
                    typeLinked.Dispose();
                    typeCts.Dispose();

                    // 텍스트 완전 표시 보장
                    _dialogView.ShowFullText();

                    // 계속 화살표 표시 + 클릭 대기
                    _dialogView.ShowContinueArrow();
                    await WaitForClickAsync(linkedCts.Token);
                    _dialogView.HideContinueArrow();
                }

                _dialogView.SetVisible(false);
            }
            catch (OperationCanceledException)
            {
                _dialogView.SetVisible(false);
            }
            finally
            {
                linkedCts.Dispose();
                _isPlaying = false;
            }
        }

        /// <summary>
        /// 스킵 버튼 클릭 대기 (UniTaskCompletionSource).
        /// </summary>
        private async UniTask WaitForClickAsync(CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource();

            var sub = _dialogView.SkipButton.OnClickAsObservable()
                .Subscribe(_ => tcs.TrySetResult());

            var registration = ct.Register(() => tcs.TrySetCanceled());

            try
            {
                await tcs.Task;
            }
            finally
            {
                sub.Dispose();
                registration.Dispose();
            }
        }

        public void Dispose()
        {
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
        }
    }
}
