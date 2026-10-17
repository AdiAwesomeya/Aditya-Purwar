using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Single-controller Kepler's Laws simulation.
///
/// What it simulates:
/// 1) Kepler's First Law  - an elliptical orbit with the star at one focus.
/// 2) Kepler's Second Law - the planet moves faster near perihelion and slower near aphelion.
/// 3) Kepler's Third Law  - P^2 = a^3 / M when P is in years, a in AU and M in solar masses.
///
/// The orbital motion is calculated from mean anomaly -> eccentric anomaly (Kepler's equation),
/// not by moving point-to-point along a LineRenderer. This avoids NaN/Infinity errors and gives
/// physically correct timing around the ellipse.
/// </summary>
[DisallowMultipleComponent]
public class KeplersLawSimulation : MonoBehaviour
{
    private const float GravitationalConstant = 6.67430E-11f;
    private const float SolarMassKg = 1.98847E30f;
    private const float AstronomicalUnitMeters = 1.495978707E11f;
    private const float SecondsPerYearSI = 31557600f;
    private const float MinimumValue = 0.000001f;

    [Header("Scene References")]
    [Tooltip("The star/Sun. It remains at one focus of the ellipse.")]
    [SerializeField] private Transform star;

    [Header("Star (focus1) Behaviour")]
    [Tooltip("World-space X coordinate for the star, matching the old focus1script xValue.")]
    [SerializeField] private float starXValue = 0f;

    [Tooltip("Base uniform scale of the star at one solar mass, matching the old focus1script size.")]
    [SerializeField, Min(0.001f)] private float starBaseSize = 2f;

    [Tooltip("If enabled, the star continuously faces the planet, matching the old focus1script LookAt behaviour.")]
    [SerializeField] private bool starLooksAtPlanet = true;

    [Tooltip("The orbiting planet/Earth model.")]
    [SerializeField] private Transform planet;

    [Tooltip("Optional object used to visualize the empty second focus.")]
    [SerializeField] private Transform secondFocusMarker;

    [Tooltip("LineRenderer used to draw the orbital ellipse.")]
    [SerializeField] private LineRenderer orbitLine;

    [Header("Orbit Units")]
    [Tooltip("How many Unity world units represent 1 AU. Scientific text still uses AU.")]
    [SerializeField, Min(0.01f)] private float unityUnitsPerAU = 5f;

    [Tooltip("Number of points used to draw the ellipse.")]
    [SerializeField, Range(32, 512)] private int orbitSegments = 180;

    [Header("Initial Orbit")]
    [Tooltip("Semi-major axis in astronomical units.")]
    [SerializeField, Min(0.01f)] private float semiMajorAxisAU = 1f;

    [Tooltip("Orbital eccentricity. 0 = circle. Values must stay below 1 for an ellipse.")]
    [SerializeField, Range(0f, 0.95f)] private float eccentricity = 0.0167f;

    [Tooltip("Central-star mass in solar masses.")]
    [SerializeField, Min(0.01f)] private float starMassSolar = 1f;

    [Tooltip("Time-scale multiplier, matching Code 2. 1 = normal speed; 1000000 = one million times faster.")]
    [SerializeField, Min(0f)] private float simulationSpeed = 1f;

    [Tooltip("Real seconds used to represent one simulated year when Simulation Speed = 1.")]
    [SerializeField, Min(0.1f)] private float secondsPerYear = 20f;

    [Header("Planet Rotation")]
    [Tooltip("Planet's sidereal rotation period in hours. Earth's rotation relative to distant stars is about 23.9345 hours.")]
    [SerializeField, Min(0.001f)] private float planetRotationPeriodHours = 23.9345f;

    [Tooltip("Local axis around which the planet rotates. The direction is used; the vector magnitude is ignored.")]
    [SerializeField] private Vector3 planetRotationAxis = Vector3.up;

    [Header("Sliders")]
    [SerializeField] private Slider semiMajorAxisSlider;
    [SerializeField] private Slider eccentricitySlider;
    [SerializeField] private Slider starMassSlider;
    [SerializeField] private Slider simulationSpeedSlider;

