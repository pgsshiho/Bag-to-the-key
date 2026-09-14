using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChapterOneArtImportUtility
{
    private const string ArtDirectory = "Assets/Sprites/ChapterOneFinal";

    [MenuItem("Tools/Bag to the key/Import Chapter 1 Art")]
    public static void Apply()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string fullPath in Directory.GetFiles(ArtDirectory, "*.png"))
        {
            string assetPath = fullPath.Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (texture == null || importer == null)
                throw new InvalidOperationException($"Could not import Chapter 1 sprite: {assetPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.spritePixelsPerUnit = Mathf.Min(texture.width, texture.height);

            if (Path.GetFileName(assetPath).Equals("Parent.png", StringComparison.OrdinalIgnoreCase))
            {
                if (texture.width != texture.height * 4)
                    throw new InvalidOperationException("Parent.png must be a 4x1 sprite sheet.");
                importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
                importer.spritesheet = Enumerable
                    .Range(0, 4)
                    .Select(index => new SpriteMetaData
                    {
                        name = $"Parent_{index}",
                        rect = new Rect(index * texture.height, 0, texture.height, texture.height),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0f),
                    })
                    .ToArray();
#pragma warning restore CS0618
            }
            else
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
            }
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Chapter 1 art imported with Point filtering and min(width, height) PPU.");
    }

    public static void ApplyAndRebuildScene()
    {
        Apply();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ChapterOneSceneBuilder.ScenePath)
        {
            if (activeScene.isDirty)
                throw new InvalidOperationException(
                    "Open FirstMap or save the active scene before rebuilding Chapter 1."
                );
            EditorSceneManager.OpenScene(ChapterOneSceneBuilder.ScenePath, OpenSceneMode.Single);
        }
        ChapterOneSceneBuilder.Apply();
    }
}
