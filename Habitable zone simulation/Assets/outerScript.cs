using UnityEngine;

public class outerScript : MonoBehaviour
{
    public annulusScript annuScript;
    public star starScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float outerDist = annuScript.outerDist * 10;
        transform.localScale = Vector3.one * outerDist;
    }
}
