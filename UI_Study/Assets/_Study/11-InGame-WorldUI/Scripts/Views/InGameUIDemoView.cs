using TMPro;
using UnityEngine;

namespace UIStudy.InGameUI.Views
{
    /// <summary>
    /// Screen Space Overlay canvas view — title, instructions, resource counters.
    /// No logic; Presenter updates the counter texts.
    /// </summary>
    public class InGameUIDemoView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _instructionText;

        [Header("Resource Counters")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _woodText;
        [SerializeField] private TextMeshProUGUI _stoneText;
        [SerializeField] private TextMeshProUGUI _foodText;

        [Header("Floating Resource Container")]
        [SerializeField] private RectTransform _floatingContainer;

        public RectTransform FloatingContainer => _floatingContainer;

        public void SetGold(int amount)
        {
            if (_goldText != null)
                _goldText.text = $"Gold: {amount}";
        }

        public void SetWood(int amount)
        {
            if (_woodText != null)
                _woodText.text = $"Wood: {amount}";
        }

        public void SetStone(int amount)
        {
            if (_stoneText != null)
                _stoneText.text = $"Stone: {amount}";
        }

        public void SetFood(int amount)
        {
            if (_foodText != null)
                _foodText.text = $"Food: {amount}";
        }
    }
}
