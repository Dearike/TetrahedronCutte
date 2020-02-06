using System;
using System.Collections;
using UnityEngine;

public class RotationByMouse : MonoBehaviour
{
    [SerializeField] private float rorationSpeed = 500f;
    [SerializeField] private float deltaRotation = 5f; // S

    public float rotationX, prevRotationX;
    public float rotationY, prevRotationY;

    public event Action OnStarDrag = () => { };
    public event Action OnEndDrag = () => { };

    public MouseSlice mouseSlice;

    private void Start()
    {
        mouseSlice = FindObjectOfType<MouseSlice>();
    }

    private void Update()
    {
        if (Mathf.Abs(rotationX) > 0.01f)
        {
            transform.Rotate(Vector3.up, -rotationX);
            rotationX *= 0.99f;
        }

        if (Mathf.Abs(rotationY) > 0.01f)
        {
            transform.Rotate(Vector3.right, rotationY);
            rotationY *= 0.99f;
        }
    }

    private void OnMouseDrag()
    {
        rotationX = Input.GetAxis("Mouse X") * rorationSpeed * Mathf.Deg2Rad;
        rotationY = Input.GetAxis("Mouse Y") * rorationSpeed * Mathf.Deg2Rad;

        mouseSlice.IsObjectDragNow = true;

        //if (Mathf.Abs(rotationX) - Mathf.Abs(rotationX) > deltaRotation ||
        //   Mathf.Abs(rotationY) - Mathf.Abs(rotationY) > deltaRotation)
        //{
        //    mouseSlice.IsObjectDragNow = true;
        //}

        //prevRotationX = rotationX;
        //prevRotationY = rotationY;
    }

    private void OnMouseUp()
    {
        mouseSlice.IsObjectDragNow = false;
    }
}
