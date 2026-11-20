// Creates the global post-processing volume with the values from 06-lighting-plan.md.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CafePostFX
{
    const string ProfilePath = "Assets/Settings/CafeVolumeProfile.asset";

    // VolumeProfile has TryGet, not Get.
    static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
    {
        T c;
        if (p.TryGet<T>(out c) && c != null) return c;
        return p.Add<T>(true);
    }

    [MenuItem("Cafe/2. Add Post-Processing Volume", false, 10)]
    public static void AddVolume()
    {
        Directory.CreateDirectory("Assets/Settings");

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        // Neutral tonemapping. ACES desaturates the pastels toward grey and the
        // whole art direction goes with it.
        var tone = GetOrAdd<Tonemapping>(profile);
        tone.mode.overrideState = true;
        tone.mode.value = TonemappingMode.Neutral;

        var bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.1f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.45f;
        bloom.scatter.overrideState = true;   bloom.scatter.value = 0.6f;

        var vig = GetOrAdd<Vignette>(profile);
        vig.intensity.overrideState = true;  vig.intensity.value = 0.22f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;

        var ca = GetOrAdd<ColorAdjustments>(profile);
        ca.contrast.overrideState = true;   ca.contrast.value = 4f;
        ca.saturation.overrideState = true; ca.saturation.value = 6f;

        var wb = GetOrAdd<WhiteBalance>(profile);
        wb.temperature.overrideState = true; wb.temperature.value = 6f;
        wb.tint.overrideState = true;        wb.tint.value = 2f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var existing = Object.FindFirstObjectByType<Volume>();
        var go = existing != null ? existing.gameObject : new GameObject("PostVolume_Global");
        var vol = existing != null ? existing : go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 0f;
        vol.sharedProfile = profile;

        var lighting = GameObject.Find("_Lighting");
        if (lighting != null) go.transform.SetParent(lighting.transform, true);

        // Turn post-processing on for every camera, otherwise the volume does nothing.
        int n = 0;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = c.GetUniversalAdditionalCameraData();
            if (data != null) { data.renderPostProcessing = true; n++; }
        }

        Debug.Log("Cafe: post-processing volume added, enabled on " + n + " camera(s).");
    }
}
