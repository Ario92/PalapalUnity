using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SnapDrawingController : MonoBehaviour
{
    // لیست نقاط همچنان اینجا ذخیره می‌شود
    public List<Vector3> points = new List<Vector3>();
     
    private LineRenderer lineRenderer;

    void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
    }

    // این متد برای به‌روزرسانی خط از اسکریپت Editor فراخوانی خواهد شد
    public void UpdateLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) return; // اگر LineRenderer پیدا نشد، ادامه نده
        }

        // تنظیمات اولیه
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 10;
        lineRenderer.numCornerVertices = 10; // برای گوشه‌های صاف‌تر

        // می‌توانید متریال و رنگ را نیز در اینجا تنظیم کنید
        if (lineRenderer.sharedMaterial == null)
        {
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")); // یک متریال پیش‌فرض
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
        }

        // به‌روزرسانی نقاط
        lineRenderer.positionCount = points.Count;
        if (points.Count > 0)
        {
            lineRenderer.SetPositions(points.ToArray());
        }
    }

    public void ResetDrawing()
    {
        points.Clear();
        UpdateLineRenderer();
    }
}