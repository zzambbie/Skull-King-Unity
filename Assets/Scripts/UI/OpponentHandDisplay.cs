// ============================================================
// OpponentHandDisplay.cs — 상대 플레이어의 카드 뒷면 표시
// 깔끔한 부채꼴 배치. 3D 회전 없이 2D로 자연스러운 손패 느낌.
// 좌석 위치에 따라 가로/세로 방향으로 카드를 팬 형태로 표시합니다.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    /// <summary>
    /// 한 명의 상대 플레이어의 카드 뒷면을 표시하는 컴포넌트.
    /// UIBindingManager에서 런타임에 생성하여 사용합니다.
    /// </summary>
    public class OpponentHandDisplay : MonoBehaviour
    {
        private int _seatSlot;
        private int _cardCount;
        private GameObject[] _cardBacks;

        // 카드 뒷면 크기
        private const float CARD_WIDTH = 40f;
        private const float CARD_HEIGHT = 58f;

        // 부채꼴 파라미터
        private const float CARD_SPACING = 16f;        // 카드 간 간격
        private const float FAN_ANGLE_PER_CARD = 4f;   // 카드당 회전 각도
        private const float MAX_FAN_ANGLE = 35f;       // 최대 총 펼침 각도

        public void Setup(int seatSlot, int cardCount)
        {
            _seatSlot = seatSlot;
            SetCardCount(cardCount);
        }

        public void SetCardCount(int count)
        {
            if (_cardBacks != null)
            {
                for (int i = 0; i < _cardBacks.Length; i++)
                {
                    if (_cardBacks[i] != null)
                        Destroy(_cardBacks[i]);
                }
            }

            _cardCount = count;
            _cardBacks = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                _cardBacks[i] = CreateCardBack(i, count);
            }
        }

        private GameObject CreateCardBack(int index, int total)
        {
            var cardObj = new GameObject($"CardBack_{index}");
            cardObj.transform.SetParent(transform, false);

            var rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

            // 카드 뒷면 배경
            var bgImage = cardObj.AddComponent<Image>();
            bgImage.color = new Color(0.10f, 0.06f, 0.14f, 1f);

            // 얇은 금색 테두리
            var borderObj = new GameObject("Border");
            borderObj.transform.SetParent(cardObj.transform, false);
            var borderRt = borderObj.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(1.5f, 1.5f);
            borderRt.offsetMax = new Vector2(-1.5f, -1.5f);
            var borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(0.55f, 0.42f, 0.15f, 0.45f);

            // 해골 심볼
            var symbolObj = new GameObject("Symbol");
            symbolObj.transform.SetParent(cardObj.transform, false);
            var symbolRt = symbolObj.AddComponent<RectTransform>();
            symbolRt.anchorMin = new Vector2(0.5f, 0.5f);
            symbolRt.anchorMax = new Vector2(0.5f, 0.5f);
            symbolRt.sizeDelta = new Vector2(20f, 20f);
            symbolRt.anchoredPosition = Vector2.zero;
            var symbolText = symbolObj.AddComponent<Text>();
            symbolText.text = "☠";
            symbolText.fontSize = 11;
            symbolText.alignment = TextAnchor.MiddleCenter;
            symbolText.color = new Color(0.55f, 0.42f, 0.15f, 0.5f);
            symbolText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            PositionCardInFan(rt, index, total);

            return cardObj;
        }

        /// <summary>
        /// 카드를 부채꼴로 배치합니다.
        /// 모든 좌석에서 동일한 가로 부채꼴을 사용하고,
        /// 컨테이너의 Z축 회전으로 방향을 전환합니다.
        /// </summary>
        private void PositionCardInFan(RectTransform rt, int index, int total)
        {
            if (total <= 0) return;

            // 부채꼴 각도
            float totalAngle = Mathf.Min(MAX_FAN_ANGLE, (total - 1) * FAN_ANGLE_PER_CARD);
            float t = total > 1 ? (float)index / (total - 1) : 0.5f;
            float angle = total > 1 ? Mathf.Lerp(-totalAngle / 2f, totalAngle / 2f, t) : 0f;

            // 가로 오프셋
            float centerOffset = (total - 1) * CARD_SPACING / 2f;
            float x = index * CARD_SPACING - centerOffset;

            // 아크 (양 끝이 약간 아래로)
            float normalizedPos = total > 1 ? (t - 0.5f) * 2f : 0f;
            float y = -(normalizedPos * normalizedPos) * totalAngle * 0.25f;

            rt.anchoredPosition = new Vector2(x, y);
            rt.localEulerAngles = new Vector3(0, 0, -angle);
        }
    }
}
