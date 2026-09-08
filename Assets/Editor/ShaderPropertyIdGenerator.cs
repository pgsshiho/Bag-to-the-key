#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ShaderPropertyIdGenerator
{
    private const string OutputFolder = "Assets/Generated";

    [MenuItem("Assets/Generate Shader Property IDs", true)]
    private static bool Validate() => Selection.activeObject is Shader;

    [MenuItem("Assets/Generate Shader Property IDs")]
    private static void Generate()
    {
        Shader shader = (Shader)Selection.activeObject;
        string className = SanitizeIdentifier(shader.name) + "ShaderIDs";
        var usedNames = new HashSet<string>();
        var source = new StringBuilder();

        source.AppendLine("// AUTO-GENERATED. DO NOT EDIT MANUALLY.");
        source.AppendLine("using UnityEngine;");
        source.AppendLine();
        source.AppendLine($"public static class {className}");
        source.AppendLine("{");

        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName = ShaderUtil.GetPropertyName(shader, i);
            string fieldName = MakeUnique(
                SanitizeIdentifier(propertyName.TrimStart('_')),
                usedNames);
            source.AppendLine(
                $"    public static readonly int {fieldName} = Shader.PropertyToID(\"{propertyName}\");");
        }

        source.AppendLine("}");

        Directory.CreateDirectory(OutputFolder);
        string path = $"{OutputFolder}/{className}.cs";
        File.WriteAllText(path, source.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Debug.Log($"Generated shader property IDs: {path}", shader);
    }

    private static string SanitizeIdentifier(string value)
    {
        var result = new StringBuilder(value.Length + 1);
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
                result.Append(character);
            else
                result.Append('_');
        }

        if (result.Length == 0)
            result.Append("ShaderProperty");
        if (char.IsDigit(result[0]))
            result.Insert(0, '_');
        return result.ToString();
    }

    private static string MakeUnique(string candidate, HashSet<string> usedNames)
    {
        string unique = candidate;
        int suffix = 2;
        while (!usedNames.Add(unique))
            unique = candidate + suffix++;
        return unique;
    }
}
#endif
