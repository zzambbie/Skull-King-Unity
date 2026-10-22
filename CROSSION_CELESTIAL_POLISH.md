# 천상강림 날개 / 타이틀 보조 아이콘 갱신

> 최신 변경은 `CROSSION_CELESTIAL_POSES_V4.md`를 확인하세요. 아래 메시 회전/단색 아이콘 방식은 이전 버전 기록이며 새 적용에서는 비활성화됩니다.

## 2차 수정: 두 날개 포개기

최신 적용은 좌우 각각 **한 장, 총 두 장**만 표시합니다. 기존 6개 레이어 연결은 씬 호환을 위해 남기되 4개는 끕니다. 깃털 구간마다 145도까지 뒤집던 메시 변형을 제거해 원기둥처럼 꼬이는 동작을 없앴습니다. 두 날개는 카드 양쪽에서 펼쳐진 다음 전체 형태를 유지하면서 대각선으로 포개집니다. 오른쪽은 약간 늦게 따라오며, 중앙 빛과 진행 잠금은 유지합니다. 실제 인상은 Unity 재생으로 확인해야 합니다.

설정/종료는 아틀라스 사각 크롭 대신 배경 없는 금빛 메시 도형을 사용합니다. 기존 Image는 비활성화하고 Icon/CleanGlyph 자식에 도형을 배치합니다. 버튼 크기/위치/기능은 바꾸지 않습니다. 적용 기록 버전은 각각 2이며, 아래 적용 메뉴를 다시 한 번 실행하면 기존 버전 1 씬을 갱신합니다.

## 적용

1. Unity에서 재생을 종료하고 열려 있는 씬을 저장합니다.
2. `CROSSION > Apply Unified UI Style (Once)`를 실행합니다.
3. 적용 안내를 확인합니다. 씬 백업은 `ProjectBackups/CouncilInterface`에 저장됩니다.

이미 테마가 적용된 프로젝트에서는 이번 날개와 아이콘을 한 번만 갱신합니다. 게임 화면 배경/비딩/점수판/중앙판 재디자인과 규칙 수정은 이번 범위에서 제외했습니다. 기존 버튼 루트의 크기, 배치, 클릭 연결을 유지합니다.

## 연출

- 천사 카드가 먼저 제출되고 중앙에 도착한 다음 연출합니다.
- 새 단일 날개 소재를 좌우 한 장씩 배치합니다. 카드 옆에 뿌리를 두고 펼침 → 포개기 → 펼쳐지며 소멸을 표현합니다.
- 깃털 끝의 지연과 약한 펄럭임을 더했습니다. 사각 흰 덮개는 투명하게 유지합니다.
- 중앙에서 가장 밝고 가장자리로 부드럽게 사라지는 타원형 빛과 실제 카드 알파 전환으로 선두 교체를 가립니다.
- 기본 시간: 펼침/감싸기 1.85초, 빛 유지 0.5초, 드러남 1.15초. 기존 진행 잠금을 유지합니다.
- 씬의 WhaleWingTransition에서 시간, SeraphMeshRig에서 Span / Root Offset / Feather Lag를 조절할 수 있습니다. 레거시 좌우 Image는 호환용으로 남기되 비활성화합니다.

설정/종료 아이콘은 배경이 없는 톱니/문 모양의 UI 메시입니다. 기존 타이틀 아틀라스 크롭은 호환용으로 남겨두고 더 이상 이 두 버튼에 표시하지 않습니다.

## 확인

코드 컴파일과 기존 순수 로직 검사 292개를 통과했습니다. 실제 Unity 재생/화질은 이 환경에서 확인하지 못했습니다. 적용 후 천사 제출 → 깃털 펼침/감싸기 → 선두 교체 → 입력 복귀를 확인해 주세요. 연출 도중 타이틀 복귀 시 카드 투명도도 원복합니다.

## 생성 소재 기록

- 도구: 내장 image_gen, 투명 배경 생성
- 참조: `Assets/Art/Cards/CROSSION/Composed/Heaven_card.png`
- 프로젝트 소재: `Assets/Art/Effects/WhiteWhale/seraph-embrace-single-wing-v3.png` (1536×1024)
- 원본 결과: `C:/Users/USER/.codex/generated_images/01a07260-561c-72f3-afe0-f90602cf9714/exec-32904660-fbc4-4993-a6c2-1df13c91a1d2.png`
- 아래 프롬프트로 생성 후 프로젝트로 복사했습니다. 별도 래스터 후편집은 하지 않았습니다.

```text
Use case: stylized-concept. Asset type: isolated 2D skeletal animation wing sprite for a premium anime card-game cinematic. Input image 1 is STYLE REFERENCE: the CROSSION Heaven card with ivory white angel wings and fine warm gold accents. Create ONE anatomically graceful LEFT angel wing, not a whole angel or three-wing fan. The root/shoulder must be at the FAR RIGHT at approximately x=91%, y=52%; wing extends to the LEFT, arching gently upward and curving down at the tips like a soft protective embrace. Broad soft layered coverts near root and elegantly separated long flight feathers toward the left tips; sumptuous soft rounded volume, pearlescent ivory feathers, restrained fine pale gold rachis and warm light between feather layers. High-end Japanese fantasy anime film key animation / richly painted fantasy illustration, delicate controlled feather detail, gentle ambient occlusion to make feather layers readable at small size. NOT a metallic badge, NOT blade-shaped sharp spines, NOT rigid heraldic emblem, NOT a feather clipart. Neutral fully spread resting pose, broad open silhouette suitable for later bending and flapping with a mesh rig. Keep the entire wing inside frame with transparent padding. Landscape 1536x1024 composition. Truly transparent background, clean alpha, no halos or glow baked outside feathers, no black/white/checkerboard background, no stars, no detached feathers, no text, no border, no other objects. The image will be mirrored for the right side; do not draw two wings.
```
