using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Defense.Bridge;

namespace ProjectSun.V2.Defense
{
    /// <summary>
    /// 밤 페이즈 전투 HUD Presenter.
    /// BattleUIBridge 폴링 데이터를 UI Toolkit 요소에 바인딩.
    /// TimeScaleController 배속/일시정지 버튼 연동.
    /// SF-WD-013.
    /// </summary>
    public class BattleHUDPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UIDocument uiDocument;
        [SerializeField] BattleUIBridge battleUIBridge;
        [SerializeField] TimeScaleController timeScaleController;
        [SerializeField] GameOverManager gameOverManager;

        // UI 요소 캐시
        VisualElement _root;

        // Top bar
        Label _waveLabel;
        VisualElement _waveProgressFill;
        Label _enemyCountLabel;

        // Squad panel
        Label _squadCountLabel;
        VisualElement _squadHPFill;
        Label _squadHPLabel;

        // Building panel
        VisualElement _hqHPFill;
        Label _hqHPLabel;
        VisualElement _buildingHPFill;
        Label _buildingHPLabel;

        // Speed controls
        Button _btn1x;
        Button _btn2x;
        Button _btnPause;

        // Game over
        VisualElement _gameOverOverlay;
        Label _gameOverSub;

        bool _isActive;

        void OnEnable()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            SetupSpeedButtons();

            if (timeScaleController != null)
                timeScaleController.OnSpeedChanged += OnSpeedChanged;

            if (gameOverManager != null)
                gameOverManager.OnGameOver += OnGameOver;
        }

        void OnDisable()
        {
            if (timeScaleController != null)
                timeScaleController.OnSpeedChanged -= OnSpeedChanged;

            if (gameOverManager != null)
                gameOverManager.OnGameOver -= OnGameOver;
        }

        /// <summary>HUD 표시. 밤 페이즈 진입 시 호출.</summary>
        public void Show()
        {
            _isActive = true;
            if (_root != null)
                _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>HUD 숨김. 낮 페이즈 진입 시 호출.</summary>
        public void Hide()
        {
            _isActive = false;
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (!_isActive) return;
            if (battleUIBridge == null) return;

            UpdateWaveInfo();
            UpdateSquadInfo();
            UpdateBuildingInfo();
        }

        void CacheElements()
        {
            // Top bar
            _waveLabel = _root.Q<Label>("wave-label");
            _waveProgressFill = _root.Q("wave-progress-fill");
            _enemyCountLabel = _root.Q<Label>("enemy-count-label");

            // Squad panel
            _squadCountLabel = _root.Q<Label>("squad-count-label");
            _squadHPFill = _root.Q("squad-hp-fill");
            _squadHPLabel = _root.Q<Label>("squad-hp-label");

            // Building panel
            _hqHPFill = _root.Q("hq-hp-fill");
            _hqHPLabel = _root.Q<Label>("hq-hp-label");
            _buildingHPFill = _root.Q("building-hp-fill");
            _buildingHPLabel = _root.Q<Label>("building-hp-label");

            // Speed controls
            _btn1x = _root.Q<Button>("btn-speed-1x");
            _btn2x = _root.Q<Button>("btn-speed-2x");
            _btnPause = _root.Q<Button>("btn-pause");

            // Game over
            _gameOverOverlay = _root.Q("game-over-overlay");
            _gameOverSub = _root.Q<Label>("game-over-sub");
        }

        void SetupSpeedButtons()
        {
            _btn1x?.RegisterCallback<ClickEvent>(_ =>
                timeScaleController?.SetSpeed(TimeScaleController.TimeSpeed.Normal));

            _btn2x?.RegisterCallback<ClickEvent>(_ =>
                timeScaleController?.SetSpeed(TimeScaleController.TimeSpeed.Fast));

            _btnPause?.RegisterCallback<ClickEvent>(_ =>
                timeScaleController?.TogglePause());
        }

        void UpdateWaveInfo()
        {
            float progress = battleUIBridge.WaveProgress;
            int killed = battleUIBridge.TotalEnemiesKilled;
            int spawned = battleUIBridge.TotalEnemiesSpawned;
            int alive = battleUIBridge.AliveEnemyCount;

            if (_waveLabel != null)
                _waveLabel.text = $"WAVE {Mathf.CeilToInt(progress * 10f)}/10";

            if (_waveProgressFill != null)
                _waveProgressFill.style.width = Length.Percent(progress * 100f);

            if (_enemyCountLabel != null)
                _enemyCountLabel.text = $"{alive} alive ({killed}/{spawned} killed)";
        }

        void UpdateSquadInfo()
        {
            int count = battleUIBridge.SquadCount;
            float hp = battleUIBridge.TotalSquadHP;
            float maxHP = battleUIBridge.TotalSquadMaxHP;

            if (_squadCountLabel != null)
                _squadCountLabel.text = $"{count} squads";

            float ratio = maxHP > 0f ? hp / maxHP : 0f;
            if (_squadHPFill != null)
                _squadHPFill.style.width = Length.Percent(ratio * 100f);

            if (_squadHPLabel != null)
                _squadHPLabel.text = $"{hp:F0} / {maxHP:F0}";
        }

        void UpdateBuildingInfo()
        {
            // HQ HP
            float hqHP = battleUIBridge.HeadquartersHP;
            float hqMaxHP = battleUIBridge.HeadquartersMaxHP;

            float hqRatio = hqMaxHP > 0f ? hqHP / hqMaxHP : 0f;
            if (_hqHPFill != null)
                _hqHPFill.style.width = Length.Percent(hqRatio * 100f);

            if (_hqHPLabel != null)
                _hqHPLabel.text = $"{hqHP:F0} / {hqMaxHP:F0}";

            // Total building HP
            float totalHP = battleUIBridge.TotalBuildingHP;
            float totalMaxHP = battleUIBridge.TotalBuildingMaxHP;

            float buildingRatio = totalMaxHP > 0f ? totalHP / totalMaxHP : 0f;
            if (_buildingHPFill != null)
                _buildingHPFill.style.width = Length.Percent(buildingRatio * 100f);

            if (_buildingHPLabel != null)
                _buildingHPLabel.text = $"{totalHP:F0} / {totalMaxHP:F0}";
        }

        void OnSpeedChanged(TimeScaleController.TimeSpeed speed)
        {
            // Active 클래스 토글
            _btn1x?.RemoveFromClassList("speed-btn--active");
            _btn2x?.RemoveFromClassList("speed-btn--active");
            _btnPause?.RemoveFromClassList("speed-btn--active");

            switch (speed)
            {
                case TimeScaleController.TimeSpeed.Normal:
                    _btn1x?.AddToClassList("speed-btn--active");
                    break;
                case TimeScaleController.TimeSpeed.Fast:
                    _btn2x?.AddToClassList("speed-btn--active");
                    break;
                case TimeScaleController.TimeSpeed.Paused:
                    _btnPause?.AddToClassList("speed-btn--active");
                    break;
            }
        }

        void OnGameOver(GameOverManager.GameOverReason reason)
        {
            if (_gameOverOverlay != null)
                _gameOverOverlay.style.display = DisplayStyle.Flex;

            if (_gameOverSub != null)
            {
                _gameOverSub.text = reason switch
                {
                    GameOverManager.GameOverReason.HeadquartersDestroyed => "Headquarters destroyed",
                    GameOverManager.GameOverReason.Victory => "Victory!",
                    _ => ""
                };
            }
        }
    }
}
