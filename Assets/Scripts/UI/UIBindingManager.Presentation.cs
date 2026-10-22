using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkullKing.UI
{
    public partial class UIBindingManager
    {
        private readonly Queue<Func<IEnumerator>> _presentations = new Queue<Func<IEnumerator>>();
        private Coroutine _presentationRoutine;
        private bool _presentationBusy;

        private void EnqueuePresentation(Func<IEnumerator> presentation)
        {
            if (!isActiveAndEnabled) return;
            _presentations.Enqueue(presentation);
            if (_presentationBusy) return;
            _presentationBusy = true;
            if (_gameManager != null) _gameManager.SetPresentationHold(this, true);
            _presentationRoutine = StartCoroutine(DrainPresentations());
        }

        private IEnumerator DrainPresentations()
        {
            try
            {
                while (_presentations.Count > 0)
                    yield return _presentations.Dequeue()();
            }
            finally
            {
                _presentationBusy = false;
                if (_gameManager != null) _gameManager.SetPresentationHold(this, false);
                _presentationRoutine = null;
                _cardInputUnlockTime = Mathf.Max(_cardInputUnlockTime, Time.time + 0.15f);
            }
        }

        private static IEnumerator PresentInstant(Action action)
        {
            action();
            yield break;
        }

        private void ApplyPresentedWinner(GameObject winner)
        {
            if (winner == null) return;
            _currentTopCardObj = winner;
            winner.SetActive(true);
            winner.transform.SetAsLastSibling();
            RepositionTrickCards();
            UpdateTrickHistoryLeader();
            // Repositioning must never lift a card above the closing wing curtain.
            if (_whaleWingTransition != null) _whaleWingTransition.KeepOnTop();
        }

        private void CancelPresentations()
        {
            if (_presentationRoutine != null) StopCoroutine(_presentationRoutine);
            _presentationRoutine = null;
            _presentations.Clear();
            _presentationBusy = false;
            if (_gameManager != null) _gameManager.SetPresentationHold(this, false);
            _customAnimatingCards.Clear();
            if (_whaleWingTransition != null) _whaleWingTransition.Cancel();
            _cardInputUnlockTime = 0f;
        }

        private void UpdateTrickHistoryLeader()
        {
            if (_trickHistory == null || _currentTrickCards.Count == 0) return;
            var result = SkullKing.Logic.TrickResolver.Resolve(_currentTrickCards,
                SkullKing.Logic.TrickResolver.DetermineLeadSuit(_currentTrickCards));
            _trickHistory.UpdateLeader(TrickPresentationRules.WinnerCardIndex(_currentTrickCards), result.IsVoided);
        }
    }
}
