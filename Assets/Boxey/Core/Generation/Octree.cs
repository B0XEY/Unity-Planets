using System;
using System.Collections.Generic;
using Boxey.Core.Generation.Data_Objects;
using Boxey.Core.Static;
using Unity.Burst;
using Boxey.Core.Editor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Boxey.Core.Generation {
    public class Octree : MonoBehaviour{
        private string m_debugText;
        
        //Mod data
        private Dictionary<Vector3, float[]> m_modDataDictionary;
        
        //Getters Values
        public Node RootNode { get; private set; }
        public Vector3 StartingPosition { get; private set; }
        public PlanetData Data => data;
        public GameObject NodePrefab => nodePrefab;
        public int ChunkSize => chunkSize;
        public int Seed => seed;
        public bool UseFoliage => hasFoliage;
        public int FoliageDistance => foliageDistance;
        public int MaxSmallFoliage => maxSmallObjects;
        public int MaxLargeFoliage => maxLargeObjects;
        public float ValueGate => valueGate;
        public float CreateGate => createGate;
        public Transform ChunkHolder => chunkHolder;
        public Material ChunkMaterial => worldMat;
        public float PlanetRadius => planetRadius;
        //Getters Functions
        public float GetSandHeight() => ChunkMaterial.GetFloat("_Sand_Height");
        public float GetSteepnessValue() => ChunkMaterial.GetFloat("_Steepness_Threshold");
        public float GetRandomRotation() => rotationRange.Random();
        public Vector3 GetRandomScale() => new (scaleRange.Random(), scaleRange.Random(), scaleRange.Random());
        public GameObject GetRandomSmallObject() => smallObjects.Random();
        public GameObject GetRandomLargeObject() => largeObjects.Random();
        
        private void RandomSeed() => seed = Random.Range(-999999, 999999);

        [Header("Planet Data")]
        [SerializeField] private int seed;
        [SerializeField] private bool randomSeed;
        [SerializeField] private PlanetData data;
        
        [Header("Planet Settings")]
        [SerializeField] private int chunkSize = 16;
        [Space(5f)]
        [SerializeField] private float planetRadius = 1000;
        [Space(5f)]
        [SerializeField] private float valueGate = .15f;
        [SerializeField] private float createGate = .5f;
        [SerializeField] private Transform chunkHolder;
        [SerializeField] private Transform player;
        [Space(5f)]
        [SerializeField] private Material worldMat;
        
        [Header("Foliage Settings")]
        [SerializeField] private bool hasFoliage = true;
        [SerializeField, ShowIf("hasFoliage")] private int foliageDistance;
        [Space(5f)]
        [SerializeField, ShowIf("hasFoliage")] private Vector2 rotationRange = new Vector2(-179, 179);
        [SerializeField, ShowIf("hasFoliage")] private Vector2 scaleRange = new Vector2(0.85f, 1.15f);
        [Space(5f)]
        [SerializeField, Min(0), ShowIf("hasFoliage")] private int maxLargeObjects;
        [SerializeField, Min(0), ShowIf("hasFoliage")] private int maxSmallObjects;
        [Space(7.5f)]
        [SerializeField, ShowIf("hasFoliage")] private GameObject[] smallObjects;
        [SerializeField, ShowIf("hasFoliage")] private GameObject[] largeObjects;
        
        [Header("Performance Settings")]
        [SerializeField, Label("Creation Limit")] private int maxNodeCreationsPerFrame = 4;
        [Space(5f)]
        [SerializeField] private int divisions = 8;
        [SerializeField] private float splitMult = 0.25f;
        [SerializeField] private float splitRadius = 1.45f;
        [Space(5f)]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private bool drawNodes;

        private void Start(){
            StartingPosition = transform.position;
            if (randomSeed) RandomSeed();
            //Create dictionary. To-Do: get the dictionary from a file that contains the data
            m_modDataDictionary = new Dictionary<Vector3, float[]>();
            worldMat.SetVector("_Center", StartingPosition);
            //Create root octree node
            RootNode = new Node(this, null, divisions, StartingPosition, true);
            RootNode.CreateMesh();
        }

        private void Update(){
            TravelTree(RootNode, 0);
        }
        [BurstCompile]
        private void TravelTree(Node currentNode, int nodesCreated){
            if (nodesCreated > maxNodeCreationsPerFrame) return;
            //Says if we make a new node or Split the current node into parts
            var madeNewNode = false;
            if (currentNode.Divisions > 1) {
                //Distance from the camera to the node
                var distanceFromNode = Vector3.Distance(player.position, currentNode.NodePosition());
                //Distance that it need to be less than to split
                var splitDistance = chunkSize * splitMult + splitRadius * currentNode.NodeScale();
                
                if (distanceFromNode <= splitDistance){
                    if (currentNode.Children == null){
                        //Reset the Current Node as it is not a leaf node and need no mesh
                        currentNode.PrepareSplit();
                        //Update bool to true so we know we made new nodes this frame
                        madeNewNode = true;
                        
                        //Create children or split the node
                        currentNode.Children = new Node[8];
                        var newDivisions = currentNode.Divisions - 1;
                        for (int i = 0; i < 8; i++){
                            currentNode.Children[i] = new Node(this, currentNode, newDivisions, NodeOffsets[i]);
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
            if (!m_modDataDictionary.TryAdd(positionKey, modData)) {
                m_modDataDictionary[positionKey] = modData;
            }
        }
        public float[] TryGetModTreeData(Vector3 positionKey){
            //Pull data from dictionary
            return m_modDataDictionary.TryGetValue(positionKey, out var modTreeData) ? modTreeData : null;
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

        private static readonly Vector3[] NodeOffsets = {
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 1f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 1f)
        };
        
    }
}
