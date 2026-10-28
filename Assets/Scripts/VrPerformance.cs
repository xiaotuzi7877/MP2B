using UnityEngine;

namespace ArcaneWorkshop
{
    /// <summary>
    /// Comfort through frame rate: a dropped frame in a headset is felt as judder, which causes as much sickness as
    /// artificial motion. With a headset running, cap the Built-in renderer's most expensive per-eye costs: extra
    /// per-pixel point lights (the islands use about twenty), long soft-shadow distances and a second shadowed sun.
    /// The desktop view keeps the project's full quality.
    /// </summary>
    public static class VrPerformance
    {
        public const int MaxPixelLights = 2;
        public const float MaxShadowDistance = 35f;

        public static bool Applied { get; private set; }

        public static void Apply()
        {
            if (Applied || !DesktopInspectView.HeadsetRunning())
            {
                return;
            }
            Applied = true;
            QualitySettings.pixelLightCount = Mathf.Min(QualitySettings.pixelLightCount, MaxPixelLights);
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, MaxShadowDistance);
            QualitySettings.shadowCascades = 1;

            // One shadowed directional light is enough; keep the brightest, the rest light without shadows.
            Light key = null;
            Light[] lights = Object.FindObjectsByType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional && light.shadows != LightShadows.None && (key == null || light.intensity > key.intensity))
                {
                    key = light;
                }
            }
            foreach (Light light in lights)
            {
                if (light != key && light.type == LightType.Directional)
                {
                    light.shadows = LightShadows.None;
                }
            }
            if (key != null)
            {
                key.shadows = LightShadows.Hard;
            }
        }
    }
}
