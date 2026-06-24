using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class annulusScript : MonoBehaviour
{
    public float luminosity = 1f;
    public float innerDist = 0.9f;
    public float outerDist = 1.2f;
    public star starScript;
    
    public float scaleMultiplier = 10f; 
    
    // Set this higher in the Inspector (e.g., 32, 64, or 128) to make the circle perfectly smooth!
    [Range(8, 128)]
    public int segments = 64; 

    private Mesh mesh;

    void Start()
    {
        starScript = GameObject.FindGameObjectWithTag("sunStuff").GetComponent<star>();
        
        // Setup a freshly generated procedural mesh instance
        mesh = new Mesh();
        mesh.name = "ProceduralAnnulus";
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        float temperature = starScript.temperature;
        float radius = starScript.radius;
        luminosity = 4 * Mathf.PI * Mathf.Pow(radius, 2) * 5.67037442E-8f * Mathf.Pow(temperature, 4);
        luminosity = luminosity/3.82799E26f;
        
        innerDist = Mathf.Sqrt(luminosity / 1.1f);
        outerDist = Mathf.Sqrt(luminosity / 0.53f);

        float scaledInner = innerDist * scaleMultiplier;
        float scaledOuter = outerDist * scaleMultiplier;

        // --- PROCEDURAL CIRCULAR GEOMETRY GENERATION ---
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            // Calculate progress angle completely around the 360 degree layout
            float progress = (float)i / segments;
            float angle = progress * Mathf.PI * 2f;

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Compute precise circular positions
            vertices[i * 2] = new Vector3(cos * scaledInner, 0f, sin * scaledInner);         // Inner ring vertex
            vertices[i * 2 + 1] = new Vector3(cos * scaledOuter, 0f, sin * scaledOuter);     // Outer ring vertex
        }

        // Bind the circular vertices together into triangle polygon faces
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int currentInner = i * 2;
            int currentOuter = i * 2 + 1;
            int nextInner = (i + 1) * 2;
            int nextOuter = (i + 1) * 2 + 1;

            // First triangle face construction
            triangles[triIndex++] = currentInner;
            triangles[triIndex++] = currentOuter;
            triangles[triIndex++] = nextInner;

            // Second triangle face construction
            triangles[triIndex++] = nextInner;
            triangles[triIndex++] = currentOuter;
            triangles[triIndex++] = nextOuter;
        }

        // Rebuild mesh architecture explicitly using procedural math data
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
