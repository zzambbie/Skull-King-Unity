using UnityEngine;
using SkullKing.Core;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.Audio
{
    /// <summary>Bind only the visible game view, never the hidden multiplayer authority or test peers.</summary>
    public sealed class GameAudioEvents : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        private void OnEnable()
        {
            if (_gameManager == null) return;
            _gameManager.OnCardPlayed += Card;
            _gameManager.OnTrickResolved += Trick;
            _gameManager.OnRoundEnded += Round;
        }
        private void OnDisable()
        {
            if (_gameManager == null) return;
            _gameManager.OnCardPlayed -= Card;
            _gameManager.OnTrickResolved -= Trick;
            _gameManager.OnRoundEnded -= Round;
        }
        private void Card(PlayedCard card) => GameAudioPlayer.Play(GameSound.Card);
        private void Trick(TrickResult result)
        {
            // A voided trick has no winner; do not play a misleading victory cue.
            if (!result.IsVoided) GameAudioPlayer.Play(GameSound.TrickWin);
        }
        private void Round(RoundScoreResult[] results)
        {
            var players = _gameManager.State?.Players;
            if (players == null || results == null) return;
            // Both single-player and network views mark the local seat as the human seat.
            for (int i = 0; i < players.Length && i < results.Length; i++)
                if (players[i].IsHuman)
                {
                    GameAudioPlayer.Play(results[i].BidSuccess ? GameSound.RoundSuccess : GameSound.RoundFailure);
                    return;
                }
        }
    }
}
