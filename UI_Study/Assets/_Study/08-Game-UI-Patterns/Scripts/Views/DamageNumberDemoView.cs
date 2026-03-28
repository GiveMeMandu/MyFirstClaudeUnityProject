using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.GameUI.Views
{
    /// <summary>
    /// 데미지 넘버 데모 View — 3개 버튼 + 타겟 이미지 + 풀 컨테이너.
    /// </summary>
    public class DamageNumberDemoView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _dealDamageButton;
        [SerializeField] private Button _criticalHitButton;
        [SerializeField] private Button _healButton;

        [Header("Target")]
        [SerializeField] private RectTransform _targetImage;

        [Header("Pool Container")]
        [SerializeField] private RectTransform _numberContainer;

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI _poolInfoText;

        public Button DealDamageButton => _dealDamageButton;
        public Button CriticalHitButton => _criticalHitButton;
        public Button HealButton => _healButton;
        public RectTransform TargetImage => _targetImage;
        public RectTransform NumberContainer => _numberContainer;

        public void SetPoolInfo(int active, int total)
        {
            if (_poolInfoText != null)
                _poolInfoText.text = $"Pool: {active}/{total}";
        }
    }
}
