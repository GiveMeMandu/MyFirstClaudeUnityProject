using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Page;
using UnityScreenNavigator.Runtime.Core.Sheet;

namespace UIStudy.Advanced.Pages
{
    /// <summary>
    /// 중첩 SheetContainer를 가진 Page.
    /// Page 안에 탭 바 + SheetContainer를 배치하여 카테고리 전환.
    /// USN의 SheetContainer는 Page/Modal/Sheet 어디에든 중첩 가능.
    ///
    /// 사용 예: 기지 관리 화면의 건설/연구/유닛 탭
    /// </summary>
    public class TabbedPageView : Page
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button[] _tabButtons;

        [Header("Nested Sheet Container")]
        [SerializeField] private SheetContainer _sheetContainer;

        [Header("Title")]
        [SerializeField] private TextMeshProUGUI _titleText;

        public SheetContainer SheetContainer => _sheetContainer;
        public Button[] TabButtons => _tabButtons;

        public void SetTitle(string title) => _titleText.text = title;

        public void SetActiveTabHighlight(int activeIndex)
        {
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == activeIndex
                        ? new Color(0.3f, 0.6f, 1f)
                        : new Color(0.4f, 0.4f, 0.4f);
            }
        }
    }
}
