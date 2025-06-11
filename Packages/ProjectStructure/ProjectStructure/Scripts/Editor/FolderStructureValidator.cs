// Assets/Editor/FolderStructureValidator.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions; // برای استفاده از متدهای LINQ مانند Any()

public class FolderStructureValidator : AssetPostprocessor
{
    // تعریف سلسله مراتب فولدرهای مورد انتظار شما
    // کلید: نام فولدر، مقدار: دیکشنری برای زیرفولدرها یا null اگر برگ (leaf) باشد
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

    // متد فراخوانی شده پس از وارد شدن یک Asset
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets.Concat(movedAssets))
        {
            if (AssetDatabase.IsValidFolder(assetPath) || assetPath.EndsWith(".meta") || !assetPath.StartsWith("Assets/_Assets/"))
            {
                continue;
            }

            ValidateAssetLocationAndNaming(assetPath);
        }
    }
    private static void ValidateAssetLocationAndNaming(string assetPath)
    {
        string relativePath = assetPath.Substring("Assets/_Assets/".Length);
        string[] pathParts = relativePath.Split('/');
        string rootFolder = pathParts.Length > 0 ? pathParts[0] : string.Empty;

        // 1. بررسی وجود در فولدر ریشه معتبر
        if (string.IsNullOrEmpty(rootFolder) || !folderHierarchy.ContainsKey(rootFolder))
        {
            Debug.LogError($"Asset '{assetPath}' is in an invalid root folder. Expected one of: {string.Join(", ", folderHierarchy.Keys)}");
            return;
        }

        // 2. بررسی نوع فایل در فولدرهای مشخص
        ValidateAssetTypeByLocation(assetPath, rootFolder, pathParts);

        // 3. بررسی قوانین نام‌گذاری
        ValidateNamingConventions(assetPath, pathParts);
    }

    /// <summary>
    /// بررسی می‌کند که آیا نوع Asset با پوشه‌ای که در آن قرار دارد مطابقت دارد یا خیر.
    /// </summary>
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
                                Debug.LogWarning($"Asset '{assetPath}' in 'Arts/Textures' is not a common image format.");
                            break;
                        case "Models":
                            if (!new[] { ".fbx", ".obj" }.Contains(extension)) // obj نیز اضافه شد
                                Debug.LogWarning($"Asset '{assetPath}' in 'Arts/Models' is not a common model format.");
                            break;
                        case "Materials":
                            if (extension != ".mat")
                                Debug.LogWarning($"Asset '{assetPath}' in 'Arts/Materials' is not a material.");
                            break;
                    }
                }
                else
                {
                    Debug.LogWarning($"Asset '{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.");
                }
                break;
            case "Scripts":
                if (pathParts.Length > 1)
                {
                    if (extension != ".cs")
                        Debug.LogWarning($"Asset '{assetPath}' in 'Scripts' folder is not a C# script.");
                }
                else
                    Debug.LogWarning($"Asset '{assetPath}' in '{rootFolder}' is not in a valid path. Consider moving it.");
                break;
            case "Presets":
                if (extension != ".preset")
                    Debug.LogWarning($"Asset '{assetPath}' in 'Presets' folder is not a preset file.");
                break;
            case "Prefabs":
                if (extension != ".prefab")
                    Debug.LogWarning($"Asset '{assetPath}' in 'Prefabs' folder is not a prefab.");
                break;
            case "GameData":
                if (!new[] { ".asset", ".inputactions", ".physicmaterial" }.Contains(extension))
                    Debug.LogWarning($"Asset '{assetPath}' in 'GameData' is not a valid data asset.");
                break;
        }
    }

    /// <summary>
    /// بررسی قوانین نام‌گذاری شامل PascalCase، پیشوندها و پسوندها.
    /// </summary>
    private static void ValidateNamingConventions(string assetPath, string[] pathParts)
    {
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string parentFolder = Path.GetDirectoryName(assetPath).Replace('\\', '/');

        // قانون 1: بررسی PascalCase برای نام فایل
        if (!IsPascalCase(fileName))
        {
            Debug.LogWarning($"Naming Convention Violation: Asset name '{fileName}' in '{assetPath}' is not in PascalCase.");
        }

        // بررسی PascalCase برای نام پوشه‌ها
        // منهای یک چون آخرین بخش نام فایل است
        for (int i = 0; i < pathParts.Length - 1; i++)
        {
            if (!IsPascalCase(pathParts[i]))
            {
                Debug.LogWarning($"Naming Convention Violation: Folder name '{pathParts[i]}' in '{assetPath}' is not in PascalCase.");
            }
        }

        // قانون 2: بررسی پیشوند برای مدل‌ها
        if (parentFolder.EndsWith("Arts/Models"))
        {
            if (!fileName.StartsWith("SM_") && !fileName.StartsWith("DM_"))
            {
                Debug.LogWarning($"Naming Convention Violation: Model '{fileName}' should start with a prefix like 'SM_' (Static Mesh) or 'DM_' (Dynamic Mesh).");
            }
        }

        // قانون 3: بررسی پسوند برای تکسچرها
        if (parentFolder.EndsWith("Arts/Textures"))
        {
            string[] requiredPrefixes = { "TC_", "RT_"}; 
            
            if (fileName.StartsWith("T_"))
            {
                string[] requiredSuffixes = { "_BC", "_N", "_MS", "_H", "_AO", "_E", "_I" }; // BaseColor, Normal, Metallic, Height, AmbientOcclusion, Emission
                if (!requiredSuffixes.Any(suffix => fileName.EndsWith(suffix)))
                {
                    Debug.LogWarning($"Naming Convention Violation: Texture '{fileName}' should end with a suffix like {string.Join(", ", requiredSuffixes)}.");
                }
            }

            else if (!requiredPrefixes.Any(suffix => fileName.StartsWith(suffix)))
            {
                Debug.LogWarning($"Naming Convention Violation: Texture '{fileName}' should start with a suffix like {string.Join(", ", requiredPrefixes)}.");
            }
        }

        // می‌توانید قوانین بیشتری برای سایر Asset ها اضافه کنید
        // مثال برای متریال‌ها
        if (parentFolder.EndsWith("Arts/Materials"))
        {
            if (!fileName.StartsWith("M_"))
            {
                Debug.LogWarning($"Naming Convention Violation: Material '{fileName}' should start with the prefix 'M_'.");
            }
        }
    }

    /// <summary>
    /// بررسی می‌کند که آیا رشته ورودی به فرمت PascalCase است یا خیر.
    /// </summary>
    private static bool IsPascalCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return true; // رشته خالی معتبر در نظر گرفته می‌شود

        // یک رشته PascalCase باید با حرف بزرگ شروع شود و شامل حروف و اعداد باشد.
        // استفاده از Regex برای بررسی دقیق‌تر
        return Regex.IsMatch(str, @"^[A-Z][a-zA-Z0-9 ]*$");
    }
  
}