#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
namespace Palapal.Shared
{
    /// <summary>
    /// Merge one folder to another by move files.
    /// </summary>
    /// <seealso cref="UnityEditor.EditorWindow" />
    public class FolderMerger : EditorWindow
    {
        private string sourceFolderPath;
        private string targetFolderPath;

        [MenuItem("Tools/Palapal/Merge Folders")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FolderMerger), false, "Folder Merger");
        }

        private void OnGUI()
        {
            GUILayout.Label("Merge Folders", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Source Folder"))
            {
                sourceFolderPath = EditorUtility.OpenFolderPanel("Select Source Folder", "Assets", "");
            }
            EditorGUILayout.LabelField("Source Folder:", sourceFolderPath);

            if (GUILayout.Button("Select Target Folder"))
            {
                targetFolderPath = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
            }
            EditorGUILayout.LabelField("Target Folder:", targetFolderPath);

            if (GUILayout.Button("Merge Folders") && !string.IsNullOrEmpty(sourceFolderPath) && !string.IsNullOrEmpty(targetFolderPath))
            {
                if (sourceFolderPath != targetFolderPath)
                {
                    MergeFolders(sourceFolderPath, targetFolderPath);
                    AssetDatabase.Refresh();
                    Debug.Log("Folders merged successfully.");
                }
                else
                {
                    Debug.LogWarning("Source and target folders cannot be the same.");
                }
            }
        }

        private void MergeFolders(string sourcePath, string targetPath)
        {
            string relativeSourcePath = "Assets" + sourcePath.Substring(Application.dataPath.Length);
            string relativeTargetPath = "Assets" + targetPath.Substring(Application.dataPath.Length);

            // Recursively move files from source to target folder
            MoveAssetsRecursively(relativeSourcePath, relativeTargetPath);

            // Delete the original source folder after moving
            if (AssetDatabase.IsValidFolder(relativeSourcePath))
            {
                AssetDatabase.DeleteAsset(relativeSourcePath);
            }
        }

        private void MoveAssetsRecursively(string sourcePath, string targetPath)
        {
            // Ensure target folder exists
            if (!AssetDatabase.IsValidFolder(targetPath))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(targetPath), Path.GetFileName(targetPath));
            }

            // Move files in the current folder
            string[] files = AssetDatabase.FindAssets("", new[] { sourcePath });
            foreach (string guid in files)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string targetFilePath = Path.Combine(targetPath, Path.GetFileName(assetPath));

                // Only move files, not folders
                if (!AssetDatabase.IsValidFolder(assetPath))
                {
                    var r = AssetDatabase.MoveAsset(assetPath, targetFilePath);
                    if (!string.IsNullOrEmpty(r))
                        Debug.LogError(r);
                }
            }

            // Recursively move subfolders
            string[] subfolders = AssetDatabase.GetSubFolders(sourcePath);
            foreach (string subfolder in subfolders)
            {
                string subfolderName = Path.GetFileName(subfolder);
                MoveAssetsRecursively(subfolder, Path.Combine(targetPath, subfolderName));
            }
        }
    }
}
#endif
