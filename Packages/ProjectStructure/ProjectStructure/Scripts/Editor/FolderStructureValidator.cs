#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
public class FolderStructureValidator : AssetPostprocessor
{
    private const string _projectStructureViolationWarningPart = "<b><color=orange>Project Structure Convention Violation:</color><color=aqua> Asset</color></b> ";
    private const string _namingViolationWarningPart = "<b><color=yellow>Naming Convention Violation:</color></b> ";
    private static readonly Dictionary<string, object> folderHierarchy = new Dictionary<string, object>
    {
       { "Arts", new Dictionary<string, object> {
            { "Fonts", null },
            { "Models", null },
            { "Materials", null },
            { "Textures", null },
            { "VFXs", new Dictionary<string, object> {
                { "Clips", null },
                { "Graphs", null }
            }} ,
            { "SFXs", null },
            { "Animations", new Dictionary<string, object> {
                { "Clips", null },
                { "Animators", null }
            }},
        }},
        { "Scripts", new Dictionary<string, object> {
            { "Editor", null },
            { "RunTime", null },
            { "ScriptableObjects", null },
            { "Shaders", null },
            { "Helpers", null }
        }},
        { "Scenes", new Dictionary<string, object> {
            { "Templates", null },
            { "Levels", null }
        }},
        { "Prefabs", null },
        { "Presets", null },
        { "GameData", new Dictionary<string, object> {
            { "InputActions", null },
        }}
    };
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        ValidateAssets(importedAssets.Concat(movedAssets).ToArray());
    }

    public static void ValidateAssetsInPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            Debug.LogWarning($"The path '{path}' is invalid or does not exist.");
            return;
        }

        string[] assets = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                   .Where(file => !file.EndsWith(".meta"))
                                   .Select(file => file.Replace("\\", "/")).ToArray();



        ValidateAssets(assets);
        Debug.Log($"The path '{path}' is Validated.", AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    }

    private static void ValidateAssets(string[] assets)
    {
        var targetFolderPaths = FolderValidatorSettingsManager.GetFolders();

        if (targetFolderPaths.Count == 0) return;

        foreach (string assetPath in assets)
        {
            ValidateAsset(targetFolderPaths, assetPath);
        }
    }
    private static void ValidateAsset(List<string> targetFolderPaths, string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath) || assetPath.EndsWith(".meta"))
        {
            return;
        }
        string containingFolder = targetFolderPaths.FirstOrDefault(
            folderPath => assetPath.StartsWith(folderPath + "/")
            );
        if (containingFolder == null)
        {
            return;
        }
        ValidateAssetLocationAndNaming(assetPath, containingFolder);
    }

    private static void ValidateAssetLocationAndNaming(string assetPath, string rootFolderPath)
    {
        string relativePath = assetPath.Substring(rootFolderPath.Length + 1);
        string[] pathParts = relativePath.Split('/');
        string rootFolder = pathParts.Length > 0 ? pathParts[0] : string.Empty;

        if (string.IsNullOrEmpty(rootFolder) || !folderHierarchy.ContainsKey(rootFolder))
        {
            LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' is in an invalid root folder inside '{rootFolderPath}'. Expected one of: {string.Join(", ", folderHierarchy.Keys)}", assetPath);
            return;
        }

        ValidateAssetTypeByLocation(assetPath, rootFolder, pathParts);

        ValidateNamingConventions(assetPath, pathParts);
    }

    private static void ValidateAssetTypeByLocation(string assetPath, string rootFolder, string[] pathParts)
    {
        string extension = Path.GetExtension(assetPath).ToLower();
        string subFolder = pathParts.Length > 1 ? pathParts[1] : string.Empty;

        switch (rootFolder)
        {
            case "Arts":
                if (pathParts.Length > 1)
                {
                    switch (subFolder)
                    {
                        case "Textures":
                            if (!new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd" }.Contains(extension))
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/Textures' is not a common image format.", assetPath);
                            break;
                        case "Models":
                            if (!new[] { ".fbx", ".obj" }.Contains(extension))
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/Models' is not a common model format.", assetPath);
                            break;
                        case "SFXs":
                            if (!new[] { ".wav", ".mp3", ".ogg", ".mixer" }.Contains(extension))
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/SFXs' is not a common audio format.", assetPath);
                            break;
                        case "Materials":
                            if (extension != ".mat")
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/Materials' is not a material.", assetPath);
                            break;
                    }
                }
                else
                {
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.", assetPath);
                }
                break;
            case "Scripts":
                if (pathParts.Length > 1)
                {
                    if (extension != ".cs")
                        LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Scripts' folder is not a C# script.", assetPath);
                }
                else
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.", assetPath);
                break;
            case "Presets":
                if (extension != ".preset")
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Presets' folder is not a preset file.", assetPath);
                break;
            case "Prefabs":
                if (extension != ".prefab")
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Prefabs' folder is not a prefab.", assetPath);
                break;
            case "GameData":
                if (!new[] { ".asset", ".inputactions", ".physicmaterial" }.Contains(extension))
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'GameData' is not a valid data asset.", assetPath);
                break;
        }
    }


    private static void ValidateNamingConventions(string assetPath, string[] pathParts)
    {
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string parentFolder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        string extension = Path.GetExtension(assetPath).ToLower();

        if (!IsPascalCase(fileName))
        {
            LogWarning($"{_namingViolationWarningPart}<b><color=cyan>Asset name</color> '{fileName}'</b> in '{assetPath}' is not in PascalCase.", assetPath);
        }

        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            if (!IsPascalCase(pathParts[i]))
            {
                LogWarning($"{_namingViolationWarningPart}<b><color=red>Folder name</color> '{pathParts[i]}'</b> in '{assetPath}' is not in PascalCase.", assetPath);
            }
        }

        if (parentFolder.EndsWith("Arts/Models"))
        {
            if (!fileName.StartsWith("SM_") && !fileName.StartsWith("DM_"))
            {
                LogWarning($"{_namingViolationWarningPart}Model <b>'{fileName}'</b> should start with a prefix like <b><color=yellow>'SM_' (Static Mesh) or 'DM_' (Dynamic Mesh)</color></b>.", assetPath);
            }
        }
        if (parentFolder.EndsWith("Arts/SFXs"))
        {
            if (extension != ".mixer")
            {
                string[] requiredSuffixes = { "_S", "_D" };
                if (!requiredSuffixes.Any(suffix => fileName.EndsWith(suffix)))
                {
                    LogWarning($"{_namingViolationWarningPart}SFX <b>'{fileName}'</b> should end with a <b><color=yellow>suffix like {string.Join(", ", requiredSuffixes)}</color></b>.", assetPath);
                }
            }
        }
        if (parentFolder.EndsWith("Arts/Textures"))
        {
            string[] requiredPrefixes = { "TC_", "RT_" };

            if (fileName.StartsWith("T_"))
            {
                string[] requiredSuffixes = { "_BC", "_N", "_MS", "_H", "_AO", "_E", "_I" }; // BaseColor, Normal, Metallic, Height, AmbientOcclusion, Emission
                if (!requiredSuffixes.Any(suffix => fileName.EndsWith(suffix)))
                {
                    LogWarning($"{_namingViolationWarningPart}Texture <b>'{fileName}'</b> should end with a <b><color=yellow>suffix like {string.Join(", ", requiredSuffixes)}</color></b>.", assetPath);
                }
            }

            else if (!requiredPrefixes.Any(suffix => fileName.StartsWith(suffix)))
            {
                LogWarning($"{_namingViolationWarningPart}Texture <b>'{fileName}'</b> should start with a <b><color=yellow>suffix like {string.Join(", ", requiredPrefixes)}</color></b>.", assetPath);
            }
        }

        if (parentFolder.EndsWith("Arts/Materials"))
        {
            if (!fileName.StartsWith("M_"))
            {
                LogWarning($"{_namingViolationWarningPart}Material <b>'{fileName}'</b> should start with the <b><color=yellow>prefix 'M_'</color></b>.", assetPath);
            }
        }
    }

    private static bool IsPascalCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return true;

        var strParts = str.Split('_');
        for (int i = 0; i < strParts.Length; i++)
        {
            if ((i == 0 && !Regex.IsMatch(strParts[i], @"^[A-Z][a-zA-Z0-9]*$")) ||
                i > 0 && !Regex.IsMatch(strParts[i], @"^[A-Z0-9][a-zA-Z0-9]*$"))
            {
                return false;
            }
        }
        return true;
    }
    private static void LogWarning(string message, string assetPath)
    {
        Debug.LogWarning(message, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
    }
}
#endif