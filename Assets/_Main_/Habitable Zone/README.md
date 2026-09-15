# Habitable zone simulation

The home page and build settings load `Scenes/Habitable Zone 1.unity`, driven by
`Scripts/HabitableZoneSimulation.cs`. The older `Habitable Zone.unity` scene uses
legacy scripts and is not the scene loaded by the application.

## Views and controls

- 2D shows the annulus and its circular boundaries. 3D shows the two spherical
  boundaries of the shell. The line and sphere renderers share objects, so their
  renderer visibility is switched separately.
- Slider changes preserve camera zoom, position, and pan. Switching modes retains
  the viewing scale at the center. The initial view uses a fixed 1.7 AU reference.
- Scroll to zoom in either mode; Shift-drag or middle-drag to pan; drag to orbit in
  3D. Press R with the pointer over the simulation to restore the reference view.
  Large zones can extend off-screen until the user zooms out.

## Scientific interpretation

Radius is in solar radii; effective temperature is in kelvin. Luminosity follows
`L/L_sun = (R/R_sun)^2 (T/5780 K)^4`. Radius and temperature are independent
educational controls, not a stellar evolution model. The star's displayed size
is exaggerated relative to the AU distance scale.

The limits use the one-Earth-mass runaway-greenhouse and maximum-greenhouse
coefficients from [Kopparapu et al. (2014)](https://arxiv.org/abs/1404.5292),
valid for 2600–7200 K. See the author's
[calculator and coefficient table](https://personal.ems.psu.edu/~jfk4/ruk15/planets/).
At the solar default the bounds are approximately 0.950–1.676 AU. These are
model-dependent conditions for potential surface liquid water with a suitable
atmosphere, not a guarantee of habitability or a simulation of a planet's climate.

## Verification

Run **Tools > Habitable Zone > Run Regression Checks** in the Unity Editor.
The checks create temporary objects and cover renderer exclusivity, repeated mode
changes, fixed camera framing at small and large manual zoom distances, slider
limits, the solar reference, and luminosity/distance scaling with stellar radius.
They can also run in batch mode using `-executeMethod HabitableZoneChecks.Run`.

For visual review, play `Habitable Zone 1.unity`, switch modes repeatedly, and
change both sliders through their full ranges. Verify that the zone grows/shrinks
on screen while the zoom remains fixed, then try scroll, pan, and R in both modes.
