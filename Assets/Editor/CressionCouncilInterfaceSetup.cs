#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SkullKing.UI;

namespace SkullKing.Editor
{
    public static class CressionCouncilInterfaceSetup
    {
        private const int Revision = 1;
        private static readonly string[] Paths = { "Assets/Scenes/LobbyScene.unity", "Assets/Scenes/GameScene.unity" };
        private static readonly Color Ivory = new Color(.95f, .92f, .83f);
        private static readonly Color Gold = new Color(.78f, .61f, .33f);
        private static readonly Color Jade = new Color(.32f, .76f, .67f);
        private static readonly Color Bottom = new Color(.018f, .031f, .058f, .98f);
        private static readonly Color Top = new Color(.055f, .09f, .14f, .98f);

        [MenuItem("CROSSION/Apply Unified UI Style (Once)")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[Council UI] Play 모드를 종료하세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                { Debug.LogWarning("[Council UI] 열려 있는 씬을 먼저 저장하세요."); return; }
            if (!EditorUtility.DisplayDialog("UI 스타일 1회 적용",
                "최초 적용은 공통 UI 스타일을 설정합니다. 이미 적용된 씬에서는 누락 복구와 아직 적용하지 않은 항목만 갱신합니다.\n\n이번 갱신: 천상강림 연속 윤곽 변형, 날개 약 30% 축소, 중앙 발광 강화.\n게임 화면 배경/패널과 규칙 본문/페이지는 변경하지 않습니다. 타이틀 버튼 크기·위치·클릭 기능도 유지합니다.\n씬을 먼저 백업하며 갱신을 반복 적용하지 않습니다.",
                "백업 후 적용", "취소")) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/malgun SDF.asset");
            var legacyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/malgun.ttf");
            if (font == null || legacyFont == null) throw new InvalidOperationException("기존 한글 폰트가 없습니다.");
            RunStyleSafetyChecks();
            string backup = "ProjectBackups/CouncilInterface/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            foreach (var path in Paths) File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
            var previous = EditorSceneManager.GetSceneManagerSetup();
            int changed = 0, repaired = 0;
            try
            {
                foreach (string path in Paths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var canvas = All<Canvas>(scene).FirstOrDefault(c => c.isRootCanvas && (c.name == "GameCanvas" || c.name == "LobbyCanvas"));
                    if (canvas == null) throw new InvalidOperationException(path + "의 Canvas가 없습니다.");
                    var stamp = canvas.GetComponent<CouncilThemeStamp>();
                    if (stamp != null && stamp.Revision >= Revision)
                    {
                        // Repair missing dependencies only: never restyle an already edited scene.
                        int fixes = RepairScene(scene);
                        if (fixes > 0)
                        {
                            EditorSceneManager.MarkSceneDirty(scene);
                            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("복구 씬 저장 실패: " + path);
                            repaired += fixes;
                        }
                        continue;
                    }
                    RepairScene(scene);
                    var before = Snapshot(scene);
                    Undo.IncrementCurrentGroup();
                    int undo = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Apply Council interface style");
                    try
                    {
                        StyleScene(scene, canvas, font, legacyFont);
                        Validate(scene, before);
                        if (stamp == null) stamp = Undo.AddComponent<CouncilThemeStamp>(canvas.gameObject);
                        var so = new SerializedObject(stamp);
                        so.FindProperty("_revision").intValue = Revision;
                        so.FindProperty("_settingsControlsRevision").intValue = 1;
                        so.ApplyModifiedProperties();
                        if (path == Paths[1]) CressionCelestialPolishUpgrade.ApplyToScene(scene);
                        else CressionTitleUtilityIconsUpgrade.ApplyToScene(scene);
                        EditorSceneManager.MarkSceneDirty(scene);
                        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("씬 저장 실패: " + path);
                        Undo.CollapseUndoOperations(undo);
                        changed++;
                    }
                    catch { Undo.RevertAllDownToGroup(undo); throw; }
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[Council UI] " + changed + "개 씬 스타일 적용, " + repaired
                    + "개 누락 구성 복구. 기존 스타일은 덮어쓰지 않았습니다. 백업: " + backup);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static IEnumerable<T> All<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
        private static int RepairScene(Scene scene)
        {
            int fixes = 0;
            foreach (var panel in All<CouncilPanelGraphic>(scene))
            {
                if (panel.GetComponent<CanvasRenderer>() == null)
                { Undo.AddComponent<CanvasRenderer>(panel.gameObject); fixes++; }
                panel.SetAllDirty();
            }
            var rulebook = AssetDatabase.LoadAssetAtPath<RulebookContent>("Assets/Settings/CrossionRulebook.asset");
            if (rulebook != null)
                foreach (var controller in All<SupportWindowsController>(scene))
                {
                    var so = new SerializedObject(controller);
                    var property = so.FindProperty("_rulebook");
                    if (property.objectReferenceValue != null) continue;
                    property.objectReferenceValue = rulebook;
                    so.ApplyModifiedProperties();
                    fixes++;
                }
            var stamp = All<CouncilThemeStamp>(scene).FirstOrDefault();
            if (stamp != null && stamp.SettingsControlsRevision < 1)
            {
                foreach (var settings in All<SettingsPanelController>(scene))
                {
                    var so = new SerializedObject(settings);
                    foreach (string field in new[] { "_masterVolumeSlider", "_musicVolumeSlider", "_effectsVolumeSlider" })
                    {
                        var slider = so.FindProperty(field).objectReferenceValue as Slider;
                        if (slider == null) continue;
                        StyleSlider(slider);
                        fixes++;
                    }
                    var toggle = so.FindProperty("_fullscreenToggle").objectReferenceValue as Toggle;
                    if (toggle != null) { StyleToggle(toggle); fixes++; }
                }
                var stampSO = new SerializedObject(stamp);
                stampSO.FindProperty("_settingsControlsRevision").intValue = 1;
                stampSO.ApplyModifiedProperties();
                fixes++;
            }
            if (scene.path == Paths[1])
                fixes += CressionCelestialPolishUpgrade.ApplyToScene(scene);
            else if(scene.path==Paths[0])
                fixes += CressionTitleUtilityIconsUpgrade.ApplyToScene(scene);
            return fixes;
        }
        private static bool IsLobbyBackdrop(Image image)
        {
            // Slider tracks also use the name Background. Protect only the root lobby backdrop.
            if (image == null || image.name != "Background") return false;
            var parent = image.transform.parent;
            if (parent == null || parent.name != "LobbyCanvas") return false;
            var canvas = parent.GetComponent<Canvas>();
            return canvas != null && canvas.isRootCanvas;
        }

        public static int RunStyleSafetyChecks()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("LobbyCanvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(root, preview);
                var backdrop = new GameObject("Background", typeof(RectTransform), typeof(Image));
                backdrop.transform.SetParent(root.transform, false);
                var window = new GameObject("SettingsWindow", typeof(RectTransform));
                window.transform.SetParent(root.transform, false);
                var track = new GameObject("MasterVolumeSlider", typeof(RectTransform), typeof(Slider));
                track.transform.SetParent(window.transform, false);
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
                background.transform.SetParent(track.transform, false);
                if (!IsLobbyBackdrop(backdrop.GetComponent<Image>()))
                    throw new InvalidOperationException("실제 로비 배경 보호 검사 실패.");
                if (IsLobbyBackdrop(background.GetComponent<Image>()))
                    throw new InvalidOperationException("슬라이더 배경을 로비 배경으로 잘못 분류했습니다.");
                var otherRoot = new GameObject("OtherCanvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(otherRoot, preview);
                var otherBackground = new GameObject("Background", typeof(RectTransform), typeof(Image));
                otherBackground.transform.SetParent(otherRoot.transform, false);
                if (IsLobbyBackdrop(otherBackground.GetComponent<Image>()))
                    throw new InvalidOperationException("다른 Canvas 배경 분류 검사 실패.");
                var snapshot = Snapshot(preview);
                if (snapshot.Backgrounds.Count != 1 || !snapshot.Backgrounds.ContainsKey(backdrop.GetComponent<Image>()))
                    throw new InvalidOperationException("로비 배경 보호 대상 수 검사 실패.");
                StyleSlider(track.GetComponent<Slider>());
                Validate(preview, snapshot);
                var panelObject = new GameObject("Panel check", typeof(RectTransform), typeof(CouncilPanelGraphic));
                SceneManager.MoveGameObjectToScene(panelObject, preview);
                var panel = panelObject.GetComponent<CouncilPanelGraphic>();
                if (panel.GetComponent<CanvasRenderer>() == null)
                    throw new InvalidOperationException("패널 CanvasRenderer 생성 검사 실패.");
                panel.rectTransform.sizeDelta = new Vector2(320,120);
                using (var vertices = new VertexHelper())
                {
                    typeof(CouncilPanelGraphic).GetMethod("OnPopulateMesh",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                        null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(panel, new object[] { vertices });
                    if (vertices.currentVertCount == 0) throw new InvalidOperationException("패널 메시 생성 검사 실패.");
                    var vertex = new UIVertex();
                    vertices.PopulateUIVertex(ref vertex, 0);
                    if (vertex.color.a == 0) throw new InvalidOperationException("패널 투명도 검사 실패.");
                }
                return 8;
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        private static bool Under(Transform target, string name)
        {
            for (var parent = target; parent != null; parent = parent.parent) if (parent.name == name) return true;
            return false;
        }
        private static bool Protected(Transform target)
            => target.GetComponentInParent<CardUI>(true) != null || target.GetComponentInParent<WhaleWingTransition>(true) != null
                || Under(target, "MainMenuPanel") || Under(target, "CouncilLogo");

        private static void StyleScene(Scene scene, Canvas canvas, TMP_FontAsset font, Font legacyFont)
        {
            bool game = scene.path == Paths[1];
            foreach (var text in All<Text>(scene).Where(t => !Protected(t.transform)))
            {
                Undo.RecordObject(text, "Council typography");
                text.font = legacyFont;
                text.color = text.name.Contains("Value") || text.name.Contains("RoundInfo") || text.name.Contains("Result") ? Gold : Ivory;
                text.raycastTarget = false;
                // Keep Text components: score, bid and network code reference these exact objects.
            }
            foreach (var text in All<TMP_Text>(scene).Where(t => !Protected(t.transform)))
            {
                Undo.RecordObject(text, "Council typography");
                text.font = font;
                text.color = text.name.Contains("Title") || text.name.Contains("PageNumber") ? Gold : Ivory;
                if (text.name == "Placeholder") text.color = new Color(.52f, .59f, .65f);
                text.raycastTarget = text.GetComponent<Button>() != null;
            }
            var windowNames = new HashSet<string> {
                "SettingsWindow", "Window", "BidPanel", "TigressPanel", "RoundResultPanel", "GameEndPanel",
                "PlayerCountPanel", "SteamMenuPanel", "SteamRoomCreationPanel", "SteamWaitingRoomPanel", "Drawer"
            };
            foreach (var rect in All<RectTransform>(scene).Where(r => !Protected(r)))
            {
                if (windowNames.Contains(rect.name)) Panel(rect, Bottom, Top, Gold);
                else if (game && (rect.name.StartsWith("PlayerInfo_") || rect.name == "CenterPlayAreaBg"))
                    Panel(rect, new Color(.018f,.031f,.058f,.65f), new Color(.04f,.065f,.10f,.7f), new Color(.6f,.48f,.26f,.45f));
            }
            foreach (var button in All<Button>(scene).Where(b => !Protected(b.transform)).ToArray()) StyleButton(button);
            foreach (var slider in All<Slider>(scene).Where(s => !Protected(s.transform))) StyleSlider(slider);
            foreach (var toggle in All<Toggle>(scene).Where(t => !Protected(t.transform))) StyleToggle(toggle);
            foreach (var input in All<TMP_InputField>(scene).Where(i => !Protected(i.transform)))
            {
                Panel(input.GetComponent<RectTransform>(), new Color(.01f,.02f,.04f), new Color(.03f,.05f,.08f), Gold * .7f);
                Undo.RecordObject(input, "Council input");
                input.caretColor = Ivory;
                input.selectionColor = new Color(.25f,.65f,.59f,.35f);
            }
            foreach (var image in All<Image>(scene).Where(i => !Protected(i.transform)))
            {
                if (game && image.name == "TableBackground")
                { Undo.RecordObject(image, "Council table color"); image.color = new Color(.025f,.055f,.075f); }
                // Hand-card hover restores sibling indices: never insert a decoration into that card container.
                if (game && image.name == "PlayerHandArea")
                { Undo.RecordObject(image, "Council hand surface"); image.color = new Color(.018f,.031f,.058f,.35f); }
                if (image.name == "TurnArrow")
                { Undo.RecordObject(image, "Council turn indicator"); image.color = Jade; }
            }
            if (game) ArrangeGameToolbar(canvas);
        }

        private static CouncilPanelGraphic Panel(RectTransform rect, Color bottom, Color top, Color gold)
        {
            var old = rect.GetComponent<Image>();
            if (old != null) { Undo.RecordObject(old, "Hide old panel surface"); old.color = Color.clear; }
            var child = rect.Find("CouncilSurface");
            CouncilPanelGraphic plate;
            if (child == null)
            {
                var visual = new GameObject("CouncilSurface", typeof(RectTransform), typeof(CanvasRenderer));
                visual.layer = rect.gameObject.layer;
                Undo.RegisterCreatedObjectUndo(visual, "Council surface");
                visual.transform.SetParent(rect, false);
                visual.transform.SetAsFirstSibling();
                var rt = (RectTransform)visual.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                plate = visual.AddComponent<CouncilPanelGraphic>();
            }
            else plate = child.GetComponent<CouncilPanelGraphic>();
            Undo.RecordObject(plate, "Council surface colors");
            plate.Configure(bottom, top, gold, 14, 1.4f);
            return plate;
        }
        private static void StyleButton(Button button)
        {
            // History controls keep their text on the button root. A filled child would cover that text.
            // Keep those as gold text controls rather than replacing/moving their bound TMP components.
            Graphic textGraphic = button.GetComponent<TMP_Text>();
            if (textGraphic == null) textGraphic = button.GetComponent<Text>();
            if (textGraphic != null)
            {
                Undo.RecordObject(button, "Council text button");
                Undo.RecordObject(textGraphic, "Council text button colors");
                textGraphic.raycastTarget = true;
                button.targetGraphic = textGraphic;
                button.transition = Selectable.Transition.ColorTint;
                var textColors = ColorBlock.defaultColorBlock;
                textColors.normalColor = Ivory; textColors.highlightedColor = Gold;
                textColors.selectedColor = Gold; textColors.pressedColor = Jade;
                textColors.disabledColor = new Color(.4f,.43f,.47f); textColors.fadeDuration = .12f;
                button.colors = textColors;
                return;
            }
            var plate = Panel((RectTransform)button.transform, Bottom, Top, Gold);
            Undo.RecordObject(button, "Council button states");
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.16f,1.15f,1.05f);
            colors.selectedColor = new Color(1.12f,1.12f,1.02f);
            colors.pressedColor = new Color(.62f,.85f,.81f);
            colors.disabledColor = new Color(.45f,.45f,.45f,.65f);
            colors.fadeDuration = .12f; button.colors = colors;
            // Text-only buttons (e.g. history) can have their raycast Graphic on the same object.
            var hit = button.GetComponent<Graphic>();
            if (hit != null) { Undo.RecordObject(hit, "Keep button hit area"); hit.raycastTarget = true; }
            var rim = plate.GetComponent<Outline>();
            if (rim == null) rim = Undo.AddComponent<Outline>(plate.gameObject);
            rim.effectColor = Color.clear; rim.effectDistance = new Vector2(1,-1);
            var hover = button.GetComponent<CouncilButtonFeedback>();
            if (hover == null) hover = Undo.AddComponent<CouncilButtonFeedback>(button.gameObject);
            Ref(hover, "_visual", plate.rectTransform); Ref(hover, "_rim", rim);
            if (button.GetComponent<SkullKing.Audio.ButtonClickSound>() == null)
                Undo.AddComponent<SkullKing.Audio.ButtonClickSound>(button.gameObject);
        }
        private static void StyleSlider(Slider slider)
        {
            // Keep the Slider and its fill/handle references; only skin their existing RectTransforms.
            var rail = slider.transform.Find("Background") as RectTransform;
            if (rail != null)
            {
                CenterVertically(rail, 14);
                var plate = Panel(rail, new Color(.009f,.018f,.032f), new Color(.025f,.045f,.07f), Gold);
                plate.Configure(new Color(.009f,.018f,.032f), new Color(.025f,.045f,.07f), Gold, 5, 1.2f);
            }
            if (slider.fillRect != null)
            {
                CenterVertically(slider.fillRect, 6);
                var fill = Panel(slider.fillRect, new Color(.11f,.4f,.36f), Jade, new Color(.35f,.68f,.55f));
                fill.Configure(new Color(.11f,.4f,.36f), Jade, new Color(.35f,.68f,.55f), 2, .5f);
            }
            if (slider.handleRect != null)
            {
                Undo.RecordObject(slider.handleRect, "Council metal handle");
                var handleSize = slider.handleRect.sizeDelta;
                handleSize.x = 24;
                slider.handleRect.sizeDelta = handleSize;
                CenterVertically(slider.handleRect, 30);
                var handle = Panel(slider.handleRect, new Color(.29f,.2f,.09f), new Color(.83f,.67f,.39f), Ivory);
                handle.Configure(new Color(.29f,.2f,.09f), new Color(.83f,.67f,.39f), Ivory, 8, 1.2f);
                Undo.RecordObject(slider, "Council slider handle graphic");
                slider.targetGraphic = handle;
            }
            Undo.RecordObject(slider, "Council slider states");
            var colors = slider.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.1f,1.1f,1);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.7f,.95f,.87f); colors.fadeDuration = .12f;
            slider.colors = colors;
        }
        private static void StyleToggle(Toggle toggle)
        {
            if (toggle.name == "FullscreenToggle")
            {
                var rect = (RectTransform)toggle.transform;
                Undo.RecordObject(rect, "Fullscreen row hit area");
                rect.sizeDelta = new Vector2(310,44);
                var hit = toggle.GetComponent<Image>();
                if (hit == null) hit = Undo.AddComponent<Image>(toggle.gameObject);
                Undo.RecordObject(hit, "Fullscreen row hit graphic");
                hit.color = Color.clear; hit.raycastTarget = true;
                if (toggle.targetGraphic != null)
                {
                    var box = toggle.targetGraphic.rectTransform;
                    PlaceControl(box, new Vector2(0,.5f), new Vector2(.5f,.5f), new Vector2(20,0), new Vector2(32,32));
                    var frame = Panel(box, Bottom, Top, Gold);
                    Undo.RecordObject(toggle, "Fullscreen gold frame");
                    toggle.targetGraphic = frame;
                }
                if (toggle.graphic != null)
                {
                    var mark = toggle.graphic.rectTransform;
                    PlaceControl(mark, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(22,22));
                    Undo.RecordObject(toggle.graphic, "Fullscreen checkmark");
                    toggle.graphic.color = Jade;
                }
                foreach (var text in toggle.GetComponentsInChildren<Text>(true))
                {
                    Undo.RecordObject(text, "Fullscreen label");
                    text.text = "전체 화면"; text.color = Ivory; text.fontSize = 22;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
                    PlaceControl(text.rectTransform, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(54,0), new Vector2(250,44));
                }
                foreach (var text in toggle.GetComponentsInChildren<TMP_Text>(true))
                {
                    Undo.RecordObject(text, "Fullscreen label");
                    text.text = "전체 화면"; text.color = Ivory; text.fontSize = 22;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
                    PlaceControl(text.rectTransform, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(54,0), new Vector2(250,44));
                }
                var colors = toggle.colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(1.12f,1.12f,1);
                colors.selectedColor = colors.highlightedColor; colors.pressedColor = Jade;
                toggle.colors = colors;
                return;
            }
            if (toggle.targetGraphic != null)
            { Undo.RecordObject(toggle.targetGraphic, "Council toggle"); toggle.targetGraphic.color = new Color(.18f,.24f,.29f); }
            if (toggle.graphic != null)
            { Undo.RecordObject(toggle.graphic, "Council checkmark"); toggle.graphic.color = Gold; }
        }
        private static void CenterVertically(RectTransform rect, float height)
        {
            Undo.RecordObject(rect, "Council control rail size");
            var min = rect.anchorMin; var max = rect.anchorMax;
            min.y = max.y = .5f; rect.anchorMin = min; rect.anchorMax = max;
            var size = rect.sizeDelta; size.y = height; rect.sizeDelta = size;
            var pos = rect.anchoredPosition; pos.y = 0; rect.anchoredPosition = pos;
        }
        private static void PlaceControl(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            Undo.RecordObject(rect, "Council control layout");
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static void ArrangeGameToolbar(Canvas canvas)
        {
            // Only the global toolbar is repositioned; all hand/opponent/trick areas stay untouched.
            string[] names = { "SettingsButton", "RulesButton", "ReturnToLobbyButton" };
            string[] captions = { "설정", "규칙", "타이틀" };
            for (int i = 0; i < names.Length; i++)
            {
                var button = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == names[i]);
                if (button == null) continue;
                var rect = (RectTransform)button.transform;
                Undo.RecordObject(rect, "Council game toolbar");
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                rect.sizeDelta = new Vector2(150,44);
                rect.anchoredPosition = new Vector2(-24-i*166,-20);
                foreach (var text in button.GetComponentsInChildren<Text>(true)) { text.text = captions[i]; text.fontSize = 19; }
                foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) { text.text = captions[i]; text.fontSize = 19; }
            }
        }

        private sealed class BindingSnapshot
        {
            public Dictionary<Button,string> Buttons = new Dictionary<Button,string>();
            public Dictionary<Component,string> Controllers = new Dictionary<Component,string>();
            public Dictionary<RectTransform,string> Layouts = new Dictionary<RectTransform,string>();
            public Dictionary<GameObject,bool> Active = new Dictionary<GameObject,bool>();
            public Dictionary<Image,string> Backgrounds = new Dictionary<Image,string>();
        }
        private static BindingSnapshot Snapshot(Scene scene)
        {
            var snapshot = new BindingSnapshot();
            foreach (var button in All<Button>(scene)) snapshot.Buttons[button] = Events(button);
            foreach (var component in All<MonoBehaviour>(scene).Where(c => c is UIBindingManager || c is SettingsPanelController || c is SupportWindowsController || c is SkullKing.Core.LobbyManager))
                snapshot.Controllers[component] = EditorJsonUtility.ToJson(component);
            foreach (var rect in All<RectTransform>(scene))
            {
                if (Protected(rect) || rect.name == "TrickArea" || rect.name == "PlayerHandArea" || rect.name.StartsWith("OpponentArea_"))
                    snapshot.Layouts[rect] = Layout(rect);
                snapshot.Active[rect.gameObject] = rect.gameObject.activeSelf;
            }
            if (scene.path == Paths[0] || EditorSceneManager.IsPreviewScene(scene))
                foreach (var image in All<Image>(scene).Where(IsLobbyBackdrop))
                    snapshot.Backgrounds[image] = EditorJsonUtility.ToJson(image);
            return snapshot;
        }
        private static void Validate(Scene scene, BindingSnapshot snapshot)
        {
            foreach (var pair in snapshot.Buttons)
                if (pair.Key == null || Events(pair.Key) != pair.Value) throw new InvalidOperationException("버튼 이벤트 연결 변경 감지.");
            foreach (var pair in snapshot.Controllers)
                if (pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("UI 컨트롤러 연결 변경 감지: " + pair.Key.name);
            foreach (var pair in snapshot.Layouts)
            {
                if (Layout(pair.Key) != pair.Value) throw new InvalidOperationException("타이틀/카드 배치 변경 감지: " + pair.Key.name);
            }
            foreach (var pair in snapshot.Active)
                if (pair.Key == null || pair.Key.activeSelf != pair.Value) throw new InvalidOperationException("기존 UI 활성 상태 변경 감지.");
            foreach (var pair in snapshot.Backgrounds)
                if (EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("로비 배경 변경 감지.");
        }
        private static string Events(Button button)
            => string.Join("|", Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Select(i =>
                button.onClick.GetPersistentTarget(i)?.GetInstanceID() + ":" + button.onClick.GetPersistentMethodName(i)))
                + EditorJsonUtility.ToJson(button).Split(new[] { "\"m_OnClick\"" }, StringSplitOptions.None).Last();
        [Serializable] private struct RectState
        {
            public Vector2 min, max, pivot, size;
            public Vector3 position, scale;
            public Quaternion rotation;
        }
        private static string Layout(RectTransform rect) => JsonUtility.ToJson(new RectState {
            min = rect.anchorMin, max = rect.anchorMax, pivot = rect.pivot, size = rect.sizeDelta,
            position = rect.anchoredPosition3D, scale = rect.localScale, rotation = rect.localRotation
        });
        private static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
        { var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
