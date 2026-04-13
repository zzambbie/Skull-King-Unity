// ============================================================
// UIBindingManager.cs — UI 바인딩 매니저
// GameManager의 이벤트를 구독하여 씬의 UI 오브젝트를 갱신합니다.
// 모든 UI 오브젝트는 [SerializeField]로 에디터에서 연결합니다.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SkullKing.Data;
using SkullKing.Logic;
using SkullKing.Core;

namespace SkullKing.UI
{
    /// <summary>
    /// GameManager의 이벤트를 구독하여 씬의 UI를 갱신하는 브릿지 역할.
    /// 모든 UI 오브젝트는 회원님이 씬에 배치 후 Inspector에서 드래그 연결합니다.
    /// </summary>
    public class UIBindingManager : MonoBehaviour
    {
        // ============================================================
        // [SerializeField] — 회원님이 연결할 UI 참조들
        // ============================================================

        [Header("=== 필수 참조 ===")]
        [SerializeField] private GameManager _gameManager;

        [Header("=== 페이즈 표시 ===")]
        [Tooltip("현재 게임 페이즈를 표시할 텍스트")]
        [SerializeField] private Text _phaseText;

        [Tooltip("현재 라운드/트릭 정보 텍스트")]
        [SerializeField] private Text _roundInfoText;

        [Header("=== 비딩 패널 ===")]
        [Tooltip("비딩 패널 루트 오브젝트 (활성화/비활성화 용)")]
        [SerializeField] private GameObject _bidPanel;

        [Tooltip("비딩 수 입력 필드 또는 표시 텍스트")]
        [SerializeField] private Text _bidValueText;

        [Tooltip("비딩 +1 버튼")]
        [SerializeField] private Button _bidUpButton;

        [Tooltip("비딩 -1 버튼")]
        [SerializeField] private Button _bidDownButton;

        [Tooltip("비딩 확정 버튼")]
        [SerializeField] private Button _bidConfirmButton;

        [Header("=== 타이그리스 선택 패널 ===")]
        [Tooltip("타이그리스 선택 패널 (해적/도주)")]
        [SerializeField] private GameObject _tigressPanel;

        [Tooltip("해적으로 선택 버튼")]
        [SerializeField] private Button _tigressPirateButton;

        [Tooltip("도주로 선택 버튼")]
        [SerializeField] private Button _tigressEscapeButton;

        [Header("=== 카드 영역 ===")]
        [Tooltip("인간 플레이어의 카드를 배치할 부모 Transform")]
        [SerializeField] private Transform _playerHandArea;

        [Tooltip("트릭에 제출된 카드를 표시할 영역")]
        [SerializeField] private Transform _trickArea;

        [Tooltip("카드 프리팹 (회원님이 제작)")]
        [SerializeField] private GameObject _cardPrefab;

        [Header("=== 스코어보드 ===")]
        [Tooltip("각 플레이어의 점수/비딩 정보를 표시할 텍스트 배열 (최대 6명)")]
        [SerializeField] private Text[] _playerInfoTexts;

        [Header("=== 결과 표시 ===")]
        [Tooltip("트릭 결과 표시 텍스트")]
        [SerializeField] private Text _trickResultText;

        [Tooltip("라운드 결과 패널")]
        [SerializeField] private GameObject _roundResultPanel;

        [Tooltip("라운드 결과 상세 텍스트")]
        [SerializeField] private Text _roundResultText;

        [Tooltip("라운드 결과 '다음' 버튼")]
        [SerializeField] private Button _roundResultNextButton;

        [Tooltip("게임 종료 패널")]
        [SerializeField] private GameObject _gameEndPanel;

        [Tooltip("게임 종료 결과 텍스트")]
        [SerializeField] private Text _gameEndText;

        // ============================================================
        // 내부 상태
        // ============================================================

        private int _currentBidValue = 0;
        private int _maxBidValue = 0;
        private List<CardData> _currentPlayableCards = new List<CardData>();
        private List<GameObject> _handCardObjects = new List<GameObject>();
        private List<GameObject> _trickCardObjects = new List<GameObject>();

        // ============================================================
        // 초기화
        // ============================================================

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            }

            // 패널 초기 비활성화
            SetActive(_bidPanel, false);
            SetActive(_tigressPanel, false);
            SetActive(_roundResultPanel, false);
            SetActive(_gameEndPanel, false);

