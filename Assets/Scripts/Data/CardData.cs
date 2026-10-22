// ============================================================
// CardData.cs — 카드 데이터 정의
// 스컬킹 보드게임의 모든 카드 종류를 정의합니다.
// ============================================================
using System;

namespace SkullKing.Data
{
    /// <summary>
    /// 카드의 색상(수트). 숫자 카드에만 적용됩니다.
    /// </summary>
    public enum CardSuit
    {
        None,       // 특수 카드용 (수트 없음)
        Green,      // 초록 (도적단)
        Yellow,     // 노랑 (마법사단)
        Purple,     // 보라 (성기사단)
        Black       // 검정 (해적/암흑기사단) — 다른 색보다 강함
    }

    /// <summary>
    /// 특수 카드 종류
    /// </summary>
    public enum SpecialType
    {
        None,           // 일반 숫자 카드
        SkullKing,      // 스컬킹 — 최강, 단 인어에게 짐
        Mermaid,        // 인어 — 스컬킹을 잡을 수 있음
        Pirate,         // 해적 — 숫자 카드와 인어를 이김
        Tigress,        // 타이그리스 — 해적 또는 도주로 선택 가능
        Kraken,         // 크라켄 — 트릭 무효화
        WhiteWhale,     // 백경 — 특수 무시, 숫자만으로 판정
        Escape,         // 도주 — 무조건 짐
        Jewel           // 보석 — 도주와 같으나 동맹 점수 창출
    }

    /// <summary>
    /// 타이그리스를 낼 때 선택하는 모드
    /// </summary>
    public enum TigressChoice
    {
        NotApplicable,  // 타이그리스가 아닌 카드
        Pirate,         // 해적으로 사용
        Escape          // 도주로 사용
    }

    /// <summary>
    /// 카드 데이터. 불변(immutable) 구조체.
    /// </summary>
    [Serializable]
    public struct CardData : IEquatable<CardData>
    {
        /// <summary>카드 고유 ID (덱 내 유일한 식별자)</summary>
        public int Id;

        /// <summary>카드 색상 (숫자 카드만 해당)</summary>
        public CardSuit Suit;

        /// <summary>카드 숫자 (1~14, 특수 카드는 0)</summary>
        public int Number;

        /// <summary>특수 카드 종류</summary>
        public SpecialType Special;

        /// <summary>표시 이름</summary>
        public string DisplayName;

        // ---- 편의 프로퍼티 ----

        /// <summary>숫자 카드인가?</summary>
        public bool IsNumberCard => Special == SpecialType.None && Number > 0;

        /// <summary>특수 카드인가? (도주/보석 포함)</summary>
        public bool IsSpecialCard => Special != SpecialType.None;

        /// <summary>도주 카드인가? (순수 도주 + 보석 + 타이그리스 도주 모드)</summary>
        public bool IsEscapeCard => Special == SpecialType.Escape || Special == SpecialType.Jewel;

        /// <summary>검정 숫자 카드인가?</summary>
        public bool IsBlackNumberCard => Suit == CardSuit.Black && IsNumberCard;

        // ---- 팩토리 메서드 ----

        /// <summary>숫자 카드 생성</summary>
        public static CardData CreateNumberCard(int id, CardSuit suit, int number)
        {
            string suitName = suit switch
            {
                CardSuit.Green => "활",       // 🏹 레인저 — 숲의 초록
                CardSuit.Yellow => "검",      // ⚔️ 나이트 — 황금 빛
                CardSuit.Purple => "지팡이",  // 🔮 메이지 — 신비 보라
                CardSuit.Black => "흑마도서", // 📖 다크메이지 — 먹빛
                _ => "?"
            };
            return new CardData
            {
                Id = id,
                Suit = suit,
                Number = number,
                Special = SpecialType.None,
                DisplayName = $"{suitName} {number}"
            };
        }

        /// <summary>특수 카드 생성</summary>
        public static CardData CreateSpecialCard(int id, SpecialType special, string displayName = null)
        {
            if (displayName == null)
            {
                displayName = special switch
                {
                    SpecialType.SkullKing => "스컬킹",
                    SpecialType.Mermaid => "인어",
                    SpecialType.Pirate => "해적",
                    SpecialType.Tigress => "타이그리스",
                    SpecialType.Kraken => "크라켄",
                    SpecialType.WhiteWhale => "백경",
                    SpecialType.Escape => "도주",
                    SpecialType.Jewel => "보석",
                    _ => "???"
                };
            }
            return new CardData
            {
                Id = id,
                Suit = CardSuit.None,
                Number = 0,
                Special = special,
                DisplayName = displayName
            };
        }

        // ---- IEquatable ----

        public bool Equals(CardData other) => Id == other.Id;
        public override bool Equals(object obj) => obj is CardData other && Equals(other);
        public override int GetHashCode() => Id;
        public static bool operator ==(CardData a, CardData b) => a.Id == b.Id;
        public static bool operator !=(CardData a, CardData b) => a.Id != b.Id;

        public override string ToString() => $"[{Id}] {DisplayName}";
    }

    /// <summary>
    /// 트릭에 제출된 카드 한 장의 정보.
    /// 누가 어떤 카드를 어떤 옵션으로 냈는지를 기록합니다.
    /// </summary>
    [Serializable]
    public struct PlayedCard
    {
        /// <summary>제출한 플레이어의 인덱스</summary>
        public int PlayerIndex;

        /// <summary>제출한 카드</summary>
        public CardData Card;

        /// <summary>타이그리스 선택 (해당하는 경우만)</summary>
        public TigressChoice TigressMode;

        /// <summary>타이그리스가 해적 모드인지 확인</summary>
        public bool IsTigressAsPirate =>
            Card.Special == SpecialType.Tigress && TigressMode == TigressChoice.Pirate;

        /// <summary>타이그리스가 도주 모드인지 확인</summary>
        public bool IsTigressAsEscape =>
            Card.Special == SpecialType.Tigress && TigressMode == TigressChoice.Escape;

        /// <summary>이 카드가 해적 역할을 하는가 (순수 해적 + 타이그리스 해적 모드)</summary>
        public bool ActsAsPirate =>
            Card.Special == SpecialType.Pirate || IsTigressAsPirate;

        /// <summary>이 카드가 도주 역할을 하는가 (순수 도주 + 보석 + 타이그리스 도주 모드)</summary>
        public bool ActsAsEscape =>
            Card.Special == SpecialType.Escape || Card.Special == SpecialType.Jewel || IsTigressAsEscape;

        public override string ToString()
        {
            string suffix = Card.Special == SpecialType.Tigress ? $" ({TigressMode})" : "";
            return $"P{PlayerIndex}: {Card}{suffix}";
        }
    }
}
