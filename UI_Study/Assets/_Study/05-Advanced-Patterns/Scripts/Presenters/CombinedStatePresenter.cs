using System;
using R3;
using TMPro;
using UIStudy.Advanced.Models;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace UIStudy.Advanced.Presenters
{
    /// <summary>
    /// CombineLatest로 다중 자원 조건을 파생하여 건설 가능 상태를 표시.
    /// ReactiveCommand 대신 CanBuild Observable로 버튼 interactable 제어.
    /// </summary>
    public class CombinedStatePresenter : IInitializable, IDisposable
    {
        private readonly BuildActionModel _model;
        private readonly CombinedStateView _view;
        private readonly CompositeDisposable _disposables = new();

        public CombinedStatePresenter(BuildActionModel model, CombinedStateView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // 개별 자원 바인딩
            _model.Gold.Subscribe(g => _view.SetGold(g)).AddTo(_disposables);
            _model.Wood.Subscribe(w => _view.SetWood(w)).AddTo(_disposables);

            // CombineLatest — 두 자원이 모두 비용 이상이면 건설 가능
            _model.CanBuild
                .Subscribe(canBuild =>
                {
                    _view.SetBuildButtonInteractable(canBuild);
                    _view.SetStatusText(canBuild
                        ? $"건설 가능 (Gold {_model.GoldCost} + Wood {_model.WoodCost})"
                        : "자원 부족!");
                })
                .AddTo(_disposables);

            // 버튼 이벤트
            _view.OnBuildClick
                .Subscribe(_ =>
                {
                    if (_model.TryBuild())
                        Debug.Log("[CombinedState] 건설 완료!");
                })
                .AddTo(_disposables);

            _view.OnAddGoldClick.Subscribe(_ => _model.AddGold(10)).AddTo(_disposables);
            _view.OnAddWoodClick.Subscribe(_ => _model.AddWood(10)).AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }

    /// <summary>
    /// CombinedState View — 건설 버튼 + 자원 표시 + 상태 텍스트.
    /// </summary>
    public class CombinedStateView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _woodText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Button _buildButton;
        [SerializeField] private Button _addGoldButton;
        [SerializeField] private Button _addWoodButton;

        public Observable<Unit> OnBuildClick => _buildButton.OnClickAsObservable();
        public Observable<Unit> OnAddGoldClick => _addGoldButton.OnClickAsObservable();
        public Observable<Unit> OnAddWoodClick => _addWoodButton.OnClickAsObservable();

        public void SetGold(int g) => _goldText.text = $"Gold: {g}";
        public void SetWood(int w) => _woodText.text = $"Wood: {w}";
        public void SetStatusText(string s) => _statusText.text = s;
        public void SetBuildButtonInteractable(bool b) => _buildButton.interactable = b;
    }
}
