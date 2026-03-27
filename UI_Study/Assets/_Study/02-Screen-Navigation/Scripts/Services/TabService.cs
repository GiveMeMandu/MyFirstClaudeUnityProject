using System;
using System.Collections.Generic;
using R3;
using UIStudy.Navigation.Sheets;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Sheet;
using VContainer.Unity;

namespace UIStudy.Navigation.Services
{
    /// <summary>
    /// SheetContainer + 탭 바 동기화 서비스.
    /// 탭 버튼 클릭 → 해당 시트 Show, 활성 탭 하이라이트.
    /// </summary>
    public class TabService : IInitializable, IDisposable
    {
        private readonly SheetContainer _sheetContainer;
        private readonly Button[] _tabButtons;
        private readonly Color _activeColor = new(0.3f, 0.6f, 1f);
        private readonly Color _inactiveColor = new(0.5f, 0.5f, 0.5f);
        private readonly CompositeDisposable _disposables = new();

        private readonly string[] _sheetKeys = { "WeaponSheet", "ArmorSheet", "ConsumableSheet" };
        private readonly Dictionary<string, string> _sheetIdMap = new();
        private int _activeIndex = -1;

        public TabService(SheetContainer sheetContainer, Button[] tabButtons)
        {
            _sheetContainer = sheetContainer;
            _tabButtons = tabButtons;
        }

        public void Initialize()
        {
            // 각 탭 버튼에 클릭 이벤트 연결
            for (var i = 0; i < _tabButtons.Length && i < _sheetKeys.Length; i++)
            {
                var index = i;
                _tabButtons[i].OnClickAsObservable()
                    .Subscribe(_ => ShowSheet(index))
                    .AddTo(_disposables);
            }

            // 첫 번째 탭 자동 활성화
            ShowSheet(0);
        }

        private void ShowSheet(int index)
        {
            if (index == _activeIndex) return;
            _activeIndex = index;

            var key = _sheetKeys[index];

            // 이미 로드된 시트가 있으면 sheetId로 Show, 없으면 resourceKey로 Show
            if (_sheetIdMap.TryGetValue(key, out var sheetId))
            {
                _sheetContainer.Show(sheetId, true);
            }
            else
            {
                _sheetContainer.ShowByResourceKey(key, true);
                // onLoad 콜백이 없으므로 sheetId를 별도로 추적
                // ShowByResourceKey 내부에서 sheetId = resourceKey로 설정됨
                _sheetIdMap[key] = key;
            }

            UpdateTabHighlight(index);
        }

        private void UpdateTabHighlight(int activeIndex)
        {
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == activeIndex ? _activeColor : _inactiveColor;
            }
        }

        public void Dispose() => _disposables.Dispose();
    }
}
