// ============================================================
// SceneSetupTool.cs — Unity 에디터 자동 씬/UI 생성 도구
// 메뉴: SkullKing > 전체 씬 자동 생성
// ============================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.InputSystem.UI;

namespace SkullKing.Editor
{
    public static class SceneSetupTool
    {
        // 색상 팔레트
        private static readonly Color TableColor = new Color(0.08f, 0.18f, 0.12f);       // 어두운 녹색 테이블
        private static readonly Color PanelColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);  // 반투명 어두운 패널
        private static readonly Color AccentColor = new Color(0.9f, 0.72f, 0.2f);         // 골드 악센트
        private static readonly Color TextColor = Color.white;
        private static readonly Color ButtonColor = new Color(0.2f, 0.5f, 0.3f);
        private static readonly Color ButtonTextColor = Color.white;
        private static readonly Color CardBackColor = new Color(0.15f, 0.1f, 0.3f);

        // ============================================================
        // 메인 메뉴
        // ============================================================

        [MenuItem("SkullKing/1. 카드 프리팹 생성")]
        public static void CreateCardPrefab()
        {
            CreateCardPrefabAsset();
            Debug.Log("★ 카드 프리팹 생성 완료! Assets/Prefabs/CardPrefab.prefab");
        }

        [MenuItem("SkullKing/2. 로비 씬 생성")]
        public static void SetupLobbyScene()
        {
            BuildLobbyScene();
            Debug.Log("★ 로비 씬 생성 완료! Assets/Scenes/LobbyScene.unity");
        }

        [MenuItem("SkullKing/3. 게임 씬 생성")]
        public static void SetupGameScene()
        {
            // 카드 프리팹이 있는지 확인
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardPrefab.prefab");
            if (cardPrefab == null)
            {
                if (EditorUtility.DisplayDialog("카드 프리팹 필요",
                    "카드 프리팹이 아직 없습니다. 먼저 생성할까요?", "생성", "취소"))
                {
                    CreateCardPrefabAsset();
                    cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardPrefab.prefab");
                }
                else return;
            }
            BuildGameScene(cardPrefab);
            Debug.Log("★ 게임 씬 생성 완료! Assets/Scenes/GameScene.unity");
        }

