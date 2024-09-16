using System.Collections.Generic;
using System.Linq;
using Boxey.Planets.Core.Classes;
using Unbegames.Noise;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Static {
    public static class JobManager {
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
        //Node Functions
        public static float3[] GetPlanetNoiseMap(int chunkRes, float3 noisePosition, float planetRadius, float3 center, float nodeScale, int seed, int maxDivisions, PlanetData settings, float[] curves) {
            var mapSize = chunkRes + 1;
            var arrayLength = mapSize * mapSize * mapSize;
            var layers = settings.NoiseLayers.Count;
            var samples = curves.Length / layers;
            
            var planetJob = new VoxelJobs.GetNoiseMap {
                ChunkResolution = mapSize,
                Radius = planetRadius,
                CurveSamples = samples,
                VoxelScale = nodeScale / chunkRes,
                NoisePosition = noisePosition,
                PlanetCenter = center,
                CenterOffset = Vector3.one * (nodeScale / 2f),
                PerlinNoise = new Perlin3D(),
                Seed = seed,
                NoiseLayers = layers,
                DoNoiseLayers = settings.UseNoise,
                Map = new NativeArray<float3>(arrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
            };

            var noiseFloatDataOne = new NativeArray<float4>(layers, Allocator.TempJob);
            var noiseFloatDataTwo = new NativeArray<float3>(layers, Allocator.TempJob);
            var noiseIntData = new NativeArray<int2>(layers, Allocator.TempJob);
            var length = settings.NoiseLayers.Sum(layer => layer.Octaves);
            var noiseOffsets = new NativeArray<float3>(length, Allocator.TempJob);
            var noiseCurves = new NativeArray<float>(curves, Allocator.TempJob);

            for (var i = 0; i < layers; i++) {
                var layer = settings.NoiseLayers[i];
                var scale = layer.Scale * (Mathf.Pow(2, maxDivisions - 8) * (maxDivisions * 4.5f));

                noiseFloatDataOne[i] = new float4(scale, layer.Lacunarity, layer.Persistence, layer.LayerPower);
                noiseFloatDataTwo[i] = new float3(0, CalculateNoiseValueClamp(layer), layer.HeightMultiplier);
                noiseIntData[i] = new int2(layer.Octaves, layer.Remove ? -1 : 1);
                //offsets
                noiseOffsets[i] = layer.Offset;
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
        public static float2[] GetTerraformMap(float2[] startingMap, int startIndex, int chunkRes, float3 noisePosition, float nodeScale, HashSet<UpdateCalls.TerraformInfo> terraformInfo) {
            var mapSize = chunkRes + 1;
            var arrayLength = mapSize * mapSize * mapSize;
            var job = new VoxelJobs.GetTerraformMap {
                StartingIndex = startIndex,
                ChunkResolution = mapSize,
                VoxelScale = nodeScale / chunkRes,
                NoisePosition = noisePosition,
                CenterOffset = Vector3.one * (nodeScale / 2f),
                Map = new NativeArray<float2>(startingMap, Allocator.TempJob),
                TerraformInfo = new NativeArray<UpdateCalls.TerraformInfo>(terraformInfo.ToArray(), Allocator.TempJob)
            };
            
            var handle = job.Schedule(arrayLength, mapSize * mapSize);
            handle.Complete();

            var outMap = job.Map.ToArray();
            job.Map.Dispose();
            job.TerraformInfo.Dispose();
            
            return outMap;
        }
        public static List<float2[]> BatchTerraformNodes(int chunkRes, int batchSize, List<UpdateCalls.NodeInfo> nodeInfo, HashSet<UpdateCalls.TerraformInfo> terraformInfo) {
            var mapSize = chunkRes + 1;
            var voxelsPerNode = mapSize * mapSize * mapSize;
            var nodeMaps = new NativeArray<float2>(nodeInfo.Count * voxelsPerNode, Allocator.TempJob);
            var nodeJobInfo = new NativeArray<UpdateCalls.NodeJobInfo>(nodeInfo.Count, Allocator.TempJob);
            for (var i = 0; i < nodeJobInfo.Length; i++) {
                nodeJobInfo[i] = new UpdateCalls.NodeJobInfo(nodeInfo[i]);
            }
            for (var nodeIndex = 0; nodeIndex < nodeInfo.Count; nodeIndex++) {
                var currentInfo = nodeInfo[nodeIndex];
                var nodeMapOffset = nodeIndex * voxelsPerNode;
                for (var voxelIndex = 0; voxelIndex < voxelsPerNode; voxelIndex++) {
                    nodeMaps[nodeMapOffset + voxelIndex] = currentInfo.TerraformMap[voxelIndex];
                }
            }
            var job = new VoxelJobs.GetTerraformMapChunks {
                ChunkResolution = mapSize,
                VoxelsPerNode = voxelsPerNode,
                NodeMaps = nodeMaps,
                NodeInfo = nodeJobInfo,
                TerraformInfo = new NativeArray<UpdateCalls.TerraformInfo>(terraformInfo.ToArray(), Allocator.TempJob)
            };

            var handle  = job.ScheduleBatch(nodeMaps.Length, voxelsPerNode);
            handle.Complete();
            
            var output = new List<float2[]>();
            for (var nodeIndex = 0; nodeIndex < nodeInfo.Count; nodeIndex++) {
                var nodeMapOffset = nodeIndex * voxelsPerNode;
                var outMap = new float2[voxelsPerNode];
                for (var voxelIndex = 0; voxelIndex < voxelsPerNode; voxelIndex++) {
                    outMap[voxelIndex] = nodeMaps[nodeMapOffset + voxelIndex];
                }
                output.Add(outMap);
            }
            
            nodeMaps.Dispose();
            nodeJobInfo.Dispose();
            job.TerraformInfo.Dispose();

            return output;
        }
        //Mesh
        public static (Vector3[] verticesArray, Vector3[] normalsArray, int[] trianglesArray, Vector2[] uv1, Vector2[]uv2) GetPlanetMeshData(int chunkSize, float voxelScale, int3 centerOffset, float createGate, float valueGate, bool smoothTerrain, float3[] noiseMap, float2[] terraformMap) {
            var noiseMapArray = new NativeArray<float3>(noiseMap, Allocator.TempJob);
            var terraformMapArray = new NativeArray<float2>(terraformMap, Allocator.TempJob);
            var marchingJob = new VoxelJobs.MarchCubes {
                CubeIntData = new int4(centerOffset, chunkSize),
                CubeFloatData = new float3(voxelScale, createGate, valueGate),
                Vertices = new NativeList<float3>(Allocator.TempJob),
                Normals = new NativeList<float3>(Allocator.TempJob),
                Triangles = new NativeList<int>(Allocator.TempJob),
                UVOne = new NativeList<float2>(Allocator.TempJob),
                UVTwo = new NativeList<float2>(Allocator.TempJob),
                OriginalVertices = new NativeList<float3>(Allocator.TempJob),
                OriginalNormals = new NativeList<float3>(Allocator.TempJob),
                OriginalTriangles = new NativeList<int>(Allocator.TempJob),
                OriginalUVOne = new NativeList<float2>(Allocator.TempJob),
                OriginalUVTwo = new NativeList<float2>(Allocator.TempJob),
                NoiseMap = noiseMapArray,
                TerraformMap = terraformMapArray
            };

            var handle = marchingJob.Schedule();
            handle.Complete();

            noiseMapArray.Dispose();
            terraformMapArray.Dispose();

            var verticesArray = marchingJob.Vertices.ToArray();
            var normalsArray = marchingJob.Normals.ToArray();
            var trianglesArray = marchingJob.Triangles.ToArray();
            var uv1 = marchingJob.UVOne.ToArray();
            var uv2 = marchingJob.UVTwo.ToArray();

            //clear
            marchingJob.Vertices.Dispose();
            marchingJob.OriginalVertices.Dispose();
            marchingJob.Normals.Dispose();
            marchingJob.OriginalNormals.Dispose();
            marchingJob.Triangles.Dispose();
            marchingJob.OriginalTriangles.Dispose();
            marchingJob.UVOne.Dispose();
            marchingJob.OriginalUVOne.Dispose();
            marchingJob.UVTwo.Dispose();
            marchingJob.OriginalUVTwo.Dispose();

            if (smoothTerrain) {
                RemoveDuplicateVertices(ref verticesArray, ref normalsArray, ref trianglesArray, ref uv1, ref uv2, out verticesArray, out normalsArray, out trianglesArray, out uv1, out uv2);
            }

            return (verticesArray, normalsArray, trianglesArray, uv1, uv2);
        }
        private static void RemoveDuplicateVertices(ref Vector3[] vertices, ref Vector3[] normals, ref int[] triangles, ref Vector2[] uv1, ref Vector2[] uv2, out Vector3[] uniqueVertices, out Vector3[] uniqueNormals, out int[] newTriangles, out Vector2[] uniqueUv1, out Vector2[] uniqueUv2) {
            var uniqueVertexList = new List<Vector3>();
            var uniqueNormalList = new List<Vector3>();
            var uniqueUV1List = new List<Vector2>();
            var uniqueUV2List = new List<Vector2>();
            var vertexMap = new Dictionary<Vector3, int>();
            var newTrianglesList = new List<int>();

            // Mapping original vertex indices to unique vertices
            for (var i = 0; i < vertices.Length; i++) {
                if (vertexMap.ContainsKey(vertices[i])) continue;
                vertexMap[vertices[i]] = uniqueVertexList.Count;
                uniqueVertexList.Add(vertices[i]);
                uniqueNormalList.Add(normals[i]);
                uniqueUV1List.Add(uv1[i]);
                uniqueUV2List.Add(uv2[i]);
            }

            foreach (var oldIndex in triangles) {
                var vertex = vertices[oldIndex];
                var newIndex = vertexMap[vertex];
                newTrianglesList.Add(newIndex);
            }

            uniqueVertices = uniqueVertexList.ToArray();
            uniqueNormals = uniqueNormalList.ToArray();
            newTriangles = newTrianglesList.ToArray();
            uniqueUv1 = uniqueUV1List.ToArray();
            uniqueUv2 = uniqueUV2List.ToArray();
        }
        //Wind
        public static float3[] GetPlanetWindMap(int mapSize, float3 planetPosition, float rootNodeScale, int seed) {
            var arrayLength = mapSize * mapSize * mapSize;
            var planetJob = new VoxelJobs.GetWindMap {
                ChunkResolution = mapSize,
                VoxelScale = rootNodeScale / mapSize,
                NoisePosition = planetPosition,
                CenterOffset = Vector3.one * (rootNodeScale / 2f),
                PerlinNoise = new Perlin3D(),
                Seed = seed,
                Map = new NativeArray<float3>(arrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
            };


            var handle = planetJob.Schedule(arrayLength, mapSize * mapSize);
            handle.Complete();

            var map = planetJob.Map.ToArray();
            planetJob.Map.Dispose();
            // Return the map
            return map;
        }
        public static Matrix4x4[] GetPlanetFoliageMap(float3[] vertices, int[] triangles, float3 nodePosition, int divisions, int density, int seed) {
            var grassAmount = triangles.Length * density;
            var planetJob = new VoxelJobs.GetGrassPosition() {
                Seed = seed,
                Density = density,
                Divisions = divisions,
                NodePosition = nodePosition,
                Vertices = new NativeArray<float3>(vertices, Allocator.TempJob),
                Triangles = new NativeArray<int>(triangles, Allocator.TempJob),
                GrassPositions = new NativeArray<Matrix4x4>(grassAmount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
            };


            var handle = planetJob.Schedule(grassAmount, density * density);
            handle.Complete();

            var map = planetJob.GrassPositions.ToArray();
            planetJob.Vertices.Dispose();
            planetJob.Triangles.Dispose();
            planetJob.GrassPositions.Dispose();
            // Return the map
            return map;
        }
    }
}