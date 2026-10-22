// ============================================================
// CardSpriteDatabase.cs — 카드 일러스트 매핑 데이터베이스
// ScriptableObject로 생성하여 Inspector에서 Sprite를 연결합니다.
// Unity 에디터: Assets > Create > SkullKing > Card Sprite Database
// ============================================================
using UnityEngine;

namespace SkullKing.Data
{
    /// <summary>
    /// 카드 타입별 일러스트 Sprite를 매핑하는 ScriptableObject.
    /// FLOW에서 생성한 이미지를 Import 후 여기 슬롯에 드래그&드롭합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CardSpriteDatabase", menuName = "SkullKing/Card Sprite Database")]
    public class CardSpriteDatabase : ScriptableObject
    {
        // ============================================================
        // 숫자 카드 — 수트별 대표 일러스트
        // ============================================================

        [Header("=== 숫자 카드 수트별 일러스트 ===")]

        [Tooltip("초록 수트 — 활 (레인저)")]
        public Sprite greenSuitSprite;

        [Tooltip("노랑 수트 — 검 (나이트)")]
        public Sprite yellowSuitSprite;

        [Tooltip("보라 수트 — 지팡이 (메이지)")]
        public Sprite purpleSuitSprite;

        [Tooltip("검정 수트 — 흑마도서 (다크메이지)")]
        public Sprite blackSuitSprite;

        [Header("=== CROSSION 숫자 카드 일러스트 풀 ===")]

        [Tooltip("초록 수트 / 무협 세계 일반병")]
        public Sprite[] greenSuitSprites;

        [Tooltip("노랑 수트 / 판타지 세계 일반병")]
        public Sprite[] yellowSuitSprites;

        [Tooltip("보라 수트 / 사이버펑크 세계 일반병")]
        public Sprite[] purpleSuitSprites;

        [Tooltip("검정 수트 / 공허 세계 일반병")]
        public Sprite[] blackSuitSprites;

        // ============================================================
        // 특수 카드 — 각각 고유 일러스트
        // ============================================================

        [Header("=== 무협 특수 — 5명 각각 다른 일러스트 ===")]

        [Tooltip("용사 1 — 예: 전사")]
        public Sprite pirate1Sprite;

        [Tooltip("용사 2 — 예: 마법사")]
        public Sprite pirate2Sprite;

        [Tooltip("용사 3 — 예: 궁수")]
        public Sprite pirate3Sprite;

        [Tooltip("용사 4 — 예: 도적")]
        public Sprite pirate4Sprite;

        [Tooltip("용사 5 — 예: 성기사")]
        public Sprite pirate5Sprite;

        [Header("=== 판타지 특수 — 용사/마왕 ===")]

        [Tooltip("MERMAID 1 — 빨간 판타지 용사")]
        public Sprite mermaid1Sprite;

        [Tooltip("MERMAID 2 — 빨간 판타지 마왕")]
        public Sprite mermaid2Sprite;

        [Header("=== 기타 특수 카드 ===")]

        [Tooltip("사이버펑크 — 보라색 보스")]
        public Sprite skullKingSprite;

        [Tooltip("암살자 — 무협 또는 항복 역할 선택")]
        public Sprite tigressSprite;

        [Tooltip("종말 — 트릭 무효화")]
        public Sprite krakenSprite;

        [Tooltip("천사 — 숫자만으로 판정")]
        public Sprite whiteWhaleSprite;

        [Tooltip("도주 — 도망/후퇴")]
        public Sprite escapeSprite;

        [Tooltip("네 세계의 항복 카드. 무협, 판타지, 사이버펑크, 공허 순서")]
        public Sprite[] escapeSprites;

        [Tooltip("보석 — 마력석/보석")]
        public Sprite jewelSprite;

        [Header("=== 카드 뒷면 ===")]

        [Tooltip("카드 뒷면 디자인")]
        public Sprite cardBackSprite;

        [Header("=== CROSSION 세계 프레임 ===")]
        public Sprite wuxiaFrameSprite;
        public Sprite fantasyFrameSprite;
        public Sprite cyberpunkFrameSprite;
        public Sprite voidFrameSprite;

        [Header("=== 특수항복 (동맹) 완성 카드 ===")]
        [Tooltip("악수 문양 프레임이 합쳐진 특수항복 카드 3장")]
        public Sprite[] allianceSprites;
        public Sprite[] allianceFrameSprites;

        // ============================================================
        // 스프라이트 조회 메서드
        // ============================================================

        /// <summary>
        /// 해적(용사) 카드의 덱 내 순서를 기반으로 고유 일러스트를 반환합니다.
        /// pirateIndex: 0~4 (덱 생성 순서 기준)
        /// </summary>
        public Sprite GetPirateSprite(int pirateIndex)
        {
            return pirateIndex switch
            {
                0 => pirate1Sprite,
                1 => pirate2Sprite,
                2 => pirate3Sprite,
                3 => pirate4Sprite,
                4 => pirate5Sprite,
                _ => pirate1Sprite
            };
        }

        /// <summary>
        /// 인어(정령) 카드의 덱 내 순서를 기반으로 고유 일러스트를 반환합니다.
        /// mermaidIndex: 0~1
        /// </summary>
        public Sprite GetMermaidSprite(int mermaidIndex)
        {
            return mermaidIndex switch
            {
                0 => mermaid1Sprite,
                1 => mermaid2Sprite,
                _ => mermaid1Sprite
            };
        }

