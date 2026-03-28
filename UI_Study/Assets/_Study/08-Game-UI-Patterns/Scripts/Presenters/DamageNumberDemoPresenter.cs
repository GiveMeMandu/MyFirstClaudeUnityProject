using System;
using R3;
using UIStudy.GameUI.Models;
using UIStudy.GameUI.Services;
using UIStudy.GameUI.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.GameUI.Presenters
{
    /// <summary>
    /// 데미지 넘버 데모 Presenter — 버튼 -> 랜덤 위치에 데미지 넘버 스폰.
    /// </summary>
    public class DamageNumberDemoPresenter : IInitializable, IDisposable
    {
        private readonly DamageNumberDemoView _demoView;
        private readonly DamageNumberService _damageService;
        private readonly CompositeDisposable _disposables = new();

        private const float SpawnRadius = 60f;

        public DamageNumberDemoPresenter(DamageNumberDemoView demoView, DamageNumberService damageService)
        {
            _demoView = demoView;
            _damageService = damageService;
        }

        public void Initialize()
        {
            // Deal Damage 버튼
            _demoView.DealDamageButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    var damage = UnityEngine.Random.Range(10, 100);
                    var pos = GetRandomPositionNearTarget();
                    _damageService.SpawnDamage(pos, damage, DamageType.Normal);
                    UpdatePoolInfo();
                })
                .AddTo(_disposables);

            // Critical Hit 버튼
            _demoView.CriticalHitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    var damage = UnityEngine.Random.Range(150, 500);
                    var pos = GetRandomPositionNearTarget();
                    _damageService.SpawnDamage(pos, damage, DamageType.Critical);
                    UpdatePoolInfo();
                })
                .AddTo(_disposables);

            // Heal 버튼
            _demoView.HealButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    var heal = UnityEngine.Random.Range(20, 80);
                    var pos = GetRandomPositionNearTarget();
                    _damageService.SpawnDamage(pos, heal, DamageType.Heal);
                    UpdatePoolInfo();
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 타겟 이미지 근처 랜덤 위치 (anchoredPosition).
        /// </summary>
        private Vector2 GetRandomPositionNearTarget()
        {
            var targetPos = _demoView.TargetImage.anchoredPosition;
            var offset = new Vector2(
                UnityEngine.Random.Range(-SpawnRadius, SpawnRadius),
                UnityEngine.Random.Range(-SpawnRadius * 0.5f, SpawnRadius * 0.5f));
            return targetPos + offset;
        }

        private void UpdatePoolInfo()
        {
            _demoView.SetPoolInfo(_damageService.ActiveCount, _damageService.TotalCount);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
