using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 2: View — UIDocument 보유 MonoBehaviour.
    /// OnEnable에서 요소 캐싱, C# event로 입력 노출, display 메서드만 보유.
    /// 비즈니스 로직 없음.
    /// </summary>
    public class ResourcePanelView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        // 캐싱 요소
        private Label _goldLabel;
        private Label _woodLabel;
        private Label _foodLabel;
        private Label _statusLabel;
        private Button _gainBtn;
        private Button _spendBtn;

        // View가 외부(Presenter)에 노출하는 이벤트
        public event Action OnGainClicked;
        public event Action OnSpendClicked;

        // named methods for proper unsubscription
        private void HandleGainClicked() => OnGainClicked?.Invoke();
        private void HandleSpendClicked() => OnSpendClicked?.Invoke();

        private void OnEnable()
        {
            var root = _document.rootVisualElement;

            _goldLabel   = root.Q<Label>("gold-label");
            _woodLabel   = root.Q<Label>("wood-label");
            _foodLabel   = root.Q<Label>("food-label");
            _statusLabel = root.Q<Label>("status-label");
            _gainBtn     = root.Q<Button>("btn-gain");
            _spendBtn    = root.Q<Button>("btn-spend");

            _gainBtn.clicked  += HandleGainClicked;
            _spendBtn.clicked += HandleSpendClicked;
        }

        private void OnDisable()
        {
            if (_gainBtn != null)  _gainBtn.clicked  -= HandleGainClicked;
            if (_spendBtn != null) _spendBtn.clicked -= HandleSpendClicked;
        }

        // Display methods — View의 유일한 역할
        public void SetGold(int value) => _goldLabel.text = value.ToString();
        public void SetWood(int value) => _woodLabel.text = value.ToString();
        public void SetFood(int value) => _foodLabel.text = value.ToString();
        public void SetStatus(string msg) => _statusLabel.text = msg;
    }
}
