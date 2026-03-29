using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 8: 검색 패널 View — 검색/빌드 이벤트 노출 + 결과 표시.
    /// C# Event 프레젠터와 R3 프레젠터 모두 동일 View를 공유.
    /// </summary>
    public class SearchPanelView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        // Cached elements
        private TextField _searchField;
        private Label _searchStatus;
        private Label _resultCount;
        private ListView _resultsList;
        private Button _buildBtn;
        private Label _buildStatus;
        private Label _buildCount;
        private Label _modeLabel;
        private Button _toggleModeBtn;
        private Label _logLabel;

        // Results backing list
        private readonly List<string> _results = new();

        // Events
        public event Action<string> OnSearchTextChanged;
        public event Action OnBuildClicked;
        public event Action OnToggleModeClicked;

        // Named handlers
        private void HandleSearchChanged(ChangeEvent<string> evt) => OnSearchTextChanged?.Invoke(evt.newValue);
        private void HandleBuildClicked() => OnBuildClicked?.Invoke();
        private void HandleToggleModeClicked() => OnToggleModeClicked?.Invoke();

        private void OnEnable()
        {
            var root = _document.rootVisualElement;

            _searchField   = root.Q<TextField>("search-field");
            _searchStatus  = root.Q<Label>("search-status");
            _resultCount   = root.Q<Label>("result-count");
            _resultsList   = root.Q<ListView>("results-list");
            _buildBtn      = root.Q<Button>("btn-build");
            _buildStatus   = root.Q<Label>("build-status");
            _buildCount    = root.Q<Label>("build-count");
            _modeLabel     = root.Q<Label>("mode-label");
            _toggleModeBtn = root.Q<Button>("btn-toggle-mode");
            _logLabel      = root.Q<Label>("log-label");

            SetupResultsList();

            _searchField.RegisterValueChangedCallback(HandleSearchChanged);
            _buildBtn.clicked      += HandleBuildClicked;
            _toggleModeBtn.clicked += HandleToggleModeClicked;
        }

        private void OnDisable()
        {
            if (_searchField != null)   _searchField.UnregisterValueChangedCallback(HandleSearchChanged);
            if (_buildBtn != null)      _buildBtn.clicked      -= HandleBuildClicked;
            if (_toggleModeBtn != null) _toggleModeBtn.clicked -= HandleToggleModeClicked;
        }

        private void SetupResultsList()
        {
            _resultsList.makeItem = () => new Label { style = { fontSize = 12, color = new Color(0.86f, 0.86f, 0.94f) } };
            _resultsList.bindItem = (element, index) => ((Label)element).text = _results[index];
            _resultsList.itemsSource = _results;
        }

        // Display methods
        public void SetResults(List<string> results)
        {
            _results.Clear();
            _results.AddRange(results);
            _resultsList.itemsSource = _results;
            _resultsList.RefreshItems();
            _resultCount.text = $"{_results.Count} results";
        }

        public void SetSearchStatus(string text) => _searchStatus.text = text;
        public void SetBuildStatus(string text) => _buildStatus.text = text;
        public void SetBuildCount(int count) => _buildCount.text = $"Build count: {count}";
        public void SetModeLabel(string text) => _modeLabel.text = text;
        public void SetToggleButtonText(string text) => _toggleModeBtn.text = text;

        public void AppendLog(string message)
        {
            string current = _logLabel.text;
            // Keep last 5 lines
            string[] lines = current.Split('\n');
            if (lines.Length > 4)
            {
                current = string.Join("\n", lines, lines.Length - 4, 4);
            }
            _logLabel.text = string.IsNullOrEmpty(current)
                ? message
                : $"{current}\n{message}";
        }

        public void ClearLog() => _logLabel.text = "";

        public void ClearSearch()
        {
            _searchField.SetValueWithoutNotify("");
        }
    }
}