            if (_roundResultText != null)
            {
                _roundResultText.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void OnEnable()
        {
            if (_gameManager == null) return;

            // 이벤트 구독
            _gameManager.OnPhaseChanged += HandlePhaseChanged;
            _gameManager.OnRoundStarted += HandleRoundStarted;
            _gameManager.OnCardsDealt += HandleCardsDealt;
            _gameManager.OnBidRequested += HandleBidRequested;
            _gameManager.OnPlayerBid += HandlePlayerBid;
            _gameManager.OnAllBidsComplete += HandleAllBidsComplete;
            _gameManager.OnTrickStarted += HandleTrickStarted;
            _gameManager.OnCardPlayRequested += HandleCardPlayRequested;
            _gameManager.OnTigressChoiceRequested += HandleTigressChoiceRequested;
            _gameManager.OnCardPlayed += HandleCardPlayed;
            _gameManager.OnTrickResolved += HandleTrickResolved;
            _gameManager.OnRoundEnded += HandleRoundEnded;
            _gameManager.OnGameEnded += HandleGameEnded;
            _gameManager.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;

            // 버튼 연결
            if (_bidUpButton) _bidUpButton.onClick.AddListener(OnBidUp);
            if (_bidDownButton) _bidDownButton.onClick.AddListener(OnBidDown);
            if (_bidConfirmButton) _bidConfirmButton.onClick.AddListener(OnBidConfirm);
            if (_tigressPirateButton) _tigressPirateButton.onClick.AddListener(OnTigressPirate);
            if (_tigressEscapeButton) _tigressEscapeButton.onClick.AddListener(OnTigressEscape);
            if (_roundResultNextButton) _roundResultNextButton.onClick.AddListener(OnRoundResultNext);
        }

        private void OnDisable()
        {
            if (_gameManager == null) return;

            _gameManager.OnPhaseChanged -= HandlePhaseChanged;
            _gameManager.OnRoundStarted -= HandleRoundStarted;
            _gameManager.OnCardsDealt -= HandleCardsDealt;
            _gameManager.OnBidRequested -= HandleBidRequested;
            _gameManager.OnPlayerBid -= HandlePlayerBid;
            _gameManager.OnAllBidsComplete -= HandleAllBidsComplete;
            _gameManager.OnTrickStarted -= HandleTrickStarted;
            _gameManager.OnCardPlayRequested -= HandleCardPlayRequested;
            _gameManager.OnTigressChoiceRequested -= HandleTigressChoiceRequested;
            _gameManager.OnCardPlayed -= HandleCardPlayed;
            _gameManager.OnTrickResolved -= HandleTrickResolved;
            _gameManager.OnRoundEnded -= HandleRoundEnded;
            _gameManager.OnGameEnded -= HandleGameEnded;
            _gameManager.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;

            if (_bidUpButton) _bidUpButton.onClick.RemoveListener(OnBidUp);
            if (_bidDownButton) _bidDownButton.onClick.RemoveListener(OnBidDown);
            if (_bidConfirmButton) _bidConfirmButton.onClick.RemoveListener(OnBidConfirm);
            if (_tigressPirateButton) _tigressPirateButton.onClick.RemoveListener(OnTigressPirate);
            if (_tigressEscapeButton) _tigressEscapeButton.onClick.RemoveListener(OnTigressEscape);
            if (_roundResultNextButton) _roundResultNextButton.onClick.RemoveListener(OnRoundResultNext);
        }

        // ============================================================
        // 이벤트 핸들러 — GameManager 이벤트 수신
        // ============================================================

        private void HandlePhaseChanged(GamePhase phase)
        {
            SetText(_phaseText, GetPhaseDisplayName(phase));
        }

        private void HandleRoundStarted(int roundNumber)
        {
            // 선턴 플레이어 정보 표시
            var leader = _gameManager.State.Players[_gameManager.State.RoundStartPlayerIndex];
            SetText(_roundInfoText, $"라운드 {roundNumber}  ┃  선턴: {leader.Name}");
            SetActive(_roundResultPanel, false);
            UpdateScoreboard();
        }

        private void HandleCardsDealt()
        {
            // 인간 플레이어의 손패를 UI에 표시
            RefreshPlayerHand();
        }

        private void HandleBidRequested(PlayerData player, int maxBid)
        {
            _currentBidValue = 0;
            _maxBidValue = maxBid;
            UpdateBidDisplay();
            SetActive(_bidPanel, true);

            // 선턴 정보를 라운드 정보에 지속 표시
            var leader = _gameManager.State.Players[_gameManager.State.RoundStartPlayerIndex];
            SetText(_roundInfoText,
                $"라운드 {_gameManager.State.CurrentRound}  ┃  선턴: {leader.Name}");
        }

        private void HandlePlayerBid(PlayerData player)
        {
            UpdateScoreboard();
        }

        private void HandleAllBidsComplete()
        {
            SetActive(_bidPanel, false);
            UpdateScoreboard();
        }

        private void HandleTrickStarted(int trickNumber, int leaderIndex)
        {
            SetText(_roundInfoText,
                $"라운드 {_gameManager.State.CurrentRound} — 트릭 {trickNumber}");
            SetText(_trickResultText, "");
            ClearTrickArea();
        }

        private void HandleCardPlayRequested(PlayerData player, List<CardData> playableCards)
        {
            _currentPlayableCards = playableCards;
            RefreshPlayerHand(); // 플레이 가능한 카드 하이라이트
        }

        private void HandleTigressChoiceRequested(PlayerData player)
        {
            SetActive(_tigressPanel, true);
        }

        private void HandleCardPlayed(PlayedCard playedCard)
        {
            // 트릭 영역에 카드 표시
            SpawnCardInTrickArea(playedCard);

            // 인간 플레이어의 카드였다면 손패 갱신
            var player = _gameManager.State.Players[playedCard.PlayerIndex];
            if (player.IsHuman)
            {
                _currentPlayableCards.Clear();
                RefreshPlayerHand();
            }

            UpdateScoreboard();
        }

        private void HandleTrickResolved(TrickResult result)
        {
            if (result.IsVoided)
            {
                SetText(_trickResultText, "크라켄! 트릭 무효!");
            }
            else
            {
                var winner = _gameManager.State.Players[result.WinnerIndex];
                string msg = $"승자: {winner.Name}";
                if (result.BonusPoints > 0)
                    msg += $"\n{result.BonusDescription}";
                SetText(_trickResultText, msg);
            }

            UpdateScoreboard();
        }

        private void HandleRoundEnded(RoundScoreResult[] results)
        {
            int pCount = results.Length;
            string topSpacing = pCount <= 4 ? "\n\n\n" : "\n";
            string itemSpacing = pCount <= 4 ? "\n\n" : "\n";

            // 라운드 결과 패널 표시 (동적 간격)
            string text = $"{topSpacing}═══  라운드 {_gameManager.State.CurrentRound} 결과  ═══{topSpacing}";

            for (int i = 0; i < results.Length; i++)
            {
                var r = results[i];
                var p = _gameManager.State.Players[i];
                string marker = p.IsHuman ? "▶ " : "  ";
                string bidResult = (r.RoundTotal >= 0) ? $"+{r.RoundTotal}" : $"{r.RoundTotal}";
                text += $"{marker}{p.Name}  |  누적: {p.TotalScore}점  |  (라운드: {bidResult})\n";
                text += $"    비딩: {p.Bid}  |  승리: {p.TricksWon}  |  {r.Description}{itemSpacing}";
            }

            SetText(_roundResultText, text);
            SetActive(_roundResultPanel, true);
            UpdateScoreboard();
        }

        private void HandleGameEnded(PlayerData winner)
        {
            string text = $"★ 게임 종료! ★\n\n";
            text += $"우승: {winner.Name} ({winner.TotalScore}점)\n\n";

            // 전체 순위 표시
            var players = _gameManager.State.Players;
            var sorted = new List<PlayerData>(players);
            sorted.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

            for (int i = 0; i < sorted.Count; i++)
            {
                text += $"{i + 1}위. {sorted[i].Name}: {sorted[i].TotalScore}점\n";
            }

            SetText(_gameEndText, text);
            SetActive(_gameEndPanel, true);
        }

        private void HandleCurrentPlayerChanged(int playerIndex)
        {
            // 현재 턴 플레이어 하이라이트 (스코어보드에서 강조)
            UpdateScoreboard();
        }

        // ============================================================
        // 버튼 콜백 — UI → GameManager
        // ============================================================

        private void OnBidUp()
        {
            if (_currentBidValue < _maxBidValue)
            {
                _currentBidValue++;
                UpdateBidDisplay();
            }
        }

        private void OnBidDown()
        {
            if (_currentBidValue > 0)
            {
                _currentBidValue--;
                UpdateBidDisplay();
            }
        }

        private void OnBidConfirm()
        {
            SetActive(_bidPanel, false);
            _gameManager.SubmitHumanBid(_currentBidValue);
        }

        private void OnTigressPirate()
        {
            SetActive(_tigressPanel, false);
            _gameManager.SubmitTigressChoice(TigressChoice.Pirate);
        }

        private void OnTigressEscape()
        {
            SetActive(_tigressPanel, false);
            _gameManager.SubmitTigressChoice(TigressChoice.Escape);
        }

        private void OnRoundResultNext()
        {
            SetActive(_roundResultPanel, false);
            _gameManager.SubmitRoundConfirm();
        }

        /// <summary>
        /// 카드가 클릭되었을 때 호출됩니다.
        /// CardUI 컴포넌트에서 호출합니다.
        /// </summary>
        public void OnCardClicked(CardData card)
        {
            // 현재 카드 제출 대기 중이고, 해당 카드가 플레이 가능한 카드인지 확인
            if (_currentPlayableCards.Count == 0) return;

            bool isPlayable = false;
            for (int i = 0; i < _currentPlayableCards.Count; i++)
            {
                if (_currentPlayableCards[i].Id == card.Id)
                {
                    isPlayable = true;
                    break;
                }
            }

            if (!isPlayable)
            {
                Debug.Log("[UI] 이 카드는 지금 낼 수 없습니다.");
                return;
            }

            _currentPlayableCards.Clear();
            _gameManager.SubmitHumanCard(card);
        }

        // ============================================================
        // UI 갱신 메서드
        // ============================================================

        private void UpdateBidDisplay()
        {
            SetText(_bidValueText, _currentBidValue.ToString());
        }

        private bool _layoutFixed = false;

        private void UpdateScoreboard()
        {
            if (_gameManager.State == null || _gameManager.State.Players == null) return;
            int playerCount = _gameManager.State.Players.Length;

            if (!_layoutFixed)
            {
                // 인원수에 맞게 완벽한 시계방향 배치를 위한 앵커 동적 할당
                Vector2[] minAnchors = {
                    new Vector2(0.01f, 0.01f), // 0: Bottom
                    new Vector2(0.01f, 0.55f), // 1: Left
                    new Vector2(0.01f, 0.78f), // 2: TopLeft
                    new Vector2(0.38f, 0.83f), // 3: Top
                    new Vector2(0.82f, 0.78f), // 4: TopRight
                    new Vector2(0.85f, 0.55f)  // 5: Right
                };
                Vector2[] maxAnchors = {
                    new Vector2(0.18f, 0.13f), // 0: Bottom
                    new Vector2(0.15f, 0.72f), // 1: Left
                    new Vector2(0.18f, 0.92f), // 2: TopLeft
                    new Vector2(0.62f, 0.93f), // 3: Top
                    new Vector2(0.99f, 0.92f), // 4: TopRight
                    new Vector2(0.99f, 0.72f)  // 5: Right
                };

                int[] mapping = new int[playerCount];
                if (playerCount == 4) mapping = new int[] { 0, 1, 3, 5 };
                else if (playerCount == 5) mapping = new int[] { 0, 1, 2, 4, 5 };
                else if (playerCount == 6) mapping = new int[] { 0, 1, 2, 3, 4, 5 };
                else if (playerCount == 3) mapping = new int[] { 0, 1, 5 };
                else if (playerCount == 2) mapping = new int[] { 0, 3 };

                for (int i = 0; i < playerCount; i++)
                {
                    if (i < _playerInfoTexts.Length && _playerInfoTexts[i] != null)
                    {
                        var panelRect = _playerInfoTexts[i].transform.parent.GetComponent<RectTransform>();
                        if (panelRect != null)
                        {
                            panelRect.anchorMin = minAnchors[mapping[i]];
                            panelRect.anchorMax = maxAnchors[mapping[i]];
                            panelRect.offsetMin = Vector2.zero;
                            panelRect.offsetMax = Vector2.zero;
                        }
                    }
                }
                _layoutFixed = true;
            }

            for (int i = 0; i < playerCount && i < _playerInfoTexts.Length; i++)
            {
                var p = _gameManager.State.Players[i];
                if (_playerInfoTexts[i] == null) continue;

                string arrow = (i == _gameManager.State.CurrentPlayerIndex) ? "▶ " : "  ";
                string leaderMark = (i == _gameManager.State.LeaderIndex) ? " ★선" : "";
                string bidInfo = p.HasBid ? $"비딩:{p.Bid} 승리:{p.TricksWon}" : "비딩 대기";
                _playerInfoTexts[i].text = $"{arrow}{p.Name}{leaderMark}  |  {bidInfo}  |  {p.TotalScore}점";
            }

            // 사용되지 않는 슬롯 비우기
            for (int i = _gameManager.State.Players.Length; i < _playerInfoTexts.Length; i++)
            {
                if (_playerInfoTexts[i] != null)
                    _playerInfoTexts[i].text = "";
            }
        }

        /// <summary>
        /// 인간 플레이어의 손패를 UI에 다시 그립니다.
        /// </summary>
        private void RefreshPlayerHand()
        {
            // 기존 카드 오브젝트 제거
            ClearHandArea();

            if (_gameManager.State == null) return;

            // 인간 플레이어 찾기
            PlayerData human = null;
            for (int i = 0; i < _gameManager.State.Players.Length; i++)
            {
                if (_gameManager.State.Players[i].IsHuman)
                {
                    human = _gameManager.State.Players[i];
                    break;
                }
            }
            if (human == null || _cardPrefab == null || _playerHandArea == null) return;

            // 카드 오브젝트 생성
            for (int i = 0; i < human.Hand.Count; i++)
            {
                var card = human.Hand[i];
                var cardObj = Instantiate(_cardPrefab, _playerHandArea);
                cardObj.transform.localScale = Vector3.one;

                // CardUI 컴포넌트가 있으면 데이터 설정
                var cardUI = cardObj.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    bool isPlayable = IsCardPlayable(card);
                    cardUI.Setup(card, isPlayable, this);
                }

                _handCardObjects.Add(cardObj);
            }
        }

