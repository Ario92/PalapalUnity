#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FolderValidatorSettingsManager
{
    // کلیدی که برای ذخیره مسیرها در PlayerPrefs استفاده می‌شود.
    private const string PrefsKey = "FolderValidator_TargetPaths";
    private const char Delimiter = ';'; // کاراکتر جداکننده مسیرها

    /// <summary>
    /// لیست مسیرهای ذخیره شده را از PlayerPrefs می‌خواند.
    /// </summary>
    public static List<string> GetFolders()
    {
        string savedPaths = EditorPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(savedPaths))
        {
            return new List<string>();
        }
        return savedPaths.Split(Delimiter).ToList();
    }

    /// <summary>
    /// لیست مسیرها را در PlayerPrefs ذخیره می‌کند.
    /// </summary>
    private static void SaveFolders(List<string> paths)
    {
        // حذف مسیرهای تکراری یا خالی
        var distinctPaths = paths.Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        string pathsToSave = string.Join(Delimiter.ToString(), distinctPaths);
        EditorPrefs.SetString(PrefsKey, pathsToSave);
    }

    /// <summary>
    /// یک فولدر جدید را به لیست اضافه می‌کند.
    /// این متد عمومی برای استفاده در اسکریپت ساخت فولدر شما طراحی شده است.
    /// </summary>
    /// <param name="folderPath">مسیر فولدری که باید اضافه شود (مثلا "Assets/MyProject")</param>
    public static void AddFolderToList(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;

        List<string> currentFolders = GetFolders();
        if (!currentFolders.Contains(folderPath))
        {
            currentFolders.Add(folderPath);
            SaveFolders(currentFolders);
            Debug.Log($"مسیر '{folderPath}' با موفقیت به لیست اعتبارسنجی اضافه شد.");
        }
    }

    /// <summary>
    /// یک فولدر را از لیست حذف می‌کند.
    /// </summary>
    public static void RemoveFolder(string folderPath)
    {
        List<string> currentFolders = GetFolders();
        if (currentFolders.Remove(folderPath))
        {
            SaveFolders(currentFolders);
        }
    }
}
#endif