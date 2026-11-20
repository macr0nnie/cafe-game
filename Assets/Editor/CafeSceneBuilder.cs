using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// Builds the playable cafe: smooth procedural furniture, first-person barista,
/// working drink stations and an order line. Menu: Cafe > 1. Build Cafe Scene
public static class CafeSceneBuilder
{
    const string MatDir    = "Assets/Art/Materials";
    const string TexDir    = "Assets/Art/Textures";
    const string PrefabDir = "Assets/Art/Prefabs";
    const string SceneDir  = "Assets/Scenes";
    const string ScenePath = SceneDir + "/CozyCafe.unity";
    const string ShaderName = "Cafe/Toon";

    // Room, in metres, origin at floor centre.
    const float XMin = -4.0f, XMax = 4.0f;
    const float ZMin = -4.5f, ZMax = 3.5f;
    const float Ceil = 3.2f, WallT = 0.15f;

    const float WinZMin = -0.5f, WinZMax = 2.2f, WinYMin = 0.90f, WinYMax = 2.40f;
    const float DoorXMin = -0.6f, DoorXMax = 0.6f, DoorY = 2.10f;

    const float CounterZ   = -2.40f;   // front counter centre
    const float CounterTopY = 1.161f;
    const float BackBarZ   = -4.28f;
    const float BackBarTopY = 0.9275f;

    static Dictionary<string, Material> _mats;
    static GameObject _cupPrefab, _ticketPrefab;

