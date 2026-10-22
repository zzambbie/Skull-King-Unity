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

        [Header("한 PC 멀티플레이 테스트 (Editor / Development Build 전용)")]
        [Tooltip("GameScene을 직접 실행할 때만 적용됩니다. Steam 대신 메모리 안에서 참가자별 메시지를 주고받습니다.")]
        [SerializeField] private bool _debugLocalMultiplayer;
        [Range(3, 6)] [SerializeField] private int _localTestPlayerCount = 3;
        [Tooltip("0은 방을 만든 참가자, 1부터는 일반 참가자입니다. 본인이 조작할 좌석입니다.")]
        [Range(0, 5)] [SerializeField] private int _localTestSeat = 1;
        [Tooltip("각 메시지의 전달 지연(초). 실제 인터넷 통신은 하지 않습니다.")]
        [Range(0f, 1f)] [SerializeField] private float _localTestMessageDelay = 0.15f;
        [Tooltip("자동 참가자가 라운드 결과를 확인하기까지의 대기 시간입니다.")]
        [Range(0.5f, 10f)] [SerializeField] private float _localTestConfirmDelay = 3f;

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

            if (_debugLocalMultiplayer && (Application.isEditor || Debug.isDebugBuild)
                && !LobbyManager.ShouldStartGame && !LobbyManager.IsMultiplayerScenePreview)
            {
                gameObject.AddComponent<SkullKing.Networking.LocalMultiplayerTest>().Begin(
                    _gameManager, _localTestPlayerCount, _localTestSeat,
                    _localTestMessageDelay, _localTestConfirmDelay);
                return;
            }

            if (LobbyManager.IsMultiplayerScenePreview)
            {
                gameObject.AddComponent<SkullKing.Networking.SteamGameSession>().Begin(_gameManager);
                return;
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
