using UnityEngine;

public class SSScript : MonoBehaviour
{
    public annulusScript annuScript;
    public star starScript;
    
    // Reference to the Renderer of the spherical shell
    public Renderer shellRenderer; 

    // Names of the properties in your shader
    [SerializeField] private string innerRadiusPropertyName = "_Inner_Radius";
    [SerializeField] private string outerRadiusPropertyName = "_Outer_Radius";

    // Cached property IDs for better performance
    private int innerRadiusID;
    private int outerRadiusID;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        // Cache shader property IDs (faster than looking up strings every frame)
        innerRadiusID = Shader.PropertyToID(innerRadiusPropertyName);
        outerRadiusID = Shader.PropertyToID(outerRadiusPropertyName);
        propBlock = new MaterialPropertyBlock();

        // Safely find the star object and script
        GameObject sunObj = GameObject.FindGameObjectWithTag("sunStuff");
        if (sunObj != null)
        {
            starScript = sunObj.GetComponent<star>();
        }
        else
        {
            Debug.LogWarning("GameObject with tag 'sunStuff' not found!");
        }

        // Automatically try to get the Renderer if not assigned in the Inspector
        if (shellRenderer == null)
        {
            shellRenderer = GetComponent<Renderer>();
        }
    }

    // void Update()
    // {
    //     if (annuScript != null && shellRenderer != null)
    //     {
    //         float innerDist = annuScript.innerDist;
    //         float outerDist = annuScript.outerDist;

    //         // Get the current property block from the renderer
    //         shellRenderer.GetPropertyBlock(propBlock);

    //         // Set the float values using the cached IDs
    //         propBlock.SetFloat(innerRadiusID, innerDist);
    //         propBlock.SetFloat(outerRadiusID, outerDist);

    //         // Apply the updated block back to the renderer
    //         shellRenderer.SetPropertyBlock(propBlock);
    //     }
    // }
void Update()
{
    if (annuScript != null && shellRenderer != null)
    {
        float innerDist = annuScript.innerDist;
        float outerDist = annuScript.outerDist;

        // Directly manipulate the local material instance
        shellRenderer.material.SetFloat(innerRadiusPropertyName, innerDist);
        shellRenderer.material.SetFloat(outerRadiusPropertyName, outerDist);
    }
}
}