    [MenuItem("Cafe/1. Build Cafe Scene", false, 0)]
    public static void Build()
    {
        if (Shader.Find(ShaderName) == null)
        {
            EditorUtility.DisplayDialog("Shader missing",
                "Could not find \"" + ShaderName + "\". Check CafeToon.shader imported and this is a URP project.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Build cafe",
            "This replaces the current scene with a freshly built cafe.\n\nUnsaved changes will be lost.",
            "Build", "Cancel")) return;

        CafeMeshLibrary.Generate();
        BuildMaterials();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lighting = New("_Lighting");
        var env      = New("Environment");
        var fixtures = New("Fixtures");
        var furn     = New("Furniture");
        var stations = New("Stations");
        var lightFix = New("Lighting_Fixtures");
        var props    = New("Props");
        var plants   = New("Plants");
        var fx       = New("FX");
        var gameplay = New("_Gameplay");

        BuildShell(env);
        BuildCounter(fixtures);
        BuildBackBar(fixtures);
        BuildSeating(furn);
        BuildPendants(lightFix);
        BuildPlants(plants);
        BuildDressing(props);

        MakeCupPrefab();
        MakeTicketPrefab();

        BuildStations(stations);
        var player = BuildPlayer(gameplay);
        BuildOrders(gameplay, player);
        gameplay.gameObject.AddComponent<CafeDay>();
        BuildAtmosphere(fx);
        BuildLighting(lighting);

        CafeLighting.ApplyMorning();

        Directory.CreateDirectory(SceneDir);
        EditorSceneManager.SaveScene(EditorSceneManager.GetSceneAt(0), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log("Cafe built and saved to " + ScenePath +
                  ". Press Play, then WASD to move, E to use a station. Esc frees the cursor.");
    }

    static Transform New(string n, Transform parent = null)
    {
        var go = new GameObject(n);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    // ------------------------------------------------------------------ colour

    static Color Hex(string hex)
    {
        Color c;
        if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
        return c;
    }

    static readonly Dictionary<string, string> Palette = new Dictionary<string, string>
    {
        { "WallMint",   "#CDEEE3" }, { "WallPink",   "#FBE0EA" },
        { "FloorA",     "#F7E1D7" }, { "FloorB",     "#E9D5F0" },
        { "Counter",    "#F6C6D0" }, { "Accent",     "#C7F0E0" },
        { "Cream",      "#FFF5E6" }, { "WarmWhite",  "#FFFAF2" },
        { "WoodMid",    "#D9A878" }, { "WoodDark",   "#A8754C" },
        { "MetalGrey",  "#E8E8EF" }, { "PlantGreen", "#B5E3C0" },
        { "PlantDark",  "#8FC79E" }, { "LampWarm",   "#FFD6A5" },
        { "Pastry",     "#F3D9B1" }, { "LineDark",   "#8E7F9A" },
        { "Paper",      "#FFF8EC" }, { "Chrome",     "#DCDCE6" },
        { "Caramel",    "#C98B4B" }, { "Strawberry", "#F0A0B0" },
        { "Taro",       "#C9B8E8" }, { "Matcha",     "#9BBF6A" },
        { "Chocolate",  "#8A5A44" }, { "Vanilla",    "#F3E0B8" },
        { "Ice",        "#E8F4F8" }, { "Boba",       "#4A3A3F" },
    };

    static void BuildMaterials()
    {
        Directory.CreateDirectory(MatDir);
        var shader = Shader.Find(ShaderName);
        var shadowTint = Hex("#B7A6C9");
        _mats = new Dictionary<string, Material>();

        foreach (var kv in Palette)
            _mats[kv.Key] = Mat("M_" + kv.Key, shader, Hex(kv.Value), shadowTint, 3f, 0.03f, 0.5f);

        // Floor uses one tiled checker texture instead of 64 separate cubes.
        var floorTex = MakeCheckerTexture("T_FloorChecker", 256, Hex("#F7E1D7"), Hex("#E9D5F0"));
        var floor = Mat("M_Floor", shader, Color.white, shadowTint, 3f, 0.03f, 0.5f);
        floor.SetTexture("_BaseMap", floorTex);
        floor.SetTextureScale("_BaseMap", new Vector2(8f, 8f));
        _mats["Floor"] = floor;

        // Ceiling faces down, so it only ever sees the shadow band.
        _mats["CeilingCream"] = Mat("M_CeilingCream", shader, Hex("#FFF5E6"), Hex("#F2E6DA"), 3f, 0.03f, 1f);

        var bulb = Mat("M_BulbGlow", shader, Hex("#FFF3C4"), Hex("#FFF3C4"), 3f, 0.03f, 1f);
        bulb.SetColor("_EmissionColor", Hex("#FFF3C4") * 1.7f);
        _mats["BulbGlow"] = bulb;

        // Outside the window. Bright enough to read as daylight, dim enough not
        // to blow out: a big emissive card at bulb strength whites the whole frame.
        var day = Mat("M_Daylight", shader, Hex("#FBF3E2"), Hex("#EFE4D4"), 3f, 0.03f, 1f);
        day.SetColor("_EmissionColor", Hex("#FFF6E4") * 0.30f);
        _mats["Daylight"] = day;

        // Liquid gets a rim so the surface disc reads inside the cup.
        var liquid = Mat("M_Liquid", shader, Hex("#8A5E3F"), Hex("#6B4430"), 3f, 0.06f, 0.7f);
        liquid.SetColor("_RimColor", Hex("#FFF5E6"));
        liquid.SetFloat("_RimStrength", 0.18f);
        liquid.SetFloat("_RimPower", 5f);
        _mats["Liquid"] = liquid;

        // Clear glass, so the drink's layers read from the side. Palette Ice, alpha blended.
        var glass = Mat("M_Glass", shader, new Color(0.91f, 0.957f, 0.973f, 0.30f), Hex("#B7A6C9"), 3f, 0.03f, 0.8f);
        glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite", 0f);
        glass.SetColor("_RimColor", Hex("#FFFAF2"));
        glass.SetFloat("_RimStrength", 0.55f);
        glass.SetFloat("_RimPower", 3f);
        glass.renderQueue = (int)RenderQueue.Transparent;
        _mats["Glass"] = glass;

        AssetDatabase.SaveAssets();
    }

    static Material Mat(string name, Shader shader, Color baseCol, Color shadow,
                        float bands, float soft, float ambientLift)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        m.SetColor("_BaseColor", baseCol);
        m.SetColor("_ShadowTint", shadow);
        m.SetFloat("_Bands", bands);
        m.SetFloat("_BandSoftness", soft);
        m.SetFloat("_AmbientLift", ambientLift);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D MakeCheckerTexture(string name, int size, Color a, Color b)
    {
        Directory.CreateDirectory(TexDir);
        string path = TexDir + "/" + name + ".png";

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        int half = size / 2;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool even = ((x < half) ^ (y < half));
            Color c = even ? a : b;
            // A touch of painted variation so the tiles are not dead flat.
            float n = Mathf.Sin(x * 0.19f) * Mathf.Cos(y * 0.23f) * 0.012f
                    + Mathf.Sin(x * 0.07f + y * 0.05f) * 0.010f;
            tex.SetPixel(x, y, new Color(c.r + n, c.g + n, c.b + n, 1f));
        }
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Bilinear;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material M(string key)
    {
        Material m;
        return _mats.TryGetValue(key, out m) ? m : _mats["Cream"];
    }

    // ------------------------------------------------------------------ spawn helpers

    /// A mesh from the library, placed in the world.
    static GameObject Obj(string name, string meshKey, string mat, Vector3 pos,
                          Transform parent, Vector3? euler = null, Vector3? scale = null)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        if (scale.HasValue) go.transform.localScale = scale.Value;
        go.GetComponent<MeshFilter>().sharedMesh = CafeMeshLibrary.Get(meshKey);
        go.GetComponent<MeshRenderer>().sharedMaterial = M(mat);
        return go;
    }

    /// Flat slab, for walls, floor and ceiling where rounding would be wasted.
    static GameObject Slab(string name, Vector3 min, Vector3 max, string mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = (min + max) * 0.5f;
        go.transform.localScale = max - min;
        go.GetComponent<MeshRenderer>().sharedMaterial = M(mat);
        return go;
    }

    static GameObject Cyl(string name, float x, float baseY, float z, float dia,
                          float h, string mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, baseY + h * 0.5f, z);
        go.transform.localScale = new Vector3(dia, h * 0.5f, dia);
        go.GetComponent<MeshRenderer>().sharedMaterial = M(mat);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // ------------------------------------------------------------------ shell

    static void BuildShell(Transform root)
    {
        var shell = New("Shell", root);

        var floor = Slab("Floor", new Vector3(XMin, -WallT, ZMin), new Vector3(XMax, 0f, ZMax), "Floor", shell);
        // One mesh, one draw call, instead of a grid of cubes.
        floor.isStatic = true;

        Slab("Ceiling", new Vector3(XMin, Ceil, ZMin), new Vector3(XMax, Ceil + WallT, ZMax), "CeilingCream", shell);
        Slab("Wall_Back", new Vector3(XMin, 0f, ZMin - WallT), new Vector3(XMax, Ceil, ZMin), "WallMint", shell);
        Slab("Wall_Left", new Vector3(XMin - WallT, 0f, ZMin), new Vector3(XMin, Ceil, ZMax), "WallPink", shell);

        var rw = New("Wall_Right", shell);
        Slab("Seg_Back",  new Vector3(XMax, 0f, ZMin),           new Vector3(XMax + WallT, Ceil, WinZMin), "WallMint", rw);
        Slab("Seg_Front", new Vector3(XMax, 0f, WinZMax),        new Vector3(XMax + WallT, Ceil, ZMax), "WallMint", rw);
        Slab("Seg_Below", new Vector3(XMax, 0f, WinZMin),        new Vector3(XMax + WallT, WinYMin, WinZMax), "WallMint", rw);
        Slab("Seg_Above", new Vector3(XMax, WinYMax, WinZMin),   new Vector3(XMax + WallT, Ceil, WinZMax), "WallMint", rw);

        var wf = New("Window_Frame", shell);
        float fx = XMax - 0.03f;
        Slab("Sill",   new Vector3(fx, WinYMin - 0.06f, WinZMin), new Vector3(fx + 0.09f, WinYMin, WinZMax), "Cream", wf);
        Slab("Head",   new Vector3(fx, WinYMax, WinZMin),         new Vector3(fx + 0.09f, WinYMax + 0.06f, WinZMax), "Cream", wf);
        Slab("Jamb_B", new Vector3(fx, WinYMin, WinZMin),         new Vector3(fx + 0.07f, WinYMax, WinZMin + 0.06f), "Cream", wf);
        Slab("Jamb_F", new Vector3(fx, WinYMin, WinZMax - 0.06f), new Vector3(fx + 0.07f, WinYMax, WinZMax), "Cream", wf);
        float mz = (WinZMin + WinZMax) * 0.5f;
        Slab("Mullion", new Vector3(fx, WinYMin, mz - 0.03f),     new Vector3(fx + 0.07f, WinYMax, mz + 0.03f), "Cream", wf);

        // A warm card outside the window, so the opening reads as daylight
        // rather than a hole onto the skybox ground.
        var sky = Slab("Window_Backdrop", new Vector3(XMax + 0.6f, 0.2f, WinZMin - 0.6f),
                                          new Vector3(XMax + 0.65f, Ceil, WinZMax + 0.6f), "Daylight", shell);
        sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var fwall = New("Wall_Front", shell);
        Slab("Seg_Left",  new Vector3(XMin, 0f, ZMax),     new Vector3(DoorXMin, Ceil, ZMax + WallT), "WallPink", fwall);
        Slab("Seg_Right", new Vector3(DoorXMax, 0f, ZMax), new Vector3(XMax, Ceil, ZMax + WallT), "WallPink", fwall);
        Slab("Seg_Above", new Vector3(DoorXMin, DoorY, ZMax), new Vector3(DoorXMax, Ceil, ZMax + WallT), "WallPink", fwall);
        Slab("Frame_L", new Vector3(DoorXMin - 0.06f, 0f, ZMax - 0.03f), new Vector3(DoorXMin, DoorY, ZMax + 0.05f), "Cream", fwall);
        Slab("Frame_R", new Vector3(DoorXMax, 0f, ZMax - 0.03f), new Vector3(DoorXMax + 0.06f, DoorY, ZMax + 0.05f), "Cream", fwall);
        Slab("Frame_T", new Vector3(DoorXMin, DoorY, ZMax - 0.03f), new Vector3(DoorXMax, DoorY + 0.06f, ZMax + 0.05f), "Cream", fwall);
    }

    // ------------------------------------------------------------------ casework

    static void BuildCounter(Transform root)
    {
        var c = New("CounterRun", root);
        Obj("Counter_Body", "CounterBody", "Counter", new Vector3(-0.5f, 0.55f, CounterZ), c);
        Obj("Counter_Top",  "CounterTop",  "Cream",   new Vector3(-0.5f, 1.13f, CounterZ), c);
        Obj("Counter_Return",     "CounterReturn",    "Counter", new Vector3(-2.72f, 0.55f, CounterZ - 0.50f), c);
        Obj("Counter_Return_Top", "CounterReturnTop", "Cream",   new Vector3(-2.72f, 1.13f, CounterZ - 0.50f), c);
        Slab("Kick", new Vector3(-2.6f, 0f, CounterZ - 0.37f), new Vector3(1.6f, 0.09f, CounterZ - 0.31f), "WoodDark", c);

        // Solid for the player to walk into.
        var col = new GameObject("Counter_Collision");
        col.transform.SetParent(c, false);
        var bc = col.AddComponent<BoxCollider>();
        bc.center = new Vector3(-0.5f, 0.58f, CounterZ);
        bc.size = new Vector3(4.3f, 1.16f, 0.78f);
        var bc2 = col.AddComponent<BoxCollider>();
        bc2.center = new Vector3(-2.72f, 0.58f, CounterZ - 0.50f);
        bc2.size = new Vector3(0.82f, 1.16f, 0.70f);
    }

    static void BuildBackBar(Transform root)
    {
        var b = New("BackBar", root);
        Obj("BackBar_Body", "BackBarBody", "Counter", new Vector3(-0.4f, 0.45f, BackBarZ), b);
        Obj("BackBar_Top",  "BackBarTop",  "Cream",   new Vector3(-0.4f, 0.90f, BackBarZ), b);

        var col = new GameObject("BackBar_Collision");
        col.transform.SetParent(b, false);
        var bc = col.AddComponent<BoxCollider>();
        bc.center = new Vector3(-0.4f, 0.47f, BackBarZ);
        bc.size = new Vector3(6.4f, 0.95f, 0.5f);

        var sh = New("Shelving", root);
        Obj("Shelf_Lower", "Shelf", "Cream", new Vector3(1.2f, 1.72f, -4.38f), sh);
        Obj("Shelf_Upper", "Shelf", "Cream", new Vector3(1.2f, 2.20f, -4.38f), sh);
        Obj("Menu_Board",  "MenuBoard", "LineDark", new Vector3(-2.9f, 2.10f, -4.40f), sh);

        string[] cupCols = { "WarmWhite", "Accent", "Counter" };
        for (int i = 0; i < 6; i++)
            Obj("Shelf_Mug_" + i, "MugBody", cupCols[i % 3],
                new Vector3(0.05f + i * 0.30f, 2.2275f, -4.38f), sh);
    }

    // ------------------------------------------------------------------ seating

    static void BuildSeating(Transform root)
    {
        var tables = New("Tables", root);
        var chairs = New("Chairs", root);

        RoundTable("Table_A", new Vector2(0.5f, 0.5f), true, tables);
        RoundTable("Table_B", new Vector2(-2.7f, 1.0f), false, tables);
        RoundTable("Table_C", new Vector2(2.20f, 2.40f), false, tables);

        ChairRing("Table_A", new Vector2(0.5f, 0.5f), 1.10f, new[] { 45f, 135f, 225f, 315f }, chairs);
        ChairRing("Table_B", new Vector2(-2.7f, 1.0f), 0.80f, new[] { 20f, 200f }, chairs);
        ChairRing("Table_C", new Vector2(2.20f, 2.40f), 0.80f, new[] { 20f, 200f }, chairs);

        var bench = New("WindowBench", root);
        Obj("Plinth", "BenchPlinth", "Accent",  new Vector3(3.76f, 0f,    0.30f), bench);
        Obj("Seat",   "BenchSeat",   "WoodMid", new Vector3(3.74f, 0.45f, 0.30f), bench);
        Obj("Back",   "BenchBack",   "WoodMid", new Vector3(3.96f, 0.515f, 0.30f), bench);
        Obj("Table",  "BenchTable",  "WoodMid", new Vector3(3.06f, 0.72f, 0.30f), bench);
        Obj("Leg_1",  "ChairLeg",    "WoodDark", new Vector3(3.06f, 0f, -0.62f), bench, null, new Vector3(1f, 1.6f, 1f));
        Obj("Leg_2",  "ChairLeg",    "WoodDark", new Vector3(3.06f, 0f,  1.22f), bench, null, new Vector3(1f, 1.6f, 1f));

        var bcol = bench.gameObject.AddComponent<BoxCollider>();
        bcol.center = new Vector3(3.78f, 0.45f, 0.30f);
        bcol.size = new Vector3(0.48f, 0.90f, 2.62f);
    }

    static void RoundTable(string name, Vector2 xz, bool large, Transform parent)
    {
        var t = New(name, parent);
        t.localPosition = new Vector3(xz.x, 0f, xz.y);
        Obj("Top",  large ? "TableTopLarge" : "TableTopSmall", "WoodMid", new Vector3(0f, 0.698f, 0f), t);
        Obj("Stem", "TablePedestal", "WoodDark", new Vector3(0f, 0.045f, 0f), t);
        Obj("Base", "TableBase", "WoodDark", Vector3.zero, t);

        var col = t.gameObject.AddComponent<CapsuleCollider>();
        col.height = 0.75f; col.radius = large ? 0.55f : 0.40f;
        col.center = new Vector3(0f, 0.375f, 0f);
    }

    static void ChairRing(string tag, Vector2 xz, float tableDia, float[] angles, Transform parent)
    {
        float r = tableDia * 0.5f + 0.42f;
        for (int i = 0; i < angles.Length; i++)
        {
            float a = angles[i] * Mathf.Deg2Rad;
            var pos = new Vector3(xz.x + Mathf.Cos(a) * r, 0f, xz.y + Mathf.Sin(a) * r);
            var chair = New(tag + "_Chair_" + (i + 1), parent);
            chair.localPosition = pos;
            chair.localRotation = Quaternion.LookRotation(
                (new Vector3(xz.x, 0f, xz.y) - pos).normalized, Vector3.up);

            Obj("Seat", "ChairSeat", "Accent", new Vector3(0f, 0.475f, 0f), chair);
            Obj("Back", "ChairBack", "Accent", new Vector3(0f, 0.70f, -0.195f), chair,
                new Vector3(-6f, 0f, 0f));
            Obj("Rail", "ChairRail", "WoodDark", new Vector3(0f, 0.20f, 0f), chair);

            float lx = 0.172f;
            Obj("Leg_FL", "ChairLeg", "WoodDark", new Vector3(-lx, 0f,  lx), chair);
            Obj("Leg_FR", "ChairLeg", "WoodDark", new Vector3( lx, 0f,  lx), chair);
            Obj("Leg_BL", "ChairLeg", "WoodDark", new Vector3(-lx, 0f, -lx), chair);
            Obj("Leg_BR", "ChairLeg", "WoodDark", new Vector3( lx, 0f, -lx), chair);

            var col = chair.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.5f, 0.9f, 0.5f);
        }
    }

