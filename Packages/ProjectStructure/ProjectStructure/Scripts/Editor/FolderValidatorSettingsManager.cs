#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Palapal.ProjectStructure.Editor;
using UnityEditor;
using UnityEngine;

public static class FolderValidatorSettingsManager
{
    private const string PrefsKey = "FolderValidator_TargetPaths";
    private const char Delimiter = ';';


    public static List<string> GetFolders()
    {
        string savedPaths = EditorPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(savedPaths))
        {
            return new List<string>();
        }
        return savedPaths.Split(Delimiter).ToList();
    }


    private static void SaveFolders(List<string> paths)
    {
        var distinctPaths = paths.Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        string pathsToSave = string.Join(Delimiter.ToString(), distinctPaths);
        EditorPrefs.SetString(PrefsKey, pathsToSave);
    }

    public static void AddFolderToList(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;

        List<string> currentFolders = GetFolders();
        if (!currentFolders.Contains(folderPath))
        {
            currentFolders.Add(folderPath);
            SaveFolders(currentFolders);
            FolderStructureValidator.ValidateAssetsInPath(folderPath);
            // Refresh the Menu's cached list (static method) instead of referencing a non-existent Instance
            FolderValidatorMenu.RefreshList();
        }

    }

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