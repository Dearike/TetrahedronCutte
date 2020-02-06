using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSlice : MonoBehaviour {

    [SerializeField] private GameObject plane;
    [SerializeField] private Transform ObjectContainer;

    // Расстояние расхождения кусков результирующих объектов (R)
    [SerializeField] private float separation;

    // Плосткость разреза
    private Plane slicePlane = new Plane();
    [SerializeField] private bool drawPlane;

    [SerializeField] private ScreenLineRenderer lineRenderer;

    private MeshCutter meshCutter;
    private TempMesh biggerMesh, smallerMesh;

    public bool IsObjectDragNow;


    private void DrawPlane(Vector3 start, Vector3 end, Vector3 normalVec)
    {
        Quaternion rotate = Quaternion.FromToRotation(Vector3.up, normalVec);

        plane.transform.localRotation = rotate;
        plane.transform.position = (end + start) / 2;
        plane.SetActive(true);
    }

    private void Start ()
    {
        meshCutter = new MeshCutter(256);
    }

    private void OnEnable()
    {
        lineRenderer.OnLineDrawn += OnLineDrawn;
    }

    private void OnDisable()
    {
        lineRenderer.OnLineDrawn -= OnLineDrawn;
    }

    private void OnLineDrawn(Vector3 start, Vector3 end, Vector3 depth)
    {
        var planeTangent = (end - start).normalized;

        // Если разрезание по клику
        if (planeTangent == Vector3.zero)
        {
            planeTangent = Vector3.right;
        }

        var normalVec = Vector3.Cross(depth, planeTangent);

        if (drawPlane) DrawPlane(start, end, normalVec);

        SliceObjects(start, normalVec);
    }


    private void SliceObjects(Vector3 point, Vector3 normal)
    {
        var toSlice = GameObject.FindGameObjectsWithTag("Sliceable");

        // Разрезанные куски будем кладем в положительный и отрицательный массив для разделения мешей при разрезе
        List<Transform> positive = new List<Transform>();
        List<Transform> negative = new List<Transform>();

        GameObject obj;
        bool slicedAny = false;

        for (int i = 0; i < toSlice.Length; ++i)
        {
            obj = toSlice[i];
            
            var transformedNormal = ((Vector3)(obj.transform.localToWorldMatrix.transpose * normal)).normalized;

            slicePlane.SetNormalAndPosition(
                transformedNormal,
                obj.transform.InverseTransformPoint(point));

            slicedAny = SliceObject(ref slicePlane, obj, positive, negative) || slicedAny;
        }

        // Разделяем меши, если что-нибудь разрезали
        if (slicedAny)
        { 
            SeparateMeshes(positive, negative, normal);
        }
    }

    private bool SliceObject(ref Plane slicePlane, GameObject sliceObject, List<Transform> positiveObjects, List<Transform> negativeObjects)
    {
        var meshFilter = sliceObject.GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;

        if (!meshCutter.SliceMesh(mesh, ref slicePlane))
        {
            // Распределяем куски объекта в соответствующие списки
            if (slicePlane.GetDistanceToPoint(meshCutter.GetFirstVertex()) >= 0)
            {
                positiveObjects.Add(sliceObject.transform);
            }
            else
            {
                negativeObjects.Add(sliceObject.transform);
            }

            return false;
        }

        // Определяем больший меш для исходного объекта
        bool posBigger = meshCutter.PositiveMesh.surfaceArea > meshCutter.NegativeMesh.surfaceArea;

        if (posBigger)
        {
            biggerMesh = meshCutter.PositiveMesh;
            smallerMesh = meshCutter.NegativeMesh;
        }
        else
        {
            biggerMesh = meshCutter.NegativeMesh;
            smallerMesh = meshCutter.PositiveMesh;
        }

        //Новый меньший отрезанный объект 
        GameObject newObject = Instantiate(sliceObject, ObjectContainer);
        newObject.transform.SetPositionAndRotation(sliceObject.transform.position, sliceObject.transform.rotation);
        var newObjMesh = newObject.GetComponent<MeshFilter>().mesh;

        //Распределение мешей и переопределение MeshCollider'ов
        ReplaceMesh(mesh, biggerMesh, sliceObject.GetComponent<MeshCollider>());
        ReplaceMesh(newObjMesh, smallerMesh, newObject.GetComponent<MeshCollider>());

        (posBigger ? positiveObjects : negativeObjects).Add(sliceObject.transform);
        (posBigger ? negativeObjects : positiveObjects).Add(newObject.transform);

        return true;
    }


    /// <summary>
    /// Заменяем старый меш на новый из буфера tempMesh.
    /// </summary>
    private void ReplaceMesh(Mesh mesh, TempMesh tempMesh, MeshCollider collider = null)
    {
        mesh.Clear();
        mesh.SetVertices(tempMesh.vertices);
        mesh.SetTriangles(tempMesh.triangles, 0);
        mesh.SetNormals(tempMesh.normals);
        mesh.SetUVs(0, tempMesh.uvs);
        
        mesh.RecalculateTangents();

        if (collider != null && collider.enabled)
        {
            collider.sharedMesh = mesh;
            collider.convex = true;
        }
    }

    private void SeparateMeshes(Transform posTransform, Transform negTransform, Vector3 localPlaneNormal)
    {
        // Нормаль в мировом пространстве
        Vector3 worldNormal = ((Vector3)(posTransform.worldToLocalMatrix.transpose * localPlaneNormal)).normalized;

        Vector3 separationVec = worldNormal * separation;

        // Раздвигаем куски
        posTransform.position += separationVec;
        negTransform.position -= separationVec;
    }

    private void SeparateMeshes(List<Transform> positives, List<Transform> negatives, Vector3 worldPlaneNormal)
    {
        //StartCoroutine(SmoothSeparatePositiveMeshes(positives, worldPlaneNormal));
        //StartCoroutine(SmoothSeparateNegativeMeshes(negatives, worldPlaneNormal));

        int i;
        var separationVector = worldPlaneNormal * separation;

        for (i = 0; i < positives.Count; ++i)
        {
            positives[i].transform.position += separationVector;
        }

        for (i = 0; i < negatives.Count; ++i)
        {
            negatives[i].transform.position -= separationVector;
        }
    }

    private IEnumerator SmoothSeparatePositiveMeshes(List<Transform> positives, Vector3 worldPlaneNormal)
    {
        int i;
        var separationVector = worldPlaneNormal * separation;
        var curretnSeparationVector = Vector3.zero;

        for (i = 0; i < positives.Count; ++i)
        {
            while (curretnSeparationVector.magnitude < separationVector.magnitude)
            {
                positives[i].transform.position += separationVector * Time.deltaTime;
                curretnSeparationVector += separationVector * Time.deltaTime;
                yield return null;
            }

        }
    }

    private IEnumerator SmoothSeparateNegativeMeshes(List<Transform> negatives, Vector3 worldPlaneNormal)
    {
        int i;
        var separationVector = worldPlaneNormal * separation;
        var curretnSeparationVector = Vector3.zero;

        for (i = 0; i < negatives.Count; ++i)
        {
            while (curretnSeparationVector.magnitude < separationVector.magnitude)
            {
                negatives[i].transform.position -= separationVector * Time.deltaTime;
                curretnSeparationVector += separationVector * Time.deltaTime;
                yield return null;
            }
        }
    }
}
