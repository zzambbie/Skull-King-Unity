# 천상강림 / 아이콘 3차 수정 기록

> 이후 움직임·빛·크기 변경은 `CROSSION_CELESTIAL_MOTION_UPDATE.md`를 확인하세요. 아래 단순 교차 페이드 설명은 이전 버전 기록입니다.

이전 방식의 문제: 145도에 가까운 화면 평면 회전은 날개를 거꾸로 뒤집어 보이게 했고, 단색 UI 메시 아이콘은 원본의 금속 질감을 잃었습니다.

이번에는 원본 UI 그림을 참조한 금빛 투명 아이콘과, 펼침→안쪽 굽힘→감싸기의 여섯 자세를 별도로 그린 소재를 사용합니다. 여섯 장은 시간 순서의 프레임이며 한 화면에 여섯 날개를 표시하지 않습니다. 두 Image에 이웃 프레임을 연결해 부드럽게 전환하며, 자세별로 평면 그림을 140도 돌리거나 음수 스케일로 뒤집지 않습니다. 3D 뼈대 시뮬레이션이 아니라 그림 자세 사이의 전환 방식입니다. 실제 재생 품질은 Unity에서 추가 확인해야 합니다.

## 적용

재생 종료 → 열린 씬 저장 → `CROSSION > Apply Unified UI Style (Once)`.
기존 적용 버전 2에서 3으로 이 두 항목만 한 번 갱신합니다. 씬 백업은 `ProjectBackups/CouncilInterface`에 저장됩니다. 게임 배경/비딩/점수판/규칙은 변경하지 않습니다.

GameScene의 SeraphMeshRig에서 Pose Frames / Pose From / Pose To와 Pose Scale을 확인할 수 있습니다. 이전 메시 레이어는 복구용으로 비활성화해 남깁니다. 기본 닫힘 시간 2.1초, 빛 유지 0.5초, 드러남 1.15초이며 입력 잠금을 유지합니다.

컴파일과 기존 순수 로직 292개 검사는 통과했습니다. 이 검사는 영상의 자연스러움을 검증하는 검사가 아니며 Unity 실제 재생은 별도로 확인해야 합니다.

## 소재와 생성 기록

내장 image_gen 사용. PNG 알파를 보존하고 Unity Sprite 영역으로만 프레임을 나눕니다. 별도 래스터 후편집은 하지 않았습니다. 아이콘은 원본 아틀라스 참조 추출 결과로 원본 픽셀과 완전히 동일한 복사본은 아닙니다.

- `Assets/Art/UI/Council/settings-gold-cutout-v3.png`
- `Assets/Art/UI/Council/exit-gold-cutout-v3.png`
- `Assets/Art/Effects/WhiteWhale/seraph-embrace-poses-v4.png`
- Sprite 영역: `Assets/Art/Effects/WhiteWhale/seraph-embrace-poses-v4.councilatlas`

### 설정 아이콘 프롬프트

```text
Use case: background-extraction. Edit target: the attached CROSSION UI atlas. Extract ONLY the existing small beveled gold settings cog at approximately (886,887). Preserve its exact original painterly metallic gold texture, beveled edge, central circular hole and silhouette; this is NOT a redesign, do not simplify it into a flat vector icon. Remove everything else including navy disc, circular medallion border, adjacent artwork and any square crop/background. Deliver one centered isolated identical gold cog with clean genuine transparent alpha, including transparent hole, generous transparent padding, no frame, no shadow or glow outside the cog, no text. Square image.
```

### 종료 아이콘 프롬프트

```text
Use case: background-extraction. Edit target: attached CROSSION UI atlas. Extract ONLY the original small metallic gold exit symbol at approximately (1407,887): the open golden door with diamond-shaped keyhole and rightward outlined gold arrow. Keep its exact painted beveled metallic gold surface, highlights, original proportions and detailed original drawing style. NOT a redesign, NOT a flat vector. Remove everything else including navy disc, medallion ring, adjacent objects, background and rectangular crop artifacts. One isolated unchanged original door-and-arrow centered on genuine transparent background with clean alpha around every edge and inside the open arrow where appropriate, generous transparent padding. No circular border, no badge, no new outline box, no text, no external glow or shadow. Square image.
```

### 날개 자세 프롬프트

```text
Use case: stylized-concept.
Asset type: production animation sprite sheet, 6 sequential keyframes of ONE PAIR of angel wings gently embracing a playing card.
Input image 1: CROSSION Heaven card, exact illustration/style reference for ivory/gold feathers. Input image 2: existing single wing, feather rendering reference only, NOT a pose to rotate.
Canvas: 1536x1024, EXACT 3 columns x 2 rows of equal 512x512 cells, no borders, no labels. True transparent background everywhere. Each cell is a fixed-camera animation frame, chronological left to right top then bottom.
In EVERY cell the invisible card would occupy the same central upright rectangle x=40%..60%, y=25%..78% of that cell. DO NOT DRAW the card or a person. The pair's shoulders stay at x=36% and x=64%, y=37% in every cell. Exactly TWO wings in each frame. Keep overall outer silhouette within 7%..93% cell margins.
Frame 1: wings fully spread elegantly upward and outward from the card, feather tips pointing upward/outward; clear empty central space.
Frame 2: anatomically bent elbow joints begin moving forward toward viewer around the card sides; feathers remain correctly upright.
Frame 3: wings cup around front of card, curved arms forming a protective bowl; down-pointing long feathers beginning to overlap card's bottom corners.
Frame 4: primary feathers naturally curl inward to cover most of front, rounded layered feather mass, soft embrace, a narrow central opening.
Frame 5: gently overlapping ivory feathers enclose the card fully, left wing slightly in front of right; an organic teardrop cocoon of folded feathers.
Frame 6: same final closed embrace settling, no new wings, no new pose direction.
Critical animation constraints: constant camera, shoulder locations, feather design, light and scale across cells. Animate proper shoulder/elbow folding IN DEPTH like real swan wings protecting a chick. DO NOT rotate the whole flat wing upside down, DO NOT twist feathers into a cylinder, DO NOT flip mirror directions between frames, DO NOT draw heraldic fans or 6 wings.
Style: beautifully painted high-end anime fantasy key animation, soft thick ivory feathers with subtle fine champagne gold along shafts, elegant broad rounded feather layers, natural volume and delicate shadows. Match Heaven card. No bright external glow, no objects, no text, no grids, no checkerboard. Clean transparent alpha.
```

