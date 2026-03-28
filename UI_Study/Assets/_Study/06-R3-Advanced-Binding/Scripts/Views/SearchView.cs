using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;

namespace UIStudy.R3Advanced.Views
{
    /// <summary>
    /// 디바운스 검색 View — 검색 InputField, 결과 카운트, 8개 결과 슬롯.
    /// </summary>
    public class SearchView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _searchField;
        [SerializeField] private TextMeshProUGUI _resultCountText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private List<TextMeshProUGUI> _resultSlots = new();

        public Observable<string> OnSearchValueChanged =>
            _searchField.onValueChanged.AsObservable();

        public void SetResultCount(string text) => _resultCountText.text = text;
        public void SetStatusText(string text) => _statusText.text = text;

        public void SetSearchFieldText(string text)
        {
            _searchField.SetTextWithoutNotify(text);
        }

        /// <summary>
        /// 결과 슬롯에 필터된 아이템 목록을 표시.
        /// 슬롯 수를 초과하는 결과는 잘린다.
        /// </summary>
        public void DisplayResults(List<string> items)
        {
            for (int i = 0; i < _resultSlots.Count; i++)
            {
                if (i < items.Count)
                {
                    _resultSlots[i].text = items[i];
                    _resultSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    _resultSlots[i].text = string.Empty;
                    _resultSlots[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 모든 결과 슬롯을 비운다.
        /// </summary>
        public void ClearResults()
        {
            foreach (var slot in _resultSlots)
            {
                slot.text = string.Empty;
                slot.gameObject.SetActive(false);
            }
        }
    }
}
