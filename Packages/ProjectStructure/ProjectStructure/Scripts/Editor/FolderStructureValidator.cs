#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Palapal.Shared;
public class FolderStructureValidator : AssetPostprocessor
{
    // Constants for warning messages
    private const string _projectStructureViolationWarningPart = "<b><color=orange>Project Structure Convention Violation:</color><color=aqua> Asset</color></b> ";
    private const string _namingViolationWarningPart = "<b><color=yellow>Naming Convention Violation:</color></b> ";

    // Method called after all assets have been imported
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        // Validate the imported and moved assets
        ValidateAssets(importedAssets.Concat(movedAssets).ToArray());
    }

    // Method to validate assets in a given path
    public static void ValidateAssetsInPath(string path)
    {
        // Check if the path is valid and exists
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            Debug.LogWarning($"The path '{path}' is invalid or does not exist.");
            return;
        }

        // Get all assets in the path
        string[] assets = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                   .Where(file => !file.EndsWith(".meta"))
                                   .Select(file => file.Replace("\\", "/")).ToArray();



        // Validate the assets
        ValidateAssets(assets);
        Debug.Log($"The path '{path}' is Validated.", AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    }

    // Method to validate assets
    private static void ValidateAssets(string[] assets)
    {
        // Get the target folder paths from the settings manager
        var targetFolderPaths = FolderValidatorSettingsManager.GetFolders();

        // If there are no target folder paths, return
        if (targetFolderPaths.Count == 0) return;

        // Loop through each asset and validate it
        foreach (string assetPath in assets)
        {
            ValidateAsset(targetFolderPaths, assetPath);
        }
    }
    // Method to validate an individual asset
    private static void ValidateAsset(List<string> targetFolderPaths, string assetPath)
    {
        // If the asset is a folder or a meta file, return
        if (AssetDatabase.IsValidFolder(assetPath) || assetPath.EndsWith(".meta"))
        {
            return;
        }
        // If the asset is in Scenes folder and not a scene file, check if it is in a scene data folder, exclude it from validation
        if (assetPath.Contains("Scenes/") && !assetPath.EndsWith(".unity"))
        {
            string directoryPath = Path.GetDirectoryName(assetPath);
            string directoryName = Path.GetFileName(directoryPath);
            string parentOfDirectory = Path.GetDirectoryName(directoryPath);

            string sceneFilePath = Path.Combine(parentOfDirectory, directoryName + ".unity").Replace("\\", "/");

            if (File.Exists(sceneFilePath))
            {
                return;
            }
        }
        // Get the containing folder path
        string containingFolder = targetFolderPaths.FirstOrDefault(
            folderPath => assetPath.StartsWith(folderPath + "/")
            );
        // If the asset is not in a target folder, return
        if (containingFolder == null)
        {
            return;
        }
        // Validate the asset location and naming
        ValidateAssetLocationAndNaming(assetPath, containingFolder);
    }

    // Method to validate the asset location and naming
    private static void ValidateAssetLocationAndNaming(string assetPath, string rootFolderPath)
    {
        // Get the relative path of the asset
        string relativePath = assetPath.Substring(rootFolderPath.Length + 1);
        // Split the relative path into parts
        string[] pathParts = relativePath.Split('/');
        // Get the root folder
        string rootFolder = pathParts.Length > 0 ? pathParts[0] : string.Empty;

        // If the root folder is empty or not in the folder hierarchy, log a warning
        if (string.IsNullOrEmpty(rootFolder) || !FolderHierarchyCreator.folderHierarchy.ContainsKey(rootFolder))
        {
            LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' is in an invalid root folder inside '{rootFolderPath}'. Expected one of: {string.Join(", ", FolderHierarchyCreator.folderHierarchy.Keys)}", assetPath);
            return;
        }

        // Validate the asset type by location
        ValidateAssetTypeByLocation(assetPath, rootFolder, pathParts);

        // Validate the naming conventions
        ValidateNamingConventions(assetPath, rootFolderPath + "/", pathParts);
    }

    // Method to validate the asset type by location
    private static void ValidateAssetTypeByLocation(string assetPath, string rootFolder, string[] pathParts)
    {
        // Get the extension of the asset
        string extension = Path.GetExtension(assetPath).ToLower();
        // Get the subfolder
        string subFolder = pathParts.Length > 1 ? pathParts[1] : string.Empty;

        // Switch statement to validate the asset type by location
        switch (rootFolder)
        {
            case "Arts":
                // If the asset is in a subfolder, validate the asset type
                if (pathParts.Length > 1)
                {
                    switch (subFolder)
                    {
                        case "Textures":
                            // If the asset is not a common image format, log a warning
                            if (!new[] { ".png", ".jpg", ".jpeg", ".tif", ".tga", ".psd", ".exr", ".hdr", ".rendertexture" }.Contains(extension))
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/Textures' is not a common image format.", assetPath);
                            break;
                        case "Models":
                            // If the asset is not a common model format, log a warning
                            if (!new[] { ".fbx", ".obj" }.Contains(extension))
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/Models' is not a common model format.", assetPath);
                            break;
                        case "SFXs":
                            // If the asset is not a common audio format, log a warning
                            if (!new[] { ".wav", ".mp3", ".ogg", ".mixer" }.Contains(extension))
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/SFXs' is not a common audio format.", assetPath);
                            break;
                        case "Materials":
                            // If the asset is not a material, log a warning
                            if (extension != ".mat")
                                LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Arts/Materials' is not a material.", assetPath);
                            break;
                    }
                }
                // If the asset is not in a subfolder, log a warning
                else
                {
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.", assetPath);
                }
                break;
            case "Scripts":
                // If the asset is in a subfolder, validate the asset type
                if (pathParts.Length > 1)
                {
                    // If the asset is not a C# script, log a warning
                    if (!new[] { ".cs", ".dll", ".asmdef", ".asmref" }.Contains(extension))
                        LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Scripts' folder is not a C# script.", assetPath);
                }
                // If the asset is not in a subfolder, log a warning
                else
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.", assetPath);
                break;
            case "Presets":
                // If the asset is not a preset file, log a warning
                if (extension != ".preset")
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Presets' folder is not a preset file.", assetPath);
                break;
            case "Prefabs":
                // If the asset is not a prefab, log a warning
                if (extension != ".prefab")
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'Prefabs' folder is not a prefab.", assetPath);
                break;
            case "GameData":
                // If the asset is not a valid data asset, log a warning
                if (!new[] { ".asset", ".inputactions", ".physicmaterial" }.Contains(extension))
                    LogWarning($"{_projectStructureViolationWarningPart}'{assetPath}' in 'GameData' is not a valid data asset.", assetPath);
                break;
        }
    }


    // Method to validate the naming conventions
    private static void ValidateNamingConventions(string assetPath, string rootFolder, string[] pathParts)
    {
        // Get the file name without the extension
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        // Get the parent folder
        string parentFolder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        // Get the extension
        string extension = Path.GetExtension(assetPath).ToLower();

        // If the file name is not in PascalCase, log a warning
        if (!IsPascalCase(fileName))
        {
            LogWarning($"{_namingViolationWarningPart}<b><color=cyan>Asset name</color> '{fileName}'</b> in '{assetPath}' is not in PascalCase.", assetPath);
        }

        // Loop through each part of the path and validate it
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            // If the part is not in PascalCase, log a warning
            if (!IsPascalCase(pathParts[i]))
            {
                LogWarning($"{_namingViolationWarningPart}<b><color=red>Folder name</color> '{pathParts[i]}'</b> in '{assetPath}' is not in PascalCase.", assetPath);
            }
        }

        // If the parent folder starts with "Arts/Models", validate the model naming conventions
        if (parentFolder.StartsWith($"{rootFolder}Arts/Models"))
        {
            // If the file name does not start with "SM_" or "DM_", log a warning
            if (!fileName.StartsWith("SM_") && !fileName.StartsWith("DM_"))
            {
                LogWarning($"{_namingViolationWarningPart}Model <b>'{fileName}'</b> should start with a prefix like <b><color=yellow>'SM_' (Static Mesh) or 'DM_' (Dynamic Mesh)</color></b>.", assetPath);
            }
        }
        // If the parent folder starts with "Arts/SFXs", validate the SFX naming conventions
        if (parentFolder.StartsWith($"{rootFolder}Arts/SFXs"))
        {
            // If the extension is not ".mixer", validate the suffix
            if (extension != ".mixer")
            {
                string[] requiredSuffixes = { "_S", "_D" };
                // If the file name does not end with a required suffix, log a warning
                if (!requiredSuffixes.Any(suffix => fileName.EndsWith(suffix)))
                {
                    LogWarning($"{_namingViolationWarningPart}SFX <b>'{fileName}'</b> should end with a <b><color=yellow>suffix like {string.Join(", ", requiredSuffixes)}</color></b>.", assetPath);
                }
            }
        }
        // If the parent folder starts with "Arts/Textures", validate the texture naming conventions
        if (parentFolder.StartsWith($"{rootFolder}Arts/Textures"))
        {
            string[] requiredPrefixes = { "TC_", "RT_","T_" };

            if (fileName.StartsWith("T_"))
            {
                string[] requiredSuffixes = { "_BC", "_N", "_MS", "_H", "_AO", "_E", "_I","_EX" }; // BaseColor, Normal, Metallic, Height, AmbientOcclusion, Emission,exclusion 
                if (!requiredSuffixes.Any(suffix => fileName.EndsWith(suffix)))
                {
                    LogWarning($"{_namingViolationWarningPart}Texture <b>'{fileName}'</b> should end with a <b><color=yellow>suffix like {string.Join(", ", requiredSuffixes)}['_*_EX' For Custom Usages]</color></b>.", assetPath);
                }
            }

            else if (!requiredPrefixes.Any(suffix => fileName.StartsWith(suffix)))
            {
                LogWarning($"{_namingViolationWarningPart}Texture <b>'{fileName}'</b> should start with a <b><color=yellow>suffix like {string.Join(", ", requiredPrefixes)}</color></b>.", assetPath);
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
        TryReportIssueToIssueConsole(message, assetPath);
    }

    private static void TryReportIssueToIssueConsole(string message, string assetPath)
    {
        try
        {
            // Attempt to locate the IssueConsole API type via reflection to avoid compile-time dependency
            var apiType = Type.GetType("Palapal.IssueConsole.Runtime.IssueConsoleAPI, Palapal.IssueConsole.Runtime");
            if (apiType == null)
            {
                apiType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == "Palapal.IssueConsole.Runtime.IssueConsoleAPI");
            }

            if (apiType == null) return;

            var reportMethod = apiType.GetMethod("Report", BindingFlags.Public | BindingFlags.Static);
            if (reportMethod == null) return;

            // build metadata
            var metadata = new Dictionary<string, string> { { "assetPath", assetPath } };

            // signature: Report(string message, string stackTrace = null, LogType type = LogType.Warning, string source = null, Dictionary<string,string> metadata = null)
            reportMethod.Invoke(null, new object[] { message, null, LogType.Warning, "FolderStructure", metadata });
        }
        catch
        {
            // ignore - optional reporting
        }
    }
}
#endif