#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SkullKing.Core;
using SkullKing.UI;
using SkullKing.Audio;

namespace SkullKing.Editor
{
    public static class CressionCouncilTitleSetup
    {
        private const string AtlasPath = "Assets/Art/UI/Council/council-title-atlas-v1.png";
        private const string SpritesPath = "Assets/Art/UI/Council/council-title-v1.councilatlas";
        private static readonly Color Ivory = new Color(.97f, .93f, .81f);

        // Headless Unity entry point; uses the same scene-authoring path as the menu.
        public static void ApplyBatch()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity", OpenSceneMode.Single);
            var background = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Image>(true))
                .FirstOrDefault(i => i.name == "Background");
            string before = background != null ? EditorJsonUtility.ToJson(background) : "";
            Apply();
            if (background != null && before != EditorJsonUtility.ToJson(background))
                throw new InvalidOperationException("Background unexpectedly changed.");
            var buttons = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CouncilButtonFeedback>(true)).ToArray();
            if (buttons.Length != 6) throw new InvalidOperationException("Expected six title buttons.");
            var settings = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SettingsPanelController>(true)).FirstOrDefault();
            if (settings == null) throw new InvalidOperationException("Missing title settings.");
            foreach (var component in settings.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var target = iterator.objectReferenceValue;
                    GameObject go = target as GameObject;
                    if (target is Component c) go = c.gameObject;
                    if (go != null && go.scene.IsValid() && go.scene != scene)
                        throw new InvalidOperationException("Cross-scene settings reference: " + iterator.propertyPath);
                }
            }
            Debug.Log("[Council UI Checks] Six buttons, existing background and local settings references verified.");
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[Council UI] Play 모드를 종료하세요."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/LobbyScene.unity")
            { Debug.LogWarning("[Council UI] LobbyScene을 열어 주세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var roots = scene.GetRootGameObjects();
            var lobby = roots.SelectMany(r => r.GetComponentsInChildren<LobbyManager>(true)).FirstOrDefault();
            if (lobby == null) throw new InvalidOperationException("LobbyManager가 없습니다.");
            var lobbySO = new SerializedObject(lobby);
            var menu = lobbySO.FindProperty("_mainMenuPanel").objectReferenceValue as GameObject;
            if (menu == null) throw new InvalidOperationException("LobbyManager의 Main Menu Panel 연결을 확인하세요.");
            if (menu.transform.Find("CouncilLogo") != null)
            { UpdateTitleCornerButtons(); return; }
            var canvas = menu.GetComponentInParent<Canvas>().rootCanvas;
            var allButtons = roots.SelectMany(r => r.GetComponentsInChildren<Button>(true)).ToArray();
            var single = FindAction(allButtons, lobby, "OpenSinglePlayerSetup");
            var multi = FindAction(allButtons, lobby, "OpenSteamMultiplayer");
            var quit = FindAction(allButtons, lobby, "QuitGame");
            var support = roots.SelectMany(r => r.GetComponentsInChildren<SupportWindowsController>(true)).FirstOrDefault();
            if (support == null) throw new InvalidOperationException("규칙/피드백 창을 먼저 연결해 주세요.");
            var rules = FindAction(allButtons, support, "ShowHelp");
            var feedback = FindAction(allButtons, support, "ShowFeedback");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/malgun SDF.asset");
            if (font == null || single == null || multi == null || quit == null || rules == null || feedback == null)
                throw new InvalidOperationException("한글 폰트 및 싱글/멀티/규칙/피드백/종료 버튼의 On Click 연결을 확인하세요.");
            var sprites = ImportAtlas();
            string backup = "ProjectBackups/CouncilTitle/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path, Path.Combine(backup, "LobbyScene.unity"));
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Council title art");
            try
            {
                // Keep the existing Background object, sprite, color and hierarchy untouched.
                var menuRect = (RectTransform)menu.transform;
                Undo.RecordObject(menuRect, "Title layout");
                Stretch(menuRect);
                var menuImage = menu.GetComponent<Image>();
                if (menuImage != null) { Undo.RecordObject(menuImage, "Title panel"); menuImage.raycastTarget = false; }
                foreach (string oldName in new[] { "TitleText", "SubtitleText" })
                {
                    var old = canvas.transform.Find(oldName);
                    if (old != null) { Undo.RecordObject(old.gameObject, "Hide old title"); old.gameObject.SetActive(false); }
                }
                var logo = Art("CouncilLogo", menu.transform, sprites[0], new Vector2(0, 245), new Vector2(820, 345));
                Undo.RegisterCreatedObjectUndo(logo.gameObject, "Council logo");
                Style(single, menu.transform, sprites[1], sprites[2], "싱글플레이", new Vector2(0, -10), false, font);
                Style(multi, menu.transform, sprites[1], sprites[3], "멀티플레이", new Vector2(0, -165), false, font);
                Style(rules, menu.transform, null, sprites[4], "규칙", new Vector2(-270, -370), true, font);
                Style(feedback, menu.transform, null, sprites[6], "피드백", new Vector2(90, -370), true, font);
                Style(quit, menu.transform, null, sprites[7], "종료", new Vector2(270, -370), true, font);

                var settings = EnsureSettings(scene, canvas.transform);
                var settingsRect = Rect("CouncilSettingsButton", menu.transform, Vector2.zero, new Vector2(112, 140));
                Undo.RegisterCreatedObjectUndo(settingsRect.gameObject, "Title settings button");
                settingsRect.gameObject.AddComponent<Image>();
                var settingsButton = settingsRect.gameObject.AddComponent<Button>();
                UnityEventTools.AddPersistentListener(settingsButton.onClick, settings.Open);
                Style(settingsButton, menu.transform, null, sprites[5], "설정", new Vector2(-90, -370), true, font);
                ArrangeCornerButtons(rules, feedback, settingsButton, quit, sprites[1]);
                Undo.CollapseUndoOperations(undo);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("LobbyScene 저장 실패");
                Selection.activeGameObject = menu;
                Debug.Log("[Council UI] 타이틀 로고/버튼/아이콘 적용 완료. 배경은 변경하지 않았습니다. 버튼 글자는 CouncilLabel에서 수정할 수 있습니다. 씬 백업: " + backup);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        public static void UpdateTitleCornerButtons()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[Council UI] Play 모드를 종료하세요."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/LobbyScene.unity")
            { Debug.LogWarning("[Council UI] LobbyScene을 열어 주세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var roots = scene.GetRootGameObjects();
            var lobby = roots.SelectMany(r => r.GetComponentsInChildren<LobbyManager>(true)).FirstOrDefault();
            var support = roots.SelectMany(r => r.GetComponentsInChildren<SupportWindowsController>(true)).FirstOrDefault();
            var settings = roots.SelectMany(r => r.GetComponentsInChildren<SettingsPanelController>(true)).FirstOrDefault();
            if (lobby == null || support == null || settings == null)
                throw new InvalidOperationException("기존 타이틀 테마와 설정/규칙 창 연결을 확인하세요.");
            var menu = new SerializedObject(lobby).FindProperty("_mainMenuPanel").objectReferenceValue as GameObject;
            if (menu == null || menu.transform.Find("CouncilLogo") == null)
                throw new InvalidOperationException("먼저 Apply Council Title Theme를 실행하세요.");
            var buttons = menu.GetComponentsInChildren<Button>(true);
            var rules = FindAction(buttons, support, "ShowHelp");
            var feedback = FindAction(buttons, support, "ShowFeedback");
            var settingsButton = FindAction(buttons, settings, "Open");
            var quit = FindAction(buttons, lobby, "QuitGame");
            if (new[] { rules, feedback, settingsButton, quit }.Any(b => b == null || b.transform.Find("CouncilVisual") == null))
                throw new InvalidOperationException("규칙/피드백/설정/종료 버튼 연결 및 CouncilVisual을 확인하세요.");
            var sprites = ImportAtlas();
            string backup = "ProjectBackups/CouncilTitle/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path, Path.Combine(backup, "LobbyScene.unity"));
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Update title corner buttons");
            try
            {
                ArrangeCornerButtons(rules, feedback, settingsButton, quit, sprites[1]);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("LobbyScene 저장 실패");
                Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject = menu;
                Debug.Log("[Council UI] 규칙/피드백은 우측 상단, 설정/종료는 우측 하단 블록 버튼으로 변경했습니다. 로고/싱글/멀티/배경 및 클릭 기능은 유지했습니다. 백업: " + backup);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        public static void MatchSettingsAndExitSize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Council UI] Play 모드를 종료하세요."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/LobbyScene.unity") { Debug.LogWarning("[Council UI] LobbyScene을 열어 주세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var roots = scene.GetRootGameObjects();
            var lobby = roots.SelectMany(r => r.GetComponentsInChildren<LobbyManager>(true)).FirstOrDefault();
            var settings = roots.SelectMany(r => r.GetComponentsInChildren<SettingsPanelController>(true)).FirstOrDefault();
            if (lobby == null || settings == null) throw new InvalidOperationException("타이틀 UI 연결을 확인하세요.");
            var menu = new SerializedObject(lobby).FindProperty("_mainMenuPanel").objectReferenceValue as GameObject;
            if (menu == null) throw new InvalidOperationException("Main Menu Panel 연결을 확인하세요.");
            var buttons = menu.GetComponentsInChildren<Button>(true);
            var single = FindAction(buttons, lobby, "OpenSinglePlayerSetup");
            var multi = FindAction(buttons, lobby, "OpenSteamMultiplayer");
            var settingsButton = FindAction(buttons, settings, "Open");
            var quit = FindAction(buttons, lobby, "QuitGame");
            if (new[] { single, multi, settingsButton, quit }.Any(b => b == null || b.transform.Find("CouncilVisual") == null))
                throw new InvalidOperationException("타이틀 버튼 연결 및 CouncilVisual을 확인하세요.");
            var menuRect = (RectTransform)menu.transform;
            var singleRect = (RectTransform)single.transform;
            var reference = MenuBounds(singleRect, menuRect);
            var label = single.transform.Find("CouncilVisual").GetComponentInChildren<TMP_Text>(true);
            var art = single.transform.Find("CouncilVisual/Frame").GetComponent<Image>();
            string backup = "ProjectBackups/CouncilTitle/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path, Path.Combine(backup, "LobbyScene.unity"));
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Match title button sizes");
            try
            {
                const float gap = 27;
                var size = reference.size;
                float x = reference.xMax - menuRect.rect.xMax - size.x / 2;
                float quitY = 35 + size.y / 2;
                MakeCornerBlock(quit, art.sprite, new Vector2(x, quitY), size, label.fontSize);
                MakeCornerBlock(settingsButton, art.sprite, new Vector2(x, quitY + size.y + gap), size, label.fontSize);
                float settingsTop = menuRect.rect.yMin + quitY + size.y * 1.5f + gap;
                var multiRect = (RectTransform)multi.transform;
                float shift = Mathf.Max(0, settingsTop + gap - MenuBounds(multiRect, menuRect).yMin);
                if (shift > 0)
                {
                    Undo.RecordObject(singleRect, "Make room for matching buttons");
                    Undo.RecordObject(multiRect, "Make room for matching buttons");
                    singleRect.anchoredPosition += Vector2.up * shift;
                    multiRect.anchoredPosition += Vector2.up * shift;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("LobbyScene 저장 실패");
                Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject = settingsButton.gameObject;
                Debug.Log("[Council UI] 설정/종료를 싱글플레이의 실제 크기와 글자 크기에 맞췄습니다. 우측 하단 배치 및 겹침 방지 완료. 백업: " + backup);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static Rect MenuBounds(RectTransform rect, RectTransform menu)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                Vector2 point = menu.InverseTransformPoint(corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void ArrangeCornerButtons(Button rules, Button feedback, Button settings, Button quit, Sprite frame)
        {
            AnchorCorner(rules, new Vector2(1, 1), new Vector2(-215, -90), new Vector2(112, 145));
            AnchorCorner(feedback, new Vector2(1, 1), new Vector2(-80, -90), new Vector2(112, 145));
            MakeCornerBlock(settings, frame, new Vector2(-205, 145));
            MakeCornerBlock(quit, frame, new Vector2(-205, 65));
        }

        private static void AnchorCorner(Button button, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)button.transform;
            Undo.RecordObject(rect, "Anchor menu button");
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void MakeCornerBlock(Button button, Sprite sprite, Vector2 position, Vector2? targetSize = null, float fontSize = 26)
        {
            var size = targetSize ?? new Vector2(340, 64);
            AnchorCorner(button, new Vector2(1, 0), position, size);
            Undo.RecordObject(button.transform, "Normalize matching button scale");
            button.transform.localScale = Vector3.one;
            var visual = (RectTransform)button.transform.Find("CouncilVisual");
            var label = visual.GetComponentInChildren<TMP_Text>(true);
            if (label == null) throw new InvalidOperationException(button.name + "의 CouncilLabel이 없습니다.");
            Undo.RecordObject(visual, "Resize block visual");
            visual.sizeDelta = size;
            visual.localScale = Vector3.one;
            var icon = visual.Find("Icon");
            if (icon != null) { Undo.RecordObject(icon.gameObject, "Hide round medallion"); icon.gameObject.SetActive(false); }
            var frameTransform = visual.Find("Frame");
            Image frame;
            if (frameTransform == null)
            {
                frame = Art("Frame", visual, sprite, Vector2.zero, size);
                Undo.RegisterCreatedObjectUndo(frame.gameObject, "Block button frame");
                frame.transform.SetAsFirstSibling();
            }
            else
            {
                frame = frameTransform.GetComponent<Image>();
                Undo.RecordObject(frame, "Block artwork");
                Undo.RecordObject(frame.rectTransform, "Block artwork size");
                frame.sprite = sprite;
                Place(frame.rectTransform, Vector2.zero, size);
            }
            frame.preserveAspect = true;
            frame.raycastTarget = false;
            Undo.RecordObject(label.rectTransform, "Block label position");
            Undo.RecordObject(label, "Block label size");
            Place(label.rectTransform, Vector2.zero, new Vector2(size.x * .75f, size.y * .7f));
            label.fontSize = fontSize;
            Undo.RecordObject(button, "Block target graphic");
            button.targetGraphic = frame;
            var rim = frame.GetComponent<Outline>();
            if (rim == null) rim = Undo.AddComponent<Outline>(frame.gameObject);
            Undo.RecordObject(rim, "Block highlight");
            rim.effectColor = Color.clear;
            rim.effectDistance = new Vector2(2, -2);
            var hover = button.GetComponent<CouncilButtonFeedback>();
            if (hover != null)
            {
                Undo.RecordObject(hover, "Block hover target");
                Ref(hover, "_visual", visual);
                Ref(hover, "_rim", rim);
            }
        }

        private static Button FindAction(Button[] buttons, UnityEngine.Object target, string method)
        {
            return buttons.FirstOrDefault(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount())
                .Any(i => b.onClick.GetPersistentTarget(i) == target && b.onClick.GetPersistentMethodName(i) == method));
        }

        private static Sprite[] ImportAtlas()
        {
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Council UI 아틀라스가 없습니다.");
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            string[] names = { "CouncilLogo", "CouncilButton", "SingleIcon", "MultiIcon", "RulesIcon", "SettingsIcon", "FeedbackIcon", "ExitIcon" };
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(SpritesPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAllAssetsAtPath(SpritesPath).OfType<Sprite>().ToArray();
            // Unity gives the main object the asset filename rather than its authored sprite name.
            // The importer explicitly makes the first piece (CouncilLogo) the main object.
            var logo = AssetDatabase.LoadMainAssetAtPath(SpritesPath) as Sprite;
            var resolved = new Sprite[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                resolved[i] = i == 0 ? logo : loaded.FirstOrDefault(s => s.name == names[i]);
                if (resolved[i] == null)
                    throw new InvalidOperationException("[Council UI] 소재를 불러오지 못했습니다: " + names[i]
                        + ". 실제 불러온 스프라이트: " + string.Join(", ", loaded.Select(s => s.name))
                        + ". 파일: " + SpritesPath);
            }
            if (resolved.Distinct().Count() != names.Length)
                throw new InvalidOperationException("[Council UI] 서로 다른 여덟 소재가 필요합니다. 아틀라스 분할 정의를 확인하세요.");
            Debug.Log("[Council UI] 로고 및 버튼/아이콘 소재 8개 확인 완료.");
            return resolved;
        }

        private static void Style(Button button, Transform parent, Sprite frame, Sprite icon, string label, Vector2 position, bool compact, TMP_FontAsset font)
        {
            Undo.RecordObject(button, "Keep button action and restyle");
            Undo.SetTransformParent(button.transform, parent, "Title navigation layout");
            var rect = (RectTransform)button.transform;
            Undo.RecordObject(rect, "Button layout");
            Place(rect, position, compact ? new Vector2(112, 145) : new Vector2(740, 128));
            foreach (var text in button.GetComponentsInChildren<Text>(true))
            { Undo.RecordObject(text.gameObject, "Preserve old label"); text.gameObject.SetActive(false); }
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            { Undo.RecordObject(text.gameObject, "Preserve old label"); text.gameObject.SetActive(false); }
            var hit = button.GetComponent<Image>();
            if (hit == null) hit = Undo.AddComponent<Image>(button.gameObject);
            Undo.RecordObject(hit, "Button hit graphic");
            hit.sprite = null; hit.color = Color.clear; hit.raycastTarget = true;
            var visual = Rect("CouncilVisual", rect, Vector2.zero, rect.sizeDelta);
            Undo.RegisterCreatedObjectUndo(visual.gameObject, "Button visuals");
            Image graphic;
            if (compact) graphic = Art("Icon", visual, icon, new Vector2(0, 22), new Vector2(94, 94));
            else
            {
                graphic = Art("Frame", visual, frame, Vector2.zero, rect.sizeDelta);
                Art("Icon", visual, icon, new Vector2(-215, 0), new Vector2(110, 94));
            }
            var title = Rect("CouncilLabel", visual, compact ? new Vector2(0, -49) : new Vector2(45, 0),
                compact ? new Vector2(140, 38) : new Vector2(440, 70)).gameObject.AddComponent<TextMeshProUGUI>();
            title.font = font; title.text = label; title.fontSize = compact ? 24 : 38;
            title.fontStyle = FontStyles.Bold; title.color = Ivory; title.richText = false;
            title.alignment = TextAlignmentOptions.Center; title.raycastTarget = false;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            button.targetGraphic = graphic; button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 1, .88f);
            colors.pressedColor = new Color(.7f, .85f, .8f);
            colors.disabledColor = new Color(.4f, .4f, .4f, .65f); colors.fadeDuration = .12f;
            button.colors = colors;
            var rim = graphic.gameObject.AddComponent<Outline>();
            rim.effectColor = Color.clear; rim.effectDistance = new Vector2(2, -2); rim.useGraphicAlpha = true;
            var hover = Undo.AddComponent<CouncilButtonFeedback>(button.gameObject);
            Ref(hover, "_visual", visual); Ref(hover, "_rim", rim);
            if (button.GetComponent<ButtonClickSound>() == null) Undo.AddComponent<ButtonClickSound>(button.gameObject);
        }

        private static SettingsPanelController EnsureSettings(Scene lobbyScene, Transform parent)
        {
            var existing = lobbyScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SettingsPanelController>(true)).FirstOrDefault();
            if (existing != null) return existing;
            var gameScene = SceneManager.GetSceneByPath("Assets/Scenes/GameScene.unity");
            bool opened = !gameScene.isLoaded;
            if (opened) gameScene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Additive);
            try
            {
                var source = gameScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SettingsPanelController>(true)).FirstOrDefault();
                if (source == null) throw new InvalidOperationException("GameScene의 SettingsPanelController가 없습니다.");
                var clone = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
                clone.name = "TitleSettingsPanel";
                clone.SetActive(false);
                Undo.RegisterCreatedObjectUndo(clone, "Reuse existing game settings");
                var rect = clone.GetComponent<RectTransform>();
                if (rect != null) Stretch(rect);
                var overlay = clone.GetComponent<Canvas>();
                if (overlay == null) overlay = clone.AddComponent<Canvas>();
                overlay.overrideSorting = true; overlay.sortingOrder = 450;
                if (clone.GetComponent<GraphicRaycaster>() == null) clone.AddComponent<GraphicRaycaster>();
                return clone.GetComponent<SettingsPanelController>();
            }
            finally { if (opened) EditorSceneManager.CloseScene(gameScene, true); }
        }

        private static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
        { var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); Place(rect, position, size); return rect; }
        private static Image Art(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        { var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return image; }
    }
}
#endif
