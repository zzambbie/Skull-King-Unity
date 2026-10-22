using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SkullKing.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SkullKing.UI
{
    /// <summary>Attach to the Unity-authored SettingsPanel; layout remains editable in Inspector.</summary>
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [Header("Unity에서 만든 설정 UI 연결")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private TMP_Text _masterVolumeValueText;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private TMP_Text _musicVolumeValueText;
        [SerializeField] private Slider _effectsVolumeSlider;
        [SerializeField] private TMP_Text _effectsVolumeValueText;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _resetButton;

        [Header("선택: 음량 확인용 AudioSource (Play On Awake 해제)")]
        [Tooltip("테스트 소리를 지정했다면 미리듣기 버튼의 On Click에 PlayPreviewSound를 연결하세요.")]
        [SerializeField] private AudioSource _previewAudioSource;
        [SerializeField] private bool _escapeClosesPanel = true;
        private bool _savePending;
        private float _saveAt;

        private void OnEnable()
        {
            GameSettings.EnsureLoaded();
            RefreshControls();
            if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(ChangeVolume);
            if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.AddListener(ChangeMusicVolume);
            if (_effectsVolumeSlider != null) _effectsVolumeSlider.onValueChanged.AddListener(ChangeEffectsVolume);
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(ChangeFullscreen);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_resetButton != null) _resetButton.onClick.AddListener(ResetDefaults);
        }

        private void OnDisable()
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.RemoveListener(ChangeVolume);
            if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.RemoveListener(ChangeMusicVolume);
            if (_effectsVolumeSlider != null) _effectsVolumeSlider.onValueChanged.RemoveListener(ChangeEffectsVolume);
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.RemoveListener(ChangeFullscreen);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
            if (_resetButton != null) _resetButton.onClick.RemoveListener(ResetDefaults);
            if (_previewAudioSource != null) _previewAudioSource.Stop();
            FlushSave();
        }

        private void Update()
        {
            if (_savePending && Time.unscaledTime >= _saveAt) FlushSave();
            if (!_escapeClosesPanel) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
        }

        private void RefreshControls()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.minValue = 0f;
                _masterVolumeSlider.maxValue = 1f;
                _masterVolumeSlider.wholeNumbers = false;
                _masterVolumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            }
            if (_fullscreenToggle != null) _fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            SetSlider(_musicVolumeSlider, GameSettings.MusicVolume);
            SetSlider(_effectsVolumeSlider, GameSettings.EffectsVolume);
            RefreshVolumeText();
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);
        }

        private void RefreshVolumeText()
        {
            if (_masterVolumeValueText != null)
                _masterVolumeValueText.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
            if (_musicVolumeValueText != null)
                _musicVolumeValueText.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
            if (_effectsVolumeValueText != null)
                _effectsVolumeValueText.text = Mathf.RoundToInt(GameSettings.EffectsVolume * 100f) + "%";
        }

        private void ChangeMusicVolume(float value)
        {
            GameSettings.SetMusicVolume(value);
            RefreshVolumeText();
            ScheduleSave();
        }

        private void ChangeEffectsVolume(float value)
        {
            GameSettings.SetEffectsVolume(value);
            RefreshVolumeText();
            ScheduleSave();
            SkullKing.Audio.GameAudioPlayer.Play(SkullKing.Audio.GameSound.Click);
        }

        private void ChangeVolume(float value)
        {
            GameSettings.SetMasterVolume(value);
            RefreshVolumeText();
            ScheduleSave();
        }

        private void ChangeFullscreen(bool value)
        {
            GameSettings.SetFullscreen(value);
            ScheduleSave();
        }

        private void ScheduleSave()
        {
            _savePending = true;
            _saveAt = Time.unscaledTime + 0.5f;
        }

        private void FlushSave()
        {
            if (!_savePending) return;
            GameSettings.Save();
            _savePending = false;
        }

        public void Open()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        public void ResetDefaults()
        {
            GameSettings.ResetDefaults();
            _savePending = false;
            RefreshControls();
        }

        public void PlayPreviewSound()
        {
            if (_previewAudioSource == null || _previewAudioSource.clip == null)
            {
                Debug.LogWarning("[Settings] 미리듣기용 AudioSource와 AudioClip을 Unity에서 연결해 주세요.", this);
                return;
            }
            _previewAudioSource.Play();
        }

        private void OnApplicationQuit() => FlushSave();
    }
}
