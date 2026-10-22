# 타이틀 복귀와 프로그램 종료

## GameScene: 타이틀로

1. Play 종료 후 GameScene을 엽니다.
2. GameCanvas의 ReturnToLobbyButton을 켭니다.
3. 자식 Text (TMP)의 표시 문구를 ‘타이틀로’로 변경합니다.
4. 기존 On Click: GameManager.ReturnMultiplayerToLobby 연결은 유지해도 됩니다. 이제 ReturnToTitle을 호출하는 호환용 이름이라 싱글에서도 작동합니다. 새로 연결할 경우 GameManager.ReturnToTitle을 선택합니다.
5. 우측 상단에 놓고 규칙 버튼과 겹치지 않게 배치해 씬을 저장합니다.

싱글은 진행을 중단하고 LobbyScene을 로드합니다. 멀티는 기존 Steam 세션의 퇴장 처리를 거쳐 LobbyScene으로 돌아갑니다. 현재는 즉시 복귀하며 별도 확인 창은 추가하지 않았습니다.

## LobbyScene: 게임 종료

1. LobbyScene의 MainMenuPanel 안에서 기존 메인 버튼을 복제하고 이름을 QuitGameButton으로 정합니다.
2. 멀티플레이 버튼 아래로 옮기고 자식 Text (TMP)에 ‘게임 종료’를 입력합니다.
3. Button > On Click의 복제된 기존 항목을 모두 제거합니다. 게임 시작/멀티 메뉴 열기 등의 함수가 남으면 안 됩니다.
4. 새 On Click 항목을 추가하고 Hierarchy의 LobbyManager를 드래그합니다.
5. LobbyManager > QuitGame()을 선택하고 씬을 저장합니다.

SteamMenuPanel의 기존 BtnQuit는 CloseSteamMultiplayer에 연결된 ‘멀티 메뉴 닫기’ 기능입니다. 프로그램 종료로 바꾸지 않습니다.

Unity Editor에서는 종료 버튼이 Play 모드를 끝냅니다. Windows 실행 파일에서는 프로그램을 종료합니다. 두 경로 모두 설정을 저장합니다.

## 확인

싱글 시작 → 타이틀로 → 다시 싱글 시작 / 멀티 퇴장 후 타이틀 / 타이틀에서 게임 종료를 확인합니다. 실제 exe에 반영하려면 씬을 저장한 뒤 Windows 빌드를 다시 만들어야 합니다.
