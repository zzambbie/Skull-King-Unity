// ============================================================
// Deck.cs — 덱 생성, 셔플, 분배
// ============================================================
using System;
using System.Collections.Generic;
using SkullKing.Data;

namespace SkullKing.Logic
{
    /// <summary>
    /// 스컬킹 카드 덱을 생성하고 관리합니다.
    /// MonoBehaviour가 아닌 순수 C# 클래스입니다.
    /// </summary>
    public class Deck
    {
        private List<CardData> _cards = new List<CardData>();
        private int _dealIndex;
        private Random _rng;

        /// <summary>남은 카드 수</summary>
        public int RemainingCount => _cards.Count - _dealIndex;

        /// <summary>전체 카드 수</summary>
        public int TotalCount => _cards.Count;

        public Deck(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// 전체 덱을 생성합니다.
        /// 숫자 카드: 4색(초록/노랑/보라/검정) × 14장 = 56장
        /// 도주 카드: 5장
        /// 특수 카드: 스컬킹 1, 인어 2, 해적 5, 타이그리스 1, 크라켄 1, 백경 1 = 11장
        /// 총 72장
        /// </summary>
        public void CreateFullDeck()
        {
            _cards.Clear();
            _dealIndex = 0;
            int id = 0;

            // ---- 숫자 카드 (4색 × 14) ----
            CardSuit[] suits = { CardSuit.Green, CardSuit.Yellow, CardSuit.Purple, CardSuit.Black };
            foreach (var suit in suits)
            {
                for (int num = 1; num <= 14; num++)
                {
                    _cards.Add(CardData.CreateNumberCard(id++, suit, num));
                }
            }

            // ---- 도주 카드 (5장) ----
            for (int i = 0; i < 5; i++)
            {
                _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.Escape));
            }

            // ---- 용사 카드 (5장) — 각각 고유 직업명 ----
            string[] heroNames = { "전사", "마법사", "궁수", "도적", "성기사" };
            for (int i = 0; i < 5; i++)
            {
                _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.Pirate, heroNames[i]));
            }

            // ---- 정령 카드 (2장) — 각각 고유 속성명 ----
            _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.Mermaid, "화염 정령"));
            _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.Mermaid, "빙결 정령"));

            // ---- 타이그리스 (1장) ----
            _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.Tigress));

            // ---- 스컬킹 (1장) ----
            _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.SkullKing));

            // ---- 크라켄 (1장) ----
            _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.Kraken));

            // ---- 백경 (1장) ----
            _cards.Add(CardData.CreateSpecialCard(id++, SpecialType.WhiteWhale));
        }

        /// <summary>
        /// Fisher-Yates 셔플
        /// </summary>
        public void Shuffle()
        {
            _dealIndex = 0;
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var temp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = temp;
            }
        }

        /// <summary>
        /// 카드를 한 장 뽑습니다.
        /// </summary>
        public CardData DrawOne()
        {
            if (_dealIndex >= _cards.Count)
                throw new InvalidOperationException("덱에 카드가 없습니다!");
            return _cards[_dealIndex++];
        }

        /// <summary>
        /// 지정한 수만큼 카드를 뽑아 리스트로 반환합니다.
        /// </summary>
        public List<CardData> Draw(int count)
        {
            var hand = new List<CardData>(count);
            for (int i = 0; i < count; i++)
            {
                hand.Add(DrawOne());
            }
            return hand;
        }

        /// <summary>
        /// 모든 플레이어에게 카드를 분배합니다.
        /// n라운드에는 n장씩 분배합니다.
        /// </summary>
        /// <param name="players">플레이어 배열</param>
        /// <param name="cardsPerPlayer">플레이어당 카드 수 (= 라운드 번호)</param>
        public void DealToPlayers(PlayerData[] players, int cardsPerPlayer)
        {
            for (int i = 0; i < players.Length; i++)
            {
                players[i].Hand.AddRange(Draw(cardsPerPlayer));
            }
        }
    }
}
