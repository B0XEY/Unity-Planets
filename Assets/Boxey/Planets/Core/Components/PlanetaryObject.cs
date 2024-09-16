using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boxey.Attributes;
using Boxey.Planets.Core.Classes;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Boxey.Planets.Core.Components {
    [AddComponentMenu("Boxey/Planetary Object", -10000)]
    public class PlanetaryObject : MonoBehaviour {
        private enum VoxelSize {
            SmallVoxels = 2,
            RegularVoxels = 4,
            LargeVoxels = 8,
        }
        private const int CurveSamples = 2048; //Larger means less terrain artifacts
        private const int MaxNewNodes = 5;
        public const int ChunkSize = 16;
        public const float ValueGate = .15f;
        public const float CreateGate = .5f;
        public const bool SmoothTerrain = false;
        
        private Camera _playerCamera;
        private Vector3 _playersLastPosition;
        private GUIStyle _style;
        
        private List<Node> _activeNodes;
        private List<List<UpdateCalls.NodeInfo>> _activeNodesBatches;
        private List<UpdateCalls.NodeUpdateCall> _nodesToUpdate;
        public HashSet<UpdateCalls.TerraformInfo> TerraformInfo { get; private set; }
        public Reporter PlanetReport { get; private set; }
        
        public Transform ChunkHolder { get; private set; }
        public GameObject NodePrefab { get; private set; }
        public Node RootNode { get; private set; }
        public float RootNodeScale { get; private set; }
        public Vector3 StartingPosition { get; private set; }
        public float VoxelScale => (float)voxelSize / 4f;
        public float[] NoiseCurves { get; private set; }
        public PlanetData PlanetData => planetData;
        public int Seed => seed;

        public Material ChunkMaterial => (useDebug && useWireFrame) ? wireFrameMaterial :planetData.PlanetMaterial;
        public float PlanetRadius => planetRadius;
        public float PlanetGravity => planetData.PlanetGravity;
        public int MaxPlanetDivisions => planetDivisions;
        public int GrassPerTriangle => bladesPerTriange;
        
        public ComputeShader NoiseComputeShader => Resources.Load<ComputeShader>("Computes/PlanetNoise");
        public ComputeShader FoliageComputeShader => Resources.Load<ComputeShader>("Computes/FoliageCompute");

        private static readonly int CenterID = Shader.PropertyToID("_Center");
        private static readonly int SeedID = Shader.PropertyToID("_Seed");
        private static readonly int RadiusID = Shader.PropertyToID("_Radius");
        private static readonly int SteepnessThresholdID = Shader.PropertyToID("_Steepness_Threshold");
        
        public float GetSteepnessValue() => ChunkMaterial.GetFloat(SteepnessThresholdID);
        private float GetRecommendedRadius() => 112.5f * ChunkSize * Mathf.Pow(2, planetDivisions - 8);
        private float GetRecommendedDivisions() => Mathf.CeilToInt(8 + Mathf.Log(planetRadius / (112.5f * ChunkSize)) / Mathf.Log(2));
        private void RandomSeed() {
            seed = Random.Range(0, 999999);
        }

        [Header("Planet Data"), Line] 
        [SerializeField, Tooltip("Use a negative value for a random seed")] private int seed;
        [SerializeField, FoldableInspector] private PlanetData planetData;

        [Header("Tree Settings"), Line] 
        [SerializeField, OnChanged(nameof(GetRecommendedRadius))] private int planetDivisions = 8;
        [SerializeField, OnChanged(nameof(GetRecommendedDivisions))] private float planetRadius = 1800;
        [SerializeField, Label("Recommended Divisions"), ShowOnly] private float planetRecommendedDivisions;
        [SerializeField, Label("Recommended Radius"), ShowOnly] private float planetRecommendedRadius;

        [Header("Performance Settings"), Line] 
        [SerializeField, EnumButtons] private VoxelSize voxelSize = VoxelSize.RegularVoxels;
        [SerializeField, Range(0.01f, 1f)] private float splitMultiplier = 0.25f;
        [SerializeField, Range(1, 2.5f)] private float splitRadius = 1.45f;
        [Space(5f)] 
        [SerializeField] private float updateDistance = .5f;
        [SerializeField] private int maxNodeUpdatesPerFrame = 10;
        [SerializeField] private int nodeBatchSize = 10;
        [Space]
        [SerializeField] private int batchSize;
        [SerializeField] private int bladesPerTriange;
        [SerializeField] private Mesh grassMesh;
        [SerializeField] private Material grassMaterial;

        [Header("Debug Settings"), Line] 
        [SerializeField] private bool useDebug;
        [Space(5f)] 
        [SerializeField, ShowIf("useDebug")] private Gradient textColors;
        [SerializeField, ShowIf("useDebug")] private bool drawNodes;
        [SerializeField, ShowIf("useDebug")] private bool travelTree;
        [Space(5f)] 
        [SerializeField, ShowIf("useDebug")] private bool useWireFrame;
        [SerializeField, ShowIf("useDebug"), ShowIf("useWireFrame")] private Material wireFrameMaterial;

        private void Awake() {
            //Seed + starting position setting
            StartingPosition = transform.position;
            if (seed < 0) {
                RandomSeed();
            }
            if (!_playerCamera) {
                _playerCamera = Helpers.GetCamera;
            }
            //Make Holder And Atmosphere Object
            ChunkHolder = new GameObject("Terrain Holder").transform;
            ChunkHolder.SetParent(transform);
            ChunkHolder.SetAsFirstSibling();
            ChunkHolder.transform.localPosition = Vector3.zero;
            //Load Node
            NodePrefab = Resources.Load<GameObject>("Boxey/Node Prefab");
            _style = new GUIStyle {
                fontSize = 25,
                normal = {
                    textColor = Color.white
                }
            };
            //Create Lists / HashSets
            _nodesToUpdate = new List<UpdateCalls.NodeUpdateCall>();
            TerraformInfo = new HashSet<UpdateCalls.TerraformInfo>();
            _activeNodes = new List<Node>();
            //Set Material Values
            planetData.PlanetMaterial.SetFloat(SeedID, seed);
            planetData.PlanetMaterial.SetFloat(RadiusID, planetRadius * .5f - 35f);
            planetData.PlanetMaterial.SetVector(CenterID, StartingPosition);
            //Create Layer Curves
            GenerateNoiseCurves();
            //Other stuff
            _playersLastPosition = _playerCamera.transform.position;
            //report file stuff
            var size = Enum.GetName(typeof(VoxelSize), voxelSize)?.Split("V");
            PlanetReport = new Reporter(seed, planetDivisions, ChunkSize, VoxelScale, planetRadius, size[0]);
        }
        private void Start() {
            //Create root octree node
            RootNode = new Node(this, null, planetDivisions, StartingPosition);
            RootNodeScale = RootNode.NodeScale();
            TravelTree(RootNode);
            _nodesToUpdate.Add(new UpdateCalls.NodeUpdateCall(null, RootNode));
            //Atmosphere Mumbo Jumbo
            transform.GetChild(1).gameObject.TryGetComponent<AtmosphereEffect>(out var atmosphere);
            if (!atmosphere) {
                return;
            }
            atmosphere.sun = GameObject.FindWithTag("Sun").transform.GetChild(0);
            atmosphere.profile = planetData.Atmosphere;
            atmosphere.atmosphereScale = planetData.AtmosphereScale;
            atmosphere.cutoffDepth = 1f;
        }

        private void Update() {
            //Update the nodes that need it
            HandelUpdateCalls();
            HandelNodeUpdate();
            if (_activeNodes.Count > 1) {
                RootNode.DestroyNode();
            }
            if (travelTree) {
                TravelTree(RootNode, checkDistance: true);
            }
        }

        //Main update calls
        private void HandelUpdateCalls() {
            if (_nodesToUpdate.Count <= 0) {
                return;
            }
            var updateAmount = Mathf.Min(_nodesToUpdate.Count, maxNodeUpdatesPerFrame);
            var nodesToProcess = _nodesToUpdate.GetRange(0, updateAmount);
            foreach (var nodeUpdate in nodesToProcess) {
                _nodesToUpdate.Remove(nodeUpdate);
                nodeUpdate.NodeToUpdate?.UpdateNode();
                nodeUpdate.ParentNode?.TrySplitNode();
            }
        }
        private void HandelNodeUpdate() {
            if (_activeNodes == null || _activeNodes.Count == 0) {
                return;
            }
            var mapSize = (int)(ChunkSize / VoxelScale);
            //batch chunks if not batched
            _activeNodesBatches = Batcher.CreateTerraformBatches(_activeNodes, nodeBatchSize, TerraformInfo.Count);
            //call Jobs
            var outputMaps = new List<float2[]>();
            foreach (var batch in _activeNodesBatches) {
                var jobOutput = JobManager.BatchTerraformNodes(mapSize, batchSize, batch, TerraformInfo);
                outputMaps.AddRange(jobOutput);
            }
            var lastIndex = 0;
            for (int i = 0; i < _activeNodes.Count; i++) {
                /* Grass
                 if (_activeNodes[i].NodeGrass != null && _activeNodes[i].NodeGrass.Length != 0 && _activeNodes[i].IsLeaf()) {
                    var matrices = _activeNodes[i].NodeGrass;
                    var batchMatrices = new List<Matrix4x4>(batchSize);
                    var batches = Mathf.CeilToInt((float)matrices.Length / batchSize);
                    for (var b = 0; b < batches; b++) {
                        batchMatrices.Clear(); // Clear the list for each batch
                        var startIndex = b * batchSize;
                        var count = Mathf.Min(matrices.Length - startIndex, batchSize);

                        for (int i = 0; i < count; i++) {
                            batchMatrices.Add(matrices[startIndex + i]);
                        }
                        Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, batchMatrices.ToArray(), count);
                    }
                }
                */
                if (TerraformInfo.Count == 0) {
                    return;
                }
                if (_activeNodes[i].GetTerraformIndex() != TerraformInfo.Count) {
                    _activeNodes[i].TryForceTerraform(outputMaps[i - lastIndex]);
                }else {
                    lastIndex++;
                }
            }
        }
        //Main octree travel
        private void TravelTree(Node currentNode, int nodesCreated = 0, bool checkDistance = false) {
            if (checkDistance) {
                //Distance check so we don't update every frame only on the root node so the whole tree updates
                var distanceTraveled = (_playerCamera.transform.position - _playersLastPosition).sqrMagnitude;
                if (distanceTraveled < updateDistance * updateDistance) {
                    return;
                }
                _activeNodes.Clear();
                _activeNodesBatches.Clear();
            }
            _playersLastPosition = _playerCamera.transform.position;
            _activeNodes.Add(currentNode);
            //normal tree update
            if (nodesCreated >= 8 * MaxNewNodes || currentNode.Divisions <= 1) {
                return;
            }
            var distanceFromNode = (_playerCamera.transform.position - currentNode.NodeWorldPosition()).sqrMagnitude;
            var splitDistance = ChunkSize * splitMultiplier + splitRadius * currentNode.NodeScale();
            splitDistance *= splitDistance;

            if (distanceFromNode < splitDistance) {
                if (currentNode.IsLeaf()) {
                    //split
                    currentNode.Children = new Node[8];
                    var newDivisions = currentNode.Divisions - 1;
                    for (var i = 0; i < 8; i++) {
                        var newNode = new Node(this, currentNode, newDivisions, VoxelTables.NodeOffsets[i]);
                        currentNode.Children[i] = newNode;
                        _nodesToUpdate.Add(new UpdateCalls.NodeUpdateCall(currentNode, newNode));
                        nodesCreated++;
                    }
                }
            }else {
                UnSplitNode(currentNode);
            }
            if (currentNode.IsLeaf()) {
                return;
            }
            foreach (var childNode in currentNode.Children) {
                TravelTree(childNode, nodesCreated);
            }
        }
        private void UnSplitNode(Node currentNode) {
            //If already has no children Leave function
            if (currentNode.Children == null) {
                return;
            }
            // Kill Children Nodes if any
            var children = currentNode.Children;
            foreach (var childNode in children) {
                UnSplitNode(childNode);
            }
            //Kill the current Child Nodes
            foreach (var childNode in children) {
                //check update Queue and remove from the queue if the childNode is In it
                var nodeUpdateCall = new UpdateCalls.NodeUpdateCall(currentNode, childNode);
                if (_nodesToUpdate.Contains(nodeUpdateCall)) {
                    _nodesToUpdate.Remove(nodeUpdateCall);
                }
                // Destroy the object
                childNode.DestroyNode();
            }
            currentNode.Children = null;
            //Add to update Queue
            currentNode.UpdateNode();
            _nodesToUpdate.Insert(0, new UpdateCalls.NodeUpdateCall(currentNode.ParentNode, currentNode));
        }
        //Terraforming
        public void Terrafrom(float3 terraformPoint, float radius, float speed, bool addTerrain, float2 color) {
            var info = new UpdateCalls.TerraformInfo(terraformPoint, new float4(radius, speed / planetData.GroundToughness, color), Time.deltaTime, addTerrain);
            TerraformInfo.Add(info);
        }
        //Extra
        private void GenerateNoiseCurves() {
            NoiseCurves ??= new float[CurveSamples * planetData.NoiseLayers.Count];
            var index = 0;
            foreach (var layer in planetData.NoiseLayers) {
                var layerCurve = layer.HeightCurve.GenerateCurveArray(CurveSamples);
                for (var j = 0; j < CurveSamples; j++) {
                    NoiseCurves[index] = layerCurve[j];
                    index++;
                }
            }
        }
        public void ToggleTravel(bool value) => travelTree = value;
        #region Debug
        private void OnDrawGizmos() {
            if (!useDebug) {
                return;
            }
            if (RootNode == null) {
                //Draw root node
                var size = ChunkSize * (int)Mathf.Pow(2, planetDivisions - 1);
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(transform.position, Vector3.one * size);
                //Close planet size
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, planetRadius * 0.5f);
                return;
            }

            if (drawNodes) {
                DrawTree(RootNode);
            }
        }
        private static void DrawTree(Node parentNode) {
            //Draw Node Outlines
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(parentNode.NodeBounds.center, parentNode.NodeBounds.size);

            //If the children nodes exist go through and draw them
            if (parentNode.Children == null) {
                return;
            }
            for (var i = 0; i < 8; i++) {
                DrawTree(parentNode.Children[i]);
            }
        }
        private void OnValidate() {
            planetRecommendedRadius = GetRecommendedRadius();
            planetRecommendedDivisions = GetRecommendedDivisions();
            if (!Application.isPlaying && transform.childCount == 0) {
                var atmosphere = new GameObject("Atmosphere").transform;
                atmosphere.SetParent(transform);
                atmosphere.transform.localPosition = Vector3.zero;
                atmosphere.gameObject.AddComponent<AtmosphereEffect>();
                atmosphere.gameObject.TryGetComponent<AtmosphereEffect>(out var atmosphereScript);
                var sunOBj = GameObject.FindWithTag("Sun").transform.GetChild(0);
                if (!sunOBj) {
                    Debug.LogError("There is currently no object with the tag 'Sun'. Please make one and assign in the inspector for the atmosphere: " + atmosphereScript);
                }
                atmosphereScript.sun = sunOBj;
                atmosphereScript.directional = !sunOBj;
                atmosphereScript.cutoffDepth = 1f;
                if (!planetData) {
                    Debug.LogError("Please assign a PlanetData object");
                    return;
                }
                atmosphereScript.profile = planetData.Atmosphere;
                atmosphereScript.atmosphereScale = planetData.AtmosphereScale;
            }else if (!Application.isPlaying && planetData && transform.childCount == 10) {
                transform.GetChild(0).gameObject.TryGetComponent<AtmosphereEffect>(out var atmosphereScript);
                atmosphereScript.profile = planetData.Atmosphere;
                atmosphereScript.atmosphereScale = planetData.AtmosphereScale;
            }
        }
        //Debug UI
        private void OnGUI() {
            //check distance to see check distance
            var dst = (_playerCamera.transform.position - transform.position).sqrMagnitude;
            var maxDst = (ChunkSize + ChunkSize) * splitMultiplier + splitRadius * RootNode.NodeScale();
            maxDst *= maxDst;
            if (dst > maxDst || !useDebug) {
                return;
            }
            //Position Colors
            var position = _playerCamera.transform.position;
            var XColor = ColorUtility.ToHtmlStringRGB(Color.red);
            var YColor = ColorUtility.ToHtmlStringRGB(Color.green);
            var ZColor = ColorUtility.ToHtmlStringRGB(Color.blue);
            var positionText = $"Camera Position: <color=#{XColor}>{position.x:N1}</color>, <color=#{YColor}>{position.y:N1}</color>, <color=#{ZColor}>{position.x:N1}</color>";
            //Updates Colors
            var updateCalls = Mathf.Clamp(_nodesToUpdate.Count - 1, 0, float.MaxValue);
            var totalCallsTextColor = ColorUtility.ToHtmlStringRGB(textColors.Evaluate(Mathf.Clamp01(updateCalls / _activeNodes.Count)));
            var displayText = $"{positionText}" +
                              $"\nCurrent Planet: {transform.name}" +
                              $"\n  Active Node Count: {_activeNodes.Count}" +
                              $"\n  Active Node Batches: {_activeNodesBatches.Count}" +
                              $"\n  Total Calls Remaining: <color=#{totalCallsTextColor}>{updateCalls}</color>" +
                              $"\n  Total Terraforms: {TerraformInfo.Count}";
            GUI.Label(new Rect(10f, 10f, 300f, 100f), displayText, _style);
        }
        #endregion
        #region Reports
        private void OnApplicationQuit() {
            PlanetReport.SaveToFile(gameObject.name);
        }
        private void OnEnable() {
            Application.logMessageReceived += PlanetReport.OnDebugLog;
        }
        private void OnDisable() {
            Application.logMessageReceived -= PlanetReport.OnDebugLog;
        }
        [ContextMenu("Open Report Folder")]
        private void OpenFileLocation() {
            var reportsFolder = Path.Combine(Application.persistentDataPath, "Planet Reports");
            if (!Directory.Exists(reportsFolder)) {
                Directory.CreateDirectory(reportsFolder);
            }
            Application.OpenURL(reportsFolder);
        }
        #endregion
    }
}