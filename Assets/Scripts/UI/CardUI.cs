// ============================================================
// CardUI.cs — 카드 프리팹에 부착하는 UI 컴포넌트
// 일러스트가 전면을 차지하고, 상단 모서리에 숫자, 하단에 이름 표시.
// 프레임/배너 색상으로 카드 타입을 구분합니다.
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using SkullKing.Data;

namespace SkullKing.UI
{
    /// <summary>
    /// 개별 카드 프리팹에 부착하는 컴포넌트.
    /// 카드의 비주얼을 설정하고 클릭 이벤트를 처리합니다.
    /// 레이아웃: 일러스트(전면) + 숫자(좌상단) + 이름(하단 배너)
    /// 손패에서 마우스 호버 시 확대 + 맨 앞으로 표시됩니다.
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [System.Serializable]
        private struct NumberTextLayout
        {
            [Tooltip("좌상단 숫자의 Anchored Position")]
            public Vector2 top;

            [Tooltip("우하단 숫자의 Anchored Position")]
            public Vector2 bottom;
        }

        // ============================================================
        // 카드 레이아웃 요소 (Inspector에서 연결)
        // ============================================================

        [Header("=== 카드 프레임 ===")]
        [Tooltip("카드 외곽 프레임 이미지 (테두리 색상으로 카드 타입 구분)")]
        [SerializeField] private Image _frameImage;

        [Header("=== 일러스트 ===")]
        [Tooltip("카드 일러스트 이미지 (카드 전면을 채움)")]
        [SerializeField] private Image _illustrationImage;

        [Header("=== 하단 이름 배너 ===")]
        [Tooltip("이름 배너 배경 이미지 (하단 중앙, 반투명)")]
        [SerializeField] private Image _nameBannerImage;

        [Tooltip("카드 이름 텍스트 (배너 위에 표시)")]
        [SerializeField] private Text _nameText;

        [Header("=== 좌상단 숫자 ===")]
        [Tooltip("숫자 배경 이미지 (좌상단 작은 원/사각)")]
        [SerializeField] private Image _numberBadgeImage;

        [Tooltip("카드 숫자 텍스트 (좌상단)")]
        [SerializeField] private Text _numberText;

        [Tooltip("카드 숫자 텍스트 (우하단, 프리팹에서 직접 배치)")]
        [SerializeField] private Text _bottomNumberText;

        [Header("=== 세력별 숫자 위치 ===")]
        [Tooltip("무협 / Green 카드의 숫자 위치")]
        [SerializeField] private NumberTextLayout _wuxiaNumberLayout = new NumberTextLayout
        {
            top = new Vector2(-50f, 80f),
            bottom = new Vector2(50f, -80f)
        };

        [Tooltip("판타지 / Yellow 카드의 숫자 위치")]
        [SerializeField] private NumberTextLayout _fantasyNumberLayout = new NumberTextLayout
        {
            top = new Vector2(-50f, 75f),
            bottom = new Vector2(50f, -75f)
        };

        [Tooltip("사이버펑크 / Purple 카드의 숫자 위치")]
        [SerializeField] private NumberTextLayout _cyberpunkNumberLayout = new NumberTextLayout
        {
            top = new Vector2(-50f, 80f),
            bottom = new Vector2(50f, -80f)
        };

        [Tooltip("공허 / Black 카드의 숫자 위치")]
        [SerializeField] private NumberTextLayout _voidNumberLayout = new NumberTextLayout
        {
            top = new Vector2(-50f, 80f),
            bottom = new Vector2(50f, -80f)
        };

        [Header("=== 소유자 라벨 (트릭 영역) ===")]
        [SerializeField] private Text _ownerLabel;

        [Header("=== 인터랙션 ===")]
        [Tooltip("선택 불가 시 어둡게 만드는 오버레이")]
        [SerializeField] private GameObject _disabledOverlay;

        [Tooltip("카드 버튼 (클릭 가능한 경우)")]
        [SerializeField] private Button _button;

        [Header("=== 하위 호환 (기존 프리팹용) ===")]
        [Tooltip("기존 프리팹의 배경 이미지 (있으면 프레임 역할 대체)")]
        [SerializeField] private Image _backgroundImage;

        // ---- 내부 상태 ----
        private CardData _cardData;
        private UIBindingManager _uiManager;
        private bool _isPlayable;
        private Color _savedFrameColor;  // 호버 시 프레임 색상 백업/복원용
        private CardFrameView _cardFrameView;
        private bool _faceDown;
        private bool _hasArtworkFrame;
        private bool _isComposedArtwork;

