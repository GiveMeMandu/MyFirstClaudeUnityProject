using System;
using R3;
using UnityEngine;

namespace UIStudy.Theme.Services
{
    public enum ThemeType
    {
        Light,
        Dark,
        ColorBlind
    }

    /// <summary>
    /// 테마 관리 서비스 — 현재 테마를 ReactiveProperty로 관리.
    /// uPalette 연동 시 팔레트 전환 로직을 여기에 추가.
    /// </summary>
    public class ThemeService : IDisposable
    {
        public ReactiveProperty<ThemeType> CurrentTheme { get; } = new(ThemeType.Light);

        /// <summary>
        /// 테마별 색상 정의 (uPalette 미사용 시 폴백).
        /// 실제 프로젝트에서는 uPalette의 Palette 에셋으로 교체.
        /// </summary>
        public Color GetPrimaryColor() => CurrentTheme.Value switch
        {
            ThemeType.Light => new Color(0.13f, 0.59f, 0.95f),   // Blue
            ThemeType.Dark => new Color(0.56f, 0.79f, 1f),       // Light Blue
            ThemeType.ColorBlind => new Color(0.0f, 0.45f, 0.7f), // Accessible Blue
            _ => Color.white
        };

        public Color GetBackgroundColor() => CurrentTheme.Value switch
        {
            ThemeType.Light => new Color(0.95f, 0.95f, 0.95f),
            ThemeType.Dark => new Color(0.12f, 0.12f, 0.12f),
            ThemeType.ColorBlind => new Color(0.95f, 0.95f, 0.95f),
            _ => Color.white
        };

        public Color GetTextColor() => CurrentTheme.Value switch
        {
            ThemeType.Light => new Color(0.13f, 0.13f, 0.13f),
            ThemeType.Dark => new Color(0.93f, 0.93f, 0.93f),
            ThemeType.ColorBlind => new Color(0.13f, 0.13f, 0.13f),
            _ => Color.black
        };

        public Color GetAccentColor() => CurrentTheme.Value switch
        {
            ThemeType.Light => new Color(1f, 0.6f, 0f),           // Orange
            ThemeType.Dark => new Color(1f, 0.76f, 0.03f),        // Amber
            ThemeType.ColorBlind => new Color(0.9f, 0.6f, 0f),    // Accessible Orange
            _ => Color.yellow
        };

        public void SetTheme(ThemeType theme)
        {
            CurrentTheme.Value = theme;
            Debug.Log($"[ThemeService] Theme changed to: {theme}");
        }

        public void CycleTheme()
        {
            var next = (ThemeType)(((int)CurrentTheme.Value + 1) % 3);
            SetTheme(next);
        }

        public void Dispose() => CurrentTheme.Dispose();
    }
}
