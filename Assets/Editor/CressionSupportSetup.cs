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
            content.pages = new[]
            {
                Page("기본 진행", "목표: 예측한 승리 횟수를 정확히 맞춰 점수를 모으세요.\n\n총 10라운드입니다. 1라운드는 1장, 2라운드는 2장…10라운드는 10장씩 받습니다.\n\n1. 손패를 보고 이번 라운드에 이길 횟수를 예측합니다(비딩).\n2. 선부터 순서대로 한 장씩 냅니다. 모두 한 장씩 낸 묶음을 ‘트릭’이라 합니다.\n3. 트릭의 승자가 승리 횟수 1회를 얻고 다음 트릭의 선이 됩니다.\n4. 손패를 모두 쓰면 예측과 실제 승리 횟수에 따라 점수를 계산합니다.\n5. 10라운드 종료 후 누적 점수가 높은 사람이 승리합니다.\n\n도움말·피드백 창을 열어도 게임은 멈추지 않습니다. 멀티플레이에서는 내 차례를 놓치지 않도록 주의하세요."),
                Page("숫자 카드와 제출", "숫자는 1~14입니다.\n\n트릭에서 처음 나온 숫자 카드의 색이 ‘리드 색’입니다. 그 색의 숫자 카드가 손에 있으면, 숫자 카드를 낼 때는 그 색을 따라야 합니다. 해당 색이 없으면 다른 색을 낼 수 있습니다. 특수 카드는 언제든 낼 수 있습니다.\n\n일반 판정\n• 검정 숫자 카드는 다른 색 숫자보다 강합니다.\n• 검정이 없다면 리드 색의 숫자 중 가장 높은 숫자가 이깁니다.\n• 같은 색은 큰 숫자가 이깁니다. 숫자 동점은 먼저 낸 카드가 우선입니다.\n\n천상강림 룰이 적용되면 색과 검정의 우위를 무시하고 오직 숫자만 비교합니다. 단, 리드 색을 따라 제출하는 의무는 그대로입니다.\n\n마우스로 손패를 선택해 제출합니다. 카드 위에서 Alt를 누르면 크게 볼 수 있습니다."),
                Page("특수 카드", "해적: 숫자 카드와 인어를 이깁니다.\n스컬킹: 해적과 숫자 카드를 이깁니다.\n인어: 스컬킹을 잡을 수 있습니다. 스컬킹이 없는 트릭에서는 해적에게 집니다.\n같은 역할의 특수 카드끼리는 먼저 낸 카드가 우선입니다.\n\n타이그리스: 제출 시 해적 또는 항복 역할을 선택합니다.\n항복: 보통 다른 카드에 집니다. 모두 항복 역할이면 먼저 낸 사람이 이깁니다.\n특수 항복(보석): 항복 역할이며, 유효한 트릭의 승자와 동맹을 맺습니다. 자신이 승자이면 자신과 동맹을 맺지 않습니다.\n\n크라켄: 트릭을 무효로 만들어 아무도 승리 횟수를 얻지 못합니다.\n천상강림(백경): 색과 특수 능력 대신 최고 숫자로 승부합니다. 숫자가 하나도 없으면 천상강림을 낸 사람이 이깁니다.\n\n크라켄과 천상강림이 같이 나오면 다음 페이지의 특별 규칙을 적용합니다."),
                Page("크라켄 · 천상강림", "두 카드 중 나중에 제출된 카드의 룰이 우선입니다.\n\n천상강림 → 크라켄\n크라켄 룰: 트릭 무효. 아무도 승리 횟수를 얻지 못합니다. 다음 선은 크라켄을 제외하고 판정했을 때 이겼을 사람이 맡습니다. 천상강림이 남아 있으므로 최고 숫자를 낸 사람이 다음 선이 됩니다.\n\n크라켄 → 천상강림\n천상강림 룰: 무효가 취소되고 정상적으로 승자를 정합니다. 최고 숫자를 낸 사람이 승리 횟수 1회를 얻고 다음 선이 됩니다.\n\n이후 일반 카드가 나와도 선택된 룰은 유지됩니다. 숫자 동점은 먼저 낸 카드가 우선이며, 숫자가 없으면 천상강림 제출자가 선택됩니다.\n\n예: 검정 2 / 초록 14 / 크라켄 / 천상강림\n→ 초록 14 승리.\n같은 조합에서 크라켄이 천상강림보다 나중이면\n→ 트릭 무효, 초록 14가 다음 선."),
                Page("점수와 보석 동맹", "예측 횟수 = 실제 승리 횟수이면 성공입니다.\n\n1회 이상 예측\n성공: 예측 횟수 × 20점\n실패: 예측과 실제 횟수의 차이 × -10점\n예: 3회 예측·3회 승리 → +60점\n예: 3회 예측·1회 승리 → -20점\n\n0회 예측\n성공: 라운드 번호 × 10점\n실패: 라운드 번호 × -10점\n예: 5라운드 0회 성공 → +50점, 실패 → -50점\n\n보석 동맹\n같은 라운드에 동맹을 맺은 양쪽이 모두 예측에 성공하면 각자 동맹당 +20점을 받습니다. 한쪽이라도 실패하면 동맹 점수는 없습니다. 동맹은 다음 라운드에 초기화됩니다.\n\n누적 점수는 0점 미만으로 내려가지 않습니다. 특수 카드 포획 보너스는 사용하지 않습니다.")
            };
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
