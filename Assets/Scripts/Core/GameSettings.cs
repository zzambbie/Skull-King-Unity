using UnityEngine;

namespace SkullKing.Core
{
    /// <summary>Local preferences only. Changing settings never pauses or changes a multiplayer match.</summary>
    public static class GameSettings
    {
        private const string VolumeKey = "CROSSION.Settings.MasterVolume";
        private const string FullscreenKey = "CROSSION.Settings.Fullscreen";
        private const string MusicKey = "CROSSION.Settings.MusicVolume";
        private const string EffectsKey = "CROSSION.Settings.EffectsVolume";
        public static event System.Action AudioVolumesChanged;
        private static bool _loaded;
        public static float MasterVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;
        public static float EffectsVolume { get; private set; } = 1f;
        public static bool Fullscreen { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _loaded = false;
            AudioVolumesChanged = null;
            EnsureLoaded();
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            MasterVolume = SanitizeVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
            MusicVolume = SanitizeVolume(PlayerPrefs.GetFloat(MusicKey, 1f));
            EffectsVolume = SanitizeVolume(PlayerPrefs.GetFloat(EffectsKey, 1f));
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) != 0;
            AudioListener.volume = MasterVolume;
            ApplyDisplay();
        }

        public static float SanitizeVolume(float volume)
            => float.IsNaN(volume) || float.IsInfinity(volume) ? 1f : Mathf.Clamp01(volume);

        public static void SetMasterVolume(float volume)
        {
            EnsureLoaded();
            MasterVolume = SanitizeVolume(volume);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        }

        public static void SetMusicVolume(float volume)
        {
            EnsureLoaded();
            MusicVolume = SanitizeVolume(volume);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            AudioVolumesChanged?.Invoke();
        }

        public static void SetEffectsVolume(float volume)
        {
            EnsureLoaded();
            EffectsVolume = SanitizeVolume(volume);
            PlayerPrefs.SetFloat(EffectsKey, EffectsVolume);
            AudioVolumesChanged?.Invoke();
        }

        public static void SetFullscreen(bool fullscreen)
        {
            EnsureLoaded();
            Fullscreen = fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            ApplyDisplay();
        }

        private static void ApplyDisplay()
        {
            // Do not alter the user's Unity Editor window or project resolution while testing.
            if (!Application.isEditor)
                Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        public static void ResetDefaults()
        {
            SetMasterVolume(1f);
            SetMusicVolume(1f);
            SetEffectsVolume(1f);
            SetFullscreen(true);
            Save();
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
