using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class MercuryOrbitFromHorizons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sunTransform;
    [SerializeField] private Transform mercuryModel;
    [SerializeField] private LineRenderer orbitLine;

    [Header("Horizons Settings")]
    [SerializeField] private string horizonsBaseUrl = "https://ssd.jpl.nasa.gov/api/horizons.api";
    [SerializeField] private string mercuryCommandId = "199";
    [SerializeField] private string centerId = "500@10";

    [Header("Trajectory Settings")]
    [SerializeField] private float trajectoryDurationDays = 88f;
    [SerializeField] private float trajectoryStepHours = 6f;

    [Tooltip("Base conversion. 1 Unity unit = 10,000,000 km.")]
    [SerializeField] private float kilometersToUnityUnits = 0.0000001f;

    [Header("Auto Visual Scaling")]
    [SerializeField] private bool autoScaleBodies = true;

    [Tooltip("Visual Sun diameter in Unity units. This is NOT real scale, this is for good visual look.")]
    [SerializeField] private float sunVisualDiameter = 2.5f;

    [Tooltip("Visual Mercury diameter in Unity units.")]
    [SerializeField] private float mercuryVisualDiameter = 0.35f;

    [Tooltip("Minimum empty space between Sun surface and Mercury orbit.")]
    [SerializeField] private float orbitGapFromSunSurface = 1.2f;

    [Tooltip("Extra multiplier for orbit size after auto-correction.")]
    [SerializeField] private float extraOrbitSizeMultiplier = 1f;

    [Header("Animation Settings")]
    [SerializeField] private bool animateMercury = true;

    [Tooltip("1 = 1 Earth day per real second.")]
    [SerializeField] private float simulatedDaysPerRealSecond = 1f;

    [SerializeField] private bool rotateMercuryTowardMotion = true;
    [SerializeField] private Vector3 modelRotationOffsetEuler = Vector3.zero;

    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 0.03f;
    [SerializeField] private bool closeOrbitLine = true;

    [Header("Coordinate Fix")]
    [SerializeField] private bool flipZAxis = false;

    [Header("Debug")]
    [SerializeField] private bool logRequestUrl = true;
    [SerializeField] private bool logRawHorizonsResult = false;

    private readonly List<Vector3> rawOrbitPoints = new List<Vector3>();
    private readonly List<Vector3> finalOrbitPoints = new List<Vector3>();

    private float simulatedSeconds;
    private bool orbitReady;

    private float finalOrbitMultiplier = 1f;

    [Serializable]
    private class HorizonsJsonResponse
    {
        public string result;
        public string error;
    }

    private void Start()
    {
        SetupSceneObjects();
        SetupLineRenderer();
        StartCoroutine(FetchMercuryOrbit());
    }

    private void Update()
    {
        if (!orbitReady || !animateMercury || mercuryModel == null || finalOrbitPoints.Count < 2)
            return;

        AnimateMercuryAlongOrbit();
    }

    private void SetupSceneObjects()
    {
        if (sunTransform != null)
        {
            sunTransform.position = Vector3.zero;
        }

        if (autoScaleBodies)
        {
            ScaleObjectToWorldDiameter(sunTransform, sunVisualDiameter);
            ScaleObjectToWorldDiameter(mercuryModel, mercuryVisualDiameter);
        }
    }

    private void SetupLineRenderer()
    {
        if (orbitLine == null)
            return;

        orbitLine.useWorldSpace = true;
        orbitLine.loop = closeOrbitLine;
        orbitLine.widthMultiplier = lineWidth;
        orbitLine.positionCount = 0;
    }

    private IEnumerator FetchMercuryOrbit()
    {
        DateTime startUtc = DateTime.UtcNow;
        DateTime stopUtc = startUtc.AddDays(trajectoryDurationDays);

        string url = BuildHorizonsUrl(startUtc, stopUtc);

        if (logRequestUrl)
        {
            Debug.Log("Mercury Horizons URL:\n" + url);
        }

        using UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch Mercury orbit from JPL Horizons: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        HorizonsJsonResponse response = JsonUtility.FromJson<HorizonsJsonResponse>(json);

        if (response == null)
        {
            Debug.LogError("Could not parse Horizons JSON response.");
            yield break;
        }

        if (!string.IsNullOrEmpty(response.error))
        {
            Debug.LogError("Horizons returned an error:\n" + response.error);
            yield break;
        }

        if (string.IsNullOrEmpty(response.result))
        {
            Debug.LogError("Horizons result was empty.");
            yield break;
        }

        if (logRawHorizonsResult)
        {
            Debug.Log(response.result);
        }

        bool parsed = ParseHorizonsCsvResult(response.result);

        if (!parsed)
        {
            Debug.LogError("Could not parse Mercury orbit points from Horizons result.");
            yield break;
        }

        BuildFinalOrbitPoints();
        DrawOrbitLine();
        PlaceMercuryAtStart();

        orbitReady = true;

        Debug.Log("Mercury trajectory loaded. Points: " + finalOrbitPoints.Count);
        Debug.Log("Final Orbit Multiplier: " + finalOrbitMultiplier);
    }

    private string BuildHorizonsUrl(DateTime startUtc, DateTime stopUtc)
    {
        string start = startUtc.ToString("yyyy-MMM-dd", CultureInfo.InvariantCulture);
        string stop = stopUtc.ToString("yyyy-MMM-dd", CultureInfo.InvariantCulture);

        string step = trajectoryStepHours.ToString("0.###", CultureInfo.InvariantCulture) + " h";

        string url = horizonsBaseUrl;
        url += "?format=json";
        url += "&COMMAND=" + Quote(mercuryCommandId);
        url += "&OBJ_DATA=" + Quote("NO");
        url += "&MAKE_EPHEM=" + Quote("YES");
        url += "&EPHEM_TYPE=" + Quote("VECTORS");
        url += "&CENTER=" + Quote(centerId);
        url += "&START_TIME=" + Quote(start);
        url += "&STOP_TIME=" + Quote(stop);
        url += "&STEP_SIZE=" + Quote(step);
        url += "&TIME_TYPE=" + Quote("UT");
        url += "&OUT_UNITS=" + Quote("KM-S");
        url += "&REF_PLANE=" + Quote("ECLIPTIC");
        url += "&VEC_TABLE=" + Quote("2");
        url += "&CSV_FORMAT=" + Quote("YES");
        url += "&VEC_LABELS=" + Quote("NO");

        return url;
    }

    private string Quote(string value)
    {
        return "%27" + UnityWebRequest.EscapeURL(value) + "%27";
    }

    private bool ParseHorizonsCsvResult(string result)
    {
        rawOrbitPoints.Clear();

        string[] lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        bool insideData = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.Contains("$$SOE"))
            {
                insideData = true;
                continue;
            }

            if (line.Contains("$$EOE"))
            {
                break;
            }

            if (!insideData)
                continue;

            string[] fields = line.Split(',');

            if (fields.Length < 5)
                continue;

            if (!TryParseDouble(fields[2], out double xKm)) continue;
            if (!TryParseDouble(fields[3], out double yKm)) continue;
            if (!TryParseDouble(fields[4], out double zKm)) continue;

            Vector3 unityPosition = ConvertHorizonsKmToUnity(xKm, yKm, zKm);
            rawOrbitPoints.Add(unityPosition);
        }

        return rawOrbitPoints.Count >= 2;
    }

    private bool TryParseDouble(string value, out double result)
    {
        return double.TryParse(
            value.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result
        );
    }

    private Vector3 ConvertHorizonsKmToUnity(double xKm, double yKm, double zKm)
    {
        float x = (float)(xKm * kilometersToUnityUnits);
        float y = (float)(zKm * kilometersToUnityUnits);
        float z = (float)(yKm * kilometersToUnityUnits);

        if (flipZAxis)
            z *= -1f;

        return new Vector3(x, y, z);
    }

    private void BuildFinalOrbitPoints()
    {
        finalOrbitPoints.Clear();

        float minRawOrbitDistance = GetMinimumOrbitDistance(rawOrbitPoints);

        float sunRadius = sunVisualDiameter * 0.5f;
        float mercuryRadius = mercuryVisualDiameter * 0.5f;

        float requiredMinimumDistance = sunRadius + mercuryRadius + orbitGapFromSunSurface;

        finalOrbitMultiplier = 1f;

        if (minRawOrbitDistance < requiredMinimumDistance)
        {
            finalOrbitMultiplier = requiredMinimumDistance / minRawOrbitDistance;
        }

        finalOrbitMultiplier *= extraOrbitSizeMultiplier;

        for (int i = 0; i < rawOrbitPoints.Count; i++)
        {
            finalOrbitPoints.Add(rawOrbitPoints[i] * finalOrbitMultiplier);
        }
    }

    private float GetMinimumOrbitDistance(List<Vector3> points)
    {
        float minDistance = float.MaxValue;

        for (int i = 0; i < points.Count; i++)
        {
            float distance = points[i].magnitude;

            if (distance < minDistance)
                minDistance = distance;
        }

        return minDistance;
    }

    private void DrawOrbitLine()
    {
        if (orbitLine == null)
            return;

        Vector3 sunPosition = sunTransform != null ? sunTransform.position : Vector3.zero;

        Vector3[] worldPositions = new Vector3[finalOrbitPoints.Count];

        for (int i = 0; i < finalOrbitPoints.Count; i++)
        {
            worldPositions[i] = sunPosition + finalOrbitPoints[i];
        }

        orbitLine.positionCount = worldPositions.Length;
        orbitLine.SetPositions(worldPositions);
        orbitLine.loop = closeOrbitLine;
        orbitLine.widthMultiplier = lineWidth;
    }

    private void PlaceMercuryAtStart()
    {
        if (mercuryModel == null || finalOrbitPoints.Count == 0)
            return;

        Vector3 sunPosition = sunTransform != null ? sunTransform.position : Vector3.zero;
        mercuryModel.position = sunPosition + finalOrbitPoints[0];
    }

    private void AnimateMercuryAlongOrbit()
    {
        float secondsPerDay = 86400f;
        float simulatedSecondsPerRealSecond = simulatedDaysPerRealSecond * secondsPerDay;

        simulatedSeconds += Time.deltaTime * simulatedSecondsPerRealSecond;

        float sampleIntervalSeconds = trajectoryStepHours * 3600f;
        float totalOrbitSeconds = sampleIntervalSeconds * (finalOrbitPoints.Count - 1);

        float currentOrbitSeconds = Mathf.Repeat(simulatedSeconds, totalOrbitSeconds);

        float indexFloat = currentOrbitSeconds / sampleIntervalSeconds;

        int indexA = Mathf.FloorToInt(indexFloat);
        int indexB = indexA + 1;

        if (indexB >= finalOrbitPoints.Count)
        {
            indexB = closeOrbitLine ? 0 : finalOrbitPoints.Count - 1;
        }

        float t = indexFloat - indexA;

        Vector3 sunPosition = sunTransform != null ? sunTransform.position : Vector3.zero;

        Vector3 positionA = sunPosition + finalOrbitPoints[indexA];
        Vector3 positionB = sunPosition + finalOrbitPoints[indexB];

        Vector3 currentPosition = Vector3.Lerp(positionA, positionB, t);

        mercuryModel.position = currentPosition;

        if (rotateMercuryTowardMotion)
        {
            Vector3 direction = (positionB - positionA).normalized;

            if (direction.sqrMagnitude > 0.0001f)
            {
                mercuryModel.rotation =
                    Quaternion.LookRotation(direction, Vector3.up) *
                    Quaternion.Euler(modelRotationOffsetEuler);
            }
        }
    }

    private void ScaleObjectToWorldDiameter(Transform target, float desiredDiameter)
    {
        if (target == null)
            return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No Renderer found on " + target.name + ". Could not auto-scale.");
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float currentDiameter = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        if (currentDiameter <= 0.0001f)
            return;

        float scaleMultiplier = desiredDiameter / currentDiameter;

        target.localScale *= scaleMultiplier;
    }

    [ContextMenu("Refresh Mercury Orbit")]
    public void RefreshMercuryOrbit()
    {
        StopAllCoroutines();
        orbitReady = false;
        simulatedSeconds = 0f;
        StartCoroutine(FetchMercuryOrbit());
    }

    [ContextMenu("Reset Mercury Animation")]
    public void ResetMercuryAnimation()
    {
        simulatedSeconds = 0f;
        PlaceMercuryAtStart();
    }
}