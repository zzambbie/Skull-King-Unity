// ============================================================
// PlayerProfilePanel.cs — 플레이어 프로필 패널
// 프론트뷰에서 각 플레이어의 정보를 표시하는 UI 컴포넌트.
// 아바타(컬러 원) + 이름 + 비딩/점수 정보를 표시합니다.
// 런타임에 동적으로 생성됩니다.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    /// <summary>
    /// 한 명의 플레이어 프로필을 표시하는 UI 컴포넌트.
    /// UIBindingManager에서 런타임에 생성하여 사용합니다.
    /// </summary>
    public class PlayerProfilePanel : MonoBehaviour
    {
        // UI 요소들 (런타임에 생성)
        private Image _avatarBg;
        private Text _nameText;
        private Text _infoText;
        private Text _cardCountText;
        private Image _highlightBorder;
        private CanvasGroup _canvasGroup;

        // 프로필 색상 팔레트 (플레이어 구분용)
        private static readonly Color[] AVATAR_COLORS = new Color[]
        {
            new Color(0.20f, 0.60f, 0.85f),  // 파랑
            new Color(0.85f, 0.30f, 0.25f),  // 빨강
            new Color(0.25f, 0.75f, 0.35f),  // 초록
            new Color(0.90f, 0.65f, 0.15f),  // 주황
            new Color(0.65f, 0.30f, 0.80f),  // 보라
            new Color(0.85f, 0.45f, 0.60f),  // 핑크
        };

        private int _playerIndex;
        private bool _isHuman;

        /// <summary>
        /// 프로필 패널을 생성하고 초기 설정합니다.
        /// </summary>
        /// <param name="playerIndex">플레이어 인덱스</param>
        /// <param name="playerName">플레이어 이름</param>
        /// <param name="isHuman">인간 플레이어인지</param>
        /// <param name="seatSlot">좌석 슬롯 (0~5)</param>
        public void Setup(int playerIndex, string playerName, bool isHuman, int seatSlot)
        {
            _playerIndex = playerIndex;
            _isHuman = isHuman;

            BuildUI(playerName, playerIndex, seatSlot);
        }

        /// <summary>
        /// 프로필 정보를 갱신합니다.
        /// </summary>
        public void UpdateInfo(int bid, int tricksWon, int totalScore, int cardCount, bool hasBid, bool isCurrentTurn)
        {
            if (_infoText != null)
            {
                string bidInfo = hasBid ? $"비딩:{bid}  승리:{tricksWon}" : "비딩 대기";
                _infoText.text = $"{bidInfo}\n{totalScore}점";
            }

            if (_cardCountText != null)
            {
                _cardCountText.text = cardCount > 0 ? cardCount.ToString() : "";
            }

            // 현재 턴 하이라이트
            SetHighlight(isCurrentTurn);
        }

        /// <summary>
        /// 현재 턴 하이라이트 토글
        /// </summary>
        public void SetHighlight(bool active)
        {
            if (_highlightBorder != null)
            {
                _highlightBorder.enabled = active;
            }
        }

        /// <summary>
        /// UI 요소들을 동적으로 생성합니다.
        /// </summary>
        private void BuildUI(string playerName, int playerIndex, int seatSlot)
        {
            var panelRt = GetComponent<RectTransform>();
            if (panelRt == null) panelRt = gameObject.AddComponent<RectTransform>();

            // 패널 크기 설정
            float panelWidth = _isHuman ? 160f : 130f;
            float panelHeight = _isHuman ? 70f : 60f;
            panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);

            // 패널 배경 (반투명 어두운 색)
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.03f, 0.10f, 0.75f);

            // CanvasGroup (투명도 제어용)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // --- 아바타 원 ---
            var avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(transform, false);
            var avatarRt = avatarObj.AddComponent<RectTransform>();
            float avatarSize = _isHuman ? 40f : 32f;
            avatarRt.anchorMin = new Vector2(0f, 0.5f);
            avatarRt.anchorMax = new Vector2(0f, 0.5f);
            avatarRt.pivot = new Vector2(0f, 0.5f);
            avatarRt.sizeDelta = new Vector2(avatarSize, avatarSize);
            avatarRt.anchoredPosition = new Vector2(8f, 0f);
            _avatarBg = avatarObj.AddComponent<Image>();
            Color avatarColor = AVATAR_COLORS[playerIndex % AVATAR_COLORS.Length];
            _avatarBg.color = avatarColor;

            // 아바타 내 이니셜
            var initialObj = new GameObject("Initial");
            initialObj.transform.SetParent(avatarObj.transform, false);
            var initialRt = initialObj.AddComponent<RectTransform>();
            initialRt.anchorMin = Vector2.zero;
            initialRt.anchorMax = Vector2.one;
            initialRt.offsetMin = Vector2.zero;
            initialRt.offsetMax = Vector2.zero;
            var initialText = initialObj.AddComponent<Text>();
            initialText.text = playerName.Length > 0 ? playerName[0].ToString() : "?";
            initialText.fontSize = _isHuman ? 18 : 14;
            initialText.alignment = TextAnchor.MiddleCenter;
            initialText.color = Color.white;
            initialText.fontStyle = FontStyle.Bold;
            initialText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- 이름 텍스트 ---
            float textLeft = avatarSize + 14f;

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);
            var nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.55f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(textLeft, 0f);
            nameRt.offsetMax = new Vector2(-5f, -3f);
            _nameText = nameObj.AddComponent<Text>();
            _nameText.text = playerName;
            _nameText.fontSize = _isHuman ? 14 : 12;
            _nameText.alignment = TextAnchor.MiddleLeft;
            _nameText.color = Color.white;
            _nameText.fontStyle = FontStyle.Bold;
            _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- 정보 텍스트 (비딩/점수) ---
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(transform, false);
            var infoRt = infoObj.AddComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0f);
            infoRt.anchorMax = new Vector2(1f, 0.55f);
            infoRt.offsetMin = new Vector2(textLeft, 3f);
            infoRt.offsetMax = new Vector2(-5f, 0f);
            _infoText = infoObj.AddComponent<Text>();
            _infoText.text = "";
            _infoText.fontSize = _isHuman ? 11 : 10;
            _infoText.alignment = TextAnchor.UpperLeft;
            _infoText.color = new Color(0.75f, 0.75f, 0.80f);
            _infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- 카드 카운트 뱃지 (상대 전용) ---
            if (!_isHuman)
            {
                var countObj = new GameObject("CardCount");
                countObj.transform.SetParent(transform, false);
                var countRt = countObj.AddComponent<RectTransform>();
                countRt.anchorMin = new Vector2(1f, 1f);
                countRt.anchorMax = new Vector2(1f, 1f);
                countRt.pivot = new Vector2(1f, 1f);
                countRt.sizeDelta = new Vector2(24f, 20f);
                countRt.anchoredPosition = new Vector2(4f, 4f);
                var countBg = countObj.AddComponent<Image>();
                countBg.color = new Color(0.15f, 0.10f, 0.25f, 0.9f);

                _cardCountText = countObj.AddComponent<Text>();
                // Text는 Image와 같은 오브젝트에 추가하면 충돌하므로 자식으로
                Destroy(_cardCountText);
                var countTextObj = new GameObject("CountText");
                countTextObj.transform.SetParent(countObj.transform, false);
                var countTextRt = countTextObj.AddComponent<RectTransform>();
                countTextRt.anchorMin = Vector2.zero;
                countTextRt.anchorMax = Vector2.one;
                countTextRt.offsetMin = Vector2.zero;
                countTextRt.offsetMax = Vector2.zero;
                _cardCountText = countTextObj.AddComponent<Text>();
                _cardCountText.fontSize = 12;
                _cardCountText.alignment = TextAnchor.MiddleCenter;
                _cardCountText.color = Color.white;
                _cardCountText.fontStyle = FontStyle.Bold;
                _cardCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // --- 턴 하이라이트 보더 ---
            var hlObj = new GameObject("Highlight");
            hlObj.transform.SetParent(transform, false);
            hlObj.transform.SetAsFirstSibling(); // 배경 뒤로
            var hlRt = hlObj.AddComponent<RectTransform>();
            hlRt.anchorMin = Vector2.zero;
            hlRt.anchorMax = Vector2.one;
            hlRt.offsetMin = new Vector2(-3f, -3f);
            hlRt.offsetMax = new Vector2(3f, 3f);
            _highlightBorder = hlObj.AddComponent<Image>();
            _highlightBorder.color = new Color(1f, 0.85f, 0.2f, 0.8f); // 금색 보더
            _highlightBorder.enabled = false;
        }
    }
}
