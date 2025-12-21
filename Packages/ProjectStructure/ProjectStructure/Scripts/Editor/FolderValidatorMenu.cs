#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

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
                Debug.Log("[Validate Folder] No validatable folder selected. Make sure the folder is under a configured validation root.");
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

             Debug.Log("[Validate Folder] Validation complete. ");
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

            List<string> foldersToRemove = new();
            List<string> foldersToValidate = new();

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
                 Debug.Log("[Validate All] No folders configured to validate. Go to Palapal > Folder Validation > Manage Folder Validation Roots to add folders.");
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

             Debug.Log("[Validate All] Validation complete. ");
        }
        [MenuItem("Assets/Palapal/ConvertToPascalCase")]
        public static void ToPascalCase()
        {
            var selectedPaths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(p => !string.IsNullOrEmpty(p) && !p.EndsWith(".meta")).Select(file => file.Replace("\\", "/")).ToList();
            string name, newName;

            typeof(Undo).GetMethod("RegisterAssetsMoveUndo", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { selectedPaths.ToArray() });

            for (int j = 0; j < selectedPaths.Count; j++)
            {
                try
                {
                    name = Path.GetFileNameWithoutExtension(selectedPaths[j]);
                    if (string.IsNullOrEmpty(name))
                        name = Path.GetDirectoryName(selectedPaths[j]);
                    if (string.IsNullOrEmpty(name))
                        continue;

                    name = name.Split('/')?[^1];
                    if (string.IsNullOrEmpty(name))
                        continue;

                    var strParts = name.Split('_');
                    for (int k = 0; k < strParts.Length; k++)
                    {
                        var parts = System.Text.RegularExpressions.Regex.Split(strParts[k], "[^A-Za-z0-9]+").Where(p => !string.IsNullOrEmpty(p)).ToArray();

                        for (int i = 0; i < parts.Length; i++)
                        {
                            var p = parts[i];
                            if (p.Length == 0) continue;
                            parts[i] = char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..] : "");
                        }
                        strParts[k] = string.Join("", parts);
                    }
                    newName = string.Join("_", strParts);

                    if (string.IsNullOrEmpty(newName) || newName.Equals(name))
                        continue;

                    string err = AssetDatabase.RenameAsset(selectedPaths[j], newName);
                    if (string.IsNullOrEmpty(err))
                        Debug.Log($"PascalCaseFix: Renamed '{selectedPaths[j]}' -> '{newName}'");
                    else
                        Debug.LogWarning($"PascalCaseFix: Failed to rename '{selectedPaths[j]}': {err}");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }
    }
}
#endif