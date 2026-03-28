using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.R3Advanced.Models;
using UIStudy.R3Advanced.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.R3Advanced.Presenters
{
    /// <summary>
    /// 구매 Presenter — ReactiveCommand 패턴 데모.
    /// CombineLatest로 canBuy를 파생하고, SubscribeAwait(Drop)로 더블클릭을 방지한다.
    /// </summary>
    public class PurchasePresenter : IInitializable, IDisposable
    {
        private readonly PurchaseModel _model;
        private readonly PurchaseView _view;
        private readonly CompositeDisposable _disposables = new();

        public PurchasePresenter(PurchaseModel model, PurchaseView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // Gold 표시 바인딩
            _model.Gold
                .Subscribe(g => _view.SetGoldText($"Gold: {g}"))
                .AddTo(_disposables);

            // Price 표시 바인딩
            _model.ItemPrice
                .Subscribe(p => _view.SetPriceText($"Price: {p}"))
                .AddTo(_disposables);

            // CanBuy → 버튼 interactable + ReactiveCommand
            var canBuyCommand = _model.CanBuy.ToReactiveCommand();
            canBuyCommand.AddTo(_disposables);

            _model.CanBuy
                .Subscribe(can => _view.SetBuyInteractable(can))
                .AddTo(_disposables);

            // Buy 버튼 — SubscribeAwait + AwaitOperation.Drop 으로 더블클릭 방지
            _view.OnBuyClick
                .SubscribeAwait(async (_, ct) =>
                {
                    await ProcessPurchaseAsync(ct);
                }, AwaitOperation.Drop, configureAwait: false)
                .AddTo(_disposables);

            // +10 Gold 버튼
            _view.OnAddGoldClick
                .Subscribe(_ => _model.AddGold(10))
                .AddTo(_disposables);
        }

        private async UniTask ProcessPurchaseAsync(CancellationToken ct)
        {
            if (!_model.TryBuy())
            {
                _view.SetStatusText("Not enough gold!");
                return;
            }

            _view.SetStatusText("Purchasing...");

            // 구매 처리 시뮬레이션 (0.5초 대기)
            await UniTask.Delay(500, cancellationToken: ct);

            _view.SetStatusText("Purchased!");
            Debug.Log("[PurchasePresenter] Purchase completed.");
        }

        public void Dispose() => _disposables.Dispose();
    }
}
