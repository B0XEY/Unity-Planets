using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Classes {
    public class NodeMarching {
        //Calculated Values
        private readonly int3 _centerOffset;
        //Set Values
        private readonly int _chunkSize;
        private readonly float _createGate;
        private readonly float3[] _noiseMap;
        private readonly float2[] _terriformMap;
        private readonly bool _smoothTerrain;
        private readonly float _valueGate;
        private readonly float _voxelScale;
        public Vector3[] NormalArray;
        public int[] TriangleArray;
        public Vector2[] UVArrayOne;
        public Vector2[] UVArrayTwo;
        //Mesh Data
        public Vector3[] VerticesArray;
        
        public NodeMarching(int chunkSize, float nodeScale, float valueGate, float createGate, bool smoothTerrain, float3[] noiseMap, float2[] terriformMap) {
            _chunkSize = chunkSize;
            _valueGate = valueGate;
            _createGate = createGate;
            _smoothTerrain = smoothTerrain;

            _noiseMap = noiseMap;
            _terriformMap = terriformMap;

            _centerOffset = new int3(1) * (int)(nodeScale / 2);
            _voxelScale = nodeScale / _chunkSize;
            //_voxelScale = Mathf.Clamp(nodeScale / _chunkSize, 1, 999999);
        }
        public void Generate() {
            (VerticesArray, NormalArray, TriangleArray, UVArrayOne, UVArrayTwo) = 
                JobManager.GetPlanetMeshData(_chunkSize, _voxelScale, _centerOffset, _createGate, _valueGate, _smoothTerrain, _noiseMap, _terriformMap);
        }
    }
}