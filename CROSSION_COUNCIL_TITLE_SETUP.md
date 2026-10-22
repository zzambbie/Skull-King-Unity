# 차원의 의회 · 타이틀 UI

## 설정/종료를 싱글플레이 크기에 맞추기

LobbyScene을 저장하고 CROSSION > UI > Match Settings And Exit Size를 실행합니다.
싱글플레이 버튼의 실제 크기, 프레임 소재, 글자 크기를 기준으로 설정/종료를 맞춥니다.
우측 하단에 위아래로 배치하며, 공간이 모자라면 싱글/멀티를 함께 위로 조금 이동합니다.
로고와 규칙/피드백 위치 및 모든 클릭 기능은 유지합니다. 씬을 먼저 백업합니다.
기존 Update Title Corner Buttons 메뉴는 이전 소형 배치를 다시 적용하므로, 크기 맞춤에는 새 메뉴를 사용하세요.

## 모서리 버튼 배치 변경

이미 테마가 적용된 LobbyScene을 저장한 뒤 CROSSION > UI > Update Title Corner Buttons를 실행합니다.

- 규칙 / 피드백: 우측 상단, 모서리 기준 고정.
- 설정 / 종료: 우측 하단에 위아래로 배치한 340×64 금속 블록. 원형 아이콘은 숨기고 기존 버튼 문구를 중앙에 표시합니다.
- 로고 / 싱글 / 멀티 / 배경과 모든 버튼의 On Click 연결은 유지합니다.
- 변경 전 씬 자동 백업. 같은 메뉴를 다시 실행해도 시각 오브젝트나 이벤트가 중복 생성되지 않습니다.
- 위치/크기는 각 버튼의 Rect Transform, 글자는 CouncilVisual/CouncilLabel에서 수정합니다.
- Apply Council Title Theme를 다시 실행해도 기존 테마에서는 모서리 배치 변경 경로로 연결됩니다.
- 소재 가져오기는 Non Power of 2 = None을 유지해 이전 이미지 잘림 문제가 재발하지 않도록 합니다.

코드 컴파일은 통과했습니다. 실제 화면의 배치/버튼 반응은 Unity에서 확인해야 합니다.

## 적용

1. Unity에서 Play 모드를 종료합니다.
2. LobbyScene을 열고 저장한 뒤 컴파일이 끝날 때까지 기다립니다.
3. CROSSION > UI > Apply Council Title Theme를 누릅니다.
4. 씬을 실행해 여섯 메뉴와 마우스 반응을 확인합니다.

현재 파일과 코드는 준비됐으나 자동 Unity 실행이 종료 코드 1로 중단되어, 이 환경에서는 씬 적용/Play 검증을 완료하지 못했습니다. 위 메뉴를 직접 실행해야 합니다.

메뉴는 수정 전에 ProjectBackups/CouncilTitle/시각_고유ID/LobbyScene.unity를 백업합니다. 이미 적용된 경우 다시 배치를 덮어쓰지 않습니다.

## 변경 범위

- 타이틀의 금색 로고, 남색 금속 버튼 두 개, 여섯 아이콘.
- 싱글/멀티/규칙/피드백/종료의 기존 On Click 연결 유지.
- 타이틀에 설정 버튼 추가. 기존 GameScene의 설정 UI를 복제하며 게임 씬은 저장/변경하지 않습니다.
- 버튼 글자는 기존 한글 TMP 글꼴을 사용하는 별도 텍스트입니다.
- 마우스/키보드 선택 시 가장자리 강조와 작은 확대, 누를 때 눌림 효과.
- 배경 오브젝트/스프라이트/색상, 게임 화면과 방 만들기/대기실 디자인은 변경하지 않습니다.
- 기존 TitleText/SubtitleText와 기존 버튼 글자는 삭제하지 않고 비활성화합니다.

## Inspector에서 수정

LobbyCanvas > MainMenuPanel 아래에서 편집합니다.

