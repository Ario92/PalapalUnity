#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FolderValidatorMenu : EditorWindow
{
    private static List<string> _targetFolders;
    private Vector2 _scrollPosition;

    [MenuItem("Palapal/Folder Validator Settings")]
    public static void ShowWindow()
    {
        GetWindow<FolderValidatorMenu>("Folder Validator");
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
}
#endif