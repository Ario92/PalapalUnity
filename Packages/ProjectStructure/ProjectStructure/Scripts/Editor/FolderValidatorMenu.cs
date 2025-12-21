#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Palapal.ProjectStructure.Editor
{
public class FolderValidatorMenu : EditorWindow
{
    private static List<string> _targetFolders;
    private Vector2 _scrollPosition;

    [MenuItem("Palapal/Folder Validator Settings")]
    public static void ShowWindow()
    {
        GetWindow<FolderValidatorMenu>("Folder Validator");
    }

    // Menu is always visible; when selected it will check and display appropriate messages if nothing is validatable

    [MenuItem("Assets/Palapal/Validate Folder")]
    private static void ValidateSelectedFolder()
    {
        var selectedPaths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(p => !string.IsNullOrEmpty(p)).ToList();
        var roots = FolderValidatorSettingsManager.GetFolders();

        var foldersToValidate = selectedPaths.Where(p => AssetDatabase.IsValidFolder(p) && roots.Any(r => p == r || p.StartsWith(r + "/"))).ToList();

        if (foldersToValidate.Count == 0)
        {
            EditorUtility.DisplayDialog("Validate Folder", "No validatable folder selected. Make sure the folder is under a configured validation root.", "OK");
            return;
        }

        try
        {
            for (int i = 0; i < foldersToValidate.Count; i++)
            {
                string folder = foldersToValidate[i];
                EditorUtility.DisplayProgressBar("Validating Folder", $"Validating {folder}", (float)i / foldersToValidate.Count);
                FolderStructureValidator.ValidateAssetsInPath(folder);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("Validate Folder", $"Validated {foldersToValidate.Count} folder(s). See Console for warnings.", "OK");
    }

    private void OnEnable()
    {
        _targetFolders = FolderValidatorSettingsManager.GetFolders();
    }


    private void OnGUI()
    {
        EditorGUILayout.LabelField("Manage Folder Validation Roots", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Folders to validate their structure can be managed here.", MessageType.Info);

        if (GUILayout.Button("Add a Folder"))
        {
            // Open folder selection window.
            string path = EditorUtility.OpenFolderPanel("Select a Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }

                FolderValidatorSettingsManager.AddFolderToList(path);
                _targetFolders = FolderValidatorSettingsManager.GetFolders();
            }
        }

        EditorGUILayout.Space();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        List<string> foldersToRemove = new List<string>();
        List<string> foldersToValidate = new List<string>();

        foreach (string folder in _targetFolders)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(folder);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                foldersToRemove.Add(folder);
            }
            if (GUILayout.Button("Validate", GUILayout.Width(60)))
            {
                foldersToValidate.Add(folder);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (foldersToRemove.Count > 0)
        {
            foreach (var folder in foldersToRemove)
            {
                FolderValidatorSettingsManager.RemoveFolder(folder);
            }
            _targetFolders = FolderValidatorSettingsManager.GetFolders();
        }

        if (foldersToValidate.Count > 0)
        {
            foreach (var folder in foldersToValidate)
            {
                FolderStructureValidator.ValidateAssetsInPath(folder);
            }
        }
    }
    public static void RefreshList()
    {
        _targetFolders = FolderValidatorSettingsManager.GetFolders();
    }

    [MenuItem("Palapal/Validate All Folders")]
    public static void ValidateAll()
    {
        var folders = FolderValidatorSettingsManager.GetFolders();
        if (folders == null || folders.Count == 0)
        {
            EditorUtility.DisplayDialog("Validate All", "No folders configured to validate.", "OK");
            return;
        }

        try
        {
            for (int i = 0; i < folders.Count; i++)
            {
                string folder = folders[i];
                EditorUtility.DisplayProgressBar("Validating Folders", $"Validating {folder}", (float)i / folders.Count);
                FolderStructureValidator.ValidateAssetsInPath(folder);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("Validate All", "Validation completed. See Console for warnings.", "OK");
    }
}
}
#endif