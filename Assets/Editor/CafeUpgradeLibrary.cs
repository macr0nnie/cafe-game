using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// Writes the shop's upgrades as ScriptableObject assets, the same way the mesh
/// library writes meshes. Edit the table here, not the assets: each build rewrites
/// them (keeping their GUIDs, so scene references survive).
public static class CafeUpgradeLibrary
{
    public const string Dir = "Assets/Art/Upgrades";

    public struct Spec
    {
        public string Id, Title, Description, Requires;
        public int Cost;
        public UpgradeTarget Target;
        public float Ease;

        public Spec(string id, string title, string description, UpgradeTarget target, int cost, float ease, string requires = null)
        { Id = id; Title = title; Description = description; Target = target; Cost = cost; Ease = ease; Requires = requires; }
    }

    // Tips run about 5 to 17 coins a drink, so a first upgrade is a few drinks and a second is most of a day.
    // Two tiers per station reach ShopProgress.MaxEase.
    public static readonly Spec[] Specs =
    {
        new Spec("grinder_1",  "Sharper burrs",       "Fresh burrs bite through beans faster.",      UpgradeTarget.Grinder,   40, 0.4f),
        new Spec("grinder_2",  "Electric motor",      "The grinder keeps spinning between taps.",    UpgradeTarget.Grinder,  120, 0.4f, "grinder_1"),
        new Spec("espresso_1", "Pressure gauge",      "Watch the needle and time the shot.",         UpgradeTarget.Espresso,  50, 0.4f),
        new Spec("espresso_2", "Pre-infusion",        "A gentler start that forgives a late stop.",  UpgradeTarget.Espresso, 140, 0.4f, "espresso_1"),
        new Spec("steam_1",    "Jug thermometer",     "A clip-on dial shows when milk is ready.",    UpgradeTarget.SteamWand, 50, 0.4f),
        new Spec("steam_2",    "Four-hole steam tip", "Silky milk over a longer stretch.",           UpgradeTarget.SteamWand,140, 0.4f, "steam_1"),
        new Spec("pour_1",     "Marked jugs",         "Fill lines on every jug and kettle.",         UpgradeTarget.Pour,      40, 0.4f),
        new Spec("pour_2",     "Measured spouts",     "Slower spouts make the line easy to hit.",    UpgradeTarget.Pour,     120, 0.4f, "pour_1"),
    };

    public static UpgradeCatalog Generate()
    {
        Directory.CreateDirectory(Dir);
        var made = new Dictionary<string, UpgradeDef>();
        var list = new List<UpgradeDef>();

        foreach (var s in Specs)    // prerequisites come first in the table
        {
            var u = LoadOrCreate<UpgradeDef>(Dir + "/" + s.Id + ".asset");
            u.Id = s.Id;
            u.Title = s.Title;
            u.Description = s.Description;
            u.Cost = s.Cost;
            u.Target = s.Target;
            u.Ease = s.Ease;
            u.Requires = s.Requires != null ? made[s.Requires] : null;
            EditorUtility.SetDirty(u);
            made[s.Id] = u;
            list.Add(u);
        }

        var cat = LoadOrCreate<UpgradeCatalog>(Dir + "/UpgradeCatalog.asset");
        cat.All = list.ToArray();
        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
        return cat;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        a = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(a, path);
        return a;
    }
}