        // ---- 호버 효과용 ----
        private bool _isInHand = false;        // 손패 카드인지 여부
        private Vector3 _originalScale;
        private Vector2 _originalAnchoredPos;  // 절대 원본 위치 (anchoredPosition)
        private bool _hasOriginalPos = false;  // 원본 위치가 설정되었는지
        private bool _isHovered = false;
        private static GameObject _altPreviewRoot;
        private static CardUI _altPreviewOwner;
        /// <summary>현재 호버 중인지 여부 (외부 읽기용)</summary>
        public bool IsHovered => _isHovered;
        /// <summary>카드 데이터 (외부 읽기용)</summary>
        public CardData CardData => _cardData;

        // ---- 카드 등장 애니메이션용 ----
        private bool _isAnimating = false;
        /// <summary>현재 등장 애니메이션 중인지 (외부 읽기용 — RepositionTrickCards에서 스킵 용도)</summary>
        public bool IsAnimating => _isAnimating;

        // ============================================================
        // Setup — 카드 데이터 설정 (UIBindingManager에서 호출)
        // ============================================================

        /// <summary>
        /// 카드 데이터를 설정합니다.
        /// UIBindingManager에서 Instantiate 후 호출합니다.
        /// </summary>
        /// <param name="card">카드 데이터</param>
        /// <param name="isPlayable">현재 낼 수 있는 카드인지</param>
        /// <param name="uiManager">클릭 이벤트를 전달할 UI 매니저</param>
        public void Setup(CardData card, bool isPlayable, UIBindingManager uiManager)
        {
            _cardData = card;
            _isPlayable = isPlayable;
            _uiManager = uiManager;
            _faceDown = false;
            _hasArtworkFrame = false;
            _isComposedArtwork = false;

            // 프레임/일러스트의 어떤 픽셀도 카드 RectTransform 밖으로 렌더링되지 않게 합니다.
            // 카드가 회전하거나 다른 카드와 겹쳐도 각 카드 자체의 사각 영역 안에서만 보입니다.
            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();

            if (_cardFrameView == null)
            {
                var frame = new GameObject("CardFrame", typeof(RectTransform), typeof(CardFrameView));
                frame.transform.SetParent(transform, false);
                _cardFrameView = frame.GetComponent<CardFrameView>();
                _cardFrameView.Build(_nameText);
            }
            _cardFrameView.Show(card, GetFrameColor(card));
            if (_nameText != null)
            {
                _nameText.transform.SetParent(_cardFrameView.transform, false);
                _nameText.gameObject.SetActive(true);
                CardFrameView.Place(_nameText.rectTransform, new Vector2(.25f, .85f), new Vector2(.94f, .96f), Vector2.zero, Vector2.zero);
                _nameText.resizeTextForBestFit = true;
                _nameText.resizeTextMinSize = 7;
                _nameText.resizeTextMaxSize = 12;
                _nameText.color = Color.black;
            }
            if (_numberText != null)
            {
                _numberText.gameObject.SetActive(true);
            }
            if (_illustrationImage != null)
            {
                _illustrationImage.sprite = null;
                _illustrationImage.gameObject.SetActive(false);
            }

            // ---- 이름 텍스트 (하단 배너) ----
            if (_nameText != null)
            {
                _nameText.text = card.DisplayName;
            }

            // ---- 숫자 텍스트 (좌상단) ----
            SyncBottomNumberTextStyle();
            if (_numberText != null)
            {
                _numberText.text = card.IsNumberCard ? card.Number.ToString() : "";
            }
            if (_bottomNumberText != null)
            {
                _bottomNumberText.text = card.IsNumberCard ? card.Number.ToString() : "";
                _bottomNumberText.gameObject.SetActive(card.IsNumberCard);
            }

            if (card.IsNumberCard)
                ApplyNumberTextLayout(card.Suit);

            // 숫자 뱃지: 숫자 카드일 때만 표시
            if (_numberBadgeImage != null)
            {
                _numberBadgeImage.gameObject.SetActive(card.IsNumberCard);
            }

            // ---- 색상 적용 ----
            ApplyColors(card);

            // ---- 플레이 가능 여부 ----
            if (_disabledOverlay != null)
            {
                _disabledOverlay.transform.SetAsLastSibling();
                _disabledOverlay.SetActive(!isPlayable);
            }

            if (_button != null)
            {
                _button.interactable = isPlayable;
                _button.onClick.RemoveAllListeners();
                if (isPlayable)
                {
                    _button.onClick.AddListener(OnClicked);
                }
            }
        }

