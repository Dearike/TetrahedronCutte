using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCutter
{
    public TempMesh PositiveMesh { get; private set; }
    public TempMesh NegativeMesh { get; private set; }

    private List<Vector3> addedPairs;

    private readonly List<Vector3> ogVertices;
    private readonly List<int> ogTriangles;
    private readonly List<Vector3> ogNormals;
    private readonly List<Vector2> ogUvs;

    private readonly Vector3[] intersectPair;
    private readonly Vector3[] tempTriangle;

    private Intersections intersect;

    private readonly float threshold = 1e-6f;

    public MeshCutter(int initialArraySize)
    {
        PositiveMesh = new TempMesh(initialArraySize);
        NegativeMesh = new TempMesh(initialArraySize);

        addedPairs = new List<Vector3>(initialArraySize);
        ogVertices = new List<Vector3>(initialArraySize);
        ogNormals = new List<Vector3>(initialArraySize);
        ogUvs = new List<Vector2>(initialArraySize);
        ogTriangles = new List<int>(initialArraySize * 3);

        intersectPair = new Vector3[2];
        tempTriangle = new Vector3[3];

        intersect = new Intersections();
    }

    /// <summary>
    /// Разрезание меша по плоскости разреза.
    /// Мы предполагаем, что плоскость уже находится в локальной системе координат меша
    /// Возвращает posMesh и negMesh, которые являются мешами по разные стороны от плоскости разреза
    /// (posMesh на стороне нормали плоскости разреза, negMesh на противоположной стороне)
    /// </summary>
    public bool SliceMesh(Mesh mesh, ref Plane slice)
    {
        // Заполняем массив вершин, чтобы мы могли получить к нему доступ, даже если не было пересечения
        mesh.GetVertices(ogVertices);

        if (!Intersections.BoundPlaneIntersect(mesh, ref slice))
        {
            return false;
        }

        mesh.GetTriangles(ogTriangles, 0);
        mesh.GetNormals(ogNormals);
        mesh.GetUVs(0, ogUvs);

        PositiveMesh.Clear();
        NegativeMesh.Clear();
        addedPairs.Clear();

        // Распределение старых вершин в новых мешах
        for (int i = 0; i < ogVertices.Count; ++i)
        {
            if (slice.GetDistanceToPoint(ogVertices[i]) >= 0)
                PositiveMesh.AddVertex(ogVertices, ogNormals, ogUvs, i);
            else
                NegativeMesh.AddVertex(ogVertices, ogNormals, ogUvs, i);
        }

        // Если у одной из мешей нет вершин, то не пересекается
        if (NegativeMesh.vertices.Count == 0 || PositiveMesh.vertices.Count == 0)
        {
            return false;
        }

        // Разделяй треугольники и разрезай те, которые пересекают плоскость
        for (int i = 0; i < ogTriangles.Count; i += 3)
        {
            if (intersect.TrianglePlaneIntersect(ogVertices, ogUvs, ogTriangles, i, ref slice, PositiveMesh, NegativeMesh, intersectPair))
                addedPairs.AddRange(intersectPair);
        }

        if (addedPairs.Count > 0)
        {
            FillBoundaryFace(addedPairs);
            return true;
        } else
        {
            throw new UnityException("Ошибка: если добавленные пары пустые, мы должны были вернуть false раньше");
        }
    }

    public Vector3 GetFirstVertex()
    {
        if (ogVertices.Count == 0)
        {
            throw new UnityException("Ошибка: либо у меша нет вершин, либо GetFirstVertex был вызван до SliceMesh.");
        }
        else
        {
            return ogVertices[0];
        }
    }

    #region Boundary fill method


    private void FillBoundaryGeneral(List<Vector3> added)
    {
        // Упорядочиваем по порядку их появления по периметру
        MeshUtils.ReorderList(added);

        Vector3 center = MeshUtils.FindCenter(added);

        // Создаем треугольник для каждого ребра к центру
        tempTriangle[2] = center;

        for (int i = 0; i < added.Count; i += 2)
        {
            // Добавляем внешний треугольник в meshPositive
            tempTriangle[0] = added[i];
            tempTriangle[1] = added[i + 1];

            PositiveMesh.AddTriangle(tempTriangle);

            // Добавить задний треугольник  в meshNegative
            tempTriangle[0] = added[i + 1];
            tempTriangle[1] = added[i];

            NegativeMesh.AddTriangle(tempTriangle);
        }
    }


    private void FillBoundaryFace(List<Vector3> added)
    {
        // Упорядочиваем по порядку их появления по периметру
        MeshUtils.ReorderList(added);

        // Находим фактические вершины грани
        var face = FindRealPolygon(added);

        // Создаем дочерние треугольники
        int t_fwd = 0;
        int t_bwd = face.Count - 1;
        int t_new = 1;

        bool incr_fwd = true;

        while (t_new != t_fwd && t_new != t_bwd)
        {
            AddTriangle(face, t_bwd, t_fwd, t_new);

            if (incr_fwd) t_fwd = t_new;
            else t_bwd = t_new;

            incr_fwd = !incr_fwd;
            t_new = incr_fwd ? t_fwd + 1 : t_bwd - 1;
        }
    }

    /// <summary>
    /// Получение многоугольника из пар вершин.
    /// Например, два коллинеарных вектора избыточны и образуют только одну сторону многоугольника.
    /// </summary>
    private List<Vector3> FindRealPolygon(List<Vector3> pairs)
    {
        List<Vector3> vertices = new List<Vector3>();
        Vector3 edge1, edge2;

        // Упорядочиваем список пар
        for (int i = 0; i < pairs.Count; i += 2)
        {
            edge1 = (pairs[i + 1] - pairs[i]);
            if (i == pairs.Count - 2)
            {
                edge2 = pairs[1] - pairs[0];
            }
            else
            {
                edge2 = pairs[i + 3] - pairs[i + 2];
            }

            edge1.Normalize();
            edge2.Normalize();

            if (Vector3.Angle(edge1, edge2) > threshold)
            {
                // Угол
                vertices.Add(pairs[i + 1]);
            }
        }

        return vertices;
    }

    private void AddTriangle(List<Vector3> face, int t1, int t2, int t3)
    {
        tempTriangle[0] = face[t1];
        tempTriangle[1] = face[t2];
        tempTriangle[2] = face[t3];
        PositiveMesh.AddTriangle(tempTriangle);

        tempTriangle[1] = face[t3];
        tempTriangle[2] = face[t2];
        NegativeMesh.AddTriangle(tempTriangle);
    }

    #endregion
}
