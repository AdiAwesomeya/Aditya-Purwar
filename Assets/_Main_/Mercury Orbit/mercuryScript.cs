using System;
using TMPro;
using UnityEngine;

public class mercuryScript : MonoBehaviour
{
    private const double TwoPi = Math.PI * 2.0;
    private const float MercuryPeriodSeconds = 7600000f;

    public Vector3 rotationSpeed = new Vector3(0, -0.000071f, 0);
    public float speedConstant;
    public float speed;
    public float multiplier = 1f;
    public TMP_Text simulationSpeedText;
    public Transform sun;
    public MercuryOrbitScript ellipseCode;
    public float timePeriod;
    public float constant1;

    private double meanAnomaly;

    private void Start()
    {
        timePeriod = MercuryPeriodSeconds;
        if (ellipseCode != null && sun != null)
            transform.position = ellipseCode.PositionAtEccentricAnomaly(0.0);
    }

    private void Update()
    {
        AdvanceOrbit(Time.deltaTime);
        transform.Rotate(rotationSpeed * Time.deltaTime * multiplier);
        if (simulationSpeedText != null)
            simulationSpeedText.text = $"Time scale: {multiplier:0.##}x";
    }

    // Advance the orbit by elapsed time, regardless of how many line segments
    // Mercury crosses in a frame. This keeps browser frame rate from limiting speed.
    public void AdvanceOrbit(float elapsedSeconds)
    {
        if (ellipseCode == null || sun == null || elapsedSeconds <= 0f) return;

        double rate = Math.Max(0.0, multiplier);
        meanAnomaly = (meanAnomaly + TwoPi * rate * elapsedSeconds / MercuryPeriodSeconds) % TwoPi;

        double eccentricity = ellipseCode.eccentricity;
        double eccentricAnomaly = meanAnomaly;
        // Kepler's equation: M = E - e sin(E).
        for (int i = 0; i < 8; i++)
        {
            eccentricAnomaly -= (eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly) - meanAnomaly)
                                / (1.0 - eccentricity * Math.Cos(eccentricAnomaly));
        }

        transform.position = ellipseCode.PositionAtEccentricAnomaly(eccentricAnomaly);
        timePeriod = MercuryPeriodSeconds;
        speedConstant = (float)(Math.PI * ellipseCode.a * ellipseCode.b / MercuryPeriodSeconds);
        double anomalyRate = TwoPi * rate /
                             (MercuryPeriodSeconds * (1.0 - eccentricity * Math.Cos(eccentricAnomaly)));
        double tangentX = ellipseCode.b * Math.Cos(eccentricAnomaly);
        double tangentZ = ellipseCode.a * Math.Sin(eccentricAnomaly);
        speed = (float)(anomalyRate * Math.Sqrt(tangentX * tangentX + tangentZ * tangentZ));
        constant1 = (float)((double)MercuryPeriodSeconds * MercuryPeriodSeconds /
                            (ellipseCode.a * ellipseCode.a * ellipseCode.a));
    }
}
