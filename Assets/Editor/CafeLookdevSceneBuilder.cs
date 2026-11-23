using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// Builds a playable scene from the Blender lookdev cafe (Assets/Art/Lookdev/CafeLookdev.fbx):
/// imports the model, gives it toon materials and colliders, puts the existing drink
/// stations on the matching props and wires the Blender prop animations to them.
/// Menu: Cafe > 4. Build Lookdev Scene.  Batch: -executeMethod CafeLookdevSceneBuilder.BuildBatch
public static class CafeLookdevSceneBuilder
{
    const string FbxPath   = LookdevImport.Folder + "CafeLookdev.fbx";
    const string DataPath  = LookdevImport.Folder + "CafeLookdev_data.json";
    // Kept apart from Assets/Art/Materials: these colours come from the Blender mock, not the
    // locked 18-colour palette the procedural builder (and its test) uses.
    const string MatDir    = LookdevImport.Folder + "Materials";
    const string ScenePath = "Assets/Scenes/CozyCafe_Lookdev.unity";

    // Blender (x, y, z), Z up  ->  Unity (x, y, z), Y up: Y and Z swap (measured on import with
    // bakeAxisConversion). Same mapping as tools/export_for_unity.py.
    static Vector3 B(float x, float y, float z) { return new Vector3(x, z, y); }

    static readonly Dictionary<string, Transform> _byName = new Dictionary<string, Transform>();
    static LookdevAnimator _anim;
    static Transform _anchors;
    static GameObject _cupPrefab;

    [MenuItem("Cafe/4. Build Lookdev Scene", false, 2)]
    public static void BuildMenu()
    {
        if (!EditorUtility.DisplayDialog("Build lookdev cafe",
            "Builds " + ScenePath + " from the Blender lookdev export.\n\nUnsaved changes in the open scene will be lost.",
            "Build", "Cancel")) return;
        Build();
    }

    public static void BuildBatch()
    {
        Build();
        EditorApplication.Exit(0);
    }

    public static void Build()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var data = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
        if (model == null || data == null)
        {
            Debug.LogError("Lookdev export missing: run tools/export_for_unity.py so " + FbxPath + " and " + DataPath + " exist.");
            return;
        }

        CafeMeshLibrary.Generate();
        CafeSceneBuilder.BuildMaterials();
        CafeSceneBuilder.MakeCupPrefab();
        CafeSceneBuilder.MakeTicketPrefab();
        _cupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/Cup.prefab");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var lighting = New("_Lighting");
        var env = New("Environment");
        var fx = New("FX");
        var gameplay = New("_Gameplay");
        _anchors = New("CupAnchors", gameplay);

        var cafe = (GameObject)PrefabUtility.InstantiatePrefab(model, env);
        PrefabUtility.UnpackPrefabInstance(cafe, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        cafe.name = "Cafe";

        _byName.Clear();
        foreach (var t in cafe.GetComponentsInChildren<Transform>(true))
            if (!_byName.ContainsKey(t.name)) _byName.Add(t.name, t);

        CheckAxes(data);
        ShowRenderTiers();
        ApplyMaterials(cafe);
        AddColliders(cafe);

        _anim = cafe.AddComponent<LookdevAnimator>();
        _anim.Data = data;

        BuildStations();

        var player = CafeSceneBuilder.BuildPlayer(gameplay);
        player.transform.position = B(1.0f, 1.76f, 0.05f);
        // Customers are on the -Z side here; turn round for the back bar.
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        CafeSceneBuilder.BuildOrders(gameplay, player);
        var line = gameplay.Find("OrderLine");
        if (line != null)
        {
            // The line was built for a barista on its -Z side; here the barista stands on +Z.
            var centre = B(0.8f, 1.42f, 2.30f);
            line.position += centre - new Vector3(-0.1f, 2.38f, -2.62f);
            line.RotateAround(centre, Vector3.up, 180f);
        }
        gameplay.gameObject.AddComponent<CafeDay>();
        BuildComputer(gameplay, LookdevData.Parse(data.text));

        BuildCustomers(gameplay, LookdevData.Parse(data.text));
        BakeNavMesh(gameplay, player);

        CafeSceneBuilder.BuildLighting(lighting);
        AddBulbLights(cafe, lighting);
        CafeSceneBuilder.BuildAtmosphere(fx);
        CafeLighting.ApplyMorning();
        CafePostFX.AddVolume();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Lookdev cafe built: " + ScenePath + ". Press Play: WASD to move, E to use a station.");
    }