    // ------------------------------------------------------------------ lights, plants, dressing

    // One over each table, rather than a row that lines up with nothing.
    static readonly Vector2[] PendantXZ = {
        new Vector2(-2.70f, 1.00f), new Vector2(0.50f, 0.50f), new Vector2(2.20f, 2.40f)
    };

    static void BuildPendants(Transform root)
    {
        for (int i = 0; i < PendantXZ.Length; i++)
        {
            var p = New("Pendant_0" + (i + 1), root);
            float x = PendantXZ[i].x, z = PendantXZ[i].y;

            Cyl("Cord", x, 2.50f, z, 0.018f, Ceil - 2.50f, "LineDark", p);
            Obj("Shade", "PendantShade", "LampWarm", new Vector3(x, 2.30f, z), p);
            var bulb = Obj("Bulb", "Bulb", "BulbGlow", new Vector3(x, 2.33f, z), p);
            bulb.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var lgo = new GameObject("Point_Light");
            lgo.transform.SetParent(p, false);
            lgo.transform.localPosition = new Vector3(x, 2.24f, z);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Hex("#FFD9A0");
            l.intensity = 1.2f;
            l.range = 4.6f;
            l.shadows = LightShadows.None;
        }
    }

    static void BuildPlants(Transform root)
    {
        Planter("Planter_A", -3.55f, 2.70f, 1.00f, 0, root);
        Planter("Planter_B",  3.55f, -2.20f, 0.86f, 1, root);
        Planter("Planter_C", -3.55f, -1.40f, 0.74f, 2, root);

        var cp = New("CounterPlant", root);
        Obj("Pot", "SmallPot", "Counter", new Vector3(1.42f, CounterTopY, CounterZ), cp);
        Obj("Leaves", "Foliage3", "PlantGreen", new Vector3(1.42f, CounterTopY + 0.26f, CounterZ), cp,
            null, Vector3.one * 0.52f);
    }

