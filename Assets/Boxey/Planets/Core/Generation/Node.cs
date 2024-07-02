using System.Collections.Generic;
using System.Linq;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Boxey.Planets.Core.Generation {
    public class Node{
        //Private Data
        private readonly PlanetaryObject _planetaryObject;
        private GameObject _nodeObject;
        private List<GameObject> _nodeFoliage;
        private readonly Vector3 _offset;
        
        //Public Data
        public Bounds NodeBounds { get; }
        public Node ParentNode { get; }
        public Node[] Children;
        public readonly int Divisions;
        
        //Terraforming data done to chunk
        private float4[] _planetMap;
        private float[] _modData;

        private Mesh _nodeMesh;
        private bool _isGenerated;

        public Node(PlanetaryObject planetaryObject, Node parent, int divisions, Vector3 offset, bool isRoot = false){
            _planetaryObject = planetaryObject;
            ParentNode = parent;
            Divisions = divisions;
            _offset = offset;
            NodeBounds = new Bounds(NodeLocalPosition(), Vector3.one * NodeScale());
            if (isRoot) return;
            _modData = _planetaryObject.TryGetModTreeData(NodeLocalPosition());
        }
        
        #region Node functions
        private void CreateNode(){
            //Only generate the terrain when the node is the smallest and has no children
            if (!IsLeaf() || _isGenerated){
                _isGenerated = true;
                return;
            }
            //Noise map check 
            var nodePosition = NodeLocalPosition();
            _planetMap ??= JobManager.GetPlanetNoiseMap(PlanetaryObject.ChunkSize, GetSamplePosition(),
                _planetaryObject.PlanetRadius, _planetaryObject.RootNode.NodeLocalPosition(), NodeScale(), _planetaryObject.Seed, _planetaryObject.MaxDivisions,
                _planetaryObject.PlanetData);
            _modData ??= _planetaryObject.TryGetModTreeData(nodePosition);
            //Build terrain
            var meshFunction = new NodeMarching(PlanetaryObject.ChunkSize, NodeScale(), _planetaryObject.ValueGate, _planetaryObject.CreateGate, _planetaryObject.SmoothTerrain, _planetMap, _modData);
            meshFunction.Generate();
            

            //Create game object for the node
            if (_nodeObject) {
                Object.Destroy(_nodeMesh);
                Object.Destroy(_nodeObject);
                _nodeMesh = null;
                _nodeObject = null;
            }
            _nodeObject = Object.Instantiate(_planetaryObject.NodePrefab, _planetaryObject.ChunkHolder);
            _nodeObject.layer = 3;
            var position = (nodePosition - _planetaryObject.StartingPosition);
            _nodeObject.name = $"Node ({Divisions}): ({position.x}, {position.y}, {position.z})";
            _nodeObject.transform.localPosition = position;
            _nodeObject.TryGetComponent<MeshFilter>(out var nodeFilter);
            _nodeObject.TryGetComponent<MeshRenderer>(out var nodeRenderer);
            _nodeObject.TryGetComponent<MeshCollider>(out var nodeCollider);
            
            if (meshFunction.VerticesArray.Length != 0){
                _nodeMesh = new Mesh{
                    name = nodePosition.ToString(),
                    vertices = meshFunction.VerticesArray,
                    normals = meshFunction.NormalArray,
                    triangles = meshFunction.TriangleArray
                };
                
                nodeFilter.sharedMesh = _nodeMesh;
                nodeRenderer.sharedMaterial = _planetaryObject.ChunkMaterial;
                if (Divisions <= _planetaryObject.MaxDivisions * 0.5f){
                    nodeCollider.sharedMesh = _nodeMesh;
                }
            }else {
                _nodeObject.SetActive(false);
            }
            _isGenerated = true;
        }
        public void TrySplitNode(){
            //make sure all children are generated
            var childNodesGenerated = Children.Sum(node => node._isGenerated ? 1 : 0);
            if (childNodesGenerated == 0 && _nodeObject) _nodeObject.SetActive(true);
            if (childNodesGenerated < 7) {
                return;
            }
            //Split the node
            if (_nodeObject != null && _nodeObject.activeSelf) {
                _nodeObject.SetActive(false);
            }
            //Foliage
            if (_nodeFoliage != null){
                _nodeFoliage.Clear();
                _nodeFoliage = null;
            }
        }
        public void UpdateNode(){
            if (_isGenerated && _nodeObject && !_nodeObject.activeSelf) {
                _nodeObject.SetActive(true);
            }
            if (!IsLeaf()) {
                //Node has kids so the mesh is not needed try to call the split function to toggle off mesh
                TrySplitNode();
                return;
            }
            if (!_isGenerated || !_nodeObject){
                //Leaf node that is not generated or the object does not exist, so we remake it
                CreateNode();
            }
        } 
        public void DestroyNode(){
            //Save mod data
            if (_modData != null) {
                _planetaryObject.SaveModTreeData(NodeLocalPosition(), _modData);
            }
            _isGenerated = false;
            //Destroy foliage
            if (_nodeFoliage != null){
                _nodeFoliage.Clear();
                _nodeFoliage = null;
            }
            //Destroy meshes / node data
            _isGenerated = false;
            Object.Destroy(_nodeMesh);
            Object.Destroy(_nodeObject);
            _nodeMesh = null;
            _nodeObject = null;
        }
        #endregion
        #region Terraforming functions
        public void Terraform(float3 terraformPoint, float radius, float speed, bool addTerrain) {
            //Call the job from the job manager
            _modData = JobManager.GetTerraformMap(PlanetaryObject.ChunkSize, NodeScale(), NodeWorldPosition(), _modData, terraformPoint, new float3(radius, speed, addTerrain ? 1 : -1));
            if (!IsLeaf()) {
                return;
            }
            _isGenerated = false;
            //Update Mesh
            UpdateNode();
        }
        #endregion
        #region Node Base Functions
        private float3 GetSamplePosition(){
            var nodePosition = NodeLocalPosition();
            var offset = (float3)_planetaryObject.StartingPosition;
            return new float3((nodePosition.z - offset.z) + offset.x, nodePosition.y, (nodePosition.x - offset.x) + offset.z);
        }
        
        public bool IsLeaf() => Children == null;
        public int NodeScale() => PlanetaryObject.ChunkSize * (int)Mathf.Pow(2, Divisions - 1);
        private Vector3 NodeCenter() => Vector3.one * (NodeScale() / 2f);
        private Vector3 NodeLocalPosition(){
            if (ParentNode == null){
                return _offset;
            }

            var nodeScale = NodeScale();
            return (_offset * nodeScale) - NodeCenter() + ParentNode.NodeLocalPosition();
        }
        public Vector3 NodeWorldPosition() {
            return _nodeObject == null ? NodeLocalPosition() : _nodeObject.transform.position;
        }
        #endregion
    }
}