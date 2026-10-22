using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SkullKing.Data;

namespace SkullKing.Editor
{
    /// <summary>
    /// skullking 폴더에서 가져온 CROSSION 카드 원화를 Unity Sprite로 설정하고
    /// CardSpriteDatabase에 일괄 연결합니다.
    /// </summary>
    public static class CressionCardAssetSetup
    {
        private const string Root = "Assets/Art/Cards/CROSSION/";
        private const string DatabasePath = "Assets/CardSpriteDatabase.asset";

        public static void Configure()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ConfigureTexture(path);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            CardSpriteDatabase database = AssetDatabase.LoadAssetAtPath<CardSpriteDatabase>(DatabasePath);
            if (database == null)
                throw new InvalidOperationException($"CardSpriteDatabase를 찾을 수 없습니다: {DatabasePath}");

            database.greenSuitSprites = LoadNumberSet("Wuxia");
            database.yellowSuitSprites = LoadNumberSet("Fantasy");
            database.purpleSuitSprites = LoadNumberSet("Cyberpunk");
            database.blackSuitSprites = LoadNumberSet("Void");

            // 기존 단일 슬롯도 채워 두어 예전 씬/도구와 호환합니다.
            database.greenSuitSprite = database.greenSuitSprites.FirstOrDefault();
            database.yellowSuitSprite = database.yellowSuitSprites.FirstOrDefault();
            database.purpleSuitSprite = database.purpleSuitSprites.FirstOrDefault();
            database.blackSuitSprite = database.blackSuitSprites.FirstOrDefault();

            // 프레임 적용 캐릭터는 일러스트+프레임을 미리 합친 완성 카드 PNG를 사용합니다.
            database.pirate1Sprite = LoadComposed("Wuxia_fighter_card.png");
            database.pirate2Sprite = LoadComposed("Wuxia_mesugaki_card.png");
            database.pirate3Sprite = LoadComposed("Wuxia_Minstrel_card.png");
            database.pirate4Sprite = LoadComposed("Wuxia_Strategist_card.png");
            database.pirate5Sprite = LoadComposed("Wuxia_Swordmaster_card.png");
            database.mermaid1Sprite = LoadComposed("Fantasy_hero_card.png");
            database.mermaid2Sprite = LoadComposed("Fantasy_satan_card.png");
            database.skullKingSprite = LoadComposed("Cyberpunk_boss_card.png");
            database.tigressSprite = LoadComposed("Wuxia_special_card.png");
            database.krakenSprite = LoadComposed("Kraken_card.png");
            database.whiteWhaleSprite = LoadComposed("Heaven_card.png");

            database.escapeSprites = LoadSurrenderSet("surrender", 4);
            database.escapeSprite = database.escapeSprites.FirstOrDefault();
            database.cardBackSprite = Load("card-back.png");

            database.wuxiaFrameSprite = Load("wuxia-soldier-frame-v1-832x1216.png");
            database.fantasyFrameSprite = Load("fantasy-soldier-frame-v1-832x1216.png");
            // 두 원본은 중앙이 불투명한 검정이어서, 일러스트를 가리지 않도록
            // 검정 배경만 투명 처리한 파생본을 UI 레이어에 사용합니다.
            database.cyberpunkFrameSprite = Load("cyberpunk-soldier-frame-v1-832x1216.png");
            database.voidFrameSprite = Load("frame_void.png");

            database.allianceSprites = LoadSurrenderSet("alliance", 3);
            database.jewelSprite = database.allianceSprites.FirstOrDefault();
            database.allianceFrameSprites = Array.Empty<Sprite>();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[CROSSION] 카드 원화, 세계 프레임, 카드 뒷면 연결 완료");
        }

