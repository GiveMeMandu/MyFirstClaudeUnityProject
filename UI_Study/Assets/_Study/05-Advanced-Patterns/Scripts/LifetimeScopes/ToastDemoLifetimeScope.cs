using System;
using R3;
using UIStudy.Advanced.Models;
using UIStudy.Advanced.Services;
using UIStudy.Advanced.Views;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Advanced.LifetimeScopes
{
    public class ToastDemoLifetimeScope : LifetimeScope
    {
        [SerializeField] private ToastView _toastView;
        [SerializeField] private Button _successButton;
        [SerializeField] private Button _warningButton;
        [SerializeField] private Button _errorButton;
        [SerializeField] private Button _infoButton;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_toastView);
            builder.Register<ToastService>(Lifetime.Singleton);
            builder.RegisterInstance(new ToastDemoController.Refs
            {
                SuccessBtn = _successButton,
                WarningBtn = _warningButton,
                ErrorBtn = _errorButton,
                InfoBtn = _infoButton
            });
            builder.RegisterEntryPoint<ToastDemoController>();
        }
    }

    public class ToastDemoController : IInitializable, IDisposable
    {
        public class Refs
        {
            public Button SuccessBtn;
            public Button WarningBtn;
            public Button ErrorBtn;
            public Button InfoBtn;
        }

        private readonly ToastService _toastService;
        private readonly Refs _refs;
        private readonly CompositeDisposable _disposables = new();

        public ToastDemoController(ToastService toastService, Refs refs)
        {
            _toastService = toastService;
            _refs = refs;
        }

        public void Initialize()
        {
            _refs.SuccessBtn.OnClickAsObservable()
                .Subscribe(_ => _toastService.Enqueue("건설 완료!", ToastType.Success))
                .AddTo(_disposables);
            _refs.WarningBtn.OnClickAsObservable()
                .Subscribe(_ => _toastService.Enqueue("자원이 부족합니다!", ToastType.Warning))
                .AddTo(_disposables);
            _refs.ErrorBtn.OnClickAsObservable()
                .Subscribe(_ => _toastService.Enqueue("적 침입 감지!", ToastType.Error))
                .AddTo(_disposables);
            _refs.InfoBtn.OnClickAsObservable()
                .Subscribe(_ => _toastService.Enqueue("턴 3 시작", ToastType.Info))
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
