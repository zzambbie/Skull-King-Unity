using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkullKing.Core;
using SkullKing.Data;
using SkullKing.Logic;
using SkullKing.Networking;

namespace SkullKing.Editor
{
    public static class CressionNetworkChecks
    {
        public static void RunBatch()
        {
            int checks = RunSnapshotChecks(true) + RunInputChecks();
            Debug.Log($"[CROSSION Multiplayer Checks] PASS {checks} checks (native Unity JSON included; Steam transport not tested)");
        }

        public static void Run()
        {
            try
            {
                int checks = RunSnapshotChecks(true) + RunInputChecks();
                Debug.Log($"[CROSSION Multiplayer Checks] {checks}개 검사 통과. 실제 Steam 다중 접속 검사는 별도입니다.");
                EditorUtility.DisplayDialog("멀티플레이 로직 검사", $"{checks}개 검사 통과\n실제 Steam 연결 검사는 별도로 필요합니다.", "확인");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorUtility.DisplayDialog("멀티플레이 로직 검사 실패", error.Message, "확인");
            }
        }

        // No Steam or scene dependency: also runnable by the standalone verification harness.
        public static int RunSnapshotChecks(bool checkUnitySerialization)
        {
            int checks = 0;
            for (int count = 3; count <= 6; count++)
            {
                GameState source = BuildState(count);
                var scores = ScoreCalculator.CalculateAll(source.Players, 4);
                for (int viewer = 0; viewer < count; viewer++)
                {
                    var view = NetworkGameSnapshot.Capture(source, viewer, "scores", 12, 8, count - 1,
                        source.TrickCards[0], TrickResult.Voided(count - 1), scores);
                    Require(view.players[0].hand.Length == 2 && view.players[0].hand[0].Id == viewer * 10 + 1,
                        "자신의 손패 전달", ref checks);
                    for (int i = 1; i < count; i++)
                        Require(view.players[i].hand.Length == 0 && view.players[i].handCount == 2,
                            "상대 손패 비공개", ref checks);
                    Require(view.current == NetworkGameSnapshot.ViewIndex(source.CurrentPlayerIndex, viewer, count)
                        && view.leader == NetworkGameSnapshot.ViewIndex(source.LeaderIndex, viewer, count),
                        "턴과 선턴 좌석 회전", ref checks);
                    Require(view.played.PlayerIndex == NetworkGameSnapshot.ViewIndex(1, viewer, count)
                        && view.trickCards[0].PlayerIndex == view.played.PlayerIndex,
                        "제출 카드 주인 회전", ref checks);
                    Require(view.result.WinnerIndex == -1 && view.result.WouldBeWinnerIndex ==
                        NetworkGameSnapshot.ViewIndex(count - 1, viewer, count), "크라켄 결과 회전", ref checks);
                    Require(view.scores[0].PlayerIndex == 0 && view.scores[0].RoundTotal == scores[viewer].RoundTotal,
                        "점수 표시 순서", ref checks);
                    if (checkUnitySerialization)
                    {
                        string json = JsonUtility.ToJson(new NetworkGamePacket
                        { session = "test", kind = "view", snapshot = view });
                        var packet = JsonUtility.FromJson<NetworkGamePacket>(json);
                        view = packet.snapshot;
                        Require(packet.protocol == 1 && packet.session == "test" &&
                            view.played.Card.Id == source.TrickCards[0].Card.Id && view.result.IsVoided &&
                            view.scores.Length == count && view.players[0].hand.Length == 2,
                            "Unity JSON 왕복", ref checks);
                    }
                    GameState restored = view.Restore();
                    Require(restored.Players[0].IsHuman && restored.Players[0].Name == source.Players[viewer].Name,
                        "모든 화면에서 자신이 아래쪽", ref checks);
                    for (int i = 1; i < count; i++)
                        Require(!restored.Players[i].IsHuman && restored.Players[i].Hand[0].Id == -1,
                            "상대는 뒷면 장수만 복구", ref checks);
                    Require(restored.Players[0].JewelAlliances[0] == 1, "동맹 좌석 회전", ref checks);
                    Require(source.TrickCards[0].PlayerIndex == 1 && source.Players[viewer].Hand.Count == 2,
                        "스냅샷 원본 불변", ref checks);
                }
            }
            return checks;
        }

