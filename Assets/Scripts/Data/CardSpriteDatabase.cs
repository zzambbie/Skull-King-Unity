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

        // ============================================================
        // 특수 카드 — 각각 고유 일러스트
        // ============================================================

        [Header("=== 용사 (해적) — 5명 각각 다른 일러스트 ===")]

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

        [Header("=== 정령 (인어) — 2종 다른 일러스트 ===")]

        [Tooltip("정령 1 — 예: 화염 정령")]
        public Sprite mermaid1Sprite;

        [Tooltip("정령 2 — 예: 빙결 정령")]
        public Sprite mermaid2Sprite;

        [Header("=== 기타 특수 카드 ===")]

        [Tooltip("스컬킹 — 마왕/최종 보스")]
        public Sprite skullKingSprite;

        [Tooltip("타이그리스 — 이중 스파이/변신술사")]
        public Sprite tigressSprite;

        [Tooltip("크라켄 — 거대 괴수")]
        public Sprite krakenSprite;

        [Tooltip("백경 — 성수/백룡")]
        public Sprite whiteWhaleSprite;

        [Tooltip("도주 — 도망/후퇴")]
        public Sprite escapeSprite;

        [Tooltip("보석 — 마력석/보석")]
        public Sprite jewelSprite;

        [Header("=== 카드 뒷면 ===")]

        [Tooltip("카드 뒷면 디자인")]
        public Sprite cardBackSprite;

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
                    SpecialType.Escape => escapeSprite,
                    SpecialType.Jewel => jewelSprite,
                    // 해적/인어는 ID 기반으로 구분 (Deck.cs의 생성 순서)
                    // 해적: ID 61~65 (숫자카드 56장 + 도주 5장 이후)
                    // 인어: ID 66~67
                    SpecialType.Pirate => GetPirateSpriteById(card.Id),
                    SpecialType.Mermaid => GetMermaidSpriteById(card.Id),
                    _ => null
                };
            }

            // 숫자 카드 — 수트별 대표 일러스트
            return card.Suit switch
            {
                CardSuit.Green => greenSuitSprite,
                CardSuit.Yellow => yellowSuitSprite,
                CardSuit.Purple => purpleSuitSprite,
                CardSuit.Black => blackSuitSprite,
                _ => null
            };
        }

        // ---- ID 기반 해적/인어 구분 ----
        // Deck.cs 생성 순서: 숫자(0~55) → 도주(56~60) → 해적(61~65) → 인어(66~67)

        private Sprite GetPirateSpriteById(int cardId)
        {
            // 첫 번째 해적의 ID를 기준으로 오프셋 계산
            // 해적 시작 ID = 61 (숫자 56장 + 도주 5장)
            int pirateIndex = cardId - 61;
            if (pirateIndex < 0 || pirateIndex > 4) pirateIndex = 0;
            return GetPirateSprite(pirateIndex);
        }

        private Sprite GetMermaidSpriteById(int cardId)
        {
            // 인어 시작 ID = 66 (숫자 56 + 도주 5 + 해적 5)
            int mermaidIndex = cardId - 66;
            if (mermaidIndex < 0 || mermaidIndex > 1) mermaidIndex = 0;
            return GetMermaidSprite(mermaidIndex);
        }
    }
}
