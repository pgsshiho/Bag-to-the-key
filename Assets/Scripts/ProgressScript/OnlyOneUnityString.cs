using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Inspector에서 문자열을 직접 입력하는 대신 프로젝트 에셋 하나를 참조하도록 하는 ID입니다.
/// 에셋 이름이 런타임과 세이브 데이터에 기록되는 문자열 값이 됩니다.
/// </summary>
[CreateAssetMenu(
    fileName = "OnlyOneUnityString",
    menuName = "Scriptable Objects/OnlyOneUnityString"
)]
public sealed class OnlyOneUnityString : ScriptableObject
{
    public string Value => name;

    public static implicit operator string(OnlyOneUnityString value)
    {
        return value != null ? value.name : string.Empty;
    }

#if UNITY_EDITOR
    private static readonly HashSet<string> Names =
        new HashSet<string>(StringComparer.Ordinal);
    private static bool validationQueued;

    private void OnValidate()
    {
        QueueProjectValidation();
    }

    [InitializeOnLoadMethod]
    private static void QueueValidationAfterDomainReload()
    {
        QueueProjectValidation();
    }

    private static void QueueProjectValidation()
    {
        if (validationQueued)
            return;

        validationQueued = true;
        EditorApplication.delayCall += RunQueuedValidation;
    }

    private static void RunQueuedValidation()
    {
        ValidateUniqueNames();
    }

    public static bool ValidateUniqueNames()
    {
        validationQueued = false;
        Names.Clear();
        bool valid = true;

        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(OnlyOneUnityString)}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            OnlyOneUnityString asset =
                AssetDatabase.LoadAssetAtPath<OnlyOneUnityString>(path);
            if (asset == null || Names.Add(asset.name))
                continue;

            valid = false;
            Debug.LogError(
                $"중복된 {nameof(OnlyOneUnityString)} 이름 '{asset.name}'을 발견했습니다: {path}",
                asset);
        }

        return valid;
    }
#endif
}
