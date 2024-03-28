using System.Collections.Generic;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Boxey.Planets.Core.Generation {
    public class Node{
        //Private Data
        private readonly Planet _planet;
        private GameObject _nodeObject;
        private List<GameObject> _nodeFoliage;
        private readonly Node _parent;
        private readonly Vector3 _offset;
        
        //Public Data
        public Node[] Children;
        public readonly int Divisions;
        
        //Terraforming data done to chunk
        private float[] _modData;

        private Mesh _nodeMesh;
        private bool _isGenerated;
        private bool _isSplit;

        public Node(Planet planet, Node parent, int divisions, Vector3 offset, bool isRoot = false){
            _planet = planet;
            _parent = parent;
            Divisions = divisions;
            _offset = offset;
            if (!isRoot) CreateMesh();
        }
        
        // Mesh functions
        private void CreateMesh(){
            //Only generate the terrain when the node is the smallest and has no children
            if (!IsLeaf() || _isGenerated){
                _isGenerated = true;
                return;
            }
            _isGenerated = true;
            //Mod data retrieval for creation
            var nodePosition = NodePosition();
            _modData = _planet.TryGetModTreeData(nodePosition);
            //Build Terrain - Get Noise Map
            var meshFunction = new NodeMarching(_planet.ChunkSize, NodeScale(), _planet.ValueGate, _planet.CreateGate, GetMap(), _modData);
            meshFunction.Generate();
            //If there is no mesh object we do not create the gameObject
            if (meshFunction.VerticesArray.Length == 0){
                return;
            }
            
            _nodeMesh = new Mesh{
                name = nodePosition.ToString(),
                vertices = meshFunction.VerticesArray,
                normals = meshFunction.NormalArray,
                triangles = meshFunction.TriangleArray
            };

            //Creat GameObject because it is a mesh
            _nodeObject = Object.Instantiate(_planet.NodePrefab, _planet.ChunkHolder);
            _nodeObject.layer = 3;
            _nodeObject.name = "Chunk: " + (nodePosition - _planet.transform.position);
            _nodeObject.transform.localPosition = (nodePosition - _planet.StartingPosition);
            _nodeObject.TryGetComponent<MeshFilter>(out var filter);
            _nodeObject.TryGetComponent<MeshRenderer>(out var renderer);
            filter.sharedMesh = _nodeMesh;
            renderer.sharedMaterial = _planet.ChunkMaterial;
            //Create the Mesh Collider for the object if it is one of the last 2 divisions
            if (Divisions <= 2){
                _nodeObject.TryGetComponent<MeshCollider>(out var collider);
                collider.sharedMesh = _nodeMesh;
            }
        }
        public void DestroyNode(){
            if (_modData != null) _planet.SaveModTreeData(NodePosition(), _modData);
            _modData = null;
            _isGenerated = false;
            //Destroy Foliage
            if (_nodeFoliage != null){
                _nodeFoliage.Clear();
                _nodeFoliage = null;
            }
            //Destroy Meshes
            _isSplit = false;
            Object.Destroy(_nodeMesh);
            _nodeMesh = null;
            Object.Destroy(_nodeObject);
            _nodeObject = null;
        }
        public void PrepareSplit(){
            _isSplit = true;
            if (_nodeObject != null) _nodeObject.SetActive(false);
            if (_nodeFoliage != null){
                _nodeFoliage.Clear();
                _nodeFoliage = null;
            }
        }
        
        // Noise functions
        private float3 GetSamplePosition(){
            var nodePosition = NodePosition();
            var offset = (float3)_planet.StartingPosition;
            return new float3((nodePosition.z - offset.z) + offset.x, (nodePosition.y - offset.y) + offset.y, (nodePosition.x - offset.x) + offset.z);
        }

        private float[] GetMap() => JobManager.GetPlanetNoiseMap(_planet.ChunkSize, GetSamplePosition(), 
            _planet.PlanetRadius, _planet.RootNode.NodePosition(), NodeScale(), _planet.Seed, _planet.CurrentDivisions, _planet.Data);
        // Terraforming functions
        public float[] GetTerraformingMap() => _modData;
        public void CompleteTerraforming(float[] data){
            // if the new map is not changed we should not remake the mesh
            if (_modData == data) return;
            // edit the current data
            _isGenerated = false;
            _modData = data;
            CreateMesh();
        }
        
        //Node Functions
        private bool IsLeaf() => Children == null;
        public void TryGeneration(){
            if (!_isSplit) return;
            if (!_isGenerated){
                CreateMesh();
                _isSplit = false;
                return;
            }
            if (_nodeObject == null){
                CreateMesh();
                _isSplit = false;
            }else {
                _nodeObject.SetActive(true);
                _isSplit = false;
            }
        }
        
        private int NodeResolution() => (int)Mathf.Pow(2, Divisions - 1);
        public int NodeScale() => _planet.ChunkSize * NodeResolution();
        public Vector3 NodePosition(){
            if (_parent == null){
                return _offset;
            }

            var nodeScale = NodeScale();
            return (_offset * nodeScale) - (Vector3.one * (nodeScale / 2f)) + _parent.NodePosition();
        }

        public Vector3 NodeWorldPosition() {
            return _nodeObject == null ? NodePosition() : _nodeObject.transform.position;
        }
    }
}