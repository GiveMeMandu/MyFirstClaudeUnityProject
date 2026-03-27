using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Page;

namespace UIStudy.Navigation.Pages
{
    /// <summary>
    /// 상세 Page — 설정으로 이동 + 뒤로 가기 버튼.
    /// </summary>
    public class DetailPageView : Page
    {
        [SerializeField] private Button _goToSettingsButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _contentText;

        public Observable<Unit> OnGoToSettingsClick => _goToSettingsButton.OnClickAsObservable();
        public Observable<Unit> OnBackClick => _backButton.OnClickAsObservable();

        public void SetTitle(string title) => _titleText.text = title;
        public void SetContent(string content) => _contentText.text = content;
    }
}
