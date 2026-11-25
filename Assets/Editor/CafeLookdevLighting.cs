using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// Baked lighting for the lookdev cafe, so it keeps its look for less runtime cost:
/// - static geometry is lightmapped and static-batched; small props read light probes,
/// - the pendant lamps are baked (no per-pixel light loop at runtime),
/// - the sun is Mixed (shadowmask): static shadows come from the bake and only moving
///   things (customers, cups, doors, pumps) still render into the realtime shadow map.
/// Menu: Cafe > 5. Bake Lookdev Lighting. The scene builder calls Setup() itself.
public static class CafeLookdevLighting
{
    const string SettingsPath = "Assets/Scenes/CozyCafe_Lookdev_Lighting.lighting";
    const float SmallProp = 0.30f;   // metres: anything smaller reads light probes, not a lightmap

    [MenuItem("Cafe/5. Bake Lookdev Lighting", false, 3)]
    public static void BakeMenu()
    {
        Setup();
        if (!Lightmapping.BakeAsync()) Debug.LogError("Lookdev bake did not start.");
        else Debug.Log("Lookdev bake started; the scene is saved when it finishes.");
        Lightmapping.bakeCompleted -= SaveAfterBake;
        Lightmapping.bakeCompleted += SaveAfterBake;
    }

    static void SaveAfterBake()
    {
        Lightmapping.bakeCompleted -= SaveAfterBake;
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Lookdev bake finished and saved.");
    }

    /// Flags, light modes, probes and lightmap settings. Safe to run again.
    public static void Setup()
    {
        var cafe = GameObject.Find("Environment/Cafe");
        if (cafe == null) { Debug.LogError("Lookdev lighting: open the lookdev scene first."); return; }

        MarkStatic(cafe);
        SetLightModes();
        AddProbes();
        ApplySettings();
    }

    /// Everything that never moves is static; anything a prop clip animates stays dynamic.
    static void MarkStatic(GameObject cafe)
    {
        var moving = new HashSet<Transform>();
        var anim = cafe.GetComponent<LookdevAnimator>();
        if (anim != null && anim.Data != null)
        {
            var byName = new Dictionary<string, Transform>();
            foreach (var t in cafe.GetComponentsInChildren<Transform>(true))
                if (!byName.ContainsKey(t.name)) byName.Add(t.name, t);
            foreach (var c in LookdevData.Parse(anim.Data.text).clips)
            {
                Transform t;
                if (!byName.TryGetValue(c.@object, out t)) continue;
                foreach (var d in t.GetComponentsInChildren<Transform>(true)) moving.Add(d);
            }
        }

        int lightmapped = 0, probed = 0, dynamic = 0;
        foreach (var r in cafe.GetComponentsInChildren<MeshRenderer>(true))
        {
            var go = r.gameObject;
            if (moving.Contains(r.transform))
            {
                GameObjectUtility.SetStaticEditorFlags(go, 0);
                r.receiveGI = ReceiveGI.LightProbes;
                dynamic++;
                continue;
            }
            var b = r.bounds;
            float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic
                      | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            if (size > 1.0f) flags |= StaticEditorFlags.OccluderStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);

            // Small props still bounce light into the room but read it back from probes,
            // which keeps the lightmap small.
            if (size < SmallProp) { r.receiveGI = ReceiveGI.LightProbes; probed++; }
            else { r.receiveGI = ReceiveGI.Lightmaps; lightmapped++; }
        }
        Debug.Log("Lookdev lighting: " + lightmapped + " lightmapped, " + probed + " probe-lit, " + dynamic + " animated (dynamic).");
    }

    static void SetLightModes()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (l.type == LightType.Directional) l.lightmapBakeType = LightmapBakeType.Mixed;
            // No shadows, like the realtime pendants they replace: light passes the shade and
            // still pools on the ceiling above each lamp.
            else if (l.type == LightType.Point) { l.lightmapBakeType = LightmapBakeType.Baked; l.shadows = LightShadows.None; }
        }
    }

    /// Probes over the whole room at sitting, standing and head height, for customers,
    /// cups and small props. Replaces the grid the procedural builder sized for its own room.
    static void AddProbes()
    {
        var old = GameObject.Find("_Lighting/LightProbes");
        if (old != null) Object.DestroyImmediate(old);
        var go = new GameObject("LightProbes");
        var parent = GameObject.Find("_Lighting");
        if (parent != null) go.transform.SetParent(parent.transform, false);

        var pts = new List<Vector3>();
        for (float x = -5.4f; x <= 3.41f; x += 0.8f)
        for (float z = -3.4f; z <= 2.91f; z += 0.8f)
        foreach (var y in new[] { 0.35f, 1.1f, 1.9f, 2.7f })
            pts.Add(new Vector3(x, y, z));
        go.AddComponent<LightProbeGroup>().probePositions = pts.ToArray();
    }

    static void ApplySettings()
    {
        var s = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
        if (s == null) { s = new LightingSettings(); AssetDatabase.CreateAsset(s, SettingsPath); }
        s.bakedGI = true;
        s.realtimeGI = false;
        s.mixedBakeMode = MixedLightingMode.Shadowmask;
        s.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        s.lightmapResolution = 24f;          // texels per metre: small room, soft toon look
        s.lightmapPadding = 3;
        s.lightmapMaxSize = 2048;
        s.lightmapCompression = LightmapCompression.NormalQuality;
        s.directionalityMode = LightmapsMode.NonDirectional;
        s.ao = true;
        s.aoMaxDistance = 0.6f;
        s.directSampleCount = 32;
        s.indirectSampleCount = 256;
        s.maxBounces = 2;
        s.filteringMode = LightingSettings.FilterMode.Auto;
        EditorUtility.SetDirty(s);
        Lightmapping.lightingSettings = s;

        // Shadowmask (not Distance Shadowmask): static casters never render into the shadow map.
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;
        }
        QualitySettings.SetQualityLevel(current, false);
    }
}
