# 설정 창 기능 연결

기능 코드는 GameSettings.cs와 SettingsPanelController.cs에 있으며 UI 배치와 색상은 씬에서 직접 수정한다. 씬이나 프리팹은 코드로 변경하지 않았다.

## GameScene 연결

Play를 끄고 SettingsPanel을 잠시 활성화한다.

1. SettingsPanel에 Add Component로 `SettingsPanelController`를 추가한다. SettingsWindow가 아닌 **SettingsPanel**에 붙인다.
2. `Master Volume Slider`에 기존 MasterVolumeSlider를 연결한다.
3. SettingsWindow 안에 Text - TextMeshPro를 하나 만든다. 이름은 MasterVolumeValueText, 초기 글자는 100%, 기존 한글 폰트를 사용한다. 슬라이더 오른쪽처럼 원하는 위치에 두고 `Master Volume Value Text`에 연결한다.
4. SettingsWindow 안에 UI > Toggle을 만들고 FullscreenToggle로 이름을 바꾼다. 라벨은 전체화면으로 바꾸고 `Fullscreen Toggle`에 연결한다. 이 필드는 Toggle 컴포넌트를 받으므로 TMP가 아닌 일반 UI Toggle이어도 된다.
5. SettingsWindow 안에 Button - TextMeshPro를 만들고 ResetSettingsButton, 글자는 기본값으로 설정한다. `Reset Button`에 연결한다.
6. 기존 CloseButton을 `Close Button`에 연결한다. 이 방식으로 연결하면 닫기 버튼의 기존 On Click > SetActive 항목은 제거해도 된다.
7. 슬라이더의 On Value Changed, 토글의 On Value Changed, 기본값 버튼의 On Click은 **별도로 연결하지 않는다**. 컨트롤러가 해당 UI 이벤트를 연결·해제한다.
8. SettingsButton의 기존 On Click > SettingsPanel.SetActive(true)는 유지한다.
9. SettingsPanel을 다시 비활성화하고 씬을 저장한다.

컨트롤러의 각 참조는 선택 사항이다. 필요한 UI만 먼저 연결해도 되지만, 연결하지 않은 기능은 해당 UI로 조작할 수 없다.

## 동작

- 음량: 0~100%, 모든 일반 AudioSource 출력의 전체 음량을 조절한다.
- 저장: 변경 후 잠시 기다리거나 설정 창을 닫으면 저장된다. 같은 PC에서 다음 실행과 다른 씬에도 적용된다.
- 전체화면: 창 모드 / 테두리 없는 전체화면. Unity Editor 창은 변경하지 않는다. 실제 화면 전환은 빌드에서 확인한다.
- 기본값: 전체 음량 100%, 전체화면 켬. 다른 저장 데이터나 전적은 지우지 않는다.
- Esc: 열린 설정 창만 닫는다. 닫힌 창을 Esc로 여는 기능은 아직 없다.
- 설정 창은 Time.timeScale을 변경하지 않는다. 멀티플레이의 턴은 계속 진행된다.

## 확인 순서

1. 실행 후 설정 창에서 슬라이더를 30%로 낮춘다. 숫자가 바뀌는지 확인한다.
2. 닫고 다시 열어 30%가 유지되는지 확인한다.
3. Play를 끄고 다시 실행해도 30%인지 확인한다.
4. 기본값을 눌러 100%로 돌아오는지 확인한다.
5. 설정 창을 열고 Esc로 닫히는지 확인한다.
6. 전체화면 토글 값이 다시 열었을 때 유지되는지 확인한다. 실제 화면 전환은 빌드에서 별도 검사한다.

## 소리 확인 (음원이 있을 때만)

아직 게임에 음원을 배치하지 않았다면 슬라이더만 움직여서는 청각적으로 확인할 수 없다. 음원 생성이나 다운로드는 이번 작업에 포함하지 않았다.

원하는 짧은 음원을 Assets로 가져와 SettingsPanel에 AudioSource를 추가한다. AudioClip을 지정하고 Play On Awake와 Loop는 끄고, Spatial Blend는 0(2D), Volume은 1로 둔다. 컨트롤러의 Preview Audio Source에 연결한다. 미리듣기 버튼을 만들 경우 On Click에 SettingsPanelController.PlayPreviewSound()를 연결한다. 이 버튼은 음량을 바꾸며 테스트하기 위한 선택 기능이다. 설정 창을 닫으면 미리듣기는 멈춘다.

배경음악과 효과음을 따로 조절하는 기능은 실제 사운드 배치와 AudioMixer 분리 단계에서 추가한다.

로비에도 같은 설정 창을 두려면 Unity에서 완성된 SettingsPanel을 프리팹으로 저장해 LobbyCanvas에 배치하고, 로비의 설정 버튼은 해당 씬의 패널을 대상으로 SetActive(true)를 연결한다. 모든 참조는 프리팹 내부 UI끼리 연결하면 된다.
