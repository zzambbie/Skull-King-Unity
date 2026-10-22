#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using SkullKing.UI;

namespace SkullKing.Editor
{
    public static class CressionTrickHistorySetup
    {
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Trick History] Play 모드를 종료하세요."); return; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/GameScene.unity") { Debug.LogWarning("[Trick History] GameScene을 열어 주세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UIBindingManager>(true)).FirstOrDefault();
            if (ui == null) throw new InvalidOperationException("UIBindingManager를 찾을 수 없습니다.");
            var settings = new SerializedObject(ui);
            var existing = settings.FindProperty("_trickHistory").objectReferenceValue as TrickHistoryView;
            var area = settings.FindProperty("_trickArea").objectReferenceValue as Transform;
            var cardPrefab = settings.FindProperty("_cardPrefab").objectReferenceValue as GameObject;
            var database = settings.FindProperty("_spriteDatabase").objectReferenceValue;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/malgun SDF.asset");
            if (area == null || cardPrefab == null || font == null) throw new InvalidOperationException("TrickArea / CardPrefab / malgun SDF 연결을 확인하세요.");
            string backup = "ProjectBackups/TrickHistory/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path, Path.Combine(backup, "GameScene.unity"));
            if (existing != null)
            {
                Undo.IncrementCurrentGroup();
                int upgradeUndo = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Upgrade history drawer");
                try
                {
                    UpgradeDrawer(existing, font);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Undo.CollapseUndoOperations(upgradeUndo);
                    Selection.activeGameObject = existing.gameObject;
                    Debug.Log("[Trick History] 상단 접이식 기록 창 적용 완료. 씬 백업: " + backup);
                }
                catch { Undo.RevertAllDownToGroup(upgradeUndo); throw; }
                return;
            }
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Public Trick History");
            var root = Rect("TrickHistory", area, new Vector2(0f, -185f), new Vector2(700f, 210f));
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Trick History UI");
            var view = root.gameObject.AddComponent<TrickHistoryView>();
            try
            {
                var title = Label("Title", root, "이번 트릭", new Vector2(-240, 113), new Vector2(290, 32), font, 18);
                var hint = Label("Hint", root, "제출 순서 · 마우스/Alt로 확인", new Vector2(0, -108), new Vector2(660, 24), font, 14);
                var current = TextButton("CurrentTrickButton", root, "이번", new Vector2(200, 113), font, view.ShowCurrent);
                var previous = TextButton("PreviousTrickButton", root, "이전", new Vector2(290, 113), font, view.ShowPrevious);
                var slots = new TrickHistoryEntry[6];
                for (int i = 0; i < slots.Length; i++)
                {
                    var slot = Rect("Entry" + (i + 1), root, new Vector2((i - 2.5f) * 114f, 0f), new Vector2(108, 200));
                    var entry = slot.gameObject.AddComponent<TrickHistoryEntry>();
                    var owner = Label("Submitter", slot, (i + 1) + ". 제출자", new Vector2(0, 80), new Vector2(108, 36), font, 16);
                    owner.overflowMode = TextOverflowModes.Ellipsis;
                    var state = Label("LeaderState", slot, "", new Vector2(0, -83), new Vector2(108, 25), font, 16);
                    var cardObject = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, slot);
                    var cardRect = cardObject.GetComponent<RectTransform>();
                    cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(.5f, .5f);
                    cardRect.anchoredPosition = new Vector2(0, -4);
                    cardRect.sizeDelta = new Vector2(120f, 120f * 19f / 13f);
                    cardRect.localScale = Vector3.one * .65f;
                    cardRect.localRotation = Quaternion.identity;
                    Ref(entry, "_card", cardObject.GetComponent<CardUI>());
                    Ref(entry, "_owner", owner); Ref(entry, "_state", state);
                    Ref(entry, "_inspectionHint", hint);
                    slots[i] = entry;
                    slot.gameObject.SetActive(false);
                }
                Ref(view, "_title", title); Ref(view, "_hint", hint); Ref(view, "_database", database);
                Ref(view, "_currentButton", current); Ref(view, "_previousButton", previous);
                var viewSettings = new SerializedObject(view);
                var entries = viewSettings.FindProperty("_entries");
                entries.arraySize = slots.Length;
                for (int i = 0; i < slots.Length; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                viewSettings.ApplyModifiedPropertiesWithoutUndo();
                settings.Update(); settings.FindProperty("_trickHistory").objectReferenceValue = view; settings.ApplyModifiedProperties();
                UpgradeDrawer(view, font);
                Undo.CollapseUndoOperations(undo);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = root.gameObject;
                Debug.Log("[Trick History] 연결 완료. GameCanvas/TrickHistory에서 상단 기록 창을 편집하세요. 씬 백업: " + backup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                throw;
            }
        }
        private static void UpgradeDrawer(TrickHistoryView view, TMP_FontAsset font)
        {
            var so = new SerializedObject(view);
            if (so.FindProperty("_layoutVersion").intValue >= 2) return;
            var root = (RectTransform)view.transform;
            var canvas = view.GetComponentInParent<Canvas>().rootCanvas;
            Undo.RecordObject(root, "Move history drawer");
            Undo.SetTransformParent(root, canvas.transform, "Move history drawer");
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, 1);
            root.anchoredPosition = new Vector2(0, -52);
            root.sizeDelta = new Vector2(740, 0);
            root.localScale = Vector3.one;
            root.SetAsLastSibling();
            var children = root.Cast<Transform>().ToArray();
            var drawer = Rect("Drawer", root, Vector2.zero, new Vector2(740, 280));
            Undo.RegisterCreatedObjectUndo(drawer.gameObject, "History drawer");
            drawer.anchorMin = drawer.anchorMax = drawer.pivot = new Vector2(.5f, 1);
            var background = drawer.gameObject.AddComponent<Image>();
            background.color = new Color(.025f, .035f, .055f, .96f);
            background.raycastTarget = true;
            var group = drawer.gameObject.AddComponent<CanvasGroup>();
            foreach (var child in children)
            {
                var rect = child as RectTransform;
                Vector2 position = rect != null ? rect.anchoredPosition : Vector2.zero;
                Undo.SetTransformParent(child, drawer, "Keep history contents");
                if (rect != null) rect.anchoredPosition = position;
            }
            var title = so.FindProperty("_title").objectReferenceValue as TMP_Text;
            if (title != null) { title.rectTransform.anchoredPosition = new Vector2(-130, 113); title.rectTransform.sizeDelta = new Vector2(430, 32); }
            var previous = so.FindProperty("_previousButton").objectReferenceValue as Button;
            var current = so.FindProperty("_currentButton").objectReferenceValue as Button;
            if (previous != null) previous.GetComponent<RectTransform>().anchoredPosition = new Vector2(125, 113);
            if (current != null)
            {
                current.GetComponent<RectTransform>().anchoredPosition = new Vector2(295, 113);
                current.GetComponent<TMP_Text>().text = "현재";
            }
            var next = TextButton("NextTrickButton", drawer, "다음", new Vector2(210, 113), font, view.ShowNext);
            var toggle = TextButton("ToggleHistoryButton", root, "▼ 제출 기록", new Vector2(0, 26), font, view.ToggleDrawer);
            toggle.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 42);
            Undo.RegisterCreatedObjectUndo(toggle.gameObject, "History toggle");
            Ref(view, "_drawer", drawer); Ref(view, "_drawerGroup", group);
            Ref(view, "_toggleLabel", toggle.GetComponent<TMP_Text>()); Ref(view, "_nextButton", next);
            so.Update(); so.FindProperty("_layoutVersion").intValue = 2; so.ApplyModifiedPropertiesWithoutUndo();
            drawer.gameObject.SetActive(false);
        }
        private static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static TMP_Text Label(string name, Transform parent, string text, Vector2 pos, Vector2 size, TMP_FontAsset font, int fontSize)
        {
            var label = Rect(name, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = fontSize; label.text = text; label.richText = false;
            label.color = Color.white; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private static Button TextButton(string name, Transform parent, string text, Vector2 pos, TMP_FontAsset font, UnityAction action)
        {
            var label = Label(name, parent, text, pos, new Vector2(80, 36), font, 19);
            label.raycastTarget = true;
            var button = label.gameObject.AddComponent<Button>(); button.targetGraphic = label;
            var colors = button.colors;
            colors.normalColor = new Color(.85f, .88f, .93f);
            colors.highlightedColor = new Color(1f, .86f, .4f);
            colors.pressedColor = new Color(.8f, .6f, .15f);
            colors.disabledColor = new Color(.4f, .43f, .47f);
            button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            label.gameObject.AddComponent<SkullKing.Audio.ButtonClickSound>();
            return button;
        }
    }
}
#endif
