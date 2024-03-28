using System.Collections.Generic;
using Boxey.Attributes;
using Boxey.Planets.Core.Static;
using Boxey.Planets.Core.Generation.Data_Objects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Boxey.Planets.Core.Generation {
    public class Planet : MonoBehaviour{
        private Vector3 _playersLastPosition;
        
        //Mod data
        private Dictionary<Vector3, float[]> _modDataDictionary;
        
        //Getters Values
        public Node RootNode { get; private set; }
        public Vector3 StartingPosition { get; private set; }
        public PlanetData Data => data;
        public GameObject NodePrefab => nodePrefab;
        public int ChunkSize => chunkSize;
        public int Seed => seed;
        public float ValueGate => valueGate;
        public float CreateGate => createGate;
        public Transform ChunkHolder => chunkHolder;
        public Material ChunkMaterial => worldMat;
        public float PlanetRadius => planetRadius;
        public int CurrentDivisions => divisions;
        //Getters Functions
        public float GetSandHeight() => ChunkMaterial.GetFloat("_Sand_Height");
        public float GetSteepnessValue() => ChunkMaterial.GetFloat("_Steepness_Threshold");
        
        private float GetRecommendedRadius() => (112.5f * chunkSize) * Mathf.Pow(2, divisions - 8);
        private float GetRecommendedDivisions() => Mathf.CeilToInt(8 + Mathf.Log(planetRadius / (112.5f * chunkSize)) / Mathf.Log(2));
        
        private void RandomSeed() => seed = Random.Range(-999999, 999999);

        [Header("Planet Data"), Line]
        [SerializeField] private int seed;
        [SerializeField] private bool randomSeed;
        [SerializeField] private PlanetData data;
        
        [Header("Tree Settings"), Line]
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private float planetRadius = 1800;
        [Space(5f)]
        [SerializeField] private float valueGate = .15f;
        [SerializeField] private float createGate = 1;
        [Space(5f)]
        [SerializeField] private Transform chunkHolder;
        [SerializeField] private Transform player;
        [SerializeField] private Material worldMat;
        
        [Header("Performance Settings"), Line]
        [SerializeField] private float updateDistance = .5f;
        [SerializeField] private int maxNodeCreationsPerFrame = 4;
        [Space(5f)]
        [SerializeField] private int divisions = 8;
        [SerializeField] private float splitMultiplier = 0.25f;
        [SerializeField] private float splitRadius = 1.45f;
        [Space(5f)]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private bool drawNodes;

        private void Start(){
            StartingPosition = transform.position;
            if (randomSeed) RandomSeed();
            //Create dictionary. To-Do: get the dictionary from a file that contains the data
            _modDataDictionary = new Dictionary<Vector3, float[]>();
            worldMat.SetVector("_Center", StartingPosition);
            //Create root octree node
            RootNode = new Node(this, null, divisions, StartingPosition, true);
            TravelTree(RootNode);
        }

        private void Update() {
            if (!(Vector3.Distance(_playersLastPosition, player.transform.position) > updateDistance)) return;
            _playersLastPosition = player.transform.position;
            
            TravelTree(RootNode);
        }
        private void TravelTree(Node currentNode, int nodesCreated = 0) {
            if (nodesCreated > maxNodeCreationsPerFrame) return;
            if (currentNode.Divisions <= 1) {
                // No need to split further
                currentNode.TryGeneration();
                return;
            }
            
            //Says if we make a new node or Split the current node into parts
            var madeNewNode = false;
            //Distance from the camera to the node
            var distanceFromNode = Vector3.Distance(player.position, currentNode.NodeWorldPosition());
            //Distance that it need to be less than to split
            var splitDistance = chunkSize * splitMultiplier + splitRadius * currentNode.NodeScale();
                
            if (distanceFromNode <= splitDistance){
                if (currentNode.Children == null){
                    //Reset the Current Node as it is not a leaf node and need no mesh
                    currentNode.PrepareSplit();
                    //Update bool to true so we know we made new nodes this frame
                    madeNewNode = true;
                        
                    //Create children or split the node
                    currentNode.Children = new Node[8];
                    var newDivisions = currentNode.Divisions - 1;
                    for (var i = 0; i < 8; i++){
                        currentNode.Children[i] = new Node(this, currentNode, newDivisions, VoxelTables.NodeOffsets[i].ToVector3());
                    }
                }
            }else{
                //Runs when we are no longer in range of the node and can unSplit the tree
                DestroyChildren(currentNode);
            }

            if (madeNewNode) nodesCreated++;
            //If Node Has Children Run to check them in needing split
            if (currentNode.Children != null){
                foreach (var childNode in currentNode.Children){
                    //Travel the octree from the child node testing for splits
                    TravelTree(childNode, nodesCreated);
                }
            }else{
                //If there is no children we want to make sure that if we need the mesh for it we rebuild that mesh
                currentNode.TryGeneration();
            }
        }
        private static void DestroyChildren(Node parentNode){
            //If already has no children Leave function
            if (parentNode.Children == null) return;

            // Kill Children Nodes if any
            foreach (var childNode in parentNode.Children){
                DestroyChildren(childNode);
            }

            //Kill the current Child Nodes
            foreach (var childNode in parentNode.Children){
                // Destroy the object
                childNode.DestroyNode();
            }
            parentNode.Children = null;
        }
        //Mod Tree Data
        public void SaveModTreeData(Vector3 positionKey, float[] modData){
            //Save data
            if (modData == null) return;
            if (!_modDataDictionary.TryAdd(positionKey, modData)) {
                _modDataDictionary[positionKey] = modData;
            }
        }
        public float[] TryGetModTreeData(Vector3 positionKey) {
            //Pull data from dictionary
            return (_modDataDictionary != null && _modDataDictionary.TryGetValue(positionKey, out var modTreeData)) ? modTreeData : new float[(chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1)];
        }

        // Draw / Debug the tree
        private void OnDrawGizmos(){
            if (RootNode == null) return;
            if (drawNodes) DrawTree(RootNode);
        }
        private static void DrawTree(Node parentNode){
            //Draw Node Outlines
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(parentNode.NodePosition(), Vector3.one * parentNode.NodeScale());

            //If the children nodes exist go though and draw them
            if (parentNode.Children == null) return;
            for (var i = 0; i < 8; i++){
                DrawTree(parentNode.Children[i]);
            }
        }
    }
}
