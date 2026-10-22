using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkullKing.Core;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.Networking
{
    /// <summary>
    /// Separate participant views in one process. JSON messages take the same session path as Steam.
    /// This tests presentation and application protocol, NOT Steam connectivity or authentication.
    /// No extra runtime UI is generated; the scene's usual GameManager displays the chosen seat.
    /// </summary>
    public sealed class LocalMultiplayerTest : MonoBehaviour
    {
        private struct Delivery
        {
            public int sender, receiver;
            public float due;
            public string json;
        }

        private readonly Dictionary<int, SteamGameSession> _peers = new Dictionary<int, SteamGameSession>();
        private readonly Queue<Delivery> _deliveries = new Queue<Delivery>();
        private float _delay;
        private int _visibleSeat;
        private bool _stopped;

        public void Begin(GameManager visibleView, int count, int visibleSeat, float delay, float confirmDelay)
        {
            if (!(Application.isEditor || Debug.isDebugBuild))
                throw new InvalidOperationException("Local multiplayer test is development-only.");
            count = Mathf.Clamp(count, 3, 6);
            _visibleSeat = Mathf.Clamp(visibleSeat, 0, count - 1);
            _delay = Mathf.Clamp(delay, 0f, 1f);
            var roster = new SteamGameRoster
            {
                token = "local-" + Guid.NewGuid().ToString("N"), host = "1000",
                ids = new string[count], names = new string[count]
            };
            for (int i = 0; i < count; i++)
            {
                roster.ids[i] = (1000 + i).ToString();
                roster.names[i] = "테스트 참가자 " + (i + 1);
            }
            for (int i = 0; i < count; i++)
            {
                var peerObject = new GameObject("Local Test Participant " + (i + 1));
                peerObject.transform.SetParent(transform);
                GameManager view = i == _visibleSeat ? visibleView : peerObject.AddComponent<GameManager>();
                var session = peerObject.AddComponent<SteamGameSession>();
                _peers.Add(i, session);
                if (i != _visibleSeat)
                    peerObject.AddComponent<LocalMultiplayerTestPeer>().Begin(view, confirmDelay);
                session.BeginLocal(view, roster, i, this);
            }
            Debug.Log($"[Local Multiplayer] {count}개 독립 참가자 화면 생성. 본인은 참가자 {_visibleSeat + 1}" +
                $" ({(_visibleSeat == 0 ? "방 생성자" : "일반 참가자")}). 나머지는 모의 입력으로 진행합니다. Steam 검증은 아닙니다.");
        }

        public void Send(int sender, int receiver, NetworkGamePacket packet)
        {
            if (_stopped) return;
            // Serialization prevents the in-process peers from accidentally sharing mutable objects.
            _deliveries.Enqueue(new Delivery
            {
                sender = sender, receiver = receiver, due = Time.realtimeSinceStartup + _delay,
                json = JsonUtility.ToJson(packet)
            });
        }

        private void Update()
        {
            int budget = 128;
            while (!_stopped && _deliveries.Count > 0 &&
                _deliveries.Peek().due <= Time.realtimeSinceStartup && budget-- > 0)
            {
                Delivery delivery = _deliveries.Dequeue();
                try
                {
                    var packet = JsonUtility.FromJson<NetworkGamePacket>(delivery.json);
                    if (packet.kind == "view") ValidatePrivateView(packet.snapshot);
                    if (_peers.TryGetValue(delivery.receiver, out var session))
                        session.ReceiveLocal(delivery.sender, packet);
                }
                catch (Exception error)
                {
                    StopTest("모의 메시지 검사 실패: " + error.Message);
                    Debug.LogException(error);
                }
            }
        }

        private static void ValidatePrivateView(NetworkGameSnapshot view)
        {
            if (view?.players == null) throw new InvalidOperationException("참가자 정보 누락");
            for (int i = 1; i < view.players.Length; i++)
                if (view.players[i].hand != null && view.players[i].hand.Length != 0)
                    throw new InvalidOperationException("다른 참가자의 손패가 메시지에 포함됨");
        }

        [ContextMenu("Simulate Participant Disconnect (Play Mode Only)")]
        public void SimulateParticipantDisconnect()
        {
            if (!Application.isPlaying || _peers.Count == 0 || _stopped) return;
            StopTest("모의 참가자 연결 해제 · 중단 안내 테스트");
        }

        private void StopTest(string reason)
        {
            _stopped = true;
            _deliveries.Clear();
            foreach (var peer in _peers.Values) peer.StopLocalTest(reason);
        }
    }

}
