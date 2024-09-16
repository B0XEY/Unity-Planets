using System.Linq;
using Boxey.Planets.Core.Components;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Boxey.Planets.Core.Classes {
    public class Node {
        private readonly Vector3 _offset;

        //Private Data
        private readonly PlanetaryObject _planetaryObject;
        private bool _isGenerated;

        private Mesh _nodeMesh;
        private MeshFilter _nodeFilter;
        private MeshRenderer _nodeRenderer;
        private MeshCollider _nodeCollider;
        private GameObject _nodeObject;

        private int _lastIndex;
        private float3[] _planetMap;
        private float2[] _terraformMap;
        public Node[] Children;

        public Node(PlanetaryObject planetaryObject, Node parent, int divisions, Vector3 offset) {
            _planetaryObject = planetaryObject;
            ParentNode = parent;
            Divisions = divisions;
            _offset = offset;
            NodeBounds = new Bounds(NodeLocalPosition(), Vector3.one * NodeScale());
            _lastIndex = 0;
            
            var mapSize = (int)(PlanetaryObject.ChunkSize / _planetaryObject.VoxelScale) + 1;
            _terraformMap = new float2[mapSize * mapSize * mapSize];
        }

        //Public Data
        public Matrix4x4[] NodeGrass { get; private set; }
        public Bounds NodeBounds { get; }
        public Node ParentNode { get; }
        public int Divisions { get; }

        #region Node functions
        private void CreateNode(bool force = false) {
            //Only generate the terrain when the node is the smallest and has no children
            if (!force && (!IsLeaf() || _isGenerated)) {
                _isGenerated = true;
                return;
            }
            var mapSize = (int)(PlanetaryObject.ChunkSize / _planetaryObject.VoxelScale);
            //Noise map check
            var startTime = Time.realtimeSinceStartup;
            _planetMap ??= JobManager.GetPlanetNoiseMap(mapSize, NodeLocalPosition(),
                _planetaryObject.PlanetRadius, _planetaryObject.RootNode.NodeLocalPosition(), NodeScale(),
                _planetaryObject.Seed, _planetaryObject.MaxPlanetDivisions,
                _planetaryObject.PlanetData, _planetaryObject.NoiseCurves);
            _planetaryObject.PlanetReport.AddReport($"Data generation took: {(Time.realtimeSinceStartup - startTime) * 1000f:00.00000}ms ({Divisions})");
            //Build terrain
            startTime = Time.realtimeSinceStartup;
            var meshFunction = new NodeMarching(mapSize, NodeScale(), PlanetaryObject.ValueGate, PlanetaryObject.CreateGate, PlanetaryObject.SmoothTerrain, _planetMap, _terraformMap);
            meshFunction.Generate();
            _planetaryObject.PlanetReport.AddReport($"Mesh generation took: {(Time.realtimeSinceStartup - startTime) * 1000f:00.00000}ms ({Divisions})");
            //Create game object for the node
            _nodeObject ??= Object.Instantiate(_planetaryObject.NodePrefab, _planetaryObject.ChunkHolder);
            _nodeObject.layer = 3;
            var nodePosition = NodeLocalPosition();
            var position = nodePosition - _planetaryObject.StartingPosition;
            _nodeObject.name = $"Node ({Divisions}): ({position.x}, {position.y}, {position.z})";
            _nodeObject.transform.localPosition = position;
            if (!_nodeFilter) {
                _nodeObject.TryGetComponent(out _nodeFilter);
            }
            if (!_nodeRenderer) {
                _nodeObject.TryGetComponent(out _nodeRenderer);
            }
            if (!_nodeCollider) {
                _nodeObject.TryGetComponent(out _nodeCollider);
            }

            if (meshFunction.VerticesArray.Length != 0) {
                _nodeMesh ??= new Mesh {
                    name = nodePosition.ToString(),
                    indexFormat = IndexFormat.UInt32
                };
                _nodeMesh.Clear();
                _nodeMesh.SetVertices(meshFunction.VerticesArray);
                _nodeMesh.SetNormals(meshFunction.NormalArray);
                _nodeMesh.SetTriangles(meshFunction.TriangleArray, 0);
                _nodeMesh.SetUVs(0, meshFunction.UVArrayOne);
                _nodeMesh.SetUVs(1, meshFunction.UVArrayTwo);
                
                _nodeFilter.sharedMesh = _nodeMesh;
                _nodeRenderer.sharedMaterial = _planetaryObject.ChunkMaterial;
                if (Divisions <= _planetaryObject.MaxPlanetDivisions * 0.5f && _nodeCollider) {
                    _nodeCollider.sharedMesh = _nodeMesh;
                }
            }else {
                _nodeObject.SetActive(false);
            }

            _isGenerated = true;
            ParentNode?.UpdateNode();
            ParentNode?.TrySplitNode();
            //grass and foliage
            if (Divisions <= _planetaryObject.MaxPlanetDivisions * 0.3f) {
                CalcGrassPositions();
            }
        }
        public void TrySplitNode() {
            if (IsLeaf()) {
                _nodeObject?.SetActive(true);
            }
            //make sure all children are generated
            var childNodesGenerated = Children?.Sum(node => node._isGenerated ? 1 : 0);
            if (childNodesGenerated <= 5) {
                return;
            }
            ParentNode?.TrySplitNode();
            //Split the node
            if (_nodeObject && _nodeObject.activeSelf) {
                _nodeObject.SetActive(false);
            }
        }
        public void UpdateNode() {
            if (_isGenerated && _nodeObject && !_nodeObject.activeSelf) {
                _nodeObject.SetActive(true);
                if(_lastIndex != _planetaryObject.TerraformInfo.Count) {
                    CreateNode();
                    _lastIndex = _planetaryObject.TerraformInfo.Count;
                }
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
        public void TryForceTerraform(float2[] newTerraformMap) {
            if (_lastIndex == _planetaryObject.TerraformInfo.Count) {
                return;
            }
            _terraformMap = newTerraformMap;
            _lastIndex = _planetaryObject.TerraformInfo.Count;
            CreateNode();
        }
        public void DestroyNode() {
            if (!IsLeaf()) {
                return;
            }
            //Destroy meshes / node data
            _isGenerated = false;
            Object.Destroy(_nodeMesh);
            Object.Destroy(_nodeObject);
            _nodeMesh = null;
            _nodeObject = null;
        }
        #endregion
        public float2[] GetTerraformMap() => _terraformMap;
        public int GetTerraformIndex() => _lastIndex;
        #region Foliage
        private void CalcGrassPositions() {
            if (!_nodeMesh) {
                return;
            }
            var vertices = _nodeMesh.vertices;
            var triangles = _nodeMesh.triangles;
            NodeGrass = JobManager.GetPlanetFoliageMap(vertices.ToArray(), triangles, NodeWorldPosition(), Divisions, _planetaryObject.GrassPerTriangle, _planetaryObject.Seed);
        }
        #endregion
        #region Node Base Functions
        public bool IsLeaf() {
            return Children == null;
        }
        public float NodeScale() {
            return PlanetaryObject.ChunkSize * (int)Mathf.Pow(2, Divisions - 1);
        }
        private Vector3 NodeCenter() {
            return Vector3.one * (NodeScale() / 2f);
        }
        public Vector3 NodeLocalPosition() {
            if (ParentNode == null) {
                return _offset;
            }

            var nodeScale = NodeScale();
            return _offset * nodeScale - NodeCenter() + ParentNode.NodeLocalPosition();
        }
        public Vector3 NodeWorldPosition() {
            return _nodeObject == null ? NodeLocalPosition() : _nodeObject.transform.position;
        }
        #endregion
    }
}