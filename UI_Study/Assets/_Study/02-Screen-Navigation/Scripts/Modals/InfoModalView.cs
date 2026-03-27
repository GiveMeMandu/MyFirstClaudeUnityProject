using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityScreenNavigator.Runtime.Core.Modal;

namespace UIStudy.Navigation.Modals
{
    /// <summary>
    /// 정보 표시 모달 — 닫기 버튼만 있는 단순 모달.
    /// </summary>
    public class InfoModalView : Modal
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Button _closeButton;

        public Observable<Unit> OnCloseClick => _closeButton.OnClickAsObservable();

        public void SetTitle(string title) => _titleText.text = title;
        public void SetBody(string body) => _bodyText.text = body;
    }
}
