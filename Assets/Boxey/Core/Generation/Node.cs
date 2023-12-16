using System;
using System.Collections.Generic;
using Boxey.Core.Static;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Boxey.Core.Generation {
    public class Node{
        //Private Data
        private readonly Octree m_octree;
        private GameObject m_nodeObject;
        private List<GameObject> m_nodeFoliage;
        private readonly Node m_parent;
        private readonly Vector3 m_offset;
        
        //Public Data
        public Node[] Children;
        public readonly int Divisions;
        
        //Terraforming data done to chunk
        private float[] m_modData;

        private Mesh m_nodeMesh;
        private bool m_isGenerated;
        private bool m_isSplit;

        public Node(Octree octree, Node parent, int divisions, Vector3 offset, bool isRoot = false){
            m_octree = octree;
            m_parent = parent;
            Divisions = divisions;
            m_offset = offset;
            if (!isRoot) CreateMesh();
        }
        
        // Mesh functions
        public void CreateMesh(){
            //Only generate the terrain when the node is the smallest and has no children
            if (!IsLeaf() || m_isGenerated){
                m_isGenerated = true;
                return;
            }
            m_isGenerated = true;
            //Mod data retrieval or creation
            var nodePosition = NodePosition();
            m_modData = m_octree.TryGetModTreeData(nodePosition);
            //Build Terrain - Get Noise Map
            var meshFunction = new NodeMarching(m_octree.ChunkSize, NodeScale(), m_octree.ValueGate, m_octree.CreateGate, GetMap(), m_modData);
            meshFunction.Generate();
            //If there is no mesh object we do not create the gameObject
            if (meshFunction.VerticesArray.Length == 0){
                return;
            }
            
            m_nodeMesh = new Mesh{
                name = nodePosition.ToString(),
                vertices = meshFunction.VerticesArray,
                normals = meshFunction.NormalArray,
                triangles = meshFunction.Triangles
            };

            //Creat GameObject because it is a mesh
            m_nodeObject = Object.Instantiate(m_octree.NodePrefab, m_octree.ChunkHolder);
            m_nodeObject.layer = 3;
            m_nodeObject.name = "Chunk: " + (nodePosition - m_octree.transform.position);
            m_nodeObject.transform.localPosition = (nodePosition - m_octree.StartingPosition);
            m_nodeObject.TryGetComponent<MeshFilter>(out var filter);
            m_nodeObject.TryGetComponent<MeshRenderer>(out var renderer);
            filter.sharedMesh = m_nodeMesh;
            renderer.sharedMaterial = m_octree.ChunkMaterial;
            //Create the Mesh Collider for the object if it is one of the last 2 divisions
            if (Divisions <= 2){
                m_nodeObject.TryGetComponent<MeshCollider>(out var collider);
                collider.sharedMesh = m_nodeMesh;
            }
            
            //Spawn Foliage
            if (m_octree.UseFoliage && Divisions <= m_octree.FoliageDistance){
                m_nodeObject.TryGetComponent<MeshCollider>(out var collider);
                collider.sharedMesh = m_nodeMesh;
                SpawnFoliage();
            }
        }
        public void DestroyNode(){
            if (m_modData != null) m_octree.SaveModTreeData(NodePosition(), m_modData);
            m_modData = null;
            m_isGenerated = false;
            //Destroy Foliage
            if (m_nodeFoliage != null){
                m_nodeFoliage.Clear();
                m_nodeFoliage = null;
            }
            //Destroy Meshes
            m_isSplit = false;
            Object.Destroy(m_nodeMesh);
            m_nodeMesh = null;
            Object.Destroy(m_nodeObject);
            m_nodeObject = null;
        }
        public void PrepareSplit(){
            m_isSplit = true;
            if (m_nodeObject != null) m_nodeObject.SetActive(false);
            if (m_nodeFoliage != null){
                m_nodeFoliage.Clear();
                m_nodeFoliage = null;
            }
        }
        
        // Noise functions
        private float3 GetSamplePosition(){
            var nodePosition = NodePosition();
            var offset = (float3)m_octree.StartingPosition;
            return new float3((nodePosition.z - offset.z) + offset.x, (nodePosition.y - offset.y) + offset.y, (nodePosition.x - offset.x) + offset.z);
        }
        public float[] GetMap() => JobManager.GetPlanetNoiseMap(m_octree.ChunkSize, GetSamplePosition(), 
            m_octree.PlanetRadius, m_octree.RootNode.NodePosition(), NodeScale(), m_octree.Seed, m_octree.Data);
        // Terraforming functions
        public float[] GetTerraformingMap() => m_modData;
        public void CompleteTerraforming(float[] data){
            // if the new map is not changed we should not remake the mesh
            if (m_modData == data) return;
            // edit the current data
            m_isGenerated = false;
            m_modData = data;
            CreateMesh();
        }
        
        //Node Functions
        private bool IsLeaf() => Children == null;
        public void TryGeneration(){ 
            if (!m_isGenerated){
                CreateMesh();
                m_isSplit = false;
                return;
            }
            if (!m_isSplit) return;
            if (m_nodeObject == null){
                CreateMesh();
                m_isSplit = false;
            }else {
                m_nodeObject.SetActive(true);
                SpawnFoliage();
                m_isSplit = false;
            }
        }
        
        private int NodeResolution() => (int)Mathf.Pow(2, Divisions - 1);
        public int NodeScale() => m_octree.ChunkSize * NodeResolution();
        public Vector3 NodePosition(){
            if (m_parent == null){
                return m_offset;
            }

            var nodeScale = NodeScale();
            return (m_offset * nodeScale) - (Vector3.one * (nodeScale / 2f)) + m_parent.NodePosition();
        }
        
        //Foliage
        private void SpawnFoliage(){
            m_nodeFoliage ??= new List<GameObject>();
            m_nodeFoliage.Clear();
            Random.InitState(m_octree.Seed);
            var nodeScale = NodeScale();
            var range = new Vector2(-nodeScale / 2f, nodeScale / 2f);
            int spawnAttempts = 0;
            var treePosition = m_octree.StartingPosition;
            for (int i = 0; i < m_octree.MaxLargeFoliage * (nodeScale / m_octree.ChunkSize);){
                //Gets random point in the nodes area
                var objectNodePosition = new Vector3(range.Random(),range.Random(),range.Random());
                var objectWorldPosition = objectNodePosition + NodePosition();
                // Get the direction to the center of the planet for the ray
                var rayDirection = treePosition -  objectWorldPosition;
                var direction = rayDirection / rayDirection.magnitude;
                var ray = new Ray(objectWorldPosition, direction);
                //Cast the ray too the planet center
                if (Physics.Raycast(ray, out var hit, nodeScale * 1.25f)){
                    var hitHeight = Vector3.Distance(treePosition, hit.point);
                    var directionFromCenter = (hit.point - treePosition).normalized;
                    var angle = Vector3.Angle(hit.normal, directionFromCenter);
                    if (hit.transform.name.Contains("Chunk:") && hitHeight > m_octree.GetSandHeight() && angle < 35){
                        //Spawn the object and set the position and rotation
                        var obj = Object.Instantiate(m_octree.GetRandomLargeObject(), hit.point, Quaternion.identity,
                            m_nodeObject.transform);
                        obj.transform.up = hit.normal;
                        obj.transform.localScale = m_octree.GetRandomScale();
                        m_nodeFoliage.Add(obj);
                        i++;
                    }else{
                        spawnAttempts++;
                        if(spawnAttempts % 5 == 0) {
                            i++;
                        }
                    }
                }else {
                    spawnAttempts++;
                    if(spawnAttempts % 5 == 0) {
                        i++;
                    }
                }
            }
        }
    }
}