        public static void ConfigureSurrenderCards()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string prefix in new[] { "surrender", "alliance" })
            {
                int count = prefix == "surrender" ? 4 : 3;
                for (int i = 1; i <= count; i++)
                    ConfigureTexture(Root + $"Composed/{prefix}_{i:00}_card.png");
            }

            CardSpriteDatabase database = AssetDatabase.LoadAssetAtPath<CardSpriteDatabase>(DatabasePath);
            if (database == null)
                throw new InvalidOperationException("CardSpriteDatabase is missing.");

            database.escapeSprites = LoadSurrenderSet("surrender", 4);
            database.escapeSprite = database.escapeSprites.FirstOrDefault();
            database.allianceSprites = LoadSurrenderSet("alliance", 3);
            database.jewelSprite = database.allianceSprites.FirstOrDefault();
            // The border is baked into each PNG; no second frame overlay is needed.
            database.allianceFrameSprites = Array.Empty<Sprite>();

            Sprite[] cards = database.escapeSprites.Concat(database.allianceSprites).ToArray();
            if (cards.Any(sprite => sprite == null || sprite.rect.width != 832 || sprite.rect.height != 1216))
                throw new InvalidOperationException("Expected seven 832x1216 surrender card sprites.");
            for (int i = 0; i < 3; i++)
            {
                var card = CardData.CreateSpecialCard(72 + i, SpecialType.Jewel);
                if (database.GetSpriteForCard(card) != database.allianceSprites[i])
                    throw new InvalidOperationException("Special surrender artwork lookup failed.");
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[CROSSION] SURRENDER_CARDS_OK: 4 surrender + 3 special surrender sprites, 832x1216, lookup verified.");
        }

        public static void ConfigureLegendaryCards()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string name in new[] { "Kraken_card.png", "Heaven_card.png" })
                ConfigureTexture(Root + "Composed/" + name);

            CardSpriteDatabase database = AssetDatabase.LoadAssetAtPath<CardSpriteDatabase>(DatabasePath);
            if (database == null)
                throw new InvalidOperationException("CardSpriteDatabase is missing.");

            database.krakenSprite = LoadComposed("Kraken_card.png");
            database.whiteWhaleSprite = LoadComposed("Heaven_card.png");
            foreach (SpecialType type in new[] { SpecialType.Kraken, SpecialType.WhiteWhale })
            {
                var card = CardData.CreateSpecialCard(0, type);
                Sprite sprite = database.GetSpriteForCard(card);
                if (sprite == null || sprite.rect.width != 832 || sprite.rect.height != 1216
                    || !database.IsComposedArtwork(card) || database.GetFrameForCard(card) != null)
                    throw new InvalidOperationException($"Invalid finished card mapping: {type}");
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[CROSSION] LEGENDARY_CARDS_OK: framed Kraken and celestial sprites, 832x1216, full-card lookup verified.");
        }

        private static Sprite[] LoadSurrenderSet(string prefix, int count)
        {
            return Enumerable.Range(1, count)
                .Select(index => LoadComposed($"{prefix}_{index:00}_card.png"))
                .ToArray();
        }

        private static void ConfigureTexture(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                return;

            Vector4 desiredBorder = Vector4.zero;

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.maxTextureSize != 2048
                || importer.spriteBorder != desiredBorder;

            if (!changed)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.spriteBorder = desiredBorder;
            importer.SaveAndReimport();
        }

        private static Sprite Load(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + fileName);
            if (sprite == null)
                Debug.LogError($"[CROSSION] Sprite를 불러오지 못했습니다: {Root + fileName}");
            return sprite;
        }

        private static Sprite LoadComposed(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Composed/" + fileName);
            if (sprite == null)
                Debug.LogError($"[CROSSION] 완성 카드 Sprite를 불러오지 못했습니다: {Root + "Composed/" + fileName}");
            return sprite;
        }

        private static Sprite[] LoadNumberSet(string world)
        {
            return new[] { 1, 4, 7, 11 }
                .Select(number => LoadComposed($"Numbers/{world}_{number:00}.png"))
                .ToArray();
        }

        private static Sprite[] LoadSet(params string[] fileNames)
        {
            return fileNames.Select(Load).ToArray();
        }
    }
}