    [Header("Slider Ranges")]
    [SerializeField] private bool configureSliderRangesAutomatically = true;
    [SerializeField] private Vector2 semiMajorAxisRangeAU = new Vector2(0.5f, 3f);
    [SerializeField] private Vector2 eccentricityRange = new Vector2(0f, 0.9f);
    [SerializeField] private Vector2 starMassRangeSolar = new Vector2(0.01f, 18f);
    [SerializeField] private Vector2 simulationSpeedRange = new Vector2(0f, 1000000f);

    [Header("UI Text - Slider Values")]
    [SerializeField] private TMP_Text semiMajorAxisText;
    [SerializeField] private TMP_Text eccentricityText;
    [SerializeField] private TMP_Text starMassText;
    [SerializeField] private TMP_Text simulationSpeedText;

    [Header("UI Text - Calculated Values")]
    [SerializeField] private TMP_Text semiMinorAxisText;
    [SerializeField] private TMP_Text focalDistanceText;
    [SerializeField] private TMP_Text perihelionText;
    [SerializeField] private TMP_Text aphelionText;
    [SerializeField] private TMP_Text orbitalPeriodText;
    [SerializeField] private TMP_Text currentDistanceText;
    [SerializeField] private TMP_Text currentSpeedText;
    [SerializeField] private TMP_Text keplerThirdLawText;
    [SerializeField] private TMP_Text lawExplanationText;

    [Header("Optional Camera Controller")]
    [Tooltip("Leave empty if another script controls the camera.")]
    [SerializeField] private Camera simulationCamera;
    [SerializeField] private bool controlCamera = false;
    [SerializeField] private float cameraDistance = 18f;
    [SerializeField] private float minCameraDistance = 5f;
    [SerializeField] private float maxCameraDistance = 80f;
    [SerializeField] private float orbitSensitivity = 0.18f;
    [SerializeField] private float zoomSensitivity = 2f;
    [SerializeField] private float minCameraPitch = -80f;
    [SerializeField] private float maxCameraPitch = 80f;

    // Public read-only values for other scripts/UI if ever needed.
    public float SemiMajorAxisAU => semiMajorAxisAU;
    public float Eccentricity => eccentricity;
    public float StarMassSolar => starMassSolar;
    public float SimulationSpeed => simulationSpeed;
    public float SemiMinorAxisAU { get; private set; }
    public float FocalDistanceAU { get; private set; }
    public float PerihelionAU { get; private set; }
    public float AphelionAU { get; private set; }
    public float OrbitalPeriodYears { get; private set; }
    public float CurrentDistanceAU { get; private set; }
    public float CurrentSpeedKmPerSec { get; private set; }

    private int targetIndex = 0;
    private float speedConstant;
    private float cameraYaw;
    private float cameraPitch = 25f;
    private bool cameraDragging;

    private void Awake()
    {
        ValidateValues();
        ConfigureLineRenderer();
        ConfigureSliders();
    }

    private void Start()
    {
        RegisterUIEvents();
        PushValuesToSliders();

        if (simulationCamera != null)
        {
            Vector3 euler = simulationCamera.transform.eulerAngles;
            cameraYaw = euler.y;
            cameraPitch = NormalizeAngle(euler.x);
        }

        ApplyStarTransform();
        RebuildSimulation(resetPlanetPosition: true);
    }

    private void Update()
    {
        UpdateOrbitalMotion();
        RotatePlanet();
        ApplyStarTransform();

        if (controlCamera)
            HandleCameraInput();
    }

    private void LateUpdate()
    {
        if (controlCamera)
            UpdateCameraTransform();
    }

    private void OnDestroy()
    {
        UnregisterUIEvents();
    }

    // ---------------------------------------------------------------------
    // UI
    // ---------------------------------------------------------------------

    private void ConfigureSliders()
    {
        // Keep the optional auto-configuration setting for the orbit-property sliders.
        // Unity serializes old Inspector values, so changing simulationSpeedRange's code
        // default alone will not update an existing scene's Slider. Set this range directly.
        if (configureSliderRangesAutomatically)
        {
            ConfigureSlider(semiMajorAxisSlider, semiMajorAxisRangeAU.x, semiMajorAxisRangeAU.y, false);
            ConfigureSlider(eccentricitySlider, eccentricityRange.x, Mathf.Min(0.99f, eccentricityRange.y), false);
            ConfigureSlider(starMassSlider, starMassRangeSolar.x, starMassRangeSolar.y, false);
        }

        // These parameter ranges are enforced even if the scene has older serialized values.
        // Mass is measured in solar masses; the range is useful for exploring a wide range
        // of central-star masses. The time multiplier supports up to 1,000,000x.
        ConfigureSlider(starMassSlider, 0.01f, 18f, false);
        ConfigureSlider(simulationSpeedSlider, 0f, 1000000f, false);
    }

