using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Also callable in batch mode with -executeMethod HabitableZoneChecks.Run.
public static class HabitableZoneChecks
{
    [MenuItem("Tools/Habitable Zone/Run Regression Checks")]
    public static void Run()
    {
        GameObject root = new GameObject("Habitable Zone Regression Checks");
        try
        {
            Camera camera = Child(root, "Camera").AddComponent<Camera>();
            camera.aspect = 16f / 9f;
            var simulation = root.AddComponent<HabitableZoneSimulation>();
            Transform star = Child(root, "Star").transform;
            GameObject ring = Child(root, "Ring");
            var ringMesh = ring.AddComponent<MeshFilter>();
            var ringRenderer = ring.AddComponent<MeshRenderer>();
            GameObject inner = Child(root, "Inner");
            GameObject outer = Child(root, "Outer");
            var innerSphere = inner.AddComponent<MeshRenderer>();
            var outerSphere = outer.AddComponent<MeshRenderer>();
            // Match the production scene: sphere and line on the SAME object.
            var innerLine = inner.AddComponent<LineRenderer>();
            var outerLine = outer.AddComponent<LineRenderer>();
            var radiusSlider = Child(root, "Radius").AddComponent<Slider>();
            var temperatureSlider = Child(root, "Temperature").AddComponent<Slider>();
            Set(simulation, "simulationCenter", star);
            Set(simulation, "starVisualRoot", star);
            Set(simulation, "simulationCamera", camera);
            Set(simulation, "annulusMeshFilter", ringMesh);
            Set(simulation, "innerBoundarySphere", inner.transform);
            Set(simulation, "outerBoundarySphere", outer.transform);
            Set(simulation, "innerBoundaryLine", innerLine);
            Set(simulation, "outerBoundaryLine", outerLine);
            Set(simulation, "radiusSlider", radiusSlider);
            Set(simulation, "temperatureSlider", temperatureSlider);
            Set(simulation, "visualSmoothTime", 0f);
            // Leave optional text references null to exercise that supported setup.
            Call(simulation, "Start");

            Near(simulation.LuminositySolar, 1f, "Solar luminosity");
            Near(simulation.InnerHabitableZoneAU, 0.9504408f, "Solar inner edge");
            Near(simulation.OuterHabitableZoneAU, 1.6760038f, "Solar outer edge");

            foreach (bool use3D in new[] { false, true, false, true })
            {
                simulation.Set3DMode(use3D);
                Tick(simulation);
                Check(camera.orthographic == !use3D, "Projection follows view mode");
                Check(ringRenderer.enabled == !use3D, "Ring visibility");
                Check(innerLine.enabled == !use3D && outerLine.enabled == !use3D, "2D boundaries visibility");
                Check(innerSphere.enabled == use3D && outerSphere.enabled == use3D, "Shell visibility");
                Check(inner.activeSelf && outer.activeSelf, "Shared line/sphere objects remain active");

                // Include zoom distances where the old radius-dependent clamps moved the camera.
                foreach (float zoom in new[] { 2f, 40f, 500f })
                {
                    Set(simulation, "cameraDistance", zoom);
                    Set(simulation, "cameraPanOffset", new Vector3(2f, 0f, 3f));
                    Tick(simulation);
                    Vector3 position = camera.transform.position;
                    Quaternion rotation = camera.transform.rotation;
                    float size = camera.orthographicSize;
                    foreach (float radius in new[] { 0.1f, 1f, 3f })
                    foreach (float temperature in new[] { 2600f, 5780f, 7200f })
                    {
                        radiusSlider.value = radius;
                        temperatureSlider.value = temperature;
                        Tick(simulation);
                        Check(Vector3.Distance(camera.transform.position, position) < 0.0001f, "Sliders preserve position and pan");
                        Check(Quaternion.Angle(camera.transform.rotation, rotation) < 0.001f, "Sliders preserve rotation");
                        Near(camera.orthographicSize, size, "Sliders preserve 2D zoom");
                        Check(simulation.InnerHabitableZoneAU > 0f && simulation.OuterHabitableZoneAU > simulation.InnerHabitableZoneAU,
                            "Valid ordered boundaries across slider extremes");
                    }
                }
            }

            simulation.SetStarTemperatureK(5780f);
            simulation.SetStarRadiusSolar(2f);
            Near(simulation.LuminositySolar, 4f, "Doubling radius quadruples luminosity");
            Near(simulation.InnerHabitableZoneAU, 2f * 0.9504408f, "Doubling radius doubles HZ distance");
            simulation.SetStarTemperatureK(9000f);
            Near(temperatureSlider.value, 7200f, "Temperature clamps to model range");
            simulation.SetStarRadiusSolar(-1f);
            Near(radiusSlider.value, 0.1f, "Radius clamps to slider range");

            simulation.ResetCameraView();
            Tick(simulation);
            Vector3 resetPosition = camera.transform.position;
            simulation.SetStarRadiusSolar(3f);
            simulation.ResetCameraView();
            Tick(simulation);
            Check(Vector3.Distance(resetPosition, camera.transform.position) < 0.0001f, "Reset uses a fixed reference view");
            Debug.Log("Habitable zone regression checks PASSED: mode exclusivity, fixed camera, slider limits, and model reference values.");
        }
        finally
        {
            // Start is invoked explicitly in Edit Mode, so dispose the generated runtime mesh here.
            var simulation = root.GetComponent<HabitableZoneSimulation>();
            var ring = root.GetComponentInChildren<MeshFilter>();
            if (ring != null) UnityEngine.Object.DestroyImmediate(ring.sharedMesh);
            if (simulation != null) Set(simulation, "annulusMesh", null);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject Child(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }

    private static void Tick(HabitableZoneSimulation simulation)
    {
        Call(simulation, "AnimateVisualValues");
        Call(simulation, "UpdateVisuals");
        Call(simulation, "UpdateCamera");
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void Call(object target, string name)
    {
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    private static void Near(float actual, float expected, string message)
    {
        Check(Mathf.Abs(actual - expected) < 0.0001f, $"{message}: expected {expected}, got {actual}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
