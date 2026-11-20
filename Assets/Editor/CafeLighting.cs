using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Three lighting presets from 06-lighting-plan.md.
// Menu: Cafe > Lighting > ...
// Uses only core UnityEngine APIs, so it is version-safe.
public static class CafeLighting
{
    const string MatDir = "Assets/Art/Materials";

    struct Mood
    {
        public string Name;
        public string KeyColor;      public float KeyIntensity;
        public Vector3 KeyDir;
        public string ShadowTint;
        public string AmbSky, AmbEquator, AmbGround;
        public float PendantIntensity;
        public string FogColor;      public float FogDensity;
    }

    static readonly Mood Morning = new Mood
    {
        Name = "Morning",
        KeyColor = "#FFF1D6", KeyIntensity = 1.8f,
        KeyDir = new Vector3(-0.62f, -0.67f, -0.41f),      // ~42 deg elevation
        ShadowTint = "#B7A6C9",
        AmbSky = "#FFF4F0", AmbEquator = "#E4DCEE", AmbGround = "#B7A6C9",
        PendantIntensity = 3f,
        FogColor = "#FDF1E7", FogDensity = 0.012f,
    };

    static readonly Mood Evening = new Mood
    {
        Name = "Pendant evening",
        KeyColor = "#FFD9A0", KeyIntensity = 0.7f,
        KeyDir = new Vector3(-0.79f, -0.31f, -0.52f),      // ~18 deg, raking
        ShadowTint = "#9A8FB3",
        AmbSky = "#E8DCD4", AmbEquator = "#C4B4C4", AmbGround = "#9A8FB3",
        PendantIntensity = 8f,
        FogColor = "#F0DFD4", FogDensity = 0.018f,
    };

    static readonly Mood Night = new Mood
    {
        Name = "Night cafe",
        KeyColor = "#FFC98A", KeyIntensity = 0.25f,
        KeyDir = new Vector3(-0.82f, -0.17f, -0.54f),      // ~10 deg
        ShadowTint = "#5D5478",
        AmbSky = "#6A5F80", AmbEquator = "#4F4668", AmbGround = "#3F3758",
        PendantIntensity = 10f,
        FogColor = "#3F3758", FogDensity = 0.028f,
    };

    [MenuItem("Cafe/Lighting/Morning", false, 20)]
    public static void ApplyMorning() { Apply(Morning); }

    [MenuItem("Cafe/Lighting/Pendant Evening", false, 21)]
    public static void ApplyEvening() { Apply(Evening); }

    [MenuItem("Cafe/Lighting/Night Cafe", false, 22)]
    public static void ApplyNight() { Apply(Night); }

    static Color Hex(string hex)
    {
        Color c;
        if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
        return c;
    }

    static void Apply(Mood m)
    {
        // Key light
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional)
            {
                l.color = Hex(m.KeyColor);
                l.intensity = m.KeyIntensity;
                l.transform.rotation = Quaternion.LookRotation(m.KeyDir.normalized);
            }
            else if (l.type == LightType.Point)
            {
                l.intensity = m.PendantIntensity;
            }
        }

        // Ambient gradient. The ground colour bouncing up is what keeps shadows alive.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex(m.AmbSky);
        RenderSettings.ambientEquatorColor = Hex(m.AmbEquator);
        RenderSettings.ambientGroundColor = Hex(m.AmbGround);

        // Fog carries the mood colour. In night this is what keeps dark reading as
        // deep violet rather than absence of light.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Hex(m.FogColor);
        RenderSettings.fogDensity = m.FogDensity;

        // Shadow tint lives per-material, so the mood has to push it into every one.
        var tint = Hex(m.ShadowTint);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || !mat.HasProperty("_ShadowTint")) continue;
            if (path.EndsWith("M_BulbGlow.mat")) continue;   // emissive, keep self-lit
            if (path.EndsWith("M_CeilingCream.mat")) continue;   // downward-facing, keeps its own light tint
            mat.SetColor("_ShadowTint", tint);
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();

        Debug.Log("Cafe lighting preset applied: " + m.Name);
    }
}