    private static void ConfigureSlider(Slider slider, float min, float max, bool wholeNumbers)
    {
        if (slider == null) return;
        slider.minValue = min;
        slider.maxValue = Mathf.Max(min + 0.0001f, max);
        slider.wholeNumbers = wholeNumbers;
    }

    private void RegisterUIEvents()
    {
        if (semiMajorAxisSlider != null)
            semiMajorAxisSlider.onValueChanged.AddListener(SetSemiMajorAxis);

        if (eccentricitySlider != null)
            eccentricitySlider.onValueChanged.AddListener(SetEccentricity);

        if (starMassSlider != null)
            starMassSlider.onValueChanged.AddListener(SetStarMass);

        if (simulationSpeedSlider != null)
            simulationSpeedSlider.onValueChanged.AddListener(SetSimulationSpeed);
    }

    private void UnregisterUIEvents()
    {
        if (semiMajorAxisSlider != null)
            semiMajorAxisSlider.onValueChanged.RemoveListener(SetSemiMajorAxis);

        if (eccentricitySlider != null)
            eccentricitySlider.onValueChanged.RemoveListener(SetEccentricity);

        if (starMassSlider != null)
            starMassSlider.onValueChanged.RemoveListener(SetStarMass);

        if (simulationSpeedSlider != null)
            simulationSpeedSlider.onValueChanged.RemoveListener(SetSimulationSpeed);
    }

    private void PushValuesToSliders()
    {
        if (semiMajorAxisSlider != null)
            semiMajorAxisSlider.SetValueWithoutNotify(semiMajorAxisAU);

        if (eccentricitySlider != null)
            eccentricitySlider.SetValueWithoutNotify(eccentricity);

        if (starMassSlider != null)
            starMassSlider.SetValueWithoutNotify(starMassSolar);

        if (simulationSpeedSlider != null)
            simulationSpeedSlider.SetValueWithoutNotify(simulationSpeed);
    }

    public void SetSemiMajorAxis(float value)
    {
        semiMajorAxisAU = Mathf.Max(0.01f, value);
        RebuildSimulation(resetPlanetPosition: false);
    }

    public void SetEccentricity(float value)
    {
        eccentricity = Mathf.Clamp(value, 0f, 0.99f);
        RebuildSimulation(resetPlanetPosition: false);
    }

    public void SetStarMass(float value)
    {
        starMassSolar = Mathf.Clamp(value, 0.01f, 18f);
        RecalculateOrbitProperties();
        ApplyStarTransform();
        UpdateAllText();
    }

    public void SetSimulationSpeed(float value)
    {
        simulationSpeed = Mathf.Clamp(value, 1f, 1000000f);
        UpdateAllText();
    }

    public void ResetOrbit()
    {
        targetIndex = 0;
        RebuildSimulation(resetPlanetPosition: true);
    }

    // ---------------------------------------------------------------------
    // Orbit geometry + Kepler laws
    // ---------------------------------------------------------------------

    private void RebuildSimulation(bool resetPlanetPosition)
    {
        ValidateValues();
        RecalculateOrbitProperties();
        DrawOrbit();
        UpdateSecondFocusMarker();

        if (resetPlanetPosition)
            InitialisePlanetOnOrbit();

        UpdateAllText();
    }

    private void RecalculateOrbitProperties()
    {
        // First law geometry.
        SemiMinorAxisAU = semiMajorAxisAU * Mathf.Sqrt(1f - eccentricity * eccentricity);
        FocalDistanceAU = semiMajorAxisAU * eccentricity;
        PerihelionAU = semiMajorAxisAU * (1f - eccentricity);
        AphelionAU = semiMajorAxisAU * (1f + eccentricity);

        // Kepler's third law using the same gravitational equation as Code 2.
        float massKg = starMassSolar * SolarMassKg;
        float aMeters = semiMajorAxisAU * AstronomicalUnitMeters;
        float periodInsideSqrt = ((4f * Mathf.PI * Mathf.PI) /
                                  Mathf.Max(MinimumValue, massKg * GravitationalConstant))
                                 * aMeters * aMeters * aMeters;
        OrbitalPeriodYears = IsValidFloat(periodInsideSqrt) && periodInsideSqrt > 0f
            ? Mathf.Sqrt(periodInsideSqrt) / SecondsPerYearSI
            : 0f;
    }

