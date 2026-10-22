#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace SkullKing.Editor
{
    /// <summary>Creates editable sprite sub-assets without requiring the optional 2D Sprite package.</summary>
    [ScriptedImporter(1, "councilatlas")]
    public sealed class CouncilSpriteAtlasImporter : ScriptedImporter
    {
        [Serializable] private sealed class Definition { public string texture; public Piece[] pieces; }
        [Serializable] private sealed class Piece { public string name; public int x, y, width, height; }
        public override void OnImportAsset(AssetImportContext context)
        {
            var definition = JsonUtility.FromJson<Definition>(File.ReadAllText(context.assetPath));
            if (definition == null || definition.pieces == null || definition.pieces.Length == 0)
                throw new InvalidDataException("Council sprite definitions are empty.");
            context.DependsOnSourceAsset(definition.texture);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.texture);
            if (texture == null) throw new InvalidDataException("Council texture is not imported: " + definition.texture);
            Sprite main = null;
            foreach (var piece in definition.pieces)
            {
                var rect = new Rect(piece.x, texture.height - piece.y - piece.height, piece.width, piece.height);
                if (rect.xMin < 0 || rect.yMin < 0 || rect.xMax > texture.width || rect.yMax > texture.height)
                    throw new InvalidDataException("Council sprite rectangle out of bounds: " + piece.name);
                var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                sprite.name = piece.name;
                context.AddObjectToAsset(piece.name, sprite);
                if (main == null) main = sprite;
            }
            // Unity renames this representative object to the filename on import.
            // Consumers must load it via LoadMainAssetAtPath, not by the piece's name.
            context.SetMainObject(main);
        }
    }
}
#endif
