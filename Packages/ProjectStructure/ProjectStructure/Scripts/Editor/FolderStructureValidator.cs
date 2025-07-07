#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions; 
public class FolderStructureValidator : AssetPostprocessor
{
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
       var targetFolderPaths = FolderValidatorSettingsManager.GetFolders();
        
        if (targetFolderPaths.Count == 0) return;

        foreach (string assetPath in importedAssets.Concat(movedAssets))
        {
            if (AssetDatabase.IsValidFolder(assetPath) || assetPath.EndsWith(".meta"))
            {
                continue;
            }
            string containingFolder = targetFolderPaths.FirstOrDefault(folderPath => assetPath.StartsWith(folderPath + "/"));
            if (containingFolder == null)
            {
                continue;
            }
            ValidateAssetLocationAndNaming(assetPath, containingFolder);
        }
    }
    private static void ValidateAssetLocationAndNaming(string assetPath, string rootFolderPath)
    {
        string relativePath = assetPath.Substring(rootFolderPath.Length + 1);
        string[] pathParts = relativePath.Split('/');
        string rootFolder = pathParts.Length > 0 ? pathParts[0] : string.Empty;

        if (string.IsNullOrEmpty(rootFolder) || !folderHierarchy.ContainsKey(rootFolder))
        {
            LogWarning($"Asset '{assetPath}' is in an invalid root folder inside '{rootFolderPath}'. Expected one of: {string.Join(", ", folderHierarchy.Keys)}", assetPath);
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
                                LogWarning($"Asset '{assetPath}' in 'Arts/Textures' is not a common image format.", assetPath);
                            break;
                        case "Models":
                            if (!new[] { ".fbx", ".obj" }.Contains(extension)) // obj نیز اضافه شد
                                LogWarning($"Asset '{assetPath}' in 'Arts/Models' is not a common model format.", assetPath);
                            break;
                        case "Materials":
                            if (extension != ".mat")
                                LogWarning($"Asset '{assetPath}' in 'Arts/Materials' is not a material.", assetPath);
                            break;
                    }
                }
                else
                {
                    LogWarning($"Asset '{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.", assetPath);
                }
                break;
            case "Scripts":
                if (pathParts.Length > 1)
                {
                    if (extension != ".cs")
                        LogWarning($"Asset '{assetPath}' in 'Scripts' folder is not a C# script.", assetPath);
                }
                else
                    LogWarning($"Asset '{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.", assetPath);
                break;
            case "Presets":
                if (extension != ".preset")
                    LogWarning($"Asset '{assetPath}' in 'Presets' folder is not a preset file.", assetPath);
                break;
            case "Prefabs":
                if (extension != ".prefab")
                    LogWarning($"Asset '{assetPath}' in 'Prefabs' folder is not a prefab.", assetPath);
                break;
            case "GameData":
                if (!new[] { ".asset", ".inputactions", ".physicmaterial" }.Contains(extension))
                    LogWarning($"Asset '{assetPath}' in 'GameData' is not a valid data asset.", assetPath);
                break;
        }
    }


    private static void ValidateNamingConventions(string assetPath, string[] pathParts)
    {
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string parentFolder = Path.GetDirectoryName(assetPath).Replace('\\', '/');

        if (!IsPascalCase(fileName))
        {
            LogWarning($"Naming Convention Violation: Asset name '{fileName}' in '{assetPath}' is not in PascalCase.", assetPath);
        }

        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            if (!IsPascalCase(pathParts[i]))
            {
                LogWarning($"Naming Convention Violation: Folder name '{pathParts[i]}' in '{assetPath}' is not in PascalCase.", assetPath);
            }
        }

        if (parentFolder.EndsWith("Arts/Models"))
        {
            if (!fileName.StartsWith("SM_") && !fileName.StartsWith("DM_"))
            {
                LogWarning($"Naming Convention Violation: Model '{fileName}' should start with a prefix like 'SM_' (Static Mesh) or 'DM_' (Dynamic Mesh).", assetPath);
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
                    LogWarning($"Naming Convention Violation: Texture '{fileName}' should end with a suffix like {string.Join(", ", requiredSuffixes)}.", assetPath);
                }
            }

            else if (!requiredPrefixes.Any(suffix => fileName.StartsWith(suffix)))
            {
                LogWarning($"Naming Convention Violation: Texture '{fileName}' should start with a suffix like {string.Join(", ", requiredPrefixes)}.", assetPath);
            }
        }

        if (parentFolder.EndsWith("Arts/Materials"))
        {
            if (!fileName.StartsWith("M_"))
            {
                LogWarning($"Naming Convention Violation: Material '{fileName}' should start with the prefix 'M_'.", assetPath);
            }
        }
    }

    private static bool IsPascalCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return true;
        return Regex.IsMatch(str, @"^[A-Z][a-zA-Z0-9 ]*$");
    }
    private static void LogWarning(string message, string assetPath)
    {
        Debug.LogWarning(message, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
    }


}
#endif