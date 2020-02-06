using System.Collections.Generic;
using UnityEngine;

public static class MeshUtils
{
    /// <summary>
    /// Возвращает центр многоугольника путем усреднения вершин
    /// </summary>
    public static Vector3 FindCenter(List<Vector3> pairs)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < pairs.Count; i += 2)
        {
            center += pairs[i];
            count++;
        }

        return center / count;
    }

    /// <summary>
    /// Замыкаем многоугольник, переупорядочивая список пар векторов
    /// </summary>
    public static void ReorderList(List<Vector3> pairs)
    {
        int nbFaces = 0;
        int faceStart = 0;
        int i = 0;

        while (i < pairs.Count)
        {
            // Ищем следующую соседную грань
            for (int j = i + 2; j < pairs.Count; j += 2)
            {
                if (pairs[j] == pairs[i + 1])
                {
                    // Заносим j в i+2
                    SwitchPairs(pairs, i + 2, j);
                    break;
                }
            }


            if (i + 3 >= pairs.Count)
            {
                //Debug.Log("Не понятно, почему это происходит");
                break;
            }
            else if (pairs[i + 3] == pairs[faceStart])
            {
                // Грань готова
                nbFaces++;
                i += 4;
                faceStart = i;
            }
            else
            {
                i += 2;
            }
        }
    }

    private static void SwitchPairs(List<Vector3> pairs, int index1, int index2)
    {
        if (index1 == index2) return;

        Vector3 temp1 = pairs[index1];
        Vector3 temp2 = pairs[index1 + 1];
        pairs[index1] = pairs[index2];
        pairs[index1 + 1] = pairs[index2 + 1];
        pairs[index2] = temp1;
        pairs[index2 + 1] = temp2;
    }
}
