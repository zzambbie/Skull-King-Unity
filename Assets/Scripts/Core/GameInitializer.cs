// ============================================================
// GameInitializer.cs — 게임 씬 초기화
// 로비에서 넘어온 설정값으로 GameManager를 시작시킵니다.
// ============================================================
using UnityEngine;

namespace SkullKing.Core
{
    /// <summary>
    /// 게임 씬에 배치하여, 씬 시작 시 GameManager를 자동으로 초기화합니다.
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("GameManager 참조 (같은 씬에 배치)")]
        [SerializeField] private GameManager _gameManager;

        [Header("직접 테스트 모드 (로비 거치지 않고 바로 시작)")]
        [SerializeField] private bool _debugDirectStart = false;

        [SerializeField] private int _debugPlayerCount = 4;

        private void Start()
        {
            if (_gameManager == null)
            {
                _gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                if (_gameManager == null)
                {
                    Debug.LogError("[GameInitializer] GameManager를 찾을 수 없습니다!");
                    return;
                }
            }

            // 로비에서 넘어온 경우
            if (LobbyManager.ShouldStartGame)
            {
                LobbyManager.ShouldStartGame = false;
                int playerCount = LobbyManager.SelectedPlayerCount;
                int humanIndex = LobbyManager.HumanPlayerIndex;

                Debug.Log($"[GameInitializer] 로비 설정 수신 — {playerCount}명, 인간 P{humanIndex}");
                _gameManager.StartGame(playerCount, humanIndex);
            }
            // 디버그 직접 시작 모드
            else if (_debugDirectStart)
            {
                Debug.Log($"[GameInitializer] 디버그 모드 — {_debugPlayerCount}명으로 바로 시작");
                _gameManager.StartGame(_debugPlayerCount);
            }
            else
            {
                Debug.Log("[GameInitializer] 로비에서 시작하지 않았고, 디버그 모드도 꺼져 있습니다. 대기 중...");
            }
        }
    }
}
