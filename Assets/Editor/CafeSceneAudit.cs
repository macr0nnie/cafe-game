using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// Builds the cafe, renders it from fixed viewpoints and lists meshes that sink
/// into each other, so the scene can be checked without anyone playing it.
/// Menu: Cafe > 3. Audit Scene. Batch: -executeMethod CafeSceneAudit.Run -auditOut <dir>
public static class CafeSceneAudit
{
    [MenuItem("Cafe/3. Audit Scene", false, 30)]
    public static void Run()
    {
        string outDir = "Temp/CafeAudit";
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-auditOut") outDir = args[i + 1];
        Directory.CreateDirectory(outDir);

        CafeSceneBuilder.Build();
        CafePostFX.AddVolume();

        // Player height eye, in each direction, plus a wide corner view.
        Shot(outDir, "1_spawn_room",   new Vector3(0.2f, 1.78f, -3.5f),  new Vector3(8f, 8f, 0f));
        Shot(outDir, "2_backbar",      new Vector3(0.2f, 1.78f, -3.3f),  new Vector3(14f, 180f, 0f));
        Shot(outDir, "3_espresso",     new Vector3(-2.2f, 1.70f, -3.4f), new Vector3(28f, 180f, 0f));
        Shot(outDir, "4_syrups_ice",   new Vector3(1.4f, 1.70f, -3.4f),  new Vector3(22f, 180f, 0f));
        Shot(outDir, "7_menuboard",    new Vector3(-2.9f, 1.75f, -3.0f),  new Vector3(-12f, 180f, 0f));
        Shot(outDir, "5_overview",     new Vector3(3.6f, 2.9f, 3.1f),    new Vector3(28f, 220f, 0f));
        Shot(outDir, "6_seating",      new Vector3(-3.4f, 1.6f, 3.0f),   new Vector3(12f, 135f, 0f));

        File.WriteAllText(Path.Combine(outDir, "overlaps.txt"), Overlaps());
        Debug.Log("Cafe audit written to " + Path.GetFullPath(outDir));
    }

    static void Shot(string dir, string name, Vector3 pos, Vector3 euler)
    {
        var go = new GameObject("AuditCam", typeof(Camera));
        var cam = go.GetComponent<Camera>();
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        cam.fieldOfView = 62f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 40f;
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }

    /// Pairs of separate props whose bounds overlap by more than a couple of
    /// centimetres on every axis. Parts of the same object are allowed to touch.
    static string Overlaps()
    {
        var rs = new List<Renderer>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            string top = Root(r.transform).name;
            if (top == "Environment") continue;          // walls and floor touch everything by design
            rs.Add(r);
        }
        var sb = new StringBuilder();
        int n = 0;
        for (int i = 0; i < rs.Count; i++)
        for (int j = i + 1; j < rs.Count; j++)
        {
            if (Owner(rs[i].transform) == Owner(rs[j].transform)) continue;
            var a = rs[i].bounds; var b = rs[j].bounds;
            Vector3 o = Vector3.Min(a.max, b.max) - Vector3.Max(a.min, b.min);
            if (o.x > 0.02f && o.y > 0.02f && o.z > 0.02f)
            {
                sb.AppendLine(NameOf(rs[i].transform) + "  <->  " + NameOf(rs[j].transform) + "   overlap " + o.ToString("F2"));
                n++;
            }
        }
        return n + " overlapping pairs\n" + sb;
    }

    static Transform Root(Transform t) { while (t.parent != null) t = t.parent; return t; }
    // The object a mesh belongs to: the child of its group (e.g. Stations/EspressoMachine).
    static Transform Owner(Transform t) { while (t.parent != null && t.parent.parent != null) t = t.parent; return t; }
    static string NameOf(Transform t) { return t.parent == null ? t.name : NameOf(t.parent) + "/" + t.name; }
}
