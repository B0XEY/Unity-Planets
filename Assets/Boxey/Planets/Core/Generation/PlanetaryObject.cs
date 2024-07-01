using System.Collections.Generic;
using System.Linq;
using Boxey.Attributes;
using Boxey.Planets.Core.Static;
using Boxey.Planets.Core.Generation.Data_Objects;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Boxey.Planets.Core.Generation {
    [AddComponentMenu("Boxey/Planet", -10000)]
    public class PlanetaryObject : MonoBehaviour {
        private const int MaxNewNodes = 5;
        public const int ChunkSize = 16;
        
        private float _rootNodeScale;
        private Camera _playerCamera;
        private Plane[] _viewBounds;
        private Vector3 _playersLastPosition;
        private struct NodeUpdateCall {
            public readonly Node ParentNode;
            public readonly Node NodeToUpdate;

            public NodeUpdateCall(Node parentNode, Node nodeToUpdate) {
                ParentNode = parentNode;
                NodeToUpdate = nodeToUpdate;
            }
        }
        private struct NodeTerraformCall {
            public readonly Node CallNode;
            public readonly float3 TerraformPoint;
            public readonly float BrushRadius;
            public readonly float BrushSpeed;
            public readonly bool AddTerrain;

            public NodeTerraformCall(Node callNode, float3 terraformPoint, float brushRadius, float brushSpeed, bool addTerrain) {
                CallNode = callNode;
                TerraformPoint = terraformPoint;
                BrushRadius = brushRadius;
                BrushSpeed = brushSpeed;
                AddTerrain = addTerrain;
            }
        }
        
        private bool _isTerraforming;
        private float3 _terraformPoint;
        private float _terraformRadius;
        private float _terraformSpeed;
        private bool _terraformAdd;
        
        private Dictionary<Vector3, float[]> _modDataDictionary;
        public Node RootNode { get; private set; }
        public Vector3 StartingPosition { get; private set; }
        public PlanetData PlanetData => planetData;
        public GameObject NodePrefab => nodePrefab;
        public int Seed => seed;
        public float ValueGate => valueGate;
        public float CreateGate => createGate;
        public bool SmoothTerrain => smoothTerrain;
        public Transform ChunkHolder => chunkHolder;
        public Material ChunkMaterial => planetData.PlanetMaterial;
        public float PlanetRadius => planetRadius;
        public int MaxDivisions => divisions;

        private static readonly int Center = Shader.PropertyToID("_Center");
        private static readonly int SandHeight = Shader.PropertyToID("_Sand_Height");
        private static readonly int SteepnessThreshold = Shader.PropertyToID("_Steepness_Threshold");
        public float GetSandHeight() => ChunkMaterial.GetFloat(SandHeight);
        public float GetSteepnessValue() => ChunkMaterial.GetFloat(SteepnessThreshold);
        
        private float GetRecommendedRadius() => (112.5f * ChunkSize) * Mathf.Pow(2, divisions - 8);
        private float GetRecommendedDivisions() => Mathf.CeilToInt(8 + Mathf.Log(planetRadius / (112.5f * ChunkSize)) / Mathf.Log(2));
        private void RandomSeed() => seed = Random.Range(-999999, 999999);

        [Header("Planet Data"), Line]
        [SerializeField] private int seed;
        [SerializeField] private bool randomSeed;
        [SerializeField, FoldableInspector(true)] private PlanetData planetData;
        
        [Header("Tree Settings"), Line]
        [SerializeField] private int divisions = 8;
        [SerializeField, OnChanged(nameof(GetRecommendedDivisions))] private float planetRadius = 1800;
        [SerializeField, Label("Recommended Divisions"), ShowOnly] private float planetRecommendedDivisions;
        [SerializeField, Label("Recommended Radius"), ShowOnly] private float planetRecommendedRadius;
        [Space(5f)]
        [SerializeField] private Transform chunkHolder;
        [SerializeField] private Transform player;
        [Space(5f)]
        [SerializeField] private bool travelTree;
        
        [Header("Marching Cubes Settings"), Line]
        [SerializeField] private float valueGate = .15f;
        [SerializeField] private float createGate = 1;
        [SerializeField] private bool smoothTerrain;
        
        [Header("Performance Settings"), Line]
        [SerializeField, Range(0.01f, 1f)] private float splitMultiplier = 0.25f;
        [SerializeField, Range(1, 2.5f)] private float splitRadius = 1.45f;
        [Space(5f)]
        [SerializeField] private float updateDistance = .5f;
        [SerializeField] private int maxNodeUpdatesPerFrame = 10;
        [SerializeField] private int maxNodeTerraformPerFrame = 2;
        [SerializeField] private Gradient textColors;
        [Space(5f)]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private bool drawNodes;
        //update Queue
        private List<NodeUpdateCall> _nodesToUpdate;
        private HashSet<NodeTerraformCall> _nodesToTerraform;

        private void Start() {
            //Seed + starting position setting
            StartingPosition = transform.position;
            _playersLastPosition = player.transform.position;
            if (randomSeed) RandomSeed();
            if (!_playerCamera) _playerCamera = Helpers.GetCamera;
            
            //Create dictionary. To-Do: get the dictionary from a file that contains the data
            _nodesToUpdate = new List<NodeUpdateCall>();
            _nodesToTerraform = new HashSet<NodeTerraformCall>();
            _modDataDictionary = new Dictionary<Vector3, float[]>();
            planetData.PlanetMaterial.SetVector(Center, StartingPosition);
            //View bounds
            _viewBounds = GeometryUtility.CalculateFrustumPlanes(_playerCamera);
            
            //Create root octree node
            RootNode = new Node(this, null, divisions, StartingPosition, true);
            _rootNodeScale = RootNode.NodeScale();
            TravelTree(RootNode);
            //Atmosphere
            transform.GetChild(1).gameObject.TryGetComponent<AtmosphereEffect>(out var atmosphere);
            atmosphere.sun = GameObject.FindWithTag("Sun").transform.GetChild(0);
            atmosphere.profile = planetData.Atmosphere;
            atmosphere.atmosphereScale = planetData.AtmosphereScale;
            atmosphere.planetRadius = planetRadius * 0.3f;
            atmosphere.cutoffDepth = 100f;
        }

        private void Update() {
            //Update the nodes that need it
            HandelUpdateCalls();
            HandelTerraformCalls();
            //travel if we are not too far from the tree
            var distanceFromRootNode = (player.position - transform.position).sqrMagnitude;
            var maxSplitDistance = (ChunkSize + ChunkSize) * splitMultiplier + splitRadius * _rootNodeScale;
            maxSplitDistance *= maxSplitDistance; 
            if (distanceFromRootNode < maxSplitDistance && travelTree) TravelTree(RootNode, 0, true);
            
            debugText.text = $"Nodes to Update: {_nodesToUpdate.Count}" +
                             $"\nNodes to Terraform: {_nodesToTerraform.Count}";
        }
        //Main update calls
        private void HandelUpdateCalls() {
            if (_nodesToUpdate.Count <= 0) return;
            var updateAmount = Mathf.Min(_nodesToUpdate.Count, maxNodeUpdatesPerFrame);
            var nodesToProcess = _nodesToUpdate.GetRange(0, updateAmount);
            foreach (var nodeUpdate in nodesToProcess) {
                _nodesToUpdate.Remove(nodeUpdate);
                nodeUpdate.NodeToUpdate?.UpdateNode();
                nodeUpdate.ParentNode?.TrySplitNode();
            }
        }
        private void HandelTerraformCalls() {
            if (_nodesToTerraform.Count <= 0) return;
            var nodesToProcess = _nodesToTerraform.Take(maxNodeTerraformPerFrame).ToList();
            foreach (var nodeUpdate in nodesToProcess) {
                _nodesToTerraform.Remove(nodeUpdate);
                nodeUpdate.CallNode.Terraform(nodeUpdate.TerraformPoint, nodeUpdate.BrushRadius, nodeUpdate.BrushSpeed, nodeUpdate.AddTerrain);
            }
        }
        //Main octree travel
        private void TravelTree(Node currentNode, int nodesCreated = 0, bool checkDistance = false) {
            if (checkDistance) {
                //Distance check so we don't update every frame only on the root node so the whole tree updates
                var distanceTraveled = (player.transform.position - _playersLastPosition).sqrMagnitude;
                if (distanceTraveled < updateDistance * updateDistance) return;
            }
            _playersLastPosition = player.transform.position;
            //normal tree update
            if (nodesCreated >= 8 * MaxNewNodes || currentNode.Divisions <= 1) return;
            var distanceFromNode = (player.position - currentNode.NodeWorldPosition()).sqrMagnitude;
            var splitDistance = ChunkSize * splitMultiplier + splitRadius * currentNode.NodeScale();
            splitDistance *= splitDistance;
            
            if (distanceFromNode < splitDistance) {
                if (currentNode.IsLeaf()) {
                    //split
                    currentNode.Children = new Node[8];
                    var newDivisions = currentNode.Divisions - 1;
                    for (var i = 0; i < 8; i++) {
                        var newNode = new Node(this, currentNode, newDivisions, VoxelTables.NodeOffsets[i].ToVector3());
                        currentNode.Children[i] = newNode;
                        _nodesToUpdate.Add(new NodeUpdateCall(currentNode, newNode));
                        nodesCreated++;
                    }
                }
            }else {
                UnSplitNode(currentNode);
            }
            if (_isTerraforming) {
                //add to queue
                _nodesToTerraform.Add(new NodeTerraformCall(currentNode, _terraformPoint, _terraformRadius, _terraformSpeed, _terraformAdd));
            }
            if (!currentNode.IsLeaf()){
                foreach (var childNode in currentNode.Children){
                    TravelTree(childNode, nodesCreated);
                }
            }
        }
        private void UnSplitNode(Node currentNode){
            //If already has no children Leave function
            if (currentNode.Children == null) return;

            // Kill Children Nodes if any
            var children = currentNode.Children;
            foreach (var childNode in children){
                UnSplitNode(childNode);
            }

            //Kill the current Child Nodes
            foreach (var childNode in children){
                //check update Queue and remove from the queue if the childNode is In it
                var nodeUpdateCall = new NodeUpdateCall(currentNode, childNode);
                if (_nodesToUpdate.Contains(nodeUpdateCall)) _nodesToUpdate.Remove(nodeUpdateCall);
                // Destroy the object
                childNode.DestroyNode();
            }
            currentNode.Children = null;
            //Add to update Queue
            _nodesToUpdate.Insert(0, new NodeUpdateCall(currentNode.ParentNode, currentNode));
        }
        //movement
        public float GetPlanetGravity() => planetData.PlanetGravity;
        public float GetPlanetRadius() => (planetRadius - (planetRadius * 0.025f)) * 0.5f;
        public void ToggleTreeTravel() => travelTree = !travelTree;
        #region Terraforming
        /// <summary>
        /// Saves modification data for a specific position key in the modification data dictionary, adding or updating the entry as needed.
        /// </summary>
        /// <param name="positionKey">The position key for the modification data.</param>
        /// <param name="modData">The modification data to be saved.</param>
        public void SaveModTreeData(Vector3 positionKey, float[] modData){
            if (modData == null) return;
            if (!_modDataDictionary.TryAdd(positionKey, modData)) {
                _modDataDictionary[positionKey] = modData;
            }
        }
        /// <summary>
        /// Retrieves modification data for a specific position key from the modification data dictionary. Returns a default array if the key is not found.
        /// </summary>
        /// <param name="positionKey">The position key for the modification data.</param>
        /// <returns>A float array containing the modification data for the specified position key, or a default array if the key is not found.</returns>
        public float[] TryGetModTreeData(Vector3 positionKey) {
            var treeData = new float[(ChunkSize + 1) * (ChunkSize + 1) * (ChunkSize + 1)];
            if (_modDataDictionary != null && _modDataDictionary.TryGetValue(positionKey, out var modTreeData)) {
                treeData = modTreeData;
            }
            return treeData;
        }
        /// <summary>
        /// Sets the parameters for a terraforming operation and enables the terraforming process.
        /// </summary>
        /// <param name="terraformPoint">The center point of the terraforming operation.</param>
        /// <param name="radius">The radius within which the terraforming operation will take place.</param>
        /// <param name="speed">The speed at which the terraforming operation will be applied.</param>
        /// <param name="addTerrain">A boolean indicating whether terrain should be added (true) or removed (false).</param>
        public void Terrafrom(float3 terraformPoint, float radius, float speed, bool addTerrain) {
            _terraformPoint = terraformPoint;
            _terraformRadius = radius;
            _terraformSpeed = speed / planetData.GroundToughness;
            _terraformAdd = addTerrain;
            _isTerraforming = true;
        }
        /// <summary>
        /// Disables the terraforming process.
        /// </summary>
        public void FinishTerrafrom() {
            _isTerraforming = false;
        }
        #endregion
        #region Debug
        private void OnDrawGizmos(){
            if (RootNode == null) {
                //Draw root node
                var size = ChunkSize * (int)Mathf.Pow(2, divisions - 1);
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(transform.position, Vector3.one * size);
                //Close planet size
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, planetRadius * 0.5f);
                return;
            }
            if (drawNodes) DrawTree(RootNode);
        }
        private static void DrawTree(Node parentNode){
            //Draw Node Outlines
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(parentNode.NodeBounds.center, parentNode.NodeBounds.size);

            //If the children nodes exist go through and draw them
            if (parentNode.Children == null) return;
            for (var i = 0; i < 8; i++){
                DrawTree(parentNode.Children[i]);
            }
        }
        //Validate
        private void OnValidate() {
            planetRecommendedRadius = GetRecommendedRadius();
            planetRecommendedDivisions = GetRecommendedDivisions();
        }
        //Debug UI
        private void OnGUI() {
            var style = new GUIStyle {
                fontSize = 25,
                normal = {
                    textColor = Color.white
                }
            };
            //Colors
            var totalCallsTextColor = ColorUtility.ToHtmlStringRGB(textColors.Evaluate(Mathf.Clamp01((float)(_nodesToTerraform.Count + _nodesToUpdate.Count) / 1000)));
            var updateTextColor = ColorUtility.ToHtmlStringRGB(textColors.Evaluate(Mathf.Clamp01((float)_nodesToUpdate.Count / 750)));
            var terraformTextColor = ColorUtility.ToHtmlStringRGB(textColors.Evaluate(Mathf.Clamp01((float)_nodesToTerraform.Count / 250)));
            
            var displayText = $"Total Calls Remaining: <color=#{totalCallsTextColor}>{(_nodesToTerraform.Count + _nodesToUpdate.Count)}</color>" +
                              $"\n  Nodes to Update: <color=#{updateTextColor}>{_nodesToUpdate.Count}</color>" +
                              $"\n  Nodes to Terraform: <color=#{terraformTextColor}>{_nodesToTerraform.Count}</color>";
            GUI.Label(new Rect(10f, 10f, 300f, 100f), displayText, style);
        }

        #endregion
    }
}