        private void ClearHandArea()
        {
            for (int i = 0; i < _handCardObjects.Count; i++)
            {
                if (_handCardObjects[i] != null)
                    Destroy(_handCardObjects[i]);
            }
            _handCardObjects.Clear();
        }

        private void ClearTrickArea()
        {
            for (int i = 0; i < _trickCardObjects.Count; i++)
            {
                if (_trickCardObjects[i] != null)
                    Destroy(_trickCardObjects[i]);
            }
            _trickCardObjects.Clear();
        }

        private void SpawnCardInTrickArea(PlayedCard playedCard)
        {
            if (_cardPrefab == null || _trickArea == null) return;

            var cardObj = Instantiate(_cardPrefab, _trickArea);
            cardObj.transform.localScale = Vector3.one;

            var cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(playedCard.Card, false, this);
                // 누가 냈는지 라벨 표시
                cardUI.SetOwnerLabel(_gameManager.State.Players[playedCard.PlayerIndex].Name);
            }

            _trickCardObjects.Add(cardObj);
        }

        private bool IsCardPlayable(CardData card)
        {
            for (int i = 0; i < _currentPlayableCards.Count; i++)
            {
                if (_currentPlayableCards[i].Id == card.Id) return true;
            }
            return false;
        }

        // ============================================================
        // 유틸리티
        // ============================================================

        private void SetText(Text textComponent, string value)
        {
            if (textComponent != null)
                textComponent.text = value;
        }

        private void SetActive(GameObject obj, bool active)
        {
            if (obj != null)
                obj.SetActive(active);
        }

        private string GetPhaseDisplayName(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Lobby => "로비",
                GamePhase.Dealing => "카드 분배 중...",
                GamePhase.Bidding => "비딩",
                GamePhase.Playing => "카드 제출",
                GamePhase.TrickResolve => "판정 중...",
                GamePhase.RoundEnd => "라운드 종료",
                GamePhase.GameEnd => "게임 종료!",
                _ => phase.ToString()
            };
        }
    }
}
