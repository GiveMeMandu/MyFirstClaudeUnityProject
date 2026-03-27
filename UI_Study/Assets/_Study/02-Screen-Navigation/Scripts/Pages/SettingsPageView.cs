using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Page;

namespace UIStudy.Navigation.Pages
{
    /// <summary>
    /// 설정 Page — 뒤로 가기만 있는 마지막 화면.
    /// </summary>
    public class SettingsPageView : Page
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _titleText;

        public Observable<Unit> OnBackClick => _backButton.OnClickAsObservable();

        public void SetTitle(string title) => _titleText.text = title;
    }
}
