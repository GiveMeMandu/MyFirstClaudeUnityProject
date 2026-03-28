using System;
using R3;
using UIStudy.R3Advanced.Models;
using UIStudy.R3Advanced.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.R3Advanced.Presenters
{
    /// <summary>
    /// 캐릭터 Presenter — Two-Way Binding 패턴 데모.
    /// InputField/Slider ↔ Model을 양방향 바인딩하고,
    /// CombineLatest로 미리보기 텍스트를 파생한다.
    /// Skip(1) + DistinctUntilChanged로 피드백 루프를 방지한다.
    /// </summary>
    public class CharacterPresenter : IInitializable, IDisposable
    {
        private readonly CharacterModel _model;
        private readonly CharacterView _view;
        private readonly CompositeDisposable _disposables = new();

        public CharacterPresenter(CharacterModel model, CharacterView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // === Two-Way Binding: Name ===
            // Model → View (초기값 포함)
            _model.Name
                .Subscribe(name => _view.SetNameWithoutNotify(name))
                .AddTo(_disposables);

            // View → Model (Skip(1)로 초기 emit 무시, DistinctUntilChanged로 중복 방지)
            _view.OnNameChanged
                .Skip(1)
                .DistinctUntilChanged()
                .Subscribe(name => _model.Name.Value = name)
                .AddTo(_disposables);

            // === Two-Way Binding: Health Slider ===
            // Model → View
            _model.Health
                .Subscribe(hp =>
                {
                    _view.SetHealthSliderWithoutNotify(hp);
                    _view.SetHealthLabel($"HP: {hp:F0}");
                })
                .AddTo(_disposables);

            // View → Model
            _view.OnHealthSliderChanged
                .Skip(1)
                .DistinctUntilChanged()
                .Subscribe(hp => _model.Health.Value = hp)
                .AddTo(_disposables);

            // === Two-Way Binding: Attack Slider ===
            // Model → View
            _model.Attack
                .Subscribe(atk =>
                {
                    _view.SetAttackSliderWithoutNotify(atk);
                    _view.SetAttackLabel($"ATK: {atk:F0}");
                })
                .AddTo(_disposables);

            // View → Model
            _view.OnAttackSliderChanged
                .Skip(1)
                .DistinctUntilChanged()
                .Subscribe(atk => _model.Attack.Value = atk)
                .AddTo(_disposables);

            // === Derived Preview Text ===
            // CombineLatest로 세 프로퍼티를 결합하여 미리보기 텍스트 생성
            Observable.CombineLatest(
                    _model.Name,
                    _model.Health,
                    _model.Attack,
                    (name, hp, atk) => $"{name}: Lv1 (HP: {hp:F0}, ATK: {atk:F0})")
                .Subscribe(preview => _view.SetPreviewText(preview))
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
