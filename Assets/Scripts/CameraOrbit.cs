using UnityEngine;

public class CameraOrbit : MonoBehaviour {

    [SerializeField] private Transform target;
    public Transform Target
    {
        get { return target; }
        set { target = value; }
    }

    [SerializeField] private float distance = 2.0f;
    [SerializeField] private float xSpeed = 5.0f;
    [SerializeField] private float ySpeed = 5.0f;
    [SerializeField] private float yMinLimit = -90f;
    [SerializeField] private float yMaxLimit = 90f;
    [SerializeField] private float distanceMin = 2f;
    [SerializeField] private float distanceMax = 10f;

    private float rotationYAxis = 0.0f;
    private float rotationXAxis = 0.0f;
    private Vector3 fixedPosition;

    void Start () {

        Vector3 angles = transform.eulerAngles;
        rotationYAxis = angles.y;
        rotationXAxis = angles.x;

        if (target)
        {
            fixedPosition = target.position;
        }
    }

    private void LateUpdate()
    {
        if (target)
        {
            if (Input.GetMouseButton(1))
            {
                rotationYAxis += xSpeed * Input.GetAxis("Mouse X") * distance;
                rotationXAxis -= ySpeed * Input.GetAxis("Mouse Y");
                rotationXAxis = ClampAngle(rotationXAxis, yMinLimit, yMaxLimit);
            }

            Quaternion toRotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0);
            Quaternion rotation = toRotation;

            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + fixedPosition;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
        {
            angle += 360F;
        }

        if (angle > 360F)
        {
            angle -= 360F;
        }

        return Mathf.Clamp(angle, min, max);
    }
}
