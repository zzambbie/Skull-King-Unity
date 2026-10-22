using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkullKing.Core;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.Networking
{
    // Simulated remote humans only know their own filtered GameManager.State, never the rule engine.
    public sealed class LocalMultiplayerTestPeer : MonoBehaviour
    {
        private GameManager _view;
        private AIController _decisions;
        private float _confirmDelay;

        public void Begin(GameManager view, float confirmDelay)
        {
            _view = view;
            _decisions = new AIController();
            _confirmDelay = Mathf.Clamp(confirmDelay, 0.5f, 10f);
            _view.OnBidRequested += Bid;
            _view.OnCardPlayRequested += Play;
            _view.OnTigressChoiceRequested += ChooseTigress;
            _view.OnRoundEnded += Confirm;
            _view.OnNetworkStatusChanged += Status;
        }

        private void Bid(PlayerData player, int maximum)
        {
            int bid = _decisions.DecideBid(player.Hand, _view.State.PlayerCount);
            StartCoroutine(After(0.5f, () => _view.SubmitHumanBid(bid)));
        }

        private void Play(PlayerData player, List<CardData> legal)
        {
            var state = _view.State;
            CardData card = _decisions.ChooseCard(player.Hand, legal, state.TrickCards, state.LeadSuit,
                player.Bid, player.TricksWon, state.TricksInRound - state.CurrentTrick + 1);
            StartCoroutine(After(0.5f, () => _view.SubmitHumanCard(card)));
        }

        private void ChooseTigress(PlayerData player)
        {
            var mode = _decisions.DecideTigressMode(player.Bid, player.TricksWon,
                _view.State.TricksInRound - _view.State.CurrentTrick + 1);
            StartCoroutine(After(0.5f, () => _view.SubmitTigressChoice(mode)));
        }

        private void Confirm(RoundScoreResult[] scores)
            => StartCoroutine(After(_confirmDelay, () => _view.SubmitRoundConfirm()));

        private IEnumerator After(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (_view != null && _view.IsGameRunning) action();
        }

        private void Status(string message)
        {
            if (!_view.IsGameRunning) StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (_view == null) return;
            _view.OnBidRequested -= Bid;
            _view.OnCardPlayRequested -= Play;
            _view.OnTigressChoiceRequested -= ChooseTigress;
            _view.OnRoundEnded -= Confirm;
            _view.OnNetworkStatusChanged -= Status;
        }
    }
}