        private static int RunInputChecks()
        {
            int checks = 0;
            var obj = new GameObject("CROSSION network test") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                GameManager engine = obj.AddComponent<GameManager>();
                var source = BuildState(3);
                source.CurrentPhase = GamePhase.Playing;
                source.CurrentPlayerIndex = 0;
                source.LeadSuit = CardSuit.Green;
                source.Players[0].Hand.Clear();
                source.Players[0].Hand.Add(CardData.CreateNumberCard(100, CardSuit.Green, 5));
                source.Players[0].Hand.Add(CardData.CreateNumberCard(101, CardSuit.Yellow, 14));
                engine.ApplyNetworkSnapshot(NetworkGameSnapshot.Capture(source, 0, "test", 1, 1));
                Set(engine, "_networkAuthority", true);
                Set(engine, "_waitingForHumanCard", true);
                Require(!engine.TryNetworkInput(1, "card", 100), "다른 차례 입력 거부", ref checks);
                Require(!engine.TryNetworkInput(0, "card", 999), "손패에 없는 카드 거부", ref checks);
                Require(!engine.TryNetworkInput(0, "card", 101), "리드 세력 규칙 검증", ref checks);
                Require(engine.TryNetworkInput(0, "card", 100), "정상 카드 입력", ref checks);
                Require(!engine.TryNetworkInput(0, "card", 100), "중복 카드 입력 거부", ref checks);
                Set(engine, "_waitingForHumanBid", true);
                Require(!engine.TryNetworkInput(0, "bid", -1) && !engine.TryNetworkInput(0, "bid", 5),
                    "입찰 범위 검증", ref checks);
                Require(engine.TryNetworkInput(0, "bid", 2), "정상 입찰 입력", ref checks);
                Set(engine, "_waitingForTigressChoice", true);
                Require(!engine.TryNetworkInput(0, "tigress", 999), "티그리스 모드 검증", ref checks);
                Require(engine.TryNetworkInput(0, "tigress", (int)TigressChoice.Escape), "정상 티그리스 입력", ref checks);

                var session = obj.AddComponent<SteamGameSession>();
                Set(session, "_view", engine);
                Set(session, "_authority", engine);
                Set(session, "_roster", new SteamGameRoster { token = "test", ids = new[] { "1", "2", "3" } });
                Set(session, "_active", true);
                Set(session, "_request", 20);
                Set(session, "_expectedCommand", "bid");
                Set(session, "_expectedSeat", 0);
                Set(engine, "_waitingForHumanBid", true);
                var method = typeof(SteamGameSession).GetMethod("Handle", BindingFlags.NonPublic | BindingFlags.Instance);
                Action<int, NetworkGamePacket> deliver = (sender, packet) => method.Invoke(session, new object[] { sender, packet });
                deliver(0, new NetworkGamePacket { session = "test", kind = "input", command = "bid", request = 19, value = 2 });
                Require(Get<bool>(engine, "_waitingForHumanBid"), "지난 요청 입력 거부", ref checks);
                deliver(0, new NetworkGamePacket { session = "other", kind = "input", command = "bid", request = 20, value = 2 });
                Require(Get<bool>(engine, "_waitingForHumanBid"), "다른 게임 입력 거부", ref checks);
                deliver(1, new NetworkGamePacket { session = "test", kind = "input", command = "bid", request = 20, value = 2 });
                Require(Get<bool>(engine, "_waitingForHumanBid"), "발신자 좌석 검증", ref checks);
                deliver(0, new NetworkGamePacket { session = "test", kind = "input", command = "bid", request = 20, value = 2 });
                Require(!Get<bool>(engine, "_waitingForHumanBid"), "인증된 요청 처리", ref checks);
                Set(session, "_expectedCommand", "confirm");
                Set(engine, "_waitingForRoundConfirm", true);
                var confirm = new NetworkGamePacket { session = "test", kind = "input", command = "confirm", request = 20 };
                deliver(0, confirm);
                deliver(0, confirm);
                deliver(1, confirm);
                Require(Get<bool>(engine, "_waitingForRoundConfirm"), "결과 확인 중복 방지와 전원 대기", ref checks);
                deliver(2, confirm);
                Require(!Get<bool>(engine, "_waitingForRoundConfirm"), "전원 확인 후 진행", ref checks);
                Set(session, "_active", false);
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
            return checks;
        }

        private static GameState BuildState(int count)
        {
            var state = new GameState { PlayerCount = count, Players = new PlayerData[count], CurrentRound = 4,
                CurrentTrick = 2, LeaderIndex = count - 1, CurrentPlayerIndex = 1, RoundStartPlayerIndex = 2 };
            for (int i = 0; i < count; i++)
            {
                var p = new PlayerData(i, "참가자" + i, true) { Bid = i % 2, HasBid = true, TricksWon = i % 2, TotalScore = 20 };
                p.Hand.Add(CardData.CreateNumberCard(i * 10 + 1, CardSuit.Green, i + 1));
                p.Hand.Add(CardData.CreateSpecialCard(i * 10 + 2, SpecialType.Escape));
                p.JewelAlliances.Add((i + 1) % count);
                state.Players[i] = p;
            }
            state.TrickCards.Add(new PlayedCard { PlayerIndex = 1,
                Card = CardData.CreateSpecialCard(200, SpecialType.Tigress), TigressMode = TigressChoice.Pirate });
            return state;
        }

        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static T Get<T>(object target, string name)
            => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Require(bool condition, string name, ref int checks)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(name);
        }
    }
}