- CouncilLogo: 로고 이미지와 크기. 로고 자체는 이미지이며 버튼 글자와 달리 이미지 교체가 필요합니다.
- 각 버튼 > CouncilVisual > CouncilLabel: 버튼 문구, 글꼴, 색상, 크기.
- CouncilVisual > Frame / Icon: 버튼 모양과 아이콘.
- 각 버튼의 Rect Transform: 위치와 클릭 영역.
- Council Button Feedback: Hover Scale / Pressed Scale / Response / Hover Rim.
- LobbyCanvas > TitleSettingsPanel: 복제한 설정 창. 기본 비활성 상태가 정상입니다.

시안 배경이 빠져 있으므로 기존 단색/이미지 배경 위에서 보입니다. 로고는 타이틀 메뉴와 함께 숨겨집니다. 설정 창 내부와 다른 화면의 디자인은 추후 별도 작업입니다.

## 소재와 생성 기록

기본 내장 imagegen 도구로 투명 UI 소재를 생성했습니다.

- 소재: Assets/Art/UI/Council/council-title-atlas-v1.png
- 분할 정의: Assets/Art/UI/Council/council-title-v1.councilatlas
- 원본 시안은 배경/분위기 참고용으로만 사용했습니다.
- Unity의 추가 2D Sprite 패키지 없이 분할 정의를 스프라이트로 가져옵니다. PNG를 자르거나 덮어쓰지 않습니다.

최종 생성 프롬프트:

Create a production-ready transparent PNG UI SPRITE ATLAS derived from the attached CROSSION title-menu concept. The reference is STYLE REFERENCE only: remove ALL environment/background completely. This is a flat orthographic asset sheet with isolated reusable UI pieces, no perspective, no shadows on a background, genuinely transparent negative space. Landscape canvas 1536x1024 preferred. Strict three row layout, no extraneous elements: TOP row y=0..320: one large centered antique gold metallic CROSSION wordmark with its delicate small astrolabe emblem above and thin line/jade diamond flourish below, same spelling and style as reference. Entire logo within x=80..1456 y=20..300. MIDDLE row y=360..620: one EMPTY main menu button, wide shallow-chamfer dark navy enamel plate with fine aged gold/brass layered frame and tiny turquoise diamonds at left/right tips, faithfully like reference. No icons, NO text inside button. Button fits x=80..1456 y=370..610. BOTTOM row y=700..990: SIX distinct isolated icons on one evenly spaced row with generous transparent gutters, each confined to own equal-width cell, all same consistent antique gold and navy visual style: cell1 single upright playing card with a single person's bust silhouette; cell2 fan of three ornate playing cards; cell3 open book inside a round gold-rimmed dark navy medallion; cell4 cog inside matching medallion; cell5 three-dot speech bubble inside matching medallion; cell6 exit doorway and arrow inside matching medallion. All icons centered vertically y=840 and horizontally in six cells centered x=128,384,640,896,1152,1408. Icons no wider than180px or taller than220px. No written labels anywhere except CROSSION wordmark. Do not draw checkerboard, white, black, fog, glow connecting separate pieces, texture or environment outside objects. Crisp natural alpha edges, subtle controlled metal highlights. Keep every row completely separate with transparent gaps so it can be sliced directly into Unity sprites.

실제 생성된 소재의 배치에 맞춰 분할 정의를 조정했습니다.

## 검증

- 런타임/에디터 코드 컴파일 오류 없음.
- 실제 이미지의 알파 투명도 확인.
- 대표 스프라이트의 이름이 파일명으로 바뀌어도 로고는 LoadMainAssetAtPath로 가져오며, 적용 전에 여덟 소재의 누락/중복을 검사합니다.
- 실제 아틀라스 배치에 맞춘 여덟 분할 영역 사용.
- 씬의 적용, 클릭, 설정 창과 화면 비율은 Unity에서 확인해야 합니다.
- ApplyBatch에는 배경 유지/여섯 버튼/설정 창의 외부 씬 참조 검사도 포함되어 있지만 자동 실행이 실패했으므로 통과했다고 보고하지 않습니다.

Windows 배포에는 적용 후 저장하고 재빌드해야 합니다.