    static void Planter(string name, float x, float z, float scale, int seed, Transform parent)
    {
        var p = New(name, parent);
        Obj("Pot", "PlantPot", "Counter", new Vector3(x, 0f, z), p, null, Vector3.one * scale);
        float potTop = 0.42f * scale;
        Obj("Foliage_1", "Foliage" + (seed % 4), "PlantGreen",
            new Vector3(x, potTop + 0.30f * scale, z), p, null, Vector3.one * scale);
        Obj("Foliage_2", "Foliage" + ((seed + 1) % 4), "PlantDark",
            new Vector3(x + 0.11f * scale, potTop + 0.54f * scale, z - 0.07f * scale), p,
            new Vector3(0f, 40f, 12f), Vector3.one * scale * 0.62f);
        Obj("Foliage_3", "Foliage" + ((seed + 2) % 4), "PlantGreen",
            new Vector3(x - 0.13f * scale, potTop + 0.46f * scale, z + 0.08f * scale), p,
            new Vector3(0f, 160f, -10f), Vector3.one * scale * 0.52f);

        var col = p.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(x, 0.4f * scale, z);
        col.radius = 0.22f * scale;
        col.height = 0.9f * scale;
    }

    static void BuildDressing(Transform root)
    {
        Obj("Pastry_Case", "PastryCase", "Cream", new Vector3(-1.75f, CounterTopY, CounterZ), root);
        for (int i = 0; i < 3; i++)
            Obj("Pastry_" + i, "Pastry", "Pastry",
                new Vector3(-2.08f + i * 0.33f, CounterTopY + 0.12f, CounterZ), root,
                new Vector3(0f, i * 48f, 0f));

        Obj("Till", "TillBody", "MetalGrey", new Vector3(1.05f, CounterTopY, CounterZ), root);
        Obj("Doormat", "Doormat", "WoodDark", new Vector3(0f, 0.012f, 2.95f), root);
    }

