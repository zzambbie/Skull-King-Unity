using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SkullKing.Data;

namespace SkullKing.UI
{
    public sealed class TrickHistoryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private Button _currentButton;
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _toggleLabel;
        [SerializeField] private RectTransform _drawer;
        [SerializeField] private CanvasGroup _drawerGroup;
        [SerializeField] private float _slideDuration = .28f;
        [SerializeField, HideInInspector] private int _layoutVersion;
        [SerializeField] private TrickHistoryEntry[] _entries;
        [SerializeField] private CardSpriteDatabase _database;
        private readonly PublicTrickHistory _history = new PublicTrickHistory();
        private int _selected = -1;
        private bool _open;
        private Coroutine _slide;

        private void Awake() { SetDrawerPosition(0); Render(); }
        private void OnDisable()
        {
            if (_slide != null) StopCoroutine(_slide);
            _slide = null;
            _open = false;
            SetDrawerPosition(0);
        }
        public void BeginRound(int round)
        {
            if (_history.Round == round) return;
            _history.BeginRound(round);
            _selected = -1;
            Render();
        }
        public void BeginTrick(int round, int number)
        {
            BeginRound(round);
            _history.Begin(round, number);
            if (_entries != null) foreach (var entry in _entries) if (entry != null) entry.ResetEntry();
            Render();
        }
        public void AddSubmitted(PlayedCard played, string owner)
        {
            if (_history.Add(played, owner)) Render();
        }
        public void UpdateLeader(int index, bool voided)
        {
            _history.SetLeader(index, voided);
            Render();
        }
        public void ShowCurrent() { _selected = -1; Render(); }
        public void ShowPrevious()
        {
            if (SelectedIndex <= 0) return;
            _selected = SelectedIndex - 1;
            Render();
        }
        public void ShowNext()
        {
            if (SelectedIndex >= _history.Tricks.Count - 1) return;
            _selected = SelectedIndex + 1;
            if (_selected == _history.Tricks.Count - 1) _selected = -1;
            Render();
        }
        private int SelectedIndex => _selected < 0 ? _history.Tricks.Count - 1 : Mathf.Min(_selected, _history.Tricks.Count - 1);
        public void ToggleDrawer()
        {
            _open = !_open;
            if (_slide != null) StopCoroutine(_slide);
            _slide = StartCoroutine(Slide());
        }
        private IEnumerator Slide()
        {
            if (_drawer == null) yield break;
            float from = _drawerGroup != null ? _drawerGroup.alpha : (_open ? 0 : 1);
            float to = _open ? 1 : 0;
            for (float elapsed = 0; elapsed < _slideDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.01f, _slideDuration));
                SetDrawerPosition(Mathf.Lerp(from, to, t), true);
                yield return null;
            }
            SetDrawerPosition(to);
            _slide = null;
        }
        private void SetDrawerPosition(float amount, bool animating = false)
        {
            if (_drawer == null) return;
            _drawer.anchoredPosition = new Vector2(0, (1 - amount) * (_drawer.rect.height + 60));
            if (_drawerGroup != null)
            {
                _drawerGroup.alpha = amount;
                _drawerGroup.interactable = _open;
                _drawerGroup.blocksRaycasts = _open;
            }
            _drawer.gameObject.SetActive(animating || amount > 0);
            if (_toggleLabel != null) _toggleLabel.text = _open ? "▲ 기록 접기" : "▼ 제출 기록";
        }
        private void Render()
        {
            int index = SelectedIndex;
            var trick = index >= 0 ? _history.Tricks[index] : _history.Current;
            bool historical = index >= 0 && index < _history.Tricks.Count - 1;
            if (_title != null) _title.text = trick.Number > 0
                ? trick.Round + "라운드 · " + trick.Number + "번째 판 (" + (index + 1) + "/" + _history.Tricks.Count + ")"
                : _history.Round + "라운드 · 아직 제출 기록이 없습니다";
            if (_hint != null) _hint.text = trick.Voided ? "종말 · 트릭 무효" : "제출 순서 · 마우스/Alt로 확인";
            if (_currentButton != null) _currentButton.interactable = historical;
            if (_previousButton != null) _previousButton.interactable = index > 0;
            if (_nextButton != null) _nextButton.interactable = historical;
            if (_entries == null) return;
            for (int i = 0; i < _entries.Length; i++)
            {
                var slot = _entries[i];
                if (slot == null) continue;
                bool visible = i < trick.Entries.Count;
                slot.gameObject.SetActive(visible);
                if (visible) slot.Bind(trick.Entries[i], i, i == trick.LeaderIndex, trick.Voided, _database, historical);
            }
        }
    }
}
