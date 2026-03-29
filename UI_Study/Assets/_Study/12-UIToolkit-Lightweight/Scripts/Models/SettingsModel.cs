using System;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 7: 설정 모델 — Clone/ApplyFrom 패턴으로 편집 취소 지원.
    /// UI 참조 없음.
    /// </summary>
    public class SettingsModel
    {
        private int _qualityLevel;
        private bool _fullscreen;
        private float _masterVolume;
        private float _sfxVolume;
        private int _difficulty;

        public event Action SettingsChanged;

        public int QualityLevel
        {
            get => _qualityLevel;
            set { _qualityLevel = value; SettingsChanged?.Invoke(); }
        }

        public bool Fullscreen
        {
            get => _fullscreen;
            set { _fullscreen = value; SettingsChanged?.Invoke(); }
        }

        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = value; SettingsChanged?.Invoke(); }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = value; SettingsChanged?.Invoke(); }
        }

        public int Difficulty
        {
            get => _difficulty;
            set { _difficulty = value; SettingsChanged?.Invoke(); }
        }

        public SettingsModel(int qualityLevel = 2, bool fullscreen = true,
            float masterVolume = 0.8f, float sfxVolume = 0.7f, int difficulty = 1)
        {
            _qualityLevel = qualityLevel;
            _fullscreen = fullscreen;
            _masterVolume = masterVolume;
            _sfxVolume = sfxVolume;
            _difficulty = difficulty;
        }

        /// <summary>
        /// 편집용 복사본 생성 — Cancel 시 원본 복구 가능.
        /// </summary>
        public SettingsModel Clone()
        {
            return new SettingsModel(_qualityLevel, _fullscreen,
                _masterVolume, _sfxVolume, _difficulty);
        }

        /// <summary>
        /// 다른 모델의 값을 이 모델에 복사 (Apply 시 사용).
        /// </summary>
        public void ApplyFrom(SettingsModel other)
        {
            QualityLevel = other.QualityLevel;
            Fullscreen = other.Fullscreen;
            MasterVolume = other.MasterVolume;
            SfxVolume = other.SfxVolume;
            Difficulty = other.Difficulty;
        }
    }
}
