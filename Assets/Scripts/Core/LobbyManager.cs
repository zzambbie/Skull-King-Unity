// ============================================================
// LobbyManager.cs — 로비 씬 관리자
// 플레이어 수(3~6) 설정 → 게임 시작
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using SkullKing.Networking;
using System.Collections.Generic;

namespace SkullKing.Core
{
    /// <summary>
    /// 로비 씬에서 게임 설정을 관리합니다.
    /// 인원수 설정 후 게임 씬으로 전환합니다.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        // ============================================================
        // 씬 간 데이터 전달용 static 변수
        // ============================================================

        /// <summary>게임에 참여할 총 플레이어 수 (3~6)</summary>
        public static int SelectedPlayerCount { get; private set; } = 4;

        /// <summary>인간 플레이어의 인덱스</summary>
        public static int HumanPlayerIndex { get; private set; } = 0;

        /// <summary>게임 시작 여부 (GameScene에서 확인용)</summary>
        public static bool ShouldStartGame { get; set; } = false;

        // ============================================================
        // [SerializeField] — 회원님이 씬에서 UI에 연결할 필드
        // ============================================================

        [Header("선택된 플레이어 수를 표시할 텍스트")]
        [SerializeField] private UnityEngine.UI.Text _playerCountText;

        [Header("멀티 방 최대 인원 표시 텍스트")]
        [SerializeField] private UnityEngine.UI.Text _steamPlayerCountText;

        [Header("멀티 방 공개 범위 표시 텍스트")]
        [SerializeField] private UnityEngine.UI.Text _steamAccessText;

        [Header("게임 씬 이름")]
        [SerializeField] private string _gameSceneName = "GameScene";

        [Header("Steam 연결 상태를 표시할 텍스트 (선택)")]
        [SerializeField] private UnityEngine.UI.Text _steamStatusText;

        [Header("Steam 멀티플레이 패널 (선택)")]
        [SerializeField] private GameObject _steamLobbyPanel;

        [Header("Steam 방 참가자 목록 텍스트 (선택)")]
        [SerializeField] private UnityEngine.UI.Text _steamMemberListText;

