using UnityEngine;

public class MercuryOrbitScript : MonoBehaviour
{
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

    void Awake() => lr = GetComponent<LineRenderer>();

    void Update()
    {
        if (sun == null || mercury == null) return;
        DrawEllipse();
    }

    void DrawEllipse()
    {

        // Ensure the total distance is valid

        a = 386.78f;               // Semi-major axis
        float c = 79.53f;         // Distance from center to focus
        b = 378.52f;      // Semi-minor axis
        eccentricity = c/a;

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
