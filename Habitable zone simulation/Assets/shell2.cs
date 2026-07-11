using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class shell2 : MonoBehaviour
{
    public annulusScript annuScript;
    private Mesh mesh;
    private MeshRenderer meshRenderer;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "DynamicHabitableZone";
        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void LateUpdate()
    {
        if (annuScript == null) return;

        float rawLuminosity = annuScript.luminosity;
        if (float.IsNaN(rawLuminosity) || float.IsInfinity(rawLuminosity) || rawLuminosity < 0.0001f) return;

        // 1. Calculate current radii
        float innerRadius = Mathf.Sqrt(rawLuminosity / 1.1f) * annuScript.scaleMultiplier;
        float outerRadius = Mathf.Sqrt(rawLuminosity / 0.53f) * annuScript.scaleMultiplier;

        // 2. CAMERA SAFETY: If the camera enters the zone, hide the mesh 
        // to prevent giant polygons from clipping through the camera lens.
        float distToCamera = Vector3.Distance(Camera.main.transform.position, transform.position);
        if (distToCamera < outerRadius + 10f)
        {
            // Instead of breaking, smoothly hide the mesh when the camera is too close
            meshRenderer.enabled = false;
            return;
        }
        else
        {
            meshRenderer.enabled = true;
        }

        // 3. DYNAMIC SEGMENTS: Add more sides automatically as the sphere grows!
        // Small sphere = 12 segments (saves performance). Giant sphere = up to 64 segments (stays perfectly smooth).
        int segments = Mathf.Clamp(Mathf.RoundToInt(outerRadius / 10f), 12, 64);
        int rings = segments;

        GenerateDynamicShell(innerRadius, outerRadius, segments, rings);
    }

    void GenerateDynamicShell(float innerRadius, float outerRadius, int segments, int rings)
    {
        int loopLength = (segments + 1) * (rings + 1);
        Vector3[] vertices = new Vector3[loopLength * 2];
        int[] triangles = new int[segments * rings * 6 * 2];
        
        int vIdx = 0;
        // Generate Outer Sphere
        for (int r = 0; r <= rings; r++) {
            float phi = Mathf.PI * (float)r / rings;
            for (int s = 0; s <= segments; s++) {
                float theta = Mathf.PI * 2f * (float)s / segments;
                Vector3 dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                vertices[vIdx++] = dir * outerRadius;
            }
        }
        // Generate Inner Sphere
        for (int r = 0; r <= rings; r++) {
            float phi = Mathf.PI * (float)r / rings;
            for (int s = 0; s <= segments; s++) {
                float theta = Mathf.PI * 2f * (float)s / segments;
                Vector3 dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                vertices[vIdx++] = dir * innerRadius;
            }
        }

        int tIdx = 0;
        for (int r = 0; r < rings; r++) {
            for (int s = 0; s < segments; s++) {
                int current = s + r * (segments + 1);
                int next = current + segments + 1;

                // Outer Faces
                triangles[tIdx++] = current;
                triangles[tIdx++] = next + 1;
                triangles[tIdx++] = current + 1;
                triangles[tIdx++] = current;
                triangles[tIdx++] = next;
                triangles[tIdx++] = next + 1;

                // Inner Faces
                int iCurrent = current + loopLength;
                int iNext = next + loopLength;
                triangles[tIdx++] = iCurrent;
                triangles[tIdx++] = iCurrent + 1;
                triangles[tIdx++] = iNext + 1;
                triangles[tIdx++] = iCurrent;
                triangles[tIdx++] = iNext + 1;
                triangles[tIdx++] = iNext;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}