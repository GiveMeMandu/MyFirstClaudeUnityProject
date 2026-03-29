using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: 자원 HUD View — 상단 바에 4종 자원 표시.
    /// 값 변경 시 USS class 토글로 플래시 효과.
    /// </summary>
    public class ResourceHudView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private Label _goldLabel;
        private Label _woodLabel;
        private Label _foodLabel;
        private Label _popLabel;

        private const string FlashClass = "hud-value--flash";
        private const long FlashDurationMs = 400;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _goldLabel = root.Q<Label>("hud-gold-value");
            _woodLabel = root.Q<Label>("hud-wood-value");
            _foodLabel = root.Q<Label>("hud-food-value");
            _popLabel  = root.Q<Label>("hud-pop-value");
        }

        public void SetGold(int value) => SetValueWithFlash(_goldLabel, value);
        public void SetWood(int value) => SetValueWithFlash(_woodLabel, value);
        public void SetFood(int value) => SetValueWithFlash(_foodLabel, value);
        public void SetPop(int value)  => SetValueWithFlash(_popLabel, value);

        /// <summary>
        /// 초기값 설정 — 플래시 없이 텍스트만 갱신.
        /// </summary>
        public void SetInitialValues(int gold, int wood, int food, int pop)
        {
            _goldLabel.text = gold.ToString();
            _woodLabel.text = wood.ToString();
            _foodLabel.text = food.ToString();
            _popLabel.text  = pop.ToString();
        }

        private void SetValueWithFlash(Label label, int value)
        {
            label.text = value.ToString();

            // USS class 토글로 CSS transition 트리거
            label.AddToClassList(FlashClass);
            label.schedule.Execute(() => label.RemoveFromClassList(FlashClass))
                .StartingIn(FlashDurationMs);
        }
    }
}
