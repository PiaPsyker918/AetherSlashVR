using System.Collections.Generic;
using UnityEngine;

public static class MeshSlicer
{
    private const float CapPointEpsilon = 0.0001f;
    private static readonly List<Vector3> capPoints = new List<Vector3>();

    public static SliceResult Slice(Mesh mesh, Transform objTransform, Plane plane, Material capMat, bool removeNegativeSide = false)
    {
        List<Vector3> pos = new List<Vector3>();
        List<int> posTri = new List<int>();
        List<Vector3> posNormals = new List<Vector3>();
        List<Vector2> posUVs = new List<Vector2>();

        List<Vector3> neg = new List<Vector3>();
        List<int> negTri = new List<int>();
        List<Vector3> negNormals = new List<Vector3>();
        List<Vector2> negUVs = new List<Vector2>();

        capPoints.Clear();

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        Matrix4x4 worldToLocal = objTransform.worldToLocalMatrix;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 w0 = objTransform.TransformPoint(vertices[triangles[i]]);
            Vector3 w1 = objTransform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 w2 = objTransform.TransformPoint(vertices[triangles[i + 2]]);

            Vector3 n0 = normals.Length > 0 ? normals[triangles[i]] : Vector3.zero;
            Vector3 n1 = normals.Length > 0 ? normals[triangles[i + 1]] : Vector3.zero;
            Vector3 n2 = normals.Length > 0 ? normals[triangles[i + 2]] : Vector3.zero;

            Vector2 u0 = uvs.Length > 0 ? uvs[triangles[i]] : Vector2.zero;
            Vector2 u1 = uvs.Length > 0 ? uvs[triangles[i + 1]] : Vector2.zero;
            Vector2 u2 = uvs.Length > 0 ? uvs[triangles[i + 2]] : Vector2.zero;

            bool s0 = plane.GetSide(w0);
            bool s1 = plane.GetSide(w1);
            bool s2 = plane.GetSide(w2);

            Vector3 l0 = worldToLocal.MultiplyPoint3x4(w0);
            Vector3 l1 = worldToLocal.MultiplyPoint3x4(w1);
            Vector3 l2 = worldToLocal.MultiplyPoint3x4(w2);

            if (s0 && s1 && s2)
            {
                AddTri(pos, posTri, posNormals, posUVs, l0, l1, l2, n0, n1, n2, u0, u1, u2);
            }
            else if (!s0 && !s1 && !s2)
            {
                AddTri(neg, negTri, negNormals, negUVs, l0, l1, l2, n0, n1, n2, u0, u1, u2);
            }
            else
            {
                SplitTriangle(
                    plane, s0, s1, s2,
                    w0, w1, w2,
                    n0, n1, n2,
                    u0, u1, u2,
                    pos, neg,
                    posTri, negTri,
                    posNormals, negNormals,
                    posUVs, negUVs,
                    worldToLocal);
            }
        }

        if (capPoints.Count >= 3)
        {
            Vector3 localCapNormal = worldToLocal.MultiplyVector(plane.normal).normalized;
            CreateCapGeometry(pos, posTri, posNormals, posUVs, localCapNormal);
            CreateCapGeometry(neg, negTri, negNormals, negUVs, -localCapNormal);
        }

        GameObject posObj = Create("Slice_POS", pos, posTri, posNormals, posUVs, capMat, plane.normal, objTransform);
        GameObject negObj = Create("Slice_NEG", neg, negTri, negNormals, negUVs, capMat, -plane.normal, objTransform);

