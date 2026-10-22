using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using SkullKing.Core;
using SkullKing.Data;
using SkullKing.Logic;
#if STEAMWORKS_NET
using Steamworks;
#endif

namespace SkullKing.Networking
{
    [Serializable]
    public sealed class SteamGameRoster
    {
        public string token, host;
        public string[] ids, names;
    }

    /// <summary>
    /// Reliable Steam peer messages, private per-recipient views, and authenticated turn input.
    /// The creator computes rules but sees the same filtered view and uses the same input path.
    /// </summary>
    public sealed class SteamGameSession : MonoBehaviour
    {
        private const int Channel = 23, MaximumPacketBytes = 65536;
        private GameManager _view, _authority;
        private SteamGameRoster _roster;
        private int _localSeat, _hostSeat, _sequence, _request, _lastApplied;
        private int _viewRequest, _expectedSeat;
        private string _viewCommand, _expectedCommand;
        private bool _active, _started, _finished, _transportReady;
        private float _startedAt, _nextHeartbeat, _nextViewAt;
        private readonly HashSet<int> _loaded = new HashSet<int>();
        private readonly HashSet<int> _confirmed = new HashSet<int>();
        private readonly Dictionary<int, float> _lastSeen = new Dictionary<int, float>();
        private readonly Queue<NetworkGameSnapshot> _views = new Queue<NetworkGameSnapshot>();
        private LocalMultiplayerTest _localTransport;
#if STEAMWORKS_NET
        private readonly IntPtr[] _receiveBuffer = new IntPtr[32];
        private Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequested;
        private Callback<SteamNetworkingMessagesSessionFailed_t> _sessionFailed;
        private CSteamID _lobby;
#endif
        private bool IsHost => _localSeat == _hostSeat;

        // Development-only transport. The normal input, privacy, ordering and rule paths stay the same.
        public void BeginLocal(GameManager view, SteamGameRoster roster, int seat, LocalMultiplayerTest transport)
        {
            if (!(Application.isEditor || Debug.isDebugBuild))
                throw new InvalidOperationException("Local multiplayer test is development-only.");
            _view = view;
            _view.AttachNetworkView(this);
            _localTransport = transport;
            _roster = roster;
            _localSeat = seat;
            _hostSeat = 0;
            _active = true;
            _loaded.Add(seat);
            Status("한 PC 모의 멀티플레이 · 실제 Steam 통신 아님");
            Send(_hostSeat, new NetworkGamePacket { kind = "loaded" });
        }

        public void ReceiveLocal(int sender, NetworkGamePacket packet)
        {
            if (_localTransport != null && sender >= 0 && sender < _roster.ids.Length)
                Handle(sender, packet);
        }

        public void StopLocalTest(string reason)
        {
            if (_localTransport != null) Fail(reason, false);
        }

        public void Begin(GameManager view)
        {
            _view = view;
            _view.AttachNetworkView(this);
#if STEAMWORKS_NET
            var steam = SteamPlatformBootstrap.Instance;
            var lobby = SteamLobbyService.Instance;
            if (steam == null || !steam.IsInitialized || lobby == null || !lobby.IsInLobby)
            { Fail("Steam 방 연결이 없습니다."); return; }
            _lobby = new CSteamID(lobby.CurrentLobbyId);
            try
            {
                _roster = JsonUtility.FromJson<SteamGameRoster>(SteamMatchmaking.GetLobbyData(_lobby, "game_manifest"));
                if (_roster == null || _roster.ids == null || _roster.names == null ||
                    _roster.ids.Length < 3 || _roster.ids.Length > 6 ||
                    _roster.ids.Length != _roster.names.Length || string.IsNullOrEmpty(_roster.token))
                    throw new ArgumentException("잘못된 참가자 명단");
                var unique = new HashSet<string>();
                foreach (string id in _roster.ids)
                    if (!ulong.TryParse(id, out ulong parsed) || parsed == 0 || !unique.Add(id))
                        throw new ArgumentException("잘못된 Steam ID");
                _localSeat = Array.IndexOf(_roster.ids, steam.LocalSteamId.ToString());
                _hostSeat = Array.IndexOf(_roster.ids, _roster.host);
                if (_localSeat < 0 || _hostSeat < 0 ||
                    _roster.host != SteamMatchmaking.GetLobbyOwner(_lobby).m_SteamID.ToString())
                    throw new ArgumentException("시작 참가자/방장 불일치");
            }
            catch (Exception error) { Fail("방 정보를 읽지 못했습니다: " + error.Message); return; }
            _sessionRequested = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequested);
            _sessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
            _active = true;
            _transportReady = true;
            SteamNetworkingUtils.InitRelayNetworkAccess();
            _startedAt = Time.realtimeSinceStartup;
            for (int i = 0; i < _roster.ids.Length; i++) _lastSeen[i] = _startedAt;
            _loaded.Add(_localSeat);
            Status("참가자들의 게임 화면 연결을 기다리는 중...");
#else
            Fail("Steamworks 연결 모듈이 없습니다.");
#endif
        }

