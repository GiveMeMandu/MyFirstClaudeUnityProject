using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.R3Advanced.Views
{
    /// <summary>
    /// 캐릭터 Two-Way Binding View — 이름 InputField, 체력/공격력 Slider + 라벨, 미리보기 텍스트.
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _attackSlider;
        [SerializeField] private TextMeshProUGUI _healthLabel;
        [SerializeField] private TextMeshProUGUI _attackLabel;
        [SerializeField] private TextMeshProUGUI _previewText;

        // --- Observable accessors for UI events ---

        public Observable<string> OnNameChanged =>
            _nameInput.onValueChanged.AsObservable();

        public Observable<float> OnHealthSliderChanged =>
            _healthSlider.onValueChanged.AsObservable();

        public Observable<float> OnAttackSliderChanged =>
            _attackSlider.onValueChanged.AsObservable();

        // --- Setters (Presenter -> View) ---

        public void SetNameWithoutNotify(string name)
        {
            _nameInput.SetTextWithoutNotify(name);
        }

        public void SetHealthSliderWithoutNotify(float value)
        {
            _healthSlider.SetValueWithoutNotify(value);
        }

        public void SetAttackSliderWithoutNotify(float value)
        {
            _attackSlider.SetValueWithoutNotify(value);
        }

        public void SetHealthLabel(string text) => _healthLabel.text = text;
        public void SetAttackLabel(string text) => _attackLabel.text = text;
        public void SetPreviewText(string text) => _previewText.text = text;
    }
}
