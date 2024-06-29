using System.Collections.Generic;
using System.Linq;
using Boxey.Planets.Core.Generation.Data_Objects;
using Unbegames.Noise;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Static {
    public static class JobManager {
        private const float Threshold = 0.001f; 
        private const int CurveSamples = 512; //Says how many times the height curves get sampled when making the arrays for them. larger means less artifacts.
        //General Functions
        private static float CalculateNoiseValueClamp(NoiseSettings settings) {
            var clamp = 0f;
            var amplitude = 1f;
            for (var o = 0; o < settings.Octaves; o++) {
                clamp += 1f * amplitude;
                amplitude *= settings.Persistence;
            }
            return clamp;
        }
        //Terraforming
        public static float[] GetTerraformMap(int chunkRes, int nodeScale, float3 noisePosition, float[] modData, float3 terraformPoint, float3 terraformData) {
            var mapSize = (chunkRes + 1);
            var arrayLength = mapSize * mapSize * mapSize;
            //make sure mod data is set
            modData ??= new float[arrayLength];
            //Job Set up
            var terraformJob = new VoxelJobs.Terraform {
                ChunkResolution = mapSize,
                VoxelScale = nodeScale / chunkRes,
                NoisePosition = noisePosition,
                CenterOffset = Vector3.one * (nodeScale / 2f),
                TerraformPoint = terraformPoint,
                TerraformData = new float4(terraformData, Time.deltaTime),
                MapData = new NativeArray<float>(modData, Allocator.TempJob)
            };
            //Call Job
            var handle = terraformJob.Schedule(arrayLength, mapSize * mapSize);
            handle.Complete();
            //Set the modMap to the new values
            var data = terraformJob.MapData.ToArray();
            terraformJob.MapData.Dispose();
            return data;
        }
        //Node Functions
        public static float[] GetPlanetNoiseMap(int chunkRes, float3 noisePosition, float planetRadius, float3 center, int nodeScale, int seed, int divisions, PlanetData settings) {
            var mapSize = (chunkRes + 1);
            var arrayLength = mapSize * mapSize * mapSize;
            var layers = settings.NoiseLayers.Count;
            var planetJob = new VoxelJobs.GetPlanetNoise { 
                ChunkResolution = mapSize, 
                Radius = planetRadius, 
                CurveSamples = CurveSamples,
                VoxelScale = nodeScale / chunkRes, 
                NoisePosition = noisePosition, 
                PlanetCenter = center, 
                CenterOffset = Vector3.one * (nodeScale / 2f), 
                PerlinNoise = new Perlin3D(),
                Seed = seed,
                NoiseLayers = layers,
                DoNoiseLayers = settings.UseNoise,
                Map = new NativeArray<float>(arrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory) 
            };
            
            var noiseFloatDataOne = new NativeArray<float4>(layers, Allocator.TempJob);
            var noiseFloatDataTwo = new NativeArray<float3>(layers, Allocator.TempJob);
            var noiseIntData = new NativeArray<int2>(layers, Allocator.TempJob);
            var length = settings.NoiseLayers.Sum(layer => layer.Octaves);
            var noiseOffsets = new NativeArray<float3>(length, Allocator.TempJob);
            var noiseCurves = new NativeArray<float>(layers * CurveSamples, Allocator.TempJob);
            
            var index = 0;
            for (var i = 0; i < layers; i++) {
                var layer = settings.NoiseLayers[i];
                var scale = layer.Scale * (Mathf.Pow(2, divisions - 8) * (divisions * 4.5f));
                
                noiseFloatDataOne[i] = new float4(scale, layer.Lacunarity, layer.Persistence, layer.LayerPower);
                noiseFloatDataTwo[i] = new float3(0, CalculateNoiseValueClamp(layer), layer.HeightMultiplier);
                noiseIntData[i] = new int2(layer.Octaves, layer.Remove ? -1 : 1);
                //offsets
                noiseOffsets[i] = layer.Offset;
                //Make curve data
                var layerCurve = layer.HeightCurve.GenerateCurveArray(CurveSamples);
                for (var j = 0; j < CurveSamples; j++) {
                    noiseCurves[index] = layerCurve[j];
                    index++;
                }
            }
            
            planetJob.NoiseFloatDataOne = noiseFloatDataOne;
            planetJob.NoiseFloatDataTwo = noiseFloatDataTwo;
            planetJob.NoiseIntData = noiseIntData;
            planetJob.NoiseOffsets = noiseOffsets;
            planetJob.NoiseCurves = noiseCurves;
            

            var handle = planetJob.Schedule(arrayLength, mapSize * mapSize);
            handle.Complete();

            var map = planetJob.Map.ToArray();

            // Dispose off all native arrays
            noiseFloatDataOne.Dispose();
            noiseFloatDataTwo.Dispose();
            noiseIntData.Dispose();
            noiseOffsets.Dispose();
            noiseCurves.Dispose();
            planetJob.Map.Dispose();

            // Return the map
            return map;
        }
        public static float[] GetPlanetObjectMap(int chunkRes, float3 noisePosition, int nodeScale, int seed) {
            var mapSize = (chunkRes + 1);
            var arrayLength = mapSize * mapSize * mapSize;
            var planetJob = new VoxelJobs.GetObjectNoise() { 
                ChunkResolution = mapSize, 
                VoxelScale = nodeScale / chunkRes, 
                NoisePosition = noisePosition, 
                CenterOffset = Vector3.one * (nodeScale / 2f), 
                Map = new NativeArray<float>(arrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory) 
            };

            var handle = planetJob.Schedule(arrayLength, mapSize * mapSize);
            handle.Complete();

            var map = planetJob.Map.ToArray();
            planetJob.Map.Dispose();
            
            // Return the map
            return map;
        }
        //Mesh
        public static void GetPlanetMeshData(int chunkSize, int voxelScale, int3 centerOffset, float createGate, float valueGate, bool smoothTerrain, float[] noiseMap, float[] modMap, out Vector3[] verticesArray, out int[] trianglesArray) {
            var verticesList = new NativeList<float3>(Allocator.TempJob);
            var trianglesList = new NativeList<int>(Allocator.TempJob);
            var noiseMapArray = new NativeArray<float>(noiseMap, Allocator.TempJob);
            var modMapArray = new NativeArray<float>(modMap, Allocator.TempJob);
            var marchingJob = new VoxelJobs.MarchCubes {
                CubeIntData = new int4(centerOffset, chunkSize),
                CubeFloatData = new float3(voxelScale, createGate, valueGate),
                Vertices = verticesList,
                Triangles = trianglesList,
                NoiseMap = noiseMapArray,
                ModMap = modMapArray
            };
            
            var handle = marchingJob.Schedule();
            handle.Complete();
            
            noiseMapArray.Dispose();
            modMapArray.Dispose();
            
            verticesArray = verticesList.ToArray();
            trianglesArray = trianglesList.ToArray();
            verticesList.Dispose();
            trianglesList.Dispose();
            
            if  (smoothTerrain) RemoveDuplicateVertices(ref verticesArray, ref trianglesArray, out verticesArray, out trianglesArray, Threshold);
        }
        private static void RemoveDuplicateVertices(ref Vector3[] vertices, ref int[] triangles, out Vector3[] uniqueVertices, out int[] newTriangles, float threshold) {
            var uniqueVertexList = new List<Vector3>();
            var vertexMap = new Dictionary<Vector3, int>(new Vector3EqualityComparer(threshold));
            var newTrianglesList = new List<int>();
            
            var spatialHash = new Dictionary<Vector3Int, List<int>>();
            for (var i = 0; i < vertices.Length; i++) {
                var vertex = vertices[i];
                var hash = GetSpatialHash(vertex, threshold);

                if (!spatialHash.ContainsKey(hash)) {
                    spatialHash[hash] = new List<int>();
                }
                spatialHash[hash].Add(i);
            }

            // Iterate through triangles and map them to unique vertices
            foreach (var oldIndex in triangles) {
                var vertex = vertices[oldIndex];
                var hash = GetSpatialHash(vertex, threshold);
                // Check within the spatial hash cell for a close enough vertex
                var found = false;
                foreach (var existingIndex in spatialHash[hash]) {
                    if (!vertexMap.TryGetValue(vertices[existingIndex], out var uniqueIndex)) continue;
                    if (!(Vector3.SqrMagnitude(vertices[existingIndex] - vertex) < threshold * threshold)) continue;
                    newTrianglesList.Add(uniqueIndex);
                    found = true;
                    break;
                }
                // If not found, add as a new unique vertex
                if (found) continue;
                var newIndex = uniqueVertexList.Count;
                uniqueVertexList.Add(vertex);
                vertexMap[vertex] = newIndex;
                newTrianglesList.Add(newIndex);
                spatialHash[hash].Add(oldIndex);
            }
            uniqueVertices = uniqueVertexList.ToArray();
            newTriangles = newTrianglesList.ToArray();
        }
        private static Vector3Int GetSpatialHash(Vector3 vertex, float cellSize) {
            return new Vector3Int(
                Mathf.FloorToInt(vertex.x / cellSize),
                Mathf.FloorToInt(vertex.y / cellSize),
                Mathf.FloorToInt(vertex.z / cellSize)
            );
        }
    }
}