        return new SliceResult { positiveObject = posObj, negativeObject = negObj };
    }

    private static void SplitTriangle(
        Plane plane,
        bool s0, bool s1, bool s2,
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 n0, Vector3 n1, Vector3 n2,
        Vector2 u0, Vector2 u1, Vector2 u2,
        List<Vector3> pos, List<Vector3> neg,
        List<int> posTri, List<int> negTri,
        List<Vector3> posNormals, List<Vector3> negNormals,
        List<Vector2> posUVs, List<Vector2> negUVs,
        Matrix4x4 worldToLocal)
    {
        List<int> sideAIndices = new List<int>();
        List<int> sideBIndices = new List<int>();

        if (s0) sideAIndices.Add(0); else sideBIndices.Add(0);
        if (s1) sideAIndices.Add(1); else sideBIndices.Add(1);
        if (s2) sideAIndices.Add(2); else sideBIndices.Add(2);

        Vector3[] verts = { v0, v1, v2 };
        Vector3[] norms = { n0, n1, n2 };
        Vector2[] uvCoords = { u0, u1, u2 };

        if (sideAIndices.Count == 1)
        {
            int aIndex = sideAIndices[0];
            int bIndex = sideBIndices[0];
            int cIndex = sideBIndices[1];

            Vector3 a = verts[aIndex];
            Vector3 b = verts[bIndex];
            Vector3 c = verts[cIndex];
            Vector3 na = norms[aIndex];
            Vector3 nb = norms[bIndex];
            Vector3 nc = norms[cIndex];
            Vector2 ua = uvCoords[aIndex];
            Vector2 ub = uvCoords[bIndex];
            Vector2 uc = uvCoords[cIndex];

            Vector3 i1 = Intersect(plane, a, b, out Vector3 ni1, out Vector2 ui1, na, nb, ua, ub);
            Vector3 i2 = Intersect(plane, a, c, out Vector3 ni2, out Vector2 ui2, na, nc, ua, uc);

            Vector3 localI1 = worldToLocal.MultiplyPoint3x4(i1);
            Vector3 localI2 = worldToLocal.MultiplyPoint3x4(i2);
            AddCapPoint(localI1);
            AddCapPoint(localI2);

            AddTri(pos, posTri, posNormals, posUVs,
                worldToLocal.MultiplyPoint3x4(a),
                localI1,
                localI2,
                na, ni1, ni2, ua, ui1, ui2);

            AddTri(neg, negTri, negNormals, negUVs,
                worldToLocal.MultiplyPoint3x4(b),
                worldToLocal.MultiplyPoint3x4(c),
                localI1,
                nb, nc, ni1, ub, uc, ui1);

            AddTri(neg, negTri, negNormals, negUVs,
                worldToLocal.MultiplyPoint3x4(c),
                localI2,
                localI1,
                nc, ni2, ni1, uc, ui2, ui1);
        }
        else if (sideAIndices.Count == 2)
        {
            int aIndex = sideAIndices[0];
            int bIndex = sideAIndices[1];
            int cIndex = sideBIndices[0];

            Vector3 a = verts[aIndex];
            Vector3 b = verts[bIndex];
            Vector3 c = verts[cIndex];
            Vector3 na = norms[aIndex];
            Vector3 nb = norms[bIndex];
            Vector3 nc = norms[cIndex];
            Vector2 ua = uvCoords[aIndex];
            Vector2 ub = uvCoords[bIndex];
            Vector2 uc = uvCoords[cIndex];

            Vector3 i1 = Intersect(plane, a, c, out Vector3 ni1, out Vector2 ui1, na, nc, ua, uc);
            Vector3 i2 = Intersect(plane, b, c, out Vector3 ni2, out Vector2 ui2, nb, nc, ub, uc);

            Vector3 localI1 = worldToLocal.MultiplyPoint3x4(i1);
            Vector3 localI2 = worldToLocal.MultiplyPoint3x4(i2);
            AddCapPoint(localI1);
            AddCapPoint(localI2);

            AddTri(pos, posTri, posNormals, posUVs,
                worldToLocal.MultiplyPoint3x4(a),
                worldToLocal.MultiplyPoint3x4(b),
                localI1,
                na, nb, ni1, ua, ub, ui1);

            AddTri(pos, posTri, posNormals, posUVs,
                worldToLocal.MultiplyPoint3x4(b),
                localI2,
                localI1,
                nb, ni2, ni1, ub, ui2, ui1);

            AddTri(neg, negTri, negNormals, negUVs,
                worldToLocal.MultiplyPoint3x4(c),
                localI1,
                localI2,
                nc, ni1, ni2, uc, ui1, ui2);
        }
    }

    private static Vector3 Intersect(
        Plane plane,
        Vector3 a,
        Vector3 b,
        out Vector3 normal,
        out Vector2 uv,
        Vector3 na,
        Vector3 nb,
        Vector2 ua,
        Vector2 ub)
    {
        Ray ray = new Ray(a, (b - a).normalized);
        plane.Raycast(ray, out float enter);
        float t = enter / (b - a).magnitude;
        t = Mathf.Clamp01(t);

        normal = Vector3.Lerp(na, nb, t).normalized;
        uv = Vector2.Lerp(ua, ub, t);
        return ray.GetPoint(enter);
    }

    private static void AddTri(
        List<Vector3> verts,
        List<int> tris,
        List<Vector3> normals,
        List<Vector2> uvs,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 na,
        Vector3 nb,
        Vector3 nc,
        Vector2 ua,
        Vector2 ub,
        Vector2 uc)
    {
        Vector3 faceNormal = Vector3.Cross(b - a, c - a);
        Vector3 targetNormal = (na + nb + nc) / 3f;
        if (targetNormal.sqrMagnitude > 0.0001f && Vector3.Dot(faceNormal, targetNormal) < 0f)
        {
            Vector3 swapVertex = b;
            b = c;
            c = swapVertex;

            Vector3 swapNormal = nb;
            nb = nc;
            nc = swapNormal;

            Vector2 swapUv = ub;
            ub = uc;
            uc = swapUv;
        }

        int startIndex = verts.Count;
        verts.Add(a);
        verts.Add(b);
        verts.Add(c);
        normals.Add(na.normalized);
        normals.Add(nb.normalized);
        normals.Add(nc.normalized);
        uvs.Add(ua);
        uvs.Add(ub);
        uvs.Add(uc);
        tris.Add(startIndex);
        tris.Add(startIndex + 1);
        tris.Add(startIndex + 2);
    }

    private static void AddCapPoint(Vector3 point)
    {
        for (int i = 0; i < capPoints.Count; i++)
        {
            if ((capPoints[i] - point).sqrMagnitude <= CapPointEpsilon * CapPointEpsilon)
            {
                return;
            }
        }

        capPoints.Add(point);
    }

    private static void CreateCapGeometry(List<Vector3> verts, List<int> tris, List<Vector3> normals, List<Vector2> uvs, Vector3 capNormal)
    {
        if (capPoints.Count < 3)
        {
            return;
        }

        Vector3 axisX = Vector3.Cross(capNormal, Vector3.up);
        if (axisX.sqrMagnitude < 0.001f)
        {
            axisX = Vector3.Cross(capNormal, Vector3.right);
        }

        axisX.Normalize();
        Vector3 axisY = Vector3.Cross(capNormal, axisX).normalized;

        List<Vector2> projected = new List<Vector2>(capPoints.Count);
        for (int i = 0; i < capPoints.Count; i++)
        {
            Vector3 point = capPoints[i];
            projected.Add(new Vector2(Vector3.Dot(point, axisX), Vector3.Dot(point, axisY)));
        }

        List<int> hull = BuildConvexHull(projected);
        if (hull.Count < 3)
        {
            return;
        }

        if (SignedArea(projected, hull) < 0f)
        {
            hull.Reverse();
        }

        int startIndex = verts.Count;
        Vector2 uvCenter = GetUvCenter(projected, hull);
        float uvScale = GetUvScale(projected, hull);

        for (int i = 0; i < hull.Count; i++)
        {
            Vector3 point = capPoints[hull[i]];
            verts.Add(point);
            normals.Add(capNormal);

            Vector2 uvPoint = projected[hull[i]] - uvCenter;
            uvs.Add(new Vector2(0.5f + uvPoint.x * uvScale, 0.5f + uvPoint.y * uvScale));
        }

        for (int i = 1; i < hull.Count - 1; i++)
        {
            int ia = startIndex;
            int ib = startIndex + i;
            int ic = startIndex + i + 1;

            Vector3 triNormal = Vector3.Cross(verts[ib] - verts[ia], verts[ic] - verts[ia]);
            if (Vector3.Dot(triNormal, capNormal) < 0f)
            {
                int swap = ib;
                ib = ic;
                ic = swap;
            }

            tris.Add(ia);
            tris.Add(ib);
            tris.Add(ic);
        }
    }

    private static List<int> BuildConvexHull(List<Vector2> points)
    {
        List<int> order = new List<int>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            order.Add(i);
        }

        order.Sort((a, b) =>
        {
            int compareX = points[a].x.CompareTo(points[b].x);
            return compareX != 0 ? compareX : points[a].y.CompareTo(points[b].y);
        });

        List<int> lower = new List<int>();
        for (int i = 0; i < order.Count; i++)
        {
            while (lower.Count >= 2 && Cross(points[lower[lower.Count - 2]], points[lower[lower.Count - 1]], points[order[i]]) <= 0f)
            {
                lower.RemoveAt(lower.Count - 1);
            }

            lower.Add(order[i]);
        }

        List<int> upper = new List<int>();
        for (int i = order.Count - 1; i >= 0; i--)
        {
            while (upper.Count >= 2 && Cross(points[upper[upper.Count - 2]], points[upper[upper.Count - 1]], points[order[i]]) <= 0f)
            {
                upper.RemoveAt(upper.Count - 1);
            }

            upper.Add(order[i]);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);

        return lower;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;
        return ab.x * ac.y - ab.y * ac.x;
    }

    private static float SignedArea(List<Vector2> points, List<int> indices)
    {
        float area = 0f;

        for (int i = 0; i < indices.Count; i++)
        {
            Vector2 current = points[indices[i]];
            Vector2 next = points[indices[(i + 1) % indices.Count]];
            area += current.x * next.y - next.x * current.y;
        }

        return area * 0.5f;
    }

    private static Vector2 GetUvCenter(List<Vector2> points, List<int> indices)
    {
        Vector2 center = Vector2.zero;

        for (int i = 0; i < indices.Count; i++)
        {
            center += points[indices[i]];
        }

        return center / indices.Count;
    }

    private static float GetUvScale(List<Vector2> points, List<int> indices)
    {
        float maxExtent = 0.0001f;

        for (int i = 0; i < indices.Count; i++)
        {
            Vector2 point = points[indices[i]];
            maxExtent = Mathf.Max(maxExtent, Mathf.Abs(point.x), Mathf.Abs(point.y));
        }

        return 0.5f / maxExtent;
    }

    private static GameObject Create(
        string name,
        List<Vector3> verts,
        List<int> tris,
        List<Vector3> normals,
        List<Vector2> uvs,
        Material mat,
        Vector3 dir,
        Transform originalTransform)
    {
        if (verts.Count == 0)
        {
            return null;
        }

        GameObject obj = new GameObject(name);
        obj.transform.parent = originalTransform.parent;
        obj.transform.SetPositionAndRotation(originalTransform.position, originalTransform.rotation);
        obj.transform.localScale = originalTransform.localScale;

        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
        Rigidbody rigidbody = obj.AddComponent<Rigidbody>();

        Mesh newMesh = new Mesh();
        newMesh.name = name + "_Mesh";
        newMesh.SetVertices(verts);
        newMesh.SetTriangles(tris, 0);

        if (normals.Count == verts.Count)
        {
            newMesh.SetNormals(normals);
        }
        else
        {
            newMesh.RecalculateNormals();
        }

        if (uvs.Count == verts.Count)
        {
            newMesh.SetUVs(0, uvs);
        }

        newMesh.RecalculateBounds();

        meshFilter.mesh = newMesh;
        meshRenderer.material = mat;

        meshCollider.convex = false;
        meshCollider.sharedMesh = newMesh;

        rigidbody.mass = 0.5f;
        rigidbody.constraints = RigidbodyConstraints.None;
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.AddForce(dir * 3f, ForceMode.Impulse);
        rigidbody.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

        return obj;
    }
}

public struct SliceResult
{
    public GameObject positiveObject;
    public GameObject negativeObject;
}
