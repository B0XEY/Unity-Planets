using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Generation {
    public class NodeMarching{
        //Calculated Values
        private readonly int3 _centerOffset;
        private readonly int _voxelScale;
        //Set Values
        private readonly int _chunkSize;
        private readonly float _valueGate;
        private readonly float _createGate;
        private readonly float[] _noiseMap;
        private readonly float[] _modMap;
        //Mesh Data
        public Vector3[] VerticesArray;
        public Vector3[] NormalArray;
        public int[] TriangleArray;


        public NodeMarching(int chunkSize, int nodeScale, float valueGate, float createGate, float[] noiseMap, float[] modMap){
            _chunkSize = chunkSize;
            _valueGate = valueGate;
            _createGate = createGate;
            
            _noiseMap = noiseMap;
            _modMap = modMap;

            _centerOffset = new int3(1,1,1) * (nodeScale / 2);
            _voxelScale = nodeScale / _chunkSize;
        }
        
        public void Generate() {
            JobManager.GetPlanetMeshData(_chunkSize, _voxelScale, _centerOffset, _createGate, _valueGate, _noiseMap, _modMap, out VerticesArray, out TriangleArray);
            CalculateNormals();
        }
        
        private void CalculateNormals() {
            var vertexNormals = new Vector3[VerticesArray.Length];
            var triangleCont = TriangleArray.Length / 3;
            for (var i = 0; i < triangleCont; i++) {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = TriangleArray[normalTriangleIndex];
                var vertexIndexB = TriangleArray[normalTriangleIndex + 1];
                var vertexIndexC = TriangleArray[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals[vertexIndexA] += triangleNormal;
                vertexNormals[vertexIndexB] += triangleNormal;
                vertexNormals[vertexIndexC] += triangleNormal;
            }

            for (var i = 0; i < vertexNormals.Length; i++) {
                vertexNormals[i].Normalize();
            }

            NormalArray = vertexNormals;
        }
        private Vector3 SurfaceNormal(int indexA, int indexB, int indexC) {
            var pointA = VerticesArray[indexA];
            var pointB = VerticesArray[indexB];
            var pointC = VerticesArray[indexC];

            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
        
            return Vector3.Cross(sideAb, sideAc).normalized;
        }
    }
}