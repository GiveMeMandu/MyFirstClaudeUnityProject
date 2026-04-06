using UnityEngine;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 씬 독립 오디오 매니저 싱글턴.
    /// BGM/SFX 볼륨 채널 분리. 설정 PlayerPrefs 저장/복원.
    /// SF-AUD-001.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const string PrefKeyBGM = "AudioManager_BGMVolume";
        const string PrefKeySFX = "AudioManager_SFXVolume";
        const string PrefKeyMaster = "AudioManager_MasterVolume";

        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] AudioSource bgmSource;
        [SerializeField] AudioSource sfxSource;

        [Header("Defaults")]
        [SerializeField] [Range(0f, 1f)] float defaultMasterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] float defaultBGMVolume = 0.7f;
        [SerializeField] [Range(0f, 1f)] float defaultSFXVolume = 1f;

        float _masterVolume;
        float _bgmVolume;
        float _sfxVolume;

        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); ApplyVolumes(); }
        }

        public float BGMVolume
        {
            get => _bgmVolume;
            set { _bgmVolume = Mathf.Clamp01(value); ApplyVolumes(); }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); ApplyVolumes(); }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();
            LoadSettings();
            ApplyVolumes();
        }

        void EnsureSources()
        {
            if (bgmSource == null)
            {
                var bgmGo = new GameObject("BGM");
                bgmGo.transform.SetParent(transform);
                bgmSource = bgmGo.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.spatialBlend = 0f; // 2D
            }

            if (sfxSource == null)
            {
                var sfxGo = new GameObject("SFX");
                sfxGo.transform.SetParent(transform);
                sfxSource = sfxGo.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D
            }
        }

        void ApplyVolumes()
        {
            if (bgmSource != null) bgmSource.volume = _masterVolume * _bgmVolume;
            if (sfxSource != null) sfxSource.volume = _masterVolume * _sfxVolume;
        }

        // ── BGM ──

        /// <summary>BGM 재생. 같은 클립이면 재시작하지 않음.</summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        /// <summary>BGM 정지.</summary>
        public void StopBGM()
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        /// <summary>BGM 일시정지/재개.</summary>
        public void PauseBGM(bool pause)
        {
            if (pause) bgmSource.Pause();
            else bgmSource.UnPause();
        }

        // ── SFX ──

        /// <summary>SFX 원샷 재생.</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume);
        }

        /// <summary>SFX를 지정 볼륨으로 재생.</summary>
        public void PlaySFX(AudioClip clip, float volumeScale)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume * volumeScale);
        }

        /// <summary>3D 위치에서 SFX 재생 (월드 공간).</summary>
        public void PlaySFXAtPoint(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, _masterVolume * _sfxVolume);
        }

        // ── Settings ──

        /// <summary>볼륨 설정을 PlayerPrefs에 저장.</summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefKeyMaster, _masterVolume);
            PlayerPrefs.SetFloat(PrefKeyBGM, _bgmVolume);
            PlayerPrefs.SetFloat(PrefKeySFX, _sfxVolume);
            PlayerPrefs.Save();
        }

        /// <summary>PlayerPrefs에서 볼륨 설정 복원.</summary>
        public void LoadSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(PrefKeyMaster, defaultMasterVolume);
            _bgmVolume = PlayerPrefs.GetFloat(PrefKeyBGM, defaultBGMVolume);
            _sfxVolume = PlayerPrefs.GetFloat(PrefKeySFX, defaultSFXVolume);
        }

        /// <summary>기본값으로 초기화.</summary>
        public void ResetToDefaults()
        {
            _masterVolume = defaultMasterVolume;
            _bgmVolume = defaultBGMVolume;
            _sfxVolume = defaultSFXVolume;
            ApplyVolumes();
        }
    }
}
