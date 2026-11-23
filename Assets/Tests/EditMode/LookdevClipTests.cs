using NUnit.Framework;
using UnityEngine;

/// Plain unit tests for the Blender prop-clip sampling (no scene needed).
public class LookdevClipTests
{
    static LookdevClip Clip(bool loop)
    {
        return new LookdevClip
        {
            clip = "Door_Open", @object = "Door", loop = loop, length = 1f,
            keys = new[]
            {
                new LookdevKey { t = 0f, p = new[] { 0f, 0f, 0f }, r = new[] { 0f, 0f, 0f, 1f }, s = new[] { 1f, 1f, 1f } },
                new LookdevKey { t = 1f, p = new[] { 0f, 1f, 0f }, r = new[] { 0f, 0.7071068f, 0f, 0.7071068f }, s = new[] { 2f, 2f, 2f } },
            }
        };
    }

    [Test]
    public void SamplesInterpolateBetweenKeys()
    {
        var pose = LookdevPose.Sample(Clip(false), 0.5f);
        Assert.AreEqual(0.5f, pose.Position.y, 1e-4f);
        Assert.AreEqual(1.5f, pose.Scale.x, 1e-4f);
        Assert.AreEqual(45f, Quaternion.Angle(Quaternion.identity, pose.Rotation), 0.1f);
    }

    [Test]
    public void OneShotHoldsLastKeyAndLoopWraps()
    {
        Assert.AreEqual(1f, LookdevPose.Sample(Clip(false), 3.0f).Position.y, 1e-4f);
        Assert.AreEqual(0.25f, LookdevPose.Sample(Clip(true), 1.25f).Position.y, 1e-4f);
    }

    [Test]
    public void ApplyAddsDeltaOnTopOfRestPose()
    {
        var go = new GameObject("t");
        try
        {
            var pose = LookdevPose.Sample(Clip(false), 1f);
            pose.ApplyTo(go.transform, new Vector3(1f, 0f, 0f), Quaternion.identity, Vector3.one);
            Assert.AreEqual(new Vector3(1f, 1f, 0f), go.transform.localPosition);
            Assert.AreEqual(2f, go.transform.localScale.x, 1e-4f);
            LookdevPose.Rest.ApplyTo(go.transform, new Vector3(1f, 0f, 0f), Quaternion.identity, Vector3.one);
            Assert.AreEqual(new Vector3(1f, 0f, 0f), go.transform.localPosition);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ParsesExportedJsonShape()
    {
        const string json = "{\"fps\":30,\"clips\":[{\"clip\":\"Fridge_Door_Open\",\"object\":\"Fridge_Door\",\"loop\":false,"
                          + "\"length\":0.5,\"keys\":[{\"t\":0,\"p\":[0,0,0],\"r\":[0,0,0,1],\"s\":[1,1,1]}]}],"
                          + "\"tiers\":{\"Tier1_ice\":[\"T1_IceBin_Tub\"]},\"slots\":[{\"id\":\"ice\",\"centre\":[0.1,0.9,-2.6]}]}";
        var d = LookdevData.Parse(json);
        Assert.AreEqual(1, d.clips.Length);
        Assert.AreEqual("Fridge_Door", d.clips[0].@object);
        Assert.IsTrue(d.ById().ContainsKey(LookdevClip.Key("Fridge_Door", "Fridge_Door_Open")));
        Assert.AreEqual("ice", d.slots[0].id);
    }
}
