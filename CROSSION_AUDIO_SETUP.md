# 임시 음악 / 효과음 적용

음원은 Assets/Audio/Music과 Assets/Audio/SFX에 있습니다. 효과음 5개는 외부 샘플 없이 새로 합성한 임시 음원입니다. 원본 생성 방법은 Tools/Audio/create_crossion_effects.py에 보관합니다.

## Unity에서 한 번 적용

1. Play 모드를 종료하고 열려 있는 씬을 저장합니다.
2. Unity 컴파일이 끝나면 CROSSION > Audio > Apply Temporary Audio Setup을 누릅니다.
3. 저장할지 묻는 창이 나오면 저장합니다. Console의 [Audio Setup] 완료 메시지를 확인합니다.

이 작업은 두 씬의 원본을 AudioDrafts/SceneBackups/실행시각에 백업합니다. 기존 로비와 게임 씬을 재생성하지 않으며, 기존 BGM이 있으면 재사용합니다. 씬을 저장한 후 원래 열려 있던 씬으로 돌아옵니다. 반복 실행해도 기존에 연결된 슬라이더와 사용자가 수정한 음원/음량을 덮어쓰지 않습니다.

추가되는 내용:
- 씬마다 BGM 1개와 SFX_Player 1개. 실행 중 SFX_Player는 하나만 유지되어 씬 전환 시 클릭음이 잘리지 않습니다.
- 기존 버튼에 ButtonClickSound. 버튼이 실제로 동작할 때 클릭음을 냅니다.
- 화면에 보이는 GameManager에 GameAudioEvents. 카드 제출, 무효가 아닌 트릭의 승리, 내 비딩 성공/실패 효과음을 연결합니다. 숨겨진 멀티 판정 객체와 모의 상대에는 연결하지 않습니다.
- 기존 SettingsPanelController가 있는 설정창에 BGM / SFX 슬라이더와 % 텍스트. 기존 전체 볼륨, 전체화면, 기본값, ESC 닫기 기능은 유지합니다.
- UI는 편집 시점에 생성되어 씬에 저장됩니다. 실행할 때 코드로 UI를 새로 만들지 않습니다.

## 바꾸는 위치

- BGM 곡: 각 씬 BGM 오브젝트의 Audio Source > Audio Resource (또는 AudioClip).
- BGM 기본 음량: Audio Volume Binding > Base Volume (기본 0.45).
- 효과음: Assets/Prefabs/Audio/SFX_Player 프리팹의 Game Audio Player에서 각 AudioClip 슬롯.
- 효과음 기본 음량: 같은 프리팹의 Audio Volume Binding > Base Volume (기본 0.65).
- 최종 음량 = 기본 음량 × BGM 또는 SFX 설정 × 전체 볼륨. 분류와 전체 볼륨은 중복 적용되지 않습니다.
- 사용자 설정은 PC에 저장됩니다. 기본값은 전체/BGM/SFX 모두 100%이며, 음원별 기본 음량은 유지합니다.
- 새 버튼을 직접 추가했다면 ButtonClickSound 컴포넌트를 붙이거나 적용 메뉴를 다시 실행하세요.

## 확인 순서

1. 로비에서 버튼 클릭 소리와 로비 음악.
2. 게임 시작 후 대전 음악으로 바뀌는지, 중복 재생되지 않는지.
3. 카드 제출, 트릭 승리, 라운드 성공/실패 때 한 번씩 소리가 나는지. 크라켄 무효 트릭에는 승리음이 없어야 합니다.
4. 설정에서 BGM을 0%: 효과음만 들려야 합니다. SFX를 0%: BGM만 들려야 합니다. 전체를 0%: 둘 다 무음이어야 합니다.
5. 설정창 닫기/다시 열기, 씬 이동, 실행 종료 후 재실행해 값 유지 확인.
6. 기본값과 ESC 닫기 확인.

게임 및 에디터 연결 코드의 컴파일을 검사했습니다. 실제 Unity에서의 적용, UI 배치, 청취, 반복/믹싱 확인은 위 순서로 진행해야 합니다. 로비에 SettingsPanelController가 없으면 새 설정창을 만들지는 않습니다. 게임의 기존 설정창에 추가됩니다.
