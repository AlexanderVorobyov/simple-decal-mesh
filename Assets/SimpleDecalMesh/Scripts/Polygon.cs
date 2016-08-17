using UnityEngine;

namespace SimpleDecalMesh
{
    public struct Polygon
    {
        private Vector3[] m_vertices;

        public Vector3[] Vertices { get { return m_vertices; } }
        public Vector3 Origin { get { return (m_vertices[0] + m_vertices[1] + m_vertices[2]) / 3; } }
        public Vector3 Direction { get { return Vector3.Cross(m_vertices[1] - m_vertices[0], m_vertices[2] - m_vertices[0]).normalized; } }

        public Polygon(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            m_vertices = new[] { v0, v1, v2 };
        }

        public Polygon(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 direction)
        {
            Vector3 dir = GetDirection(v0, v1, v2);
            m_vertices = direction + dir == Vector3.zero ? new[] { v0, v1, v2 } : new[] { v2, v1, v0 };
        }
        
        public Polygon[] Cut(Plane plane)
        {
            Vector3[] vertices = new Vector3[4];
            for (int i = 0; i < 3; i++)
            {
                if (plane.GetSide(m_vertices[i]))
                {
                    for (int v = 0; v < 3; v++)
                    {
                        if (v == i || plane.GetSide(m_vertices[v])) continue;
                        Vector3 cut = CutEdge(plane, m_vertices[i], m_vertices[v]);

                        for (int j = 0; j < vertices.Length; j++)
                        {
                            if (vertices[j] == Vector3.zero)
                            {
                                vertices[j] = cut;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < vertices.Length; j++)
                    {
                        if (vertices[j] == Vector3.zero)
                        {
                            vertices[j] = m_vertices[i];
                            break;
                        }
                    }

                    if (vertices[3] != Vector3.zero) break;
                }
            }

            return DevideFace(vertices, Direction);
        }

        private static Vector3 CutEdge(Plane plane, Vector3 a, Vector3 b)
        {
            float dist;
            var ray = new Ray(a, b - a);
            plane.Raycast(ray, out dist);
            return ray.GetPoint(dist);
        }

        public static Polygon[] DevideFace(Vector3[] vertices, Vector3 direction)
        {
            if (vertices[0] == Vector3.zero) return new Polygon[0];

            Polygon[] polygons;
            Vector3 checkDir = GetDirection(vertices[0], vertices[1], vertices[2]);

            var polygon1 = checkDir + direction == Vector3.zero
                ? new Polygon(vertices[2], vertices[1], vertices[0])
                : new Polygon(vertices[0], vertices[1], vertices[2]);

            if (vertices[3] == Vector3.zero)
            {
                polygons = new[] { polygon1 };
                return polygons;
            }

            var vec0 = vertices[3];
            var vec1 = Vector3.zero;
            var vec2 = vec1;

            for (int i = 0; i < 3; i++)
            {
                Vector3 point = vec0 + (vertices[i] - vec0) * 0.999f;
                if (polygon1.IsInsidePolygon(point)) continue;
                if (vec1 == Vector3.zero)
                {
                    vec1 = vertices[i];
                }
                else
                {
                    vec2 = vertices[i];
                    break;
                }
            }

            if (vec2 == Vector3.zero)
            {
                polygons = new[] { polygon1 };
                return polygons;
            }

            checkDir = GetDirection(vec0, vec1, vec2);

            var polygon2 = checkDir + direction == Vector3.zero
                ? new Polygon(vec2, vec1, vec0)
                : new Polygon(vec0, vec1, vec2);

            polygons = new[] { polygon1, polygon2 };
            return polygons;
        }

        public bool IsInsidePolygon(Vector3 point)
        {
            return IsInTriangle(point, m_vertices[0], m_vertices[1], m_vertices[2]);
        }

        private static bool IsInTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            return SameSide(point, a, b, c) && SameSide(point, b, a, c) && SameSide(point, c, a, b);
        }

        private static bool SameSide(Vector3 p1, Vector3 p2, Vector3 a, Vector3 b)
        {
            var cp1 = Vector3.Cross(b - a, p1 - a);
            var cp2 = Vector3.Cross(b - a, p2 - a);
            return Vector3.Dot(cp1, cp2) >= 0;
        }

        private static Vector3 GetDirection(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            return Vector3.Cross(v1 - v0, v2 - v0).normalized;
        }
    }
}
