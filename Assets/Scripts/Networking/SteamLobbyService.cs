using System;
using System.Collections.Generic;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace SkullKing.Networking
{
    public enum SteamLobbyAccess
    {
        Public = 0,
        FriendsOnly = 1,
        Private = 2
    }

    /// <summary>
    /// CROSSION의 Steam 로비 생성, 빠른 참가, 퇴장을 담당합니다.
    /// 게임 참가자 명단은 시작 시 고정하며 실제 대전은 SteamGameSession에서 처리합니다.
    /// </summary>
    public sealed class SteamLobbyService : MonoBehaviour
    {
        private const string GameKey = "cression_game";
        private const string GameValue = "CROSSION";
        private const string VersionKey = "cression_version";
        private const string VersionValue = "3"; // Latest Kraken/White Whale overrides; exclude older-rule builds.

        public static SteamLobbyService Instance { get; private set; }

        public bool IsInLobby { get; private set; }
        public bool IsLobbyOwner { get; private set; }
        public ulong CurrentLobbyId { get; private set; }
        public int CurrentMaximumMembers { get; private set; }
        public SteamLobbyAccess CurrentAccess { get; private set; } = SteamLobbyAccess.Public;
        public IReadOnlyList<string> MemberNames => _memberNames;
        public bool LocalReady { get; private set; }
        public bool GameStarting { get; private set; }
        public bool CanStartGame { get; private set; }
        public string StartBlockReason { get; private set; } = "먼저 방에 참가하세요.";
        public event Action GameStartRequested;

        public event Action<string> StatusChanged;
        public event Action<IReadOnlyList<string>> MembersChanged;
        public event Action LobbyJoined;
        public event Action LobbyLeft;

        private readonly List<string> _memberNames = new List<string>();
        private int _pendingMaximumMembers = 4;
        private SteamLobbyAccess _pendingAccess = SteamLobbyAccess.Public;

#if STEAMWORKS_NET
        private CSteamID _currentLobby;
        private Callback<LobbyEnter_t> _lobbyEntered;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdated;
        private Callback<LobbyDataUpdate_t> _lobbyDataUpdated;
        private Callback<GameLobbyJoinRequested_t> _joinRequested;
        private CallResult<LobbyCreated_t> _lobbyCreated;
        private CallResult<LobbyMatchList_t> _lobbyListReceived;
#endif

        public static SteamLobbyService GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            GameObject serviceObject = new GameObject("Steam Lobby Service (Auto)");
            return serviceObject.AddComponent<SteamLobbyService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if STEAMWORKS_NET
            _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyChatUpdated = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdated);
            _lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
            _joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyListReceived = CallResult<LobbyMatchList_t>.Create(OnLobbyListReceived);
#endif
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

#if STEAMWORKS_NET
            _lobbyEntered?.Dispose();
            _lobbyChatUpdated?.Dispose();
            _lobbyDataUpdated?.Dispose();
            _joinRequested?.Dispose();
            _lobbyCreated?.Dispose();
            _lobbyListReceived?.Dispose();
#endif

            Instance = null;
        }

        public void CreateLobby(int maximumMembers, SteamLobbyAccess access)
        {
            maximumMembers = Mathf.Clamp(maximumMembers, 3, 6);
            if (!CanUseSteam("방을 만들 수 없습니다."))
                return;

#if STEAMWORKS_NET
            LeaveCurrentLobbyWithoutNotice();
            _pendingMaximumMembers = maximumMembers;
            _pendingAccess = access;

            ELobbyType lobbyType = access switch
            {
                SteamLobbyAccess.FriendsOnly => ELobbyType.k_ELobbyTypeFriendsOnly,
                SteamLobbyAccess.Private => ELobbyType.k_ELobbyTypePrivate,
                _ => ELobbyType.k_ELobbyTypePublic
            };

            PublishStatus($"Steam 방 생성 중... (최대 {maximumMembers}명 · {GetAccessLabel(access)})");
            _lobbyCreated.Set(SteamMatchmaking.CreateLobby(lobbyType, maximumMembers));
#endif
        }

        public void CreatePublicLobby(int maximumMembers)
        {
            CreateLobby(maximumMembers, SteamLobbyAccess.Public);
        }

        public void QuickJoinLobby()
        {
            if (!CanUseSteam("방을 검색할 수 없습니다."))
                return;

#if STEAMWORKS_NET
            LeaveCurrentLobbyWithoutNotice();
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                GameKey,
                GameValue,
                ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                VersionKey,
                VersionValue,
                ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                "state", "waiting", ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                "access",
                SteamLobbyAccess.Public.ToString(),
                ELobbyComparison.k_ELobbyComparisonEqual);

            PublishStatus("참가 가능한 CROSSION 방 검색 중...");
            _lobbyListReceived.Set(SteamMatchmaking.RequestLobbyList());
