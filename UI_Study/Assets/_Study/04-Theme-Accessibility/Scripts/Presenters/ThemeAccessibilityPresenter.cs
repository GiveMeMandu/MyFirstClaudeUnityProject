using System;
using R3;
using UIStudy.Theme.Services;
using UIStudy.Theme.Views;
using VContainer.Unity;

namespace UIStudy.Theme.Presenters
{
    /// <summary>
    /// 테마 + 접근성 Presenter — Pure C#.
    /// ThemeService와 AccessibilityService를 View에 바인딩.
    /// </summary>
    public class ThemeAccessibilityPresenter : IInitializable, IDisposable
    {
        private readonly ThemeDemoView _view;
        private readonly ThemeService _themeService;
        private readonly AccessibilityService _accessibilityService;
        private readonly CompositeDisposable _disposables = new();

        public ThemeAccessibilityPresenter(
            ThemeDemoView view,
            ThemeService themeService,
            AccessibilityService accessibilityService)
        {
            _view = view;
            _themeService = themeService;
            _accessibilityService = accessibilityService;
        }

        public void Initialize()
        {
            // 테마 변화 → UI 업데이트
            _themeService.CurrentTheme
                .Subscribe(theme =>
                {
                    _view.SetThemeLabel($"Theme: {theme}");
                    _view.SetBackgroundColor(_themeService.GetBackgroundColor());
                    _view.SetPrimaryTextColor(_themeService.GetTextColor());
                    _view.SetAccentColor(_themeService.GetAccentColor());
                    _view.SetPreviewTitle("Preview Title");
                    _view.SetPreviewBody("이 패널은 현재 테마를 반영합니다.\n색상이 변경되는 것을 확인하세요.");
                })
                .AddTo(_disposables);

            // 테마 전환 버튼
            _view.OnCycleThemeClick
                .Subscribe(_ =>
                {
                    if (_accessibilityService.IsColorBlindMode.Value)
                        _themeService.SetTheme(ThemeType.ColorBlind);
                    else
                        _themeService.CycleTheme();
                })
                .AddTo(_disposables);

            // 폰트 스케일 슬라이더
            _view.OnFontScaleChanged
                .Subscribe(scale =>
                {
                    _accessibilityService.SetFontScale(scale);
                })
                .AddTo(_disposables);

            _accessibilityService.FontScale
                .Subscribe(scale =>
                {
                    _view.SetFontScaleLabel($"Font Scale: {scale:F2}x");
                    _view.ApplyFontScale(scale);
                })
                .AddTo(_disposables);

            // 색약 모드 토글
            _view.OnColorBlindToggleClick
                .Subscribe(_ =>
                {
                    _accessibilityService.ToggleColorBlindMode();
                })
                .AddTo(_disposables);

            _accessibilityService.IsColorBlindMode
                .Subscribe(isOn =>
                {
                    _view.SetColorBlindLabel(isOn ? "색약 모드: ON" : "색약 모드: OFF");
                    if (isOn)
                        _themeService.SetTheme(ThemeType.ColorBlind);
                    else
                        _themeService.SetTheme(ThemeType.Light);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
