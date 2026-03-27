using R3;
using R3.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.MVRP.Views
{
    /// <summary>
    /// 카운터 View — UI 요소 참조와 이벤트 노출만 담당.
    /// 비즈니스 로직 없음. Model 직접 참조 없음.
    /// </summary>
    public class CounterView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private Button _incrementButton;
        [SerializeField] private Button _decrementButton;
        [SerializeField] private Button _resetButton;

        // View → Presenter 방향: Observable 이벤트 노출
        public Observable<Unit> OnIncrementClick => _incrementButton.OnClickAsObservable();
        public Observable<Unit> OnDecrementClick => _decrementButton.OnClickAsObservable();
        public Observable<Unit> OnResetClick => _resetButton.OnClickAsObservable();

        // Presenter → View 방향: 단순 표시 메서드
        public void SetCountText(int count) => _countText.text = $"Count: {count}";
    }
}
