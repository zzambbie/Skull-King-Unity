#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SkullKing.Data;
using SkullKing.Logic;
using SkullKing.UI;

namespace SkullKing.Editor
{
    /// <summary>Read-only rulebook checks. Scene reviews use disposable in-memory scene instances, never save scenes.</summary>
    public static class CressionRulebookChecks
    {
        public static int RunChecks()
        {
            int checks = 0;
            var content = AssetDatabase.LoadAssetAtPath<RulebookContent>("Assets/Settings/CrossionRulebook.asset");
            Need(content != null && content.illustratedLayout, "Illustrated guide asset", ref checks);
            Need(content.pages != null && content.pages.Length == 12, "Twelve short pages", ref checks);
            string[] obsolete = { "해적", "인어", "스컬킹", "크라켄", "고래", "백경", "타이그리스", "티그리스" };
            foreach (var page in content.pages)
            {
                Need(page != null && !string.IsNullOrWhiteSpace(page.title) && !string.IsNullOrWhiteSpace(page.summary), "Page heading", ref checks);
                Need(!string.IsNullOrWhiteSpace(page.body) && page.body.Length <= 390, "Short page body", ref checks);
                Need(page.illustration != null, "Bound illustration: " + page.title, ref checks);
                string text = page.title + page.summary + page.body + page.illustrationCaption + page.exampleA + page.exampleB + page.exampleC + page.exampleResult;
                Need(!obsolete.Any(text.Contains), "CROSSION-only terms: " + page.title, ref checks);
                Need(!string.IsNullOrEmpty(page.exampleA) && !string.IsNullOrEmpty(page.exampleB) && !string.IsNullOrEmpty(page.exampleC), "Three visual examples", ref checks);
            }
            var roles = new[] { SpecialType.Pirate, SpecialType.Mermaid, SpecialType.SkullKing, SpecialType.WhiteWhale, SpecialType.Kraken, SpecialType.Tigress };
            var names = new[] { "무협", "판타지", "사이버펑크", "천사", "종말", "암살자" };
            for (int i = 0; i < roles.Length; i++)
                Need(CardData.CreateSpecialCard(i, roles[i]).DisplayName == names[i], "Role name " + names[i], ref checks);
            Need((int)SpecialType.SkullKing == 1 && (int)SpecialType.Pirate == 3 && (int)SpecialType.WhiteWhale == 6, "Stable protocol enum values", ref checks);

            var deck = new Deck(20261022);
            deck.CreateFullDeck();
            Need(deck.TotalCount == 72, "Unchanged 72-card deck", ref checks);
            var cards = deck.Draw(72);
            Need(cards.Count(c => c.IsNumberCard) == 56, "56 numbers", ref checks);
            Need(cards.Count(c => c.Special == SpecialType.Pirate) == 5 && cards.Count(c => c.Special == SpecialType.Mermaid) == 2, "Original role counts", ref checks);
            Need(cards.Count(c => c.Special == SpecialType.Jewel) == 0, "No unshipped alliance in default deck", ref checks);
            Need(cards.Where(c => c.IsSpecialCard).All(c => c.DisplayName == CrossionCardNames.Role(c.Special)), "Deck uses canonical names", ref checks);
            Need(CardData.CreateNumberCard(0, CardSuit.Green, 7).DisplayName == "무협 7", "Numeric world label", ref checks);

            Need(Winner(SpecialType.SkullKing, SpecialType.Pirate) == 0, "Cyber beats Wuxia", ref checks);
            Need(Winner(SpecialType.Pirate, SpecialType.Mermaid) == 0, "Wuxia beats Fantasy", ref checks);
            Need(Winner(SpecialType.SkullKing, SpecialType.Mermaid) == 1, "Fantasy beats Cyber", ref checks);
            Need(Winner(SpecialType.Pirate, SpecialType.Pirate) == 0, "First equal role wins", ref checks);
            foreach (var order in Permutations())
            {
                var trick = new List<PlayedCard>();
                for (int i = 0; i < order.Length; i++) trick.Add(Played(i, order[i]));
                Need(trick[TrickResolver.Resolve(trick, CardSuit.None).WinnerIndex].Card.Special == SpecialType.Mermaid,
                    "All-three exception in every order", ref checks);
            }
            var normal = new List<PlayedCard> { Number(0, CardSuit.Green, 7), Number(1, CardSuit.Yellow, 14), Number(2, CardSuit.Green, 10) };
            Need(TrickResolver.Resolve(normal, CardSuit.Green).WinnerIndex == 2, "Guide's lead-world example", ref checks);
            var overrideTrick = new List<PlayedCard> { Number(0, CardSuit.Black, 2), Number(1, CardSuit.Green, 14), Played(2, SpecialType.Kraken), Played(3, SpecialType.WhiteWhale) };
            Need(TrickResolver.Resolve(overrideTrick, CardSuit.Black).WinnerIndex == 1, "Last Angel compares numbers", ref checks);
            overrideTrick.Reverse();
            Need(TrickResolver.Resolve(overrideTrick, CardSuit.Black).IsVoided, "Last Doom voids the trick", ref checks);
            var onlySpecial = new List<PlayedCard> { Played(0, SpecialType.Pirate), Played(1, SpecialType.WhiteWhale), Played(2, SpecialType.SkullKing) };
            Need(TrickResolver.Resolve(onlySpecial, CardSuit.None).WinnerIndex == 1, "Angel wins if no numeric card", ref checks);
            var hand = new List<CardData> { CardData.CreateNumberCard(0, CardSuit.Green, 5), CardData.CreateNumberCard(1, CardSuit.Yellow, 14), CardData.CreateSpecialCard(2, SpecialType.WhiteWhale) };
            Need(TrickResolver.GetPlayableCards(hand, CardSuit.Green).Count == 2, "Follow-world and special exception unchanged", ref checks);
            Score(3, 3, 5, 60, ref checks);
            Score(3, 1, 5, -20, ref checks);
            Score(3, 4, 5, -10, ref checks);
            Score(0, 0, 5, 50, ref checks);
            Score(0, 2, 5, -50, ref checks);
            var loser = new PlayerData(0, "test", true) { Bid = 0, TricksWon = 1, TotalScore = 20 };
            ScoreCalculator.CalculateAll(new[] { loser }, 5);
            Need(loser.TotalScore == 0, "Zero score floor", ref checks);
            return checks;
        }