    private void UpdateOrbitalMotion()
    {
        if (planet == null || star == null || simulationSpeed <= 0f)
            return;

        if (orbitLine == null || star == null || planet == null || orbitLine.positionCount <= 0)
            return;

        if (targetIndex >= orbitLine.positionCount)
            targetIndex = 0;

        Vector3 targetPos = orbitLine.GetPosition(targetIndex);
        if (!IsValidVector(targetPos))
        {
            Debug.LogError($"Invalid ellipse point at index {targetIndex}: {targetPos}");
            GoToNextPoint();
            return;
        }

        float side1 = Vector3.Distance(planet.position, star.position);
        float side2 = Vector3.Distance(targetPos, star.position);
        float base1 = Vector3.Distance(planet.position, targetPos);

        if (base1 <= 0.001f)
        {
            GoToNextPoint();
            return;
        }

        // Calculate the period using the same gravitational equation as Code 2.
        float massKg = starMassSolar * SolarMassKg;
        float aMeters = semiMajorAxisAU * AstronomicalUnitMeters;
        float periodInsideSqrt = ((4f * Mathf.PI * Mathf.PI) / (massKg * GravitationalConstant))
                                 * aMeters * aMeters * aMeters;

        if (periodInsideSqrt <= 0f || !IsValidFloat(periodInsideSqrt))
            return;

        float periodSeconds = Mathf.Sqrt(periodInsideSqrt);
        if (!IsValidFloat(periodSeconds) || periodSeconds <= MinimumValue)
            return;

        OrbitalPeriodYears = periodSeconds / SecondsPerYearSI;
        // Use Code 2's physical orbital period directly. The multiplier below speeds up
        // simulated time; it must not alter the period or the Kepler-law calculations.
        speedConstant = (Mathf.PI * semiMajorAxisAU * unityUnitsPerAU
                         * SemiMinorAxisAU * unityUnitsPerAU) / periodSeconds;

        if (!IsValidFloat(speedConstant) || Mathf.Abs(speedConstant) <= MinimumValue)
            return;

        // Heron's formula calculates the triangle swept out by the star, planet,
        // and the next point on the orbit, matching Code 2's movement logic.
        // The multiplier is applied only after calculating the physical segment speed,
        // so it changes playback time-scale without changing the orbit or Kepler period.
        float s = (side1 + side2 + base1) / 2f;
        float areaSquared = s * (s - side1) * (s - side2) * (s - base1);
        float area = Mathf.Sqrt(Mathf.Max(0f, areaSquared));

        if (!IsValidFloat(area))
            return;

        float timeTaken = area / speedConstant;
        if (timeTaken <= MinimumValue)
        {
            GoToNextPoint();
            return;
        }

        float movementSpeed = (base1 / timeTaken) * simulationSpeed; // Code 2: multiplier scales time, not orbital physics.
        if (!IsValidFloat(movementSpeed))
            return;

        movementSpeed = Mathf.Max(0f, movementSpeed);
        float unscaledMovementSpeed = movementSpeed / Mathf.Max(0.0001f, simulationSpeed);
        CurrentSpeedKmPerSec = unscaledMovementSpeed / Mathf.Max(0.0001f, unityUnitsPerAU)
                               * (AstronomicalUnitMeters / 1000f);
        CurrentDistanceAU = side1 / Mathf.Max(0.0001f, unityUnitsPerAU);

        Vector3 newPosition = Vector3.MoveTowards(
            planet.position,
            targetPos,
            movementSpeed * Time.deltaTime);

        if (IsValidVector(newPosition))
            planet.position = newPosition;

        if (Vector3.Distance(planet.position, targetPos) < 0.1f)
            GoToNextPoint();

        UpdateDynamicText();
    }

    private void InitialisePlanetOnOrbit()
    {
        if (planet == null || orbitLine == null || orbitLine.positionCount <= 0)
            return;

        targetIndex = 0;
        Vector3 firstPoint = orbitLine.GetPosition(0);
        if (IsValidVector(firstPoint))
            planet.position = firstPoint;

        // Start by moving toward the next point, as in Code 2.
        GoToNextPoint();
    }

