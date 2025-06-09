#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ThisSome1.ColorfulHierarchy
{
    [InitializeOnLoad]
    internal class StyleHierarchy
    {
        static StyleHierarchy()
        {
            // Check if the color palette asset is importing.
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindow;
        }

        private static void OnHierarchyWindow(int instanceID, Rect selectionRect)
        {
            Object instance = EditorUtility.InstanceIDToObject(instanceID);
            if (instance == null)
                return;

            // Check if the name of each gameObject is begin with keyChar in colorDesigns list.
            if (System.Text.RegularExpressions.Regex.IsMatch(instance.name, @"\\\\ .+"))
            {
                if (instance is not GameObject)
                    return;

                GameObject thisGO = instance as GameObject;

                if (thisGO.transform.localPosition != Vector3.zero || thisGO.transform.localRotation != Quaternion.identity || thisGO.transform.localScale != Vector3.one)
                {
                    Undo.RecordObject(thisGO.transform, "Reset Folder Transform");
                    foreach (Transform child in thisGO.transform)
                    {
                        Undo.RecordObject(child, "Reset Folder Transform");
                        child.localPosition += thisGO.transform.localPosition;
                        child.localRotation *= thisGO.transform.localRotation;
                        child.localScale.Scale(thisGO.transform.localScale);
                    }
                    thisGO.transform.SetLocalPositionAndRotation(new(0, 0, 0), Quaternion.identity);
                    thisGO.transform.localScale = new(1, 1, 1);
                }

                foreach (Component c in thisGO.GetComponents<Component>())
                    if (c is not Transform and not ColorDesign)
                        Object.DestroyImmediate(c);
                if (!thisGO.TryGetComponent(out ColorDesign cd))
                    cd = thisGO.AddComponent<ColorDesign>();
                cd.enabled = true;

                // Get the desired folder design from the palette
                cd.Settings ??= new();
                FolderDesign design = cd.Settings;

                // Create a new GUIStyle to match the design in colorDesigns list.
                GUIStyle nameStyle = new()
                {
                    fontSize = design.fontSize,
                    fontStyle = design.fontStyle,
                    alignment = design.textAlignment,
                    normal = new GUIStyleState() { textColor = design.textColor }
                };

                // Draw a rectangle as a background, and set the color.
                design.backgroundColor.a = 1;
                Rect boxRect = new(selectionRect.x, selectionRect.y, selectionRect.height, selectionRect.height);
                Rect nameRect = new(selectionRect.x + selectionRect.height, selectionRect.y, selectionRect.width - selectionRect.height, selectionRect.height);
                if (thisGO && !thisGO.activeInHierarchy)
                {
                    EditorGUI.DrawRect(boxRect, Color.white);
                    EditorGUI.DrawRect(nameRect, design.backgroundColor);
                    // Draw a label to show the name without the prefix and with the newStyle.
                    EditorGUI.LabelField(nameRect, instance.name[(instance.name.IndexOf(' ') + 1)..], nameStyle);
                    EditorGUI.LabelField(boxRect, "X", new GUIStyle() { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState() { textColor = Color.red } });
                }
                else
                {
                    EditorGUI.DrawRect(selectionRect, design.backgroundColor);
                    // Draw a label to show the name without the prefix and with the newStyle.
                    EditorGUI.LabelField(selectionRect, instance.name[(instance.name.IndexOf(' ') + 1)..], nameStyle);
                }
            }
            else if (instance is GameObject && (instance as GameObject).TryGetComponent(out ColorDesign nc))
                Object.DestroyImmediate(nc);
        }

        [MenuItem("GameObject/Palapal/Colorful Hierarchy/Colored Folder", true)]
        private static bool SelectionHaveSameParent()
        {
            if (Selection.count < 2) return true;
            Transform pr = Selection.activeGameObject.transform.parent;
            foreach (var child in Selection.gameObjects)
                if (child.transform.parent != pr) return false;
            return true;
        }
        [MenuItem("GameObject/Palapal/Colorful Hierarchy/Colored Folder", false)]
        private static void CreateNewFolder(MenuCommand cmd)
        {
            bool notFound = cmd.context != null;
            if (notFound && Selection.count > 0)
                foreach (Object obj in Selection.objects)
                    if (obj == cmd.context)
                        notFound = false;
            if (notFound)
                return;

            var folder = new GameObject(@"\\ Colored Folder");
            if (Selection.count > 0)
                folder.transform.parent = Selection.activeGameObject.transform.parent;
            foreach (GameObject go in Selection.gameObjects)
            {
                Undo.RegisterFullObjectHierarchyUndo(go, "Moved to folder");
                GameObjectUtility.SetParentAndAlign(go, folder);
            }
            Undo.RegisterCreatedObjectUndo(folder, "new colored folder");
            Selection.activeObject = folder;
        }
        [MenuItem("GameObject/Palapal/Colorful Hierarchy/Folder Structure", true)]
        private static bool NoSelection() => Selection.count < 2;
        [MenuItem("GameObject/Palapal/Colorful Hierarchy/Folder Structure", false)]
        private static void CreateFolderStructure() => SelectStructureWindow.ShowWindow();
    }
}
#endif