        public static void RunBatchReview()
        {
            try
            {
                int checks = RunChecks();
                checks += CressionWhalePresentationChecks.RunChecks();
                string output = "Tools/Verification/Output/Rulebook-v1";
                Directory.CreateDirectory(output);
                foreach (string path in new[] { "Assets/Scenes/LobbyScene.unity", "Assets/Scenes/GameScene.unity" })
                    checks += ReviewScene(path, output);
                Debug.Log("[CROSSION Rulebook] " + checks + " checks passed; preview PNGs: " + output + ". No scene files were saved.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static int ReviewScene(string path, string output)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int checks = 0;
            try
            {
                var controller = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SupportWindowsController>(true)).Single();
                var serialized = new SerializedObject(controller);
                var body = (TMP_Text)serialized.FindProperty("_pageBody").objectReferenceValue;
                var title = (TMP_Text)serialized.FindProperty("_pageTitle").objectReferenceValue;
                var scroll = (ScrollRect)serialized.FindProperty("_helpScroll").objectReferenceValue;
                var panel = (GameObject)serialized.FindProperty("_helpPanel").objectReferenceValue;
                var canvas = controller.GetComponentInParent<Canvas>().rootCanvas;
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                canvas.transform.localScale = Vector3.one;
                var canvasRect = (RectTransform)canvas.transform;
                canvasRect.sizeDelta = new Vector2(1920, 1080);
                var cameraObject = new GameObject("Rulebook review camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.cameraType = CameraType.Game;
                camera.enabled = false;
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, -100);
                camera.orthographic = true;
                camera.orthographicSize = 540;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.015f, .025f, .04f);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 1000;
                canvas.worldCamera = camera;
                controller.ShowHelp();
                Need(controller.GetComponent<IllustratedRulebookView>().IsReady, "Runtime help view attached", ref checks);
                Need(!scroll.gameObject.activeSelf && body.transform.parent == title.transform.parent, "No scroll or masked body", ref checks);
                int childCount = title.transform.parent.childCount;
                for (int page = 0; page < 12; page++)
                {
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in panel.GetComponentsInChildren<TMP_Text>(true))
                        if (label.gameObject.activeInHierarchy) label.ForceMeshUpdate(true, true);
                    Canvas.ForceUpdateCanvases();
                    Need(body.preferredHeight <= body.rectTransform.rect.height + 1, "Body fits without scrolling: " + title.text, ref checks);
                    Need(!body.isTextOverflowing, "No text overflow: " + title.text, ref checks);
                    foreach (var label in panel.GetComponentsInChildren<TMP_Text>(false))
                        Need(!label.isTextOverflowing, "Caption fits: " + label.name + " / " + page, ref checks);
                    Capture(camera, 1920, 1080, Path.Combine(output, Path.GetFileNameWithoutExtension(path) + "-" + (page + 1).ToString("00") + ".png"));
                    controller.NextPage();
                }
                controller.Close();
                controller.ShowHelp();
                Need(title.transform.parent.childCount == childCount, "Reopening creates no duplicate decoration", ref checks);
                controller.PreviousPage();
                Need(title.text.Contains("0회"), "Previous from first wraps to last", ref checks);
                controller.NextPage();
                Need(title.text.Contains("승리 횟수"), "Next wraps to first", ref checks);
                canvasRect.sizeDelta = new Vector2(1280, 720);
                Canvas.ForceUpdateCanvases();
                controller.ShowHelp();
                Canvas.ForceUpdateCanvases();
                var window = (RectTransform)title.transform.parent;
                Need(window.rect.width * window.localScale.x <= 1280 && window.rect.height * window.localScale.y <= 720, "Fits 1280x720", ref checks);
                camera.orthographicSize = 360;
                Capture(camera, 1280, 720, Path.Combine(output, Path.GetFileNameWithoutExtension(path) + "-720p.png"));
                return checks;
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        private static void Capture(Camera camera, int width, int height, string path)
        {
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                var pixels = image.GetPixels32();
                int varied = 0;
                var first = pixels[0];
                for (int i = 0; i < pixels.Length; i += 127)
                    if (Math.Abs(pixels[i].r - first.r) + Math.Abs(pixels[i].g - first.g) + Math.Abs(pixels[i].b - first.b) > 12) varied++;
                if (varied < 100) throw new InvalidOperationException("Preview render was empty: " + path);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static PlayedCard Played(int player, SpecialType type) => new PlayedCard { PlayerIndex = player, Card = CardData.CreateSpecialCard(player, type) };
        private static PlayedCard Number(int player, CardSuit suit, int number) => new PlayedCard { PlayerIndex = player, Card = CardData.CreateNumberCard(player, suit, number) };
        private static int Winner(SpecialType a, SpecialType b) => TrickResolver.Resolve(new List<PlayedCard> { Played(0, a), Played(1, b) }, CardSuit.None).WinnerIndex;
        private static IEnumerable<SpecialType[]> Permutations()
        {
            var roles = new[] { SpecialType.Pirate, SpecialType.Mermaid, SpecialType.SkullKing };
            foreach (var a in roles) foreach (var b in roles) foreach (var c in roles)
                if (a != b && b != c && a != c) yield return new[] { a, b, c };
        }
        private static void Score(int bid, int wins, int round, int expected, ref int checks)
        {
            Need(ScoreCalculator.Calculate(new PlayerData(0, "test", true) { Bid = bid, TricksWon = wins }, round).RoundTotal == expected,
                "Guide score example " + bid + "/" + wins, ref checks);
        }
        private static void Need(bool condition, string name, ref int checks)
        {
            if (!condition) throw new InvalidOperationException("Rulebook check failed: " + name);
            checks++;
        }
    }
}
#endif
