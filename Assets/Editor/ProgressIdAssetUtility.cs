using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ProgressIdAssetUtility
{
    public const string FolderPath = "Assets/ProgressIds";

    private static readonly string[] ChapterOneIdNames =
    {
        "ch01.introduction",
        "ch01.box",
        "ch01.box.push.1",
        "ch01.chest",
        "ch01.hole_clue",
        "ch01.table",
        "ch01.table.socket.BearDoll",
        "ch01.table.socket.RabbitDoll",
        "ch01.books",
        "ch01.books.socket.RedBook",
        "ch01.books.socket.GreenBook",
        "ch01.books.socket.BrownBook",
        "ch01.track_installed",
        "ch01.track_installed.step.track",
        "ch01.ball_finished",
        "ch01.parent_gift",
        "ch01.parent_gift.step.cat",
        "ch01.complete",
        "ch01.pickup.RedBook",
        "ch01.pickup.GreenBook",
        "ch01.pickup.BrownBook",
        "ch01.pickup.RabbitDoll",
        "ch01.pickup.BearDoll",
        "ch01.pickup.PathPieceA",
        "ch01.pickup.PathPieceB",
        "ch01.pickup.PathPieceC",
        "ch01.pickup.CatDoll",
        "ch01.pickup.PinkBall"
    };

    [MenuItem("Tools/Bag to the key/Progress IDs/Ensure Chapter 1 IDs")]
    public static void EnsureChapterOneAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureFolder();
        bool createdAny = false;
        foreach (string id in ChapterOneIdNames)
        {
            GetOrCreate(id, false, out bool created);
            createdAny |= created;
        }

        if (createdAny)
            AssetDatabase.SaveAssets();

        OnlyOneUnityString.ValidateUniqueNames();
    }

    public static OnlyOneUnityString Get(string id)
    {
        OnlyOneUnityString asset = GetOrCreate(id, true, out _);
        if (asset == null)
            throw new InvalidOperationException($"Progress ID asset '{id}' could not be created.");
        return asset;
    }

    private static OnlyOneUnityString GetOrCreate(
        string id,
        bool saveImmediately,
        out bool created)
    {
        EnsureFolder();
        string path = $"{FolderPath}/{id}.asset";
        OnlyOneUnityString asset =
            AssetDatabase.LoadAssetAtPath<OnlyOneUnityString>(path);
        if (asset != null)
        {
            created = false;
            return asset;
        }

        asset = ScriptableObject.CreateInstance<OnlyOneUnityString>();
        asset.name = id;
        AssetDatabase.CreateAsset(asset, path);
        created = true;
        if (saveImmediately)
            AssetDatabase.SaveAssets();
        return asset;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(FolderPath))
            AssetDatabase.CreateFolder("Assets", "ProgressIds");
    }
}

public static class ChapterOneProgressIds
{
    public static OnlyOneUnityString Introduction => Get("ch01.introduction");
    public static OnlyOneUnityString Box => Get("ch01.box");
    public static OnlyOneUnityString BoxPushOne => Get("ch01.box.push.1");
    public static OnlyOneUnityString Chest => Get("ch01.chest");
    public static OnlyOneUnityString HoleClue => Get("ch01.hole_clue");
    public static OnlyOneUnityString Table => Get("ch01.table");
    public static OnlyOneUnityString BearSocket => Get("ch01.table.socket.BearDoll");
    public static OnlyOneUnityString RabbitSocket => Get("ch01.table.socket.RabbitDoll");
    public static OnlyOneUnityString Books => Get("ch01.books");
    public static OnlyOneUnityString RedBookSocket => Get("ch01.books.socket.RedBook");
    public static OnlyOneUnityString GreenBookSocket => Get("ch01.books.socket.GreenBook");
    public static OnlyOneUnityString BrownBookSocket => Get("ch01.books.socket.BrownBook");
    public static OnlyOneUnityString TrackInstalled => Get("ch01.track_installed");
    public static OnlyOneUnityString TrackInstallStep => Get("ch01.track_installed.step.track");
    public static OnlyOneUnityString BallFinished => Get("ch01.ball_finished");
    public static OnlyOneUnityString ParentGift => Get("ch01.parent_gift");
    public static OnlyOneUnityString ParentGiftStep => Get("ch01.parent_gift.step.cat");
    public static OnlyOneUnityString Complete => Get("ch01.complete");

    public static OnlyOneUnityString Pickup(string itemName)
    {
        return Get($"ch01.pickup.{itemName}");
    }

    private static OnlyOneUnityString Get(string id)
    {
        return ProgressIdAssetUtility.Get(id);
    }
}

public sealed class OnlyOneUnityStringBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!OnlyOneUnityString.ValidateUniqueNames())
            throw new BuildFailedException("OnlyOneUnityString 에셋 이름이 중복되었습니다.");
    }
}
