using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WaterMarchingSquares
{

    private readonly struct QKey : IEquatable<QKey>
    {
        private readonly int x;
        private readonly int y;

        public QKey(Vector2 v, float q)
        {
            this.x = Mathf.RoundToInt(v.x / q);
            this.y = Mathf.RoundToInt(v.y / q);
        }

        public bool Equals(QKey other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is QKey other && Equals(other);
        public override int GetHashCode() => (x * 73856093) ^ (y * 19349663);
    }

    public static Mesh BuildWaterMesh(int xSize, int zSize, bool[,] basinVertex, Func<int, int, float> getHeight, float waterLevel, float quantSize = 0.001f)
    {
        var segments = BuildSegments(xSize, zSize, basinVertex, getHeight, waterLevel);

        if (segments.Count < 3) return null;

        Debug.Log("Segment count: " + segments.Count);

        var loops = BuildLoops(segments, quantSize);
        if (loops.Count == 0) return null;

        Debug.Log("Loop count: " + loops.Count);

        var largestLoop = SelectLargestLoop(loops);
        if (largestLoop.Count < 3) return null;

        CleanupPolygon(largestLoop);
        if (largestLoop.Count < 3) return null;

        Debug.Log("Largest loop vert count: " + largestLoop.Count);

        if (SignedArea(largestLoop) < 0f) largestLoop.Reverse();

        if (!EarClipTriangulate(largestLoop, out List<int> indices)) return null;

        Debug.Log("Index count: " + indices.Count);

        var mesh = new Mesh();
        var verts3 = new Vector3[largestLoop.Count];
        var uvs = new Vector2[largestLoop.Count];

        for (int i = 0; i < largestLoop.Count; i++)
        {
            verts3[i] = new Vector3(largestLoop[i].x, waterLevel, largestLoop[i].y);
            uvs[i] = largestLoop[i] * 0.1f;
        }

        mesh.vertices = verts3;
        mesh.triangles = indices.ToArray();
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        var normals = mesh.normals;
        Vector3 avg = Vector3.zero;
        for (int i = 0; i < normals.Length; i++) avg += normals[i];

        if (avg.y < 0f)
        {
            int[] tri = mesh.triangles;
            for (int i = 0; i < tri.Length; i += 3)
            {
                (tri[i + 1], tri[i + 2]) = (tri[i + 2], tri[i + 1]);
            }
            mesh.triangles = tri;
            mesh.RecalculateNormals();
        }
        mesh.RecalculateBounds();
        return mesh;
    }

    public struct Segment
    {
        public Vector2 a;
        public Vector2 b;
        public Segment(Vector2 a, Vector2 b) { this.a = a; this.b = b; }
    }

    private static List<Segment> BuildSegments(int xSize, int zSize, bool[,] basinVertex, Func<int, int, float> getHeight, float waterLevel)
    {
        List <Segment> segments = new List<Segment>(xSize * zSize);

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                bool i0 = basinVertex[x, z];
                bool i1 = basinVertex[x + 1, z];
                bool i2 = basinVertex[x + 1, z + 1];
                bool i3 = basinVertex[x, z + 1];

                int mask = (i0 ? 1 : 0) | (i1 ? 2 : 0) | (i2 ? 4 : 0) | (i3 ? 8 : 0);
                if (mask == 0 || mask == 15)
                    continue;

                Vector2 p0 = new Vector2(x, z);
                Vector2 p1 = new Vector2(x + 1, z);
                Vector2 p2 = new Vector2(x + 1, z + 1);
                Vector2 p3 = new Vector2(x, z + 1);

                float h0 = getHeight(x, z);
                float h1 = getHeight(x + 1, z);
                float h2 = getHeight(x + 1, z + 1);
                float h3 = getHeight(x, z + 1);

                Vector2 e0 = default; bool hasE0 = false;
                Vector2 e1 = default; bool hasE1 = false;
                Vector2 e2 = default; bool hasE2 = false;
                Vector2 e3 = default; bool hasE3 = false;

                Vector2 EdgeInterp(Vector2 a, Vector2 b, float ha, float hb)
                {
                    float denom = hb - ha;
                    float t = (denom == 0f) ? 0.5f : (waterLevel - ha) / denom;
                    t = Mathf.Clamp01(t);
                    return Vector2.Lerp(a, b, t);
                }

                Vector2 GetE0() { if (!hasE0) { e0 = EdgeInterp(p0, p1, h0, h1); hasE0 = true; } return e0; };
                Vector2 GetE1() { if (!hasE1) { e1 = EdgeInterp(p1, p2, h1, h2); hasE1 = true; } return e1; };
                Vector2 GetE2() { if (!hasE2) { e2 = EdgeInterp(p2, p3, h2, h3); hasE2 = true; } return e2; };
                Vector2 GetE3() { if (!hasE3) { e3 = EdgeInterp(p3, p0, h3, h0); hasE3 = true; } return e3; };

                void AddSegment(Vector2 a, Vector2 b)
                {
                    if ((a - b).sqrMagnitude < 1e-10f) return;
                    segments.Add(new Segment(a, b));
                }

                switch (mask)
                {
                    case 1: AddSegment(GetE3(), GetE0()); break;
                    case 2: AddSegment(GetE0(), GetE1()); break;
                    case 3: AddSegment(GetE3(), GetE1()); break;
                    case 4: AddSegment(GetE1(), GetE2()); break;
                    case 5:
                        {
                            float hCenter = 0.25f * (h0 + h1 + h2 + h3);

                            if (hCenter <= waterLevel)
                            {
                                AddSegment(GetE3(), GetE2());
                                AddSegment(GetE0(), GetE1());
                            }
                            else
                            {
                                AddSegment(GetE3(), GetE0());
                                AddSegment(GetE1(), GetE2());
                            }
                            break;
                        }
                    case 6: AddSegment(GetE0(), GetE2()); break;
                    case 7: AddSegment(GetE3(), GetE2()); break;
                    case 8: AddSegment(GetE2(), GetE3()); break;
                    case 9: AddSegment(GetE0(), GetE2()); break;
                    case 10:
                        {
                            float hCenter = 0.25f * (h0 + h1 + h2 + h3);
                            if (hCenter <= waterLevel)
                            {
                                AddSegment(GetE0(), GetE3());
                                AddSegment(GetE1(), GetE2());
                            }
                            else
                            {
                                AddSegment(GetE0(), GetE1());
                                AddSegment(GetE2(), GetE3());
                            }
                            break;
                        }
                    case 11: AddSegment(GetE1(), GetE2()); break;
                    case 12: AddSegment(GetE1(), GetE3()); break;
                    case 13: AddSegment(GetE0(), GetE1()); break;
                    case 14: AddSegment(GetE3(), GetE0()); break;
                }
            }
        }
        return segments;
    }

    private static List<List<Vector2>> BuildLoops(List<Segment> segments, float quantize)
    {
        Dictionary<QKey, List<QKey>> adj = new();
        Dictionary<QKey, Vector2> actual = new();

        void AddEdge(Vector2 a, Vector2 b)
        {
            var ka = new QKey(a, quantize);
            var kb = new QKey(b, quantize);

            if (ka.Equals(kb)) return; // skip degenerate after quantization

            if (!adj.ContainsKey(ka)) { adj[ka] = new List<QKey>(2); actual[ka] = a; }
            if (!adj.ContainsKey(kb)) { adj[kb] = new List<QKey>(2); actual[kb] = b; }

            adj[ka].Add(kb);
            adj[kb].Add(ka);
        }

        for (int i = 0; i < segments.Count; i++)
            AddEdge(segments[i].a, segments[i].b);

        var loops = new List<List<Vector2>>();
        var used = new HashSet<(QKey, QKey)>();

        bool IsUsed(QKey a, QKey b) => used.Contains((a, b)) || used.Contains((b, a));
        void MarkUsed(QKey a, QKey b) => used.Add((a, b));

        foreach (var kv in adj)
        {
            QKey start = kv.Key;
            var neigh = kv.Value;

            for (int i = 0; i < neigh.Count; i++)
            {
                QKey next = neigh[i];
                if (IsUsed(start, next)) continue;

                var loop = new List<Vector2>();
                loop.Add(actual[start]);

                QKey prev = start;
                QKey curr = next;
                MarkUsed(prev, curr);

                int safety = 0;
                while (safety++ < 50000)
                {
                    loop.Add(actual[curr]);

                    if (curr.Equals(start))
                        break;

                    var nbs = adj[curr];
                    QKey chosen = default;
                    bool found = false;

                    for (int j = 0; j < nbs.Count; j++)
                    {
                        var cand = nbs[j];
                        if (cand.Equals(prev)) continue;
                        if (IsUsed(curr, cand)) continue;

                        chosen = cand;
                        found = true;
                        break;
                    }

                    if (!found)
                        break;

                    MarkUsed(curr, chosen);
                    prev = curr;
                    curr = chosen;
                }

                bool closed = loop.Count >= 2 && (loop[loop.Count - 1] - loop[0]).sqrMagnitude < (quantize * quantize * 4f);

                if (closed)
                {
                    loop.RemoveAt(loop.Count - 1); // remove duplicate start
                }

                for (int k = loop.Count - 1; k > 0; k--)
                {
                    if ((loop[k] - loop[k - 1]).sqrMagnitude < 1e-10f)
                        loop.RemoveAt(k);
                }

                // Accept only if we have enough vertices AND it was actually closed
                if (closed && loop.Count >= 3)
                {
                    loops.Add(loop);
                }
            }
        }

        int deg1 = 0, deg2 = 0, degOther = 0;
        foreach (var kv in adj)
        {
            int d = kv.Value.Count;
            if (d == 1) deg1++;
            else if (d == 2) deg2++;
            else degOther++;
        }
        Debug.Log($"Water contour degrees: deg1={deg1}, deg2={deg2}, other={degOther}, nodes={adj.Count}");

        return loops;
    }

    private static List<Vector2> SelectLargestLoop(List<List<Vector2>> loops)
    {
        float bestArea = -1f;
        List<Vector2> best = loops[0];

        for (int i = 0; i < loops.Count; i++)
        {
            float a = Mathf.Abs(SignedArea(loops[i]));
            if (a > bestArea)
            {
                bestArea = a;
                best = loops[i];
            }
        }
        return best;
    }

    private static void CleanupPolygon(List<Vector2> poly, float eps = 1e-4f)
    {
        for (int i = poly.Count - 1; i >= 0; i--)
        {
            int j = (i - 1 + poly.Count) % poly.Count;
            if ((poly[i] - poly[j]).sqrMagnitude < eps * eps)
            {
                poly.RemoveAt(i);
            }
        }

        for (int i = poly.Count - 1; i >= 0 && poly.Count >= 3; i--)
        {
            int i0 = (i - 1 + poly.Count) % poly.Count;
            int i1 = i;
            int i2 = (i + 1) % poly.Count;

            Vector2 a = poly[i0], b = poly[i1], c = poly[i2];
            Vector2 ab = b - a;
            Vector2 bc = c - b;

            float cross = ab.x * bc.y - ab.y * bc.x;
            if (Mathf.Abs(cross) < eps)
            {
                poly.RemoveAt(i1);
            }
        }
    }

    private static float SignedArea(List<Vector2> poly)
    {
        float area = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            int j = (i + 1) % poly.Count;
            area += (poly[i].x * poly[j].y) - (poly[j].x * poly[i].y);
        }
        return area * 0.5f;
    }

    private static bool EarClipTriangulate(List<Vector2> poly, out List<int> indices)
    {
        indices = new List<int>();
        int n = poly.Count;
        if (n < 3) return false;

        List<int> V = new List<int>(n);
        for (int i = 0; i < n; i++) V.Add(i);

        bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
        {
            return Cross(b - a, c - b) > 0f;
        }

        bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float c1 = Cross(b - a, p - a);
            float c2 = Cross(c - b, p - b);
            float c3 = Cross(a - c, p - c);
            return (c1 >= 0F && c2 >= 0F && c3 >= 0f);
        }

        int guard = 0;
        while (V.Count > 3 && guard++ < 100000)
        {
            bool clipped = false;

            for (int i = 0; i < V.Count; i++)
            {
                int i0 = V[(i - 1 + V.Count) % V.Count];
                int i1 = V[i];
                int i2 = V[(i + 1) % V.Count];

                Vector2 a = poly[i0];
                Vector2 b = poly[i1];
                Vector2 c = poly[i2];

                if (!IsConvex(a, b, c))
                    continue;

                bool anyInside = false;
                for (int j = 0; j < V.Count; j++)
                {
                    int ij = V[j];
                    if (ij == i0 || ij == i1 || ij == i2) continue;
                    if (PointInTri(poly[ij], a, b, c))
                    {
                        anyInside = true;
                        break;
                    }
                }
                if (anyInside) continue;

                indices.Add(i0);
                indices.Add(i1);
                indices.Add(i2);
                V.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)
                return false;
        }

        if (V.Count == 3)
        {
            indices.Add(V[0]);
            indices.Add(V[1]);
            indices.Add(V[2]);
            return true;
        }

        return false;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

}
