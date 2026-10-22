using System;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace SkullKing.Networking
{
    /// <summary>
    /// Steam API의 수명과 콜백 처리를 담당하는 단일 진입점입니다.
    /// 로비와 게임 네트워크 코드는 이 컴포넌트의 초기화 결과만 사용합니다.
    /// </summary>
    public sealed class SteamPlatformBootstrap : MonoBehaviour
    {
        public static SteamPlatformBootstrap Instance { get; private set; }

        public bool IsInitialized { get; private set; }
        public string LocalPersonaName { get; private set; } = "Steam Offline";
        public ulong LocalSteamId { get; private set; }

        public event Action<bool> OnInitializationChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            if (Instance != null) return;

            var bootstrapObject = new GameObject("SteamPlatform (Auto)");
            bootstrapObject.AddComponent<SteamPlatformBootstrap>();
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
            InitializeSteam();
        }

        private void Update()
        {
#if STEAMWORKS_NET
            if (IsInitialized)
                SteamAPI.RunCallbacks();
#endif
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

#if STEAMWORKS_NET
            if (IsInitialized)
                SteamAPI.Shutdown();
#endif

            IsInitialized = false;
            Instance = null;
        }

        private void InitializeSteam()
        {
            Debug.Log("[Steam] 초기화를 시작합니다...");
#if STEAMWORKS_NET
            try
            {
                if (!Packsize.Test())
                    throw new InvalidOperationException("Steamworks 구조체 크기가 현재 플랫폼과 맞지 않습니다.");
                if (!DllCheck.Test())
                    throw new InvalidOperationException("Steamworks 네이티브 라이브러리 버전이 맞지 않습니다.");
                if (!SteamAPI.Init())
                    throw new InvalidOperationException("Steam 클라이언트 연결에 실패했습니다. Steam이 실행 중인지 확인하세요.");

                IsInitialized = true;
                LocalPersonaName = SteamFriends.GetPersonaName();
                LocalSteamId = SteamUser.GetSteamID().m_SteamID;
                Debug.Log($"[Steam] 연결 성공: {LocalPersonaName} ({LocalSteamId})");
            }
            catch (Exception exception)
            {
                IsInitialized = false;
                LocalPersonaName = "Steam Offline";
                LocalSteamId = 0;
                Debug.LogError($"[Steam] 초기화 실패: {exception.Message}");
            }
#else
            IsInitialized = false;
            Debug.LogWarning("[Steam] Steamworks.NET 패키지를 불러오는 중이거나 현재 플랫폼에서 비활성화되어 있습니다.");
#endif

            OnInitializationChanged?.Invoke(IsInitialized);
        }
    }
}
