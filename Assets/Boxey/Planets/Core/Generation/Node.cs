using System.Collections.Generic;
using System.Linq;
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
        private readonly Vector3 _offset;
        
        //Public Data
        public Bounds NodeBounds { get; }
        public Node ParentNode { get; }
        public Node[] Children;
        public readonly int Divisions;
        
        //Terraforming data done to chunk
        private float[] _planetMap;
        private float[] _modData;

        private Mesh _nodeMesh;
        private MeshFilter _nodeFilter;
        private MeshRenderer _nodeRenderer;
        private MeshCollider _nodeCollider;
        private bool _isGenerated;

        public Node(Planet planet, Node parent, int divisions, Vector3 offset, bool isRoot = false){
            _planet = planet;
            ParentNode = parent;
            Divisions = divisions;
            _offset = offset;
            NodeBounds = new Bounds(NodeLocalPosition(), Vector3.one * NodeScale());
            if (isRoot) return;
            _planetMap = JobManager.GetPlanetNoiseMap(Planet.ChunkSize, GetSamplePosition(), 
                _planet.PlanetRadius, _planet.RootNode.NodeLocalPosition(), NodeScale(), _planet.Seed, _planet.CurrentDivisions, _planet.PlanetData);
            _modData = _planet.TryGetModTreeData(NodeLocalPosition());
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
            _planetMap ??= JobManager.GetPlanetNoiseMap(Planet.ChunkSize, GetSamplePosition(),
                _planet.PlanetRadius, _planet.RootNode.NodeLocalPosition(), NodeScale(), _planet.Seed, _planet.CurrentDivisions,
                _planet.PlanetData);
            _modData ??= _planet.TryGetModTreeData(nodePosition);
            //Build terrain
            var meshFunction = new NodeMarching(Planet.ChunkSize, NodeScale(), _planet.ValueGate, _planet.CreateGate, _planet.SmoothTerrain, _planetMap, _modData);
            meshFunction.Generate();
            

            //Create game object for the node
            if (_nodeObject) {
                Object.Destroy(_nodeMesh);
                Object.Destroy(_nodeObject);
                _nodeMesh = null;
                _nodeObject = null;
            }
            _nodeObject = Object.Instantiate(_planet.NodePrefab, _planet.ChunkHolder);
            _nodeObject.layer = 3;
            var position = (nodePosition - _planet.StartingPosition);
            _nodeObject.name = $"Node ({Divisions}): ({position.x}, {position.y}, {position.z})";
            _nodeObject.transform.localPosition = position;
            _nodeObject.TryGetComponent(out _nodeFilter);
            _nodeObject.TryGetComponent(out _nodeRenderer);
            _nodeObject.TryGetComponent(out _nodeCollider);
            
            if (meshFunction.VerticesArray.Length != 0){
                _nodeMesh = new Mesh{
                    name = nodePosition.ToString(),
                    vertices = meshFunction.VerticesArray,
                    normals = meshFunction.NormalArray,
                    triangles = meshFunction.TriangleArray
                };
                
                _nodeFilter.sharedMesh = _nodeMesh;
                _nodeRenderer.sharedMaterial = _planet.ChunkMaterial;
                //Create the Mesh Collider for the object if it is one of the last 2 divisions
                if (Divisions <= _planet.CurrentDivisions * 0.5f){
                    _nodeCollider.sharedMesh = _nodeMesh;
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
            if (childNodesGenerated < 8) return;
            //Split the node
            if (_nodeObject != null && _nodeObject.activeSelf) _nodeObject.SetActive(false);
            //Foliage
            if (_nodeFoliage != null){
                _nodeFoliage.Clear();
                _nodeFoliage = null;
            }
        }
        public void UpdateNode(){
            if (_isGenerated && _nodeObject && !_nodeObject.activeSelf) {
                _nodeObject.SetActive(true);
                return;
            }
            if (!IsLeaf()) {
                //Node has kids so the mesh is not needed try to call the split function to toggle off mesh
                TrySplitNode();
                return;
            }
            if (!_isGenerated || !_nodeObject){
                //Leaf node that is not generated or the object does not exist, so we remake it
                CreateNode();
                return;
            }
        } 
        public void DestroyNode(){
            //Save mod data
            if (_modData != null) _planet.SaveModTreeData(NodeLocalPosition(), _modData);
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
            _modData = JobManager.GetTerraformMap(Planet.ChunkSize, NodeScale(), NodeWorldPosition(), _modData, terraformPoint, new float3(radius, speed, addTerrain ? 1 : -1));
            if (!IsLeaf()) return;
            _isGenerated = false;
            //Update Mesh
            UpdateNode();
        }
        #endregion
        #region Node Base Functions
        private float3 GetSamplePosition(){
            var nodePosition = NodeLocalPosition();
            var offset = (float3)_planet.StartingPosition;
            return new float3((nodePosition.z - offset.z) + offset.x, nodePosition.y, (nodePosition.x - offset.x) + offset.z);
        }
        
        public bool IsLeaf() => Children == null;
        public int NodeScale() => Planet.ChunkSize * (int)Mathf.Pow(2, Divisions - 1);
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