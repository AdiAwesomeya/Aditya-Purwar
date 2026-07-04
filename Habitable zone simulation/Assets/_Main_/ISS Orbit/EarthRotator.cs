using UnityEngine;

public class EarthRotator : MonoBehaviour
{
    [Tooltip("Real Earth rotation is 86400 seconds. Use smaller value for visual effect.")]
    [SerializeField] private float rotationPeriodSeconds = 240f;

    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private void Update()
    {
        if (rotationPeriodSeconds <= 0f)
            return;

        float degreesPerSecond = 360f / rotationPeriodSeconds;
        transform.Rotate(rotationAxis.normalized, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}