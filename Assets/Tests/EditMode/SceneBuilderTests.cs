using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Runs Cafe > 1. Build Cafe Scene and checks what it produced.
/// The build rewrites the scene and generated art, so both folders are
/// snapshotted first and put back afterwards: running tests never leaves a diff.
public class SceneBuilderTests
{
    static readonly string[] Touched = { "Assets/Scenes", "Assets/Art" };
    const string MaterialDir = "Assets/Art/Materials";

    // Locked palette from the art direction doc: 18 colours.
    static readonly string[] Locked =
    {
        "#CDEEE3", "#FBE0EA", "#F7E1D7", "#E9D5F0", "#F6C6D0", "#C7F0E0", "#B7A6C9",
        "#FFF5E6", "#FFFAF2", "#D9A878", "#A8754C", "#E8E8EF", "#B5E3C0", "#8FC79E",
        "#FFD6A5", "#FFF3C4", "#F3D9B1", "#8E7F9A",
    };

    // Colours already in the builder that sit outside the locked 18. Adding to this
    // list is a deliberate art-direction decision, not a way to make the test pass.
    static readonly string[] KnownExceptions =
    {
        "#FFFFFF",                       // M_Floor: white carrier for the FloorA/FloorB checker texture
        "#FFF8EC", "#DCDCE6",            // Paper, Chrome
        "#C98B4B", "#F0A0B0", "#C9B8E8", // Caramel, Strawberry, Taro
        "#9BBF6A", "#8A5A44", "#F3E0B8", // Matcha, Chocolate, Vanilla
        "#E8F4F8", "#4A3A3F",            // Ice, Boba
        "#F2E6DA",                       // ceiling shadow tint
        "#FBF3E2", "#EFE4D4",            // window daylight card + its shadow
        "#8A5E3F", "#6B4430",            // coffee liquid + its shadow
    };

    string _backup;

    [OneTimeSetUp]
    public void BuildScene()
    {
        // In the editor this would ask for confirmation and replace the scene you have open.
        Assume.That(Application.isBatchMode, "Runs from tools/run-tests.ps1 or CI only.");

        _backup = Path.Combine(Path.GetTempPath(), "cafe-test-backup-" + System.Guid.NewGuid().ToString("N"));
        foreach (var dir in Touched) CopyDir(dir, Path.Combine(_backup, dir));

        CafeSceneBuilder.Build();
    }

    [OneTimeTearDown]
    public void Restore()
    {
        if (_backup == null) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (var dir in Touched)
        {
            Directory.Delete(dir, true);
            CopyDir(Path.Combine(_backup, dir), dir);
        }
        Directory.Delete(_backup, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    [Test]
    public void Build_SavesSceneWithGameplayRoots()
    {
        Assert.IsTrue(File.Exists("Assets/Scenes/CozyCafe.unity"));
        foreach (var root in new[] { "_Lighting", "Environment", "Stations", "_Gameplay" })
            Assert.IsNotNull(GameObject.Find(root), "Built scene is missing " + root);
    }

    [Test]
    public void Materials_UseOnlyPaletteColours()
    {
        var allowed = new List<Color>();
        foreach (var hex in Locked) allowed.Add(Hex(hex));
        foreach (var hex in KnownExceptions) allowed.Add(Hex(hex));

        var offenders = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialDir }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var prop in new[] { "_BaseColor", "_ShadowTint" })
            {
                if (!mat.HasProperty(prop)) continue;
                var c = mat.GetColor(prop);
                if (!allowed.Exists(a => Close(a, c)))
                    offenders.Add(mat.name + "." + prop + " #" + ColorUtility.ToHtmlStringRGB(c));
            }
        }
        Assert.IsEmpty(offenders,
            "Off-palette colours (replace one of the 18 rather than adding): " + string.Join(", ", offenders));
    }

    static bool Close(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.003f && Mathf.Abs(a.g - b.g) < 0.003f && Mathf.Abs(a.b - b.b) < 0.003f;

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
        foreach (var d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
    }
}
