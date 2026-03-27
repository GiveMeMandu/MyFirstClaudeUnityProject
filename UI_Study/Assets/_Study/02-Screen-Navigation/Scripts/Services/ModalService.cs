using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.Navigation.Modals;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace UIStudy.Navigation.Services
{
    /// <summary>
    /// ModalContainer를 래핑하는 모달 서비스.
    /// UniTask로 확인 모달 결과를 await 가능하게 제공.
    /// </summary>
    public class ModalService
    {
        private readonly ModalContainer _modalContainer;

        public ModalService(ModalContainer modalContainer)
        {
            _modalContainer = modalContainer;
        }

        /// <summary>
        /// 확인 모달을 표시하고 사용자 응답을 await.
        /// </summary>
        public async UniTask<bool> ShowConfirmAsync(string message, CancellationToken ct = default)
        {
            var confirmed = false;
            var tcs = new UniTaskCompletionSource();

            _modalContainer.Push("ConfirmModal", true, onLoad: args =>
            {
                var modal = (ConfirmModalView)args.modal;
                modal.SetMessage(message);

                modal.OnConfirmClick.Subscribe(_ =>
                {
                    confirmed = true;
                    tcs.TrySetResult();
                }).AddTo(modal);

                modal.OnCancelClick.Subscribe(_ =>
                {
                    confirmed = false;
                    tcs.TrySetResult();
                }).AddTo(modal);
            });

            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                confirmed = false;
            }

            _modalContainer.Pop(true);
            return confirmed;
        }

        /// <summary>
        /// 정보 모달을 표시하고 닫기를 await.
        /// </summary>
        public async UniTask ShowInfoAsync(string title, string body, CancellationToken ct = default)
        {
            var tcs = new UniTaskCompletionSource();

            _modalContainer.Push("InfoModal", true, onLoad: args =>
            {
                var modal = (InfoModalView)args.modal;
                modal.SetTitle(title);
                modal.SetBody(body);

                modal.OnCloseClick.Subscribe(_ =>
                {
                    tcs.TrySetResult();
                }).AddTo(modal);
            });

            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                // 취소 시에도 모달 닫기
            }

            _modalContainer.Pop(true);
        }
    }
}
