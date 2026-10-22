#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SkullKing.UI;
using SkullKing.Audio;

namespace SkullKing.Editor
{
    public static class CressionSupportSetup
    {
        private static TMP_FontAsset _font;
        private static readonly Color Ink = new Color(.065f, .085f, .12f, .99f);
        private static readonly string[] Scenes = { "Assets/Scenes/LobbyScene.unity", "Assets/Scenes/GameScene.unity" };

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Support UI] Play 모드를 종료해 주세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                { Debug.LogWarning("[Support UI] 열린 씬을 먼저 저장해 주세요."); return; }
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/malgun SDF.asset");
            if (_font == null) throw new InvalidOperationException("Assets/Fonts/malgun SDF.asset 한글 폰트가 필요합니다.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            string backup = "ProjectBackups/SupportUI/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            foreach (string path in Scenes) File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
            try
            {
                RulebookContent content = Content();
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var roots = scene.GetRootGameObjects();
                    if (roots.Any(root => root.GetComponentInChildren<SupportWindowsController>(true) != null)) continue;
                    var canvas = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).FirstOrDefault(c => c.isRootCanvas);
                    if (canvas == null) throw new InvalidOperationException(path + "의 Canvas를 찾지 못했습니다.");
                    Build(canvas, content);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("씬 저장 실패: " + path);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[Support UI] 로비/게임 규칙·피드백 창 적용 완료. RulebookContent와 SupportWindows에서 수정하세요. 씬 백업: " + backup);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static RulebookContent Content()
        {
            const string path = "Assets/Settings/CrossionRulebook.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RulebookContent>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.Refresh();
            var content = ScriptableObject.CreateInstance<RulebookContent>();
            content.illustratedLayout = true;
            content.pages = RulebookDefaults.CreatePages();
            AssetDatabase.CreateAsset(content, path);
            return content;
        }
        private static RulebookContent.Page Page(string title, string body) => new RulebookContent.Page { title = title, body = body };

        private static void Build(Canvas parent, RulebookContent content)
        {
            var root = Rect("SupportWindows", parent.transform, Vector2.zero, Vector2.zero);
            Stretch(root);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var controller = root.gameObject.AddComponent<SupportWindowsController>();
            var help = Panel("RulesPanel", root, out var helpWindow);
            var feedback = Panel("FeedbackPanel", root, out var feedbackWindow);
            Label("Title", helpWindow, "게임 규칙", new Vector2(0, 285), new Vector2(730, 50), 30);
            var title = Label("PageTitle", helpWindow, "", new Vector2(0, 239), new Vector2(790, 42), 24);
            var scrollRect = Rect("RulesScroll", helpWindow, new Vector2(0, 0), new Vector2(800, 420));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", scrollRect, Vector2.zero, Vector2.zero); Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // A transparent viewport graphic makes wheel scrolling work over empty text areas.
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var body = Label("RulesBody", viewport, "", Vector2.zero, new Vector2(780, 0), 23);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.rectTransform.anchorMin = new Vector2(0, 1); body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.pivot = new Vector2(.5f, 1); body.rectTransform.sizeDelta = new Vector2(-20, 0);
            body.rectTransform.anchoredPosition = new Vector2(0, -5);
            var fit = body.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = body.rectTransform;
            var pageNumber = Label("PageNumber", helpWindow, "", new Vector2(0, -275), new Vector2(180, 40), 22);
            Button("PreviousPage", helpWindow, "이전", new Vector2(-230, -275), new Vector2(160, 48), controller.PreviousPage);
            Button("NextPage", helpWindow, "다음", new Vector2(230, -275), new Vector2(160, 48), controller.NextPage);
            Button("CloseRules", helpWindow, "닫기", new Vector2(380, 300), new Vector2(90, 40), controller.Close);

            Label("Title", feedbackWindow, "피드백", new Vector2(0, 285), new Vector2(730, 50), 30);
            Label("PrivacyNotice", feedbackWindow, "이 PC에만 저장합니다. 자동 전송되지 않습니다.\n개인정보·비밀번호는 적지 마세요. 창을 열어도 게임은 계속됩니다.", new Vector2(0, 225), new Vector2(810, 64), 18);
            var category = Button("CategoryButton", feedbackWindow, "", new Vector2(0, 165), new Vector2(650, 42), controller.NextCategory).GetComponentInChildren<TMP_Text>();
            var count = Label("CharacterCount", feedbackWindow, "0 / 4000", new Vector2(280, 125), new Vector2(230, 30), 18);
            var inputRect = Rect("FeedbackInput", feedbackWindow, new Vector2(0, -10), new Vector2(800, 245));
            var inputBackground = inputRect.gameObject.AddComponent<Image>();
            inputBackground.color = new Color(.13f, .16f, .21f);
            var inputViewport = Rect("TextViewport", inputRect, Vector2.zero, Vector2.zero); Stretch(inputViewport);
            inputViewport.offsetMin = new Vector2(14, 10); inputViewport.offsetMax = new Vector2(-14, -10);
            inputViewport.gameObject.AddComponent<RectMask2D>();
            var inputText = Label("Text", inputViewport, "", Vector2.zero, Vector2.zero, 22); Stretch(inputText.rectTransform);
            inputText.alignment = TextAlignmentOptions.TopLeft;
            var placeholder = Label("Placeholder", inputViewport, "어떤 상황이었는지, 기대한 동작과 실제 동작을 적어 주세요.", Vector2.zero, Vector2.zero, 22);
            Stretch(placeholder.rectTransform); placeholder.alignment = TextAlignmentOptions.TopLeft; placeholder.color = new Color(.6f, .65f, .7f);
            var input = inputRect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = inputBackground;
            input.textViewport = inputViewport; input.textComponent = inputText; input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.MultiLineNewline; input.characterLimit = 4000; input.richText = false;
            var status = Label("Status", feedbackWindow, "", new Vector2(0, -185), new Vector2(810, 90), 18);
            var save = Button("SaveFeedback", feedbackWindow, "파일로 저장", new Vector2(-260, -275), new Vector2(220, 48), controller.SaveFeedback);
            Button("CopyFeedback", feedbackWindow, "내용 복사", new Vector2(0, -275), new Vector2(220, 48), controller.CopyFeedback);
            Button("OpenFolder", feedbackWindow, "저장 폴더", new Vector2(260, -275), new Vector2(220, 48), controller.OpenFolder);
            Button("CloseFeedback", feedbackWindow, "닫기", new Vector2(380, 300), new Vector2(90, 40), controller.Close);
            Button("RulesButton", root, "규칙", Vector2.zero, new Vector2(140, 44), controller.ShowHelp, new Vector2(-255, -35));
            Button("FeedbackButton", root, "피드백", Vector2.zero, new Vector2(140, 44), controller.ShowFeedback, new Vector2(-95, -35));
            Ref(controller, "_helpPanel", help.gameObject); Ref(controller, "_rulebook", content);
            Ref(controller, "_pageTitle", title); Ref(controller, "_pageBody", body); Ref(controller, "_pageNumber", pageNumber); Ref(controller, "_helpScroll", scroll);
            Ref(controller, "_feedbackPanel", feedback.gameObject); Ref(controller, "_feedbackInput", input);
            Ref(controller, "_categoryText", category); Ref(controller, "_characterCount", count); Ref(controller, "_status", status); Ref(controller, "_saveButton", save);
            help.gameObject.SetActive(false); feedback.gameObject.SetActive(false);
        }
        private static RectTransform Panel(string name, Transform parent, out RectTransform window)
        {
            var panel = Rect(name, parent, Vector2.zero, Vector2.zero); Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .7f);
            window = Rect("Window", panel, Vector2.zero, new Vector2(900, 680));
            window.gameObject.AddComponent<Image>().color = Ink;
            return panel;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font; label.fontSize = fontSize; label.color = Color.white; label.text = text;
            label.richText = false; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private static Button Button(string name, Transform parent, string caption, Vector2 position, Vector2 size, UnityAction action, Vector2? topRight = null)
        {
            var rect = Rect(name, parent, position, size);
            if (topRight.HasValue) { rect.anchorMin = rect.anchorMax = Vector2.one; rect.anchoredPosition = topRight.Value; }
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.17f, .32f, .3f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Label("Text", rect, caption, Vector2.zero, size, 21); Stretch(label.rectTransform);
            UnityEventTools.AddPersistentListener(button.onClick, action);
            rect.gameObject.AddComponent<ButtonClickSound>();
            return button;
        }
        private static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
        { var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