    private void GoToNextPoint()
    {
        if (orbitLine == null || orbitLine.positionCount <= 0)
        {
            targetIndex = 0;
            return;
        }

        targetIndex++;
        if (targetIndex >= orbitLine.positionCount)
            targetIndex = 0;
    }

    private static bool IsValidFloat(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsValidVector(Vector3 value)
    {
        return IsValidFloat(value.x) && IsValidFloat(value.y) && IsValidFloat(value.z);
    }

    private void DrawOrbit()
    {
        if (orbitLine == null || star == null)
            return;

        orbitSegments = Mathf.Clamp(orbitSegments, 32, 512);
        orbitLine.useWorldSpace = true;
        orbitLine.loop = true;
        orbitLine.positionCount = orbitSegments;

        // Ellipse center lies c units away from the star, because the star is at one focus.
        Vector3 ellipseCenter = star.position - Vector3.right * (FocalDistanceAU * unityUnitsPerAU);
        float aUnits = semiMajorAxisAU * unityUnitsPerAU;
        float bUnits = SemiMinorAxisAU * unityUnitsPerAU;

        for (int i = 0; i < orbitSegments; i++)
        {
            float angle = (i / (float)orbitSegments) * Mathf.PI * 2f;
            Vector3 point = ellipseCenter + new Vector3(
                Mathf.Cos(angle) * aUnits,
                0f,
                Mathf.Sin(angle) * bUnits);

            orbitLine.SetPosition(i, point);
        }
    }

    private void ApplyStarTransform()
    {
        if (star == null)
            return;

        // Integrated behaviour from the old focus1script:
        // 1) set a uniform scale, 2) lock the star's world X coordinate, and
        // 3) optionally point the star toward the planet.
        // Mass scales the visual size relative to starBaseSize at one solar mass.
        // The cube-root relationship is only a visual convention for this simulation.
        float massScale = Mathf.Pow(Mathf.Max(0.000001f, starMassSolar), 1f / 3f);
        float visualSize = Mathf.Max(0.001f, starBaseSize) * massScale;
        star.localScale = new Vector3(visualSize, visualSize, visualSize);

        Vector3 position = star.position;
        position.x = starXValue;
        star.position = position;

        if (starLooksAtPlanet && planet != null)
            star.LookAt(planet);
    }

    private void UpdateSecondFocusMarker()
    {
        if (secondFocusMarker == null || star == null)
            return;

        // If the star is the right focus, the second focus is 2c to its left.
        secondFocusMarker.position = star.position -
                                     Vector3.right * (2f * FocalDistanceAU * unityUnitsPerAU);
    }

    private void RotatePlanet()
    {
        if (planet == null || simulationSpeed <= 0f)
            return;

        // Orbital movement is calculated using physical seconds (the period from
        // Kepler's third law), so rotation must use the same simulated time scale.
        // Earth completes one sidereal rotation in about 23.9345 hours, rather
        // than rotating at an arbitrary 100 degrees per real-time second.
        float rotationPeriodSeconds = Mathf.Max(0.001f, planetRotationPeriodHours * 3600f);
        float degreesThisFrame = (360f / rotationPeriodSeconds)
                                 * Time.deltaTime * simulationSpeed;

        Vector3 axis = planetRotationAxis.sqrMagnitude > 0.000001f
            ? planetRotationAxis.normalized
            : Vector3.up;

        planet.Rotate(axis * degreesThisFrame, Space.Self);
    }

    // ---------------------------------------------------------------------
    // Text
    // ---------------------------------------------------------------------

    private void UpdateAllText()
    {
        if (semiMajorAxisText != null)
            semiMajorAxisText.text = $"Semi-major axis (a): {semiMajorAxisAU:0.00} AU";

        if (eccentricityText != null)
            eccentricityText.text = $"Eccentricity (e): {eccentricity:0.000}";

        if (starMassText != null)
            starMassText.text = $"Star mass: {starMassSolar:0.00} Solar Masses";

        if (simulationSpeedText != null)
            simulationSpeedText.text = $"Time scale: {simulationSpeed:0.##}x";

        if (semiMinorAxisText != null)
            semiMinorAxisText.text = $"Semi-minor axis (b): {SemiMinorAxisAU:0.00} AU";

        if (focalDistanceText != null)
            focalDistanceText.text = $"Focus distance (c): {FocalDistanceAU:0.00} AU";

        if (perihelionText != null)
            perihelionText.text = $"Perihelion: {PerihelionAU:0.00} AU";

        if (aphelionText != null)
            aphelionText.text = $"Aphelion: {AphelionAU:0.00} AU";

        if (orbitalPeriodText != null)
            orbitalPeriodText.text = $"Orbital period: {FormatPeriod(OrbitalPeriodYears)}";

        if (keplerThirdLawText != null)
        {
            float ratio = (OrbitalPeriodYears * OrbitalPeriodYears) /
                          Mathf.Max(0.0001f, semiMajorAxisAU * semiMajorAxisAU * semiMajorAxisAU);

            keplerThirdLawText.text =
                "Scroll: zoom.\n3D drag: orbit.";
        }

        if (lawExplanationText != null)
        {
            lawExplanationText.text =
                "Kepler's 1st Law: The orbit of a planet follows an ellipse with the sun as one of its foci.\n" +
                "Kepler's 2nd Law: A line joining a star to a planet sweeps out equal areas in equal intervals of time.\n(the closer the planet is to the star, the faster it will move)\n" +
                "Kepler's 3rd Law: T² is directly proportional to a³, where T is the time it takes for a planet to fully complete one orbit, and a is the largest radius of the ellipse.";
        }

        UpdateDynamicText();
    }

    private void UpdateDynamicText()
    {
        if (currentDistanceText != null)
            currentDistanceText.text = $"Current star distance: {CurrentDistanceAU:0.000} AU";

        if (currentSpeedText != null)
            currentSpeedText.text = $"Orbital speed: {CurrentSpeedKmPerSec:0.00} km/s";
    }

    private static string FormatPeriod(float years)
    {
        if (years >= 1f)
            return $"{years:0.00} years";

        float days = years * 365.25f;
        return days >= 1f ? $"{days:0.0} days" : $"{days * 24f:0.0} hours";
    }

    // ---------------------------------------------------------------------
    // Optional camera controls
    // ---------------------------------------------------------------------

    private void HandleCameraInput()
    {
        if (simulationCamera == null)
            return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0))
            cameraDragging = !overUI;

