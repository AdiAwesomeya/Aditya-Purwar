using UnityEngine;
using UnityEngine.UI;

public class sliderScript : MonoBehaviour
{
    public Slider multiplierSlider;
    public mercuryScript mercScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Three revolutions per second is fast enough to show the effect without
        // asking the browser to display hundreds of invisible orbits per second.
        multiplierSlider.maxValue = 22800000f;
        multiplierSlider.SetValueWithoutNotify(Mathf.Clamp(mercScript.multiplier,
            multiplierSlider.minValue, multiplierSlider.maxValue));
        mercScript.multiplier = multiplierSlider.value;
        multiplierSlider.onValueChanged.AddListener(UpdateMultiplier);
    }
    void UpdateMultiplier(float value)
    {
        mercScript.multiplier = value;
    }
    private void OnDestroy()
    {
        if (multiplierSlider != null)
            multiplierSlider.onValueChanged.RemoveListener(UpdateMultiplier);
    }
}
