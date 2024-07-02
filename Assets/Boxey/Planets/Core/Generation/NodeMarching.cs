using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Generation {
    public class NodeMarching {
        //Calculated Values
        private readonly int3 _centerOffset;
        private readonly int _voxelScale;
        //Set Values
        private readonly int _chunkSize;
        private readonly float _valueGate;
        private readonly float _createGate;
        private readonly bool _smoothTerrain;
        private readonly float4[] _noiseMap;
        private readonly float[] _modMap;
        //Mesh Data
        public Vector3[] VerticesArray;
        public Vector3[] NormalArray;
        public int[] TriangleArray;


        public NodeMarching(int chunkSize, int nodeScale, float valueGate, float createGate, bool smoothTerrain, float4[] noiseMap, float[] modMap){
            _chunkSize = chunkSize;
            _valueGate = valueGate;
            _createGate = createGate;
            _smoothTerrain = smoothTerrain;
            
            _noiseMap = noiseMap;
            _modMap = modMap;

            _centerOffset = new int3(1,1,1) * (nodeScale / 2);
            _voxelScale = nodeScale / _chunkSize;
        }
        
        public void Generate() {
            JobManager.GetPlanetMeshData(_chunkSize, _voxelScale, _centerOffset, _createGate, _valueGate, _smoothTerrain, _noiseMap, _modMap, out VerticesArray, out NormalArray, out TriangleArray);
        }
    }
}