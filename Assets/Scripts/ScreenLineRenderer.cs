using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenLineRenderer : MonoBehaviour {

    public delegate void LineDrawnHandler(Vector3 begin, Vector3 end, Vector3 depth);
    public event LineDrawnHandler OnLineDrawn;

    bool dragging;
    Vector3 start;
    Vector3 end;
    Camera cam;

    public Material lineMaterial;

    private MouseSlice mouseSlice;

    void Start () {
        cam = Camera.main;
        dragging = false;

        mouseSlice = FindObjectOfType<MouseSlice>();
    }

    private void OnEnable()
    {
        Camera.onPostRender += PostRenderDrawLine;
    }

    private void OnDisable()
    {
        Camera.onPostRender -= PostRenderDrawLine;
    }

    void Update ()
    {
        if (mouseSlice.IsObjectDragNow)
        {
            dragging = false;
            return;
        }

        if (!dragging && Input.GetMouseButtonDown(0))
        {
            start = cam.ScreenToViewportPoint(Input.mousePosition);
            dragging = true;
        }

        if (dragging)
        {
            end = cam.ScreenToViewportPoint(Input.mousePosition);
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            end = cam.ScreenToViewportPoint(Input.mousePosition);
            dragging = false;

            var startRay = cam.ViewportPointToRay(start);
            var endRay = cam.ViewportPointToRay(end);

            OnLineDrawn?.Invoke(
                startRay.GetPoint(cam.nearClipPlane),
                endRay.GetPoint(cam.nearClipPlane),
                endRay.direction.normalized);
        }
    }


    /// <summary>
    /// Рисует линию в области видового экрана, используя переменные начала и конца
    /// </summary>
    private void PostRenderDrawLine(Camera cam)
    {
        if (mouseSlice.IsObjectDragNow)
        {
            return;
        }

        if (dragging && lineMaterial)
        {
            GL.PushMatrix();
            lineMaterial.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.LINES);
            GL.Color(Color.black);
            GL.Vertex(start);
            GL.Vertex(end);
            GL.End();
            GL.PopMatrix();
        }
    }
}
