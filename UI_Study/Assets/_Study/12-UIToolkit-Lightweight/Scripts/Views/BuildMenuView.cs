using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: 건설 메뉴 View — BuildingCatalog에서 카드 동적 생성.
    /// 카드 클릭 이벤트 + USS class로 affordable/expensive/locked 상태 관리.
    /// </summary>
    public class BuildMenuView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _cardContainer;
        private Label _statusLabel;
        private readonly List<VisualElement> _cards = new();

        // 카드 클릭 이벤트 — index 전달
        public event Action<int> OnBuildingSelected;

        // 카드 hover 이벤트 — 툴팁용
        public event Action<int, Vector2> OnCardHoverEnter;
        public event Action OnCardHoverExit;

        private const string AffordableClass = "build-card--affordable";
        private const string ExpensiveClass  = "build-card--expensive";
        private const string LockedClass     = "build-card--locked";

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _cardContainer = root.Q<VisualElement>("card-container");
            _statusLabel   = root.Q<Label>("build-status");
        }

        /// <summary>
        /// 카탈로그로부터 카드 UI 동적 생성.
        /// </summary>
        public void BuildCards(IReadOnlyList<BuildingData> buildings)
        {
            _cardContainer.Clear();
            _cards.Clear();

            for (int i = 0; i < buildings.Count; i++)
            {
                var data = buildings[i];
                var card = CreateCard(data, i);
                _cardContainer.Add(card);
                _cards.Add(card);
            }
        }

        /// <summary>
        /// 개별 카드의 어포더빌리티 상태를 USS class로 갱신.
        /// </summary>
        public void UpdateCardState(int index, CardState state)
        {
            if (index < 0 || index >= _cards.Count) return;

            var card = _cards[index];
            card.RemoveFromClassList(AffordableClass);
            card.RemoveFromClassList(ExpensiveClass);
            card.RemoveFromClassList(LockedClass);

            switch (state)
            {
                case CardState.Affordable:
                    card.AddToClassList(AffordableClass);
                    break;
                case CardState.TooExpensive:
                    card.AddToClassList(ExpensiveClass);
                    break;
                case CardState.Locked:
                    card.AddToClassList(LockedClass);
                    break;
            }
        }

        public void SetStatus(string message)
        {
            _statusLabel.text = message;
        }

        private VisualElement CreateCard(BuildingData data, int index)
        {
            var card = new VisualElement();
            card.AddToClassList("build-card");

            // Icon
            var icon = new Label { text = data.Name[..1] };
            icon.AddToClassList("card-icon");
            card.Add(icon);

            // Name
            var nameLabel = new Label { text = data.Name };
            nameLabel.AddToClassList("card-name");
            card.Add(nameLabel);

            // Cost row
            var costRow = new VisualElement();
            costRow.AddToClassList("card-cost-row");

            var goldIcon = new Label { text = "G" };
            goldIcon.AddToClassList("card-cost-icon");
            goldIcon.AddToClassList("card-cost-icon--gold");
            costRow.Add(goldIcon);

            var goldVal = new Label { text = data.GoldCost.ToString() };
            goldVal.AddToClassList("card-cost-value");
            costRow.Add(goldVal);

            var woodIcon = new Label { text = "W" };
            woodIcon.AddToClassList("card-cost-icon");
            woodIcon.AddToClassList("card-cost-icon--wood");
            costRow.Add(woodIcon);

            var woodVal = new Label { text = data.WoodCost.ToString() };
            woodVal.AddToClassList("card-cost-value");
            costRow.Add(woodVal);

            card.Add(costRow);

            // Click — named callback via closure over captured index
            int capturedIndex = index;
            card.RegisterCallback<ClickEvent>(HandleCardClick);

            // Hover — for tooltip
            card.RegisterCallback<PointerEnterEvent>(HandleCardPointerEnter);
            card.RegisterCallback<PointerLeaveEvent>(HandleCardPointerLeave);

            // Store index in userData for event handlers
            card.userData = capturedIndex;

            return card;
        }

        private void HandleCardClick(ClickEvent evt)
        {
            if (evt.currentTarget is VisualElement card && card.userData is int index)
            {
                OnBuildingSelected?.Invoke(index);
            }
        }

        private void HandleCardPointerEnter(PointerEnterEvent evt)
        {
            if (evt.currentTarget is VisualElement card && card.userData is int index)
            {
                var pos = new Vector2(evt.position.x, evt.position.y);
                OnCardHoverEnter?.Invoke(index, pos);
            }
        }

        private void HandleCardPointerLeave(PointerLeaveEvent evt)
        {
            OnCardHoverExit?.Invoke();
        }

        private void OnDisable()
        {
            // Unregister callbacks from dynamically created cards
            foreach (var card in _cards)
            {
                card.UnregisterCallback<ClickEvent>(HandleCardClick);
                card.UnregisterCallback<PointerEnterEvent>(HandleCardPointerEnter);
                card.UnregisterCallback<PointerLeaveEvent>(HandleCardPointerLeave);
            }
        }
    }

    public enum CardState
    {
        Affordable,
        TooExpensive,
        Locked
    }
}
