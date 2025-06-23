
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(SnapDrawingController))]
public class SnapDrawingControllerEditor : Editor
{
    public enum EditMode
    {
        None,
        Adding,
        Editing
    }
    private enum AddPointMode
    {
        FreeOnPlane,
        SnapOnPlaneGrid,
        FreeOnCollider,
        SnapOnAngle,
        SnapOnVertex
    }

    private static EditMode currentMode = EditMode.None; // تغییر به static

    private SnapDrawingController controller;
    private List<int> selectedPointsIndices = new List<int>();
    private Vector2 marqueeStartMousePos;
    private bool isMarqueeSelecting = false;
    private Rect toolbarRect = new Rect(50, 10, 250, 80);
    // Define your grid size
    public float GridSize = 0.2f;
    string _gridSize = "0.2";
    public float SnapAngle = 0.2f;
    string _snapAngle = "0.2";

    void OnEnable()
    {
        controller = (SnapDrawingController)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        selectedPointsIndices.Clear();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tool Mode", EditorStyles.boldLabel);

        if (currentMode != EditMode.Adding)
        {
            if (GUILayout.Button("Start Adding Points"))
            {
                currentMode = EditMode.Adding;
                selectedPointsIndices.Clear();
            }
        }
        else
            if (GUILayout.Button("Stop Adding Points"))
            currentMode = EditMode.None;

        if (currentMode != EditMode.Editing)
        {
            if (GUILayout.Button("Edit/Delete Points"))
            {
                currentMode = EditMode.Editing;
            }
        }
        else
        {
            if (GUILayout.Button("Finish Editing"))
            {
                currentMode = EditMode.None;
                selectedPointsIndices.Clear();
            }
        }

        EditorGUILayout.Space();

        switch (currentMode)
        {
            case EditMode.Adding:
                EditorGUILayout.HelpBox("ADD MODE:\n- Click in the scene to add a new snapped point.\n- Hold SHIFT to add points on existing meshes.\n- Hold V to snap to vertices.", MessageType.Info);
                break;
            case EditMode.Editing:
                EditorGUILayout.HelpBox("EDIT MODE:\n- Drag the handles to move points.\n- Hold Alt and click a point to delete it.\n- Drag to marquee select multiple points.\n- Move selected points by dragging any of their handles.", MessageType.Info);
                break;
            default:
                EditorGUILayout.HelpBox("Select a mode to start.", MessageType.None);
                break;
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Reset Drawing"))
        {
            if (EditorUtility.DisplayDialog("Reset Drawing?", "Are you sure you want to delete all points?", "Yes", "No"))
            {
                Undo.RecordObject(controller, "Reset Drawing");
                controller.ResetDrawing();
                selectedPointsIndices.Clear();
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(controller);
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        Handles.BeginGUI();
        toolbarRect = GUI.Window(GUIUtility.GetControlID(FocusType.Passive), toolbarRect, DrawToolbarWindow, "");
        Handles.EndGUI();

        switch (currentMode)
        {
            case EditMode.Adding:
                HandleAddingPoints(e);
                break;
            case EditMode.Editing:
                HandleEditingPoints(e);
                break;
        }
        if (currentMode == EditMode.Adding)
        {
            ShowAddingPointGizmo(e);
        }
        DrawPointsPassively();
        if (GUI.changed)
        {
            sceneView.Repaint();
        }
    }

    public static void SetMode(EditMode newMode)
    {
        currentMode = newMode;
        SceneView.RepaintAll(); // اطمینان از به‌روزرسانی Scene View
        // اگر Inspector برای SnapDrawingController باز است، آن را هم Repaint کن
        EditorUtility.SetDirty(Selection.activeGameObject); // برای Trigger کردن Repaint
    }

    // متد جدید برای رسم محتویات پنجره ابزار
    void DrawToolbarWindow(int windowID)
    {

        GUILayout.BeginHorizontal();

        // دکمه None (حالت عادی)
        GUI.backgroundColor = (currentMode == EditMode.None) ? Color.cyan : Color.white;

        GUILayout.Label("Grid Size:");
        _gridSize = GUILayout.TextField(_gridSize);
        if (float.TryParse(_gridSize, out var t))
        {
            GridSize = t;
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        GUILayout.Label("Snap Angle:");
        _snapAngle = GUILayout.TextField(_snapAngle);
        if (float.TryParse(_snapAngle, out t))
        {

            SnapAngle = t;
        }
        GUILayout.EndHorizontal();


        GUI.backgroundColor = Color.white;



        // امکان جابجایی پنجره با ماوس
        GUI.DragWindow(new Rect(0, 0, toolbarRect.width, 20));
    }
    private void HandleAddingPoints(Event e)
    {
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Vector3? pointToAdd = GetValidPoint(e);
            if (pointToAdd.HasValue)
            {
                Undo.RecordObject(controller, "Add Point");
                controller.points.Add(pointToAdd.Value);
                controller.UpdateLineRenderer();
                e.Use();
            }
        }
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    private void HandleEditingPoints(Event e)
    {
        bool repaint = false;
        int closestPointIndex = -1;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2 mouseGUIPos = e.mousePosition;
            float minDistance = 20f;

            for (int i = 0; i < controller.points.Count; i++)
            {
                Vector2 pointGUIPos = HandleUtility.WorldToGUIPoint(controller.points[i]);
                float distance = Vector2.Distance(mouseGUIPos, pointGUIPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPointIndex = i;
                }
            }

            if (e.alt)
            {
                if (closestPointIndex != -1)
                {
                    Undo.RecordObject(controller, "Delete Point");
                    controller.points.RemoveAt(closestPointIndex);
                    selectedPointsIndices.Remove(closestPointIndex);
                    for (int i = 0; i < selectedPointsIndices.Count; i++)
                    {
                        if (selectedPointsIndices[i] > closestPointIndex)
                        {
                            selectedPointsIndices[i]--;
                        }
                    }
                    e.Use();
                    repaint = true;
                }
            }
            else
            {
                if (closestPointIndex == -1)
                {
                    isMarqueeSelecting = true;
                    marqueeStartMousePos = e.mousePosition;

                }
                //else
                //{
                //    if (e.modifiers != EventModifiers.Control && selectedPointsIndices.Count == 1) selectedPointsIndices.Clear();
                //    selectedPointsIndices.Add(closestPointIndex);
                //}


            }
        }


        else if (e.type == EventType.MouseDrag && isMarqueeSelecting)
        {
            selectedPointsIndices.Clear();
            Rect marqueeRect = GetMarqueeRect(marqueeStartMousePos, e.mousePosition);
            for (int i = 0; i < controller.points.Count; i++)
            {
                Vector2 pointGUIPos = HandleUtility.WorldToGUIPoint(controller.points[i]);
                if (marqueeRect.Contains(pointGUIPos))
                {
                    selectedPointsIndices.Add(i);
                }
            }
            e.Use();
            repaint = true;
        }
        else if (e.type == EventType.MouseUp && isMarqueeSelecting)
        {
            isMarqueeSelecting = false;
            e.Use();
            repaint = true;


        }
        if (!isMarqueeSelecting)
        {
            for (int i = 0; i < controller.points.Count; i++)
            {
                bool isSelected = selectedPointsIndices.Contains(i);
                Color originalColor = Handles.color;
                Handles.color = isSelected ? Color.yellow : new Color(0, 1, 1, 0.5f);
                if (isSelected)
                {
                    EditorGUI.BeginChangeCheck();
                    // THIS IS THE CORRECTED LINE:
                    Vector3 newPosition = Handles.PositionHandle(controller.points[i], Quaternion.identity);
                    //float newSize=   HandleUtility.GetHandleSize(controller.points[i]) * 0.2f;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(controller, "Move Point(s)");
                        if (isSelected)
                        {
                            Vector3 delta = newPosition - controller.points[i];
                            foreach (int index in selectedPointsIndices)
                            {
                                controller.points[index] += delta;
                            }
                        }
                        //else
                        //{
                        //    selectedPointsIndices.Clear();
                        //    controller.points[i] = newPosition;
                        //}
                        repaint = true;
                    }
                }
                else
                {
                    Handles.SphereHandleCap(0, controller.points[i], Quaternion.identity, 0.2f, EventType.Repaint);

                }

                Handles.color = originalColor;
            }
        }
        if (isMarqueeSelecting)
        {
            Handles.BeginGUI();
            Rect rect = GetMarqueeRect(marqueeStartMousePos, e.mousePosition);
            Handles.DrawSolidRectangleWithOutline(rect, new Color(0, 0.5f, 1f, 0.1f), new Color(0, 0.5f, 1f, 0.8f));
            Handles.EndGUI();
        }

        if (closestPointIndex != -1 && HandleUtility.nearestControl >= 0)
        {
            if (selectedPointsIndices.Contains(closestPointIndex))
            {
                if (e.modifiers == (EventModifiers.Control | EventModifiers.Shift))
                    selectedPointsIndices.Remove(closestPointIndex);
            }
            else
            {
                if (e.modifiers != EventModifiers.Control)
                    selectedPointsIndices.Clear();
                selectedPointsIndices.Add(closestPointIndex);
            }

        }

        if (repaint)
        {
            controller.UpdateLineRenderer();
        }
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    private void DrawPointsPassively()
    {
        if (currentMode == EditMode.Editing) return;

        Handles.color = new Color(0, 1, 1, 0.5f);
        foreach (var point in controller.points)
        {
            Handles.SphereHandleCap(0, point, Quaternion.identity, 0.2f, EventType.Repaint);
        }
    }

    private void ShowAddingPointGizmo(Event e)
    {
        Vector3? previewPos = GetValidPoint(e);
        if (previewPos.HasValue)
        {
            Handles.color = new Color(1, 0.5f, 0, 0.7f);
            Handles.SphereHandleCap(0, previewPos.Value, Quaternion.identity, HandleUtility.GetHandleSize(previewPos.Value) * 0.2f, EventType.Repaint);
            SceneView.currentDrawingSceneView.Repaint();
        }
    }

    #region Helper Methods
    private Vector3? GetValidPoint(Event e)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        Vector3? target = null;
        if (e.modifiers == (EventModifiers.Shift | EventModifiers.Control))
        {
            Vector3? nearestVertex = GetNearestVertex(ray);
            if (nearestVertex.HasValue)
            {
                target = nearestVertex.Value;
            }
            if (target == null)
            {
                if (TryGetPointOnCollider(ray, out hit, out target))
                    return target;
                else
                    return GetOnPlaneFreeTargetPosition(ray);
            }
            return target;
        }
        else if (e.modifiers == EventModifiers.Shift)
        {
            target = GetOnPlaneFreeTargetPosition(ray);
            if (controller.points.Count > 0 && target.HasValue)
            {
                List<Vector3> SnapDirections = GetSnapDirections();

                Vector3 lastPoint = controller.points[controller.points.Count - 1];
                target = GetSnappedPoint(target.Value, lastPoint, SnapDirections);
            }
            return target;
        }
        else if (e.modifiers == EventModifiers.Control)
        {
            return GetOnPlaneGridTargetPosition(ray);
        }

        else
        {
            if (TryGetPointOnCollider(ray, out hit, out target))
                return target;
            else
                return GetOnPlaneFreeTargetPosition(ray);
        }
    }

    private List<Vector3> GetSnapDirections()
    {
        var snapPoints = new List<Vector3>();

        if (SceneView.currentDrawingSceneView == null || SceneView.currentDrawingSceneView.camera == null)
        {
            return snapPoints;
        }

        var planeTransform = SceneView.currentDrawingSceneView.camera.transform;

        Handles.color = new Color(0, 1, 1, 0.7f);
        Vector3 initialDirection = planeTransform.right;
        snapPoints.AddRange(GetSnapDirections(planeTransform, initialDirection));

        if (180 % SnapAngle != 0)
        {
            Handles.color = new Color(1, 1, 0, 0.7f);
            initialDirection = -planeTransform.right;
            snapPoints.AddRange(GetSnapDirections(planeTransform, initialDirection));
        }

        return snapPoints;
    }

    private List<Vector3> GetSnapDirections(Transform planeTransform, Vector3 initialDirection)
    {
        var snapPoints = new List<Vector3>();
        Vector3 lastPoint = controller.points.Count > 0 ? controller.points[controller.points.Count - 1] :
                           Vector3.zero;

        for (float angle = 0; angle < 180.0f; angle += SnapAngle)
        {
            snapPoints.Add((Quaternion.AngleAxis(angle, planeTransform.forward) * initialDirection).normalized);

            if (angle > 0)
                snapPoints.Add((Quaternion.AngleAxis(-angle, planeTransform.forward) * initialDirection).normalized);
        }
        if (180 % SnapAngle == 0)
            snapPoints.Add((Quaternion.AngleAxis(180, planeTransform.forward) * initialDirection).normalized);

        foreach (var point in snapPoints)
        {
            Handles.DrawDottedLine(lastPoint, lastPoint + point * 30f, 4.0f);

            float handleSize = HandleUtility.GetHandleSize(lastPoint + point);
            Handles.SphereHandleCap(0, lastPoint + point * GridSize, Quaternion.identity, handleSize * 0.05f, EventType.Repaint);
        }
        return snapPoints;
    }

    private bool TryGetPointOnCollider(Ray ray, out RaycastHit hit, out Vector3? target)
    {
        if (Physics.Raycast(ray, out hit))
        {
            target = hit.point;
            return true;
        }
        else
        {
            target = null;
            return false;
        }
    }

    private Vector3? GetOnPlaneFreeTargetPosition(Ray ray)
    {
        Vector3 lastPoint = controller.points.Count > 0 ? controller.points[controller.points.Count - 1] :
                            Vector3.zero;

        Plane interactionPlane = new Plane(SceneView.currentDrawingSceneView.camera.transform.forward, lastPoint);



        if (interactionPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return null;
    }
    private Vector3? GetOnPlaneGridTargetPosition(Ray ray)
    {
        Vector3 lastPoint = controller.points.Count > 0 ? controller.points[controller.points.Count - 1] :
                            Vector3.zero;

        var planeTransform = SceneView.currentDrawingSceneView.camera.transform;

        Plane interactionPlane = new Plane(planeTransform.forward, lastPoint);

        Handles.color = new(0, 1, 1, 0.5f);

        var points = GetGridPointsOnPlane(planeTransform, lastPoint, new((int)SceneView.currentDrawingSceneView.cameraViewport.width, (int)SceneView.currentDrawingSceneView.cameraViewport.height), GridSize);

        Handles.DrawLines(points.ToArray());

        float enter;
        if (interactionPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            Vector3 vectorFromOrigin = hitPoint - lastPoint;

            int nearestX = Mathf.RoundToInt(Vector3.Dot(vectorFromOrigin, planeTransform.right) / GridSize);
            int nearestY = Mathf.RoundToInt(Vector3.Dot(vectorFromOrigin, planeTransform.up) / GridSize);

            return lastPoint + (planeTransform.right * nearestX * GridSize) + (planeTransform.up * nearestY * GridSize);

        }

        return null;

    }

    /// <summary>
    /// Generates a grid of points on a plane defined by a transform and an origin point.
    /// </summary>
    /// <param name="planeTransform">The transform whose 'forward' vector is the plane's normal. Its 'right' and 'up' vectors will define the grid's orientation.</param>
    /// <param name="origin">The origin point of the grid on the plane (your 'lastPoint').</param>
    /// <param name="gridSize">A Vector2Int defining the number of points in the X (width) and Y (height) dimensions of the grid.</param>
    /// <param name="cellSize">The distance between adjacent points in the grid.</param>
    /// <returns>A List<Vector3> containing the points of the grid.</returns>
    public static List<Vector3> GetGridPointsOnPlane(Transform planeTransform, Vector3 origin, Vector2Int gridSize, float cellSize)
    {
        List<Vector3> gridPoints = new List<Vector3>();

        if (gridSize.x <= 0 || gridSize.y <= 0 || cellSize <= 0)
        {
            Debug.LogError("Grid size and cell size must be positive.");
            return gridPoints;
        }

        Vector3 gridRight = planeTransform.right;
        Vector3 gridUp = planeTransform.up;
        Vector3 currentOffset, point;
        Vector3 startPoint = origin;
        for (int x = -gridSize.x / 2; x < gridSize.x / 2; x++)
        {
            currentOffset = (gridRight * x * cellSize) + (gridUp * -gridSize.y / 2 * cellSize);
            point = startPoint + currentOffset;
            gridPoints.Add(point);

            currentOffset = (gridRight * x * cellSize) + (gridUp * gridSize.y / 2 * cellSize);
            point = startPoint + currentOffset;
            gridPoints.Add(point);
        }
        for (int y = -gridSize.y / 2; y < gridSize.y / 2; y++)
        {
            currentOffset = (gridRight * -gridSize.x / 2 * cellSize) + (gridUp * y * cellSize);
            point = startPoint + currentOffset;
            gridPoints.Add(point);

            currentOffset = (gridRight * gridSize.x / 2 * cellSize) + (gridUp * y * cellSize);
            point = startPoint + currentOffset;
            gridPoints.Add(point);
        }
        return gridPoints;
    }
    private Vector3 GetSnappedPoint(Vector3 currentMousePos, Vector3 lastPoint, List<Vector3> SnapDirections)
    {
        Vector3 directionToMouse = (currentMousePos - lastPoint);
        float distanceToMouse = directionToMouse.magnitude;
        if (distanceToMouse < 0.001f) return lastPoint;

        directionToMouse.Normalize();
        Vector3 bestSnapDirection = Vector3.zero;
        float maxDotProduct = -1f;

        foreach (var snapDir in SnapDirections)
        {
            float dot = Vector3.Dot(directionToMouse, snapDir);
            if (dot > maxDotProduct)
            {
                maxDotProduct = dot;
                bestSnapDirection = snapDir;
            }
        }
        return lastPoint + (bestSnapDirection * distanceToMouse);
    }

    private Vector3? GetNearestVertex(Ray ray)
    {
        float minScreenDistance = float.MaxValue;
        Vector3? closestWorldVertex = null;

        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            UnityEngine.Transform meshTransform = mf.transform;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldVertex = meshTransform.TransformPoint(vertices[i]);
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldVertex);

                float dist = Vector2.Distance(Event.current.mousePosition, screenPos);

                if (dist < minScreenDistance)
                {
                    minScreenDistance = dist;
                    closestWorldVertex = worldVertex;
                }
            }
        }

        if (minScreenDistance < 20f)
        {
            return closestWorldVertex;
        }
        return null;
    }

    private Rect GetMarqueeRect(Vector2 startPos, Vector2 endPos)
    {
        Vector2 min = Vector2.Min(startPos, endPos);
        Vector2 max = Vector2.Max(startPos, endPos);
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    #endregion
}