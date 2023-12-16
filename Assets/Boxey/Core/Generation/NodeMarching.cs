using System.Collections.Generic;
using System.Linq;
using Boxey.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Core.Generation {
    public class NodeMarching{
        //Calculated Values
        private readonly int3 m_centerOffset;
        private readonly int m_voxelScale;
        //Set Values
        private readonly int m_chunkSize;
        private readonly float m_valueGate;
        private readonly float m_createGate;
        private readonly float[] m_noiseMap;
        private readonly float[] m_modMap;
        //Lists / Other
        private readonly List<Vector3> m_verticesList;
        private readonly List<int> m_triangles;
        
        public Vector3[] VerticesArray;
        public Vector3[] NormalArray;
        public int[] Triangles;


        public NodeMarching(int chunkSize, int nodeScale, float valueGate, float createGate, float[] noiseMap, float[] modMap){
            m_chunkSize = chunkSize;
            m_valueGate = valueGate;
            m_createGate = createGate;
            
            m_noiseMap = noiseMap;
            m_modMap = modMap;

            m_centerOffset = new int3(1,1,1) * (nodeScale / 2);
            m_voxelScale = nodeScale / m_chunkSize;
            m_verticesList = new List<Vector3>();
            m_triangles = new List<int>();
        }
        
        //Functions
        public void Generate(){
            m_verticesList.Clear();
            m_triangles.Clear();
            
            for (int x = 0; x < m_chunkSize; x++){
                for (int y = 0; y < m_chunkSize; y++){
                    for (int z = 0; z < m_chunkSize; z++){
                        var noiseValue = SampleMap(new int3(x, y, z));
                        if (noiseValue < -m_createGate || noiseValue > m_createGate) continue;
                        CreateCube(new int3(x, y, z));
                    }
                }
            }
            
            VerticesArray = m_verticesList.Select(vert => new Vector3(vert.x, vert.y, vert.z)).ToArray();
            Triangles = m_triangles.ToArray();
            NormalArray = CalculateNormals();
        }
        private int GetCubeConfig(float[] cube) {
            var configIndex = 0;
            for (int i = 0; i < 8; i++) {
                if (cube[i] < m_valueGate) configIndex |= 1 << i;
            }

            return configIndex;
        }
        private void CreateCube(int3 voxelPoint) {
            var worldPosition = (voxelPoint * m_voxelScale) - m_centerOffset;
            var cube = new float[8];
            for (int i = 0; i < 8; i++) {
                cube[i] = SampleMap(voxelPoint + VoxelTables.CornerTable[i]);
            }
            var configIndex = GetCubeConfig(cube);
            if (configIndex is 0 or 255) {
                return;
            }
            var edgeIndex = 0;
            for (int i = 0; i < 5; i++) { //Mesh triangles
                for (int j = 0; j < 3; j++) { // Mesh Points
                    var indice = VoxelTables.TriangleTable[configIndex, edgeIndex];
                    if (indice == -1) return;
                    var edge1 = VoxelTables.EdgeIndexes[indice, 0];
                    var edge2 = VoxelTables.EdgeIndexes[indice, 1];
                    //Get Vert Positions
                    float3 vert1 = worldPosition + VoxelTables.CornerTable[edge1] * m_voxelScale;
                    float3 vert2 = worldPosition + VoxelTables.CornerTable[edge2] * m_voxelScale;
                    float vert1Sample = cube[edge1];
                    float vert2Sample = cube[edge2];
                    float difference = vert2Sample - vert1Sample;
                    if (difference == 0) difference = m_valueGate;
                    else difference = (m_valueGate - vert1Sample) / difference;
                    var vertPosition = vert1 + ((vert2 - vert1) * difference);
                    //Add to the lists
                    m_verticesList.Add(vertPosition);
                    m_triangles.Add(m_verticesList.Count - 1);
                    edgeIndex++;
                }
            }
        }
        private float SampleMap(int3 point){
            var index = point.x + (m_chunkSize + 1) * (point.y + (m_chunkSize + 1) * point.z);
            var value = m_noiseMap[index];
            if (m_modMap != null) value += m_modMap[index];
            return value;
        }
        
        #region Mesh Functions

        private Vector3[] CalculateNormals() {
            var vertexNormals = new Vector3[m_verticesList.Count];
            var triangleCont = m_triangles.Count / 3;
            for (var i = 0; i < triangleCont; i++) {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = m_triangles[normalTriangleIndex];
                var vertexIndexB = m_triangles[normalTriangleIndex + 1];
                var vertexIndexC = m_triangles[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals[vertexIndexA] += triangleNormal;
                vertexNormals[vertexIndexB] += triangleNormal;
                vertexNormals[vertexIndexC] += triangleNormal;
            }

            for (var i = 0; i < vertexNormals.Length; i++) {
                vertexNormals[i].Normalize();
            }

            return vertexNormals;
        }
        private Vector3 SurfaceNormal(int indexA, int indexB, int indexC) {
            Vector3 pointA = m_verticesList[indexA];
            Vector3 pointB = m_verticesList[indexB];
            Vector3 pointC = m_verticesList[indexC];

            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
        
            return Vector3.Cross(sideAb, sideAc).normalized;
        }

        #endregion
    }
}