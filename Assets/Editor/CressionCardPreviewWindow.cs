using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SkullKing.Data;

namespace SkullKing.Editor
{
    /// <summary>
    /// Unity 안에서 원화 + 프레임 적용 결과를 한 화면에 확인하는 간단한 갤러리입니다.
    /// 실제 카드와 같은 13:19 비율 및 앞면 안전 영역을 사용합니다.
    /// </summary>
    public sealed class CressionCardPreviewWindow : EditorWindow
    {
        private const string DatabasePath = "Assets/CardSpriteDatabase.asset";
        private const float CardWidth = 130f;
        private const float CardHeight = CardWidth * 19f / 13f;

        private CardSpriteDatabase _database;
        private Vector2 _scroll;

        private readonly struct PreviewItem
        {
            public readonly string Label;
            public readonly Sprite Artwork;
            public readonly Sprite Frame;

            public PreviewItem(string label, Sprite artwork, Sprite frame)
            {
                Label = label;
                Artwork = artwork;
                Frame = frame;
            }
        }

        [MenuItem("CROSSION/Card Preview (Read Only)")]
        public static void Open()
        {
            GetWindow<CressionCardPreviewWindow>("CROSSION Cards");
        }

        private void OnEnable()
        {
            _database = AssetDatabase.LoadAssetAtPath<CardSpriteDatabase>(DatabasePath);
            minSize = new Vector2(620f, 480f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("CROSSION 카드 프레임 미리보기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "숫자 카드는 세계별 private 원화 4장과 얇게 조정한 프레임을 사용합니다. " +
                "실제 숫자와 위치·글꼴·색상은 CardPrefab의 Unity Text에서 설정합니다.", MessageType.Info);

            _database = (CardSpriteDatabase)EditorGUILayout.ObjectField(
                "Card Database", _database, typeof(CardSpriteDatabase), false);

            if (GUILayout.Button("GameScene 열기"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
            }

            if (_database == null)
            {
                EditorGUILayout.HelpBox("CardSpriteDatabase를 찾지 못했습니다.", MessageType.Error);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSection("무협 숫자 카드 1–14", BuildNumberItems("Wuxia", _database.greenSuitSprites));
            DrawSection("판타지 숫자 카드 1–14", BuildNumberItems("Fantasy", _database.yellowSuitSprites));
            DrawSection("사이버펑크 숫자 카드 1–14", BuildNumberItems("Cyberpunk", _database.purpleSuitSprites));
            DrawSection("공허 숫자 카드 1–14", BuildNumberItems("Void", _database.blackSuitSprites));
            DrawSection("프레임 적용 캐릭터", BuildFramedItems());
            var surrenderItems = new List<PreviewItem>();
            AddArray(surrenderItems, "항복", _database.escapeSprites);
            AddArray(surrenderItems, "특수항복", _database.allianceSprites);
            DrawSection("항복 / 특수항복 완성 카드", surrenderItems);
            DrawSection("프레임 제외 확인", BuildUnframedItems());
            EditorGUILayout.EndScrollView();
        }

        private List<PreviewItem> BuildFramedItems()
        {
            return new List<PreviewItem>
            {
                new("Wuxia Fighter", _database.pirate1Sprite, null),
                new("Wuxia Mesugaki", _database.pirate2Sprite, null),
                new("Wuxia Minstrel", _database.pirate3Sprite, null),
                new("Wuxia Strategist", _database.pirate4Sprite, null),
                new("Wuxia Swordmaster", _database.pirate5Sprite, null),
                new("Wuxia Special", _database.tigressSprite, null),
                new("MERMAID · Fantasy Hero", _database.mermaid1Sprite, null),
                new("MERMAID · Fantasy Satan", _database.mermaid2Sprite, null),
                new("SKULL KING · Cyberpunk Boss", _database.skullKingSprite, null),
                new("Kraken", _database.krakenSprite, null),
                new("천상강림", _database.whiteWhaleSprite, null)
            };
        }

        private static List<PreviewItem> BuildNumberItems(string world, Sprite[] sprites)
        {
            var items = new List<PreviewItem>();
            if (sprites == null) return items;

            string[] ranges = { "private 01 · 1–3", "private 02 · 4–6", "private 03 · 7–10", "private 04 · 11–14" };
            for (int i = 0; i < sprites.Length; i++)
            {
                string label = i < ranges.Length ? ranges[i] : $"group {i + 1}";
                items.Add(new PreviewItem($"{world} {label}", sprites[i], null));
            }
            return items;
        }

        private List<PreviewItem> BuildUnframedItems()
        {
            var items = new List<PreviewItem>
            {
                new("Card Back", _database.cardBackSprite, null)
            };

            return items;
        }

        private static void AddArray(List<PreviewItem> items, string label, Sprite[] sprites)
        {
            if (sprites == null) return;
            for (int i = 0; i < sprites.Length; i++)
                items.Add(new PreviewItem($"{label} {i + 1}", sprites[i], null));
        }

        private static void DrawSection(string title, List<PreviewItem> items)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            int columns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 35f) / (CardWidth + 14f)));
            for (int i = 0; i < items.Count; i += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    int end = Mathf.Min(i + columns, items.Count);
                    for (int j = i; j < end; j++)
                        DrawCard(items[j]);
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private static void DrawCard(PreviewItem item)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(CardWidth)))
            {
                Rect card = GUILayoutUtility.GetRect(CardWidth, CardHeight, GUILayout.Width(CardWidth), GUILayout.Height(CardHeight));
                EditorGUI.DrawRect(card, new Color(.08f, .08f, .09f));

                if (item.Artwork != null)
                {
                    Rect artworkRect = item.Frame == null
                        ? card
                        : new Rect(
                            card.x + card.width * .04f,
                            card.y + card.height * .09f,
                            card.width * .92f,
                            card.height * .898f);
                    GUI.DrawTexture(artworkRect, item.Artwork.texture, ScaleMode.ScaleAndCrop, true);
                }

                if (item.Frame != null)
                    GUI.DrawTexture(card, item.Frame.texture, ScaleMode.StretchToFill, true);

                GUILayout.Label(item.Label, EditorStyles.miniLabel, GUILayout.Width(CardWidth));
            }
        }
    }
}