        /// <summary>
        /// 완성 카드 이미지의 세력별 숫자 슬롯 중심에 Text를 배치합니다.
        /// 좌표 값 자체는 CardPrefab의 Inspector에서 조절합니다.
        /// </summary>
        private void ApplyNumberTextLayout(CardSuit suit)
        {
            NumberTextLayout layout = suit switch
            {
                CardSuit.Green => _wuxiaNumberLayout,
                CardSuit.Yellow => _fantasyNumberLayout,
                CardSuit.Purple => _cyberpunkNumberLayout,
                CardSuit.Black => _voidNumberLayout,
                _ => _fantasyNumberLayout
            };

            if (_numberText != null)
                _numberText.rectTransform.anchoredPosition = layout.top;
            if (_bottomNumberText != null)
                _bottomNumberText.rectTransform.anchoredPosition = layout.bottom;
        }

        /// <summary>
        /// 숫자 모양은 NumberText 한 곳에서만 관리합니다.
        /// BottomNumberText의 위치와 회전은 유지하고 글자 스타일만 복사합니다.
        /// </summary>
        private void SyncBottomNumberTextStyle()
        {
            if (_numberText == null || _bottomNumberText == null) return;

            _bottomNumberText.font = _numberText.font;
            _bottomNumberText.fontStyle = _numberText.fontStyle;
            _bottomNumberText.fontSize = _numberText.fontSize;
            _bottomNumberText.lineSpacing = _numberText.lineSpacing;
            _bottomNumberText.supportRichText = _numberText.supportRichText;
            _bottomNumberText.alignment = _numberText.alignment;
            _bottomNumberText.alignByGeometry = _numberText.alignByGeometry;
            _bottomNumberText.horizontalOverflow = _numberText.horizontalOverflow;
            _bottomNumberText.verticalOverflow = _numberText.verticalOverflow;
            _bottomNumberText.resizeTextForBestFit = _numberText.resizeTextForBestFit;
            _bottomNumberText.resizeTextMinSize = _numberText.resizeTextMinSize;
            _bottomNumberText.resizeTextMaxSize = _numberText.resizeTextMaxSize;
            _bottomNumberText.color = _numberText.color;
            _bottomNumberText.material = _numberText.material;
        }

