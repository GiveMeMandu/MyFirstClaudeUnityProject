using System;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 7: 설정 Presenter — Clone-Edit-Apply/Cancel 패턴.
    /// 열 때 원본 Clone → 편집 복사본 수정 → Apply 시 원본에 복사,
    /// Cancel 시 SetValueWithoutNotify로 UI 복구.
    /// </summary>
    public class SettingsPresenter : IDisposable
    {
        private readonly SettingsModel _original;
        private readonly SettingsView _view;
        private SettingsModel _editCopy;

        public SettingsPresenter(SettingsModel original, SettingsView view)
        {
            _original = original;
            _view = view;

            _view.OnQualityChanged      += HandleQualityChanged;
            _view.OnFullscreenChanged   += HandleFullscreenChanged;
            _view.OnMasterVolumeChanged += HandleMasterVolumeChanged;
            _view.OnSfxVolumeChanged    += HandleSfxVolumeChanged;
            _view.OnDifficultyChanged   += HandleDifficultyChanged;
            _view.OnApplyClicked        += HandleApply;
            _view.OnCancelClicked       += HandleCancel;
        }

        /// <summary>
        /// 설정 패널 열기 — 원본을 클론하여 편집 시작.
        /// </summary>
        public void Initialize()
        {
            _editCopy = _original.Clone();
            _view.SetValues(_editCopy);
            Debug.Log("[SettingsPresenter] Settings panel opened — editing clone.");
        }

        private void HandleQualityChanged(int index)
        {
            _editCopy.QualityLevel = index;
            Debug.Log($"[SettingsPresenter] Quality → {index}");
        }

        private void HandleFullscreenChanged(bool value)
        {
            _editCopy.Fullscreen = value;
            Debug.Log($"[SettingsPresenter] Fullscreen → {value}");
        }

        private void HandleMasterVolumeChanged(float value)
        {
            _editCopy.MasterVolume = value;
        }

        private void HandleSfxVolumeChanged(float value)
        {
            _editCopy.SfxVolume = value;
        }

        private void HandleDifficultyChanged(int index)
        {
            _editCopy.Difficulty = index;
            Debug.Log($"[SettingsPresenter] Difficulty → {index}");
        }

        private void HandleApply()
        {
            _original.ApplyFrom(_editCopy);
            Debug.Log("[SettingsPresenter] Settings applied to original model.");
        }

        private void HandleCancel()
        {
            // 편집 복사본을 원본 값으로 되돌림
            _editCopy = _original.Clone();
            // SetValueWithoutNotify로 이벤트 발화 없이 UI 복구
            _view.SetValues(_editCopy);
            Debug.Log("[SettingsPresenter] Settings cancelled — restored from original.");
        }

        public void Dispose()
        {
            _view.OnQualityChanged      -= HandleQualityChanged;
            _view.OnFullscreenChanged   -= HandleFullscreenChanged;
            _view.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
            _view.OnSfxVolumeChanged    -= HandleSfxVolumeChanged;
            _view.OnDifficultyChanged   -= HandleDifficultyChanged;
            _view.OnApplyClicked        -= HandleApply;
            _view.OnCancelClicked       -= HandleCancel;
        }
    }
}
