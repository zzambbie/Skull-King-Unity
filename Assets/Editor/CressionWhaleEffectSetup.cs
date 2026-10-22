#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SkullKing.UI;

namespace SkullKing.Editor
{
    public static class CressionWhaleEffectSetup
    {
        private const string WingPath = "Assets/Art/Effects/WhiteWhale/heaven-descent-wings-v2.png";
        private const string PrefabPath = "Assets/Prefabs/Effects/WhiteWhaleWings.prefab";

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[Whale] Play 모드를 종료하세요."); return; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/GameScene.unity")
            { Debug.LogWarning("[Whale] GameScene을 열고 다시 실행하세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || scene.isDirty) return;
            var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UIBindingManager>(true)).FirstOrDefault();
            if (ui == null) { Debug.LogError("[Whale] GameScene에 UIBindingManager가 없습니다."); return; }
            var so = new SerializedObject(ui);
            var existing = so.FindProperty("_whaleWingTransition").objectReferenceValue as WhaleWingTransition;
            var area = so.FindProperty("_trickArea").objectReferenceValue as Transform;
            if (area == null) { Debug.LogError("[Whale] UIBindingManager의 Trick Area를 연결하세요."); return; }
            var canvas = area.GetComponentInParent<Canvas>();
            if (canvas == null) { Debug.LogError("[Whale] TrickArea의 부모 Canvas가 없습니다."); return; }
            canvas = canvas.rootCanvas;
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(WingPath) as TextureImporter;
            if (importer == null) { Debug.LogError("[Whale] 날개 이미지의 Import가 끝났는지 확인하세요."); return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WingPath);
            if (sprite == null) { Debug.LogError("[Whale] Sprite Import 실패."); return; }

            string backup = "AudioDrafts/SceneBackups/Whale_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path, Path.Combine(backup, "GameScene.unity"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = CreatePrefab(sprite);
            else
            {
                var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    ConfigureFullScreen(contents.GetComponent<WhaleWingTransition>());
                    PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Connect White Whale Wings");
            var instance = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            if (existing == null) Undo.RegisterCreatedObjectUndo(instance, "White Whale Wings");
            else Undo.SetTransformParent(instance.transform, canvas.transform, "Full-screen whale overlay");
            ConfigureFullScreen(instance.GetComponent<WhaleWingTransition>());
            so.Update();
            so.FindProperty("_whaleWingTransition").objectReferenceValue = instance.GetComponent<WhaleWingTransition>();
            so.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
            Debug.Log("[Whale] 전체 화면 연출 연결/갱신 완료. GameCanvas/WhiteWhaleWings에서 속도·크기를 조절하세요. 원본 씬 백업: " + backup);
        }

        private static GameObject CreatePrefab(Sprite sprite)
        {
            Directory.CreateDirectory("Assets/Prefabs/Effects");
            AssetDatabase.Refresh();
            var root = Rect("WhiteWhaleWings", null);
            try
            {
                var effect = root.gameObject.AddComponent<WhaleWingTransition>();
                var curtain = Rect("WingCurtain", root);
                var group = curtain.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                group.alpha = 0f;
                var cover = Picture("InnerCover", curtain, null);
                cover.color = new Color(1f, .94f, .78f, 0f);
                cover.rectTransform.sizeDelta = new Vector2(112f, 163f);
                var left = Picture("LeftWing", curtain, sprite);
                var right = Picture("RightWing", curtain, sprite);
                var settings = new SerializedObject(effect);
                settings.FindProperty("_visualRoot").objectReferenceValue = curtain;
                settings.FindProperty("_group").objectReferenceValue = group;
                settings.FindProperty("_leftWing").objectReferenceValue = left;
                settings.FindProperty("_rightWing").objectReferenceValue = right;
                settings.FindProperty("_innerCover").objectReferenceValue = cover;
                settings.ApplyModifiedPropertiesWithoutUndo();
                ConfigureFullScreen(effect);
                curtain.gameObject.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(240f, 300f);
            return rect;
        }

        private static void ConfigureFullScreen(WhaleWingTransition effect)
        {
            var settings = new SerializedObject(effect);
            bool upgrade = settings.FindProperty("_layoutVersion").intValue < 3;
            if (upgrade)
            {
                settings.FindProperty("_closeSeconds").floatValue = 2.4f;
                settings.FindProperty("_coveredSeconds").floatValue = .7f;
                settings.FindProperty("_openSeconds").floatValue = 1.6f;
                settings.FindProperty("_wingSize").vector2Value = new Vector2(1.65f, 3.2f);
                settings.FindProperty("_openOffset").floatValue = 1.25f;
                settings.FindProperty("_closedOffset").floatValue = .15f;
                settings.FindProperty("_openAngle").floatValue = 12f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            Stretch(effect.transform as RectTransform);
            // Unity missing-component wrappers can be CLR-non-null but compare equal to null.
            // The ?? operator skips AddComponent in that case and the next access throws.
            var canvas = effect.GetComponent<Canvas>();
            if (canvas == null) canvas = effect.gameObject.AddComponent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("전체 화면 Canvas를 추가하지 못했습니다.");
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32000;
            if (effect.GetComponent<GraphicRaycaster>() == null) effect.gameObject.AddComponent<GraphicRaycaster>();
            Canvas.ForceUpdateCanvases();
            // In the isolated prefab scene this is a root Canvas, not a nested one.
            // Apply the nested override again only after it is actually parented in GameCanvas.
            if (!canvas.isRootCanvas) canvas.overrideSorting = true;
            var visual = settings.FindProperty("_visualRoot").objectReferenceValue as RectTransform;
            var cover = settings.FindProperty("_innerCover").objectReferenceValue as Image;
            if (visual == null || cover == null) throw new InvalidOperationException("날개 프리팹의 UI 참조가 누락됐습니다.");
            Stretch(visual);
            Stretch(cover.rectTransform);
            cover.name = "FullScreenWhiteLight";
            cover.sprite = null;
            cover.type = Image.Type.Simple;
            cover.color = new Color(1f, 1f, 1f, 0f);
            cover.raycastTarget = true;
            cover.transform.SetAsLastSibling();
            var group = settings.FindProperty("_group").objectReferenceValue as CanvasGroup;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            visual.gameObject.SetActive(false);
            // Mark migration complete only after every required component/reference is valid.
            settings.Update();
            settings.FindProperty("_layoutVersion").intValue = 3;
            settings.ApplyModifiedPropertiesWithoutUndo();
            ValidateFullScreen(effect);
            EditorUtility.SetDirty(effect);
        }

        private static void ValidateFullScreen(WhaleWingTransition effect)
        {
            var canvas = effect.GetComponent<Canvas>();
            if (canvas == null)
                throw new InvalidOperationException("날개 Canvas가 없습니다.");
            if (effect.GetComponent<GraphicRaycaster>() == null)
                throw new InvalidOperationException("날개 GraphicRaycaster가 없습니다.");
            // overrideSorting is a nested-Canvas feature. Testing it on an isolated
            // prefab root produces a false failure before the scene migration can run.
            if (!canvas.isRootCanvas && !canvas.overrideSorting)
                throw new InvalidOperationException("GameCanvas 아래 날개 Canvas의 Override Sorting이 꺼져 있습니다.");
            if (canvas.sortingOrder != 32000)
                throw new InvalidOperationException("날개 Canvas의 Sorting Order가 32000이 아닙니다.");
            var settings = new SerializedObject(effect);
            var visual = settings.FindProperty("_visualRoot").objectReferenceValue as RectTransform;
            var light = settings.FindProperty("_innerCover").objectReferenceValue as Image;
            var group = settings.FindProperty("_group").objectReferenceValue as CanvasGroup;
            if (visual == null || light == null || group == null ||
                settings.FindProperty("_layoutVersion").intValue != 3 ||
                light.rectTransform.anchorMin != Vector2.zero || light.rectTransform.anchorMax != Vector2.one)
                throw new InvalidOperationException("날개 UI 참조 또는 전체 화면 가림 설정이 누락됐습니다.");
        }

        public static void RunSetupChecks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new InvalidOperationException("먼저 날개 적용 메뉴를 실행해 주세요.");
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var effect = contents.GetComponent<WhaleWingTransition>();
                // Exercise the old-prefab migration path even if the saved prefab is already upgraded.
                var canvas = contents.GetComponent<Canvas>();
                var raycaster = contents.GetComponent<GraphicRaycaster>();
                if (raycaster != null) UnityEngine.Object.DestroyImmediate(raycaster);
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
                var settings = new SerializedObject(effect);
                settings.FindProperty("_layoutVersion").intValue = 0;
                settings.ApplyModifiedPropertiesWithoutUndo();
                ConfigureFullScreen(effect);
                ValidateFullScreen(effect);
                ConfigureFullScreen(effect);
                ValidateFullScreen(effect);
                // Also test the other context: instantiated beneath a real parent Canvas.
                var parent = new GameObject("WhaleSetupTestCanvas", typeof(RectTransform), typeof(Canvas));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(parent, contents.scene);
                try
                {
                    contents.transform.SetParent(parent.transform, false);
                    ConfigureFullScreen(effect);
                    ValidateFullScreen(effect);
                    if (contents.GetComponent<Canvas>().isRootCanvas)
                        throw new InvalidOperationException("Nested Canvas 검사 환경이 잘못됐습니다.");
                }
                finally
                {
                    contents.transform.SetParent(null, false);
                    UnityEngine.Object.DestroyImmediate(parent);
                }
                if (contents.GetComponents<Canvas>().Length != 1 || contents.GetComponents<GraphicRaycaster>().Length != 1)
                    throw new InvalidOperationException("전체 화면 컴포넌트가 중복 생성됐습니다.");
                Debug.Log("[Whale Setup Checks] Canvas 없는 프리팹 복구·Root/Nested Canvas 검증·전체 화면 참조·재실행 중복 방지 통과. 원본 프리팹은 변경하지 않았습니다.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
        private static Image Picture(string name, Transform parent, Sprite sprite)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            // Stretch inside the wing rectangle, making the feather curtain broad enough to cover a card.
            image.preserveAspect = false;
            return image;
        }
    }
}
#endif
