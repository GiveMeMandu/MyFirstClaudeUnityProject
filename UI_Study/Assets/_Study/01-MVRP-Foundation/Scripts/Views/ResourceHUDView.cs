using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.MVRP.Views
{
    /// <summary>
    /// 자원 HUD View — 3개 자원 표시 + 획득/소비 버튼.
    /// 비즈니스 로직 없음. Observable 이벤트 노출 + 표시 메서드만.
    /// </summary>
    public class ResourceHUDView : MonoBehaviour
    {
        [Header("Resource Texts")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _woodText;
        [SerializeField] private TextMeshProUGUI _populationText;

        [Header("Gold Buttons")]
        [SerializeField] private Button _addGoldButton;
        [SerializeField] private Button _spendGoldButton;

        [Header("Wood Buttons")]
        [SerializeField] private Button _addWoodButton;
        [SerializeField] private Button _spendWoodButton;

        [Header("Population Button")]
        [SerializeField] private Button _addPopulationButton;

        // View → Presenter: Observable 이벤트 노출
        public Observable<Unit> OnAddGoldClick => _addGoldButton.OnClickAsObservable();
        public Observable<Unit> OnSpendGoldClick => _spendGoldButton.OnClickAsObservable();
        public Observable<Unit> OnAddWoodClick => _addWoodButton.OnClickAsObservable();
        public Observable<Unit> OnSpendWoodClick => _spendWoodButton.OnClickAsObservable();
        public Observable<Unit> OnAddPopulationClick => _addPopulationButton.OnClickAsObservable();

        // Presenter → View: 단순 표시 메서드
        public void SetGold(int gold) => _goldText.text = $"Gold: {gold:N0}";
        public void SetWood(int wood) => _woodText.text = $"Wood: {wood:N0}";
        public void SetPopulation(int pop) => _populationText.text = $"Pop: {pop:N0}";

        public void SetSpendGoldInteractable(bool interactable) =>
            _spendGoldButton.interactable = interactable;

        public void SetSpendWoodInteractable(bool interactable) =>
            _spendWoodButton.interactable = interactable;
    }
}
