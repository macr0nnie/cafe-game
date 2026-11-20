using System.Collections.Generic;
using UnityEngine;

/// Procedural mesh builders. Unity's primitives are boxy; these give rounded,
/// turned and organic shapes so the furniture reads as real furniture.
public static class CafeMesh
{
    // ---------------------------------------------------------------- rounded box

    /// A box with rounded edges and corners. `seg` controls corner smoothness.
    /// Works by sampling the cube surface then pushing each sample out from an
    /// inset core by `radius`, which rounds edges and corners in one step.
    /// `baseAtOrigin` puts y=0 at the bottom face instead of the centre, so the
    /// mesh can be dropped straight onto a surface without a half-height offset.
    public static Mesh RoundedBox(Vector3 size, float radius, int seg = 6,
                                  string name = "RoundedBox", bool baseAtOrigin = false)
    {
        float minHalf = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f;
        radius = Mathf.Clamp(radius, 0.0005f, minHalf * 0.98f);
        seg = Mathf.Max(2, seg);

        Vector3 h = size * 0.5f;
        Vector3 inner = new Vector3(
            Mathf.Max(0f, h.x - radius),
            Mathf.Max(0f, h.y - radius),
            Mathf.Max(0f, h.z - radius));

        // n, u, v per face with u cross v == n, so winding stays outward.
        Vector3[] N = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        Vector3[] U = { new Vector3(0,0,-1), new Vector3(0,0,1), Vector3.right, Vector3.right, Vector3.right, Vector3.left };
        Vector3[] V = { Vector3.up, Vector3.up, new Vector3(0,0,-1), new Vector3(0,0,1), Vector3.up, Vector3.up };

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        for (int f = 0; f < 6; f++)
        {
            int start = verts.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * 2f - 1f;
                for (int j = 0; j <= seg; j++)
                {
                    float b = (float)j / seg * 2f - 1f;
                    Vector3 cube = N[f] + U[f] * a + V[f] * b;      // on the unit cube face
                    Vector3 p = Vector3.Scale(cube, h);

                    Vector3 core = new Vector3(
                        Mathf.Clamp(p.x, -inner.x, inner.x),
                        Mathf.Clamp(p.y, -inner.y, inner.y),
                        Mathf.Clamp(p.z, -inner.z, inner.z));

                    Vector3 d = p - core;
                    Vector3 nrm = d.sqrMagnitude > 1e-10f ? d.normalized : N[f];

                    verts.Add(core + nrm * radius);
                    norms.Add(nrm);
                    uvs.Add(new Vector2((a + 1f) * 0.5f, (b + 1f) * 0.5f));
                }
            }

            for (int i = 0; i < seg; i++)
            for (int j = 0; j < seg; j++)
            {
                int i0 = start + i * (seg + 1) + j;
                int i1 = start + i * (seg + 1) + j + 1;
                int i2 = start + (i + 1) * (seg + 1) + j + 1;
                int i3 = start + (i + 1) * (seg + 1) + j;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i0); tris.Add(i2); tris.Add(i3);
            }
        }

        if (baseAtOrigin)
        {
            float lift = size.y * 0.5f;
            for (int i = 0; i < verts.Count; i++) verts[i] += new Vector3(0f, lift, 0f);
        }

        return Build(name, verts, tris, norms, uvs);
    }

    // ---------------------------------------------------------------- lathe

    /// Revolve a 2D profile (x = radius, y = height) around the Y axis.
    /// Repeat a profile point to get a hard crease there; otherwise it smooths.
    /// Set capBottom/capTop when the profile does not already close the shape.
    public static Mesh Lathe(Vector2[] profile, int segments = 24, bool capBottom = false,
                             bool capTop = false, string name = "Lathe")
    {
        segments = Mathf.Max(3, segments);
        int n = profile.Length;

        var verts = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        for (int s = 0; s <= segments; s++)
        {
            float t = (float)s / segments;
            float ang = t * Mathf.PI * 2f;
            float c = Mathf.Cos(ang), si = Mathf.Sin(ang);
            for (int i = 0; i < n; i++)
            {
                verts.Add(new Vector3(profile[i].x * c, profile[i].y, profile[i].x * si));
                uvs.Add(new Vector2(t, (float)i / Mathf.Max(1, n - 1)));
            }
        }

        for (int s = 0; s < segments; s++)
        for (int i = 0; i < n - 1; i++)
        {
            int a = s * n + i;
            int b = s * n + i + 1;
            int c2 = (s + 1) * n + i + 1;
            int d = (s + 1) * n + i;
            tris.Add(a); tris.Add(b); tris.Add(c2);
            tris.Add(a); tris.Add(c2); tris.Add(d);
        }

        if (capBottom && profile[0].x > 1e-5f)
        {
            int centre = verts.Count;
            verts.Add(new Vector3(0f, profile[0].y, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int s = 0; s < segments; s++)
            {
                tris.Add(centre);
                tris.Add((s + 1) * n + 0);
                tris.Add(s * n + 0);
            }
        }

        if (capTop && profile[n - 1].x > 1e-5f)
        {
            int centre = verts.Count;
            verts.Add(new Vector3(0f, profile[n - 1].y, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int s = 0; s < segments; s++)
            {
                tris.Add(centre);
                tris.Add(s * n + (n - 1));
                tris.Add((s + 1) * n + (n - 1));
            }
        }

        return Build(name, verts, tris, null, uvs);
    }

    // ---------------------------------------------------------------- organic blob

    /// A lumpy sphere for foliage. Deterministic from `seed`.
    public static Mesh Blob(float radius, int seed = 0, float lumpiness = 0.22f,
                            int rings = 14, int sectors = 18, float squashY = 1f,
                            string name = "Blob")
    {
        var verts = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();
        float sd = seed * 1.7f + 0.3f;

        for (int r = 0; r <= rings; r++)
        {
            float v = (float)r / rings;
            float phi = v * Mathf.PI;
            for (int s = 0; s <= sectors; s++)
            {
                float u = (float)s / sectors;
                float theta = u * Mathf.PI * 2f;

                float wob =
                      Mathf.Sin(theta * 3f + sd) * Mathf.Cos(phi * 2.6f + sd * 1.4f) * 0.55f
                    + Mathf.Sin(theta * 5.4f + sd * 2.1f) * Mathf.Cos(phi * 4.3f + sd) * 0.30f
                    + Mathf.Sin(theta * 8.1f + sd * 0.7f) * Mathf.Cos(phi * 6.9f + sd * 1.9f) * 0.15f;

                float rr = radius * (1f + lumpiness * wob);
                float sp = Mathf.Sin(phi);
                verts.Add(new Vector3(
                    rr * sp * Mathf.Cos(theta),
                    rr * Mathf.Cos(phi) * squashY,
                    rr * sp * Mathf.Sin(theta)));
                uvs.Add(new Vector2(u, v));
            }
        }

        int row = sectors + 1;
        for (int r = 0; r < rings; r++)
        for (int s = 0; s < sectors; s++)
        {
            int a = r * row + s;
            int b = r * row + s + 1;
            int c = (r + 1) * row + s + 1;
            int d = (r + 1) * row + s;
            tris.Add(a); tris.Add(d); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(b);
        }

        return Build(name, verts, tris, null, uvs);
    }

    // ---------------------------------------------------------------- torus arc

    /// A torus, or an arc of one. Used for mug handles (arc ~0.62 of a turn).
    public static Mesh Torus(float majorRadius, float minorRadius, float arc = 1f,
                             int majorSeg = 20, int minorSeg = 10, string name = "Torus")
    {
        var verts = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        for (int i = 0; i <= majorSeg; i++)
        {
            float u = (float)i / majorSeg;
            float a = u * Mathf.PI * 2f * arc;
            Vector3 centre = new Vector3(Mathf.Cos(a) * majorRadius, Mathf.Sin(a) * majorRadius, 0f);
            Vector3 outward = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);

            for (int j = 0; j <= minorSeg; j++)
            {
                float v = (float)j / minorSeg;
                float b = v * Mathf.PI * 2f;
                verts.Add(centre + outward * (Mathf.Cos(b) * minorRadius) + Vector3.forward * (Mathf.Sin(b) * minorRadius));
                uvs.Add(new Vector2(u, v));
            }
        }

        int row = minorSeg + 1;
        for (int i = 0; i < majorSeg; i++)
        for (int j = 0; j < minorSeg; j++)
        {
            int a = i * row + j;
            int b = i * row + j + 1;
            int c = (i + 1) * row + j + 1;
            int d = (i + 1) * row + j;
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }

        return Build(name, verts, tris, null, uvs);
    }

    // ---------------------------------------------------------------- helpers

    /// Build a smooth profile from a few control points using a Catmull-Rom spline,
    /// so turned legs and cup walls curve instead of stepping.
    public static Vector2[] Smooth(Vector2[] control, int perSegment = 5)
    {
        if (control.Length < 3) return control;
        var outp = new List<Vector2>();
        for (int i = 0; i < control.Length - 1; i++)
        {
            Vector2 p0 = control[Mathf.Max(0, i - 1)];
            Vector2 p1 = control[i];
            Vector2 p2 = control[i + 1];
            Vector2 p3 = control[Mathf.Min(control.Length - 1, i + 2)];
            int steps = (i == control.Length - 2) ? perSegment : perSegment;
            for (int s = 0; s < steps; s++)
            {
                float t = (float)s / steps;
                outp.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        outp.Add(control[control.Length - 1]);
        return outp.ToArray();
    }

    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    static Mesh Build(string name, List<Vector3> verts, List<int> tris,
                      List<Vector3> norms, List<Vector2> uvs)
    {
        var m = new Mesh();
        m.name = name;
        if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        if (uvs != null) m.SetUVs(0, uvs);
        if (norms != null) m.SetNormals(norms);
        else m.RecalculateNormals();
        m.RecalculateBounds();
        m.RecalculateTangents();
        return m;
    }
}
