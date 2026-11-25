using NUnit.Framework;
using UnityEngine;

/// Example of a plain unit test against the Cafe runtime assembly: no scene, no editor state.
public class CafeMeshTests
{
    [Test]
    public void RoundedBox_MatchesRequestedSize()
    {
        var size = new Vector3(0.44f, 0.055f, 0.44f);
        var mesh = CafeMesh.RoundedBox(size, 0.022f, 7);
        try
        {
            Assert.That(Vector3.Distance(mesh.bounds.size, size), Is.LessThan(0.001f));
            Assert.That(mesh.bounds.center.magnitude, Is.LessThan(0.001f));
        }
        finally { Object.DestroyImmediate(mesh); }
    }

    [Test]
    public void RoundedBox_BaseAtOrigin_SitsOnFloor()
    {
        var mesh = CafeMesh.RoundedBox(Vector3.one, 0.1f, 4, "Box", baseAtOrigin: true);
        try { Assert.That(mesh.bounds.min.y, Is.EqualTo(0f).Within(0.001f)); }
        finally { Object.DestroyImmediate(mesh); }
    }
}
