using System;
using R3;
using UIStudy.AdvancedHUD.Models;
using UIStudy.AdvancedHUD.Views;
using VContainer.Unity;

namespace UIStudy.AdvancedHUD.Presenters
{
    /// <summary>
    /// Radial Menu Presenter — 버튼 클릭 -> 모델 갱신, 모델 변경 -> 뷰 갱신.
    /// </summary>
    public class RadialMenuPresenter : IInitializable, IDisposable
    {
        private readonly RadialMenuModel _model;
        private readonly RadialMenuView _view;
        private readonly CompositeDisposable _disposables = new();

        public RadialMenuPresenter(RadialMenuModel model, RadialMenuView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // 메뉴 라벨 설정
            for (int i = 0; i < RadialMenuModel.MenuItems.Length; i++)
            {
                _view.SetLabel(i, RadialMenuModel.MenuItems[i]);
            }

            // 선택 인덱스 변경 구독 -> 중앙 텍스트 + 하이라이트 갱신
            _model.SelectedIndex
                .Subscribe(index =>
                {
                    if (index >= 0 && index < RadialMenuModel.MenuItems.Length)
                    {
                        _view.SetCenterText(RadialMenuModel.MenuItems[index]);
                    }
                    else
                    {
                        _view.SetCenterText("Select");
                    }
                    _view.SetHighlight(index);
                })
                .AddTo(_disposables);

            // 각 메뉴 버튼 클릭 바인딩
            var buttons = _view.MenuButtons;
            for (int i = 0; i < buttons.Length; i++)
            {
                int capturedIndex = i;
                buttons[i].OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        // 같은 항목 재클릭 시 선택 해제
                        if (_model.SelectedIndex.Value == capturedIndex)
                            _model.SelectedIndex.Value = -1;
                        else
                            _model.SelectedIndex.Value = capturedIndex;
                    })
                    .AddTo(_disposables);
            }

            // 토글 버튼 바인딩
            _view.ToggleButton.OnClickAsObservable()
                .Subscribe(_ => _view.ToggleMenu())
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
