// ============================================================
// LobbyManager.cs — 로비 씬 관리자
// 플레이어 수(3~6) 설정 → 게임 시작
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header("게임 씬 이름")]
        [SerializeField] private string _gameSceneName = "GameScene";

        // ============================================================
        // 내부 상태
        // ============================================================

        private int _currentPlayerCount = 4;
        private const int MinPlayers = 3;
        private const int MaxPlayers = 6;

        private void Start()
        {
            _currentPlayerCount = 4;
            UpdateUI();
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

        /// <summary>
        /// 게임을 시작합니다. (시작 버튼에 연결)
        /// </summary>
        public void StartGame()
        {
            SelectedPlayerCount = _currentPlayerCount;
            HumanPlayerIndex = 0;
            ShouldStartGame = true;

            Debug.Log($"[Lobby] 게임 시작! 플레이어 {SelectedPlayerCount}명 → {_gameSceneName} 로딩");
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
        }
    }
}
