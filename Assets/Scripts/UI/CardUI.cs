// ============================================================
// CardUI.cs — 카드 프리팹에 부착하는 UI 컴포넌트
// 회원님이 카드 프리팹을 만들 때 이 스크립트를 부착합니다.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using SkullKing.Data;

namespace SkullKing.UI
{
    /// <summary>
    /// 개별 카드 프리팹에 부착하는 컴포넌트.
    /// 카드의 비주얼을 설정하고 클릭 이벤트를 처리합니다.
    /// </summary>
    public class CardUI : MonoBehaviour
    {
        [Header("카드 앞면 요소들")]
        [Tooltip("카드 이름 텍스트")]
        [SerializeField] private Text _nameText;

        [Tooltip("카드 숫자 텍스트")]
        [SerializeField] private Text _numberText;

        [Tooltip("카드 배경 이미지")]
        [SerializeField] private Image _backgroundImage;

        [Tooltip("카드 일러스트 이미지")]
        [SerializeField] private Image _illustrationImage;

        [Header("소유자 라벨 (트릭 영역에서 사용)")]
        [SerializeField] private Text _ownerLabel;

        [Header("선택 불가 시 어둡게 만드는 오버레이")]
        [SerializeField] private GameObject _disabledOverlay;

        [Header("카드 버튼 (클릭 가능한 경우)")]
        [SerializeField] private Button _button;

        // ---- 내부 상태 ----
        private CardData _cardData;
        private UIBindingManager _uiManager;
        private bool _isPlayable;

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

            // 텍스트 설정
            if (_nameText != null)
            {
                _nameText.text = card.DisplayName;
            }

            if (_numberText != null)
            {
                _numberText.text = card.IsNumberCard ? card.Number.ToString() : "";
            }

            // 배경색 설정 (추후 이미지로 교체 가능)
            if (_backgroundImage != null)
            {
                _backgroundImage.color = GetCardColor(card);
            }

            // 플레이 가능 여부에 따른 시각적 처리
            if (_disabledOverlay != null)
            {
                _disabledOverlay.SetActive(!isPlayable);
            }

            // 버튼 활성화
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
        /// 카드 일러스트 이미지를 설정합니다.
        /// 추후 Flow AI로 생성한 이미지를 적용할 때 사용합니다.
        /// </summary>
        public void SetIllustration(Sprite illustration)
        {
            if (_illustrationImage != null && illustration != null)
            {
                _illustrationImage.sprite = illustration;
                _illustrationImage.gameObject.SetActive(true);
            }
        }

        // ---- 클릭 처리 ----
        private void OnClicked()
        {
            if (_uiManager != null && _isPlayable)
            {
                _uiManager.OnCardClicked(_cardData);
            }
        }

        // ---- 색상 헬퍼 ----
        private Color GetCardColor(CardData card)
        {
            if (card.IsSpecialCard)
            {
                return card.Special switch
                {
                    SpecialType.SkullKing => new Color(0.2f, 0.0f, 0.0f), // 진한 붉은색
                    SpecialType.Mermaid => new Color(0.0f, 0.6f, 0.8f),   // 청록
                    SpecialType.Pirate => new Color(0.5f, 0.1f, 0.1f),    // 적갈색
                    SpecialType.Tigress => new Color(0.8f, 0.5f, 0.0f),   // 주황
                    SpecialType.Kraken => new Color(0.1f, 0.1f, 0.3f),    // 어두운 남색
                    SpecialType.WhiteWhale => new Color(0.9f, 0.9f, 1.0f),// 흰색
                    SpecialType.Escape => new Color(0.6f, 0.6f, 0.6f),    // 회색
                    _ => Color.white
                };
            }

            return card.Suit switch
            {
                CardSuit.Green => new Color(0.2f, 0.7f, 0.3f),   // 초록
                CardSuit.Yellow => new Color(0.9f, 0.8f, 0.2f),  // 노랑
                CardSuit.Purple => new Color(0.6f, 0.2f, 0.8f),  // 보라
                CardSuit.Black => new Color(0.15f, 0.15f, 0.2f), // 검정
                _ => Color.white
            };
        }
    }
}
