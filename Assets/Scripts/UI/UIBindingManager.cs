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

        [Tooltip("카드 일러스트 데이터베이스 (Assets > Create > SkullKing > Card Sprite Database)")]
        [SerializeField] private CardSpriteDatabase _spriteDatabase;

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
        private bool _trickCardsDirty = false;

        // ============================================================
        // 프론트뷰 동적 UI 요소
        // ============================================================

        private PlayerProfilePanel[] _profilePanels;
        private OpponentHandDisplay[] _opponentDisplays;
        private bool _frontViewInitialized = false;

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

            // 영역의 Layout 컴포넌트 제거 (수동 배치 사용)
            DestroyAreaLayoutComponents(_trickArea);
            DestroyAreaLayoutComponents(_playerHandArea);
        }

        /// <summary>
        /// 매 프레임 트릭 카드 위치를 강제 적용합니다.
        /// Unity 레이아웃 시스템보다 늦게 실행되어 위치를 확실히 보장합니다.
        /// </summary>
        private void LateUpdate()
        {
            if (_trickCardObjects.Count > 0)
            {
                RepositionTrickCards();
            }

            // 손패 카드도 매 프레임 위치 강제 — Unity 레이아웃 시스템 간섭 완전 차단
            if (_handCardObjects.Count > 0)
            {
                RepositionHandCards();
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
            // 프론트뷰 초기화 (첫 라운드에서만)
            if (!_frontViewInitialized)
            {
                SetupFrontViewLayout();
            }

            // 인간 플레이어의 손패를 UI에 표시
            RefreshPlayerHand();

            // 상대 플레이어 카드 수 갱신
            RefreshOpponentHands();
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

            // 상대 카드 수 갱신
            RefreshOpponentHands();
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

            // ---- 프론트뷰 프로필 패널 갱신 ----
            UpdateProfilePanels();
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
                cardObj.transform.localScale = Vector3.one * 1.1f;

                // LayoutElement 즉시 제거 — 레이아웃 간섭 원천 차단
                var layoutElem = cardObj.GetComponent<LayoutElement>();
                if (layoutElem != null) DestroyImmediate(layoutElem);

                // CardUI 컴포넌트가 있으면 데이터 설정
                var cardUI = cardObj.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    bool isPlayable = IsCardPlayable(card);
                    cardUI.Setup(card, isPlayable, this);

                    // 일러스트 자동 적용
                    if (_spriteDatabase != null)
                    {
                        var sprite = _spriteDatabase.GetSpriteForCard(card);
                        cardUI.SetIllustration(sprite);
                    }

                    // 손패 호버 효과 활성화 (실제 스케일과 일치시킴)
                    cardUI.SetAsHandCard(Vector3.one * 1.1f, i);
                }

                _handCardObjects.Add(cardObj);
            }

            // 즉시 배치 (LateUpdate에서도 매 프레임 강제 적용됨)
            RepositionHandCards();
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
            cardObj.transform.localScale = Vector3.one * 0.75f;

            // LayoutElement 즉시 제거 — 레이아웃 간섭 원천 차단
            var layoutElem = cardObj.GetComponent<LayoutElement>();
            if (layoutElem != null) DestroyImmediate(layoutElem);

            var cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(playedCard.Card, false, this);
                // 누가 냈는지 라벨 표시
                cardUI.SetOwnerLabel(_gameManager.State.Players[playedCard.PlayerIndex].Name);

                // 타이그리스 모드 표시 (모든 플레이어가 용사/항복 선택을 볼 수 있도록)
                if (playedCard.Card.Special == SpecialType.Tigress)
                {
                    cardUI.SetTigressMode(playedCard.TigressMode);
                }

                // 일러스트 자동 적용
                if (_spriteDatabase != null)
                {
                    var sprite = _spriteDatabase.GetSpriteForCard(playedCard.Card);
                    cardUI.SetIllustration(sprite);
                }
            }

            _trickCardObjects.Add(cardObj);

            // 기존 카드는 새 카드가 추가됨에 따라 위치 재계산
            RepositionTrickCards();

            // 새로 추가된 카드는 애니메이션으로 등장 (방사형 위치)
            if (cardUI != null)
            {
                Vector2 targetPos = GetTrickCardPosition(playedCard.PlayerIndex);
                Vector2 startOffset = GetCardEntryOffset(playedCard.PlayerIndex);
                cardUI.PlayEntryAnimation(targetPos, Vector3.one * 0.75f, startOffset);
            }

            _trickCardsDirty = true;
        }

        /// <summary>
        /// 플레이어 인덱스에 해당하는 패널 위치에서 트릭 영역 방향으로의 시작 오프셋을 반환합니다.
        /// 각 슬롯 위치:
        ///   0: Bottom (아래), 1: Left (왼쪽), 2: TopLeft (왼쪽위),
        ///   3: Top (위), 4: TopRight (오른쪽위), 5: Right (오른쪽)
        /// </summary>
        private Vector2 GetCardEntryOffset(int playerIndex)
        {
            int playerCount = _gameManager.State.Players.Length;

            // UpdateScoreboard과 동일한 슬롯 매핑
            int[] mapping;
            if (playerCount == 6) mapping = new int[] { 0, 1, 2, 3, 4, 5 };
            else if (playerCount == 5) mapping = new int[] { 0, 1, 2, 4, 5 };
            else if (playerCount == 4) mapping = new int[] { 0, 1, 3, 5 };
            else if (playerCount == 3) mapping = new int[] { 0, 1, 5 };
            else mapping = new int[] { 0, 3 };  // 2인

            int slot = (playerIndex < mapping.Length) ? mapping[playerIndex] : 3;

            // 슬롯별 방향 벡터 (트릭 영역 중앙 기준)
            switch (slot)
            {
                case 0: return new Vector2(0, -250f);       // Bottom → 아래에서 올라옴
                case 1: return new Vector2(-350f, 0);       // Left → 왼쪽에서
                case 2: return new Vector2(-250f, 200f);    // TopLeft → 왼쪽 위에서
                case 3: return new Vector2(0, 250f);        // Top → 위에서
                case 4: return new Vector2(250f, 200f);     // TopRight → 오른쪽 위에서
                case 5: return new Vector2(350f, 0);        // Right → 오른쪽에서
                default: return new Vector2(0, 200f);
            }
        }

        /// <summary>
        /// 지정된 영역의 모든 Layout 관련 컴포넌트를 제거합니다.
        /// enabled=false로 즉시 비활성화 + Destroy로 완전 제거를 병행합니다.
        /// </summary>
        private void DestroyAreaLayoutComponents(Transform area)
        {
            if (area == null) return;

            // 모든 레이아웃 그룹 타입 처리 (DestroyImmediate로 즉시 제거)
            var hlg = area.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) { hlg.enabled = false; DestroyImmediate(hlg); }

            var vlg = area.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) { vlg.enabled = false; DestroyImmediate(vlg); }

            var glg = area.GetComponent<GridLayoutGroup>();
            if (glg != null) { glg.enabled = false; DestroyImmediate(glg); }

            var csf = area.GetComponent<ContentSizeFitter>();
            if (csf != null) { csf.enabled = false; DestroyImmediate(csf); }
        }

        // ============================================================
        // 손패 배치 — 프론트뷰 스타일 (3D 원근감은 부모 X회전으로 처리)
        // ============================================================

        /// <summary>
        /// 손패 영역의 카드를 정갈한 가로 배치합니다.
        /// 부모 Transform의 X축 회전이 3D 원근감을 처리합니다.
        /// </summary>
        private void RepositionHandCards()
        {
            int count = _handCardObjects.Count;
            if (count == 0) return;

            float cardWidth = 140f;
            float cardHeight = 200f;
            float overlap = -15f; // 카드 격치는 정도
            float totalWidth = count * cardWidth + (count - 1) * overlap;
            float startX = -totalWidth / 2f + cardWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                if (_handCardObjects[i] == null) continue;

                // 호버 중인 카드는 건너뛰 — 호버 효과와 충돌 방지
                var cardUI = _handCardObjects[i].GetComponent<CardUI>();
                if (cardUI != null && cardUI.IsHovered) continue;

                var rt = _handCardObjects[i].GetComponent<RectTransform>();
                if (rt == null) continue;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.anchoredPosition = new Vector2(startX + i * (cardWidth + overlap), 0);
                rt.localEulerAngles = Vector3.zero; // 개별 카드는 회전 없음 (부모 X회전이 3D 처리)

                // 원본 위치 캐시 (호버 복원용)
                if (cardUI != null)
                {
                    cardUI.CaptureOriginalPosition();
                }
            }
        }

        // ============================================================
        // 트릭 영역 방사형 배치 — 플레이어 위치 기반
        // ============================================================

        /// <summary>
        /// 트릭 영역의 카드를 플레이어 위치 기반으로 방사형 배치합니다.
        /// 각 카드는 해당 플레이어의 방향에서 중앙으로 놓입니다.
        /// </summary>
        private void RepositionTrickCards()
        {
            int count = _trickCardObjects.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                if (_trickCardObjects[i] == null) continue;

                // 등장 애니메이션 중인 카드는 스킵
                var cardUI = _trickCardObjects[i].GetComponent<CardUI>();
                if (cardUI != null && cardUI.IsAnimating) continue;

                var rt = _trickCardObjects[i].GetComponent<RectTransform>();
                if (rt == null) continue;

                // 카드를 낸 플레이어의 인덱스 얻기
                int playerIndex = GetTrickCardPlayerIndex(i);
                Vector2 pos = GetTrickCardPosition(playerIndex);

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(120f, 170f);
                rt.anchoredPosition = pos;

                // 약간의 회전으로 자연스러운 느낌
                float tiltAngle = (i - count / 2f) * 5f;
                rt.localEulerAngles = new Vector3(0, 0, tiltAngle);
            }
        }

        /// <summary>
        /// 트릭 카드 인덱스에 해당하는 플레이어 인덱스를 반환합니다.
        /// TrickCards 리스트에서 PlayerIndex를 추출합니다.
        /// </summary>
        private int GetTrickCardPlayerIndex(int trickCardIndex)
        {
            if (_gameManager.State == null) return 0;
            var trickCards = _gameManager.State.TrickCards;
            if (trickCardIndex < trickCards.Count)
                return trickCards[trickCardIndex].PlayerIndex;
            return 0;
        }

        /// <summary>
        /// 플레이어 인덱스에 따른 트릭 카드 배치 좌표를 반환합니다.
        /// 각 플레이어의 좌석 위치에서 중앙 방향으로 카드를 놓습니다.
        /// </summary>
        private Vector2 GetTrickCardPosition(int playerIndex)
        {
            int playerCount = _gameManager.State.Players.Length;
            int seat = GetSeatSlot(playerIndex, playerCount);
            float r = 85f; // 중앙에서의 거리

            return seat switch
            {
                0 => new Vector2(0, -r),                 // Bottom (나)
                1 => new Vector2(-r * 1.3f, -r * 0.2f),  // Left
                2 => new Vector2(-r * 0.9f, r * 0.7f),   // TopLeft
                3 => new Vector2(0, r),                   // Top
                4 => new Vector2(r * 0.9f, r * 0.7f),    // TopRight
                5 => new Vector2(r * 1.3f, -r * 0.2f),   // Right
                _ => Vector2.zero
            };
        }

        /// <summary>
        /// 플레이어 인덱스를 화면 좌석 슬롯(0~5)으로 변환합니다.
        /// 인간 플레이어는 항상 0(Bottom), 나머지는 시계방향.
        /// </summary>
        private int GetSeatSlot(int playerIndex, int playerCount)
        {
            // UpdateScoreboard과 동일한 매핑
            int[] mapping;
            if (playerCount == 6) mapping = new int[] { 0, 1, 2, 3, 4, 5 };
            else if (playerCount == 5) mapping = new int[] { 0, 1, 2, 4, 5 };
            else if (playerCount == 4) mapping = new int[] { 0, 1, 3, 5 };
            else if (playerCount == 3) mapping = new int[] { 0, 1, 5 };
            else mapping = new int[] { 0, 3 };

            return (playerIndex < mapping.Length) ? mapping[playerIndex] : 3;
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

        // ============================================================
        // 3D 원근 캔버스 설정 — "정면에서 보는 카드" 느낌의 핵심
        // ============================================================

        /// <summary>
        /// Canvas를 Screen Space - Camera 모드로 전환하여
        /// 3D 회전에 원근감(perspective)이 적용되도록 합니다.
        /// </summary>
        private void SetupPerspectiveCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // Canvas를 카메라 기반 렌더링으로 전환
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 100f;

            // 카메라 원근 투영 확인
            cam.orthographic = false;
            cam.fieldOfView = 60f;

            Debug.Log("[FrontView] Canvas -> Screen Space - Camera (원근 투영 활성화)");

            // 손패 영역: 약한 X축 회전 (강하면 카드가 너무 누워보임)
            if (_playerHandArea != null)
            {
                _playerHandArea.localEulerAngles = new Vector3(3f, 0f, 0f);
            }

            // 트릭 영역: X축 회전으로 테이블 면 원근감
            if (_trickArea != null)
            {
                _trickArea.localEulerAngles = new Vector3(35f, 0f, 0f);
            }
        }

        // ============================================================
        // 프론트뷰 동적 UI 생성 및 관리
        // ============================================================

        /// <summary>
        /// 프론트뷰 UI 요소들을 런타임에 동적으로 생성합니다.
        /// 첫 라운드 시작 시 한 번만 호출됩니다.
        /// </summary>
        private void SetupFrontViewLayout()
        {
            if (_frontViewInitialized) return;
            if (_gameManager.State == null) return;

            int playerCount = _gameManager.State.Players.Length;
            var players = _gameManager.State.Players;

            // ★ Canvas를 3D 원근 모드로 전환 (핵심!)
            SetupPerspectiveCanvas();

            // Canvas 찾기 (프로필 패널/상대 카드를 배치할 부모)
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[FrontView] Canvas를 찾을 수 없습니다!");
                return;
            }

            Transform canvasTransform = canvas.transform;

            // ---- 상대 카드 뒷면 표시 생성 (3D 회전 적용!) ----
            var opponentList = new List<OpponentHandDisplay>();
            for (int i = 0; i < playerCount; i++)
            {
                if (players[i].IsHuman) continue;

                int seat = GetSeatSlot(i, playerCount);
                Vector2 opponentAnchor = GetOpponentHandAnchor(seat);

                var handObj = new GameObject($"OpponentHand_{players[i].Name}");
                handObj.transform.SetParent(canvasTransform, false);

                var rt = handObj.AddComponent<RectTransform>();
                rt.anchorMin = opponentAnchor;
                rt.anchorMax = opponentAnchor;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(300f, 150f);

                // ★ 좌석별 3D 회전 적용 — 상대 카드도 프론트뷰!
                Vector3 seatRotation = GetOpponentHandRotation(seat);
                rt.localEulerAngles = seatRotation;

                var display = handObj.AddComponent<OpponentHandDisplay>();
                display.Setup(seat, players[i].Hand.Count);

                opponentList.Add(display);
            }
            _opponentDisplays = opponentList.ToArray();

            _frontViewInitialized = true;
            Debug.Log($"[FrontView] 프론트뷰 UI 초기화 완료 ({playerCount}명)");
        }

        /// <summary>
        /// 좌석 슬롯에 따른 상대 카드 뒷면 표시 앵커 위치를 반환합니다.
        /// </summary>
        private Vector2 GetOpponentHandAnchor(int seat)
        {
            return seat switch
            {
                1 => new Vector2(0.10f, 0.45f),   // Left
                2 => new Vector2(0.15f, 0.75f),   // TopLeft
                3 => new Vector2(0.50f, 0.82f),   // Top
                4 => new Vector2(0.85f, 0.75f),   // TopRight
                5 => new Vector2(0.90f, 0.45f),   // Right
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        /// <summary>
        /// 좌석 슬롯에 따른 상대 카드 컨테이너 회전을 반환합니다.
        /// Z축 회전만 사용하여 부채꼴 방향을 전환합니다.
        /// (3D X/Y 회전은 원근 Canvas에서 심하게 왜곡되므로 사용하지 않음)
        /// </summary>
        private Vector3 GetOpponentHandRotation(int seat)
        {
            return seat switch
            {
                1 => new Vector3(0f, 0f, -90f),    // Left: 세로 (아래를 향함)
                2 => new Vector3(0f, 0f, -35f),    // TopLeft: 대각
                3 => Vector3.zero,                  // Top: 가로 (기본)
                4 => new Vector3(0f, 0f, 35f),     // TopRight: 대각 반대
                5 => new Vector3(0f, 0f, 90f),     // Right: 세로 (아래를 향함)
                _ => Vector3.zero
            };
        }

        /// <summary>
        /// 모든 프로필 패널의 정보를 갱신합니다.
        /// </summary>
        private void UpdateProfilePanels()
        {
            if (_profilePanels == null || _gameManager.State == null) return;

            for (int i = 0; i < _profilePanels.Length; i++)
            {
                if (_profilePanels[i] == null) continue;
                var p = _gameManager.State.Players[i];

                _profilePanels[i].UpdateInfo(
                    p.Bid,
                    p.TricksWon,
                    p.TotalScore,
                    p.Hand.Count,
                    p.HasBid,
                    i == _gameManager.State.CurrentPlayerIndex
                );
            }
        }

        /// <summary>
        /// 상대 플레이어의 카드 뒷면 표시를 갱신합니다.
        /// </summary>
        private void RefreshOpponentHands()
        {
            if (_opponentDisplays == null || _gameManager.State == null) return;

            int displayIdx = 0;
            for (int i = 0; i < _gameManager.State.Players.Length; i++)
            {
                if (_gameManager.State.Players[i].IsHuman) continue;
                if (displayIdx >= _opponentDisplays.Length) break;

                _opponentDisplays[displayIdx].SetCardCount(
                    _gameManager.State.Players[i].Hand.Count
                );
                displayIdx++;
            }
        }
    }
}

