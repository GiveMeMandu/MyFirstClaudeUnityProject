using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 8: Bootstrapper — C# Event vs R3 프레젠터 전환 데모.
    /// 토글 버튼으로 동일 View에 다른 Presenter를 연결/해제.
    /// </summary>
    public class ComplexityDemoBootstrapper : MonoBehaviour
    {
        [SerializeField] private SearchPanelView _view;

        private List<string> _searchItems;
        private SearchPresenter_CSharpEvent _csharpPresenter;
        private SearchPresenter_R3 _r3Presenter;
        private bool _useR3;

        private void Start()
        {
            _searchItems = GenerateSearchItems(500);

            // Start with C# Event presenter
            _useR3 = false;
            ActivateCSharpPresenter();

            _view.OnToggleModeClicked += HandleToggleMode;
        }

        private void HandleToggleMode()
        {
            // Dispose current presenter
            DisposeCurrent();

            _useR3 = !_useR3;
            _view.ClearSearch();
            _view.ClearLog();

            if (_useR3)
            {
                ActivateR3Presenter();
            }
            else
            {
                ActivateCSharpPresenter();
            }
        }

        private void ActivateCSharpPresenter()
        {
            _csharpPresenter = new SearchPresenter_CSharpEvent(_view, _searchItems);
            _csharpPresenter.Initialize();
            _view.SetModeLabel("Mode: C# Event (Manual Debounce)");
            _view.SetToggleButtonText("Switch to R3");
            Debug.Log("[ComplexityDemo] Switched to C# Event presenter");
        }

        private void ActivateR3Presenter()
        {
            _r3Presenter = new SearchPresenter_R3(_view, _searchItems);
            _r3Presenter.Initialize();
            _view.SetModeLabel("Mode: R3 (Observable Debounce)");
            _view.SetToggleButtonText("Switch to C# Event");
            Debug.Log("[ComplexityDemo] Switched to R3 presenter");
        }

        private void DisposeCurrent()
        {
            _csharpPresenter?.Dispose();
            _csharpPresenter = null;
            _r3Presenter?.Dispose();
            _r3Presenter = null;
        }

        private void OnDestroy()
        {
            _view.OnToggleModeClicked -= HandleToggleMode;
            DisposeCurrent();
        }

        private static List<string> GenerateSearchItems(int count)
        {
            var items = new List<string>(count);
            string[] categories = { "Weapon", "Armor", "Potion", "Scroll", "Gem",
                "Rune", "Material", "Food", "Tool", "Key" };
            string[] adjectives = { "Ancient", "Mystic", "Cursed", "Blessed", "Broken",
                "Enchanted", "Rusty", "Golden", "Shadow", "Crystal" };
            var rng = new System.Random(123);

            for (int i = 0; i < count; i++)
            {
                string adj = adjectives[rng.Next(adjectives.Length)];
                string cat = categories[rng.Next(categories.Length)];
                items.Add($"{adj} {cat} #{i + 1:D3}");
            }

            return items;
        }
    }
}
