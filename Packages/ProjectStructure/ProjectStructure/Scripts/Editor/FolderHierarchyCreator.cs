#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace Palapal.Shared
{
    /// <summary>
    /// Create a Folder Hierarchy under a selected 
    /// </summary>
    public class FolderHierarchyCreator : EditorWindow
    {
        // Define a nested dictionary for multi-level folder structure
        private static readonly Dictionary<string, object> folderHierarchy = new Dictionary<string, object>
        {
            { 
                "_Assets", new Dictionary<string, object> {
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
                }
            }
        };

        [MenuItem("Palapal/Create Folder Hierarchy")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FolderHierarchyCreator), false, "Folder Hierarchy Creator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select Base Folder to Create Hierarchy", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Folder and Create Hierarchy"))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Base Folder", "Assets", "");

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    CreateFolderHierarchy(selectedPath, folderHierarchy);
                    AssetDatabase.Refresh();
                    Debug.Log("Folder hierarchy created successfully.");
                }
                else
                {
                    Debug.LogWarning("No folder selected.");
                }
            }
        }

        private static void CreateFolderHierarchy(string baseFolderPath, Dictionary<string, object> hierarchy)
        {
            foreach (var entry in hierarchy)
            {
                string folderPath = Path.Combine(baseFolderPath, entry.Key);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // If this folder has nested subfolders, recursively create them
                if (entry.Value is Dictionary<string, object> subfolders)
                {
                    CreateFolderHierarchy(folderPath, subfolders);
                }
            }
        }
    }
}
#endif