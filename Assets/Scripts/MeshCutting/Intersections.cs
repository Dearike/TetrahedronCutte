using System;
using System.Collections.Generic;
using UnityEngine;

public class Intersections
{
    /// <summary>
    /// https://gdbooks.gitbooks.io/3dcollisions/content/Chapter2/static_aabb_plane.html
    /// </summary>
    public static bool BoundPlaneIntersect(Mesh mesh, ref Plane plane)
    {
        // Вычисляем радиус проекции
        float r = mesh.bounds.extents.x * Mathf.Abs(plane.normal.x) +
            mesh.bounds.extents.y * Mathf.Abs(plane.normal.y) +
            mesh.bounds.extents.z * Mathf.Abs(plane.normal.z);

        // Вычисляем расстояние от центра меша до плоскости
        float s = Vector3.Dot(plane.normal, mesh.bounds.center) - (-plane.distance);

        // Пересечение происходит, когда расстояние s попадает в интервал [-r, + r]
        return Mathf.Abs(s) <= r;
    }

    // Единоразовая нициализия массивов, чтобы не инициализировать их каждый раз при вызове TrianglePlaneIntersect
    private readonly Vector3[] v;
    private readonly Vector2[] u;
    private readonly int[] t;
    private readonly bool[] positive;

    // Используется в методе пересечения
    private Ray edgeRay;

    public Intersections()
    {
        v = new Vector3[3];
        u = new Vector2[3];
        t = new int[3];
        positive = new bool[3];
    }

    /// <summary>
    /// Ппересечение между плоскостью и отрезком.
    /// </summary>
    public ValueTuple<Vector3, Vector2> Intersect(Plane plane, Vector3 first, Vector3 second, Vector2 uv1, Vector2 uv2)
    {
        edgeRay.origin = first;
        edgeRay.direction = (second - first).normalized;
        float dist;
        float maxDist = Vector3.Distance(first, second);

        if (!plane.Raycast(edgeRay, out dist))
        {
            throw new UnityException("Пересечение в неправильном направлении");
        }
        else if (dist > maxDist)
        {
            throw new UnityException("Пересечение за пределами отрезка");
        }

        var returnVal = new ValueTuple<Vector3, Vector2>
        {
            Item1 = edgeRay.GetPoint(dist)
        };

        var relativeDist = dist / maxDist;
        // Вычисление нового uv через линейную интерполяцию между uv1 и uv2
        returnVal.Item2.x = Mathf.Lerp(uv1.x, uv2.x, relativeDist);
        returnVal.Item2.y = Mathf.Lerp(uv1.y, uv2.y, relativeDist);
        return returnVal;
    }

    public bool TrianglePlaneIntersect(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, int startIdx, ref Plane plane, TempMesh posMesh, TempMesh negMesh, Vector3[] intersectVectors)
    {
        int i;

        for(i = 0; i < 3; ++i)
        {
            t[i] = triangles[startIdx + i];
            v[i] = vertices[t[i]];
            u[i] = uvs[t[i]];
        }

        // Находится ли вершина на положительной сетке
        posMesh.ContainsKeys(triangles, startIdx, positive);

        // Если они все на одной стороне, не пересечения нет
        if (positive[0] == positive[1] && positive[1] == positive[2])
        {
            // Все точки на одной стороне, пересечения нет
            // Добавляем их в положительный или отрицательный меш
            (positive[0] ? posMesh : negMesh).AddOriginalTriangle(t);
            return false;
        }

        // Поиск одиноой точки
        int lonelyPoint = 0;
        if (positive[0] != positive[1])
        {
            lonelyPoint = positive[0] != positive[2] ? 0 : 1;
        }
        else
        {
            lonelyPoint = 2;
        }

        // Устанавливаем предыдущую точку относительно порядка передней грани
        int prevPoint = lonelyPoint - 1;
        if (prevPoint == -1) prevPoint = 2;
        // Устанавливаем следующую точку
        int nextPoint = lonelyPoint + 1;
        if (nextPoint == 3) nextPoint = 0;

        // Получаем 2 точки пересечения
        ValueTuple<Vector3, Vector2> newPointPrev = Intersect(plane, v[lonelyPoint], v[prevPoint], u[lonelyPoint], u[prevPoint]);
        ValueTuple<Vector3, Vector2> newPointNext = Intersect(plane, v[lonelyPoint], v[nextPoint], u[lonelyPoint], u[nextPoint]);

        // Добавляем новые треугольники и сохраните их в соответствующих буферных мешах
        (positive[lonelyPoint] ? posMesh : negMesh).AddSlicedTriangle(t[lonelyPoint], newPointNext.Item1, newPointPrev.Item1, newPointNext.Item2, newPointPrev.Item2);

        (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[prevPoint], newPointPrev.Item1, newPointPrev.Item2, t[nextPoint]);

        (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[nextPoint], newPointPrev.Item1, newPointNext.Item1, newPointPrev.Item2, newPointNext.Item2);

        // Возвращаем ребро, которое будет в правильной ориентации для положительного меша
        if (positive[lonelyPoint])
        {
            intersectVectors[0] = newPointPrev.Item1;
            intersectVectors[1] = newPointNext.Item1;
        } else
        {
            intersectVectors[0] = newPointNext.Item1;
            intersectVectors[1] = newPointPrev.Item1;
        }
        return true;
    }
}
