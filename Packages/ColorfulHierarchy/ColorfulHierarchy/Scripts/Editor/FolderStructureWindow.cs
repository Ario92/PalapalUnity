#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThisSome1.ColorfulHierarchy
{
    public class FolderStructureWindow : EditorWindow
    {
        private static string _structuresPath;
        private static readonly List<bool> _folds = new();

        [MenuItem("Palapal/ColorfulHierarchy/Folder Structures")]
        private static void ShowWindow()
        {
            _structuresPath = FolderStructureSO.GetDataPath;
            _folds.Clear();

            var window = GetWindow<FolderStructureWindow>();
            window.titleContent = new GUIContent("Folder Structures");
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        private void OnGUI()
        {
            if (_structuresPath == null || _structuresPath == "")
                _structuresPath = FolderStructureSO.GetDataPath;

            FolderStructureSO savedStructures = AssetDatabase.LoadAssetAtPath<FolderStructureSO>(_structuresPath);
            if (savedStructures == null)
            {
                if (!System.IO.Directory.Exists(_structuresPath[.._structuresPath.LastIndexOf('/')]))
                    System.IO.Directory.CreateDirectory(_structuresPath[.._structuresPath.LastIndexOf('/')]);
                FolderStructureSO folderStructure = CreateInstance<FolderStructureSO>();
                AssetDatabase.CreateAsset(folderStructure, _structuresPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                OnGUI();
            }
            else
            {
                if (savedStructures.Structures.Length == 0)
                {
                    EditorGUILayout.LabelField("No Structures Found.");
                    EditorGUILayout.LabelField("Select edit button and add structures.");
                }
                else
                {
                    if (_folds.Count < savedStructures.Structures.Length)
                        for (int i = 0; i < savedStructures.Structures.Length - _folds.Count; i++)
                            _folds.Add(false);
                    EditorGUILayout.LabelField("Structures", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);
                    for (int i = 0; i < savedStructures.Structures.Length; i++)
                    {
                        EditorGUI.indentLevel = 0;
                        _folds[i] = EditorGUILayout.Foldout(_folds[i], savedStructures.Structures[i].Title);
                        if (_folds[i])
                        {
                            Color originalColor = GUI.backgroundColor;
                            foreach (FolderData folder in savedStructures.Structures[i].Folders)
                                ViewFolderRecursive(folder, 2);
                            GUI.backgroundColor = originalColor;
                        }
                    }
                }

                EditorGUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Edit Structures", GUILayout.Width(200)))
                {
                    Selection.activeObject = savedStructures;
                    EditorGUIUtility.PingObject(savedStructures);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private void ViewFolderRecursive(FolderData folderData, int indentLevel = 0)
        {
            EditorGUI.indentLevel = indentLevel;
            GUI.backgroundColor = folderData.Design.backgroundColor;
            if (folderData.SubFolders.Length > 0)
            {
                EditorGUILayout.LabelField(folderData.Name, new GUIStyle()
                {
                    fontSize = folderData.Design.fontSize,
                    fontStyle = folderData.Design.fontStyle,
                    alignment = folderData.Design.textAlignment,
                    normal = new GUIStyleState() { textColor = folderData.Design.textColor, background = Texture2D.whiteTexture }
                });
                if (folderData.SubFolders.Length > 0)
                    foreach (FolderData subFolder in folderData.SubFolders)
                        ViewFolderRecursive(subFolder, indentLevel + 1);
            }
            else
                EditorGUILayout.LabelField(folderData.Name, new GUIStyle()
                {
                    fontSize = folderData.Design.fontSize,
                    fontStyle = folderData.Design.fontStyle,
                    alignment = folderData.Design.textAlignment,
                    normal = new GUIStyleState() { textColor = folderData.Design.textColor, background = Texture2D.whiteTexture }
                });
        }
    }

    public class SelectStructureWindow : EditorWindow
    {
        private static string _structuresPath;

        internal static void ShowWindow()
        {
            _structuresPath = FolderStructureSO.GetDataPath;
            var window = GetWindow<SelectStructureWindow>();
            window.titleContent = new GUIContent("Select Structure");
            window.minSize = new Vector2(200, 100);
            window.Show();
        }

        private void OnGUI()
        {
            if (_structuresPath == null || _structuresPath == "")
                _structuresPath = FolderStructureSO.GetDataPath;

            FolderStructureSO savedStructures = AssetDatabase.LoadAssetAtPath<FolderStructureSO>(_structuresPath);
            if (savedStructures == null)
            {
                if (!System.IO.Directory.Exists(_structuresPath[.._structuresPath.LastIndexOf('/')]))
                    System.IO.Directory.CreateDirectory(_structuresPath[.._structuresPath.LastIndexOf('/')]);
                FolderStructureSO folderStructure = CreateInstance<FolderStructureSO>();
                AssetDatabase.CreateAsset(folderStructure, _structuresPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                OnGUI();
            }
            else if (savedStructures.Structures.Length == 0)
            {
                EditorGUILayout.LabelField("No Structures Found.");
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Edit Structures", GUILayout.Width(200)))
                {
                    Selection.activeObject = savedStructures;
                    EditorGUIUtility.PingObject(savedStructures);
                    GetWindow<SelectStructureWindow>().Close();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (FolderStructure structure in savedStructures.Structures)
                    if (GUILayout.Button(structure.Title))
                    {
                        foreach (FolderData folder in structure.Folders)
                            CreateFolderRecursive(folder, Selection.activeGameObject);
                        GetWindow<SelectStructureWindow>().Close();
                    }
            }
        }

        private void CreateFolderRecursive(FolderData folderData, GameObject parent = null)
        {
            GameObject folder = new($@"\\ {folderData.Name}");
            Undo.RegisterCreatedObjectUndo(folder, "Load Structure");
            ColorDesign design = folder.AddComponent<ColorDesign>();
            design.Settings = folderData.Design;

            if (parent)
                folder.transform.SetParent(parent.transform);
            foreach (FolderData subFolder in folderData.SubFolders)
                CreateFolderRecursive(subFolder, folder);
        }
    }
}

#endif