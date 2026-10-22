using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using SkullKing.Data;

namespace SkullKing.UI
{
    public sealed class TrickHistoryEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private CardUI _card;
        [SerializeField] private TMP_Text _owner;
        [SerializeField] private TMP_Text _state;
        [SerializeField] private TMP_Text _inspectionHint;
        [SerializeField, Range(.3f, 1f)] private float _cardScale = .65f;
        private bool _bound;
        private int _cardId;
        private TigressChoice _mode;
        private string _details;
        private string _idleHint;

        public void Bind(PublicTrickHistory.Entry entry, int order, bool leader, bool voided, CardSpriteDatabase database, bool previous)
        {
            _details = (order + 1) + ". " + entry.owner + " · " + entry.played.Card.DisplayName;
            if (entry.played.Card.Special == SpecialType.Tigress)
                _details += entry.played.TigressMode == TigressChoice.Pirate ? " (해적 선택)" : " (항복 선택)";
            _idleHint = voided ? "크라켄 · 트릭 무효" : "제출 순서 · 마우스/Alt로 확인";
            if (_owner != null)
            {
                _owner.richText = false;
                _owner.text = (order + 1) + ". " + entry.owner;
            }
            if (_card != null && (!_bound || _cardId != entry.played.Card.Id || _mode != entry.played.TigressMode))
            {
                _card.Setup(entry.played.Card, false, null);
                if (database != null)
                {
                    _card.SetIllustration(database.GetSpriteForCard(entry.played.Card));
                    _card.SetComposedArtwork(database.IsComposedArtwork(entry.played.Card));
                }
                if (entry.played.Card.Special == SpecialType.Tigress) _card.SetTigressMode(entry.played.TigressMode);
                _card.SetAsReadOnlyInspection(Vector3.one * _cardScale);
                _cardId = entry.played.Card.Id;
                _mode = entry.played.TigressMode;
                _bound = true;
            }
            if (_state != null)
            {
                string mode = entry.played.Card.Special == SpecialType.Tigress
                    ? (entry.played.TigressMode == TigressChoice.Pirate ? "해적" : "항복") : "";
                _state.text = voided ? "무효" : leader ? (previous ? "승리 카드" : "현재 선두") : mode;
                _state.color = leader && !voided ? new Color(1f, .83f, .35f) : new Color(.8f, .83f, .88f);
            }
        }
        public void ResetEntry()
        {
            _bound = false;
            gameObject.SetActive(false);
        }
        public void OnPointerEnter(PointerEventData data)
        { if (_inspectionHint != null) _inspectionHint.text = _details; }
        public void OnPointerExit(PointerEventData data)
        { if (_inspectionHint != null) _inspectionHint.text = _idleHint; }
    }
}
