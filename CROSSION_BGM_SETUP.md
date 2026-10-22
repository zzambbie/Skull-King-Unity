# 임시 BGM 적용
음원은 Assets/Audio/Music, 재생 프리팹은 Assets/Prefabs/Audio에 있습니다.
새 재생 스크립트는 없으며, Unity 기본 Audio Source만 사용합니다.

1. Play 모드를 중지합니다. 현재 씬에서 작업한 내용을 저장합니다.
2. LobbyScene을 열고 Project의 Assets/Prefabs/Audio/BGM_Lobby를 Hierarchy의 빈 곳으로 드래그합니다. Canvas나 비활성 패널 안에는 넣지 않습니다. 씬을 저장합니다.
3. GameScene을 열고 BGM_Gameplay를 같은 방법으로 넣고 씬을 저장합니다.
4. LobbyScene에서 Play: 로비 음악 → 싱글플레이 시작: 대전 음악 → 로비 복귀: 로비 음악을 확인합니다.
5. 설정의 전체 볼륨을 0%로 내려 무음인지, 다시 올려 복구되는지 확인합니다.
6. Game 뷰의 Mute Audio가 켜져 있으면 끕니다. 각 씬에는 활성 Audio Listener가 하나씩 있어야 합니다.

프리팹 설정: Play On Awake 켬, Loop 켬, Volume 0.45, Spatial Blend 0 (2D), Pitch 1.
전체 볼륨은 기존 GameSettings의 AudioListener.volume이 적용되므로 추가 연결은 필요 없습니다.
각 씬의 BGM은 씬을 떠날 때 종료됩니다. DontDestroyOnLoad를 붙이지 않습니다.
같은 씬에 프리팹은 하나만 넣으세요. 둘 다 넣으면 곡이 겹칩니다.

교체: 씬의 BGM을 선택하고 Audio Source의 Audio Resource(또는 AudioClip)에 새 곡을 드래그합니다.
기본 곡 자체를 바꾸려면 Project의 프리팹을 열어 수정합니다.
음량: Audio Source의 Volume을 조절합니다. 지금은 BGM 전용 사용자 설정 슬라이더는 없고 전체 볼륨만 적용됩니다.
제거: Hierarchy의 해당 BGM 오브젝트를 지우고 씬을 저장하면 재생되지 않습니다. 원본 음원은 AudioDrafts에도 보관되어 있습니다.

파일/참조 및 설정은 정적으로 검증했지만 실제 Unity 재생과 씬 이동은 에디터에서 확인해야 합니다.
