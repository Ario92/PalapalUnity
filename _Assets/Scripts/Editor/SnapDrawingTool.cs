using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;


[EditorTool("Snap Drawing/Add", targetType = typeof(SnapDrawingController), toolPriority = 1)]
public class SnapDrawingTool_AddMode : EditorTool
{
    public SnapDrawingController TargetController => target as SnapDrawingController;
    public override void OnActivated() => SnapDrawingControllerEditor.SetMode(SnapDrawingControllerEditor.EditMode.Adding);
    public override void OnWillBeDeactivated() => SnapDrawingControllerEditor.SetMode(SnapDrawingControllerEditor.EditMode.None);
    public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("Toolbar Plus", "AddMode");
}

[EditorTool("Snap Drawing/Edit", targetType = typeof(SnapDrawingController), toolPriority = 2)]
public class SnapDrawingTool_EditMode : EditorTool
{
    public SnapDrawingController TargetController => target as SnapDrawingController;
    public override void OnActivated() => SnapDrawingControllerEditor.SetMode(SnapDrawingControllerEditor.EditMode.Editing);
    public override void OnWillBeDeactivated() => SnapDrawingControllerEditor.SetMode(SnapDrawingControllerEditor.EditMode.None);
    public override GUIContent toolbarIcon => EditorGUIUtility.IconContent("editicon.sml", "EditMode");
}