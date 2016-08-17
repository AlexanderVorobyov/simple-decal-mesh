using UnityEngine;
using System.Collections.Generic;

namespace SimpleDecalMesh
{
    public static class DecalMesh
    {
        public static Mesh Create(DecalVolume decal, float surfaceMaxAngle, float surfaceDistance, float smoothAngle)
        {
            var pols = new List<Polygon>();
            var transform = decal.transform;
            var bounds = decal.GetBounds();
            GameObject[] affectedList = decal.GetGameObjectsInBounds(decal.Layer);
            Terrain[] terrains = Terrain.activeTerrains;

            for (int i = 0, count = terrains.Length; i < count; i++)
            {
                if ((1 << terrains[i].gameObject.layer & decal.Layer) == 0) continue;

                Vector3 terrainPos = terrains[i].GetPosition();
                Vector3 terrainSize = terrains[i].terrainData.size;
                var terrainBounds = new Bounds(terrainPos + terrainSize * 0.5f, terrainSize);

                if (bounds.Intersects(terrainBounds) == false) continue;
                pols.AddRange(CreateFromTerrain(terrains[i], terrainPos, terrainSize, bounds, decal, surfaceMaxAngle));
            }

            pols.AddRange(CreateFromObjects(affectedList, decal, surfaceMaxAngle));
            
            Mesh mesh = CreateMesh(pols, true);

            var verts = mesh.vertices;
            for (int i = 0, count = verts.Length; i < count; i++)
            {
                verts[i] = transform.InverseTransformPoint(verts[i]);
            }
            mesh.vertices = verts;

            mesh.vertices = PushMesh(mesh, surfaceDistance);
            mesh.uv = CreateUVs(mesh.vertices, decal.Volume, decal.Sprite);
            mesh.name = "Decal Mesh";

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

        private static Vector3[] PushMesh(Mesh mesh, float distance)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            for (int i = 0, count = vertices.Length; i < count; i++)
            {
                vertices[i] += normals[i] * distance;
            }

            return vertices;
        }

        private static List<Polygon> CreateFromObjects(GameObject[] objects, TransformVolume volume, float surfaceMaxAngle)
        {
            var pols = new List<Polygon>();
            for (int i = 0, count = objects.Length; i < count; i++)
            {
                Mesh mesh = objects[i].GetComponent<MeshFilter>().sharedMesh;
                var verts = mesh.vertices;
                for (int v = 0, count2 = verts.Length; v < count2; v++)
                {
                    verts[v] = objects[i].transform.TransformPoint(verts[v]);
                }

                pols.AddRange(CreatePolygons(verts, mesh.triangles));
            }

            return CutPolygons(pols, volume, surfaceMaxAngle);
        }
        
        private static List<Polygon> CreateFromTerrain(Terrain terrain, Vector3 terrainPos, Vector3 terrainSize, Bounds bounds, TransformVolume volume, float surfaceMaxAngle)
        {
            Vector3 start = bounds.center - bounds.size * 0.5f;
            Vector3 end = bounds.center + bounds.size * 0.5f;
            float size = terrainSize.x / (terrain.terrainData.heightmapResolution - 1);
            var pols = TerrainPlane(terrainPos, size, start, end, terrainSize);

            for (int p = 0, count = pols.Count; p < count; p++)
            {
                for (int v = 0; v < 3; v++)
                {
                    Vector3 vert = pols[p].Vertices[v];
                    vert.y = terrainPos.y;
                    vert.y += terrain.SampleHeight(pols[p].Vertices[v]);
                    pols[p].Vertices[v] = vert;
                }
            }

            return CutPolygons(pols, volume, surfaceMaxAngle);
        }