#endif
        }

        public void LeaveLobby()
        {
            if (!IsInLobby)
            {
                PublishStatus("현재 참가 중인 Steam 방이 없습니다.");
                return;
            }

            LeaveCurrentLobbyWithoutNotice();
            PublishStatus("Steam 방에서 나왔습니다.");
            LobbyLeft?.Invoke();
        }

        public void InviteFriends()
        {
            if (!CanUseSteam("친구 초대를 열 수 없습니다."))
                return;

            if (!IsInLobby)
            {
                PublishStatus("먼저 Steam 방을 만들어야 친구를 초대할 수 있습니다.");
                return;
            }

#if STEAMWORKS_NET
            SteamFriends.ActivateGameOverlayInviteDialog(_currentLobby);
            PublishStatus("Steam 친구 초대 창을 열었습니다.");
#endif
        }

        public void ToggleReady()
        {
            if (!CanUseSteam("준비 상태를 변경할 수 없습니다.") || !IsInLobby || GameStarting)
                return;
#if STEAMWORKS_NET
            LocalReady = !LocalReady;
            SteamMatchmaking.SetLobbyMemberData(_currentLobby, "ready", LocalReady ? "1" : "0");
            RefreshMembers();
#endif
        }

        public void RequestStartGame()
        {
            if (!CanUseSteam("게임을 시작할 수 없습니다.") || !IsInLobby || GameStarting)
                return;
#if STEAMWORKS_NET
            RefreshMembers();
            if (!CanStartGame)
            {
                PublishStatus(StartBlockReason);
                return;
            }
            if (!SteamMatchmaking.SetLobbyJoinable(_currentLobby, false))
            {
                PublishStatus("방 참가 잠금에 실패했습니다. 다시 시도하세요.");
                return;
            }
            // One metadata value fixes seats and session identity before announcing the start.
            RefreshMembers();
            if (!CanStartGame || !FreezeGameRoster())
            {
                SteamMatchmaking.SetLobbyJoinable(_currentLobby, true);
                PublishStatus("참가자 정보를 확정하지 못했습니다. 모두 준비한 뒤 다시 시도하세요.");
                return;
            }
            if (!SteamMatchmaking.SetLobbyData(_currentLobby, "state", "starting"))
            {
                SteamMatchmaking.SetLobbyJoinable(_currentLobby, true);
                PublishStatus("시작 요청 전송에 실패했습니다. 다시 시도하세요.");
                return;
            }
            HandleStartSignal();
#endif
        }

        private bool CanUseSteam(string failureMessage)
        {
            SteamPlatformBootstrap steam = SteamPlatformBootstrap.Instance;
            if (steam != null && steam.IsInitialized)
                return true;

            PublishStatus($"{failureMessage} Steam 연결을 확인하세요.");
            return false;
        }

        private void PublishStatus(string message)
        {
            Debug.Log($"[Steam Lobby] {message}");
            StatusChanged?.Invoke(message);
        }

        private void PublishMembers()
        {
            MembersChanged?.Invoke(MemberNames);
        }

#if STEAMWORKS_NET
        private bool FreezeGameRoster()
        {
            int count = SteamMatchmaking.GetNumLobbyMembers(_currentLobby);
            var roster = new SteamGameRoster
            {
                token = Guid.NewGuid().ToString("N"),
                host = SteamUser.GetSteamID().m_SteamID.ToString(),
                ids = new string[count], names = new string[count]
            };
            for (int i = 0; i < count; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobby, i);
                roster.ids[i] = member.m_SteamID.ToString();
                roster.names[i] = SteamFriends.GetFriendPersonaName(member);
            }
            return SteamMatchmaking.SetLobbyData(_currentLobby, "game_manifest", JsonUtility.ToJson(roster));
        }
#endif

        private void LeaveCurrentLobbyWithoutNotice()
        {
#if STEAMWORKS_NET
            if (IsInLobby && _currentLobby.IsValid())
                SteamMatchmaking.LeaveLobby(_currentLobby);

            _currentLobby = CSteamID.Nil;
#endif
            IsInLobby = false;
            IsLobbyOwner = false;
            CurrentLobbyId = 0;
            CurrentMaximumMembers = 0;
            CurrentAccess = SteamLobbyAccess.Public;
            LocalReady = false;
            GameStarting = false;
            CanStartGame = false;
            StartBlockReason = "먼저 방에 참가하세요.";
            _memberNames.Clear();
            PublishMembers();
        }