        public void Submit(string command, int value)
        {
            if (!_active || _finished || _viewCommand != command) return;
            _viewCommand = null; // A second click cannot reuse this request.
            Send(_hostSeat, new NetworkGamePacket
            { kind = "input", command = command, value = value, request = _viewRequest });
            if (command == "confirm") Status("다른 참가자들의 결과 확인을 기다리는 중...");
        }

        private void Update()
        {
            if (!_active) return;
            if (_localTransport == null)
            {
#if STEAMWORKS_NET
            if (SteamPlatformBootstrap.Instance == null || !SteamPlatformBootstrap.Instance.IsInitialized)
            { Fail("Steam 연결이 끊겼습니다."); return; }
            Receive();
            if (!_active) return;
            float now = Time.realtimeSinceStartup;
            if (now >= _nextHeartbeat)
            {
                _nextHeartbeat = now + 2f;
                if (!_finished && !CheckRoster()) { Fail("참가자가 방을 떠나 대전을 중단했습니다."); return; }
                if (IsHost)
                {
                    for (int i = 0; i < _roster.ids.Length; i++)
                        if (i != _localSeat) Send(i, new NetworkGamePacket { kind = "ping" });
                }
                else Send(_hostSeat, new NetworkGamePacket { kind = "loaded" });
            }
            if (!_started && now - _startedAt > 60f)
            { Fail("참가자 연결 대기 시간이 초과됐습니다."); return; }
            if (_started && !_finished)
            {
                for (int i = 0; i < _roster.ids.Length; i++)
                    if (i != _localSeat && (IsHost || i == _hostSeat) && now - _lastSeen[i] > 30f)
                    { Fail("참가자의 응답이 끊겨 대전을 중단했습니다."); return; }
            }
            if (IsHost && !_started && _loaded.Count == _roster.ids.Length) StartAuthority();
#endif
            }
            else if (IsHost && !_started && _loaded.Count == _roster.ids.Length) StartAuthority();
            // Pace delayed/batched packets too, so animations cannot overlap after a network stall.
            int budget = 32;
            while (_active && _views.Count > 0 && Time.realtimeSinceStartup >= _nextViewAt && budget-- > 0)
            {
                var snapshot = _views.Dequeue();
                if (snapshot.sequence <= _lastApplied) continue;
                _lastApplied = snapshot.sequence;
                _viewRequest = snapshot.request;
                _viewCommand = snapshot.kind == "scores" ? "confirm" : snapshot.actor != 0 ? null :
                    snapshot.kind == "bidRequest" ? "bid" : snapshot.kind == "cardRequest" ? "card" :
                    snapshot.kind == "tigressRequest" ? "tigress" : null;
                _view.ApplyNetworkSnapshot(snapshot);
                if (snapshot.kind == "played" || snapshot.kind == "resolved")
                    _nextViewAt = Time.realtimeSinceStartup + 1.25f;
                if (snapshot.kind == "end") { _finished = true; Status("게임이 종료되었습니다."); }
            }
        }

