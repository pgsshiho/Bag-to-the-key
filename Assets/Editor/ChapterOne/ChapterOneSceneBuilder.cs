using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit authoring command. Re-running only replaces the generated Chapter01 root.
public static class ChapterOneSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/FirstScene/FirstMap.unity";
    private const string Art = "Assets/Sprites/RoomSprite/FirstRoom/";
    private const string Items = "Assets/Inventory/Resources/Items/Chapter01/";
    private const string PlaceholderPath = "Assets/Sprites/ChapterOnePlaceholders/";
    private static TMP_FontAsset font;
    private static Material spriteMaterial;
    private static Sprite square;
    private static InventoryManager inventory;
    private static ChapterOnePresentation presentation;
    private static readonly List<ChapterOneStateView.Binding> bindings = new();

    [MenuItem("Tools/Bag to the key/Apply Chapter 1 to FirstMap")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open FirstMap before applying Chapter 1.");

        // Preserve a reviewable snapshot including any unsaved user changes.
        Directory.CreateDirectory("Temp/ChapterOneBackup");
        if (!File.Exists("Temp/ChapterOneBackup/FirstMap.before-chapter1.unity"))
            EditorSceneManager.SaveScene(scene, "Temp/ChapterOneBackup/FirstMap.before-chapter1.unity", true);
        GameObject previous = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Chapter01");
        if (previous != null) Object.DestroyImmediate(previous);
        bindings.Clear();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/GriunXHangeul_Equal-Rg SDF.asset");
        if (font == null) throw new InvalidOperationException("Korean UI font missing.");
        font.isMultiAtlasTexturesEnabled = true;
        EditorUtility.SetDirty(font);
        spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
        square = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(
            "311925a002f4447b3a28927169b83ea6")).OfType<Sprite>().FirstOrDefault();
        if (square == null) square = MakeIcon("Panel", 0, new Color(.8f, .65f, .48f));
        inventory = Object.FindAnyObjectByType<InventoryManager>();
        if (inventory == null) throw new InvalidOperationException("Existing inventory missing.");

        foreach (string name in new[] { "Items", "InvestPoint", "DialogueInteractor", "InventorySettingTestOnly", "Canvas", "Square", "Square (1)", "Square (3)", "Square (4)" })
        {
            GameObject legacy = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (legacy != null) legacy.SetActive(false);
        }

        GameObject root = new GameObject("Chapter01");
        presentation = root.AddComponent<ChapterOnePresentation>();
        ChapterOneStateView stateView = root.AddComponent<ChapterOneStateView>();
        Transform[] walls = new Transform[4];
        string[] wallNames = { "01_Entry", "02_BoxAndHole", "03_DollTableAndBooks", "04_BallTrack" };
        for (int i = 0; i < 4; i++)
        {
            walls[i] = Node(wallNames[i], root.transform, Vector3.zero).transform;
            walls[i].localRotation = Quaternion.Euler(0f, i * 90f, 0f);
            Visual("Wallpaper", walls[i], LoadSprite("벽지 1"), 0, 0, 16, 36, 21);
            Visual("Floor", walls[i], square, 0, -7.4f, 15.8f, 36, 4).GetComponent<SpriteRenderer>().color = new Color(.40f,.29f,.24f);
            Visual("Skirting", walls[i], square, 0, -5.2f, 15.7f, 36, .18f).GetComponent<SpriteRenderer>().color = new Color(.90f,.79f,.66f);
        }
        SetupCamera();
        SetupInventoryUi();
        SetupItems();
        SetupHud(root.transform);
        SetupEntry(walls[0]);
        SetupBox(walls[1]);
        SetupTableAndBooks(walls[2]);
        SetupTrack(walls[3]);
        Set(stateView, "bindings", new List<ChapterOneStateView.Binding>(bindings));
        stateView.Refresh();
        EditorUtility.SetDirty(stateView);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("FirstMap could not be saved.");
        Selection.activeGameObject = root;
        Debug.Log("Chapter 1 applied to FirstMap. Existing scene snapshot: Temp/ChapterOneBackup/FirstMap.before-chapter1.unity");
    }

    private static void SetupCamera()
    {
        NextPosition pivot = Object.FindAnyObjectByType<NextPosition>();
        InvestigationCameraController controller = Object.FindAnyObjectByType<InvestigationCameraController>();
        if (pivot == null || controller == null) throw new InvalidOperationException("Existing camera rig missing.");
        pivot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        pivot.targetY = 0;
        pivot.rotateSpeed = 180;
        Rigidbody body = pivot.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        var defaultCamera = Get<CinemachineCamera>(controller, "defaultCamera");
        defaultCamera.transform.SetParent(pivot.transform, false);
        defaultCamera.transform.localPosition = Vector3.zero;
        defaultCamera.transform.localRotation = Quaternion.identity;
        defaultCamera.Lens.FieldOfView = 60;
        defaultCamera.Priority = 10;
        Get<CinemachineCamera>(controller, "investigationCamera").Priority = 0;
        Camera.main.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Camera.main.orthographic = false;
        Camera.main.fieldOfView = 60;
        Camera.main.transparencySortMode = TransparencySortMode.Orthographic;
        // Persistent prefab callbacks must resolve the new scene's presentation after loading.
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                string method = button.onClick.GetPersistentMethodName(i);
                if (method != "TurnLeft" && method != "TurnRight") continue;
                UnityEventTools.RemovePersistentListener(button.onClick, i);
                RoomNavigationButton navigation = button.GetComponent<RoomNavigationButton>();
                if (navigation == null) navigation = button.gameObject.AddComponent<RoomNavigationButton>();
                if (method == "TurnLeft") UnityEventTools.AddPersistentListener(button.onClick, navigation.TurnLeft);
                else UnityEventTools.AddPersistentListener(button.onClick, navigation.TurnRight);
                EditorUtility.SetDirty(button);
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
        }
    }

    private static void SetupItems()
    {
        foreach (ItemData item in AssetDatabase.FindAssets("t:ItemData", new[] { Items.TrimEnd('/') })
                     .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g))))
        {
            item.canDiscard = false;
            EditorUtility.SetDirty(item);
        }
        var colors = new[] { new Color(.92f,.57f,.35f), new Color(.45f,.70f,.50f), new Color(.43f,.60f,.84f) };
        for (int i = 0; i < 3; i++)
        {
            ItemData item = Item("PathPiece" + (char)('A' + i));
            item.icon = MakeIcon("PathPiece" + (char)('A' + i), i + 1, colors[i]);
            EditorUtility.SetDirty(item);
        }
        ItemData track = Item("CompletedBallTrack");
        track.icon = MakeIcon("CompletedTrack", 4, new Color(.76f,.61f,.40f));
        EditorUtility.SetDirty(track);
        if (!File.Exists(Items + "PinkBall.asset"))
        {
            ItemData ball = ScriptableObject.CreateInstance<ItemData>();
            ball.itemId = "ch1_pink_ball";
            ball.itemName = "핑크 공";
            ball.description = "작은 길을 끝까지 굴러온 공. 다음 걸음에도 함께한다.";
            ball.width = ball.height = 1;
            ball.canDiscard = false;
            ball.icon = MakeIcon("PinkBall", 5, new Color(.94f,.49f,.63f));
            AssetDatabase.CreateAsset(ball, Items + "PinkBall.asset");
        }
    }

    private static void SetupInventoryUi()
    {
        InventoryUI ui = Object.FindAnyObjectByType<InventoryUI>();
        // Keep the existing 10 x 10 bag capacity while fitting the full grid on screen.
        Set(ui, "cellSize", 48f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
        foreach (Button button in ui.GetComponentsInChildren<Button>(true))
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            if (button.name == "InventoryOpenButton")
            {
                rect.anchorMin = rect.anchorMax = new Vector2(1,0);
                rect.pivot = new Vector2(.5f,.5f);
                rect.anchoredPosition = new Vector2(-120,100);
                rect.sizeDelta = new Vector2(140,110);
                Image image = button.GetComponent<Image>();
                image.color = Color.white;
                image.preserveAspect = true;
                image.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/RoomSprite/UISprite/가방.png").OfType<Sprite>().FirstOrDefault();
            }
            else if (button.name == "LeftRotateButton" || button.name == "RightRotateButton")
            {
                bool left = button.name == "LeftRotateButton";
                rect.anchorMin = rect.anchorMax = new Vector2(left ? 0 : 1,.5f);
                rect.pivot = new Vector2(.5f,.5f);
                rect.anchoredPosition = new Vector2(left ? 72 : -72,0);
                rect.sizeDelta = new Vector2(100,100);
                button.GetComponent<Image>().color = new Color(.25f,.16f,.15f,.85f);
            }
            else continue;
            EditorUtility.SetDirty(rect); EditorUtility.SetDirty(button.GetComponent<Image>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button.GetComponent<Image>());
        }
        GameObject bag = Get<GameObject>(ui,"invenUI");
        RectTransform bagRect = bag.GetComponent<RectTransform>();
        bagRect.sizeDelta = new Vector2(960,640);
        EditorUtility.SetDirty(bagRect); PrefabUtility.RecordPrefabInstancePropertyModifications(bagRect);
        bag.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(bag);
    }

    private static void SetupHud(Transform parent)
    {
        GameObject hud = new GameObject("ChapterHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hud.transform.SetParent(parent, false);
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        hud.GetComponent<Canvas>().sortingOrder = 20;
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = .5f;
        TMP_Text objective = UiText("Objective", hud.transform, "집에서 문을 열고 나가라", new Vector2(0,480), new Vector2(1300,55), 28);
        TMP_Text hint = UiText("Hint", hud.transform, "물건 클릭  ·  가방에서 R 회전  ·  장착 후 사용  ·  ESC 닫기  ·  F5 저장 / F9 불러오기", new Vector2(0,-495), new Vector2(1420,70), 22);
        hint.color = new Color(.98f,.91f,.80f);
        TMP_Text dialogueText = UiText("ParentDialogue", hud.transform, "", new Vector2(0,360), new Vector2(1360,130), 32);
        DialogueTextController dialogue = dialogueText.gameObject.AddComponent<DialogueTextController>();
        Set(dialogue, "useCurrentPositionAsShown", true);
        Set(dialogue, "charactersPerSecond", 45f);
        Set(presentation, "objective", objective);
        Set(presentation, "hint", hint);
        Set(presentation, "dialogue", dialogue);

        GameObject opening = new GameObject("Opening", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        opening.transform.SetParent(hud.transform, false);
        RectTransform rect = opening.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        opening.GetComponent<Image>().color = new Color(.09f,.065f,.07f,1);
        UiText("Title", opening.transform, "1 Chapter\n빈손\n<size=28>유아기</size>", Vector2.zero, new Vector2(1000,320), 56).color = new Color(.98f,.91f,.80f);
        opening.GetComponent<CanvasGroup>().alpha = 0;
        opening.GetComponent<CanvasGroup>().blocksRaycasts = false;
        Set(presentation, "opening", opening.GetComponent<CanvasGroup>());
    }

    private static void SetupEntry(Transform wall)
    {
        GameObject door = Visual("ExitDoor_Placeholder", wall, square, -5,-1.3f,14.8f,4,7.5f,true);
        door.GetComponent<SpriteRenderer>().color = new Color(.47f,.30f,.22f);
        Label("DoorLabel", wall, "집 밖으로", -5,2.8f,14.5f,1.1f);
        Visual("DoorKnob", wall, Item("PinkBall").icon, -3.7f,-1.5f,14.5f,.3f,.3f);
        ChapterFlowController exit = door.AddComponent<ChapterFlowController>();
        Set(exit, "chapterCompletionId", "ch01.complete");
        Set(exit, "requiredPuzzleIds", new List<string> { "ch01.box", "ch01.chest", "ch01.table", "ch01.books", "ch01.ball_finished", "ch01.parent_gift", "ch01.pickup.PinkBall" });
        Set(exit, "nextSceneName", "BaseMap");
        Set(exit, "nextChapterTitle", "2 Chapter : 채워지는 가방\n청소년기");
        Event(exit, "onExitBlocked", presentation.ParentHint);
        Set(presentation, "exit", exit);
        GameObject glow = Visual("OpenDoorLight", wall, square, -5,-1.3f,14.6f,3.5f,7f);
        glow.GetComponent<SpriteRenderer>().color = new Color(1f,.89f,.63f,.65f);
        Bind(glow, new[] { "ch01.parent_gift" });

        GameObject parent = Visual("Parent_Placeholder", wall, square, 4,-1.3f,14.5f,3.4f,5.5f,true);
        parent.GetComponent<SpriteRenderer>().color = new Color(.66f,.47f,.57f);
        Label("ParentLabel", wall, "부모\n꼬맹이, 이리 오렴", 4,2.5f,14.2f,1f);
        MultiStepItemUsePuzzle gift = parent.AddComponent<MultiStepItemUsePuzzle>();
        Set(gift.GetComponent<PuzzleStateController>(), "puzzleId", "ch01.parent_gift");
        var step = new ItemUsePuzzleStep { stepId = "cat", requiredEquippedItem = Item("CatDoll"), consumeOnUse = true };
        step.onMissingItem = new UnityEvent();
        UnityEventTools.AddPersistentListener(step.onMissingItem, presentation.ParentHint);
        step.onFirstCompleted = new UnityEvent();
        UnityEventTools.AddStringPersistentListener(step.onFirstCompleted, presentation.Say, "고마워, 꼬맹이. 이제 문을 열 수 있단다. 네 손으로 찾아낸 것들을 기억하렴.");
        Set(gift, "steps", new List<ItemUsePuzzleStep> { step });
        Event(gift, "onAlreadyCompleted", presentation.ParentHint);
        Visual("ChildDrawing", wall, LoadSprite("어린이그림"), 10,2.5f,15,3.5f,3.8f);
        Pickup("RedBook", wall, -10,-3.9f,14,1.3f,1.6f);
    }

    private static void SetupBox(Transform wall)
    {
        Visual("SmallBookshelf", wall, LoadSprite("책장"), -7,-1.3f,15,6,7);
        Pickup("GreenBook", wall, -8.5f,0,14.4f,1.1f,1.7f);
        GameObject box = Visual("PushBox", wall, LoadSprite("박스"), 4,-2.3f,13.5f,7,5.5f,true);
        PushablePuzzleObject push = box.AddComponent<PushablePuzzleObject>();
        Set(push.GetComponent<PuzzleStateController>(), "puzzleId", "ch01.box");
        Set(push, "localPushOffset", new Vector3(6,0,0));
        EventText(push, "onPushCompleted", "상자 뒤에 작은 상자와 토끼가 숨어 있었네. 토끼를 가방에 담아 보렴.");
        Bind(Label("PushLabel", wall, "밀어 보기", 4,1,13.3f,1f).gameObject,Array.Empty<string>(),new[] { "ch01.box" });
        GameObject chest = Visual("LockedChest", wall, LoadSprite("박스"), 2,-3.1f,14.4f,3,2.4f,true);
        chest.GetComponent<SpriteRenderer>().color = new Color(.75f,.81f,.91f);
        NumericCodeLock code = chest.AddComponent<NumericCodeLock>();
        Set(code.GetComponent<PuzzleStateController>(), "puzzleId", "ch01.chest");
        Set(code, "displayTitle", "작은 상자 · 구멍 너머의 네 숫자");
        Set(code, "expectedCode", "2413");
        EventText(code, "onCorrectCode", "찰칵! 곰인형과 첫 번째 길 조각이 들어 있구나.");
        EventText(code, "onAlreadyUnlocked", "상자는 열려 있어. 옆에 남아 있는 물건을 챙기렴.");
        Bind(chest, new[] { "ch01.box" });
        Pickup("RabbitDoll", wall, 5,-2.8f,14.1f,1.7f,3,new[] { "ch01.box" });
        Pickup("BearDoll", wall, 1,-1.5f,14,1.5f,2.5f,new[] { "ch01.chest" });
        Pickup("PathPieceA", wall, 3.1f,-1.7f,13.8f,1.3f,1.3f,new[] { "ch01.chest" });

        GameObject hole = Visual("CrawlHole_Placeholder", wall, square, -2,-4.1f,14.6f,2.5f,2.3f,true);
        hole.GetComponent<SpriteRenderer>().color = new Color(.08f,.065f,.10f);
        ChapterOneClue clue = hole.AddComponent<ChapterOneClue>();
        InvestigationPoint point = hole.AddComponent<InvestigationPoint>();
        Transform viewpoint = Node("InvestigationViewPoint", wall, new Vector3(-2,-3.3f,11.7f)).transform;
        Set(point, "viewPoint", viewpoint); Set(point, "fieldOfView", 40f);
        var controller = Object.FindAnyObjectByType<InvestigationCameraController>();
        Set(point, "cameraController", controller);
        Set(clue, "cameraController", controller); Set(clue, "investigationPoint", point); Set(clue, "presentation", presentation);
        Label("ClueDigits_Placeholder", wall, "2  4  1  3", -2,-3.8f,14.3f,.27f, Color.white);
        Label("HoleLabel", wall, "작은 구멍 · 들여다보기", -2,-5.9f,14.1f,.65f);
    }

    private static void SetupTableAndBooks(Transform wall)
    {
        GameObject table = Visual("DollTable", wall, LoadSprite("테이블"), -5,-2.8f,14.8f,8,4.2f);
        GameObject puzzleRoot = Node("DollPlacement", wall, Vector3.zero);
        ItemPlacementPuzzle puzzle = puzzleRoot.AddComponent<ItemPlacementPuzzle>();
        Set(puzzle.GetComponent<PuzzleStateController>(), "puzzleId", "ch01.table");
        Set(puzzle, "inventoryManager", inventory); Set(puzzle, "requireSequence", false);
        List<ItemPlacementSocket> sockets = new();
        for (int i = 0; i < 2; i++)
        {
            string itemName = i == 0 ? "BearDoll" : "RabbitDoll";
            float x = -7 + i*4;
            GameObject slot = Visual(itemName+"Socket", puzzleRoot.transform, square, x,-.8f,14,2.1f,3.3f,true);
            slot.GetComponent<SpriteRenderer>().color = new Color(.98f,.90f,.70f,.23f);
            ItemPlacementSocket socket = slot.AddComponent<ItemPlacementSocket>();
            Set(socket, "puzzle", puzzle); Set(socket, "socketId", itemName); Set(socket, "requiredItem", Item(itemName));
            Set(socket, "sequenceIndex", i);
            GameObject placed = Visual("Placed"+itemName, wall, Item(itemName).icon, x,-.7f,13.8f,1.7f,3f);
            Set(socket, "placedVisual", placed); placed.SetActive(false);
            EventText(socket, "onWrongItem", i == 0 ? "갈색 곰인형을 장착해서 왼쪽 자리에 놓아 보렴." : "키 큰 흰 토끼인형을 장착해서 오른쪽 자리에 놓아 보렴.");
            sockets.Add(socket);
        }
        Set(puzzle, "sockets", sockets);
        EventText(puzzle.GetComponent<PuzzleStateController>(), "onFirstCompleted", "인형들이 제자리를 찾았네. 테이블의 비밀 서랍이 열렸어.");
        Label("DollRule", wall, "갈색 곰  →  키 큰 흰 토끼", -5,2.1f,14,.8f);
        GameObject drawer = Visual("SecretDrawer_Placeholder", wall, square, -5,-3.1f,14.3f,3,1);
        drawer.GetComponent<SpriteRenderer>().color = new Color(.20f,.13f,.09f);
        Bind(drawer,new[] { "ch01.table" });
        Pickup("PathPieceB",wall,-5,-3,13.7f,1,1,new[] { "ch01.table" });
        Visual("Bookcase",wall,LoadSprite("채워 야 할 책장"),6,-.7f,15,6,8);
        Pickup("BrownBook",wall,10,-4.1f,14,1.2f,1.6f);
        Label("BookRule", wall, "가방 첫 줄 · 왼쪽 세 칸\n빨강 → 초록 → 갈색", 6,4.8f,14,.65f);
        GameObject bookPuzzle = Node("BookArrangement",wall,Vector3.zero);
        InventoryLayoutPuzzle layout = bookPuzzle.AddComponent<InventoryLayoutPuzzle>();
        Set(layout.GetComponent<PuzzleStateController>(),"puzzleId","ch01.books");
        Set(layout,"inventoryManager",inventory);
        Set(layout,"requirements",new List<InventoryLayoutRequirement>
        {
            new() { item=Item("RedBook"), position=new Vector2Int(0,0) },
            new() { item=Item("GreenBook"), position=new Vector2Int(1,0) },
            new() { item=Item("BrownBook"), position=new Vector2Int(2,0) }
        });
        EventText(layout,"onLayoutMatched","책의 색이 이어졌어. 책장 아래에서 마지막 길 조각을 찾아보렴.");
        Pickup("PathPieceC",wall,6,-3.8f,14,1.3f,1.3f,new[] { "ch01.books" });
    }

    private static void SetupTrack(Transform wall)
    {
        GameObject machine = Visual("BallTrackMachine",wall,LoadSprite("공굴리기"),0,0,14.7f,11,10,true);
        MultiStepItemUsePuzzle install = machine.AddComponent<MultiStepItemUsePuzzle>();
        Set(install.GetComponent<PuzzleStateController>(),"puzzleId","ch01.track_installed");
        Set(install,"inventoryManager",inventory);
        var step = new ItemUsePuzzleStep { stepId="track", requiredEquippedItem=Item("CompletedBallTrack"), consumeOnUse=true, onMissingItem=new UnityEvent() };
        UnityEventTools.AddStringPersistentListener(step.onMissingItem,presentation.Say,"A, B, C를 가방 안에서 좌우로 이어 조합하고, 완성된 길을 장착해서 끼워 보렴.");
        Set(install,"steps",new List<ItemUsePuzzleStep> { step });
        Label("TrackRule",wall,"A + B + C  →  하나의 길",0,6.1f,14,.9f);
        GameObject missing = Visual("TrackGap_Placeholder",wall,square,0,0,14.3f,7,.8f);
        missing.GetComponent<SpriteRenderer>().color = new Color(.12f,.08f,.05f,.9f);
        Bind(missing,Array.Empty<string>(),new[] { "ch01.track_installed" });
        Transform ball = Visual("RollingBall_Placeholder",wall,Item("PinkBall").icon,-3.2f,3.4f,14.1f,.5f,.5f).transform;
        Vector2[] points = { new(-3.2f,3.4f),new(2.8f,3.4f),new(4,2.1f),new(2.7f,.9f),new(-3.4f,.9f),new(-4,-.4f),new(-3,-1.4f),new(2.8f,-1.4f),new(4,-2.5f),new(2.6f,-3.5f) };
        var waypoints = points.Select((p,i)=>Node("BallWaypoint"+i,wall,new Vector3(p.x,p.y,14.1f)).transform).ToArray();
        ChapterOneBallRun roll = Node("BallRun",wall,Vector3.zero).AddComponent<ChapterOneBallRun>();
        Set(roll,"ball",ball); Set(roll,"waypoints",waypoints); Set(roll,"presentation",presentation);
        GameObject cage = Visual("CatLatch_Placeholder",wall,square,8,-2,14.7f,3.5f,4.3f);
        cage.GetComponent<SpriteRenderer>().color = new Color(.40f,.30f,.23f,.6f);
        GameObject lockedCat = Visual("LockedCat",wall,Item("CatDoll").icon,8,-2,14.4f,2.2f,3.5f);
        Bind(lockedCat,Array.Empty<string>(),new[] { "ch01.ball_finished" });
        Pickup("CatDoll",wall,8,-2,14.2f,2.2f,3.5f,new[] { "ch01.ball_finished" });
        Pickup("PinkBall",wall,3,-4.7f,13.8f,.8f,.8f,new[] { "ch01.ball_finished" });
        Label("CatLabel",wall,"길의 끝에서 기다리는 친구",8,1,14,.65f);
    }

    private static GameObject Pickup(string itemName, Transform parent, float x,float y,float z,float width,float height,string[] required=null)
    {
        string id = "ch01.pickup."+itemName;
        GameObject go = Visual("Pickup_"+itemName,parent,Item(itemName).icon,x,y,z,width,height,true);
        Itemgetbase pickup = go.AddComponent<Itemgetbase>(); pickup.item=Item(itemName);
        Set(pickup,"inventoryManager",inventory); Set(pickup,"persistentPickupId",id);
        Set(pickup,"keepForStateRestore",true);
        Bind(go,required ?? Array.Empty<string>(),new[] { id });
        if (itemName.StartsWith("PathPiece")) Label(itemName+"Label",go.transform,itemName.Substring(itemName.Length-1),0,0,-.1f,.5f);
        return go;
    }

    private static void Bind(GameObject go,string[] required,string[] excluded=null)
    {
        bindings.Add(new ChapterOneStateView.Binding { target=go,required=required,excluded=excluded??Array.Empty<string>() });
    }

    private static GameObject Node(string name,Transform parent,Vector3 position)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; return go;
    }

    private static GameObject Visual(string name,Transform parent,Sprite sprite,float x,float y,float z,float width,float height,bool collider=false)
    {
        if(sprite==null) throw new InvalidOperationException("Missing sprite for "+name);
        GameObject go=Node(name,parent,new Vector3(x,y,z));
        SpriteRenderer renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;
        if(spriteMaterial!=null) renderer.sharedMaterial=spriteMaterial;
        renderer.sortingOrder=Mathf.RoundToInt((16f-z)*100f);
        go.transform.localScale=new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1);
        if(collider) { var box=go.AddComponent<BoxCollider>(); box.size=new Vector3(sprite.bounds.size.x,sprite.bounds.size.y,.15f); }
        return go;
    }

    private static TMP_Text Label(string name,Transform parent,string text,float x,float y,float z,float size,Color? color=null)
    {
        GameObject go=Node(name,parent,new Vector3(x,y,z));
        TextMeshPro label=go.AddComponent<TextMeshPro>(); label.font=font; label.text=text; label.fontSize=size*10;
        label.alignment=TextAlignmentOptions.Center; label.color=color??new Color(.21f,.13f,.14f);
        label.rectTransform.sizeDelta=new Vector2(16,3); label.textWrappingMode=TextWrappingModes.Normal;
        label.GetComponent<MeshRenderer>().sortingOrder=400;
        return label;
    }

    private static TMP_Text UiText(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
        TextMeshProUGUI text=go.GetComponent<TextMeshProUGUI>(); text.font=font; text.text=value; text.fontSize=fontSize;
        text.color=new Color(.22f,.13f,.15f); text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
        text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);
        text.rectTransform.anchoredPosition=position; text.rectTransform.sizeDelta=size;
        return text;
    }

    private static ItemData Item(string name)=>AssetDatabase.LoadAssetAtPath<ItemData>(Items+name+".asset");
    private static Sprite LoadSprite(string name)=>AssetDatabase.LoadAllAssetsAtPath(Art+name+".png").OfType<Sprite>().FirstOrDefault();

    private static Sprite MakeIcon(string name,int style,Color color)
    {
        Directory.CreateDirectory(PlaceholderPath);
        string path=PlaceholderPath+name+".png";
        if(!File.Exists(path))
        {
            Texture2D texture=new Texture2D(96,96,TextureFormat.RGBA32,false);
            for(int y=0;y<96;y++) for(int x=0;x<96;x++)
            {
                bool inside=style==5 ? (x-48)*(x-48)+(y-48)*(y-48)<40*40 : x>4&&x<91&&y>10&&y<85;
                Color pixel=inside?color:Color.clear;
                if(inside&&style>0&&style<5&&Mathf.Abs(y-48)<7) pixel=new Color(.25f,.17f,.12f);
                if(inside&&style>0&&style<4&&y>65&&y<74)
                    for(int dot=0;dot<style;dot++) if(Mathf.Abs(x-(32+dot*15))<4) pixel=Color.white;
                texture.SetPixel(x,y,pixel);
            }
            texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=96; importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.alphaIsTransparency=true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EventText(Object target,string field,string message)
    {
        UnityEvent evt=new UnityEvent(); UnityEventTools.AddStringPersistentListener(evt,presentation.Say,message); Set(target,field,evt);
    }
    private static void Event(Object target,string field,UnityAction action)
    {
        UnityEvent evt=new UnityEvent(); UnityEventTools.AddPersistentListener(evt,action); Set(target,field,evt);
    }
    private static T Get<T>(Object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(target);
    private static void Set(Object target,string field,object value)
    {
        FieldInfo info=target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
        if(info==null) throw new MissingFieldException(target.GetType().Name,field);
        info.SetValue(target,value); EditorUtility.SetDirty(target);
    }
}
