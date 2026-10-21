using UnityEngine;

public class MercuryOrbitScript : MonoBehaviour
{
    private const float SemiMajorAxis = 386.78f;
    private const float FocusDistance = 79.53f;
    private const float SemiMinorAxis = 378.52f;
    public Transform sun;
    public Transform mercury;
    public float totalDistance = 774f; // Sum of distances to foci (must be > distance between foci)
    public int segments = 100;
    public float eccentricity = 0;
    public float a;
    public float b;
    private LineRenderer lr;
    public mercuryScript mercScript;
    public Transform reference;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        a = SemiMajorAxis;
        b = SemiMinorAxis;
        eccentricity = FocusDistance / SemiMajorAxis;
    }

    void Update()
    {
        if (sun == null || reference == null || lr == null) return;
        DrawEllipse();
    }

    public Vector3 PositionAtEccentricAnomaly(double anomaly)
    {
        Vector3 center = reference.position;
        Quaternion rotation = Quaternion.LookRotation((center - sun.position).normalized, Vector3.up);
        Vector3 point = new Vector3(
            (float)(b * System.Math.Sin(anomaly)), 0f,
            (float)(-a * System.Math.Cos(anomaly)));
        return center + rotation * point;
    }

    void DrawEllipse()
    {

        // Ensure the total distance is valid

        Vector3 center = reference.position;
        // Vector3 direction = (mercury.position - sun.position).normalized;
        Vector3 direction = (reference.position - sun.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2;
            // Parametric points in local space
            Vector3 point = new Vector3(Mathf.Cos(angle) * b, 0, Mathf.Sin(angle) * a);
            // Rotate and translate to world space
            lr.SetPosition(i, center + (rotation * point));
        }
    }
}
