using System;
using SkullKing.Data;
using SkullKing.Logic;
using SkullKing.Networking;

namespace SkullKing.Core
{
    public partial class GameManager
    {
        private bool _networkAuthority;
        private SteamGameSession _networkSession;
        public event Action<string> OnNetworkStatusChanged;

        public void AttachNetworkView(SteamGameSession session) => _networkSession = session;
        public void SetNetworkStatus(string text) => OnNetworkStatusChanged?.Invoke(text);
        // Keep the old method as an alias so existing Unity button references keep working.
        public void ReturnMultiplayerToLobby() => ReturnToTitle();

        public void ReturnToTitle()
        {
            GameSettings.Save();
            LobbyManager.ShouldStartGame = false;
            if (_networkSession != null)
            {
                _networkSession.ReturnToLobby();
                return;
            }
            StopNetworkGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        // A separate, non-rendering engine runs on the creator's PC. Every seat uses human input.
        public void StartNetworkAuthority(string[] names)
        {
            _networkAuthority = true;
            State = new GameState { PlayerCount = names.Length, Players = new PlayerData[names.Length] };
            for (int i = 0; i < names.Length; i++)
                State.Players[i] = new PlayerData(i, names[i], true) { TotalScore = 20 };
            _deck = new Deck();
            _ai = new AIController();
            IsGameRunning = true;
            StartCoroutine(GameLoop());
        }

        public bool TryNetworkInput(int seat, string command, int value)
        {
            if (!_networkAuthority || !IsGameRunning || State == null) return false;
            if (command == "confirm")
            {
                if (!_waitingForRoundConfirm) return false;
                SubmitRoundConfirm();
                return true;
            }
            if (seat != State.CurrentPlayerIndex) return false;
            if (command == "bid" && _waitingForHumanBid && value >= 0 && value <= State.CurrentRound)
            { SubmitHumanBid(value); return true; }
            if (command == "tigress" && _waitingForTigressChoice &&
                (value == (int)TigressChoice.Pirate || value == (int)TigressChoice.Escape))
            { SubmitTigressChoice((TigressChoice)value); return true; }
            if (command == "card" && _waitingForHumanCard)
            {
                var legal = TrickResolver.GetPlayableCards(State.Players[seat].Hand, State.LeadSuit);
                foreach (var card in legal)
                    if (card.Id == value) { SubmitHumanCard(card); return true; }
            }
            return false;
        }

        public void ApplyNetworkSnapshot(NetworkGameSnapshot snapshot)
        {
            State = snapshot.Restore();
            IsGameRunning = State.CurrentPhase != GamePhase.GameEnd;
            var actor = State.Players[snapshot.actor];
            switch (snapshot.kind)
            {
                case "phase": OnPhaseChanged?.Invoke(State.CurrentPhase); break;
                case "round": OnRoundStarted?.Invoke(State.CurrentRound); break;
                case "dealt": OnCardsDealt?.Invoke(); break;
                case "turn": OnCurrentPlayerChanged?.Invoke(State.CurrentPlayerIndex); break;
                case "bidRequest": if (snapshot.actor == 0) OnBidRequested?.Invoke(actor, State.CurrentRound); break;
                case "bid": OnPlayerBid?.Invoke(actor); break;
                case "allBids": OnAllBidsComplete?.Invoke(); break;
                case "trick": OnTrickStarted?.Invoke(State.CurrentTrick, State.LeaderIndex); break;
                case "cardRequest":
                    if (snapshot.actor == 0) OnCardPlayRequested?.Invoke(actor,
                        TrickResolver.GetPlayableCards(actor.Hand, State.LeadSuit));
                    break;
                case "tigressRequest": if (snapshot.actor == 0) OnTigressChoiceRequested?.Invoke(actor); break;
                case "played": OnCardPlayed?.Invoke(snapshot.played); break;
                case "resolved": OnTrickResolved?.Invoke(snapshot.result); break;
                case "scores": OnRoundEnded?.Invoke(snapshot.scores); break;
                case "end": OnGameEnded?.Invoke(actor); break;
            }
        }

        public void StopNetworkGame()
        {
            StopAllCoroutines();
            IsGameRunning = false;
        }
    }
}
