using UnityEngine;

namespace SkullKing.Audio
{
    public enum GameSound { Click, Card, TrickWin, RoundSuccess, RoundFailure }

    /// <summary>One persistent SFX voice bank so a menu click survives scene changes.</summary>
    [RequireComponent(typeof(AudioSource), typeof(AudioVolumeBinding))]
    public sealed class GameAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip _click;
        [SerializeField] private AudioClip _card;
        [SerializeField] private AudioClip _trickWin;
        [SerializeField] private AudioClip _roundSuccess;
        [SerializeField] private AudioClip _roundFailure;
        private AudioSource _source;
        private static GameAudioPlayer _instance;
        private float _lastClick = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => _instance = null;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (_instance == this) _instance = null; }

        public static void Play(GameSound sound)
        {
            if (_instance == null || _instance._source == null) return;
            if (sound == GameSound.Click)
            {
                // Slider changes and overlapping submit/pointer events must not stack clicks.
                if (Time.unscaledTime - _instance._lastClick < 0.07f) return;
                _instance._lastClick = Time.unscaledTime;
            }
            AudioClip clip = sound switch
            {
                GameSound.Click => _instance._click,
                GameSound.Card => _instance._card,
                GameSound.TrickWin => _instance._trickWin,
                GameSound.RoundSuccess => _instance._roundSuccess,
                GameSound.RoundFailure => _instance._roundFailure,
                _ => null
            };
            if (clip != null) _instance._source.PlayOneShot(clip);
        }
    }
}
