using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 1: UI Toolkit 기초 — UIDocument에서 요소 쿼리 + 텍스트 변경.
    /// UXML로 선언한 UI에 C#으로 데이터를 연결하는 최소 패턴.
    /// </summary>
    // NOTE: Step 1은 MVP 도입 전이므로 의도적으로 View에 로직을 포함한다.
    // Step 2부터 Model/Presenter 분리 패턴을 적용한다.
    public class ProfileCardView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        // 캐싱된 UI 요소
        private Label _nameLabel;
        private Label _levelLabel;
        private Label _hpLabel;
        private Label _atkLabel;
        private Label _defLabel;
        private Label _avatarInitial;
        private Label _statusLabel;
        private Button _levelUpBtn;
        private Button _resetBtn;

        // 상태
        private int _level = 1;
        private int _baseHp = 100;
        private int _baseAtk = 25;
        private int _baseDef = 10;

        private void OnEnable()
        {
            // OnEnable에서 rootVisualElement 접근 — Awake에서는 아직 준비 안 됨
            var root = _document.rootVisualElement;

            // Q<T>("name") 쿼리로 요소 캐싱
            _nameLabel     = root.Q<Label>("player-name");
            _levelLabel    = root.Q<Label>("player-level");
            _hpLabel       = root.Q<Label>("stat-hp");
            _atkLabel      = root.Q<Label>("stat-atk");
            _defLabel      = root.Q<Label>("stat-def");
            _avatarInitial = root.Q<Label>("avatar-initial");
            _statusLabel   = root.Q<Label>("status-label");
            _levelUpBtn    = root.Q<Button>("btn-level-up");
            _resetBtn      = root.Q<Button>("btn-reset");

            // named method로 이벤트 등록 (익명 람다 금지 — 해제 불가 방지)
            _levelUpBtn.clicked += OnLevelUpClicked;
            _resetBtn.clicked   += OnResetClicked;

            // 초기 표시
            UpdateDisplay();
        }

        private void OnDisable()
        {
            // OnDisable에서 이벤트 해제 — 메모리 누수 방지
            if (_levelUpBtn != null) _levelUpBtn.clicked -= OnLevelUpClicked;
            if (_resetBtn != null)   _resetBtn.clicked   -= OnResetClicked;
        }

        private void OnLevelUpClicked()
        {
            _level++;
            UpdateDisplay();
            _statusLabel.text = $"Level Up! Now Lv. {_level}";
        }

        private void OnResetClicked()
        {
            _level = 1;
            UpdateDisplay();
            _statusLabel.text = "Reset to Lv. 1";
        }

        private void UpdateDisplay()
        {
            _nameLabel.text     = "Hero";
            _levelLabel.text    = $"Lv. {_level}";
            _avatarInitial.text = "H";

            // 레벨에 비례하여 스탯 증가
            _hpLabel.text  = $"{_baseHp + (_level - 1) * 20}";
            _atkLabel.text = $"{_baseAtk + (_level - 1) * 5}";
            _defLabel.text = $"{_baseDef + (_level - 1) * 3}";
        }
    }
}
