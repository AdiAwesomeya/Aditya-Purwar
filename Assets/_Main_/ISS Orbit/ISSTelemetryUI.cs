using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ISSTelemetryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform issModel;
    [SerializeField] private Transform earthCenter;

    [Header("UI Text - Use Either TMP or Normal Text")]
    [SerializeField] private TMP_Text telemetryTMPText;
    [SerializeField] private Text telemetryUIText;

    [Header("Satellite Info")]
    [SerializeField] private string satelliteName = "ISS (ZARYA)";
    [SerializeField] private string noradId = "25544";
    [SerializeField] private string dataSource = "CelesTrak TLE + SGP4";

    [Header("Scale Settings")]
    [Tooltip("Use the same scale as your orbit script. If 1 Unity unit = 1000 km, keep this 1000.")]
    [SerializeField] private float kilometersPerUnityUnit = 1000f;

    [Tooltip("Earth radius in km.")]
    [SerializeField] private float earthRadiusKm = 6371f;

    [Header("Simulation Settings")]
    [Tooltip("Keep this same as your ISSOrbitManager Simulation Time Scale.")]
    [SerializeField] private float simulationTimeScale = 1f;

    [Header("UI Settings")]
    [SerializeField] private float updateInterval = 0.25f;
    [SerializeField] private bool useEarthRotationForLatLong = true;
    [SerializeField] private bool showApproxText = true;

    private Vector3 previousPosition;
    private float previousTime;
    private float currentSpeedKmh;
    private float timer;

    private void Start()
    {
        if (issModel == null)
        {
            Debug.LogError("ISSTelemetryUI: ISS Model is not assigned.");
            enabled = false;
            return;
        }

        if (earthCenter == null)
        {
            Debug.LogError("ISSTelemetryUI: Earth Center is not assigned.");
            enabled = false;
            return;
        }

        previousPosition = issModel.position;
        previousTime = Time.time;

        UpdateTelemetryUI();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateTelemetryUI();
        }
    }

    private void UpdateTelemetryUI()
    {
        Vector3 relativePosition = issModel.position - earthCenter.position;

        float distanceFromEarthCenterKm = relativePosition.magnitude * kilometersPerUnityUnit;
        float altitudeKm = distanceFromEarthCenterKm - earthRadiusKm;

        UpdateSpeed();

        Vector2 latLong = GetLatitudeLongitude(relativePosition);
        float latitude = latLong.x;
        float longitude = latLong.y;

        string latitudeText = FormatLatitude(latitude);
        string longitudeText = FormatLongitude(longitude);

        string daylightStatus = GetDayNightStatus(issModel.position);

        string utcTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        string approxLabel = showApproxText ? "Approx. " : "";

        string uiText =
            $"MISSION CONTROL — ISS TRACKER\n\n" +
            $"Satellite: {satelliteName}\n" +
            $"NORAD ID: {noradId}\n" +
            $"Source: {dataSource}\n" +
            $"Status: Live Prediction\n\n" +
            $"{approxLabel}Latitude: {latitudeText}\n" +
            $"{approxLabel}Longitude: {longitudeText}\n" +
            $"Altitude: {altitudeKm:0} km\n" +
            $"Velocity: {currentSpeedKmh:0} km/h\n" +
            $"Orbit Type: Low Earth Orbit\n" +
            $"Inclination: 51.6°\n" +
            $"Orbit Period: ~90 min\n\n" +
            $"UTC Time: {utcTime}\n" +
            $"Simulation Speed: {simulationTimeScale:0.#}x\n" +
            $"Day/Night: {daylightStatus}";

        SetText(uiText);
    }

    private void UpdateSpeed()
    {
        float currentTime = Time.time;
        float deltaTime = currentTime - previousTime;

        if (deltaTime <= 0.001f)
            return;

        float distanceUnity = Vector3.Distance(previousPosition, issModel.position);
        float visualSpeedKmPerSecond = (distanceUnity * kilometersPerUnityUnit) / deltaTime;

        float realSpeedKmPerSecond = visualSpeedKmPerSecond / Mathf.Max(1f, simulationTimeScale);

        float calculatedSpeedKmh = realSpeedKmPerSecond * 3600f;

        if (calculatedSpeedKmh > 1000f)
        {
            currentSpeedKmh = Mathf.Lerp(currentSpeedKmh, calculatedSpeedKmh, 0.25f);
        }
        else
        {
            // Fallback to realistic ISS average speed if movement is too slow or paused.
            currentSpeedKmh = Mathf.Lerp(currentSpeedKmh, 27600f, 0.1f);
        }

        previousPosition = issModel.position;
        previousTime = currentTime;
    }

    private Vector2 GetLatitudeLongitude(Vector3 relativePosition)
    {
        Vector3 positionForLatLong = relativePosition;

        if (useEarthRotationForLatLong && earthCenter != null)
        {
            positionForLatLong = Quaternion.Inverse(earthCenter.rotation) * relativePosition;
        }

        positionForLatLong.Normalize();

        float latitude = Mathf.Asin(positionForLatLong.y) * Mathf.Rad2Deg;
        float longitude = Mathf.Atan2(positionForLatLong.z, positionForLatLong.x) * Mathf.Rad2Deg;

        longitude = NormalizeLongitude(longitude);

        return new Vector2(latitude, longitude);
    }

    private string FormatLatitude(float latitude)
    {
        string direction = latitude >= 0f ? "N" : "S";
        return $"{Mathf.Abs(latitude):0.00}° {direction}";
    }

    private string FormatLongitude(float longitude)
    {
        string direction = longitude >= 0f ? "E" : "W";
        return $"{Mathf.Abs(longitude):0.00}° {direction}";
    }

    private float NormalizeLongitude(float longitude)
    {
        while (longitude > 180f) longitude -= 360f;
        while (longitude < -180f) longitude += 360f;
        return longitude;
    }

    private string GetDayNightStatus(Vector3 issWorldPosition)
    {
        Light sun = RenderSettings.sun;

        if (sun == null)
            return "Unknown";

        Vector3 fromEarthToISS = (issWorldPosition - earthCenter.position).normalized;

        // Directional light points in the opposite direction of sunlight.
        Vector3 sunlightDirection = -sun.transform.forward.normalized;

        float dot = Vector3.Dot(fromEarthToISS, sunlightDirection);

        return dot >= 0f ? "Daylight" : "Night";
    }

    private void SetText(string value)
    {
        if (telemetryTMPText != null)
            telemetryTMPText.text = value;

        if (telemetryUIText != null)
            telemetryUIText.text = value;
    }

    public void SetSimulationTimeScale(float newTimeScale)
    {
        simulationTimeScale = Mathf.Max(1f, newTimeScale);
    }
}