using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UIStudy.MVRP.Models;
using UIStudy.MVRP.Services;
using UIStudy.MVRP.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.MVRP.Presenters
{
    /// <summary>
    /// 자원 HUD + 다이얼로그 Presenter — 자원 소비 시 확인 다이얼로그를 띄운다.
    /// UniTask의 SubscribeAwait로 비동기 처리를 R3 스트림 안에서 수행.
    /// </summary>
    public class ResourceWithDialogPresenter : IInitializable, IDisposable
    {
        private readonly ResourceModel _model;
        private readonly ResourceHUDView _view;
        private readonly DialogService _dialogService;
        private readonly CancellationTokenSource _cts = new();
        private readonly CompositeDisposable _disposables = new();

        private const int GoldPerClick = 10;
        private const int WoodPerClick = 5;
        private const int GoldCost = 25;
        private const int WoodCost = 15;

        public ResourceWithDialogPresenter(
            ResourceModel model,
            ResourceHUDView view,
            DialogService dialogService)
        {
            _model = model;
            _view = view;
            _dialogService = dialogService;
        }

        public void Initialize()
        {
            // Model → View 바인딩
            _model.Gold
                .Subscribe(gold =>
                {
                    _view.SetGold(gold);
                    _view.SetSpendGoldInteractable(gold >= GoldCost);
                })
                .AddTo(_disposables);

            _model.Wood
                .Subscribe(wood =>
                {
                    _view.SetWood(wood);
                    _view.SetSpendWoodInteractable(wood >= WoodCost);
                })
                .AddTo(_disposables);

            _model.Population
                .Subscribe(_view.SetPopulation)
                .AddTo(_disposables);

            // 즉시 실행 버튼 (다이얼로그 불필요)
            _view.OnAddGoldClick
                .Subscribe(_ => _model.AddGold(GoldPerClick))
                .AddTo(_disposables);

            _view.OnAddWoodClick
                .Subscribe(_ => _model.AddWood(WoodPerClick))
                .AddTo(_disposables);

            _view.OnAddPopulationClick
                .Subscribe(_ => _model.AddPopulation(1))
                .AddTo(_disposables);

            // 소비 버튼 — 다이얼로그 확인 후 실행 (SubscribeAwait)
            _view.OnSpendGoldClick
                .SubscribeAwait(async (_, ct) =>
                {
                    var confirmed = await _dialogService.ShowConfirmAsync(
                        $"Gold {GoldCost}을 소비하시겠습니까?", ct);
                    if (confirmed)
                    {
                        _model.TrySpendGold(GoldCost);
                        Debug.Log($"[Dialog] Gold {GoldCost} 소비 완료");
                    }
                }, AwaitOperation.Drop, configureAwait: false)
                .AddTo(_disposables);

            _view.OnSpendWoodClick
                .SubscribeAwait(async (_, ct) =>
                {
                    var confirmed = await _dialogService.ShowConfirmAsync(
                        $"Wood {WoodCost}를 소비하시겠습니까?", ct);
                    if (confirmed)
                    {
                        _model.TrySpendWood(WoodCost);
                        Debug.Log($"[Dialog] Wood {WoodCost} 소비 완료");
                    }
                }, AwaitOperation.Drop, configureAwait: false)
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }
    }
}
