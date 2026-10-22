using UnityEngine;
using SkullKing.Core;

namespace SkullKing.Audio
{
    /// <summary>Source base gain multiplied by its category; master gain belongs to AudioListener.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioVolumeBinding : MonoBehaviour
    {
        public enum Category { Music, Effects }
        [SerializeField] private Category _category;
        [SerializeField, Range(0f, 1f)] private float _baseVolume = 0.45f;
        private AudioSource _source;

        private void OnEnable()
        {
            _source = GetComponent<AudioSource>();
            GameSettings.EnsureLoaded();
            GameSettings.AudioVolumesChanged += Apply;
            Apply();
        }
        private void OnDisable() => GameSettings.AudioVolumesChanged -= Apply;
        private void OnValidate()
        {
            if (Application.isPlaying && _source != null) Apply();
        }
        private void Apply()
        {
            _source.volume = GameSettings.SanitizeVolume(_baseVolume) *
                (_category == Category.Music ? GameSettings.MusicVolume : GameSettings.EffectsVolume);
        }
    }
}