        if (Input.GetMouseButtonUp(0))
            cameraDragging = false;

        if (cameraDragging && Input.GetMouseButton(0) && !overUI)
        {
            cameraYaw += Input.GetAxis("Mouse X") * 180f * orbitSensitivity * Time.deltaTime;
            cameraPitch -= Input.GetAxis("Mouse Y") * 120f * orbitSensitivity * Time.deltaTime;
            cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);
        }

        if (!overUI)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                cameraDistance -= scroll * zoomSensitivity * 10f;
                cameraDistance = Mathf.Clamp(cameraDistance, minCameraDistance, maxCameraDistance);
            }
        }
    }

    private void UpdateCameraTransform()
    {
        if (simulationCamera == null || star == null)
            return;

        Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        simulationCamera.transform.rotation = rotation;
        simulationCamera.transform.position =
            star.position - rotation * Vector3.forward * cameraDistance;
    }

    // ---------------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------------

    private void ConfigureLineRenderer()
    {
        if (orbitLine == null)
            return;

        orbitLine.useWorldSpace = true;
        orbitLine.loop = true;
    }

    private void ValidateValues()
    {
        unityUnitsPerAU = Mathf.Max(0.01f, unityUnitsPerAU);
        orbitSegments = Mathf.Clamp(orbitSegments, 32, 512);
        semiMajorAxisAU = Mathf.Max(0.01f, semiMajorAxisAU);
        eccentricity = Mathf.Clamp(eccentricity, 0f, 0.99f);
        starMassSolar = Mathf.Clamp(starMassSolar, 0.01f, 18);
        simulationSpeed = Mathf.Clamp(simulationSpeed, 1f, 1000000f);
        secondsPerYear = Mathf.Max(0.1f, secondsPerYear);
        minCameraDistance = Mathf.Max(0.1f, minCameraDistance);
        maxCameraDistance = Mathf.Max(minCameraDistance, maxCameraDistance);
        cameraDistance = Mathf.Clamp(cameraDistance, minCameraDistance, maxCameraDistance);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}