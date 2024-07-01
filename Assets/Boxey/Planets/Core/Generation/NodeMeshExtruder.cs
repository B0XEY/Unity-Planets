using System.Collections.Generic;
using UnityEngine;

namespace Boxey.Planets.Core.Generation {
    public static class NodeMeshExtruder {
        public static void UpdateMesh( Mesh mesh, float extrusionDistance) {
            var vertices = new List<Vector3>(mesh.vertices);
            var triangles = new List<int>(mesh.triangles);

            // Loop through each triangle
            for (var i = 0; i < triangles.Count; i += 3) {
                var vertexA = triangles[i];
                var vertexB = triangles[i + 1];
                var vertexC = triangles[i + 2];

                // Calculate extrusion direction (adjust as needed)
                var normal = Vector3.Cross(vertices[vertexB] - vertices[vertexA], vertices[vertexC] - vertices[vertexA]).normalized;

                // Extrude vertices
                var extrudedA = vertices[vertexA] + normal * extrusionDistance;
                var extrudedB = vertices[vertexB] + normal * extrusionDistance;
                var extrudedC = vertices[vertexC] + normal * extrusionDistance;

                // Update triangles with new vertices
                triangles.Add(vertices.Count);
                vertices.Add(extrudedA);

                triangles.Add(vertices.Count);
                vertices.Add(extrudedB);

                triangles.Add(vertices.Count);
                vertices.Add(extrudedC);

                // Update original triangle (optional, for closed edges)
                triangles[i] = vertices.Count;
                vertices.Add(vertices[vertexA]);

                triangles[i + 1] = vertices.Count;
                vertices.Add(vertices[vertexB]);

                triangles[i + 2] = vertices.Count;
                vertices.Add(vertices[vertexC]);
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
        }
    }
}