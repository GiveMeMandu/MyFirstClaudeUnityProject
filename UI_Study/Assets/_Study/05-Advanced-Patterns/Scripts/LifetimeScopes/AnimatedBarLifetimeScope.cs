using System;
using R3;
using UIStudy.Advanced.Models;
using UIStudy.Advanced.Presenters;
using UIStudy.Advanced.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Advanced.LifetimeScopes
{
    public class AnimatedBarLifetimeScope : LifetimeScope
    {
        [SerializeField] private AnimatedBarView _barView;
        [SerializeField] private Button _damage10Button;
        [SerializeField] private Button _damage30Button;
        [SerializeField] private Button _heal20Button;
        [SerializeField] private TextMeshProUGUI _hpLabel;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(new StatModel(100));
            builder.RegisterComponent(_barView);
            builder.RegisterEntryPoint<AnimatedBarPresenter>();
            builder.RegisterInstance(new AnimatedBarDemoController.Refs
            {
                Damage10 = _damage10Button,
                Damage30 = _damage30Button,
                Heal20 = _heal20Button,
                HpLabel = _hpLabel
            });
            builder.RegisterEntryPoint<AnimatedBarDemoController>();
        }
    }

    public class AnimatedBarDemoController : IInitializable, IDisposable
    {
        public class Refs
        {
            public Button Damage10;
            public Button Damage30;
            public Button Heal20;
            public TextMeshProUGUI HpLabel;
        }

        private readonly StatModel _model;
        private readonly Refs _refs;
        private readonly CompositeDisposable _disposables = new();

        public AnimatedBarDemoController(StatModel model, Refs refs)
        {
            _model = model;
            _refs = refs;
        }

        public void Initialize()
        {
            _refs.Damage10.OnClickAsObservable()
                .Subscribe(_ => _model.TakeDamage(10)).AddTo(_disposables);
            _refs.Damage30.OnClickAsObservable()
                .Subscribe(_ => _model.TakeDamage(30)).AddTo(_disposables);
            _refs.Heal20.OnClickAsObservable()
                .Subscribe(_ => _model.Heal(20)).AddTo(_disposables);

            Observable.CombineLatest(_model.CurrentValue, _model.MaxValue,
                    (c, m) => $"HP: {c} / {m}")
                .Subscribe(text => _refs.HpLabel.text = text)
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