#if STEAMWORKS_NET
        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                PublishStatus($"방 생성 실패: {result.m_eResult}");
                return;
            }

            CSteamID lobby = new CSteamID(result.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(lobby, GameKey, GameValue);
            SteamMatchmaking.SetLobbyData(lobby, VersionKey, VersionValue);
            SteamMatchmaking.SetLobbyData(lobby, "host_name", SteamFriends.GetPersonaName());
            SteamMatchmaking.SetLobbyData(lobby, "access", _pendingAccess.ToString());
            SteamMatchmaking.SetLobbyData(lobby, "max_members", _pendingMaximumMembers.ToString());
            SteamMatchmaking.SetLobbyData(lobby, "state", "waiting");
            SteamMatchmaking.SetLobbyJoinable(lobby, true);
            PublishStatus($"Steam 방 생성 성공: {result.m_ulSteamIDLobby}");
        }

        private void OnLobbyEntered(LobbyEnter_t result)
        {
            EChatRoomEnterResponse response = (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse;
            if (response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                PublishStatus($"방 참가 실패: {response}");
                return;
            }

            _currentLobby = new CSteamID(result.m_ulSteamIDLobby);
            CurrentLobbyId = result.m_ulSteamIDLobby;
            IsInLobby = true;
            IsLobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobby) == SteamUser.GetSteamID();
            CurrentMaximumMembers = SteamMatchmaking.GetLobbyMemberLimit(_currentLobby);

            string accessValue = SteamMatchmaking.GetLobbyData(_currentLobby, "access");
            if (!Enum.TryParse(accessValue, out SteamLobbyAccess parsedAccess))
                parsedAccess = SteamLobbyAccess.Public;
            CurrentAccess = parsedAccess;

            LocalReady = false;
            GameStarting = false;
            SteamMatchmaking.SetLobbyMemberData(_currentLobby, "ready", "0");

            RefreshMembers();

            string role = IsLobbyOwner ? "방장" : "참가자";
            PublishStatus($"Steam 방 참가 완료 ({role}) · {CurrentLobbyId}");
            LobbyJoined?.Invoke();
            HandleStartSignal();
        }

        private void OnLobbyListReceived(LobbyMatchList_t result, bool ioFailure)
        {
            if (ioFailure)
            {
                PublishStatus("Steam 방 검색 중 통신 오류가 발생했습니다.");
                return;
            }

            if (result.m_nLobbiesMatching == 0)
            {
                PublishStatus("참가 가능한 CROSSION 방을 찾지 못했습니다.");
                return;
            }

            CSteamID targetLobby = SteamMatchmaking.GetLobbyByIndex(0);
            PublishStatus($"방을 찾았습니다. 참가 중... ({targetLobby.m_SteamID})");
            SteamMatchmaking.JoinLobby(targetLobby);
        }

        private void OnLobbyChatUpdated(LobbyChatUpdate_t result)
        {
            if (!IsInLobby || result.m_ulSteamIDLobby != CurrentLobbyId)
                return;

            RefreshMembers();
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t result)
        {
            LeaveCurrentLobbyWithoutNotice();
            PublishStatus($"Steam 친구 초대를 통해 방 참가 중... ({result.m_steamIDLobby.m_SteamID})");
            SteamMatchmaking.JoinLobby(result.m_steamIDLobby);
        }

        private void RefreshMembers()
        {
            _memberNames.Clear();
            IsLobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobby) == SteamUser.GetSteamID();
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(_currentLobby);
            bool allReady = true;
            for (int index = 0; index < memberCount; index++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobby, index);
                string memberName = SteamFriends.GetFriendPersonaName(member);
                if (member == SteamMatchmaking.GetLobbyOwner(_currentLobby))
                    memberName += " (방장)";

                bool ready = SteamMatchmaking.GetLobbyMemberData(_currentLobby, member, "ready") == "1";
                if (member == SteamUser.GetSteamID())
                    ready = LocalReady;
                allReady &= ready;
                memberName += ready ? " · 준비 완료" : " · 준비 대기";

                _memberNames.Add(memberName);
            }

            StartBlockReason = !IsLobbyOwner ? "방장만 시작할 수 있습니다." :
                memberCount < 3 ? "최소 3명이 필요합니다." :
                !allReady ? "모든 참가자가 준비해야 합니다." :
                GameStarting ? "게임 시작 중입니다." : "";
            CanStartGame = StartBlockReason.Length == 0;
            PublishMembers();
        }

        private void OnLobbyDataUpdated(LobbyDataUpdate_t result)
        {
            if (!IsInLobby || result.m_ulSteamIDLobby != CurrentLobbyId || result.m_bSuccess == 0)
                return;
            RefreshMembers();
            HandleStartSignal();
        }

        private void HandleStartSignal()
        {
            if (!IsInLobby || GameStarting ||
                SteamMatchmaking.GetLobbyData(_currentLobby, "state") != "starting" ||
                string.IsNullOrEmpty(SteamMatchmaking.GetLobbyData(_currentLobby, "game_manifest")))
                return;
            GameStarting = true;
            RefreshMembers();
            PublishStatus("게임 화면으로 이동합니다. 모든 참가자의 연결을 확인합니다.");
            GameStartRequested?.Invoke();
        }
#endif

        public static string GetAccessLabel(SteamLobbyAccess access)
        {
            return access switch
            {
                SteamLobbyAccess.FriendsOnly => "친구 전용",
                SteamLobbyAccess.Private => "비공개 (초대만)",
                _ => "공개 (빠른 참가 가능)"
            };
        }
    }
}
