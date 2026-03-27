using System;
using R3;
using UIStudy.Advanced.Models;
using UIStudy.Advanced.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Advanced.Presenters
{
    /// <summary>
    /// 체력바 Presenter — Model의 ReactiveProperty 변화를 AnimatedBarView에 전달.
    /// </summary>
    public class AnimatedBarPresenter : IInitializable, IDisposable
    {
        private readonly StatModel _model;
        private readonly AnimatedBarView _view;
        private readonly CompositeDisposable _disposables = new();

        public AnimatedBarPresenter(StatModel model, AnimatedBarView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // 현재값/최대값이 변할 때마다 정규화된 값으로 바 업데이트
            Observable.CombineLatest(
                    _model.CurrentValue,
                    _model.MaxValue,
                    (current, max) => max > 0 ? (float)current / max : 0f)
                .Subscribe(normalized =>
                {
                    _view.SetValue(normalized);

                    // 잔량에 따라 색상 변경
                    _view.SetForegroundColor(normalized switch
                    {
                        <= 0.2f => Color.red,
                        <= 0.5f => Color.yellow,
                        _ => Color.green
                    });
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