    static Transform New(string n, Transform parent = null)
    {
        var go = new GameObject(n);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Transform Find(string n)
    {
        Transform t;
        if (!_byName.TryGetValue(n, out t)) Debug.LogWarning("Lookdev: no object named " + n);
        return t;
    }

    // ------------------------------------------------------------------ import checks

    /// The slot empties carry their own Blender positions; if the import does not land
    /// them where the converter says, every hand-placed anchor below would be off too.
    static void CheckAxes(TextAsset data)
    {
        var d = LookdevData.Parse(data.text);
        foreach (var s in d.slots)
        {
            if (s.id != "espresso") continue;
            var t = Find("SLOT_espresso");
            if (t == null) return;
            var want = new Vector3(s.centre[0], s.centre[1], s.centre[2]);
            float err = Vector3.Distance(t.position, want);
            if (err > 0.02f) Debug.LogError("Lookdev axis mismatch: SLOT_espresso at " + t.position + ", expected " + want);
            else Debug.Log("Lookdev axes OK (SLOT_espresso off by " + err.ToString("0.000") + " m)");
        }
    }

    /// Renders show the top tier; lower tiers stay in the scene, switched off, for upgrades.
    static void ShowRenderTiers()
    {
        foreach (var t in _byName.Values)
        {
            if (t.name.StartsWith("T0_") || t.name.StartsWith("T1_") || t.name == "Oven_Convection")
                t.gameObject.SetActive(false);
            if (t.name.StartsWith("FOOT_"))
            {
                var r = t.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }
    }

    // ------------------------------------------------------------------ materials

    static readonly HashSet<string> Clear = new HashSet<string> { "M_Glass", "M_PET", "M_Water", "M_Ice" };
    static readonly HashSet<string> Glow = new HashSet<string> { "M_Bulb", "M_Edison" };

    /// Swap the FBX materials for the project's toon shader, keeping each Blender colour.
    static void ApplyMaterials(GameObject cafe)
    {
        Directory.CreateDirectory(MatDir);
        var shader = Shader.Find(CafeSceneBuilder.ShaderName);
        var shadow = CafeSceneBuilder.Hex("#B7A6C9");
        var made = new Dictionary<string, Material>();

        foreach (var r in cafe.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string key = mats[i].name.Replace(" (Instance)", "");
                Material m;
                if (!made.TryGetValue(key, out m))
                {
                    if (Clear.Contains(key)) m = CafeSceneBuilder.M("Glass");
                    else if (Glow.Contains(key)) m = CafeSceneBuilder.M("BulbGlow");
                    else
                    {
                        Color c = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : mats[i].color;
                        c.a = 1f;
                        m = ToonMat(key, shader, c, shadow);
                        if (key == "M_SkyCard")
                        {
                            m.SetColor("_EmissionColor", c * 0.35f);
                            m.SetFloat("_AmbientLift", 1f);
                        }
                    }
                    made[key] = m;
                }
                mats[i] = m;
            }
            r.sharedMaterials = mats;
        }
    }

    static Material ToonMat(string name, Shader shader, Color baseCol, Color shadow)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        m.SetColor("_BaseColor", baseCol);
        m.SetColor("_ShadowTint", shadow);
        m.SetFloat("_Bands", 3f);
        m.SetFloat("_BandSoftness", 0.03f);
        m.SetFloat("_AmbientLift", 0.5f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ colliders

    /// Walls, floor, counters and furniture get mesh colliders. Counter-top props do not:
    /// the player cannot reach them anyway, and stations get their own trigger boxes.
    static void AddColliders(GameObject cafe)
    {
        foreach (var mf in cafe.GetComponentsInChildren<MeshFilter>(true))
        {
            var r = mf.GetComponent<Renderer>();
            if (r == null || mf.sharedMesh == null || !mf.gameObject.activeInHierarchy) continue;
            if (mf.name.StartsWith("FOOT_")) continue;
            var b = r.bounds;
            bool onFloor = b.min.y < 0.12f && b.size.y > 0.05f;
            bool large = Mathf.Max(b.size.x, b.size.y, b.size.z) > 1.2f;
            if (!onFloor && !large) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }
    }

    // ------------------------------------------------------------------ lights

    static void AddBulbLights(GameObject cafe, Transform root)
    {
        foreach (var r in cafe.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.name.Contains("Bulb") && !r.name.Contains("bulb")) continue;
            var go = new GameObject("Pendant_" + r.name);
            go.transform.SetParent(root, false);
            go.transform.position = r.bounds.center - Vector3.up * 0.06f;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = CafeSceneBuilder.Hex("#FFD6A5");
            l.range = 3.2f;
            l.intensity = 0.9f;
            l.shadows = LightShadows.None;
        }
    }

    // ------------------------------------------------------------------ stations

    static StationClips.ClipRef C(string obj, string clip) { return new StationClips.ClipRef(obj, clip); }

    /// Trigger box around the object's own mesh (not its children), so a machine body
    /// and a wand on it can be aimed at separately.
    static GameObject Target(string name, float minSize = 0.10f)
    {
        var t = Find(name);
        if (t == null) return null;
        var mf = t.GetComponent<MeshFilter>();
        var box = t.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        if (mf != null && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds;
            var s = t.lossyScale;
            float m = minSize;
            box.center = b.center;
            box.size = new Vector3(Mathf.Max(b.size.x, m / Mathf.Max(1e-4f, Mathf.Abs(s.x))),
                                   Mathf.Max(b.size.y, m / Mathf.Max(1e-4f, Mathf.Abs(s.y))),
                                   Mathf.Max(b.size.z, m / Mathf.Max(1e-4f, Mathf.Abs(s.z))));
        }
        else box.size = Vector3.one * minSize;
        return t.gameObject;
    }

    static Transform Anchor(string name, Vector3 worldPos)
    {
        var a = New("CupAnchor_" + name, _anchors);
        a.position = worldPos;
        return a;
    }

    static StationClips Clips(GameObject go, StationClips.ClipRef[] begin, StationClips.ClipRef[] loop = null,
                              StationClips.ClipRef[] end = null)
    {
        var sc = go.AddComponent<StationClips>();
        sc.Animator = _anim;
        sc.OnBegin = begin ?? new StationClips.ClipRef[0];
        sc.WhileRunning = loop ?? new StationClips.ClipRef[0];
        sc.OnEnd = end ?? new StationClips.ClipRef[0];
        foreach (var arr in new[] { sc.OnBegin, sc.WhileRunning, sc.OnEnd })
            foreach (var c in arr)
                if (!_anim.Has(c.Object, c.Clip)) Debug.LogWarning("Lookdev: clip " + c.Clip + " on " + c.Object + " not found");
        return sc;
    }

    static IngredientStation Ingredient(string obj, global::Ingredient produces, StationVerb verb, float duration,
                                        string verbWord, Vector3? cupAt, int scoops = 3)
    {
        var go = Target(obj);
        if (go == null) return null;
        var st = go.AddComponent<IngredientStation>();
        st.Produces = produces;
        st.Verb = verb;
        st.Duration = duration;
        st.VerbOverride = verbWord;
        st.Scoops = scoops;
        st.PourEndAt = 1f;
        if (cupAt.HasValue) st.CupAnchor = Anchor(obj, cupAt.Value);
        return st;
    }

    static void BuildStations()
    {
        const float Back = 2.36f, Front = 1.16f, Top = 0.91f;   // Blender: cup rests at the counter edges

        // Cups, at the register.
        foreach (var n in new[] { "CupDispenser_Base", "PETCups_Base" })
        {
            var go = Target(n);
            if (go == null) continue;
            go.AddComponent<CupDispenser>().CupPrefab = _cupPrefab;
        }
        Clips(Find("CupDispenser_Base").gameObject, null, null, new[] { C("CupDispenser_TopCup_12oz", "CupDispenser_Pop") });
        Clips(Find("PETCups_Base").gameObject, null, null, new[] { C("CupDispenser_TopCup_PET16oz", "CupDispenser_Pop") });

        // Espresso: grinder arms the machine, the machine pulls into the cup on group L.
        var espGO = Target("Espresso_Body");
        var esp = espGO.AddComponent<EspressoStation>();
        esp.Duration = 3.2f;
        esp.PourEndAt = 1f;
        esp.CupAnchor = Anchor("Espresso", B(1.57f, 2.41f, 0.94f));
        esp.Steam = CafeSceneBuilder.MakeSteam("Steam", _anchors, B(1.57f, 2.41f, 1.10f), 0.5f);
        Clips(espGO, new[] { C("Espresso_Portafilter_L", "Portafilter_LockIn") }, null,
                     new[] { C("Espresso_Portafilter_L", "Portafilter_Remove") });

        var grGO = Target("Grinder_Body");
        var gr = grGO.AddComponent<GrinderStation>();
        gr.Machine = esp;
        Clips(grGO, new[] { C("Grinder_GroundsPile", "Grinder_Dose"), C("Grinder_Beans", "Grinder_Dose_Hopper") });

        foreach (var side in new[] { "L", "R" })
        {
            string sfx = side == "R" ? "_R" : "";
            var wandGO = Target("Espresso_SteamWand_" + side, 0.14f);
            var wand = wandGO.AddComponent<SteamWandStation>();
            wand.Duration = 2.6f;
            wand.SweetStart = 0.55f;
            wand.SweetEnd = 0.80f;
            float x = side == "L" ? 1.42f : 2.08f;
            wand.CupAnchor = Anchor("SteamWand_" + side, B(x, Back, Top));
            wand.Steam = CafeSceneBuilder.MakeSteam("Steam", _anchors, B(x, 2.40f, 1.02f), 1.2f);
            Clips(wandGO, new[] { C("Espresso_SteamWand_" + side, "SteamWand_SwingIn" + sfx), C("Espresso_SteamValve_" + side, "SteamValve_Open") },
                  null, new[] { C("Espresso_SteamWand_" + side, "SteamWand_SwingOut" + sfx) });
        }

        // Milk from the fridge under the wands.
        var milk = Ingredient("Fridge_Door", global::Ingredient.Milk, StationVerb.Pour, 2.4f, "Pour", B(1.80f, Back, Top));
        Clips(milk.gameObject, new[] { C("Fridge_Door", "Fridge_Door_Open") }, null, new[] { C("Fridge_Door", "Fridge_Door_Close") });

        // Hot water and tea.
        var water = Ingredient("HotWater_Tower", global::Ingredient.Water, StationVerb.Pour, 2.4f, "Pour", B(0.27f, 2.48f, Top));
        Clips(water.gameObject, new[] { C("HotWater_Lever", "HotWater_Pour"), C("HotWater_Stream", "HotWater_Pour_Stream") });
        Ingredient("Teapot_1", global::Ingredient.TeaBrew, StationVerb.Pour, 3.0f, "Pour", B(0.23f, Front, Top));

        // Matcha in the chawan: sift, whisk while you work it, then pour into the cup.
        var matcha = Ingredient("Matcha_Chawan", global::Ingredient.Matcha, StationVerb.Scoop, 0f, "Whisk", B(0.55f, 2.37f, Top), 2);
        Clips(matcha.gameObject, new[] { C("Matcha_Sifter", "Matcha_Sift") },
              new[] { C("Matcha_Chasen", "Matcha_Whisk") },
              new[] { C("Matcha_Chawan", "Matcha_Pour") });

        Ingredient("Tin_Cocoa", global::Ingredient.Chocolate, StationVerb.Scoop, 0f, "Scoop", B(0.75f, Front, Top), 2);

        var ice = Ingredient("IceWell_Ice", global::Ingredient.Ice, StationVerb.Scoop, 0f, "Scoop", B(-0.08f, 2.34f, Top), 3);
        Clips(ice.gameObject, new[] { C("IceScoop", "IceScoop_Scoop") });

        // Flavour and toppings.
        var foam = Ingredient("Frother_Stand", global::Ingredient.Foam, StationVerb.Pump, 1.2f, "Froth", B(2.50f, Back, Top));
        Clips(foam.gameObject, null, new[] { C("Frother_Whisk", "Frother_Run") });
        Ingredient("WhippedCream_Canister", global::Ingredient.WhippedCream, StationVerb.Pump, 0.5f, "Top with", B(2.66f, Back, Top));
        Ingredient("Boba_Pot", global::Ingredient.BobaPearls, StationVerb.Scoop, 0f, "Scoop", B(2.97f, Back, Top), 3);

        var syrups = new Dictionary<string, global::Ingredient>
        {
            { "Vanilla", global::Ingredient.VanillaSyrup }, { "Caramel", global::Ingredient.CaramelSyrup },
            { "Strawberry", global::Ingredient.StrawberrySyrup }, { "Taro", global::Ingredient.TaroSyrup },
        };
        foreach (var kv in syrups)
        {
            string head = "SyrupPump_Head_" + kv.Key;
            var st = Ingredient(head, kv.Value, StationVerb.Pump, 0.5f, "Pump", null);
            if (st != null) Clips(st.gameObject, new[] { C(head, "SyrupPump_Press") });
        }

        // Finish: serve at the pickup shelf, bin under the back counter.
        Target("Pickup_Shelf").AddComponent<ServeWindow>();
        Target("BinCabinet_Front_1").AddComponent<TrashBin>();
        Target("BinCabinet_Front_2").AddComponent<TrashBin>();

        // Bakery corner: the pink oven door opens and closes.
        var oven = Target("T2_Stove_Door");
        if (oven != null)
        {
            oven.AddComponent<PropToggle>().Noun = "oven";
            Clips(oven, new[] { C("T2_Stove_Door", "Stove_Door_Open") }, null, new[] { C("T2_Stove_Door", "Stove_Door_Close") });
        }
    }

    // ------------------------------------------------------------------ customers

    /// Door, till queue, pickup spot and seats come from the Blender NPC points, so moving
    /// an empty in Blender and re-exporting moves the customers' route too.
    static void BuildCustomers(Transform root, LookdevData data)
    {
        CafeSceneBuilder.MakeCustomerPrefab();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/Customer.prefab");

        var mgr = Object.FindAnyObjectByType<OrderManager>();
        if (mgr != null) mgr.CustomersOrder = true;

        var c = New("Customers", root);
        Transform door = null, pickup = null;
        var queue = New("TillQueue", c).gameObject.AddComponent<CustomerQueue>();
        var spots = new List<Transform>();
        var seats = New("Seats", c);

        foreach (var p in data.points)
        {
            if (p.name == "DOOR_SPAWN") door = Point(p, c);
            else if (p.name == "PICKUP_01") pickup = Point(p, c);
            else if (p.name.StartsWith("QUEUE_")) spots.Add(Point(p, queue.transform));
            else if (p.name.StartsWith("SEAT_"))
            {
                // The point sits at seat height; the Seat lives on the floor below it.
                var t = Point(p, seats);
                float seatTop = t.position.y;
                t.position = new Vector3(t.position.x, 0f, t.position.z);
                var seat = t.gameObject.AddComponent<Seat>();
                // Chairs and stools are walked up to from behind; a sofa from in front.
                seat.ApproachOffset = new Vector3(0f, 0f, p.kind == "sofa" ? 0.6f : -0.6f);
                seat.SitDrop = CafeSceneBuilder.NpcHipY - seatTop;
            }
        }
        spots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        queue.Spots = spots.ToArray();

        var serve = Find("Pickup_Shelf");
        if (serve != null && serve.GetComponent<ServeWindow>() != null)
        {
            // Served cups wait on top of the pickup shelf, where customers can see them.
            var rest = New("CupRest", _anchors);
            rest.position = B(2.26f, 0.62f, 1.22f);
            rest.rotation = Quaternion.Euler(0f, 180f, 0f);
            serve.GetComponent<ServeWindow>().CupRest = rest;
        }

        var sp = c.gameObject.AddComponent<CustomerSpawner>();
        sp.Prefab = prefab != null ? prefab.GetComponent<Customer>() : null;
        sp.Door = door;
        sp.Pickup = pickup;
        sp.Queue = queue;
        var outfits = new List<Material>();
        foreach (var k in CafeSceneBuilder.Outfits) outfits.Add(CafeSceneBuilder.M(k));
        sp.Outfits = outfits.ToArray();
        sp.MaxInside = seats.childCount + queue.Spots.Length;
        if (door == null || pickup == null || spots.Count == 0)
            Debug.LogError("Lookdev: customer points missing from the export (DOOR_SPAWN, PICKUP_01, QUEUE_xx).");
    }

    static Transform Point(LookdevPoint p, Transform parent)
    {
        var t = New(p.name, parent);
        t.position = p.Position;
        t.rotation = p.Rotation;
        return t;
    }

    /// Walkable floor from the colliders, saved beside this scene (not the original cafe's).
    static void BakeNavMesh(Transform root, GameObject player)
    {
        var nav = New("Navigation", root).gameObject.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        nav.collectObjects = Unity.AI.Navigation.CollectObjects.All;
        nav.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;

        bool wasActive = player.activeSelf;
        player.SetActive(false);
        nav.BuildNavMesh();
        player.SetActive(wasActive);

        const string path = "Assets/Scenes/CozyCafe_Lookdev_NavMesh.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(nav.navMeshData, path);
    }

    /// The shop computer goes in the empty front-counter slot, screen towards the barista (+Z).
    static void BuildComputer(Transform root, LookdevData data)
    {
        foreach (var s in data.slots)
        {
            if (s.id != "reserved_A") continue;
            CafeSceneBuilder.BuildComputer(root, new Vector3(s.centre[0], s.centre[1], s.centre[2]), 0f);
            return;
        }
        Debug.LogWarning("Lookdev: no reserved_A slot, so no shop computer was placed");
    }

    static void AddToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == ScenePath)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