        [MenuItem("SkullKing/4. Build Settings에 씬 등록")]
        public static void RegisterScenesToBuild()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/LobbyScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("★ Build Settings에 LobbyScene(0), GameScene(1) 등록 완료!");
        }

        [MenuItem("SkullKing/★ 전체 자동 세팅 (1~4 한번에)")]
        public static void SetupAll()
        {
            CreateCardPrefabAsset();
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardPrefab.prefab");
            BuildLobbyScene();
            BuildGameScene(cardPrefab);
            RegisterScenesToBuild();

            // 로비 씬을 열어주기
            EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");

            EditorUtility.DisplayDialog("세팅 완료!",
                "모든 씬과 프리팹이 생성되었습니다.\n\n" +
                "▶ LobbyScene이 열려 있습니다.\n" +
                "▶ Play 버튼을 눌러 게임을 테스트하세요!\n\n" +
                "(GameScene은 디버그 모드가 켜져 있어\n 로비 없이도 직접 테스트 가능합니다)",
                "확인");
        }

        // ============================================================
        // 카드 프리팹 생성
        // ============================================================

        private static void CreateCardPrefabAsset()
        {
            EnsureDirectory("Assets/Prefabs");

            // 루트 오브젝트 (카드)
            var cardRoot = new GameObject("CardPrefab");
            var cardRect = cardRoot.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(120, 170);

            // 카드에 LayoutElement 추가 (레이아웃 그룹 내에서 크기 유지)
            var layoutElem = cardRoot.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = 120;
            layoutElem.preferredHeight = 170;
            layoutElem.minWidth = 80;
            layoutElem.minHeight = 110;

            // 배경 이미지
            var bgImage = cardRoot.AddComponent<Image>();
            bgImage.color = Color.white;

            // 버튼 (클릭 가능)
            var button = cardRoot.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 0.7f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.5f);
            button.colors = colors;

            // 카드 내부 배경 (약간 안쪽에 실제 색상 표시)
            var innerBg = CreateUIChild(cardRoot, "InnerBackground", new Vector2(110, 160));
            var innerBgImg = innerBg.AddComponent<Image>();
            innerBgImg.color = new Color(0.3f, 0.7f, 0.3f); // 기본 초록

            // 카드 이름 텍스트 (상단)
            var nameObj = CreateUIChild(cardRoot, "NameText", Vector2.zero);
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.7f);
            nameRect.anchorMax = new Vector2(1, 0.95f);
            nameRect.offsetMin = new Vector2(5, 0);
            nameRect.offsetMax = new Vector2(-5, 0);
            var nameText = nameObj.AddComponent<Text>();
            nameText.text = "카드 이름";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // 카드 숫자 텍스트 (중앙 대형)
            var numObj = CreateUIChild(cardRoot, "NumberText", Vector2.zero);
            var numRect = numObj.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0, 0.2f);
            numRect.anchorMax = new Vector2(1, 0.75f);
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;
            var numText = numObj.AddComponent<Text>();
            numText.text = "7";
            numText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            numText.fontSize = 48;
            numText.fontStyle = FontStyle.Bold;
            numText.alignment = TextAnchor.MiddleCenter;
            numText.color = Color.white;
            numText.raycastTarget = false;

            // 일러스트 이미지 (나중에 사용, 초기엔 비활성)
            var illObj = CreateUIChild(cardRoot, "IllustrationImage", Vector2.zero);
            var illRect = illObj.GetComponent<RectTransform>();
            illRect.anchorMin = new Vector2(0.1f, 0.15f);
            illRect.anchorMax = new Vector2(0.9f, 0.7f);
            illRect.offsetMin = Vector2.zero;
            illRect.offsetMax = Vector2.zero;
            var illImg = illObj.AddComponent<Image>();
            illImg.color = new Color(1, 1, 1, 0.3f);
            illObj.SetActive(false); // 초기 비활성

            // 소유자 라벨 (트릭 영역에서 사용, 초기 비활성)
            var ownerObj = CreateUIChild(cardRoot, "OwnerLabel", Vector2.zero);
            var ownerRect = ownerObj.GetComponent<RectTransform>();
            ownerRect.anchorMin = new Vector2(0, 0);
            ownerRect.anchorMax = new Vector2(1, 0.15f);
            ownerRect.offsetMin = new Vector2(2, 2);
            ownerRect.offsetMax = new Vector2(-2, 0);
            var ownerText = ownerObj.AddComponent<Text>();
            ownerText.text = "";
            ownerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ownerText.fontSize = 11;
            ownerText.alignment = TextAnchor.MiddleCenter;
            ownerText.color = new Color(1, 1, 0.7f);
            ownerText.raycastTarget = false;
            ownerObj.SetActive(false);

            // 선택 불가 오버레이 (어둡게)
            var overlayObj = CreateUIChild(cardRoot, "DisabledOverlay", Vector2.zero);
            var overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.5f);
            overlayImg.raycastTarget = false;
            overlayObj.SetActive(false);

            // CardUI 컴포넌트 부착 및 참조 연결
            var cardUI = cardRoot.AddComponent<SkullKing.UI.CardUI>();

            // SerializedObject로 참조 연결
            var so = new SerializedObject(cardUI);
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_numberText").objectReferenceValue = numText;
            so.FindProperty("_backgroundImage").objectReferenceValue = innerBgImg;
            so.FindProperty("_illustrationImage").objectReferenceValue = illImg;
            so.FindProperty("_ownerLabel").objectReferenceValue = ownerText;
            so.FindProperty("_disabledOverlay").objectReferenceValue = overlayObj;
            so.FindProperty("_button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 프리팹 저장
            string path = "Assets/Prefabs/CardPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(cardRoot, path);
            Object.DestroyImmediate(cardRoot);

            AssetDatabase.Refresh();
        }

        // ============================================================
        // 로비 씬 생성
        // ============================================================

        private static void BuildLobbyScene()
        {
            EnsureDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 카메라 배경색
            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null) cam.backgroundColor = new Color(0.05f, 0.08f, 0.12f);

            // ---- Canvas ----
            var canvasObj = CreateCanvas("LobbyCanvas");

            // 배경
            var bg = CreateUIChild(canvasObj, "Background", Vector2.zero);
            var bgRect = StretchFull(bg);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = TableColor;

            // 타이틀
            var titleObj = CreateUIChild(canvasObj, "TitleText", Vector2.zero);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.65f);
            titleRect.anchorMax = new Vector2(0.8f, 0.85f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "SKULL KING";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = AccentColor;

            // 부제목
            var subObj = CreateUIChild(canvasObj, "SubtitleText", Vector2.zero);
            var subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.2f, 0.58f);
            subRect.anchorMax = new Vector2(0.8f, 0.65f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            var subText = subObj.AddComponent<Text>();
            subText.text = "— Card Game —";
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = 24;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.7f, 0.7f, 0.7f);

            // 플레이어 수 표시 패널
            var countPanel = CreatePanel(canvasObj, "PlayerCountPanel",
                new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.55f));

            // "플레이어 수" 라벨
            var labelObj = CreateUIChild(countPanel, "Label", Vector2.zero);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelObj.AddComponent<Text>();
            labelText.text = "플레이어 수 (본인 포함)";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 22;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = TextColor;

            // - 버튼
            var minusBtn = CreateButton(countPanel, "DecreaseButton", "◀",
                new Vector2(0.15f, 0.05f), new Vector2(0.3f, 0.5f), 36);

            // 숫자 텍스트
            var countObj = CreateUIChild(countPanel, "CountText", Vector2.zero);
            var countRect = countObj.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.3f, 0.05f);
            countRect.anchorMax = new Vector2(0.7f, 0.5f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            var countText = countObj.AddComponent<Text>();
            countText.text = "4명";
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 40;
            countText.fontStyle = FontStyle.Bold;
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = AccentColor;

            // + 버튼
            var plusBtn = CreateButton(countPanel, "IncreaseButton", "▶",
                new Vector2(0.7f, 0.05f), new Vector2(0.85f, 0.5f), 36);

            // 시작 버튼
            var startBtn = CreateButton(canvasObj, "StartButton", "게 임  시 작",
                new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.28f), 36);
            var startBtnImg = startBtn.GetComponent<Image>();
            startBtnImg.color = new Color(0.8f, 0.2f, 0.15f); // 빨간색 시작 버튼

            // ---- LobbyManager 부착 ----
            var lobbyMgrObj = new GameObject("LobbyManager");
            var lobbyMgr = lobbyMgrObj.AddComponent<SkullKing.Core.LobbyManager>();

            var lso = new SerializedObject(lobbyMgr);
            lso.FindProperty("_playerCountText").objectReferenceValue = countText;
            lso.FindProperty("_gameSceneName").stringValue = "GameScene";
            lso.ApplyModifiedPropertiesWithoutUndo();

            // 버튼 이벤트 연결
            ConnectButtonOnClick(minusBtn, lobbyMgr, "DecreasePlayerCount");
            ConnectButtonOnClick(plusBtn, lobbyMgr, "IncreasePlayerCount");
            ConnectButtonOnClick(startBtn, lobbyMgr, "StartGame");

            // 씬 저장
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LobbyScene.unity");
        }

        // ============================================================
        // 게임 씬 생성 (UNO 스타일 레이아웃)
        // ============================================================

        private static void BuildGameScene(GameObject cardPrefab)
        {
            EnsureDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null) cam.backgroundColor = new Color(0.03f, 0.06f, 0.1f);

            // ---- 메인 Canvas ----
            var canvasObj = CreateCanvas("GameCanvas");

            // 테이블 배경
            var tableBg = CreateUIChild(canvasObj, "TableBackground", Vector2.zero);
            StretchFull(tableBg);
            var tableBgImg = tableBg.AddComponent<Image>();
            tableBgImg.color = TableColor;

            // 중앙 원형 느낌의 플레이 영역 배경
            var centerBg = CreateUIChild(canvasObj, "CenterPlayAreaBg", Vector2.zero);
            var centerBgRect = centerBg.GetComponent<RectTransform>();
            centerBgRect.anchorMin = new Vector2(0.25f, 0.25f);
            centerBgRect.anchorMax = new Vector2(0.75f, 0.72f);
            centerBgRect.offsetMin = Vector2.zero;
            centerBgRect.offsetMax = Vector2.zero;
            var centerBgImg = centerBg.AddComponent<Image>();
            centerBgImg.color = new Color(0.06f, 0.15f, 0.1f, 0.8f);

            // ═══════════════════════════════════════════
            // 상단 정보 바 (라운드, 페이즈)
            // ═══════════════════════════════════════════
            var topBar = CreatePanel(canvasObj, "TopInfoBar",
                new Vector2(0.25f, 0.93f), new Vector2(0.75f, 1f));
            var topBarImg = topBar.GetComponent<Image>();
            topBarImg.color = new Color(0, 0, 0, 0.6f);

            var phaseTextObj = CreateUIChild(topBar, "PhaseText", Vector2.zero);
            var phaseRect = phaseTextObj.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0, 0);
            phaseRect.anchorMax = new Vector2(0.5f, 1);
            phaseRect.offsetMin = new Vector2(15, 0);
            phaseRect.offsetMax = Vector2.zero;
            var phaseText = phaseTextObj.AddComponent<Text>();
            phaseText.text = "대기 중...";
            phaseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phaseText.fontSize = 22;
            phaseText.alignment = TextAnchor.MiddleLeft;
            phaseText.color = AccentColor;

            var roundTextObj = CreateUIChild(topBar, "RoundInfoText", Vector2.zero);
            var roundRect = roundTextObj.GetComponent<RectTransform>();
            roundRect.anchorMin = new Vector2(0.5f, 0);
            roundRect.anchorMax = new Vector2(1, 1);
            roundRect.offsetMin = Vector2.zero;
            roundRect.offsetMax = new Vector2(-15, 0);
            var roundText = roundTextObj.AddComponent<Text>();
            roundText.text = "";
            roundText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundText.fontSize = 22;
            roundText.alignment = TextAnchor.MiddleRight;
            roundText.color = TextColor;

            // ═══════════════════════════════════════════
            // AI 플레이어 패널들 (UNO 스타일 배치)
            // ═══════════════════════════════════════════

            // 0번: 인간 (하단) → 별도 처리
            // 1번: AI - 왼쪽
            // 2번: AI - 상단
            // 3번: AI - 오른쪽
            // 4번: AI - 좌상단 (5인 이상)
            // 5번: AI - 우상단 (6인)

            Text[] playerInfoTexts = new Text[6];

            // 인간 플레이어 정보 (좌하단)
            playerInfoTexts[0] = CreatePlayerInfoPanel(canvasObj, "PlayerInfo_0",
                new Vector2(0.01f, 0.01f), new Vector2(0.18f, 0.13f), "Player");

            // AI 1 - 왼쪽
            playerInfoTexts[1] = CreatePlayerInfoPanel(canvasObj, "PlayerInfo_1",
                new Vector2(0.01f, 0.55f), new Vector2(0.15f, 0.72f), "AI 1");

            // AI 2 - 상단 중앙
            playerInfoTexts[2] = CreatePlayerInfoPanel(canvasObj, "PlayerInfo_2",
                new Vector2(0.38f, 0.83f), new Vector2(0.62f, 0.93f), "AI 2");

            // AI 3 - 오른쪽
            playerInfoTexts[3] = CreatePlayerInfoPanel(canvasObj, "PlayerInfo_3",
                new Vector2(0.85f, 0.55f), new Vector2(0.99f, 0.72f), "AI 3");

            // AI 4 - 좌상단 (5인용)
            playerInfoTexts[4] = CreatePlayerInfoPanel(canvasObj, "PlayerInfo_4",
                new Vector2(0.01f, 0.78f), new Vector2(0.18f, 0.92f), "AI 4");

            // AI 5 - 우상단 (6인용)
            playerInfoTexts[5] = CreatePlayerInfoPanel(canvasObj, "PlayerInfo_5",
                new Vector2(0.82f, 0.78f), new Vector2(0.99f, 0.92f), "AI 5");

            // ═══════════════════════════════════════════
            // 중앙 트릭 영역 (낸 카드 표시)
            // ═══════════════════════════════════════════
            var trickArea = CreateUIChild(canvasObj, "TrickArea", Vector2.zero);
            var trickRect = trickArea.GetComponent<RectTransform>();
            trickRect.anchorMin = new Vector2(0.28f, 0.38f);
            trickRect.anchorMax = new Vector2(0.72f, 0.62f);
            trickRect.offsetMin = Vector2.zero;
            trickRect.offsetMax = Vector2.zero;
            // LayoutGroup 없음 — UIBindingManager가 코드로 카드 위치를 직접 계산

            // 트릭 결과 텍스트 (트릭 영역 위)
            var trickResultObj = CreateUIChild(canvasObj, "TrickResultText", Vector2.zero);
            var trickResultRect = trickResultObj.GetComponent<RectTransform>();
            trickResultRect.anchorMin = new Vector2(0.25f, 0.63f);
            trickResultRect.anchorMax = new Vector2(0.75f, 0.72f);
            trickResultRect.offsetMin = Vector2.zero;
            trickResultRect.offsetMax = Vector2.zero;
            var trickResultText = trickResultObj.AddComponent<Text>();
            trickResultText.text = "";
            trickResultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            trickResultText.fontSize = 24;
            trickResultText.fontStyle = FontStyle.Bold;
            trickResultText.alignment = TextAnchor.MiddleCenter;
            trickResultText.color = AccentColor;

            // ═══════════════════════════════════════════
            // 하단 플레이어 손패 영역
            // ═══════════════════════════════════════════
            var handArea = CreateUIChild(canvasObj, "PlayerHandArea", Vector2.zero);
            var handRect = handArea.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.1f, 0.01f);
            handRect.anchorMax = new Vector2(0.9f, 0.23f);
            handRect.offsetMin = Vector2.zero;
            handRect.offsetMax = Vector2.zero;
            var handLayout = handArea.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = -20; // 카드 겹침 (UNO 스타일)
            handLayout.childAlignment = TextAnchor.MiddleCenter;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;
            handLayout.childControlWidth = false;
            handLayout.childControlHeight = false;
            handLayout.padding = new RectOffset(20, 20, 5, 5);

            // 카드가 영역 안에서 자동 정렬됨 (앵커 기반)

            // 손패 배경
            var handBg = handArea.AddComponent<Image>();
            handBg.color = new Color(0, 0, 0, 0.3f);
            handBg.raycastTarget = false;

            // ═══════════════════════════════════════════
            // 비딩 패널 (중앙 팝업)
            // ═══════════════════════════════════════════
            var bidPanel = CreatePanel(canvasObj, "BidPanel",
                new Vector2(0.32f, 0.35f), new Vector2(0.68f, 0.65f));
            var bidPanelImg = bidPanel.GetComponent<Image>();
            bidPanelImg.color = new Color(0.12f, 0.12f, 0.2f, 0.95f);

            // 비딩 타이틀
            var bidTitle = CreateUIChild(bidPanel, "BidTitle", Vector2.zero);
            var bidTitleRect = bidTitle.GetComponent<RectTransform>();
            bidTitleRect.anchorMin = new Vector2(0, 0.7f);
            bidTitleRect.anchorMax = new Vector2(1, 0.95f);
            bidTitleRect.offsetMin = Vector2.zero;
            bidTitleRect.offsetMax = Vector2.zero;
            var bidTitleText = bidTitle.AddComponent<Text>();
            bidTitleText.text = "이번 라운드 예측 승리 수";
            bidTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bidTitleText.fontSize = 24;
            bidTitleText.alignment = TextAnchor.MiddleCenter;
            bidTitleText.color = TextColor;

            // 비딩 - 버튼
            var bidDown = CreateButton(bidPanel, "BidDownButton", "◀",
                new Vector2(0.15f, 0.3f), new Vector2(0.3f, 0.65f), 40);

            // 비딩 수 텍스트
            var bidValueObj = CreateUIChild(bidPanel, "BidValueText", Vector2.zero);
            var bidValueRect = bidValueObj.GetComponent<RectTransform>();
            bidValueRect.anchorMin = new Vector2(0.3f, 0.3f);
            bidValueRect.anchorMax = new Vector2(0.7f, 0.65f);
            bidValueRect.offsetMin = Vector2.zero;
            bidValueRect.offsetMax = Vector2.zero;
            var bidValueText = bidValueObj.AddComponent<Text>();
            bidValueText.text = "0";
            bidValueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bidValueText.fontSize = 60;
            bidValueText.fontStyle = FontStyle.Bold;
            bidValueText.alignment = TextAnchor.MiddleCenter;
            bidValueText.color = AccentColor;

            // 비딩 + 버튼
            var bidUp = CreateButton(bidPanel, "BidUpButton", "▶",
                new Vector2(0.7f, 0.3f), new Vector2(0.85f, 0.65f), 40);

            // 비딩 확정 버튼
            var bidConfirm = CreateButton(bidPanel, "BidConfirmButton", "확  정",
                new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.25f), 28);
            bidConfirm.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.15f);

            bidPanel.SetActive(false); // 초기 비활성

            // ═══════════════════════════════════════════
            // 타이그리스 선택 패널
            // ═══════════════════════════════════════════
            var tigressPanel = CreatePanel(canvasObj, "TigressPanel",
                new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.6f));
            tigressPanel.GetComponent<Image>().color = new Color(0.15f, 0.1f, 0.2f, 0.95f);

            var tigressTitle = CreateUIChild(tigressPanel, "Title", Vector2.zero);
            var tigressTitleRect = tigressTitle.GetComponent<RectTransform>();
            tigressTitleRect.anchorMin = new Vector2(0, 0.6f);
            tigressTitleRect.anchorMax = new Vector2(1, 0.95f);
            tigressTitleRect.offsetMin = Vector2.zero;
            tigressTitleRect.offsetMax = Vector2.zero;
            var tigressTitleText = tigressTitle.AddComponent<Text>();
            tigressTitleText.text = "타이그리스 모드 선택";
            tigressTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tigressTitleText.fontSize = 24;
            tigressTitleText.alignment = TextAnchor.MiddleCenter;
            tigressTitleText.color = TextColor;

            var tigressPirateBtn = CreateButton(tigressPanel, "TigressPirateButton", "🏴 해적",
                new Vector2(0.05f, 0.1f), new Vector2(0.48f, 0.55f), 24);
            tigressPirateBtn.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f);

            var tigressEscapeBtn = CreateButton(tigressPanel, "TigressEscapeButton", "🏳 도주",
                new Vector2(0.52f, 0.1f), new Vector2(0.95f, 0.55f), 24);
            tigressEscapeBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);

            tigressPanel.SetActive(false);

            // ═══════════════════════════════════════════
            // 라운드 결과 패널
            // ═══════════════════════════════════════════
            var roundResultPanel = CreatePanel(canvasObj, "RoundResultPanel",
                new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.95f));
            roundResultPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f, 0.95f);

            var roundResultObj = CreateUIChild(roundResultPanel, "RoundResultText", Vector2.zero);
            StretchFull(roundResultObj);
            var rrt = roundResultObj.GetComponent<RectTransform>();
            rrt.offsetMin = new Vector2(20, 20);
            rrt.offsetMax = new Vector2(-20, -20);
            var roundResultText = roundResultObj.AddComponent<Text>();
            roundResultText.text = "";
            roundResultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundResultText.fontSize = 18;
            roundResultText.alignment = TextAnchor.UpperCenter;
            roundResultText.color = TextColor;

            var roundResultNextBtn = CreateButton(roundResultPanel, "NextButton", "다  음",
                new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.15f), 24);

            roundResultPanel.SetActive(false);

            // ═══════════════════════════════════════════
            // 게임 종료 패널
            // ═══════════════════════════════════════════
            var gameEndPanel = CreatePanel(canvasObj, "GameEndPanel",
                new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f));
            gameEndPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.97f);

            var gameEndObj = CreateUIChild(gameEndPanel, "GameEndText", Vector2.zero);
            StretchFull(gameEndObj);
            var ger = gameEndObj.GetComponent<RectTransform>();
            ger.offsetMin = new Vector2(30, 30);
            ger.offsetMax = new Vector2(-30, -30);
            var gameEndText = gameEndObj.AddComponent<Text>();
            gameEndText.text = "";
            gameEndText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gameEndText.fontSize = 28;
            gameEndText.alignment = TextAnchor.MiddleCenter;
            gameEndText.color = AccentColor;

            gameEndPanel.SetActive(false);

            // ═══════════════════════════════════════════
            // 매니저 오브젝트들 생성 및 연결
            // ═══════════════════════════════════════════

            // GameManager
            var gmObj = new GameObject("GameManager");
            var gm = gmObj.AddComponent<SkullKing.Core.GameManager>();

            // GameInitializer (디버그 모드 기본 ON)
            var giObj = new GameObject("GameInitializer");
            var gi = giObj.AddComponent<SkullKing.Core.GameInitializer>();
            var giSo = new SerializedObject(gi);
            giSo.FindProperty("_gameManager").objectReferenceValue = gm;
            giSo.FindProperty("_debugDirectStart").boolValue = true;
            giSo.FindProperty("_debugPlayerCount").intValue = 4;
            giSo.ApplyModifiedPropertiesWithoutUndo();

            // UIBindingManager
            var uiObj = new GameObject("UIBindingManager");
            var uiBind = uiObj.AddComponent<SkullKing.UI.UIBindingManager>();

            var uiSo = new SerializedObject(uiBind);
            uiSo.FindProperty("_gameManager").objectReferenceValue = gm;
            uiSo.FindProperty("_phaseText").objectReferenceValue = phaseText;
            uiSo.FindProperty("_roundInfoText").objectReferenceValue = roundText;
            uiSo.FindProperty("_bidPanel").objectReferenceValue = bidPanel;
            uiSo.FindProperty("_bidValueText").objectReferenceValue = bidValueText;
            uiSo.FindProperty("_bidUpButton").objectReferenceValue = bidUp.GetComponent<Button>();
            uiSo.FindProperty("_bidDownButton").objectReferenceValue = bidDown.GetComponent<Button>();
            uiSo.FindProperty("_bidConfirmButton").objectReferenceValue = bidConfirm.GetComponent<Button>();
            uiSo.FindProperty("_tigressPanel").objectReferenceValue = tigressPanel;
            uiSo.FindProperty("_tigressPirateButton").objectReferenceValue = tigressPirateBtn.GetComponent<Button>();
            uiSo.FindProperty("_tigressEscapeButton").objectReferenceValue = tigressEscapeBtn.GetComponent<Button>();
            uiSo.FindProperty("_playerHandArea").objectReferenceValue = handArea.transform;
            uiSo.FindProperty("_trickArea").objectReferenceValue = trickArea.transform;
            uiSo.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
            uiSo.FindProperty("_trickResultText").objectReferenceValue = trickResultText;
            uiSo.FindProperty("_roundResultPanel").objectReferenceValue = roundResultPanel;
            uiSo.FindProperty("_roundResultText").objectReferenceValue = roundResultText;
            uiSo.FindProperty("_roundResultNextButton").objectReferenceValue = roundResultNextBtn.GetComponent<Button>();
            uiSo.FindProperty("_gameEndPanel").objectReferenceValue = gameEndPanel;
            uiSo.FindProperty("_gameEndText").objectReferenceValue = gameEndText;

            // 플레이어 정보 텍스트 배열 연결
            var playerInfoProp = uiSo.FindProperty("_playerInfoTexts");
            playerInfoProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                playerInfoProp.GetArrayElementAtIndex(i).objectReferenceValue = playerInfoTexts[i];
            }

            uiSo.ApplyModifiedPropertiesWithoutUndo();

            // 씬 저장
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        }

        // ============================================================
        // 유틸리티 헬퍼 메서드들
        // ============================================================

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasObj = new GameObject(name);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem (없으면 생성) — 새 Input System 사용
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }

            return canvasObj;
        }

        private static GameObject CreateUIChild(GameObject parent, string name, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            var rect = obj.AddComponent<RectTransform>();
            if (size != Vector2.zero)
                rect.sizeDelta = size;
            return obj;
        }

        private static RectTransform StretchFull(GameObject obj)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static GameObject CreatePanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = CreateUIChild(parent, name, Vector2.zero);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = PanelColor;
            return panel;
        }

        private static GameObject CreateButton(GameObject parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            var btnObj = CreateUIChild(parent, name, Vector2.zero);
            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = ButtonColor;

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.65f, 0.4f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.2f);
            btn.colors = colors;

            // 텍스트 자식
            var textObj = CreateUIChild(btnObj, "Text", Vector2.zero);
            StretchFull(textObj);
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = ButtonTextColor;

            return btnObj;
        }

        private static Text CreatePlayerInfoPanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string defaultText)
        {
            var panel = CreatePanel(parent, name, anchorMin, anchorMax);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            var textObj = CreateUIChild(panel, "InfoText", Vector2.zero);
            StretchFull(textObj);
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.offsetMin = new Vector2(5, 3);
            tRect.offsetMax = new Vector2(-5, -3);

            var text = textObj.AddComponent<Text>();
            text.text = defaultText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private static void ConnectButtonOnClick(GameObject buttonObj, MonoBehaviour target, string methodName)
        {
            var button = buttonObj.GetComponent<Button>();
            if (button == null) return;

            // SerializedProperty로 직접 연결

            // Reflection-free approach: use SerializedObject
            var so = new SerializedObject(button);
            var onClickProp = so.FindProperty("m_OnClick");
            var callsProp = onClickProp.FindPropertyRelative("m_PersistentCalls.m_Calls");

            callsProp.arraySize++;
            var callProp = callsProp.GetArrayElementAtIndex(callsProp.arraySize - 1);
            callProp.FindPropertyRelative("m_Target").objectReferenceValue = target;
            callProp.FindPropertyRelative("m_MethodName").stringValue = methodName;
            callProp.FindPropertyRelative("m_Mode").intValue = 1; // Void
            callProp.FindPropertyRelative("m_CallState").intValue = 2; // RuntimeOnly → EditorAndRuntime
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
