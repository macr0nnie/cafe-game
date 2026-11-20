using NUnit.Framework;
using UnityEngine;

public class AimAssistTests
{
    static readonly Vector3 O = Vector3.zero, F = Vector3.forward;

    [Test]
    public void DeadCentreScoresZero()
    {
        Assert.AreEqual(0f, AimAssist.Score(O, F, new Vector3(0, 0, 2), 2.5f, 12f, false, 4f), 1e-3f);
    }

    [Test]
    public void SlightlyOffCentreIsStillPicked()
    {
        // ~8 degrees off: a raycast misses, the cone should not.
        float s = AimAssist.Score(O, F, new Vector3(0.28f, 0, 2), 2.5f, 12f, false, 4f);
        Assert.That(s, Is.EqualTo(8f).Within(0.5f));
    }

    [Test]
    public void OutsideConeOrRangeIsRejected()
    {
        Assert.IsTrue(float.IsPositiveInfinity(AimAssist.Score(O, F, new Vector3(1, 0, 1), 2.5f, 12f, false, 4f)));
        Assert.IsTrue(float.IsPositiveInfinity(AimAssist.Score(O, F, new Vector3(0, 0, 3), 2.5f, 12f, false, 4f)));
        Assert.IsTrue(float.IsPositiveInfinity(AimAssist.Score(O, F, new Vector3(0, 0, -1), 2.5f, 12f, false, 4f)));
    }

    [Test]
    public void CurrentFocusIsStickySoItDoesNotFlicker()
    {
        var a = new Vector3(0.20f, 0, 2); // ~5.7 deg, current
        var b = new Vector3(0.14f, 0, 2); // ~4.0 deg, slightly better newcomer
        Assert.Less(AimAssist.Score(O, F, a, 2.5f, 12f, true, 4f), AimAssist.Score(O, F, b, 2.5f, 12f, false, 4f));
        // Sticky only stretches the cone for the current target by that much.
        Assert.IsFalse(float.IsPositiveInfinity(AimAssist.Score(O, F, new Vector3(0.5f, 0, 2), 2.5f, 12f, true, 4f)));
    }

    [Test]
    public void PointOnRayClampsToRange()
    {
        Assert.AreEqual(new Vector3(0, 0, 1), AimAssist.PointOnRay(O, F, new Vector3(3, 0, 1), 2.5f));
        Assert.AreEqual(Vector3.zero, AimAssist.PointOnRay(O, F, new Vector3(0, 0, -2), 2.5f));
        Assert.AreEqual(new Vector3(0, 0, 2.5f), AimAssist.PointOnRay(O, F, new Vector3(0, 0, 9), 2.5f));
    }
}
