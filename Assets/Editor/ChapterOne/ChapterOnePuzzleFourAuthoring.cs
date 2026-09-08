#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ChapterOnePuzzleFourAuthoring
{
    private const string ScenePath = "Assets/Scenes/FirstScene/FirstMap.unity";
    private const string ArtFolder = "Assets/Sprites/RoomSprite/FirstRoom/";
    private const string ItemFolder = "Assets/Inventory/Resources/Items/Chapter01/";
    private const string MixMaterialPath = "Assets/Materials/SpriteTextureMix.mat";

    [MenuItem("Tools/Bag to the key/Apply Chapter 1 Puzzle 4 Only")]
    public static void ApplyToOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before authoring.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open FirstMap before applying Puzzle 4.");

        ProgressIdAssetUtility.EnsureChapterOneAssets();
        Transform wall = GameObject.Find("Chapter01/03_DollTableAndBooks")?.transform;
        if (wall == null)
            throw new InvalidOperationException("Chapter 1 book wall was not found.");

        InventoryManager inventory = Object.FindAnyObjectByType<InventoryManager>();
        ChapterOnePresentation presentation =
            Object.FindAnyObjectByType<ChapterOnePresentation>();
        Material mixMaterial = AssetDatabase.LoadAssetAtPath<Material>(MixMaterialPath);
        if (inventory == null || presentation == null || mixMaterial == null)
            throw new InvalidOperationException(
                "Puzzle 4 dependencies (inventory, presentation, or mix material) are missing.");

        Transform bookcase = wall.Find("Bookcase");
        if (bookcase == null)
            throw new InvalidOperationException("Bookcase was not found.");

        BookPlacement redPlacement = CapturePlacement(
            wall,
            "RedBook",
            new Vector3(8.549f, -.665f, 15f),
            new Vector3(8.5346f, -.6415f, 15f),
            new Vector3(.436083943f, .6305067f, 1f));
        BookPlacement greenPlacement = CapturePlacement(
            wall,
            "GreenBook",
            new Vector3(6.28f, 1.65f, 15f),
            new Vector3(6.2899f, 1.6175f, 15f),
            new Vector3(.485102445f, .677090943f, 1f));
        BookPlacement brownPlacement = CapturePlacement(
            wall,
            "BrownBook",
            new Vector3(5.051f, -2.853f, 15f),
            new Vector3(5.04f, -2.8804f, 15f),
            new Vector3(.4691358f, .662830651f, 1f));

        RemoveChild(wall, "BookArrangement");
        RemoveChild(wall, "BookRule");
        RemoveChild(wall, "PlacedRedBook");
        RemoveChild(wall, "PlacedGreenBook");
        RemoveChild(wall, "PlacedBrownBook");

        SpriteRenderer bookcaseRenderer = bookcase.GetComponent<SpriteRenderer>();
        Undo.RecordObject(bookcaseRenderer, "Assign bookcase texture mix material");
        bookcaseRenderer.sharedMaterial = mixMaterial;

        SpriteTextureMixer mixer = GetOrAdd<SpriteTextureMixer>(bookcase.gameObject);
        Set(mixer, "targetSprite", LoadSprite("퍼즐 완료 책장"));
        Set(mixer, "duration", 1.15f);
        Set(mixer, "ease", DG.Tweening.Ease.InOutSine);

        ProgressSpriteTextureMixer progressMixer =
            GetOrAdd<ProgressSpriteTextureMixer>(bookcase.gameObject);
        Set(progressMixer, "completedProgressId", ChapterOneProgressIds.Books);

        GameObject puzzleRoot = NewObject("BookArrangement", wall);
        ItemPlacementPuzzle puzzle = puzzleRoot.AddComponent<ItemPlacementPuzzle>();
        PuzzleStateController completion =
            puzzleRoot.GetComponent<PuzzleStateController>();
        Set(completion, "puzzleId", ChapterOneProgressIds.Books);
        Set(puzzle, "inventoryManager", inventory);
        Set(puzzle, "requireSequence", false);
        Set(puzzle, "hidePlacedVisualsWhenCompleted", true);

        var sockets = new List<ItemPlacementSocket>
        {
            CreateSocket(puzzleRoot.transform, wall, presentation, "RedBook", "빨간책 옆", ChapterOneProgressIds.RedBookSocket, redPlacement),
            CreateSocket(puzzleRoot.transform, wall, presentation, "GreenBook", "초록책 옆", ChapterOneProgressIds.GreenBookSocket, greenPlacement),
            CreateSocket(puzzleRoot.transform, wall, presentation, "BrownBook", "갈색 책 옆", ChapterOneProgressIds.BrownBookSocket, brownPlacement)
        };
        foreach (ItemPlacementSocket socket in sockets)
            Set(socket, "puzzle", puzzle);
        Set(puzzle, "sockets", sockets);
        Set(
            progressMixer,
            "hideWhenCompleted",
            sockets.Select(socket => socket.PlacedVisual).ToArray());
        Set(
            completion,
            "onFirstCompleted",
            CreateTextEvent(
                presentation,
                "책장이 정리됐어. 책장 아래에서 마지막 길 조각을 찾아보렴."));

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("FirstMap could not be saved.");
        Selection.activeGameObject = bookcase.gameObject;
        Debug.Log("Chapter 1 Puzzle 4 texture mixing and book sockets applied.");
    }

    private static ItemPlacementSocket CreateSocket(
        Transform puzzleRoot,
        Transform wall,
        ChapterOnePresentation presentation,
        string itemName,
        string spriteName,
        OnlyOneUnityString progressId,
        BookPlacement placement)
    {
        GameObject slot = NewObject(itemName + "BookcaseSocket", puzzleRoot);
        slot.transform.localPosition = placement.SocketPosition;
        BoxCollider collider = slot.AddComponent<BoxCollider>();
        collider.size = new Vector3(.62f, 2.15f, .2f);

        ItemPlacementSocket socket = slot.AddComponent<ItemPlacementSocket>();
        ItemData item = LoadItem(itemName);
        Set(socket, "progressId", progressId);
        Set(socket, "requiredItem", item);
        Set(socket, "consumeOnPlace", true);

        GameObject visual = NewObject("Placed" + itemName, wall);
        visual.transform.localPosition = placement.VisualPosition;
        Sprite sprite = LoadSprite(spriteName);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 125;
        visual.transform.localScale = placement.VisualScale;
        visual.SetActive(false);
        Set(socket, "placedVisual", visual);
        Set(
            socket,
            "onWrongItem",
            CreateTextEvent(
                presentation,
                $"{item.itemName}을 장착한 뒤 빈자리에 놓아 보렴."));
        Set(
            socket,
            "onFirstPlaced",
            CreateTextEvent(presentation, $"{item.itemName}이 제자리를 찾았어."));
        return socket;
    }

    private static BookPlacement CapturePlacement(
        Transform wall,
        string itemName,
        Vector3 defaultSocketPosition,
        Vector3 defaultVisualPosition,
        Vector3 defaultVisualScale)
    {
        Transform socket = wall.Find(
            $"BookArrangement/{itemName}BookcaseSocket");
        Transform visual = wall.Find("Placed" + itemName);
        return new BookPlacement(
            socket != null ? socket.localPosition : defaultSocketPosition,
            visual != null ? visual.localPosition : defaultVisualPosition,
            visual != null ? visual.localScale : defaultVisualScale);
    }

    private readonly struct BookPlacement
    {
        public readonly Vector3 SocketPosition;
        public readonly Vector3 VisualPosition;
        public readonly Vector3 VisualScale;

        public BookPlacement(
            Vector3 socketPosition,
            Vector3 visualPosition,
            Vector3 visualScale)
        {
            SocketPosition = socketPosition;
            VisualPosition = visualPosition;
            VisualScale = visualScale;
        }
    }

    private static UnityEvent CreateTextEvent(
        ChapterOnePresentation presentation,
        string message)
    {
        var result = new UnityEvent();
        UnityEventTools.AddStringPersistentListener(result, presentation.Say, message);
        return result;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static GameObject NewObject(string name, Transform parent)
    {
        var result = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(result, "Create Chapter 1 Puzzle 4 object");
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void RemoveChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            Undo.DestroyObjectImmediate(child.gameObject);
    }

    private static Sprite LoadSprite(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(ArtFolder + name + ".png")
            .OfType<Sprite>()
            .FirstOrDefault();

    private static ItemData LoadItem(string name) =>
        AssetDatabase.LoadAssetAtPath<ItemData>(ItemFolder + name + ".asset");

    private static void Set(Object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
            throw new MissingFieldException(target.GetType().Name, fieldName);
        Undo.RecordObject(target, "Configure Chapter 1 Puzzle 4");
        field.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }
}
#endif
