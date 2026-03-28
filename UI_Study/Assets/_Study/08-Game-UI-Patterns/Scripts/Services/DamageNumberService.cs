using System;
using System.Collections.Generic;
using UIStudy.GameUI.Models;
using UIStudy.GameUI.Views;
using UnityEngine;

namespace UIStudy.GameUI.Services
{
    /// <summary>
    /// 데미지 넘버 오브젝트 풀 서비스 — 간단한 List 기반 풀.
    /// </summary>
    public class DamageNumberService : IDisposable
    {
        private readonly DamageNumberView _prefab;
        private readonly RectTransform _container;
        private readonly List<DamageNumberView> _pool = new();
        private int _activeCount;

        private const int InitialPoolSize = 8;

        public int ActiveCount => _activeCount;
        public int TotalCount => _pool.Count;

        public DamageNumberService(DamageNumberView prefab, RectTransform container)
        {
            _prefab = prefab;
            _container = container;
            Prewarm();
        }

        private void Prewarm()
        {
            for (int i = 0; i < InitialPoolSize; i++)
            {
                var view = CreateInstance();
                view.ResetView();
            }
        }

        private DamageNumberView CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _container);
            instance.gameObject.SetActive(false);
            _pool.Add(instance);
            return instance;
        }

        /// <summary>
        /// 풀에서 비활성 인스턴스를 꺼내 반환. 없으면 새로 생성.
        /// </summary>
        private DamageNumberView Get()
        {
            foreach (var view in _pool)
            {
                if (!view.gameObject.activeSelf)
                    return view;
            }
            return CreateInstance();
        }

        /// <summary>
        /// 데미지 넘버 스폰 — 지정 위치에 표시 후 애니메이션 완료 시 풀에 반환.
        /// </summary>
        public void SpawnDamage(Vector2 anchoredPosition, int damage, DamageType type)
        {
            var view = Get();
            var data = new DamageModel(damage, type);
            _activeCount++;

            view.Animate(data, anchoredPosition, () =>
            {
                view.ResetView();
                _activeCount--;
            });
        }

        public void Dispose()
        {
            foreach (var view in _pool)
            {
                if (view != null && view.gameObject != null)
                    UnityEngine.Object.Destroy(view.gameObject);
            }
            _pool.Clear();
        }
    }
}
