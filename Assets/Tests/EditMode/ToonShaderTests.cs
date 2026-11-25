using NUnit.Framework;
using UnityEngine;

/// Guards the baked-lighting support in the toon shader: without a Meta pass the
/// lightmapper bakes every surface grey, and without the lightmap keywords baked
/// scenes render with no baked light at all.
public class ToonShaderTests
{
    static Shader Toon()
    {
        var s = Shader.Find("Cafe/Toon");
        Assert.IsNotNull(s, "Cafe/Toon shader missing");
        return s;
    }

    [Test]
    public void HasMetaPassForTheLightmapper()
    {
        var m = new Material(Toon());
        try
        {
            Assert.GreaterOrEqual(m.FindPass("Meta"), 0,
                "Cafe/Toon needs a Meta pass so baked bounce light keeps each surface's colour.");
        }
        finally { Object.DestroyImmediate(m); }
    }

    [Test]
    public void SupportsLightmapsAndShadowmask()
    {
        var s = Toon();
        var names = new System.Collections.Generic.List<string>();
        foreach (var k in s.keywordSpace.keywordNames) names.Add(k);
        CollectionAssert.Contains(names, "LIGHTMAP_ON");
        CollectionAssert.Contains(names, "SHADOWS_SHADOWMASK");
    }

    [Test]
    public void BakedStrengthDefaultsBelowOne()
    {
        var s = Toon();
        int i = s.FindPropertyIndex("_BakedStrength");
        Assert.GreaterOrEqual(i, 0, "_BakedStrength property missing");
        Assert.That(s.GetPropertyDefaultFloatValue(i), Is.InRange(0f, 1f));
    }
}
