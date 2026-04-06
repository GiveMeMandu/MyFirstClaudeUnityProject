using UnityEngine;
using UnityEngine.UIElements;
using ProjectSun.V2.Core;
using ProjectSun.V2.Construction;

namespace ProjectSun.V2.UI
{
    /// <summary>
    /// 인카운터 팝업 Presenter (UI Toolkit).
    /// EncounterBridge 이벤트를 구독하여 팝업 표시.
    /// SF-UX-005.
    /// </summary>
    public class EncounterPopupPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UIDocument uiDocument;
        [SerializeField] EncounterBridge encounterBridge;

        VisualElement _root;
        VisualElement _overlay;
        Label _title;
        Label _desc;
        VisualElement _choiceList;

        void OnEnable()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            _overlay = _root.Q("encounter-overlay");
            _title = _root.Q<Label>("encounter-title");
            _desc = _root.Q<Label>("encounter-desc");
            _choiceList = _root.Q("choice-list");

            if (encounterBridge != null)
            {
                encounterBridge.OnEncounterStarted += ShowPopup;
                encounterBridge.OnEncounterEnded += HidePopup;
            }
        }

        void OnDisable()
        {
            if (encounterBridge != null)
            {
                encounterBridge.OnEncounterStarted -= ShowPopup;
                encounterBridge.OnEncounterEnded -= HidePopup;
            }
        }

        void ShowPopup(EncounterData data)
        {
            _title?.SetText(data.Title);
            _desc?.SetText(data.Description);

            _choiceList?.Clear();

            for (int i = 0; i < data.Choices.Length; i++)
            {
                var choice = data.Choices[i];
                int choiceIdx = i;

                var btn = new Button(() => encounterBridge?.ApplyChoice(choiceIdx));
                btn.AddToClassList("choice-btn");

                // 비용 표시
                string label = choice.Text;
                if (choice.CostBasic > 0 || choice.CostAdvanced > 0)
                    btn.AddToClassList("choice-btn--cost");

                btn.text = label;
                _choiceList?.Add(btn);
            }

            _overlay?.SetDisplay(true);
        }

        void HidePopup()
        {
            _overlay?.SetDisplay(false);
        }
    }
}
