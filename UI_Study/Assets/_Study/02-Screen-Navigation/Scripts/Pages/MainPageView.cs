using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Page;

namespace UIStudy.Navigation.Pages
{
    /// <summary>
    /// 메인 Page — 앱의 첫 화면. 상세 화면으로 이동하는 버튼을 제공.
    /// UnityScreenNavigator의 Page를 상속하여 수명주기 콜백을 받는다.
    /// </summary>
    public class MainPageView : Page
    {
        [SerializeField] private Button _goToDetailButton;
        [SerializeField] private TextMeshProUGUI _titleText;

        public Observable<Unit> OnGoToDetailClick => _goToDetailButton.OnClickAsObservable();

        public void SetTitle(string title) => _titleText.text = title;
    }
}
