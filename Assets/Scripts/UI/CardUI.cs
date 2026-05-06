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

        // ---- 호버 효과용 ----
        private bool _isInHand = false;        // 손패 카드인지 여부
        private Vector3 _originalScale;
        private Vector2 _originalAnchoredPos;  // 절대 원본 위치 (anchoredPosition)
        private float _originalRotationZ;      // 원본 Z 회전값 (부채꼴 배치용)
        private bool _hasOriginalPos = false;  // 원본 위치가 설정되었는지
        private bool _isHovered = false;
        private float _hoverYOffset = 40f;     // 호버 시 위로 올라가는 픽셀
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

            // ---- 이름 텍스트 (하단 배너) ----
            if (_nameText != null)
            {
                _nameText.text = card.DisplayName;
            }

            // ---- 숫자 텍스트 (좌상단) ----
            if (_numberText != null)
            {
                _numberText.text = card.IsNumberCard ? card.Number.ToString() : "";
            }

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
        /// 카드 일러스트를 설정합니다.
        /// CardSpriteDatabase에서 가져온 Sprite를 적용합니다.
        /// </summary>
        public void SetIllustration(Sprite illustration)
        {
            if (_illustrationImage != null && illustration != null)
            {
                _illustrationImage.sprite = illustration;
                _illustrationImage.gameObject.SetActive(true);
            }
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
                TigressChoice.Pirate => "용사",
                TigressChoice.Escape => "항복",
                _ => ""
            };

            if (!string.IsNullOrEmpty(modeLabel))
            {
                _nameText.text = $"타이그리스\n<size=14>({modeLabel})</size>";
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

            // ★ 루트 Image 색상 직접 설정 (테두리 색상)
            // _frameImage가 프리팹에서 연결 안 되어 있어도 루트 Image를 직접 찾아서 적용
            var rootImage = GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = frameColor;
            }

            // 프레임 (Inspector에서 연결된 경우)
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
                    SpecialType.Pirate    => new Color(0.72f, 0.15f, 0.15f),
                    SpecialType.Mermaid   => new Color(0.10f, 0.55f, 0.65f),
                    SpecialType.SkullKing => new Color(0.18f, 0.05f, 0.05f),
                    SpecialType.Tigress   => new Color(0.80f, 0.50f, 0.10f),
                    SpecialType.Kraken    => new Color(0.08f, 0.08f, 0.25f),
                    SpecialType.WhiteWhale => new Color(0.85f, 0.88f, 0.92f),
                    SpecialType.Escape    => new Color(0.55f, 0.55f, 0.52f),
                    SpecialType.Jewel     => new Color(0.82f, 0.68f, 0.20f),
                    _ => Color.white
                };
            }

            return card.Suit switch
            {
                CardSuit.Green  => new Color(0.18f, 0.42f, 0.22f),
                CardSuit.Yellow => new Color(0.78f, 0.63f, 0.22f),
                CardSuit.Purple => new Color(0.45f, 0.18f, 0.62f),
                CardSuit.Black  => new Color(0.12f, 0.08f, 0.18f),
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
                    SpecialType.Pirate     => new Color(0.55f, 0.08f, 0.08f, 0.88f),
                    SpecialType.Mermaid    => new Color(0.05f, 0.38f, 0.50f, 0.88f),
                    SpecialType.SkullKing  => new Color(0.12f, 0.02f, 0.02f, 0.92f),
                    SpecialType.Tigress    => new Color(0.62f, 0.35f, 0.05f, 0.88f),
                    SpecialType.Kraken     => new Color(0.05f, 0.05f, 0.18f, 0.92f),
                    SpecialType.WhiteWhale => new Color(0.70f, 0.75f, 0.82f, 0.88f),
                    SpecialType.Escape     => new Color(0.40f, 0.40f, 0.38f, 0.88f),
                    SpecialType.Jewel      => new Color(0.65f, 0.52f, 0.12f, 0.88f),
                    _ => new Color(1f, 1f, 1f, 0.85f)
                };
            }

            return card.Suit switch
            {
                CardSuit.Green  => new Color(0.22f, 0.50f, 0.28f, 0.88f),
                CardSuit.Yellow => new Color(0.62f, 0.48f, 0.15f, 0.88f),
                CardSuit.Purple => new Color(0.35f, 0.12f, 0.50f, 0.88f),
                CardSuit.Black  => new Color(0.15f, 0.10f, 0.22f, 0.92f),
                _ => new Color(1f, 1f, 1f, 0.85f)
            };
        }

        // ============================================================
        // 클릭 처리
        // ============================================================

        private void OnClicked()
        {
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

        /// <summary>
        /// RepositionHandCards() 이후 호출하여 정확한 원본 위치를 기록합니다.
        /// </summary>
        public void CaptureOriginalPosition()
        {
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                _originalAnchoredPos = rt.anchoredPosition;
                _originalRotationZ = rt.localEulerAngles.z;
                _hasOriginalPos = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInHand) return;

            // 보류 중인 지연 퇴장을 취소 (경계선 깜빡임 방지)
            if (_exitDelayCoroutine != null)
            {
                StopCoroutine(_exitDelayCoroutine);
                _exitDelayCoroutine = null;
            }

            if (_isHovered) return;
            _isHovered = true;

            // 원본 위치/회전 캡처 (강제 고정용)
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                _originalAnchoredPos = rt.anchoredPosition;
                _originalRotationZ = rt.localEulerAngles.z;
                _hasOriginalPos = true;
            }

            // 축소 애니메이션 중이었다면 중단
            if (_isExitAnimating)
            {
                StopAllCoroutines();
                _isExitAnimating = false;
                _exitDelayCoroutine = null;
            }

            // 다른 카드 위에 렌더링 (Update()에서 위치 강제 고정하므로 안전)
            transform.SetAsLastSibling();

            // 부드러운 확대 애니메이션 시작
            _hoverScaleCoroutine = StartCoroutine(AnimateHoverScale());

            // 위치를 위로 올리고 회전을 0으로 (부채꼴에서 똑바로)
            if (rt != null)
            {
                rt.anchoredPosition = _originalAnchoredPos + new Vector2(0, _hoverYOffset);
                rt.localEulerAngles = Vector3.zero;
            }

            // 프레임 하이라이트
            if (_frameImage != null)
            {
                _savedFrameColor = _frameImage.color;
                _frameImage.color = Color.white;
            }
        }

        private Coroutine _hoverScaleCoroutine;
        private Coroutine _exitDelayCoroutine;   // 지연 퇴장 코루틴 (깜빡임 방지)
        private bool _isExitAnimating = false;    // 축소 애니메이션 중 여부

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
            // 즉시 퇴장하지 않고 짧은 지연을 둬서 경계선 깜빡임을 방지
            if (_exitDelayCoroutine != null) StopCoroutine(_exitDelayCoroutine);
            _exitDelayCoroutine = StartCoroutine(DelayedExitHover());
        }

        /// <summary>
        /// 짧은 지연 후 호버 해제. 카드가 위로 이동하면서 마우스가 경계선을
        /// 벗어났다 다시 들어오는 깜빡임 현상을 방지합니다.
        /// </summary>
        private IEnumerator DelayedExitHover()
        {
            yield return new WaitForSeconds(0.06f);
            _exitDelayCoroutine = null;
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
            // 축소 애니메이션 중에는 간섭하지 않음
            if (_isExitAnimating) return;

            if (!_isInHand || !_isHovered) return;

            // 매 프레임 위치 + 회전 강제 고정 (호버 상태: 위로 올라감 + 회전 0)
            if (_hasOriginalPos)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = _originalAnchoredPos + new Vector2(0, _hoverYOffset);
                    rt.localEulerAngles = Vector3.zero;
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

            // 실행 중인 확대 코루틴 중단
            if (_hoverScaleCoroutine != null)
            {
                StopCoroutine(_hoverScaleCoroutine);
                _hoverScaleCoroutine = null;
            }

            // ★ 즉시 복원: 렌더링 순서 + 프레임 색상 (다음 호버 카드가 가려지지 않도록)
            transform.SetSiblingIndex(_originalSiblingIndex);
            if (_frameImage != null)
                _frameImage.color = _savedFrameColor;

            // 부드러운 축소 애니메이션 시작 (스케일만 담당)
            StartCoroutine(AnimateExitScale());
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
            Vector3 startRotation = transform.localEulerAngles;
            Vector3 targetRotation = new Vector3(0, 0, _originalRotationZ);

            // 현재 호버 위치에서 시작
            var rt = GetComponent<RectTransform>();
            Vector2 startPos = rt != null ? rt.anchoredPosition : _originalAnchoredPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, targetScale, ease);

                if (_hasOriginalPos && rt != null)
                {
                    rt.anchoredPosition = Vector2.Lerp(startPos, _originalAnchoredPos, ease);
                    // 회전도 부드럽게 복원
                    transform.localEulerAngles = Vector3.Lerp(startRotation, targetRotation, ease);
                }

                yield return null;
            }

            transform.localScale = targetScale;
            transform.localEulerAngles = targetRotation;

            if (_hasOriginalPos && rt != null)
            {
                rt.anchoredPosition = _originalAnchoredPos;
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


    }
}
