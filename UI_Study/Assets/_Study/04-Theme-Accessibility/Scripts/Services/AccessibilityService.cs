using System;
using R3;
using UnityEngine;

namespace UIStudy.Theme.Services
{
    /// <summary>
    /// 접근성 서비스 — 폰트 스케일링과 색약 모드를 관리.
    /// ReactiveProperty로 설정 변화를 UI에 전파.
    /// </summary>
    public class AccessibilityService : IDisposable
    {
        public ReactiveProperty<float> FontScale { get; } = new(1.0f);
        public ReactiveProperty<bool> IsColorBlindMode { get; } = new(false);

        public const float MinFontScale = 0.75f;
        public const float MaxFontScale = 2.0f;

        public void SetFontScale(float scale)
        {
            FontScale.Value = Mathf.Clamp(scale, MinFontScale, MaxFontScale);
            Debug.Log($"[Accessibility] Font scale: {FontScale.Value:F2}");
        }

        public void ToggleColorBlindMode()
        {
            IsColorBlindMode.Value = !IsColorBlindMode.Value;
            Debug.Log($"[Accessibility] Color blind mode: {IsColorBlindMode.Value}");
        }

        public void Dispose()
        {
            FontScale.Dispose();
            IsColorBlindMode.Dispose();
        }
    }
}
