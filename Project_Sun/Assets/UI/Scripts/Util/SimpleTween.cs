using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectSun.UI.Util
{
    /// <summary>
    /// Minimal tween utility for UI Toolkit PoC.
    /// Replaces DOTween dependency for tech spike validation.
    /// When DOTween is installed, replace calls with DOTween.To().
    /// </summary>
    public static class SimpleTween
    {
        private static readonly List<TweenState> _activeTweens = new();
        private static readonly List<int> _completedIds = new();
        private static bool _registered;

        private struct TweenState
        {
            public int Id;
            public float StartValue;
            public float EndValue;
            public float Duration;
            public float Elapsed;
            public EaseType Ease;
            public Action<float> Setter;
            public Action OnComplete;
        }

        public enum EaseType
        {
            Linear,
            OutQuad,
            OutBack,
            InOutQuad,
        }

        private static int _nextId;

        public static int To(
            Func<float> getter,
            Action<float> setter,
            float endValue,
            float duration,
            EaseType ease = EaseType.OutQuad,
            Action onComplete = null)
        {
            EnsureRegistered();

            float startValue = getter();
            int id = ++_nextId;

            _activeTweens.Add(new TweenState
            {
                Id = id,
                StartValue = startValue,
                EndValue = endValue,
                Duration = Mathf.Max(duration, 0.001f),
                Elapsed = 0f,
                Ease = ease,
                Setter = setter,
                OnComplete = onComplete,
            });

            return id;
        }

        public static void Kill(int id)
        {
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                if (_activeTweens[i].Id == id)
                {
                    _activeTweens.RemoveAt(i);
                    break;
                }
            }
        }

        public static void KillAll()
        {
            _activeTweens.Clear();
        }

        private static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;

            // Use a hidden MonoBehaviour to drive updates
            var go = new GameObject("[SimpleTween]");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SimpleTweenUpdater>();
        }

        internal static void UpdateAll()
        {
            float dt = Time.unscaledDeltaTime;
            _completedIds.Clear();

            for (int i = 0; i < _activeTweens.Count; i++)
            {
                var t = _activeTweens[i];
                t.Elapsed += dt;
                float progress = Mathf.Clamp01(t.Elapsed / t.Duration);
                float eased = ApplyEase(progress, t.Ease);
                float value = Mathf.LerpUnclamped(t.StartValue, t.EndValue, eased);

                try { t.Setter(value); } catch { /* element may be gone */ }

                _activeTweens[i] = t;

                if (progress >= 1f)
                    _completedIds.Add(t.Id);
            }

            for (int i = _completedIds.Count - 1; i >= 0; i--)
            {
                int id = _completedIds[i];
                for (int j = _activeTweens.Count - 1; j >= 0; j--)
                {
                    if (_activeTweens[j].Id == id)
                    {
                        var completed = _activeTweens[j];
                        _activeTweens.RemoveAt(j);
                        try { completed.OnComplete?.Invoke(); } catch { /* safe */ }
                        break;
                    }
                }
            }
        }

        private static float ApplyEase(float t, EaseType ease)
        {
            switch (ease)
            {
                case EaseType.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case EaseType.OutBack:
                    float c1 = 1.70158f;
                    float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case EaseType.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                default: // Linear
                    return t;
            }
        }
    }

    internal class SimpleTweenUpdater : MonoBehaviour
    {
        private void Update() => SimpleTween.UpdateAll();
    }
}
