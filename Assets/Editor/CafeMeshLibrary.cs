using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// Generates every procedural mesh once and saves it as an asset, so the scene
/// keeps working after a reload. Meshes created at edit time and left unsaved
/// vanish, which would leave the scene full of null filters.
public static class CafeMeshLibrary
{
    const string Dir = "Assets/Art/Meshes";
    static Dictionary<string, Mesh> _cache;

    public static Mesh Get(string key)
    {
        if (_cache == null) Generate();
        Mesh m;
        return _cache.TryGetValue(key, out m) ? m : null;
    }

    static Mesh Save(Mesh m, string key)
    {
        string path = Dir + "/" + key + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(m, existing);
            Object.DestroyImmediate(m);
            _cache[key] = existing;
            return existing;
        }
        m.name = key;
        AssetDatabase.CreateAsset(m, path);
        _cache[key] = m;
        return m;
    }

    static Vector2 V(float x, float y) { return new Vector2(x, y); }

    [MenuItem("Cafe/Regenerate Meshes", false, 60)]
    public static void Generate()
    {
        Directory.CreateDirectory(Dir);
        _cache = new Dictionary<string, Mesh>();

        // ---------------------------------------------------------- tables
        // Bullnose edge: the rounded rim is what stops a table reading as a disc.
        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.520f,0.000f), V(0.548f,0.010f),
            V(0.552f,0.025f), V(0.546f,0.042f), V(0.520f,0.052f), V(0.000f,0.052f)
        }, 3), 48, false, false, "TableTopLarge"), "TableTopLarge");

        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.368f,0.000f), V(0.396f,0.010f),
            V(0.400f,0.024f), V(0.394f,0.040f), V(0.368f,0.050f), V(0.000f,0.050f)
        }, 3), 40, false, false, "TableTopSmall"), "TableTopSmall");

        // Turned pedestal with a waist and a swell, like a real cafe table column.
        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.00f), V(0.078f,0.00f), V(0.072f,0.030f), V(0.040f,0.095f),
            V(0.052f,0.170f), V(0.036f,0.330f), V(0.042f,0.520f), V(0.058f,0.630f),
            V(0.072f,0.685f), V(0.000f,0.690f)
        }, 6), 32, false, false, "TablePedestal"), "TablePedestal");

        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.255f,0.000f), V(0.248f,0.014f),
            V(0.170f,0.032f), V(0.095f,0.048f), V(0.000f,0.050f)
        }, 5), 40, false, false, "TableBase"), "TableBase");

        // ---------------------------------------------------------- chair
        Save(CafeMesh.RoundedBox(new Vector3(0.44f, 0.055f, 0.44f), 0.022f, 7, "ChairSeat"), "ChairSeat");
        Save(CafeMesh.RoundedBox(new Vector3(0.42f, 0.40f, 0.045f), 0.020f, 7, "ChairBack"), "ChairBack");
        Save(CafeMesh.RoundedBox(new Vector3(0.40f, 0.035f, 0.035f), 0.016f, 6, "ChairRail"), "ChairRail");

        // Turned leg: taper, ankle, swell. Four of these per chair, eight chairs.
        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.0235f,0.000f), V(0.0215f,0.022f), V(0.0150f,0.070f),
            V(0.0205f,0.135f), V(0.0175f,0.290f), V(0.0195f,0.395f), V(0.0150f,0.450f),
            V(0.000f,0.450f)
        }, 5), 16, false, false, "ChairLeg"), "ChairLeg");

        // ---------------------------------------------------------- counter and casework
        Save(CafeMesh.RoundedBox(new Vector3(4.20f, 1.10f, 0.70f), 0.030f, 5, "CounterBody"), "CounterBody");
        Save(CafeMesh.RoundedBox(new Vector3(4.34f, 0.062f, 0.84f), 0.030f, 6, "CounterTop"), "CounterTop");
        Save(CafeMesh.RoundedBox(new Vector3(0.74f, 1.10f, 0.62f), 0.030f, 5, "CounterReturn"), "CounterReturn");
        Save(CafeMesh.RoundedBox(new Vector3(0.86f, 0.062f, 0.70f), 0.030f, 6, "CounterReturnTop"), "CounterReturnTop");
        Save(CafeMesh.RoundedBox(new Vector3(6.40f, 0.90f, 0.42f), 0.026f, 5, "BackBarBody"), "BackBarBody");
        Save(CafeMesh.RoundedBox(new Vector3(6.52f, 0.055f, 0.48f), 0.026f, 6, "BackBarTop"), "BackBarTop");
        Save(CafeMesh.RoundedBox(new Vector3(2.60f, 0.045f, 0.28f), 0.018f, 5, "Shelf"), "Shelf");
        Save(CafeMesh.RoundedBox(new Vector3(0.46f, 0.065f, 2.60f), 0.026f, 5, "BenchSeat", true), "BenchSeat");
        Save(CafeMesh.RoundedBox(new Vector3(0.06f, 0.46f, 2.60f), 0.022f, 5, "BenchBack", true), "BenchBack");
        Save(CafeMesh.RoundedBox(new Vector3(0.40f, 0.45f, 2.50f), 0.020f, 4, "BenchPlinth", true), "BenchPlinth");
        Save(CafeMesh.RoundedBox(new Vector3(0.52f, 0.062f, 2.40f), 0.026f, 5, "BenchTable", true), "BenchTable");
        Save(CafeMesh.RoundedBox(new Vector3(1.40f, 0.90f, 0.045f), 0.018f, 5, "MenuBoard"), "MenuBoard");

        // ---------------------------------------------------------- machines
        Save(CafeMesh.RoundedBox(new Vector3(0.90f, 0.58f, 0.45f), 0.055f, 7, "MachineBody", true), "MachineBody");
        Save(CafeMesh.RoundedBox(new Vector3(0.70f, 0.13f, 0.12f), 0.045f, 6, "GroupHead"), "GroupHead");
        Save(CafeMesh.RoundedBox(new Vector3(0.22f, 0.34f, 0.26f), 0.035f, 6, "GrinderBody", true), "GrinderBody");
        Save(CafeMesh.RoundedBox(new Vector3(0.36f, 0.30f, 0.30f), 0.030f, 6, "TillBody", true), "TillBody");
        Save(CafeMesh.RoundedBox(new Vector3(1.22f, 0.50f, 0.62f), 0.030f, 6, "PastryCase", true), "PastryCase");
        Save(CafeMesh.RoundedBox(new Vector3(0.28f, 0.34f, 0.28f), 0.045f, 6, "DispenserBody", true), "DispenserBody");

        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.085f,0.000f), V(0.092f,0.020f),
            V(0.088f,0.140f), V(0.070f,0.160f), V(0.000f,0.160f)
        }, 4), 24, false, false, "Hopper"), "Hopper");

        Save(CafeMesh.Lathe(new[]{
            V(0.000f,0.000f), V(0.016f,0.000f), V(0.016f,0.120f), V(0.000f,0.120f)
        }, 12, false, false, "SteamWand"), "SteamWand");

        Save(CafeMesh.Lathe(new[]{
            V(0.000f,0.000f), V(0.018f,0.000f), V(0.014f,0.090f), V(0.000f,0.090f)
        }, 12, false, false, "Spout"), "Spout");

        // ---------------------------------------------------------- cups
        // Hollow: up the outside, over the lip, back down the inside.
        Save(CafeMesh.Lathe(new[]{
            V(0.0000f,0.0000f), V(0.0350f,0.0000f), V(0.0392f,0.0065f),
            V(0.0418f,0.0350f), V(0.0438f,0.0880f), V(0.0438f,0.0950f),
            V(0.0408f,0.0950f), V(0.0392f,0.0880f), V(0.0372f,0.0350f),
            V(0.0342f,0.0100f), V(0.0000f,0.0100f)
        }, 28, false, false, "MugBody"), "MugBody");

        Save(CafeMesh.Torus(0.030f, 0.0075f, 0.60f, 20, 10, "MugHandle"), "MugHandle");

        Save(CafeMesh.Lathe(new[]{
            V(0.000f,0.000f), V(0.0340f,0.000f), V(0.0340f,1.000f), V(0.000f,1.000f)
        }, 24, false, false, "LiquidColumn"), "LiquidColumn");

        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.034f,0.004f), V(0.030f,0.016f),
            V(0.022f,0.026f), V(0.012f,0.034f), V(0.000f,0.040f)
        }, 4), 20, true, false, "CreamSwirl"), "CreamSwirl");

        // Hangs from its origin and is scaled on Y to reach the cup.
        Save(CafeMesh.Lathe(new[]{
            V(0.0000f,-1.000f), V(0.0050f,-1.000f), V(0.0085f,0.000f), V(0.0000f,0.000f)
        }, 10, false, false, "PourStream"), "PourStream");

        Save(CafeMesh.RoundedBox(new Vector3(0.22f, 0.018f, 0.20f), 0.008f, 4, "DripTray", true), "DripTray");

        // Tall syrup bottle with a pump neck, for the shelf.
        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.034f,0.000f), V(0.038f,0.012f), V(0.040f,0.120f),
            V(0.030f,0.150f), V(0.013f,0.170f), V(0.012f,0.215f), V(0.000f,0.215f)
        }, 4), 18, false, true, "SyrupBottle"), "SyrupBottle");

        Save(CafeMesh.Blob(0.0085f, 3, 0.04f, 8, 10, 1f, "BobaPearl"), "BobaPearl");
        Save(CafeMesh.RoundedBox(new Vector3(0.024f, 0.024f, 0.024f), 0.006f, 4, "IceCube"), "IceCube");

        // ---------------------------------------------------------- plants
        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.142f,0.000f), V(0.150f,0.016f), V(0.176f,0.300f),
            V(0.196f,0.392f), V(0.200f,0.420f), V(0.188f,0.420f), V(0.184f,0.396f),
            V(0.164f,0.306f), V(0.138f,0.022f), V(0.000f,0.022f)
        }, 4), 32, false, false, "PlantPot"), "PlantPot");

        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.000f), V(0.062f,0.000f), V(0.066f,0.010f), V(0.076f,0.090f),
            V(0.084f,0.115f), V(0.086f,0.124f), V(0.078f,0.124f), V(0.074f,0.112f),
            V(0.064f,0.092f), V(0.058f,0.014f), V(0.000f,0.014f)
        }, 4), 24, false, false, "SmallPot"), "SmallPot");

        for (int i = 0; i < 4; i++)
            Save(CafeMesh.Blob(0.30f, i * 7 + 1, 0.26f, 16, 20, 0.86f, "Foliage" + i), "Foliage" + i);

        // ---------------------------------------------------------- lighting and dressing
        Save(CafeMesh.Lathe(CafeMesh.Smooth(new[]{
            V(0.000f,0.200f), V(0.052f,0.198f), V(0.122f,0.178f), V(0.176f,0.118f),
            V(0.200f,0.028f), V(0.202f,0.000f), V(0.195f,0.000f), V(0.193f,0.030f),
            V(0.169f,0.116f), V(0.116f,0.174f), V(0.048f,0.193f), V(0.000f,0.195f)
        }, 4), 32, false, false, "PendantShade"), "PendantShade");

        Save(CafeMesh.Blob(0.052f, 2, 0.02f, 10, 12, 1.15f, "Bulb"), "Bulb");
        Save(CafeMesh.RoundedBox(new Vector3(0.145f, 0.195f, 0.004f), 0.007f, 5, "PaperTicket"), "PaperTicket");
        Save(CafeMesh.RoundedBox(new Vector3(0.016f, 0.040f, 0.012f), 0.005f, 4, "Peg"), "Peg");
        Save(CafeMesh.Blob(0.065f, 5, 0.18f, 10, 12, 0.72f, "Pastry"), "Pastry");
        Save(CafeMesh.RoundedBox(new Vector3(1.00f, 0.02f, 0.62f), 0.010f, 4, "Doormat", true), "Doormat");
        Save(CafeMesh.RoundedBox(new Vector3(0.62f, 0.022f, 0.42f), 0.010f, 4, "Tray", true), "Tray");

        AssetDatabase.SaveAssets();
        Debug.Log("Cafe: generated " + _cache.Count + " meshes into " + Dir);
    }
}
