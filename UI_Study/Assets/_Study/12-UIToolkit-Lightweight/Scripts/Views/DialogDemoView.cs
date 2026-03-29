using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 3: 데모 트리거 View — 다이얼로그/로딩 시작 버튼 + 결과 표시.
    /// </summary>
    public class DialogDemoView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private Button _triggerBtn;
        private Button _loadingBtn;
        private Label _resultLabel;

        public event Action OnTriggerDialogClicked;
        public event Action OnTriggerLoadingClicked;

        private void HandleTriggerDialog() => OnTriggerDialogClicked?.Invoke();
        private void HandleTriggerLoading() => OnTriggerLoadingClicked?.Invoke();

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _triggerBtn  = root.Q<Button>("btn-trigger");
            _loadingBtn  = root.Q<Button>("btn-loading");
            _resultLabel = root.Q<Label>("result-label");

            _triggerBtn.clicked += HandleTriggerDialog;
            _loadingBtn.clicked += HandleTriggerLoading;
        }

        private void OnDisable()
        {
            if (_triggerBtn != null) _triggerBtn.clicked -= HandleTriggerDialog;
            if (_loadingBtn != null) _loadingBtn.clicked -= HandleTriggerLoading;
        }

        public void SetResult(string text) => _resultLabel.text = text;
    }
}