    // ------------------------------------------------------------------ prefabs

    static void MakeCupPrefab()
    {
        Directory.CreateDirectory(PrefabDir);
        var go = new GameObject("Cup");

        // Big enough to read at a glance in first person.
        go.transform.localScale = Vector3.one * CupScale;
        Obj("Body", "MugBody", "Glass", Vector3.zero, go.transform)
            .GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        Obj("Handle", "MugHandle", "Glass", new Vector3(0.056f, 0.052f, 0f), go.transform,
            new Vector3(0f, 90f, -108f)).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var liquid = Obj("Liquid", "LiquidColumn", "Liquid", new Vector3(0f, 0.011f, 0f), go.transform,
            null, new Vector3(1f, 0.082f, 1f));
        liquid.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var pearls = New("Pearls", go.transform);
        for (int i = 0; i < 11; i++)
        {
            float a = i * 2.39996f;
            float r = 0.021f * Mathf.Sqrt((i + 0.5f) / 11f);
            Obj("Pearl_" + i, "BobaPearl", "Boba",
                new Vector3(Mathf.Cos(a) * r, 0.014f + (i % 3) * 0.007f, Mathf.Sin(a) * r), pearls);
        }

        var ice = New("Ice", go.transform);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.7f;
            Obj("Ice_" + i, "IceCube", "Ice",
                new Vector3(Mathf.Cos(a) * 0.016f, 0.062f + (i % 2) * 0.012f, Mathf.Sin(a) * 0.016f), ice,
                new Vector3(i * 27f, i * 41f, i * 13f));
        }

        var cream = Obj("Cream", "CreamSwirl", "WarmWhite", new Vector3(0f, 0.090f, 0f), go.transform);

        var cc = go.AddComponent<CupContents>();
        cc.LiquidVisual = liquid.transform;
        cc.LiquidRenderer = liquid.GetComponent<MeshRenderer>();
        cc.PearlVisual = pearls;
        cc.IceVisual = ice;
        cc.CreamVisual = cream.transform;
        cc.MaxFill = 0.082f;

        pearls.gameObject.SetActive(false);
        ice.gameObject.SetActive(false);
        cream.SetActive(false);
        liquid.SetActive(false);