        private void StartAuthority()
        {
            _started = true;
            Status("모든 참가자 연결 완료. 대전을 시작합니다.");
            var engineObject = new GameObject("Steam Rules Engine (No UI)");
            engineObject.transform.SetParent(transform);
            _authority = engineObject.AddComponent<GameManager>();
            _authority.OnPhaseChanged += _ => Broadcast("phase");
            _authority.OnRoundStarted += _ => Broadcast("round");
            _authority.OnCardsDealt += () => Broadcast("dealt");
            _authority.OnCurrentPlayerChanged += i => Broadcast("turn", i);
            _authority.OnBidRequested += (p, _) => Request("bidRequest", "bid", p.Index);
            _authority.OnPlayerBid += p => Broadcast("bid", p.Index);
            _authority.OnAllBidsComplete += () => Broadcast("allBids");
            _authority.OnTrickStarted += (_, i) => Broadcast("trick", i);
            _authority.OnCardPlayRequested += (p, _) => Request("cardRequest", "card", p.Index);
            _authority.OnTigressChoiceRequested += p => Request("tigressRequest", "tigress", p.Index);
            _authority.OnCardPlayed += p => Broadcast("played", p.PlayerIndex, p);
            _authority.OnTrickResolved += r => Broadcast("resolved", result: r);
            _authority.OnRoundEnded += scores =>
            {
                _request++;
                _expectedCommand = "confirm";
                _confirmed.Clear();
                Broadcast("scores", scores: scores);
            };
            _authority.OnGameEnded += winner => { Broadcast("end", winner.Index); _finished = true; };
            _authority.StartNetworkAuthority(_roster.names);
        }

        private void Request(string kind, string command, int seat)
        {
            _request++;
            _expectedCommand = command;
            _expectedSeat = seat;
            Broadcast(kind, seat);
        }

        private void Broadcast(string kind, int actor = 0, PlayedCard played = default,
            TrickResult result = default, RoundScoreResult[] scores = null)
        {
            _sequence++;
            for (int i = 0; i < _roster.ids.Length && _active; i++)
                Send(i, new NetworkGamePacket { kind = "view", snapshot = NetworkGameSnapshot.Capture(
                    _authority.State, i, kind, _sequence, _request, actor, played, result, scores) });
        }

        private void Handle(int sender, NetworkGamePacket packet)
        {
            if (!_active || packet == null || packet.protocol != 1 || packet.session != _roster.token) return;
            _lastSeen[sender] = Time.realtimeSinceStartup;
            if (packet.kind == "abort" && sender == _hostSeat)
            { Fail("대전 연결이 중단되었습니다.", false); return; }
            if (packet.kind == "ping" && sender == _hostSeat)
            { Send(_hostSeat, new NetworkGamePacket { kind = "loaded" }); return; }
            if (packet.kind == "loaded" && IsHost) { _loaded.Add(sender); return; }
            if (packet.kind == "view" && sender == _hostSeat && packet.snapshot != null)
            {
                _started = true;
                if (_views.Count >= 256) { Fail("화면 동기화 대기열이 초과됐습니다."); return; }
                _views.Enqueue(packet.snapshot);
                return;
            }
            if (packet.kind != "input" || !IsHost || _authority == null ||
                packet.request != _request || packet.command != _expectedCommand) return;
            if (packet.command == "confirm")
            {
                if (!_confirmed.Add(sender)) return;
                if (_confirmed.Count == _roster.ids.Length)
                {
                    _expectedCommand = null;
                    _authority.TryNetworkInput(sender, "confirm", 0);
                }
                return;
            }
            if (sender != _expectedSeat) return;
            if (_authority.TryNetworkInput(sender, packet.command, packet.value)) _expectedCommand = null;
        }

        private void Send(int seat, NetworkGamePacket packet)
        {
            if (!_active) return;
            packet.session = _roster.token;
            if (_localTransport != null)
            {
                _localTransport.Send(_localSeat, seat, packet);
                return;
            }
            if (seat == _localSeat) { Handle(seat, packet); return; }
#if STEAMWORKS_NET
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
            if (bytes.Length > MaximumPacketBytes) { Fail("전송 데이터가 너무 큽니다."); return; }
            IntPtr memory = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, memory, bytes.Length);
                var identity = Identity(seat);
                EResult result = SteamNetworkingMessages.SendMessageToUser(ref identity, memory,
                    (uint)bytes.Length, Constants.k_nSteamNetworkingSend_Reliable, Channel);
                if (result != EResult.k_EResultOK) Fail("Steam 대전 전송 실패: " + result, false);
            }
            finally { Marshal.FreeHGlobal(memory); }
