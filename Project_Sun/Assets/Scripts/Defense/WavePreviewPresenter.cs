using System;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Data;
using ProjectSun.V2.Construction;

namespace ProjectSun.V2.Defense
{
    /// <summary>
    /// 웨이브 미리보기 + 전투 결과 화면 Presenter.
    /// SF-WD-012 (미리보기) + SF-WD-014 (결과).
    /// </summary>
    public class WavePreviewPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UIDocument uiDocument;

        VisualElement _root;

        // Wave Preview
        VisualElement _previewOverlay;
        Label _dirNorthCount, _dirEastCount, _dirSouthCount, _dirWestCount;
        VisualElement _dirNorth, _dirEast, _dirSouth, _dirWest;
        Label _previewTotal, _previewWaves, _previewThreat, _scoutInfo;

        // Battle Result
        VisualElement _resultOverlay;
        Label _resultGrade, _rewardBasic, _rewardAdvanced, _rewardRelic;
        Label _resultKilled, _resultDamage;
        VisualElement _damageList;
        Label _damageListItems;

        /// <summary>결과 화면 닫힘 시 발행. 다음 턴 진행 트리거.</summary>
        public event Action OnResultClosed;

        void OnEnable()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            SetupButtons();
        }

        void CacheElements()
        {
            // Preview
            _previewOverlay = _root.Q("wave-preview-overlay");
            _dirNorth = _root.Q("dir-north");
            _dirEast = _root.Q("dir-east");
            _dirSouth = _root.Q("dir-south");
            _dirWest = _root.Q("dir-west");
            _dirNorthCount = _root.Q<Label>("dir-north-count");
            _dirEastCount = _root.Q<Label>("dir-east-count");
            _dirSouthCount = _root.Q<Label>("dir-south-count");
            _dirWestCount = _root.Q<Label>("dir-west-count");
            _previewTotal = _root.Q<Label>("preview-total");
            _previewWaves = _root.Q<Label>("preview-waves");
            _previewThreat = _root.Q<Label>("preview-threat");
            _scoutInfo = _root.Q<Label>("scout-info");

            // Result
            _resultOverlay = _root.Q("battle-result-overlay");
            _resultGrade = _root.Q<Label>("result-grade");
            _rewardBasic = _root.Q<Label>("reward-basic");
            _rewardAdvanced = _root.Q<Label>("reward-advanced");
            _rewardRelic = _root.Q<Label>("reward-relic");
            _resultKilled = _root.Q<Label>("result-killed");
            _resultDamage = _root.Q<Label>("result-damage");
            _damageList = _root.Q("damage-list");
            _damageListItems = _root.Q<Label>("damage-list-items");
        }

        void SetupButtons()
        {
            _root.Q<Button>("btn-close-preview")?.RegisterCallback<ClickEvent>(_ => HidePreview());
            _root.Q<Button>("btn-close-result")?.RegisterCallback<ClickEvent>(_ =>
            {
                HideResult();
                OnResultClosed?.Invoke();
            });
        }

        /// <summary>
        /// 웨이브 미리보기 표시.
        /// SF-WD-012. 정찰 수준에 따라 상세도 변경.
        /// </summary>
        public void ShowPreview(int turn, int totalEnemies, int waveCount, ScoutLevel scoutLevel)
        {
            _previewOverlay?.SetDisplay(true);

            // 정찰 수준별 정보 공개
            bool showDetails = scoutLevel >= ScoutLevel.Scouted;

            _previewTotal?.SetText(showDetails ? totalEnemies.ToString() : "???");
            _previewWaves?.SetText(showDetails ? waveCount.ToString() : "???");

            string threat = turn switch
            {
                <= 3 => "Low",
                <= 8 => "Medium",
                <= 15 => "High",
                _ => "Extreme"
            };
            _previewThreat?.SetText(showDetails ? threat : "???");

            // 방향별 적 수 (스텁: 4방향 균등)
            int perDir = showDetails ? totalEnemies / 4 : 0;
            SetDirection(_dirNorth, _dirNorthCount, perDir, showDetails);
            SetDirection(_dirEast, _dirEastCount, perDir + (showDetails ? totalEnemies % 4 : 0), showDetails);
            SetDirection(_dirSouth, _dirSouthCount, perDir, showDetails);
            SetDirection(_dirWest, _dirWestCount, perDir, showDetails);

            _scoutInfo?.SetText(scoutLevel switch
            {
                ScoutLevel.Unknown => "Scout level: Unknown — dispatch expedition for details",
                ScoutLevel.Scouted => "Scout level: Scouted — basic information available",
                ScoutLevel.Detailed => "Scout level: Detailed — full intelligence report",
                _ => ""
            });
        }

        /// <summary>
        /// 전투 결과 화면 표시.
        /// SF-WD-014. WaveResult 데이터 바인딩.
        /// </summary>
        public void ShowResult(WaveResult result)
        {
            if (result == null) return;
            _resultOverlay?.SetDisplay(true);

            // Grade
            if (_resultGrade != null)
            {
                _resultGrade.ClearClassList();
                _resultGrade.AddToClassList("result-grade");

                string gradeText;
                string gradeClass;
                switch (result.grade)
                {
                    case DefenseResultGrade.PerfectDefense:
                        gradeText = "PERFECT DEFENSE";
                        gradeClass = "grade--perfect";
                        break;
                    case DefenseResultGrade.MinorDamage:
                        gradeText = "MINOR DAMAGE";
                        gradeClass = "grade--minor";
                        break;
                    case DefenseResultGrade.ModerateDamage:
                        gradeText = "MODERATE DAMAGE";
                        gradeClass = "grade--moderate";
                        break;
                    default:
                        gradeText = "MAJOR DAMAGE";
                        gradeClass = "grade--major";
                        break;
                }
                _resultGrade.text = gradeText;
                _resultGrade.AddToClassList(gradeClass);
            }

            // Rewards
            _rewardBasic?.SetText($"+{result.basicReward}");
            _rewardAdvanced?.SetText($"+{result.advancedReward}");
            _rewardRelic?.SetText($"+{result.relicReward}");

            // Stats
            _resultKilled?.SetText($"{result.enemiesDefeated} / {result.enemiesTotal}");
            _resultDamage?.SetText($"{result.damageRatio:P0}");

            // Damaged buildings
            bool hasDamaged = result.damagedBuildingSlotIds != null && result.damagedBuildingSlotIds.Count > 0;
            _damageList?.SetDisplay(hasDamaged);
            if (hasDamaged)
                _damageListItems?.SetText(string.Join(", ", result.damagedBuildingSlotIds));
        }

        public void HidePreview() => _previewOverlay?.SetDisplay(false);
        public void HideResult() => _resultOverlay?.SetDisplay(false);

        void SetDirection(VisualElement indicator, Label countLabel, int count, bool visible)
        {
            if (indicator == null) return;
            indicator.RemoveFromClassList("direction-indicator--active");
            if (visible && count > 0)
                indicator.AddToClassList("direction-indicator--active");
            countLabel?.SetText(visible ? count.ToString() : "?");
        }
    }
}