        private static Mesh CreateMesh(List<Polygon> polygons, bool merge)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int i = 0, count = polygons.Count; i < count; i++)
            {
                for (int v = 0; v < 3; v++)
                {
                    if (merge)
                    {
                        if (vertices.Contains(polygons[i].Vertices[v]))
                        {
                            triangles.Add(vertices.IndexOf(polygons[i].Vertices[v]));
                        }
                        else
                        {
                            vertices.Add(polygons[i].Vertices[v]);
                            triangles.Add(vertices.Count - 1);
                        }
                    }
                    else
                    {
                        vertices.Add(polygons[i].Vertices[v]);
                        triangles.Add(vertices.Count - 1);
                    }
                }
            }
            
            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };

            mesh.RecalculateBounds();
            mesh.Optimize();
            mesh.RecalculateNormals();

            return mesh;
        }

        private static List<Polygon> CutPolygons(List<Polygon> polygons, TransformVolume volume, float surfaceMaxAngle)
        {
            Vector3 up = volume.transform.up;
            var pols = new List<Polygon>();

            for (int i = 0, count = polygons.Count; i < count; i++)
            {
                var polygon = polygons[i];
                if (!volume.IsInBounds(polygon.Vertices) || !IsAngle(surfaceMaxAngle, up, polygon.Direction)) continue;
                if (!volume.IsOnBorder(polygon.Vertices))
                {
                    pols.Add(polygon);
                    continue;
                }
                
                var tocut = new List<Polygon> { polygon };
                for (int s = 0; s < 6; s++)
                {
                    var plane = new Plane(volume.GetSideDirection(s), volume.GetSidePosition(s));
                    var cutted = new List<Polygon>();
                    for (int c = 0, cc = tocut.Count; c < cc; c++)
                    {
                        cutted.AddRange(tocut[c].Cut(plane));
                    }

                    tocut = cutted;
                }

                pols.AddRange(tocut);
            }
            
            return pols;
        }

        public static Polygon[] CreatePolygons(Vector3[] vertices, int[] triangles)
        {
            Polygon[] polys = new Polygon[triangles.Length / 3];

            for (int i = 0; i < polys.Length; i++)
            {
                int t1 = triangles[i * 3];
                int t2 = triangles[i * 3 + 1];
                int t3 = triangles[i * 3 + 2];
                polys[i] = new Polygon(vertices[t1], vertices[t2], vertices[t3]);
            }

            return polys;
        }

        private static Vector2[] CreateUVs(Vector3[] vertices, Volume volume, Sprite sprite)
        {
            Vector2[] uvs = new Vector2[vertices.Length];
            Rect rect = sprite != null ? sprite.rect : new Rect(0, 0, 0.5f, 0.5f);

            if (sprite != null)
            {
                rect.x /= sprite.texture.width;
                rect.y /= sprite.texture.height;
                rect.width /= sprite.texture.width;
                rect.height /= sprite.texture.height;
            }
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                vertex -= volume.Origin;
                vertex.x /= volume.Size.x;
                vertex.z /= volume.Size.z;

                var uv = new Vector2(vertex.x + 0.5f, vertex.z + 0.5f);

                uv.x *= rect.width * 2;
                uv.y *= rect.height * 2;
                uv.x += rect.x * 2;
                uv.y += rect.y * 2;

                uvs[i] = uv;
            }

            return uvs;
        }

        private static bool IsAngle(float surfaceMaxAngle, Vector3 up, Vector3 direction)
        {
            return Vector3.Angle(-up, direction) >= surfaceMaxAngle;
        }

        private static List<Polygon> TerrainPlane(Vector3 gridStart, float gridSize, Vector3 start, Vector3 end, Vector3 planeSize)
        {
            var pols = new List<Polygon>();
            Vector3 snapedStart = GetSnapedPosition(start, gridStart, gridSize);
            Vector3 snapedEnd = GetSnapedPosition(end, gridStart, gridSize);
            Vector3 place = snapedEnd - snapedStart;

            int sizeX = Mathf.FloorToInt(place.x / gridSize) + 1;
            int sizeZ = Mathf.FloorToInt(place.z / gridSize) + 1;

            snapedStart.y = gridStart.y;

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3 facePos = snapedStart + new Vector3(x * gridSize, 0, z * gridSize);
                    if (IsInPlane(facePos, gridStart, planeSize))
                    {
                        pols.AddRange(PolygonFace(facePos, gridSize));
                    }
                }
            }
            
            return pols;
        }

        private static Polygon[] PolygonFace(Vector3 position, float size)
        {
            var fwr = Vector3.forward;
            var ri = Vector3.right;

            var vertices = new[]
            {
                position + (-fwr - ri) * 0.5f * size,
                position + (fwr - ri) * 0.5f * size,
                position + (fwr + ri) * 0.5f * size,
                position + (-fwr + ri) * 0.5f * size
            };

            return new[]
            {
                new Polygon(vertices[0], vertices[1], vertices[2]),
                new Polygon(vertices[2], vertices[3], vertices[0])
            };
        }

        private static Vector3 GetSnapedPosition(Vector3 position, Vector3 gridStart, float gridSize)
        {
            Vector3 pos = position - gridStart;
            Vector3 snapedPos = gridStart;

            int x = Mathf.FloorToInt(pos.x / gridSize);
            int z = Mathf.FloorToInt(pos.z / gridSize);

            snapedPos.x += x * gridSize + gridSize * 0.5f;
            snapedPos.z += z * gridSize + gridSize * 0.5f;

            return snapedPos;
        }

        private static bool IsInPlane(Vector3 position, Vector3 planePosition, Vector3 planeSize)
        {
            return position.x > planePosition.x && position.x < planePosition.x + planeSize.x
                   && position.z > planePosition.z && position.z < planePosition.z + planeSize.z;
        }
    }
}