        [Header("화면 전환 패널 (모두 선택 사항)")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _singlePlayerSetupPanel;
        [SerializeField] private GameObject _steamRoomCreationPanel;
        [SerializeField] private GameObject _steamWaitingRoomPanel;

        [Header("대기방 준비 / 게임 시작 버튼")]
        [SerializeField] private UnityEngine.UI.Button _readyButton;
        [SerializeField] private UnityEngine.UI.Button _startMultiplayerButton;
        [Tooltip("준비 버튼 자식의 Text 또는 Text (TMP)를 연결하세요.")]
        [SerializeField] private UnityEngine.UI.Text _readyButtonText;
        [SerializeField] private TMPro.TMP_Text _readyButtonTmpText;

        public static bool IsMultiplayerScenePreview { get; private set; }

        // ============================================================
        // 내부 상태
        // ============================================================

        private int _currentPlayerCount = 4;
        private int _steamMaximumPlayers = 4;
        private SteamLobbyAccess _steamAccess = SteamLobbyAccess.Public;
        private const int MinPlayers = 3;
        private const int MaxPlayers = 6;
        private SteamLobbyService _steamLobbyService;

        /// <summary>Connect only the main-title Exit button; room Back/Leave buttons remain separate.</summary>
        public void QuitGame()
        {
            GameSettings.Save();
            if (SteamLobbyService.Instance != null) SteamLobbyService.Instance.LeaveLobby();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Start()
        {
            _currentPlayerCount = 4;
            _steamMaximumPlayers = 4;
            UpdateUI();
            RefreshSteamStatus();
            if (SteamPlatformBootstrap.Instance != null && SteamPlatformBootstrap.Instance.IsInitialized)
            {
                _steamLobbyService = SteamLobbyService.GetOrCreate();
                SubscribeToSteamLobby();
                if (_steamLobbyService.IsInLobby)
                    OpenSteamWaitingRoom();
            }
            Debug.Log("[Lobby] 로비 씬 로드됨. 플레이어 수를 설정하세요 (3~6).");
        }

        // ============================================================
        // UI에서 호출할 Public 메서드 (버튼에 연결)
        // ============================================================

        /// <summary>
        /// 플레이어 수를 +1 합니다. (UI 버튼에 연결)
        /// </summary>
        public void IncreasePlayerCount()
        {
            if (_currentPlayerCount < MaxPlayers)
            {
                _currentPlayerCount++;
                UpdateUI();
                Debug.Log($"[Lobby] 플레이어 수: {_currentPlayerCount}");
            }
        }

        /// <summary>
        /// 플레이어 수를 -1 합니다. (UI 버튼에 연결)
        /// </summary>
        public void DecreasePlayerCount()
        {
            if (_currentPlayerCount > MinPlayers)
            {
                _currentPlayerCount--;
                UpdateUI();
                Debug.Log($"[Lobby] 플레이어 수: {_currentPlayerCount}");
            }
        }

        /// <summary>
        /// 특정 숫자로 플레이어 수를 직접 설정합니다. (드롭다운 등에 연결 가능)
        /// </summary>
        public void SetPlayerCount(int count)
        {
            _currentPlayerCount = Mathf.Clamp(count, MinPlayers, MaxPlayers);
            UpdateUI();
        }

        public void OpenSinglePlayerSetup()
        {
            SetPanelActive(_mainMenuPanel, false);
            SetPanelActive(_singlePlayerSetupPanel, true);
        }

        public void CloseSinglePlayerSetup()
        {
            SetPanelActive(_singlePlayerSetupPanel, false);
            SetPanelActive(_mainMenuPanel, true);
        }

        /// <summary>
        /// 봇과 싱글플레이 게임을 시작합니다. (기존 시작 버튼에 연결)
        /// </summary>
        public void StartSinglePlayer()
        {
            IsMultiplayerScenePreview = false;
            SelectedPlayerCount = _currentPlayerCount;
            HumanPlayerIndex = 0;
            ShouldStartGame = true;

            Debug.Log($"[Lobby] 싱글플레이 시작! 플레이어 1명 + 봇 {SelectedPlayerCount - 1}명 → {_gameSceneName} 로딩");
            SceneManager.LoadScene(_gameSceneName);
        }

        /// <summary>
        /// 기존 씬에 저장된 버튼 연결을 깨지 않기 위한 호환용 메서드입니다.
        /// 새로 연결할 때는 StartSinglePlayer를 사용하세요.
        /// </summary>
        public void StartGame()
        {
            StartSinglePlayer();
        }

        /// <summary>
        /// Steam 멀티플레이 화면으로 진입합니다.
        /// 현재 단계에서는 Steam 연결을 확인하고, 다음에 만들 방 생성/참가 UI의 진입점 역할을 합니다.
        /// </summary>
        public void OpenSteamMultiplayer()
        {
            SteamPlatformBootstrap steam = SteamPlatformBootstrap.Instance;
            if (steam == null || !steam.IsInitialized)
            {
                SetSteamStatus("Steam 연결 실패\nSteam을 실행한 뒤 다시 시도하세요.");
                Debug.LogWarning("[Steam Lobby] Steam에 연결되지 않아 멀티플레이를 열 수 없습니다.");
                return;
            }

            _steamLobbyService = SteamLobbyService.GetOrCreate();
            SubscribeToSteamLobby();

            SetPanelActive(_mainMenuPanel, false);
            if (_steamLobbyPanel != null)
                _steamLobbyPanel.SetActive(true);

            SetSteamStatus($"Steam 연결됨: {steam.LocalPersonaName}\n방 만들기 또는 빠른 참가를 선택하세요.");
            Debug.Log($"[Steam Lobby] 멀티플레이 선택: {steam.LocalPersonaName} ({steam.LocalSteamId})");
        }

        public void CloseSteamMultiplayer()
        {
            SetPanelActive(_steamRoomCreationPanel, false);
            SetPanelActive(_steamWaitingRoomPanel, false);
            SetPanelActive(_steamLobbyPanel, false);
            SetPanelActive(_mainMenuPanel, true);
        }

        public void OpenSteamRoomCreation()
        {
            SetPanelActive(_steamLobbyPanel, false);
            SetPanelActive(_steamRoomCreationPanel, true);
            UpdateUI();
        }

        public void CloseSteamRoomCreation()
        {
            SetPanelActive(_steamRoomCreationPanel, false);
            SetPanelActive(_steamLobbyPanel, true);
        }

        public void IncreaseSteamPlayerCount()
        {
            if (_steamMaximumPlayers < MaxPlayers)
                _steamMaximumPlayers++;
            UpdateUI();
        }

        public void DecreaseSteamPlayerCount()
        {
            if (_steamMaximumPlayers > MinPlayers)
                _steamMaximumPlayers--;
            UpdateUI();
        }

        public void SetSteamLobbyAccess(int accessIndex)
        {
            _steamAccess = (SteamLobbyAccess)Mathf.Clamp(accessIndex, 0, 2);
            UpdateUI();
        }

        public void NextSteamLobbyAccess()
        {
            _steamAccess = (SteamLobbyAccess)(((int)_steamAccess + 1) % 3);
            UpdateUI();
        }

        public void PreviousSteamLobbyAccess()
        {
            _steamAccess = (SteamLobbyAccess)(((int)_steamAccess + 2) % 3);
            UpdateUI();
        }

        public void CreateSteamLobby()
        {
            _steamLobbyService = SteamLobbyService.GetOrCreate();
            SubscribeToSteamLobby();
            _steamLobbyService.CreateLobby(_steamMaximumPlayers, _steamAccess);
        }

        public void QuickJoinSteamLobby()
        {
            _steamLobbyService = SteamLobbyService.GetOrCreate();
            SubscribeToSteamLobby();
            _steamLobbyService.QuickJoinLobby();
        }

        public void LeaveSteamLobby()
        {
            if (_steamLobbyService != null)
                _steamLobbyService.LeaveLobby();

            SetPanelActive(_steamWaitingRoomPanel, false);
            SetPanelActive(_steamLobbyPanel, true);
        }

        public void InviteSteamFriends()
        {
            _steamLobbyService = SteamLobbyService.GetOrCreate();
            SubscribeToSteamLobby();
            _steamLobbyService.InviteFriends();
        }

        public void ToggleSteamReady()
        {
            if (_steamLobbyService != null)
                _steamLobbyService.ToggleReady();
        }

        public void StartSteamMultiplayerGame()
        {
            if (_steamLobbyService == null)
                return;
            if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
            {
                SetSteamStatus("GameScene이 빌드 씬 목록에 없습니다.");
                return;
            }
            _steamLobbyService.RequestStartGame();
        }

        private void OnSteamGameStartRequested()
        {
            // 아직 네트워크 대전 엔진이 없으므로 로컬 봇 게임은 시작하지 않습니다.
            IsMultiplayerScenePreview = true;
            ShouldStartGame = false;
            SceneManager.LoadScene(_gameSceneName);
        }

        // ============================================================
        // 내부 UI 갱신
        // ============================================================

        private void UpdateUI()
        {
            if (_playerCountText != null)
            {
                _playerCountText.text = $"{_currentPlayerCount}명";
            }

            if (_steamPlayerCountText != null)
                _steamPlayerCountText.text = $"{_steamMaximumPlayers}명";

            if (_steamAccessText != null)
                _steamAccessText.text = SteamLobbyService.GetAccessLabel(_steamAccess);
        }

        private void RefreshSteamStatus()
        {
            SteamPlatformBootstrap steam = SteamPlatformBootstrap.Instance;
            if (steam != null && steam.IsInitialized)
                SetSteamStatus($"Steam 연결됨: {steam.LocalPersonaName}");
            else
                SetSteamStatus("Steam 연결 확인 중...");
        }

        private void SetSteamStatus(string message)
        {
            if (_steamStatusText != null)
                _steamStatusText.text = message;
        }

        private void SubscribeToSteamLobby()
        {
            _steamLobbyService.StatusChanged -= SetSteamStatus;
            _steamLobbyService.StatusChanged += SetSteamStatus;
            _steamLobbyService.MembersChanged -= UpdateSteamMemberList;
            _steamLobbyService.MembersChanged += UpdateSteamMemberList;
            _steamLobbyService.LobbyJoined -= OpenSteamWaitingRoom;
            _steamLobbyService.LobbyJoined += OpenSteamWaitingRoom;
            _steamLobbyService.GameStartRequested -= OnSteamGameStartRequested;
            _steamLobbyService.GameStartRequested += OnSteamGameStartRequested;
            UpdateSteamMemberList(_steamLobbyService.MemberNames);
        }

        private void OpenSteamWaitingRoom()
        {
            SetPanelActive(_mainMenuPanel, false);
            SetPanelActive(_singlePlayerSetupPanel, false);
            SetPanelActive(_steamLobbyPanel, false);
            SetPanelActive(_steamRoomCreationPanel, false);
            SetPanelActive(_steamWaitingRoomPanel, true);

            if (_steamLobbyService != null)
            {
                SetSteamStatus(
                    $"{SteamLobbyService.GetAccessLabel(_steamLobbyService.CurrentAccess)} · " +
                    $"최대 {_steamLobbyService.CurrentMaximumMembers}명\n" +
                    $"방 ID: {_steamLobbyService.CurrentLobbyId}");
            }
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        private void UpdateSteamMemberList(IReadOnlyList<string> memberNames)
        {
            UpdateWaitingRoomButtons();
            if (_steamMemberListText == null)
                return;

            if (memberNames == null || memberNames.Count == 0)
            {
                _steamMemberListText.text = "참가자 없음";
                return;
            }

            _steamMemberListText.text = $"참가자 ({memberNames.Count}명)\n• " +
                                        string.Join("\n• ", memberNames);
        }

        private void UpdateWaitingRoomButtons()
        {
            bool inLobby = _steamLobbyService != null && _steamLobbyService.IsInLobby;
            bool starting = inLobby && _steamLobbyService.GameStarting;
            if (_readyButton != null)
                _readyButton.interactable = inLobby && !starting;
            string label = inLobby && _steamLobbyService.LocalReady ? "준비 취소" : "준비";
            if (_readyButtonText != null) _readyButtonText.text = label;
            if (_readyButtonTmpText != null) _readyButtonTmpText.text = label;
            if (_startMultiplayerButton != null)
            {
                _startMultiplayerButton.gameObject.SetActive(inLobby && _steamLobbyService.IsLobbyOwner);
                _startMultiplayerButton.interactable = inLobby && _steamLobbyService.CanStartGame;
            }
        }

        private void OnDestroy()
        {
            if (_steamLobbyService != null)
            {
                _steamLobbyService.StatusChanged -= SetSteamStatus;
                _steamLobbyService.MembersChanged -= UpdateSteamMemberList;
                _steamLobbyService.LobbyJoined -= OpenSteamWaitingRoom;
                _steamLobbyService.GameStartRequested -= OnSteamGameStartRequested;
            }
        }
    }
}
