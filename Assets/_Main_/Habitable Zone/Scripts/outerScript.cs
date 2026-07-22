using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class outerScript : MonoBehaviour
{
    public annulusScript annuScript;
    private Mesh mesh;
    private Vector3[] baseVertices; 
    private int cachedSegments = -1; // Tracks if annulus resolution changed

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "ProceduralSphere";
        GetComponent<MeshFilter>().mesh = mesh;
        
        RebuildSphereIfTopologyChanges();
    }

    void LateUpdate()
    {
        if (annuScript == null) return;

        // Automatically rebuild the core shape if your annulus uses a different segment count
        RebuildSphereIfTopologyChanges();

        if (baseVertices == null) return;

        // Calculate exact target radius matching the annulus inner ring
        float targetRadius = Mathf.Sqrt(annuScript.luminosity / 0.53f) * annuScript.scaleMultiplier;

        Vector3[] updatedVertices = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            updatedVertices[i] = baseVertices[i] * targetRadius;
        }

        mesh.vertices = updatedVertices;
        mesh.RecalculateNormals(); 
        mesh.RecalculateBounds();  
    }

    void RebuildSphereIfTopologyChanges()
    {
        // Fallback to 40 if the annulus script segment count can't be fetched
        int targetSegments = (annuScript != null) ? annuScript.segments : 40;

        // Only run heavy structural array allocations if the resolution actually changes
        if (targetSegments == cachedSegments) return;
        cachedSegments = targetSegments;

        int longitudes = targetSegments;
        int latitudes = targetSegments;
        
        Vector3[] vertices = new Vector3[(longitudes + 1) * (latitudes + 1)];
        int[] triangles = new int[longitudes * latitudes * 6];

        for (int lat = 0; lat <= latitudes; lat++) {
            float a1 = Mathf.PI * (float)lat / latitudes;
            float sin1 = Mathf.Sin(a1);
            float cos1 = Mathf.Cos(a1);

            for (int lon = 0; lon <= longitudes; lon++) {
                float a2 = Mathf.PI * 2f * (float)lon / longitudes;
                float sin2 = Mathf.Sin(a2);
                float cos2 = Mathf.Cos(a2);

                // Pristine 1-unit base coordinates
                vertices[lon + lat * (longitudes + 1)] = new Vector3(sin1 * cos2, cos1, sin1 * sin2);
            }
        }

        int idx = 0;
        for (int lat = 0; lat < latitudes; lat++) {
            for (int lon = 0; lon < longitudes; lon++) {
                int current = lon + lat * (longitudes + 1);
                int next = current + longitudes + 1;

                triangles[idx++] = current;
                triangles[idx++] = next + 1;
                triangles[idx++] = current + 1;

                triangles[idx++] = current;
                triangles[idx++] = next;
                triangles[idx++] = next + 1;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        baseVertices = vertices; // Store the clean template
    }
}