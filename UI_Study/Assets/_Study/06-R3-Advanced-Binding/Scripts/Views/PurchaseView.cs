using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.R3Advanced.Views
{
    /// <summary>
    /// 구매 데모 View — Buy 버튼, Gold 표시, Price 표시, 상태 텍스트, +10 Gold 버튼.
    /// </summary>
    public class PurchaseView : MonoBehaviour
    {
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _addGoldButton;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _statusText;

        public Button BuyButton => _buyButton;
        public Button AddGoldButton => _addGoldButton;

        public Observable<Unit> OnBuyClick => _buyButton.OnClickAsObservable();
        public Observable<Unit> OnAddGoldClick => _addGoldButton.OnClickAsObservable();

        public void SetGoldText(string text) => _goldText.text = text;
        public void SetPriceText(string text) => _priceText.text = text;
        public void SetStatusText(string text) => _statusText.text = text;
        public void SetBuyInteractable(bool interactable) => _buyButton.interactable = interactable;
    }
}