        /// <summary>
        /// 카드 일러스트를 설정합니다.
        /// CardSpriteDatabase에서 가져온 Sprite를 적용합니다.
        /// </summary>
        public void SetIllustration(Sprite illustration)
        {
            if (_illustrationImage != null && illustration != null)
            {
                _illustrationImage.sprite = illustration;
                _illustrationImage.color = Color.white;
                _illustrationImage.preserveAspect = !_faceDown && !_isComposedArtwork;
                CardFrameView.Place(_illustrationImage.rectTransform,
                    _faceDown || _isComposedArtwork ? Vector2.zero : _hasArtworkFrame ? new Vector2(.04f, .012f) : new Vector2(.04f, .15f),
                    _faceDown || _isComposedArtwork ? Vector2.one : _hasArtworkFrame ? new Vector2(.96f, .91f) : new Vector2(.96f, .85f), Vector2.zero, Vector2.zero);
                _illustrationImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 일러스트와 프레임이 한 PNG로 합쳐진 완성 카드를 사용합니다.
        /// 기존 색상 카드판과 런타임 프레임 레이어는 모두 숨깁니다.
        /// </summary>
        public void SetComposedArtwork(bool isComposed)
        {
            _isComposedArtwork = isComposed;
            _hasArtworkFrame = false;

            if (_cardFrameView != null)
                _cardFrameView.SetComposedMode(isComposed);

            Image rootBackground = GetComponent<Image>();
            if (isComposed)
            {
                // 완성 카드 PNG의 장식 바깥쪽은 투명하므로 카드 뒤 배경이 비칠 수 있습니다.
                // 카드 사각형 전체를 흰색으로 받쳐 상단/모서리까지 항상 불투명하게 보이게 합니다.
                if (rootBackground != null) rootBackground.color = Color.white;
                if (_backgroundImage != null)
                {
                    _backgroundImage.sprite = null;
                    _backgroundImage.color = Color.clear;
                }
                if (_frameImage != null) _frameImage.color = Color.clear;
            }
            else
            {
                if (rootBackground != null) rootBackground.color = Color.white;
                ApplyColors(_cardData);
            }

            if (_illustrationImage != null && _illustrationImage.sprite != null && !_faceDown)
            {
                _illustrationImage.preserveAspect = !isComposed;
                CardFrameView.Place(_illustrationImage.rectTransform,
                    isComposed ? Vector2.zero : new Vector2(.04f, .15f),
                    isComposed ? Vector2.one : new Vector2(.96f, .85f),
                    Vector2.zero, Vector2.zero);
            }

            if (_nameText != null)
            {
                // 완성 카드의 이름은 추후 이미지 속 이름표에 맞춰 따로 작업합니다.
                // 지금은 기존 흰색 고딕 이름이 그림 위에 중복되지 않게 숨깁니다.
                _nameText.gameObject.SetActive(!isComposed);
                if (!isComposed)
                {
                    CardFrameView.Place(_nameText.rectTransform,
                        new Vector2(.25f, .85f), new Vector2(.94f, .96f),
                        Vector2.zero, Vector2.zero);
                    _nameText.color = Color.black;
                    _nameText.transform.SetAsLastSibling();
                }
            }

            // 숫자는 PNG에 굽지 않고 Unity Text로 표시해 프리팹에서 위치를 조정할 수 있게 합니다.
            if (_numberText != null)
                _numberText.gameObject.SetActive(_cardData.IsNumberCard);
            if (_bottomNumberText != null)
                _bottomNumberText.gameObject.SetActive(_cardData.IsNumberCard);
            if (_numberBadgeImage != null)
                _numberBadgeImage.gameObject.SetActive(_cardData.IsNumberCard);
        }

        /// <summary>
        /// 이름이 있는 캐릭터 카드에 세계별 투명 프레임을 얹습니다.
        /// 일러스트와 프레임은 별도 레이어이므로 원본 이미지는 손상되지 않습니다.
        /// </summary>
        public void SetArtworkFrame(Sprite frame)
        {
            _hasArtworkFrame = frame != null;
            if (_cardFrameView != null)
                _cardFrameView.SetArtworkFrame(frame);

            // 전용 카드 프레임이 있으면 예전 타입 색상 배경을 완전히 제거합니다.
            // 레이어는 투명 바탕 → 캐릭터 일러스트 → 전용 프레임만 남습니다.
            Image rootBackground = GetComponent<Image>();
            if (_hasArtworkFrame)
            {
                if (rootBackground != null)
                    rootBackground.color = Color.white;
                if (_backgroundImage != null)
                {
                    _backgroundImage.sprite = null;
                    _backgroundImage.color = Color.clear;
                }
                if (_frameImage != null)
                    _frameImage.color = Color.clear;
            }
            else
            {
                if (rootBackground != null)
                    rootBackground.color = Color.white;
                ApplyColors(_cardData);
            }

            if (_illustrationImage != null && _illustrationImage.sprite != null && !_faceDown)
            {
                _illustrationImage.preserveAspect = !_hasArtworkFrame;
                CardFrameView.Place(_illustrationImage.rectTransform,
                    _hasArtworkFrame ? new Vector2(.04f, .012f) : new Vector2(.04f, .15f),
                    _hasArtworkFrame ? new Vector2(.96f, .91f) : new Vector2(.96f, .85f), Vector2.zero, Vector2.zero);
            }

            if (_nameText != null)
            {
                CardFrameView.Place(_nameText.rectTransform,
                    _hasArtworkFrame ? new Vector2(.18f, .035f) : new Vector2(.25f, .85f),
                    _hasArtworkFrame ? new Vector2(.82f, .145f) : new Vector2(.94f, .96f),
                    Vector2.zero, Vector2.zero);
                _nameText.color = _hasArtworkFrame ? Color.white : Color.black;
                _nameText.transform.SetAsLastSibling();
            }

            // 숫자의 위치·글꼴·색상은 CardPrefab의 Text 설정을 그대로 사용합니다.
        }

        /// <summary>
        /// 소유자 라벨을 설정합니다 (트릭 영역에서 누가 냈는지 표시).
        /// </summary>
        public void SetOwnerLabel(string ownerName)
        {
            if (_ownerLabel != null)
            {
                _ownerLabel.text = ownerName;
                _ownerLabel.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 타이그리스 카드의 선택 모드(해적/도주)를 카드 이름에 표시합니다.
        /// 트릭 영역에서 모든 플레이어가 선택 결과를 알 수 있습니다.
        /// </summary>
        public void SetTigressMode(TigressChoice mode)
        {
            if (_nameText == null) return;

            string modeLabel = mode switch
            {
                TigressChoice.Pirate => "무협",
                TigressChoice.Escape => "항복",
                _ => ""
            };

            if (!string.IsNullOrEmpty(modeLabel))
            {
                _nameText.text = $"암살자\n<size=14>({modeLabel})</size>";
            }

            // 프레임 색상도 선택에 맞게 변경하여 시각적으로 즉시 구분 가능
            Color modeFrameColor = mode switch
            {
                TigressChoice.Pirate => new Color(0.72f, 0.15f, 0.15f), // 해적 빨강
                TigressChoice.Escape => new Color(0.55f, 0.55f, 0.52f), // 도주 회색
                _ => new Color(0.80f, 0.50f, 0.10f) // 기본 타이그리스 색
            };

            if (_frameImage != null)
                _frameImage.color = modeFrameColor;
            if (_backgroundImage != null)
                _backgroundImage.color = modeFrameColor;

            Color modeBannerColor = mode switch
            {
                TigressChoice.Pirate => new Color(0.55f, 0.08f, 0.08f, 0.88f),
                TigressChoice.Escape => new Color(0.40f, 0.40f, 0.38f, 0.88f),
                _ => new Color(0.62f, 0.35f, 0.05f, 0.88f)
            };

            if (_nameBannerImage != null)
                _nameBannerImage.color = modeBannerColor;
        }

        // ============================================================
        // 색상 적용
        // ============================================================

        private void ApplyColors(CardData card)
        {
            Color frameColor = GetFrameColor(card);
            Color bannerColor = GetBannerColor(card);

            // 프레임 (새 레이아웃)
            if (_frameImage != null)
            {
                _frameImage.color = frameColor;
            }

            // 하위 호환: 기존 _backgroundImage가 있으면 프레임 색상 적용
            if (_backgroundImage != null)
            {
                _backgroundImage.color = frameColor;
            }

            // 이름 배너 배경
            if (_nameBannerImage != null)
            {
                _nameBannerImage.color = bannerColor;
            }

            // 숫자 뱃지 배경 (프레임과 동일 계열, 약간 어둡게)
            if (_numberBadgeImage != null)
            {
                _numberBadgeImage.color = new Color(
                    frameColor.r * 0.7f,
                    frameColor.g * 0.7f,
                    frameColor.b * 0.7f,
                    0.85f
                );
            }
        }

        // ============================================================
        // 프레임 색상 — 카드 타입 즉시 인식용 (테두리)
        // ============================================================

        /// <summary>
        /// 카드 외곽 프레임 색상. 카드 타입을 한눈에 구분하는 핵심 요소.
        /// </summary>
        private Color GetFrameColor(CardData card)
        {
            if (card.IsSpecialCard)
            {
                return card.Special switch
                {
                    SpecialType.Pirate => new Color(0.72f, 0.15f, 0.15f),
                    SpecialType.Mermaid => new Color(0.10f, 0.55f, 0.65f),
                    SpecialType.SkullKing => new Color(0.18f, 0.05f, 0.05f),
                    SpecialType.Tigress => new Color(0.80f, 0.50f, 0.10f),
                    SpecialType.Kraken => new Color(0.08f, 0.08f, 0.25f),
                    SpecialType.WhiteWhale => new Color(0.85f, 0.88f, 0.92f),
                    SpecialType.Escape => new Color(0.55f, 0.55f, 0.52f),
                    SpecialType.Jewel => new Color(0.82f, 0.68f, 0.20f),
                    _ => Color.white
                };
            }

            return card.Suit switch
            {
                CardSuit.Green => new Color(0.18f, 0.42f, 0.22f),
                CardSuit.Yellow => new Color(0.78f, 0.63f, 0.22f),
                CardSuit.Purple => new Color(0.45f, 0.18f, 0.62f),
                CardSuit.Black => new Color(0.12f, 0.08f, 0.18f),
                _ => Color.white
            };
        }

        // ============================================================
        // 배너 색상 — 하단 이름 배너 (프레임과 조화, 약간 밝은 톤)
        // ============================================================

        private Color GetBannerColor(CardData card)
        {
            if (card.IsSpecialCard)
            {
                return card.Special switch
                {
                    SpecialType.Pirate => new Color(0.55f, 0.08f, 0.08f, 0.88f),
                    SpecialType.Mermaid => new Color(0.05f, 0.38f, 0.50f, 0.88f),
                    SpecialType.SkullKing => new Color(0.12f, 0.02f, 0.02f, 0.92f),
                    SpecialType.Tigress => new Color(0.62f, 0.35f, 0.05f, 0.88f),
                    SpecialType.Kraken => new Color(0.05f, 0.05f, 0.18f, 0.92f),
                    SpecialType.WhiteWhale => new Color(0.70f, 0.75f, 0.82f, 0.88f),
                    SpecialType.Escape => new Color(0.40f, 0.40f, 0.38f, 0.88f),
                    SpecialType.Jewel => new Color(0.65f, 0.52f, 0.12f, 0.88f),
                    _ => new Color(1f, 1f, 1f, 0.85f)
                };
            }

            return card.Suit switch
            {
                CardSuit.Green => new Color(0.22f, 0.50f, 0.28f, 0.88f),
                CardSuit.Yellow => new Color(0.62f, 0.48f, 0.15f, 0.88f),
                CardSuit.Purple => new Color(0.35f, 0.12f, 0.50f, 0.88f),
                CardSuit.Black => new Color(0.15f, 0.10f, 0.22f, 0.92f),
                _ => new Color(1f, 1f, 1f, 0.85f)
            };
        }

        // ============================================================
        // 클릭 처리
        // ============================================================

        private void OnClicked()
        {
            if (_altPreviewOwner == this) return;

            if (_uiManager != null && _isPlayable)
            {
                _uiManager.OnCardClicked(_cardData);
            }
        }

        // ============================================================
        // 호버 효과 (손패 전용) — 원본 카드 직접 제어
        // ============================================================

        private int _originalSiblingIndex;

        /// <summary>
        /// 이 카드가 손패에 있는 카드임을 설정하고 원본 스케일을 기억합니다.
        /// </summary>
        public void SetAsHandCard(Vector3 baseScale, int siblingIndex)
        {
            _isInHand = true;
            _originalScale = baseScale;
            _originalSiblingIndex = siblingIndex;
            _isHovered = false;
            _hasOriginalPos = false; // 아직 배치 전이므로 나중에 캡처
        }

        /// <summary>Public played-card inspection: reuse hover/Alt without allowing submission.</summary>
        public void SetAsReadOnlyInspection(Vector3 baseScale)
        {
            StopAllCoroutines();
            _hoverScaleCoroutine = null;
            _isExitAnimating = false;
            if (_altPreviewOwner == this) HideAltPreview();
            _isPlayable = false;
            _uiManager = null;
            if (_ownerLabel != null) _ownerLabel.gameObject.SetActive(false);
            if (_disabledOverlay != null) _disabledOverlay.SetActive(false);
            if (_button != null) _button.enabled = false;
            transform.localScale = baseScale;
            SetAsHandCard(baseScale, transform.GetSiblingIndex());
            CaptureOriginalPosition();
        }

        /// <summary>
        /// RepositionHandCards() 이후 호출하여 정확한 원본 위치를 기록합니다.
        /// </summary>
        public void CaptureOriginalPosition()
        {
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                _originalAnchoredPos = rt.anchoredPosition;
                _hasOriginalPos = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInHand) return;
            if (_isHovered) return;
            _isHovered = true;

            // 원본 위치 캡처 (강제 고정용)
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                _originalAnchoredPos = rt.anchoredPosition;
                _hasOriginalPos = true;
            }

            // 축소 애니메이션 중이었다면 중단
            if (_isExitAnimating)
            {
                StopAllCoroutines();
                _isExitAnimating = false;
            }

            // 다른 카드 위에 렌더링 (Update()에서 위치 강제 고정하므로 안전)
            transform.SetAsLastSibling();

            // 부드러운 확대 애니메이션 시작
            _hoverScaleCoroutine = StartCoroutine(AnimateHoverScale());

            // 위치 즉시 고정
            if (rt != null) rt.anchoredPosition = _originalAnchoredPos;

            // 프레임 하이라이트
            if (_cardFrameView != null)
                _cardFrameView.SetHighlighted(true);
            if (_frameImage != null)
            {
                _savedFrameColor = _frameImage.color;
                _frameImage.color = Color.white;
            }
        }

        private Coroutine _hoverScaleCoroutine;
        private bool _isExitAnimating = false;  // 축소 애니메이션 중 여부

        /// <summary>
        /// 호버 시 부드러운 확대 애니메이션 (ease-out back, 0.3초)
        /// </summary>
        private IEnumerator AnimateHoverScale()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = _originalScale * 1.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float overshoot = 1.4f;
                float ease = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f)
                               + overshoot * Mathf.Pow(t - 1f, 2f);

                transform.localScale = Vector3.Lerp(startScale, targetScale, ease);
                yield return null;
            }