        /// <summary>
        /// CardData를 기반으로 적절한 일러스트 Sprite를 반환합니다.
        /// 해적/인어는 카드 ID를 기반으로 구분합니다.
        /// </summary>
        public Sprite GetSpriteForCard(CardData card)
        {
            // 특수 카드
            if (card.IsSpecialCard)
            {
                return card.Special switch
                {
                    SpecialType.SkullKing => skullKingSprite,
                    SpecialType.Tigress => tigressSprite,
                    SpecialType.Kraken => krakenSprite,
                    SpecialType.WhiteWhale => whiteWhaleSprite,
                    SpecialType.Escape => GetEscapeSpriteById(card.Id),
                    SpecialType.Jewel => GetAllianceSpriteById(card.Id),
                    // 해적/인어는 ID 기반으로 구분 (Deck.cs의 생성 순서)
                    // 해적: ID 61~65 (숫자카드 56장 + 도주 5장 이후)
                    // 인어: ID 66~67
                    SpecialType.Pirate => GetPirateSpriteById(card.Id),
                    SpecialType.Mermaid => GetMermaidSpriteById(card.Id),
                    _ => null
                };
            }

            // 숫자는 Unity Text로 표시하므로 01~04 원화 그룹별 완성 카드만 사용합니다.
            return card.Suit switch
            {
                CardSuit.Green => GetNumberSprite(greenSuitSprites, greenSuitSprite, card.Number),
                CardSuit.Yellow => GetNumberSprite(yellowSuitSprites, yellowSuitSprite, card.Number),
                CardSuit.Purple => GetNumberSprite(purpleSuitSprites, purpleSuitSprite, card.Number),
                CardSuit.Black => GetNumberSprite(blackSuitSprites, blackSuitSprite, card.Number),
                _ => null
            };
        }

        /// <summary>
        /// 카드 앞면 위에 얹을 세계별 투명 프레임을 반환합니다.
        /// 파일명 기준 Wuxia/Fantasy/Cyberpunk 캐릭터 중 private가 아닌 원화만
        /// 전용 프레임을 사용합니다. 항복/동맹/천상강림/크라켄은 제외합니다.
        /// </summary>
        public Sprite GetFrameForCard(CardData card)
        {
            // 현재 프레임 적용 캐릭터는 모두 완성 카드 PNG에 프레임이 합쳐져 있습니다.
            // 런타임에서 프레임을 다시 덧씌우지 않습니다.
            return null;
        }

        public bool IsComposedArtwork(CardData card)
        {
            // 현재 CROSSION의 앞면 이미지는 숫자 카드뿐 아니라 특수 카드도 모두
            // 832x1216의 완성 카드 비율입니다. 항복/특수항복도 합성된 프레임을 포함해
            // 카드 전체 면적을 사용합니다.
            return card.IsNumberCard || card.IsSpecialCard;
        }

        private static Sprite GetNumberSprite(Sprite[] sprites, Sprite fallback, int number)
        {
            if (sprites == null || sprites.Length == 0)
                return fallback;

            // 01=1~3, 02=4~6, 03=7~10, 04=11~14.
            int index;
            if (sprites.Length >= 14)
                index = Mathf.Clamp(number - 1, 0, sprites.Length - 1);
            else if (sprites.Length >= 4)
                index = number <= 3 ? 0 : number <= 6 ? 1 : number <= 10 ? 2 : 3;
            else
                index = Mathf.Abs(number - 1) % sprites.Length;
            return sprites[index] != null ? sprites[index] : fallback;
        }

        private Sprite GetEscapeSpriteById(int cardId)
        {
            if (escapeSprites == null || escapeSprites.Length == 0)
                return escapeSprite;

            int index = cardId < 0
                ? Mathf.Abs(cardId) % escapeSprites.Length
                : Mathf.Abs(cardId - 56) % escapeSprites.Length;
            return escapeSprites[index] != null ? escapeSprites[index] : escapeSprite;
        }

        private Sprite GetAllianceSpriteById(int cardId)
        {
            if (allianceSprites == null || allianceSprites.Length == 0)
                return jewelSprite;

            // Stable artwork variation without changing the deck or scoring rules.
            int index = (int)(System.Math.Abs((long)cardId) % allianceSprites.Length);
            return allianceSprites[index] != null ? allianceSprites[index] : jewelSprite;
        }

        // ---- ID 기반 해적/인어 구분 ----
        // Deck.cs 생성 순서: 숫자(0~55) → 도주(56~60) → 해적(61~65) → 인어(66~67)

        private Sprite GetPirateSpriteById(int cardId)
        {
            if (cardId < 0)
                return GetPirateSprite(Mathf.Abs(cardId) % 5);

            // 첫 번째 해적의 ID를 기준으로 오프셋 계산
            // 해적 시작 ID = 61 (숫자 56장 + 도주 5장)
            int pirateIndex = cardId - 61;
            if (pirateIndex < 0 || pirateIndex > 4) pirateIndex = 0;
            return GetPirateSprite(pirateIndex);
        }

        private Sprite GetMermaidSpriteById(int cardId)
        {
            if (cardId < 0)
                return GetMermaidSprite(Mathf.Abs(cardId) % 2);

            // 인어 시작 ID = 66 (숫자 56 + 도주 5 + 해적 5)
            int mermaidIndex = cardId - 66;
            if (mermaidIndex < 0 || mermaidIndex > 1) mermaidIndex = 0;
            return GetMermaidSprite(mermaidIndex);
        }
    }
}
