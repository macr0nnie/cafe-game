using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Guards the standing project rules that can be checked without building anything.
public class ProjectRulesTests
{
    const string ProfilePath = "Assets/Settings/CafeVolumeProfile.asset";

    [Test]
    public void Tonemapping_IsNeutral()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        Assert.IsNotNull(profile, ProfilePath + " is missing. Run Cafe > 2. Add Post-Processing Volume.");
        Assert.IsTrue(profile.TryGet<Tonemapping>(out var tone), "Volume profile has no Tonemapping override.");
        Assert.IsTrue(tone.active, "Tonemapping override is disabled.");
        Assert.IsTrue(tone.mode.overrideState, "Tonemapping mode is not overridden.");
        Assert.AreEqual(TonemappingMode.Neutral, tone.mode.value,
            "ACES and the other modes grey out the pastel palette; keep Neutral.");
    }

    [Test]
    public void MonoBehaviours_EachLiveInOwnFileNamedAfterClass()
    {
        // MonoScript.GetClass() only returns the class whose name matches the file,
        // so a MonoBehaviour with no matching script is either misnamed or sharing a file.
        var scripted = new HashSet<Type>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            var cls = script != null ? script.GetClass() : null;
            if (cls != null) scripted.Add(cls);
        }

        var offenders = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name == "Cafe" || a.GetName().Name == "Assembly-CSharp")
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && !t.IsGenericTypeDefinition && !scripted.Contains(t))
            .Select(t => t.FullName)
            .OrderBy(n => n)
            .ToList();

        Assert.IsEmpty(offenders,
            "Each MonoBehaviour needs its own .cs file named after the class: " + string.Join(", ", offenders));
    }
}