            transform.localScale = targetScale;
            _hoverScaleCoroutine = null;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isInHand) return;
            ExitHover();
        }

        /// <summary>
        /// 매 프레임:
        /// 1) 위치를 원본으로 강제 고정
        /// 2) 확대 코루틴 완료 후 스케일 강제 고정
        /// 3) 마우스가 카드 영역을 벗어나면 호버 해제
        /// </summary>
        private void Update()
        {
            bool altPressed = Keyboard.current != null
                && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);

            if (_isInHand && _isHovered && altPressed)
            {
                ShowAltPreview();
            }
            else if (_altPreviewOwner == this)
            {
                HideAltPreview();
            }

            // 축소 애니메이션 중에는 간섭하지 않음
            if (_isExitAnimating) return;

            if (!_isInHand || !_isHovered) return;

            // 매 프레임 위치 강제 고정
            if (_hasOriginalPos)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = _originalAnchoredPos;
                }
            }

            // 확대 코루틴이 끝난 후에만 스케일 강제 고정
            if (_hoverScaleCoroutine == null)
            {
                transform.localScale = _originalScale * 1.35f;
            }
        }



        /// <summary>
        /// 호버 해제 — 부드러운 축소 후 원래 상태로 복원
        /// </summary>
        private void ExitHover()
        {
            _isHovered = false;
            if (_altPreviewOwner == this) HideAltPreview();

            // 실행 중인 확대 코루틴 중단
            if (_hoverScaleCoroutine != null)
            {
                StopCoroutine(_hoverScaleCoroutine);
                _hoverScaleCoroutine = null;
            }

            // ★ 즉시 복원: 렌더링 순서 + 프레임 색상 (다음 호버 카드가 가려지지 않도록)
            transform.SetSiblingIndex(_originalSiblingIndex);
            if (_cardFrameView != null)
                _cardFrameView.SetHighlighted(false);
            if (_frameImage != null)
                _frameImage.color = _savedFrameColor;

            // 부드러운 축소 애니메이션 시작 (스케일만 담당)
            StartCoroutine(AnimateExitScale());
        }

        /// <summary>
        /// 현재 카드의 일러스트를 화면 중앙에 크게 표시합니다.
        /// 숫자 카드는 프레임/배경 없이 프리팹의 숫자 Text만 함께 확대합니다.
        /// </summary>
        private void ShowAltPreview()
        {
            if (_altPreviewOwner == this && _altPreviewRoot != null) return;
            HideAltPreview();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;

            _altPreviewRoot = new GameObject("AltCardPreviewOverlay", typeof(RectTransform));
            _altPreviewRoot.layer = gameObject.layer;
            _altPreviewRoot.transform.SetParent(rootCanvas.transform, false);

            RectTransform overlayRect = _altPreviewRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();

            if (_illustrationImage == null || _illustrationImage.sprite == null)
            {
                HideAltPreview();
                return;
            }

            GameObject previewArtwork = new GameObject(
                "AltCardArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            previewArtwork.layer = gameObject.layer;
            previewArtwork.transform.SetParent(_altPreviewRoot.transform, false);

            Image previewImage = previewArtwork.GetComponent<Image>();
            previewImage.sprite = _illustrationImage.sprite;
            previewImage.color = Color.white;
            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 900f;
            float previewHeight = Mathf.Min(720f, canvasHeight * 0.82f);
            if (previewHeight <= 0f) previewHeight = 650f;

            RectTransform cardRect = previewArtwork.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(previewHeight * 13f / 19f, previewHeight);
            cardRect.localScale = Vector3.one;
            cardRect.localRotation = Quaternion.identity;

            if (_cardData != null && _cardData.IsNumberCard)
            {
                AddAltPreviewNumber(_numberText, cardRect, false);

                // 우하단 숫자가 프리팹에 연결되어 있으면 그 설정을 그대로 사용합니다.
                // 아직 연결하지 않은 프리팹도 좌상단 숫자를 대각선으로 복제해 빠짐없이 표시합니다.
                if (_bottomNumberText != null)
                    AddAltPreviewNumber(_bottomNumberText, cardRect, false);
                else
                    AddAltPreviewNumber(_numberText, cardRect, true);
            }

            _altPreviewOwner = this;
        }

        /// <summary>
        /// 카드 프리팹의 Text 스타일과 카드 기준 위치를 확대 이미지 위에 복사합니다.
        /// 따라서 Inspector에서 글꼴, 색, 외곽선, 위치를 바꾸면 Alt 확대에도 그대로 반영됩니다.
        /// </summary>
        private void AddAltPreviewNumber(Text source, RectTransform previewCard, bool mirrorToOppositeCorner)
        {
            if (source == null || previewCard == null) return;

            RectTransform sourceCard = transform as RectTransform;
            if (sourceCard == null || sourceCard.rect.width <= 0f || sourceCard.rect.height <= 0f) return;

            GameObject numberObject = Instantiate(source.gameObject, previewCard, false);
            numberObject.name = mirrorToOppositeCorner ? "AltBottomNumber" : $"Alt{source.gameObject.name}";
            numberObject.layer = gameObject.layer;
            numberObject.SetActive(true);

            Text previewText = numberObject.GetComponent<Text>();
            if (previewText == null)
            {
                Destroy(numberObject);
                return;
            }

            previewText.text = _cardData.Number.ToString();
            previewText.raycastTarget = false;

            RectTransform sourceRect = source.rectTransform;
            RectTransform numberRect = previewText.rectTransform;
            Bounds sourceBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(sourceCard, sourceRect);
            float scaleX = previewCard.rect.width / sourceCard.rect.width;
            float scaleY = previewCard.rect.height / sourceCard.rect.height;
            float textScale = Mathf.Min(scaleX, scaleY);

            Vector3 center = sourceBounds.center;
            Quaternion relativeRotation = Quaternion.Inverse(sourceCard.rotation) * sourceRect.rotation;
            if (mirrorToOppositeCorner)
            {
                center.x = -center.x;
                center.y = -center.y;
                relativeRotation *= Quaternion.Euler(0f, 0f, 180f);
            }

            numberRect.anchorMin = new Vector2(0.5f, 0.5f);
            numberRect.anchorMax = new Vector2(0.5f, 0.5f);
            numberRect.pivot = new Vector2(0.5f, 0.5f);
            numberRect.anchoredPosition = new Vector2(center.x * scaleX, center.y * scaleY);
            numberRect.sizeDelta = new Vector2(sourceBounds.size.x * scaleX, sourceBounds.size.y * scaleY);
            numberRect.localScale = Vector3.one;
            numberRect.localRotation = relativeRotation;

            previewText.fontSize = Mathf.Max(1, Mathf.RoundToInt(source.fontSize * textScale));
            previewText.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(source.resizeTextMinSize * textScale));
            previewText.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(source.resizeTextMaxSize * textScale));

            // Outline/Shadow의 픽셀 거리도 카드 확대 비율에 맞춥니다.
            Shadow[] shadows = numberObject.GetComponents<Shadow>();
            foreach (Shadow shadow in shadows)
                shadow.effectDistance *= textScale;
        }

        private static void HideAltPreview()
        {
            if (_altPreviewRoot != null) Destroy(_altPreviewRoot);
            _altPreviewRoot = null;
            _altPreviewOwner = null;
        }

        private void OnDisable()
        {
            if (_altPreviewOwner == this) HideAltPreview();
        }

        /// <summary>
        /// 호버 해제 시 부드러운 축소 애니메이션 (ease-out, 0.2초)
        /// </summary>
        private IEnumerator AnimateExitScale()
        {
            _isExitAnimating = true;

            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = _originalScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, targetScale, ease);

                if (_hasOriginalPos)
                {
                    var rt = GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = _originalAnchoredPos;
                }

                yield return null;
            }

            transform.localScale = targetScale;

            if (_hasOriginalPos)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = _originalAnchoredPos;
            }

            _isExitAnimating = false;
        }

        // ============================================================
        // 카드 등장 애니메이션 (트릭 영역)
        // ============================================================

        /// <summary>
        /// 카드가 트릭 영역에 등장할 때 슬라이드+스케일 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="targetPos">최종 anchoredPosition</param>
        /// <param name="targetScale">최종 localScale</param>
        /// <param name="startOffset">시작 위치 오프셋 (targetPos 기준)</param>
        public void PlayEntryAnimation(Vector2 targetPos, Vector3 targetScale, Vector2 startOffset)
        {
            StartCoroutine(AnimateEntry(targetPos, targetScale, startOffset));
        }

        private IEnumerator AnimateEntry(Vector2 targetPos, Vector3 targetScale, Vector2 startOffset)
        {
            _isAnimating = true;

            var rt = GetComponent<RectTransform>();
            if (rt == null) { _isAnimating = false; yield break; }

            Vector2 startPos = targetPos + startOffset;
            Vector3 startScale = targetScale * 0.3f;

            // 시작 상태 설정
            rt.anchoredPosition = startPos;
            transform.localScale = startScale;

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease-out cubic — 빠르게 출발, 부드럽게 착지
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                transform.localScale = Vector3.Lerp(startScale, targetScale, ease);

                yield return null;
            }

            // 최종 위치 정확히 맞춤
            rt.anchoredPosition = targetPos;
            transform.localScale = targetScale;
            _isAnimating = false;
        }
        /// <summary>
        /// 상대방 카드용: 숫자와 이름 텍스트 등을 숨기고 완벽한 뒷면으로 만듭니다.
        /// </summary>
        public void SetAsBack()
        {
            _faceDown = true;
            if (_cardFrameView != null) _cardFrameView.gameObject.SetActive(false);
            if (_disabledOverlay != null) _disabledOverlay.SetActive(false);
            if (_ownerLabel != null) _ownerLabel.gameObject.SetActive(false);
            if (_nameText != null) _nameText.gameObject.SetActive(false);
            if (_numberText != null) _numberText.gameObject.SetActive(false);
            if (_bottomNumberText != null) _bottomNumberText.gameObject.SetActive(false);
            if (_numberBadgeImage != null) _numberBadgeImage.gameObject.SetActive(false);
            if (_nameBannerImage != null) _nameBannerImage.gameObject.SetActive(false);

            // ★ 카드 테두리는 아주 어둡게, 배경은 짙은 회색으로 칠해서 낱장이 확실히 구분되게 만듭니다!
            if (_frameImage != null) _frameImage.color = new Color(0.1f, 0.1f, 0.1f);
            if (_backgroundImage != null) _backgroundImage.color = new Color(0.3f, 0.3f, 0.3f);

            var tilt = GetComponent<CardTiltEffect>();
            if (tilt != null) tilt.enabled = false;
        }
    }
}
