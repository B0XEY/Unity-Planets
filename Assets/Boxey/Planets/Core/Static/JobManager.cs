using Boxey.Planets.Core.Generation.Data_Objects;
using Unbegames.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Static {
    public static class JobManager {
        private const int CurveSamples = 256; //Says how many times the height curves get sampled when making the arrays for them. Over 256 is redundant
        private static float CalculateNoiseValueClamp(NoiseSettings settings) {
            var clamp = 0f;
            var amplitude = 1f;
            for (var i = 0; i < settings.octaves; i++) {
                clamp += 1f * amplitude;
                amplitude *= settings.persistence;
            }
            return clamp;
        }
        
        public static float[] GetPlanetNoiseMap(int chunkRes, float3 noisePosition, float planetRadius, float3 center, int nodeScale, int seed, int divisions, PlanetData settings) {
            var mapSize = chunkRes + 1;
            var arrayLength = mapSize * mapSize * mapSize;
            var layers = settings.noiseLayers.Count;
            var planetJob = new GetPlanetNoise { 
                ChunkResolution = mapSize, 
                Radius = planetRadius, 
                VoxelScale = nodeScale / chunkRes, 
                NoisePosition = noisePosition, 
                PlanetCenter = center, 
                CenterOffset = Vector3.one * (nodeScale / 2f), 
                CaveHeights = settings.caveLayers.ToNativeArray(Allocator.TempJob),
                Seed = seed, 
                PerlinNoise = new Perlin3D(), 
                NoiseLayers = layers,
                DoNoiseLayers = settings.useNoise,
                Map = new NativeArray<float>(arrayLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory) 
            };
            
            var noiseOffsets = new NativeArray<float3>(layers, Allocator.TempJob);
            var scales = new NativeArray<float>(layers, Allocator.TempJob);
            var lacunaries = new NativeArray<float>(layers, Allocator.TempJob);
            var persistences = new NativeArray<float>(layers, Allocator.TempJob);
            var powers = new NativeArray<float>(layers, Allocator.TempJob);
            var octaves = new NativeArray<int>(layers, Allocator.TempJob);
            var removes = new NativeArray<bool>(layers, Allocator.TempJob);
            var minimums = new NativeArray<float>(layers, Allocator.TempJob);
            var maximums = new NativeArray<float>(layers, Allocator.TempJob);
            var curves = new NativeArray<float>(layers * CurveSamples, Allocator.TempJob);

            var index = 0;
            for (var i = 0; i < layers; i++) {
                var layer = settings.noiseLayers[i];
                noiseOffsets[i] = layer.offset;
                scales[i] = layer.scale * (Mathf.Pow(2, divisions - 8) * (divisions * 4.5f));
                octaves[i] = layer.octaves;
                lacunaries[i] = layer.lacunarity;
                persistences[i] = layer.persistence;
                powers[i] = layer.layerPower;
                removes[i] = layer.remove;
                minimums[i] = -CalculateNoiseValueClamp(layer);
                maximums[i] = CalculateNoiseValueClamp(layer);
                var layerCurve = layer.heightCurve.GenerateCurveArray(CurveSamples);
                for (var j = 0; j < CurveSamples; j++) {
                    curves[index] = layerCurve[j];
                    index++;
                }
            }

            planetJob.NoiseOffset = noiseOffsets;
            planetJob.Scale = scales;
            planetJob.Lacunarity = lacunaries;
            planetJob.Persistence = persistences;
            planetJob.Power = powers;
            planetJob.Octaves = octaves;
            planetJob.Remove = removes;
            planetJob.Minimums = minimums;
            planetJob.Maximums = maximums;
            planetJob.Curve = curves;
            

            var handle = planetJob.Schedule(arrayLength, mapSize * mapSize);
            handle.Complete();

            var map = planetJob.Map.ToArray();

            // Dispose off all native arrays
            noiseOffsets.Dispose();
            scales.Dispose();
            lacunaries.Dispose();
            persistences.Dispose();
            powers.Dispose();
            octaves.Dispose();
            removes.Dispose();
            minimums.Dispose();
            maximums.Dispose();
            curves.Dispose();
            planetJob.CaveHeights.Dispose();
            planetJob.Map.Dispose();

            // Return the map
            return map;
        }
        public static void GetPlanetMeshData(int chunkSize, int voxelScale, int3 centerOffset, float createGate, float valueGate, float[] noiseMap, float[] modMap, out Vector3[] verticesArray, out int[] trianglesArray) {
            var verticesList = new NativeList<float3>(Allocator.TempJob);
            var trianglesList = new NativeList<int>(Allocator.TempJob);
            var noiseMapArray = noiseMap.ToNativeArray(Allocator.TempJob);
            var modMapArray = modMap.ToNativeArray(Allocator.TempJob);
            var marchingJob = new MarchCubes {
                ChunkSize = chunkSize,
                VoxelScale = voxelScale,
                CenterOffset = centerOffset,
                CreateGate = createGate,
                ValueGate = valueGate,
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
        }
        
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        private struct GetPlanetNoise : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public float Radius;
            [ReadOnly] public int VoxelScale;
            
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 PlanetCenter;
            [ReadOnly] public float3 CenterOffset;
            
            [ReadOnly] public int Seed;
            [ReadOnly] public bool DoNoiseLayers;
            
            //Caves
            [ReadOnly] public NativeArray<float2> CaveHeights;
            
            //use native arrays for all of these to allow for many noise layers
            [ReadOnly] public int NoiseLayers;
            [ReadOnly] public Perlin3D PerlinNoise;
            
            [ReadOnly] public NativeArray<float3> NoiseOffset;
            [ReadOnly] public NativeArray<float> Scale;
            [ReadOnly] public NativeArray<float> Lacunarity;
            [ReadOnly] public NativeArray<float> Persistence;
            [ReadOnly] public NativeArray<float> Power;
            [ReadOnly] public NativeArray<int> Octaves;
            [ReadOnly] public NativeArray<bool> Remove;
            [ReadOnly] public NativeArray<float> Minimums;
            [ReadOnly] public NativeArray<float> Maximums;
            
            [ReadOnly] public NativeArray<float> Curve;
            
            // Returned Map
            public NativeArray<float> Map;
            
            public void Execute(int index) {
                var z = index % ChunkResolution;
                var y = (index / ChunkResolution) % ChunkResolution;
                var x = index / (ChunkResolution * ChunkResolution);
                var voxelPosition = ((new float3(x, y, z) * VoxelScale) + NoisePosition) - CenterOffset;
                var distanceFromCenter = math.distance(voxelPosition, PlanetCenter);
                var radiusValue = math.saturate((distanceFromCenter - Radius) / (-Radius)) * 2 - 1;
                var scale = Radius * 0.075f;
                var baseValue = PerlinNoise.GetValue(Seed, new float3(voxelPosition.x / scale, voxelPosition.y / scale, voxelPosition.z / scale)) * 5f;
                var mapValue = radiusValue;
                for (var i = 0; i < CaveHeights.Length; i++) {
                    if (radiusValue >= CaveHeights[i].x) mapValue = radiusValue - baseValue;
                    if (radiusValue >= CaveHeights[i].y) mapValue = radiusValue;
                }
                Map[index] = mapValue;
                if (DoNoiseLayers) {
                    for (var i = 0; i < NoiseLayers; i++) {
                        var sampleX = (voxelPosition.x) / Scale[i] + NoiseOffset[i].x;
                        var sampleY = (voxelPosition.y) / Scale[i] + NoiseOffset[i].y;
                        var sampleZ = (voxelPosition.z) / Scale[i] + NoiseOffset[i].z;
                        var pointHeight = 0f;
                        var frequency = 1f;
                        var amplitude = 1f;

                        for (var o = 0; o < Octaves[i]; o++) {
                            sampleX *= frequency;
                            sampleY *= frequency;
                            sampleZ *= frequency;
                            pointHeight += ((PerlinNoise.GetValue(Seed, new float3(sampleX, sampleY, sampleZ)) + 1) / 2) * amplitude;
                            frequency *= Lacunarity[i];
                            amplitude *= Persistence[i];
                        }

                        var clampedValue = (pointHeight - Minimums[i]) / (Maximums[i] - Minimums[i]);
                        var normalizedValue = EvaluateCurve(clampedValue, CurveSamples * i) * 0.1f;
                        
                        if (Remove[i]) Map[index] -= (normalizedValue * Power[i]);
                        else Map[index] += (normalizedValue * Power[i]);
                    }
                }
            }
            
            private float EvaluateCurve(float time, int offset) {
                var length = CurveSamples - 1;
                var clampedValue = time < 0 ? 0 : (time > 1 ? 1 : time);
                var curveIndex = (clampedValue * length); //between 0 and CurveSamples
                var floorIndex = (int)math.floor(curveIndex); //Floor to Int
                if (floorIndex == length) { //If it is the curve sample (256 in this case) return the value at 256
                    return Curve[length + offset]; //Add the layer offset
                }
 
                var value1 = Curve[floorIndex + offset];
                var value2 = Curve[(floorIndex + 1) + offset];
                return math.lerp(value1, value2, math.frac(curveIndex));
            }
        }
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        private struct MarchCubes : IJob {
            [ReadOnly] public int ChunkSize;
            [ReadOnly] public int VoxelScale;
            [ReadOnly] public int3 CenterOffset;
            
            [ReadOnly] public float CreateGate;
            [ReadOnly] public float ValueGate;
            
            [ReadOnly] public NativeArray<float> NoiseMap;
            [ReadOnly] public NativeArray<float> ModMap;
            
            public NativeList<float3> Vertices;
            public NativeList<int> Triangles;
            
            private float SampleMap(int3 point){
                var index = point.x + (ChunkSize + 1) * (point.y + (ChunkSize + 1) * point.z);
                return NoiseMap[index] + ModMap[index];
            }
            private int GetCubeConfig(int3 point) {
                var configIndex = 0;
                var cornerTable = VoxelTables.CornerTable;

                for (var i = 0; i < 8; i++) {
                    var sample = SampleMap(point + cornerTable[i]);
                    if (sample < ValueGate)
                        configIndex |= 1 << i;
                }

                return configIndex;
            }
            
            public void Execute() {
                for (var x = 0; x < ChunkSize; x++) {
                    for (var y = 0; y < ChunkSize; y++) {
                        for (var z = 0; z < ChunkSize; z++) {
                            var point = new int3(x, y, z);
                            var noiseValue = SampleMap(point);
                            if (math.abs(noiseValue - CreateGate) < .001f) continue;
                            CreateCube(point);
                        }
                    }
                }
            }
            private void CreateCube(int3 voxelPoint) {
                var configIndex = GetCubeConfig(voxelPoint);
                if (configIndex == 0 || configIndex == 255) return;
            
                var worldPosition = (voxelPoint * VoxelScale) - CenterOffset;
                var edgeIndex = 0;
                for (var i = 0; i < 5; i++) { //Mesh triangles
                    for (var j = 0; j < 3; j++) { // Mesh Vertex
                        var indice = VoxelTables.TriangleTable[configIndex * 16 + edgeIndex];
                        if (indice == -1) return;
                        //Get Vert Positions
                        var corner1 = VoxelTables.CornerTable[VoxelTables.EdgeIndexes[indice * 2]];
                        var corner2 = VoxelTables.CornerTable[VoxelTables.EdgeIndexes[indice * 2 + 1]];
                        float3 vert1 = worldPosition + corner1 * VoxelScale;
                        float3 vert2 = worldPosition + corner2 * VoxelScale;
                        var vert1Sample = SampleMap(voxelPoint + corner1);
                        var vert2Sample = SampleMap(voxelPoint + corner2);
                        var difference = vert2Sample - vert1Sample;
                        var vertPosition = vert1;
                        if (difference != 0)
                            vertPosition += (vert2 - vert1) * ((ValueGate - vert1Sample) / difference);
                        //Add to the lists
                        Vertices.Add(vertPosition);
                        Triangles.Add(Vertices.Length - 1);
                        edgeIndex++;
                    }
                }
            }
        }
        //Mesh Job
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        private struct Terraform : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public float Radius;
            [ReadOnly] public int VoxelScale;
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 TerraformPoint;
            [ReadOnly] public float3 CenterOffset;
            
            [ReadOnly] public int Mult;
            [ReadOnly] public float EffectRadius;
            [ReadOnly] public float EffectSpeed;
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public float3 EffectCenter;
            
            public NativeArray<float> PlanetMap;
            public void Execute(int index){
                var z = index % (ChunkResolution + 1);
                var y = (index / (ChunkResolution + 1)) % (ChunkResolution + 1);
                var x = index / ((ChunkResolution + 1) * (ChunkResolution + 1));
                var voxelPosition = ((new float3(x, y, z) * VoxelScale) + NoisePosition) - CenterOffset;
                var distanceFromPoint = math.distance(voxelPosition, TerraformPoint);
                if (distanceFromPoint < EffectRadius && distanceFromPoint > -EffectRadius) {
                    var weight = SmoothStep(EffectRadius, EffectRadius * 0.7f, distanceFromPoint);
                    PlanetMap[index] += -(EffectSpeed * weight * DeltaTime) * Mult;
                }
                
            }
            private static float SmoothStep(float min, float max, float time) {
                time = math.saturate((time - min) / (max - min));
                return time * time * (3 - 2 * time);
            }
        }
    }
}