#endif
        }

#if STEAMWORKS_NET
        private SteamNetworkingIdentity Identity(int seat)
        {
            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(ulong.Parse(_roster.ids[seat]));
            return identity;
        }

        private bool CheckRoster()
        {
            if (SteamLobbyService.Instance == null || !SteamLobbyService.Instance.IsInLobby ||
                SteamLobbyService.Instance.CurrentLobbyId != _lobby.m_SteamID ||
                SteamMatchmaking.GetLobbyOwner(_lobby).m_SteamID.ToString() != _roster.host ||
                SteamMatchmaking.GetNumLobbyMembers(_lobby) != _roster.ids.Length) return false;
            for (int i = 0; i < _roster.ids.Length; i++)
                if (Array.IndexOf(_roster.ids, SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i).m_SteamID.ToString()) < 0)
                    return false;
            return true;
        }

        private void Receive()
        {
            int count = SteamNetworkingMessages.ReceiveMessagesOnChannel(Channel, _receiveBuffer, _receiveBuffer.Length);
            for (int i = 0; i < count; i++)
            {
                IntPtr pointer = _receiveBuffer[i];
                try
                {
                    var message = SteamNetworkingMessage_t.FromIntPtr(pointer);
                    int sender = Array.IndexOf(_roster.ids, message.m_identityPeer.GetSteamID64().ToString());
                    if (!_active || sender < 0 || message.m_cbSize <= 0 || message.m_cbSize > MaximumPacketBytes) continue;
                    byte[] bytes = new byte[message.m_cbSize];
                    Marshal.Copy(message.m_pData, bytes, 0, bytes.Length);
                    Handle(sender, JsonUtility.FromJson<NetworkGamePacket>(Encoding.UTF8.GetString(bytes)));
                }
                catch (Exception error) { Fail("대전 데이터 처리 실패: " + error.Message); }
                finally { SteamNetworkingMessage_t.Release(pointer); }
            }
        }

        private void OnSessionRequested(SteamNetworkingMessagesSessionRequest_t request)
        {
            if (!_active || Array.IndexOf(_roster.ids, request.m_identityRemote.GetSteamID64().ToString()) < 0) return;
            SteamNetworkingMessages.AcceptSessionWithUser(ref request.m_identityRemote);
        }

        private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t failure)
        {
            if (_active && Array.IndexOf(_roster.ids, failure.m_info.m_identityRemote.GetSteamID64().ToString()) >= 0)
                Fail("참가자와 Steam 통신을 연결하지 못했습니다.");
        }
#endif
        private void Status(string text)
        {
            Debug.Log((_localTransport == null ? "[Steam Game] " : "[Local Multiplayer] ") + text);
            _view?.SetNetworkStatus(text);
        }

        private void Fail(string reason, bool notify = true)
        {
            if (_active && notify && IsHost)
                for (int i = 0; i < _roster.ids.Length; i++)
                    if (i != _localSeat) Send(i, new NetworkGamePacket { kind = "abort" });
            _active = false;
            _viewCommand = null;
            _views.Clear();
            _authority?.StopNetworkGame();
            _view?.StopNetworkGame();
            Status(reason + " 게임을 다시 시작하려면 로비로 돌아가 새 방을 만들어 주세요.");
        }

        public void ReturnToLobby()
        {
            _active = false;
            if (_localTransport == null) SteamLobbyService.Instance?.LeaveLobby();
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        private void OnDestroy()
        {
            _active = false;
#if STEAMWORKS_NET
            _sessionRequested?.Dispose();
            _sessionFailed?.Dispose();
            if (_transportReady && SteamPlatformBootstrap.Instance != null && SteamPlatformBootstrap.Instance.IsInitialized)
                for (int i = 0; i < _roster.ids.Length; i++)
                    if (i != _localSeat)
                    {
                        var identity = Identity(i);
                        SteamNetworkingMessages.CloseChannelWithUser(ref identity, Channel);
                    }
#endif
        }
    }
}
