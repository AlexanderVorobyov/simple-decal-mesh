using UnityEngine;
using UnityEditor;

namespace SimpleDecalMesh
{
    public static class EditorTools
    {
        public static void DrawPolygonalMesh(Mesh mesh, Transform transform, bool vertices = true, bool edges = true, bool triangleOrigins = true, bool triangleDirections = true, bool originCorners = false, bool edgeCenters = false, bool edgeNormals = false)
        {
            if (mesh == null) return;
            Polygon[] list = DecalMesh.CreatePolygons(mesh.vertices, mesh.triangles);
            DrawPolygonalMesh(list, transform, vertices, edges, triangleOrigins, triangleDirections, originCorners, edgeCenters, edgeNormals);
        }

        public static void DrawPolygonalMesh(Polygon[] polygons, Transform transform, bool vertices = true, bool edges = true, bool triangleOrigins = true, bool triangleDirections = true, bool originCorners = true, bool edgeCenters = false, bool edgeNormals = false)
        {
            for (int i = 0; i < polygons.Length; i++)
            {
                for (int v = 0; v < 3; v++)
                {
                    polygons[i].Vertices[v] = transform.TransformPoint(polygons[i].Vertices[v]);
                }
            }

            for (int i = 0; i < polygons.Length; i++)
            {
                if (edges)
                {
                    Handles.color = Color.red;
                    Handles.DrawLine(polygons[i].Vertices[0], polygons[i].Vertices[1]);
                    Handles.DrawLine(polygons[i].Vertices[1], polygons[i].Vertices[2]);
                    Handles.DrawLine(polygons[i].Vertices[2], polygons[i].Vertices[0]);
                }

                var edgeDir1 = Vector3.zero;
                var edgeDir2 = Vector3.zero;
                var edgeDir3 = Vector3.zero;
                var edge1 = Vector3.zero;
                var edge2 = Vector3.zero;
                var edge3 = Vector3.zero;

                if (edgeNormals || edgeCenters)
                {
                    edgeDir1 = polygons[i].Vertices[1] - polygons[i].Vertices[0];
                    edgeDir2 = polygons[i].Vertices[2] - polygons[i].Vertices[1];
                    edgeDir3 = polygons[i].Vertices[0] - polygons[i].Vertices[2];

                    edge1 = polygons[i].Vertices[0] + edgeDir1 * 0.5f;
                    edge2 = polygons[i].Vertices[1] + edgeDir2 * 0.5f;
                    edge3 = polygons[i].Vertices[2] + edgeDir3 * 0.5f;
                }

                if (edgeCenters)
                {
                    Handles.color = Color.cyan;
                    Handles.DotCap(0, edge1, Quaternion.identity, 0.01f);
                    Handles.DotCap(0, edge2, Quaternion.identity, 0.01f);
                    Handles.DotCap(0, edge3, Quaternion.identity, 0.01f);
                }

                if (edgeNormals)
                {
                    Handles.DrawLine(edge1, edge1 - GetDirectionNormal(edgeDir1) * 0.1f);
                    Handles.DrawLine(edge2, edge2 - GetDirectionNormal(edgeDir2) * 0.1f);
                    Handles.DrawLine(edge3, edge3 - GetDirectionNormal(edgeDir3) * 0.1f);
                }

                for (int v = 0; v < polygons[i].Vertices.Length; v++)
                {
                    var originDir = Vector3.zero;
                    if (originCorners)
                    {
                        originDir = polygons[i].Origin - polygons[i].Vertices[v];
                    }

                    Handles.color = Color.blue;
                    if (originCorners)
                    {
                        Handles.DrawLine(polygons[i].Vertices[v], polygons[i].Vertices[v] + originDir);
                    }

                    Handles.color = Color.yellow;
                    if (triangleOrigins)
                    {
                        Handles.DotCap(0, polygons[i].Origin, Quaternion.identity, 0.005f);
                    }

                    if (triangleDirections)
                    {
                        Handles.DrawLine(polygons[i].Origin, polygons[i].Origin + polygons[i].Direction * 0.25f);
                    }

                    Handles.color = Color.white;
                    if (vertices)
                    {
                        Handles.DotCap(0, polygons[i].Vertices[v], Quaternion.identity, 0.01f);
                    }
                }
            }
        }

        public static Vector3 GetDirectionNormal(Vector3 direction)
        {
            var angle = Vector3.zero;
            angle.y = 90;

            direction = Quaternion.Euler(angle) * direction;
            direction.Normalize();

            return direction;
        }
    }
}
