using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 3: Bootstrapper — DialogDemo 씬 조립.
    /// </summary>
    public class DialogDemoBootstrapper : MonoBehaviour
    {
        [SerializeField] private DialogDemoView _demoView;
        [SerializeField] private ConfirmDialogView _confirmDialog;
        [SerializeField] private LoadingScreenView _loadingScreen;

        private DialogDemoPresenter _presenter;

        private void Start()
        {
            _presenter = new DialogDemoPresenter(
                _demoView, _confirmDialog, _loadingScreen, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
