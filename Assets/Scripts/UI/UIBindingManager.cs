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
    public partial class UIBindingManager : MonoBehaviour
    {
        // ============================================================
        // [SerializeField] — UI 참조들
        // ============================================================
        [Header("=== 필수 참조 ===")]
        [SerializeField] private GameManager _gameManager;

        [Header("=== 페이즈 표시 ===")]
        [SerializeField] private Text _phaseText;
        [SerializeField] private Text _roundInfoText;

        [Header("=== 비딩 패널 ===")]
        [SerializeField] private GameObject _bidPanel;
        [SerializeField] private Text _bidValueText;
        [SerializeField] private Button _bidUpButton;
        [SerializeField] private Button _bidDownButton;
        [SerializeField] private Button _bidConfirmButton;

        [Header("=== 타이그리스 선택 패널 ===")]
        [SerializeField] private GameObject _tigressPanel;
        [SerializeField] private Button _tigressPirateButton;
        [SerializeField] private Button _tigressEscapeButton;

        [Header("=== 카드 영역 ===")]
        [SerializeField] private Transform _playerHandArea;
        [SerializeField] private Transform[] _opponentHandAreas;
        [SerializeField] private Transform _trickArea;
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private CardSpriteDatabase _spriteDatabase;
        [SerializeField] private TrickHistoryView _trickHistory;
        [Header("=== 백경: 날개로 가린 순간 선두 카드 교체 ===")]
        [SerializeField] private WhaleWingTransition _whaleWingTransition;

        [Header("=== 스코어보드 ===")]
        [SerializeField] private Text[] _playerInfoTexts;

        [Header("=== 결과 표시 ===")]
        [SerializeField] private Text _trickResultText;
        [SerializeField] private GameObject _roundResultPanel;
        [SerializeField] private Text _roundResultText;
        [SerializeField] private Button _roundResultNextButton;
        [SerializeField] private GameObject _gameEndPanel;
        [SerializeField] private Text _gameEndText;

        [Header("=== 턴 화살표 ===")]
        [SerializeField] private GameObject _turnArrow;

        // ============================================================
        // 내부 상태
        // ============================================================
        private int _currentBidValue = 0;
        private int _maxBidValue = 0;
        private List<CardData> _currentPlayableCards = new List<CardData>();
        private List<GameObject> _handCardObjects = new List<GameObject>();
        private List<GameObject> _trickCardObjects = new List<GameObject>();
        private bool _trickCardsDirty = false;
        private GameObject _currentTopCardObj; // 현재 1등 카드 기록용
        private List<GameObject> _customAnimatingCards = new List<GameObject>(); // 2단계 애니메이션 추적용
        private List<PlayedCard> _currentTrickCards = new List<PlayedCard>();
        private float _cardInputUnlockTime;

        private Coroutine _phaseTextRoutine;
        private GamePhase _currentPhase;
        private bool _layoutFixed = false;

        // ============================================================
        // 초기화
        // ============================================================
        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            }

            SetActive(_bidPanel, false);
            SetActive(_tigressPanel, false);
            SetActive(_roundResultPanel, false);
            SetActive(_gameEndPanel, false);

            if (_roundResultText != null)
                _roundResultText.alignment = TextAnchor.MiddleCenter;

            DestroyAreaLayoutComponents(_trickArea);
            DestroyAreaLayoutComponents(_playerHandArea);
        }

        private void DestroyAreaLayoutComponents(Transform area)
        {
            if (area == null) return;
            var layoutGroups = area.GetComponents<UnityEngine.UI.LayoutGroup>();
            foreach (var layout in layoutGroups)
            {
                DestroyImmediate(layout);
            }
        }

        private void LateUpdate()
        {
            if (_trickCardObjects.Count > 0 && _trickCardsDirty)
            {
                RepositionTrickCards();
                _trickCardsDirty = false;
            }
        }

        private void OnEnable()
        {
            if (_gameManager == null) return;
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
            _gameManager.OnNetworkStatusChanged += HandleNetworkStatus;

            if (_bidUpButton) _bidUpButton.onClick.AddListener(OnBidUp);
            if (_bidDownButton) _bidDownButton.onClick.AddListener(OnBidDown);
            if (_bidConfirmButton) _bidConfirmButton.onClick.AddListener(OnBidConfirm);
            if (_tigressPirateButton) _tigressPirateButton.onClick.AddListener(OnTigressPirate);
            if (_tigressEscapeButton) _tigressEscapeButton.onClick.AddListener(OnTigressEscape);
            if (_roundResultNextButton) _roundResultNextButton.onClick.AddListener(OnRoundResultNext);
        }

        private void OnDisable()
        {
            CancelPresentations();
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
            _gameManager.OnNetworkStatusChanged -= HandleNetworkStatus;

            if (_bidUpButton) _bidUpButton.onClick.RemoveListener(OnBidUp);
            if (_bidDownButton) _bidDownButton.onClick.RemoveListener(OnBidDown);
            if (_bidConfirmButton) _bidConfirmButton.onClick.RemoveListener(OnBidConfirm);
            if (_tigressPirateButton) _tigressPirateButton.onClick.RemoveListener(OnTigressPirate);
            if (_tigressEscapeButton) _tigressEscapeButton.onClick.RemoveListener(OnTigressEscape);
            if (_roundResultNextButton) _roundResultNextButton.onClick.RemoveListener(OnRoundResultNext);
        }

        // ============================================================
        // 이벤트 핸들러
        // ============================================================
        private void HandleNetworkStatus(string message)
        {
            SetText(_trickResultText, message);
            if (_gameManager.IsGameRunning) return;
            CancelPresentations();
            _currentPlayableCards.Clear();
            SetActive(_bidPanel, false);
            SetActive(_tigressPanel, false);
            SetActive(_roundResultPanel, false);
            if (_turnArrow != null) _turnArrow.SetActive(false);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            EnqueuePresentation(() => PresentInstant(() => ShowPhaseChanged(phase)));
        }

        private void ShowPhaseChanged(GamePhase phase)
        {
            _currentPhase = phase;
            string txt = GetPhaseDisplayName(phase);

            if (phase == GamePhase.Bidding || phase == GamePhase.Playing || phase == GamePhase.Dealing)
            {
                if (_phaseTextRoutine != null) StopCoroutine(_phaseTextRoutine);
                _phaseTextRoutine = StartCoroutine(TemporaryTextCoroutine(_phaseText, txt, 2f));
            }
            else
            {
                SetActive(_phaseText.gameObject, true);
                SetText(_phaseText, txt);
            }

            if (_gameManager != null && _gameManager.State != null)
            {
                HandleCurrentPlayerChanged(_gameManager.State.CurrentPlayerIndex);
            }
        }

        private System.Collections.IEnumerator TemporaryTextCoroutine(Text textObj, string msg, float duration)
        {
            if (textObj != null)
            {
                textObj.gameObject.SetActive(true);
                textObj.text = msg;
                yield return new WaitForSeconds(duration);
                textObj.gameObject.SetActive(false);
            }
        }

        private void HandleRoundStarted(int roundNumber)
        {
            if (_trickHistory != null) _trickHistory.BeginRound(roundNumber);
            SetText(_roundInfoText, $"라운드 {roundNumber}");
            SetActive(_roundResultPanel, false);
            UpdateScoreboard();
        }

        private void HandleCardsDealt()
        {
            RefreshAllHands();
        }

        private void HandleBidRequested(PlayerData player, int maxBid)
        {
            _currentBidValue = 0;
            _maxBidValue = maxBid;
            UpdateBidDisplay();
            SetActive(_bidPanel, true);
            SetText(_roundInfoText, $"라운드 {_gameManager.State.CurrentRound}");
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
            EnqueuePresentation(() => PresentInstant(() => ShowTrickStarted(trickNumber, leaderIndex)));
        }

        private void ShowTrickStarted(int trickNumber, int leaderIndex)
        {
            SetText(_roundInfoText, $"라운드 {_gameManager.State.CurrentRound}");
            SetText(_trickResultText, "");
            ClearTrickArea();
            if (_trickHistory != null) _trickHistory.BeginTrick(_gameManager.State.CurrentRound, trickNumber);
        }

        private void HandleCardPlayRequested(PlayerData player, List<CardData> playableCards)
        {
            _currentPlayableCards = playableCards;
            RefreshAllHands();
        }

        private void HandleTigressChoiceRequested(PlayerData player)
        {
            SetActive(_tigressPanel, true);
        }

        private void HandleCardPlayed(PlayedCard playedCard)
        {
            SpawnCardInTrickArea(playedCard);

            var player = _gameManager.State.Players[playedCard.PlayerIndex];
            if (player.IsHuman)
            {
                _currentPlayableCards.Clear();
            }

            RefreshAllHands();
            UpdateScoreboard();
        }

        private void HandleTrickResolved(TrickResult result)
        {
            EnqueuePresentation(() => PresentInstant(() => ShowTrickResolved(result)));
        }

        private void ShowTrickResolved(TrickResult result)
        {
            if (result.IsVoided)
            {
                SetText(_trickResultText, "크라켄! 트릭 무효!");
            }
            else
            {
                var winner = _gameManager.State.Players[result.WinnerIndex];
                string msg = $"승자: {winner.Name}";
                SetText(_trickResultText, msg);
            }
            UpdateScoreboard();
        }

        private void HandleRoundEnded(RoundScoreResult[] results)
        {
            EnqueuePresentation(() => PresentInstant(() => ShowRoundEnded(results)));
        }

        private void ShowRoundEnded(RoundScoreResult[] results)
        {
            int pCount = results.Length;
            string topSpacing = pCount <= 4 ? "\n\n\n" : "\n";
            string itemSpacing = pCount <= 4 ? "\n\n" : "\n";

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
            EnqueuePresentation(() => PresentInstant(() => ShowGameEnded(winner)));
        }

        private void ShowGameEnded(PlayerData winner)
        {
            string text = $"★ 게임 종료! ★\n\n";
            text += $"우승: {winner.Name} ({winner.TotalScore}점)\n\n";

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
            UpdateScoreboard();

            if (_gameManager.State == null || _turnArrow == null) return;

            int targetIndex = playerIndex;
            if (_currentPhase == GamePhase.Bidding || _currentPhase == GamePhase.Dealing)
            {
                targetIndex = _gameManager.State.LeaderIndex;
            }

            if (targetIndex >= 0 && targetIndex < _playerInfoTexts.Length)
            {
                var targetPanel = _playerInfoTexts[targetIndex].transform.parent;
                if (targetPanel != null && targetPanel.gameObject.activeSelf)
                {
                    _turnArrow.SetActive(true);
                    var arrowRect = _turnArrow.GetComponent<RectTransform>();

                    arrowRect.SetParent(targetPanel, false);

                    arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
                    arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
                    arrowRect.pivot = new Vector2(0.5f, 0.5f);
                    arrowRect.anchoredPosition = new Vector2(0, 80f);
                    arrowRect.localRotation = Quaternion.Euler(0, 0, 180f);
                    arrowRect.localScale = Vector3.one;
                }
            }
            else
            {
                _turnArrow.SetActive(false);
            }
        }

        // ============================================================
        // 버튼 콜백
        // ============================================================
        private void OnBidUp() { if (_currentBidValue < _maxBidValue) { _currentBidValue++; UpdateBidDisplay(); } }
        private void OnBidDown() { if (_currentBidValue > 0) { _currentBidValue--; UpdateBidDisplay(); } }
        private void OnBidConfirm() { SetActive(_bidPanel, false); _gameManager.SubmitHumanBid(_currentBidValue); }
        private void OnTigressPirate() { SetActive(_tigressPanel, false); _gameManager.SubmitTigressChoice(TigressChoice.Pirate); }
        private void OnTigressEscape() { SetActive(_tigressPanel, false); _gameManager.SubmitTigressChoice(TigressChoice.Escape); }
        private void OnRoundResultNext() { SetActive(_roundResultPanel, false); _gameManager.SubmitRoundConfirm(); }

        public void OnCardClicked(CardData card)
        {
            // 상대 카드가 중앙에 도착해 정리되기 전에 다음 카드를 내면 연출 순서가 뒤집힙니다.
            // 진행 중인 제출 연출과 짧은 착지 여유 시간이 끝날 때까지 입력만 무시합니다.
            _customAnimatingCards.RemoveAll(obj => obj == null);
            if (_presentationBusy || _customAnimatingCards.Count > 0 || Time.time < _cardInputUnlockTime) return;

            if (_currentPlayableCards.Count == 0) return;
            bool isPlayable = false;
            for (int i = 0; i < _currentPlayableCards.Count; i++)
            {
                if (_currentPlayableCards[i].Id == card.Id) { isPlayable = true; break; }
            }
            if (!isPlayable) return;

            _currentPlayableCards.Clear();
            _gameManager.SubmitHumanCard(card);
        }

        // ============================================================
        // UI 갱신 & 배치 메서드
        // ============================================================
        private void UpdateBidDisplay()
        {
            SetText(_bidValueText, _currentBidValue.ToString());
        }

        private void UpdateScoreboard()
        {
            if (_gameManager.State == null || _gameManager.State.Players == null) return;
            int playerCount = _gameManager.State.Players.Length;

            if (!_layoutFixed)
            {
                SetupDynamicLayout(playerCount);
                _layoutFixed = true;
            }

            for (int i = 0; i < playerCount && i < _playerInfoTexts.Length; i++)
            {
                var p = _gameManager.State.Players[i];
                if (_playerInfoTexts[i] == null) continue;

                string bidInfo = p.HasBid ? $"비딩:{p.Bid} 승리:{p.TricksWon}" : "비딩 대기";
                _playerInfoTexts[i].text = $"{p.Name}  |  {bidInfo}  |  {p.TotalScore}점";

                _playerInfoTexts[i].transform.parent.gameObject.SetActive(true);
            }

            for (int i = playerCount; i < _playerInfoTexts.Length; i++)
            {
                if (_playerInfoTexts[i] != null)
                {
                    _playerInfoTexts[i].text = "";
                    _playerInfoTexts[i].transform.parent.gameObject.SetActive(false);
                }
            }
        }

        private int[] GetOpponentAreaMapping(int playerCount)
        {
            if (playerCount == 3) return new int[] { 0, 4 };
            if (playerCount == 4) return new int[] { 0, 2, 4 };
            if (playerCount == 5) return new int[] { 0, 1, 3, 4 };
            if (playerCount == 6) return new int[] { 0, 1, 2, 3, 4 };
            return new int[] { 2 };
        }

        private void SetupDynamicLayout(int playerCount)
        {
            if (_playerInfoTexts.Length > 0 && _playerInfoTexts[0] != null)
            {
                var myPanel = _playerInfoTexts[0].transform.parent.GetComponent<RectTransform>();
                if (myPanel != null)
                {
                    myPanel.anchorMin = new Vector2(0f, 0f);
                    myPanel.anchorMax = new Vector2(0f, 0f);
                    myPanel.pivot = new Vector2(0f, 0f);
                    myPanel.anchoredPosition = new Vector2(30f, 30f);
                    myPanel.sizeDelta = new Vector2(320f, 120f);
                }
            }

            int[] mapping = GetOpponentAreaMapping(playerCount);
            Vector2[] cardPos = new Vector2[5];
            Vector2[] scorePos = new Vector2[5];
            float[] rot = new float[5];

            if (playerCount == 6)
            {
                scorePos[0] = new Vector2(-700, 100); cardPos[0] = new Vector2(-630, 20); rot[0] = 0f;
                scorePos[1] = new Vector2(-450, 320); cardPos[1] = new Vector2(-380, 240); rot[1] = 0f;
                scorePos[2] = new Vector2(0, 350); cardPos[2] = new Vector2(0, 250); rot[2] = 0f;
                scorePos[3] = new Vector2(450, 320); cardPos[3] = new Vector2(380, 240); rot[3] = 0f;
                scorePos[4] = new Vector2(700, 100); cardPos[4] = new Vector2(630, 20); rot[4] = 0f;
            }
            else if (playerCount == 5)
            {
                scorePos[0] = new Vector2(-750, 150); cardPos[0] = new Vector2(-650, 70); rot[0] = 0f;
                scorePos[1] = new Vector2(-400, 320); cardPos[1] = new Vector2(-330, 240); rot[1] = 0f;
                scorePos[2] = Vector2.zero; cardPos[2] = Vector2.zero; rot[2] = 0f;
                scorePos[3] = new Vector2(400, 320); cardPos[3] = new Vector2(330, 240); rot[3] = 0f;
                scorePos[4] = new Vector2(750, 150); cardPos[4] = new Vector2(650, 70); rot[4] = 0f;
            }
            else if (playerCount == 4)
            {
                scorePos[0] = new Vector2(-750, 180); cardPos[0] = new Vector2(-650, 100); rot[0] = 0f;
                scorePos[1] = Vector2.zero; cardPos[1] = Vector2.zero; rot[1] = 0f;
                scorePos[2] = new Vector2(0, 350); cardPos[2] = new Vector2(0, 250); rot[2] = 0f;
                scorePos[3] = Vector2.zero; cardPos[3] = Vector2.zero; rot[3] = 0f;
                scorePos[4] = new Vector2(750, 180); cardPos[4] = new Vector2(650, 100); rot[4] = 0f;
            }
            else
            {
                scorePos[0] = new Vector2(-600, 250); cardPos[0] = new Vector2(-500, 150); rot[0] = 0f;
                scorePos[1] = Vector2.zero; cardPos[1] = Vector2.zero; rot[1] = 0f;
                scorePos[2] = new Vector2(0, 350); cardPos[2] = new Vector2(0, 250); rot[2] = 0f;
                scorePos[3] = Vector2.zero; cardPos[3] = Vector2.zero; rot[3] = 0f;
                scorePos[4] = new Vector2(600, 250); cardPos[4] = new Vector2(500, 150); rot[4] = 0f;
            }

            for (int i = 0; i < mapping.Length; i++)
            {
                int slot = mapping[i];

                if (slot < _opponentHandAreas.Length && _opponentHandAreas[slot] != null)
                {
                    var rt = _opponentHandAreas[slot].GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = cardPos[slot];
                        rt.localRotation = Quaternion.Euler(0, 0, rot[slot]);
                    }
                }

                int botIndex = i + 1;
                if (botIndex < _playerInfoTexts.Length && _playerInfoTexts[botIndex] != null)
                {
                    var panelRect = _playerInfoTexts[botIndex].transform.parent.GetComponent<RectTransform>();
                    if (panelRect != null)
                    {
                        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                        panelRect.pivot = new Vector2(0.5f, 0.5f);
                        panelRect.sizeDelta = new Vector2(320f, 120f);
                        panelRect.anchoredPosition = scorePos[slot];
                    }
                }
            }
        }

        private void RefreshAllHands()
        {
            ClearHandArea();

            if (_gameManager.State == null || _cardPrefab == null) return;

            SetupDynamicLayout(_gameManager.State.PlayerCount);

            int[] areaMapping = GetOpponentAreaMapping(_gameManager.State.PlayerCount);
            int botCount = 0;

            for (int pIndex = 0; pIndex < _gameManager.State.Players.Length; pIndex++)
            {
                var player = _gameManager.State.Players[pIndex];

                if (player.IsHuman)
                {
                    for (int i = 0; i < player.Hand.Count; i++)
                    {
                        var card = player.Hand[i];
                        var cardObj = Instantiate(_cardPrefab, _playerHandArea);

                        var layoutElem = cardObj.GetComponent<LayoutElement>();
                        if (layoutElem != null) DestroyImmediate(layoutElem);

                        var cardUI = cardObj.GetComponent<CardUI>();
                        if (cardUI != null)
                        {
                            cardUI.Setup(card, IsCardPlayable(card), this);
                            if (_spriteDatabase != null)
                            {
                                cardUI.SetIllustration(_spriteDatabase.GetSpriteForCard(card));
                                cardUI.SetComposedArtwork(_spriteDatabase.IsComposedArtwork(card));
                            }
                            cardUI.SetAsHandCard(Vector3.one, i);
                        }
                        _handCardObjects.Add(cardObj);
                    }
                }
                else
                {
                    if (botCount >= areaMapping.Length) continue;

                    int targetAreaIndex = areaMapping[botCount];
                    botCount++;

                    if (targetAreaIndex >= _opponentHandAreas.Length || _opponentHandAreas[targetAreaIndex] == null)
                        continue;

                    Transform targetArea = _opponentHandAreas[targetAreaIndex];

                    for (int i = 0; i < player.Hand.Count; i++)
                    {
                        var cardObj = Instantiate(_cardPrefab, targetArea);
                        cardObj.transform.localScale = Vector3.one * 0.8f;

                        var layoutElem = cardObj.GetComponent<LayoutElement>();
                        if (layoutElem != null) DestroyImmediate(layoutElem);

                        var cardUI = cardObj.GetComponent<CardUI>();
                        if (cardUI != null)
                        {
                            cardUI.Setup(player.Hand[i], false, this);
                            cardUI.SetAsBack();

                            if (_spriteDatabase != null && _spriteDatabase.cardBackSprite != null)
                                cardUI.SetIllustration(_spriteDatabase.cardBackSprite);
                        }
                        _handCardObjects.Add(cardObj);
                    }
                }
            }
            RepositionHandCards();
        }

        private void ClearHandArea()
        {
            for (int i = 0; i < _handCardObjects.Count; i++)
            {
                if (_handCardObjects[i] != null)
                {
                    _handCardObjects[i].transform.SetParent(null);
                    Destroy(_handCardObjects[i]);
                }
            }
            _handCardObjects.Clear();
        }

        private void ClearTrickArea()
        {
            _currentTopCardObj = null;
            _customAnimatingCards.Clear();
            _currentTrickCards.Clear(); // ★ 중요: 이전 트릭 카드 기록 지우기

            for (int i = 0; i < _trickCardObjects.Count; i++)
            {
                if (_trickCardObjects[i] != null) Destroy(_trickCardObjects[i]);
            }
            _trickCardObjects.Clear();
        }

        private void RepositionTrickCards()
        {
            int count = _trickCardObjects.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                if (_trickCardObjects[i] == null) continue;
                if (_customAnimatingCards.Contains(_trickCardObjects[i])) continue; // 애니메이션 중 건드림 방지

                // 겹쳐진 이전 카드의 색이나 프레임이 현재 카드의 투명 모서리로
                // 비치지 않도록, 트릭이 쌓인 뒤에는 현재 1등 카드만 렌더링합니다.
                bool isVisibleTopCard = _currentTopCardObj == null
                    || _trickCardObjects[i] == _currentTopCardObj;
                _trickCardObjects[i].SetActive(isVisibleTopCard);
                if (!isVisibleTopCard) continue;

                var rt = _trickCardObjects[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    // 카드 원본 일러스트(832x1216)의 13:19 비율 유지
                    rt.sizeDelta = new Vector2(120f, 120f * 19f / 13f);

                    // 모든 트릭 카드를 정확히 같은 위치에 겹칩니다.
                    // 이전 카드의 빨강/검정 테두리가 위나 옆으로 삐져나오지 않습니다.
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                }
            }
        }

        private void RepositionHandCards()
        {
            ArrangeCardsInArea(_playerHandArea, 70f, 1.5f, 2f);

            if (_opponentHandAreas != null)
            {
                for (int i = 0; i < _opponentHandAreas.Length; i++)
                {
                    ArrangeCardsInArea(_opponentHandAreas[i], 25f, 0f, 0f);
                }
            }
        }

        private void ArrangeCardsInArea(Transform area, float spacing, float fanAngle, float yDrop)
        {
            if (area == null) return;

            List<CardUI> validCards = new List<CardUI>();
            for (int i = 0; i < area.childCount; i++)
            {
                var cardUI = area.GetChild(i).GetComponent<CardUI>();
                if (cardUI != null && cardUI.gameObject.activeSelf)
                {
                    validCards.Add(cardUI);
                }
            }

            int count = validCards.Count;
            if (count == 0) return;

            float totalWidth = (count - 1) * spacing;
            float startX = -totalWidth / 2f;
            float centerIndex = (count - 1) / 2f;

            for (int i = 0; i < count; i++)
            {
                var cardUI = validCards[i];
                if (cardUI.IsHovered) continue;

                var rt = cardUI.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);

                    float offsetFromCenter = i - centerIndex;
                    float xPos = startX + (i * spacing);
                    float yPos = Mathf.Abs(offsetFromCenter) * -yDrop;
                    rt.anchoredPosition = new Vector2(xPos, yPos);

                    float rotZ = offsetFromCenter * -fanAngle;
                    rt.localRotation = Quaternion.Euler(0, 0, rotZ);
                }
                cardUI.CaptureOriginalPosition();
            }
        }

        private void SpawnCardInTrickArea(PlayedCard playedCard)
        {
            EnqueuePresentation(() => PresentCard(playedCard));
        }

        private System.Collections.IEnumerator PresentCard(PlayedCard playedCard)
        {
            if (_cardPrefab == null || _trickArea == null) yield break;

            var cardObj = Instantiate(_cardPrefab, _trickArea);
            cardObj.transform.localScale = Vector3.one * 0.85f;
            cardObj.transform.localRotation = Quaternion.identity;

            var layoutElem = cardObj.GetComponent<LayoutElement>();
            if (layoutElem != null) DestroyImmediate(layoutElem);

            var cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(playedCard.Card, false, this);
                cardUI.SetOwnerLabel(_gameManager.State.Players[playedCard.PlayerIndex].Name);

                if (playedCard.Card.Special == SpecialType.Tigress)
                    cardUI.SetTigressMode(playedCard.TigressMode);

                if (_spriteDatabase != null)
                {
                    cardUI.SetIllustration(_spriteDatabase.GetSpriteForCard(playedCard.Card));
                    cardUI.SetComposedArtwork(_spriteDatabase.IsComposedArtwork(playedCard.Card));
                }
            }

            _trickCardObjects.Add(cardObj);

            // 1. 모든 카드는 정확히 같은 중앙 위치에 도착합니다.
            Vector2 targetPos = Vector2.zero;

            // 2. 출발지 계산
            Vector2 startOffset = GetCardEntryOffset(playedCard.PlayerIndex);

            // 3. 승패 판별 (UI가 1등인지 아닌지 체크)
            _currentTrickCards.Add(playedCard);
            if (_trickHistory != null)
                _trickHistory.AddSubmitted(playedCard, _gameManager.State.Players[playedCard.PlayerIndex].Name);
            int winnerIndex = TrickPresentationRules.WinnerCardIndex(_currentTrickCards);
            GameObject winner = winnerIndex >= 0 ? _trickCardObjects[winnerIndex] : _currentTopCardObj;
            bool whale = TrickPresentationRules.IsWhaleEntrance(_currentTrickCards);

            // 4. 새로운 2단계 애니메이션 실행 (기존 PlayEntryAnimation 대신 이걸 씁니다)
            yield return PlayTwoStepAnimation(cardObj, startOffset, targetPos, winner, whale);
            _trickCardsDirty = true;
        }

        /// <summary>
        /// 중앙으로 날아와 확대(뽐내기) 후, 승패에 따라 위/아래로 스르륵 겹쳐지는 애니메이션
        /// </summary>
        private System.Collections.IEnumerator PlayTwoStepAnimation(GameObject cardObj, Vector2 startPos, Vector2 finalPos, GameObject winner, bool whale)
        {
            _customAnimatingCards.Add(cardObj);
            var rt = cardObj.GetComponent<RectTransform>();

            float duration1 = 0.25f; // 중앙으로 날아오며 확대되는 시간
            float pause = 0.5f;      // 중앙에서 카드를 뽐내며 대기하는 시간
            float duration2 = 0.25f; // 승패에 따라 덱으로 들어가는 시간
            _cardInputUnlockTime = Mathf.Max(
                _cardInputUnlockTime, Time.time + duration1 + pause + duration2 + 0.15f);

            Vector2 centerPos = new Vector2(0, 50f); // 뽐낼 위치 (중앙에서 살짝 위)
            Vector3 startScale = Vector3.one * 0.5f;
            Vector3 bigScale = Vector3.one * 1.3f; // 1.3배 확대
            Vector3 finalScale = Vector3.one * 0.85f; // 덱에 쌓이는 최종 스케일

            rt.anchoredPosition = startPos;
            rt.localScale = startScale;

            // 1단계: 이름표에서 정중앙으로 날아오면서 커짐
            rt.SetAsLastSibling(); // 날아올 때는 무조건 맨 위에서 당당하게!
            float elapsed = 0f;
            while (elapsed < duration1)
            {
                if (cardObj == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration1;
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                rt.anchoredPosition = Vector2.Lerp(startPos, centerPos, ease);
                rt.localScale = Vector3.Lerp(startScale, bigScale, ease);
                yield return null;
            }
            rt.anchoredPosition = centerPos;
            rt.localScale = bigScale;

            // 2단계: 중앙에서 카드 뽐내며 잠시 대기
            yield return new WaitForSeconds(pause);

            if (whale)
            {
                // Show the celestial card's actual submission first. Keep it visible at the
                // showcase position until white light hides the swap; never change leader early.
                System.Action swap = () =>
                {
                    if (cardObj != null)
                    {
                        rt.anchoredPosition = finalPos;
                        rt.localScale = finalScale;
                        _customAnimatingCards.Remove(cardObj);
                    }
                    ApplyPresentedWinner(winner);
                };
                if (_whaleWingTransition != null) yield return _whaleWingTransition.Play(swap, rt,
                    _trickArea as RectTransform, finalPos,
                    _currentTopCardObj != null ? _currentTopCardObj.GetComponent<RectTransform>() : null,
                    winner != null ? winner.GetComponent<RectTransform>() : null);
                else swap();
                _customAnimatingCards.Remove(cardObj);
                _cardInputUnlockTime = Mathf.Max(_cardInputUnlockTime, Time.time + .15f);
                RepositionTrickCards();
                yield break;
            }

            // 3단계 진입 전: ★★★ 승패에 따라 쌓이는 순서(Z-Order) 마술 부리기 ★★★
            if (winner != null)
            {
                _currentTopCardObj = winner;
                winner.SetActive(true);
                winner.transform.SetAsLastSibling();
            }
            else
            {
                // 지는 카드라면? 현재 1등 카드의 Z축 순서를 복사해옴!
                // 이렇게 하면 1등 카드가 자동으로 밀려 올라가면서, 내 카드가 그 밑으로 깔리게 됨
                if (_currentTopCardObj != null)
                {
                    int topIndex = _currentTopCardObj.transform.GetSiblingIndex();
                    rt.SetSiblingIndex(topIndex);
                }
            }

            // 3단계: 스르륵 덱에 겹치기 (이때 지는 카드는 1등 카드 뒤로 숨어 들어가는 것처럼 보임!)
            UpdateTrickHistoryLeader();
            elapsed = 0f;
            while (elapsed < duration2)
            {
                if (cardObj == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration2;

                rt.anchoredPosition = Vector2.Lerp(centerPos, finalPos, t);
                rt.localScale = Vector3.Lerp(bigScale, finalScale, t);
                yield return null;
            }
            rt.anchoredPosition = finalPos;
            rt.localScale = finalScale;

            _customAnimatingCards.Remove(cardObj);
            _cardInputUnlockTime = Mathf.Max(_cardInputUnlockTime, Time.time + 0.15f);
            RepositionTrickCards();
        }

        private Vector2 GetCardEntryOffset(int playerIndex)
        {
            if (playerIndex == 0)
            {
                return new Vector2(0f, -450f);
            }

            if (playerIndex > 0 && playerIndex < _playerInfoTexts.Length)
            {
                if (_playerInfoTexts[playerIndex] != null)
                {
                    var panelRect = _playerInfoTexts[playerIndex].transform.parent.GetComponent<RectTransform>();
                    if (panelRect != null && panelRect.gameObject.activeSelf)
                    {
                        return panelRect.anchoredPosition;
                    }
                }
            }
            return new Vector2(0f, -450f);
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
            if (textComponent != null) textComponent.text = value;
        }

        private void SetActive(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
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