        _cupPrefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Cup.prefab");
        Object.DestroyImmediate(go);
    }

    static void MakeTicketPrefab()
    {
        Directory.CreateDirectory(PrefabDir);
        var go = new GameObject("OrderTicket");

        // Pivot sits at the peg so the slip swings from the line.
        var paper = New("Paper", go.transform);
        paper.localPosition = new Vector3(0f, -0.105f, 0f);
        Obj("Slip", "PaperTicket", "Paper", Vector3.zero, paper);
        Obj("Peg", "Peg", "Counter", new Vector3(0f, 0.102f, 0.006f), paper);

        var tmGO = new GameObject("Label");
        tmGO.transform.SetParent(paper, false);
        tmGO.transform.localPosition = new Vector3(0f, 0.035f, -0.004f);
        tmGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var tm = tmGO.AddComponent<TextMesh>();
        tm.text = "Latte";
        tm.anchor = TextAnchor.UpperCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 56;
        tm.characterSize = 0.0045f;
        tm.color = new Color(0.33f, 0.24f, 0.30f, 1f);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            tmGO.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        go.AddComponent<OrderTicket>();

        _ticketPrefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/OrderTicket.prefab");
        Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------------ stations

    static GameObject StationRoot(string name, Transform parent, Vector3 pos,
                                  Vector3 colCentre, Vector3 colSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var bc = go.AddComponent<BoxCollider>();
        bc.center = colCentre;
        bc.size = colSize;
        return go;
    }

    /// Spout, cup rest and pour stream, placed consistently so every dispenser
    /// behaves the same: cup sits in front, stream lands in it.
    const float SpoutY   = 0.26f;
    const float SpoutZ   = 0.16f;
    const float StreamY  = 0.17f;
    const float CupScale = 1.6f;
    const float CupRimY  = 0.095f * CupScale;

    static IngredientStation Simple(string name, Transform parent, float x, float z, float topY,
                                    string meshKey, string mat, Ingredient produces,
                                    float duration, string verb, bool withSpout = true)
    {
        var go = StationRoot(name, parent, new Vector3(x, topY, z),
                             new Vector3(0f, 0.17f, 0.04f), new Vector3(0.34f, 0.38f, 0.42f));
        Obj("Body", meshKey, mat, Vector3.zero, go.transform);

        var st = go.AddComponent<IngredientStation>();
        st.Produces = produces;
        st.Duration = duration;
        st.VerbOverride = verb;
        st.PourEndAt = 1f;      // pour verbs stream for as long as you hold

        if (withSpout)
        {
            Obj("Spout", "Spout", "Chrome", new Vector3(0f, SpoutY, SpoutZ), go.transform,
                new Vector3(180f, 0f, 0f));

            var cupRest = New("CupAnchor", go.transform);
            cupRest.localPosition = new Vector3(0f, 0f, SpoutZ);
            st.CupAnchor = cupRest;

            var stream = Obj("PourStream", "PourStream", mat,
                             new Vector3(0f, StreamY, SpoutZ), go.transform);
            st.PourStream = stream.transform;
            st.PourLength = StreamY - CupRimY;
            stream.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            stream.SetActive(false);
        }
        return st;
    }

    static void BuildStations(Transform root)
    {
        float y = BackBarTopY;
        float z = BackBarZ;

        // ---------------- espresso zone, left end of the bar
        var espGO = StationRoot("EspressoMachine", root, new Vector3(-2.25f, y, z),
                                new Vector3(0f, 0.30f, 0.04f), new Vector3(0.95f, 0.62f, 0.52f));
        Obj("Body",  "MachineBody", "MetalGrey", Vector3.zero, espGO.transform);
        Obj("Group", "GroupHead",   "LineDark",  new Vector3(0f, 0.30f, 0.19f), espGO.transform);
        Obj("Tray",  "DripTray",    "Chrome",    new Vector3(0f, 0.0f, 0.19f), espGO.transform);
        Obj("Spout_L", "Spout", "Chrome", new Vector3(-0.035f, 0.30f, 0.19f), espGO.transform,
            new Vector3(180f, 0f, 0f));
        Obj("Spout_R", "Spout", "Chrome", new Vector3( 0.035f, 0.30f, 0.19f), espGO.transform,
            new Vector3(180f, 0f, 0f));

        var puck = Obj("Puck", "DripTray", "WoodDark", new Vector3(0f, 0.285f, 0.19f),
                       espGO.transform, null, new Vector3(0.55f, 0.6f, 0.55f));

        var espCup = New("CupAnchor", espGO.transform);
        espCup.localPosition = new Vector3(0f, 0.018f, 0.19f);   // on the drip tray

        var espStreams = New("PourStreams", espGO.transform);
        espStreams.localPosition = new Vector3(0f, 0.21f, 0.19f);
        Obj("S_L", "PourStream", "Chocolate", new Vector3(-0.035f, 0f, 0f), espStreams)
            .GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        Obj("S_R", "PourStream", "Chocolate", new Vector3( 0.035f, 0f, 0f), espStreams)
            .GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var esp = espGO.AddComponent<EspressoStation>();
        esp.Duration = 3.2f;
        esp.PourEndAt = 1f;
        esp.GroundsVisual = puck.transform;
        esp.CupAnchor = espCup;
        esp.PourStream = espStreams;
        esp.PourLength = 0.21f - (0.018f + CupRimY);
        esp.Steam = MakeSteam("Steam", espGO.transform, new Vector3(0f, 0.34f, 0.19f), 0.5f);
        puck.SetActive(false);
        espStreams.gameObject.SetActive(false);

        var grGO = StationRoot("Grinder", root, new Vector3(-3.00f, y, z),
                               new Vector3(0f, 0.26f, 0f), new Vector3(0.30f, 0.56f, 0.32f));
        Obj("Body", "GrinderBody", "MetalGrey", Vector3.zero, grGO.transform);
        var hopper = Obj("Hopper", "Hopper", "LineDark", new Vector3(0f, 0.34f, 0f), grGO.transform);
        var gr = grGO.AddComponent<GrinderStation>();
        gr.Duration = 2.0f;
        gr.Machine = esp;
        gr.MovingPart = hopper.transform;
        gr.MovingPartSpeed = 460f;

        var wandGO = StationRoot("SteamWand", root, new Vector3(-1.50f, y, z),
                                 new Vector3(0.05f, 0.20f, 0.06f), new Vector3(0.46f, 0.44f, 0.38f));
        Obj("Jug", "Hopper", "Chrome", new Vector3(-0.04f, 0f, 0.08f), wandGO.transform,
            null, new Vector3(0.85f, 1.05f, 0.85f));
        var wandPivot = New("WandPivot", wandGO.transform);
        wandPivot.localPosition = new Vector3(-0.04f, 0.32f, -0.05f);
        Obj("Wand", "SteamWand", "Chrome", new Vector3(0f, -0.13f, 0.07f), wandPivot);

        var wandCup = New("CupAnchor", wandGO.transform);
        wandCup.localPosition = new Vector3(0.17f, 0f, 0.12f);    // rests beside the jug

        var wand = wandGO.AddComponent<SteamWandStation>();
        wand.Duration = 2.6f;
        wand.SweetStart = 0.55f;
        wand.SweetEnd = 0.80f;
        wand.Wand = wandPivot;
        wand.CupAnchor = wandCup;
        wand.Steam = MakeSteam("Steam", wandGO.transform, new Vector3(-0.04f, 0.18f, 0.08f), 1.2f);

        // ---------------- milk and brew zone
        // Pour verb: hold, watch it rise in the glass, let go at the line.
        Simple("MilkJug",   root, -0.95f, z, y, "Hopper",        "Chrome",  Ingredient.Milk,    2.4f, "Pour");
        Simple("HotWater",  root, -0.50f, z, y, "DispenserBody", "Chrome",  Ingredient.Water,   2.4f, "Pour");
        Simple("TeaBrewer", root, -0.05f, z, y, "DispenserBody", "WoodMid", Ingredient.TeaBrew, 3.0f, "Pour");

        // Scoop verb: tap once per scoop.
        Simple("Matcha",    root,  0.40f, z, y, "DispenserBody", "Matcha",    Ingredient.Matcha,    0f, "Scoop", false).Scoops = 2;
        Simple("Chocolate", root,  0.80f, z, y, "DispenserBody", "Chocolate", Ingredient.Chocolate, 0f, "Scoop", false).Scoops = 2;
        Simple("IceDispenser", root, 1.25f, z, y, "DispenserBody", "Ice",     Ingredient.Ice,        0f, "Scoop", false).Scoops = 3;
        Simple("BobaCooker",   root, 1.70f, z, y, "DispenserBody", "Boba",    Ingredient.BobaPearls, 0f, "Scoop", false).Scoops = 3;
        foreach (var n in new[] { "Matcha", "Chocolate", "IceDispenser", "BobaCooker" })
            root.Find(n).GetComponent<IngredientStation>().Verb = StationVerb.Scoop;

        // Pump verb: one tap.
        var cream = Simple("WhippedCream", root, 2.15f, z, y, "DispenserBody", "WarmWhite", Ingredient.WhippedCream, 0.5f, "Top with");
        cream.Verb = StationVerb.Pump;

        // Scooped stations get an open tub rather than a spout.
        foreach (var n in new[] { "Matcha", "Chocolate", "IceDispenser", "BobaCooker", "WhippedCream" })
        {
            var t = root.Find(n);
            if (t != null)
            {
                var rest = New("CupAnchor", t);
                rest.localPosition = new Vector3(0f, 0f, SpoutZ);
                var st = t.GetComponent<IngredientStation>();
                if (st != null) st.CupAnchor = rest;
            }
        }

        // ---------------- syrup bottles live on the shelf, like a real bar
        float shelfY = 1.7425f, shelfZ = -4.38f;
        SyrupBottle("TaroSyrup",       root, 1.05f, shelfZ, shelfY, "Taro",       Ingredient.TaroSyrup);
        SyrupBottle("VanillaSyrup",    root, 1.40f, shelfZ, shelfY, "Vanilla",    Ingredient.VanillaSyrup);
        SyrupBottle("CaramelSyrup",    root, 1.75f, shelfZ, shelfY, "Caramel",    Ingredient.CaramelSyrup);
        SyrupBottle("StrawberrySyrup", root, 2.10f, shelfZ, shelfY, "Strawberry", Ingredient.StrawberrySyrup);

        // ---------------- cups, serving, bin
        var cupGO = StationRoot("CupDispenser", root,
                                new Vector3(-2.72f, CounterTopY, CounterZ - 0.50f),
                                new Vector3(0f, 0.11f, 0f), new Vector3(0.26f, 0.26f, 0.26f));
        for (int i = 0; i < 4; i++)
            Obj("Stack_" + i, "MugBody", "Glass", new Vector3(0f, i * 0.030f * CupScale, 0f), cupGO.transform,
                null, Vector3.one * CupScale).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        var disp = cupGO.AddComponent<CupDispenser>();
        disp.CupPrefab = _cupPrefab;

        var serveGO = StationRoot("ServeWindow", root,
                                  new Vector3(0.30f, CounterTopY, CounterZ + 0.06f),
                                  new Vector3(0f, 0.06f, 0f), new Vector3(0.66f, 0.16f, 0.46f));
        Obj("Tray", "Tray", "WoodMid", Vector3.zero, serveGO.transform);
        serveGO.AddComponent<ServeWindow>();

        var binGO = StationRoot("TrashBin", root, new Vector3(-3.35f, 0f, -3.45f),
                                new Vector3(0f, 0.30f, 0f), new Vector3(0.40f, 0.60f, 0.40f));
        Obj("Body", "PlantPot", "LineDark", Vector3.zero, binGO.transform, null, new Vector3(0.95f, 1.35f, 0.95f));
        binGO.AddComponent<TrashBin>();
    }

    static void SyrupBottle(string name, Transform parent, float x, float z, float topY,
                            string mat, Ingredient produces)
    {
        var go = StationRoot(name, parent, new Vector3(x, topY, z),
                             new Vector3(0f, 0.12f, 0.03f), new Vector3(0.20f, 0.26f, 0.24f));
        Obj("Bottle", "SyrupBottle", mat, Vector3.zero, go.transform);

        var st = go.AddComponent<IngredientStation>();
        st.Produces = produces;
        st.Duration = 0.5f;
        st.VerbOverride = "Pump";
        st.Verb = StationVerb.Pump;

        // Held up under the pump, so no cup rest here.
        var stream = Obj("PourStream", "PourStream", mat, new Vector3(0f, 0.20f, 0.05f), go.transform);
        st.PourStream = stream.transform;
        st.PourLength = 0.08f;
        stream.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        stream.SetActive(false);
    }

    // ------------------------------------------------------------------ player

    static GameObject BuildPlayer(Transform root)
    {
        var p = new GameObject("Player");
        p.transform.SetParent(root, false);
        p.transform.localPosition = new Vector3(0.2f, 0.15f, -3.50f);
        // Facing out into the room on spawn: you see the cafe you are working in.
        // Turn around for the machines behind you.
        p.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);

        var cc = p.AddComponent<CharacterController>();
        cc.height = 1.75f;
        cc.radius = 0.28f;
        cc.center = new Vector3(0f, 0.875f, 0f);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;

        var eye = new GameObject("Eye", typeof(Camera), typeof(AudioListener));
        eye.transform.SetParent(p.transform, false);
        eye.transform.localPosition = new Vector3(0f, 1.63f, 0f);
        eye.tag = "MainCamera";
        var cam = eye.GetComponent<Camera>();
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 40f;

        var hold = new GameObject("HoldAnchor");
        hold.transform.SetParent(eye.transform, false);
        // Low right and a bit further out, so the bigger glass sits fully in frame.
        hold.transform.localPosition = new Vector3(0.22f, -0.21f, 0.46f);
        hold.transform.localRotation = Quaternion.Euler(-8f, -14f, 6f);

        var fps = p.AddComponent<FirstPersonController>();
        fps.CameraPivot = eye.transform;

        var hands = p.AddComponent<PlayerHands>();
        hands.HoldAnchor = hold.transform;
        hands.DefaultCupPrefab = _cupPrefab;

        var inter = p.AddComponent<PlayerInteractor>();
        inter.Eye = cam;
        inter.Hands = hands;
        inter.Range = 2.4f;

        var hud = p.AddComponent<CafeHUD>();
        hud.Interactor = inter;

        // UI: open orders down the left, and every menu screen.
        p.AddComponent<OrderBoard>().Hands = hands;
        p.AddComponent<CafeMenus>();

        return p;
    }

    // ------------------------------------------------------------------ orders

    static void BuildOrders(Transform root, GameObject player)
    {
        var line = New("OrderLine", root);

        // Above the counter, not across the aisle: tickets hung at 2.16 on the
        // walkway were at face height.
        var a = New("LineStart", line); a.localPosition = new Vector3(-1.80f, 2.38f, -2.62f);
        var b = New("LineEnd",   line); b.localPosition = new Vector3( 1.60f, 2.38f, -2.62f);

        // The string itself.
        var cord = Cyl("Cord", 0f, 0f, 0f, 0.008f, 1f, "LineDark", line);
        cord.transform.localPosition = (a.localPosition + b.localPosition) * 0.5f;
        cord.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        float span = Vector3.Distance(a.localPosition, b.localPosition);
        cord.transform.localScale = new Vector3(0.008f, span * 0.5f, 0.008f);

        Cyl("Hook_L", a.localPosition.x - 0.06f, a.localPosition.y, a.localPosition.z, 0.02f, 0.18f, "LineDark", line);
        Cyl("Hook_R", b.localPosition.x + 0.06f, b.localPosition.y, b.localPosition.z, 0.02f, 0.18f, "LineDark", line);

        var mgr = line.gameObject.AddComponent<OrderManager>();
        mgr.LineStart = a;
        mgr.LineEnd = b;
        mgr.TicketPrefab = _ticketPrefab;
        mgr.MaxOpenOrders = 4;
        mgr.FirstOrderDelay = 4f;
        mgr.SecondsBetweenOrders = 18f;
    }

    // ------------------------------------------------------------------ atmosphere

    static Material _particleMat;

    static Material ParticleMaterial()
    {
        if (_particleMat != null) return _particleMat;
        Directory.CreateDirectory(TexDir);
        string path = TexDir + "/T_SoftPuff.png";

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
        {
            int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s - 0.5f, v = (y + 0.5f) / s - 0.5f;
                float d = Mathf.Sqrt(u * u + v * v) * 2f;
                float aa = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, aa * aa * 0.9f));
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.alphaIsTransparency = true; imp.SaveAndReimport(); }
        }

        string matPath = MatDir + "/M_Puff.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (m == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, matPath);
        }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        EditorUtility.SetDirty(m);
        _particleMat = m;
        return m;
    }

    static ParticleSystem MakeSteam(string name, Transform parent, Vector3 pos, float rate)
    {
        var go = new GameObject(name, typeof(ParticleSystem));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.9f;
        main.startSpeed = 0.17f;
        main.startSize = 0.055f;
        main.startColor = new Color(1f, 0.98f, 0.95f, 0.30f);
        main.gravityModifier = -0.035f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.playOnAwake = false;

        var em = ps.emission; em.rateOverTime = rate * 10f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 14f; sh.radius = 0.02f;
        sh.rotation = new Vector3(-90f, 0f, 0f);

        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.22f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sol = ps.sizeOverLifetime; sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 2.4f));

        var noise = ps.noise; noise.enabled = true;
        noise.strength = 0.12f; noise.frequency = 0.6f;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = ParticleMaterial();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;

        ps.Stop();
        return ps;
    }

    static void BuildAtmosphere(Transform root)
    {
        // Dust drifting in the window light. Cheap, and it does most of the
        // atmospheric work in a scene like this.
        var go = new GameObject("DustMotes", typeof(ParticleSystem));
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(1.6f, 1.5f, 0.3f);

        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 14f;
        main.startSpeed = 0.035f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.010f);
        main.startColor = new Color(1f, 0.96f, 0.88f, 0.30f);
        main.gravityModifier = -0.004f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        var em = ps.emission; em.rateOverTime = 6f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(4.4f, 2.4f, 5.0f);

        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.3f),
                    new GradientAlphaKey(0.45f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var noise = ps.noise; noise.enabled = true;
        noise.strength = 0.07f; noise.frequency = 0.17f;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = ParticleMaterial();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // ------------------------------------------------------------------ lighting

    static void BuildLighting(Transform root)
    {
        var sun = new GameObject("Sun_Directional");
        sun.transform.SetParent(root, false);
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        sun.transform.rotation = Quaternion.LookRotation(new Vector3(-0.62f, -0.67f, -0.41f).normalized);
        l.color = Hex("#FFF1D6");
        l.intensity = 1.8f;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.75f;
        l.shadowBias = 0.03f;
        l.shadowNormalBias = 0.2f;

        var probes = new GameObject("LightProbes");
        probes.transform.SetParent(root, false);
        var lpg = probes.AddComponent<LightProbeGroup>();
        var pts = new List<Vector3>();
        for (float x = -3f; x <= 3.01f; x += 1.5f)
        for (float z = -4f; z <= 3.01f; z += 1.5f)
        { pts.Add(new Vector3(x, 0.5f, z)); pts.Add(new Vector3(x, 1.7f, z)); }
        lpg.probePositions = pts.ToArray();
    }
}
