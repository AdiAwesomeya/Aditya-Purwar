using UnityEngine;

public class EarthOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Orbit Settings")]
    public float distance = 18f;
    public float minDistance = 8f;
    public float maxDistance = 40f;

    public float xSpeed = 180f;
    public float ySpeed = 120f;

    public float yMinLimit = -80f;
    public float yMaxLimit = 80f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 8f;
    public float zoomSmoothness = 10f;

    [Header("Movement Smoothness")]
    public float rotationSmoothness = 10f;

    [Header("Mouse Controls")]
    public int orbitMouseButton = 0; // 0 = Left Mouse, 1 = Right Mouse, 2 = Middle Mouse

    private float x = 0f;
    private float y = 20f;

    private float targetDistance;
    private Quaternion currentRotation;
    private Vector3 currentPosition;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("EarthOrbitCamera: Target is not assigned.");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        targetDistance = distance;

        UpdateCameraInstant();
    }

    private void LateUpdate()
    {
        HandleMouseOrbit();
        HandleMouseZoom();
        HandleTouchControls();

        distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * zoomSmoothness);

        Quaternion targetRotation = Quaternion.Euler(y, x, 0f);
        Vector3 targetPosition = target.position - targetRotation * Vector3.forward * distance;

        currentRotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothness);
        currentPosition = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * rotationSmoothness);

        transform.rotation = currentRotation;
        transform.position = currentPosition;
    }

    private void HandleMouseOrbit()
    {
        if (Input.GetMouseButton(orbitMouseButton))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }
    }

    private void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetDistance -= scroll * zoomSpeed * 10f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    private void HandleTouchControls()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;

                x += delta.x * xSpeed * 0.01f * Time.deltaTime;
                y -= delta.y * ySpeed * 0.01f * Time.deltaTime;

                y = ClampAngle(y, yMinLimit, yMaxLimit);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevious = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevious = touchOne.position - touchOne.deltaPosition;

            float previousTouchDistance = Vector2.Distance(touchZeroPrevious, touchOnePrevious);
            float currentTouchDistance = Vector2.Distance(touchZero.position, touchOne.position);

            float difference = currentTouchDistance - previousTouchDistance;

            targetDistance -= difference * 0.01f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    private void UpdateCameraInstant()
    {
        Quaternion rotation = Quaternion.Euler(y, x, 0f);
        Vector3 position = target.position - rotation * Vector3.forward * distance;

        transform.rotation = rotation;
        transform.position = position;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;

        return Mathf.Clamp(angle, min, max);
    }
}