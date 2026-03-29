using System;
using System.Collections.Generic;
using UIStudy.InGameUI.Models;
using UIStudy.InGameUI.Views;
using UnityEngine;

namespace UIStudy.InGameUI.Services
{
    /// <summary>
    /// Object pool for FloatingResourceView instances.
    /// Converts world positions to canvas-local positions and spawns floating text.
    /// </summary>
    public class FloatingResourceService : IDisposable
    {
        private readonly FloatingResourceView _prefab;
        private readonly RectTransform _container;
        private readonly List<FloatingResourceView> _pool = new();

        private const int PrewarmCount = 10;

        /// <summary>
        /// World-position offset applied before screen projection (lifts text above the building).
        /// </summary>
        private static readonly Vector3 WorldOffset = Vector3.up * 1.5f;

        public FloatingResourceService(FloatingResourceView prefab, RectTransform container)
        {
            _prefab = prefab;
            _container = container;
            Prewarm();
        }

        private void Prewarm()
        {
            for (int i = 0; i < PrewarmCount; i++)
            {
                var view = CreateInstance();
                view.ResetView();
            }
        }

        private FloatingResourceView CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _container);
            instance.gameObject.SetActive(false);
            _pool.Add(instance);
            return instance;
        }

        private FloatingResourceView Get()
        {
            foreach (var view in _pool)
            {
                if (!view.gameObject.activeSelf)
                    return view;
            }
            return CreateInstance();
        }

        /// <summary>
        /// Spawn a floating resource pop-up.
        /// Converts world position -> screen position -> canvas local position (Overlay canvas).
        /// </summary>
        public void Spawn(FloatingResourceData data, Camera camera)
        {
            // World -> Screen
            Vector3 screenPos = camera.WorldToScreenPoint(data.WorldPosition + WorldOffset);

            // If behind camera, skip
            if (screenPos.z < 0f)
                return;

            // Screen -> Canvas local (null camera for Screen Space Overlay)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _container, screenPos, null, out Vector2 localPos);

            // Format text and pick color
            string text = FormatText(data.Type, data.Amount);
            Color color = GetColor(data.Type);

            // Spawn
            var view = Get();
            view.Show(text, color, localPos);
        }

        private static string FormatText(ResourceType type, int amount)
        {
            return type switch
            {
                ResourceType.Gold  => $"+{amount} Gold",
                ResourceType.Wood  => $"+{amount} Wood",
                ResourceType.Stone => $"+{amount} Stone",
                ResourceType.Food  => $"+{amount} Food",
                _                  => $"+{amount}"
            };
        }

        private static Color GetColor(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold  => Color.yellow,
                ResourceType.Wood  => new Color(0.6f, 0.4f, 0.2f),  // brown
                ResourceType.Stone => Color.gray,
                ResourceType.Food  => Color.green,
                _                  => Color.white
            };
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
