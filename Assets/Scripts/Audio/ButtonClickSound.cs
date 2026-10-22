using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonClickSound : MonoBehaviour
    {
        private Button _button;
        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Play);
        }
        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(Play);
        }
        private void Play() => GameAudioPlayer.Play(GameSound.Click);
    }
}
