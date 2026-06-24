using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class star : MonoBehaviour
{
    public Transform starTransform;
    public float radius = 695700000f;
    public float temperature = 5772f;
    public float scaleConstant;
    public LensFlareComponentSRP sunLight;
    public annulusScript annuScript;
    public Slider radiusSlider;
    public Slider tempSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        radiusSlider.onValueChanged.AddListener(UpdateRadius);
        tempSlider.onValueChanged.AddListener(UpdateTemp);
    }

    // Update is called once per frame

    void UpdateRadius(float value)
    {
        radius = value;
    }
    void UpdateTemp(float value)
    {
        temperature = value;
    }
    void Update()
    {
        radius = radiusSlider.value;
        temperature = tempSlider.value;
        scaleConstant = (radius*2)/1.496E8f;
        scaleConstant = scaleConstant/10f;
        transform.localScale = new Vector3(scaleConstant, scaleConstant, scaleConstant);
        sunLight.intensity = annuScript.luminosity/100f;
    }
}
