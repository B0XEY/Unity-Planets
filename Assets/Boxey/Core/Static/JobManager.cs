using System.Collections.Generic;
using Boxey.Core.Generation.Data_Objects;
using Unbegames.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Core.Static {
    public static class JobManager{
        public static float[] GetPlanetNoiseMap(int chunkRes, float3 noisePosition, float planetRadius, float3 center, int nodeScale, int seed, PlanetData settings){
            var noiseOffsets = new NativeArray<float3>(settings.noiseLayers.Count, Allocator.TempJob);
            var scales = new NativeArray<float>(settings.noiseLayers.Count, Allocator.TempJob);
            var amplitudes = new NativeArray<float>(settings.noiseLayers.Count, Allocator.TempJob);
            var frequencies = new NativeArray<float>(settings.noiseLayers.Count, Allocator.TempJob);
            var lacunaries = new NativeArray<float>(settings.noiseLayers.Count, Allocator.TempJob);
            var persistences = new NativeArray<float>(settings.noiseLayers.Count, Allocator.TempJob);
            var powers = new NativeArray<float>(settings.noiseLayers.Count, Allocator.TempJob);
            var octaves = new NativeArray<int>(settings.noiseLayers.Count, Allocator.TempJob);
            var removes = new NativeArray<bool>(settings.noiseLayers.Count, Allocator.TempJob);
            var noiseTypes = new NativeArray<int>(settings.noiseLayers.Count, Allocator.TempJob);
            var noisePerlin = new NativeArray<Perlin3D>(settings.noiseLayers.Count, Allocator.TempJob);
            var noiseBillow = new NativeArray<FractalBillow<Value3D>>(settings.noiseLayers.Count, Allocator.TempJob);
            var noiseRigid = new NativeArray<FractalRiged<ValueCubic3D>>(settings.noiseLayers.Count, Allocator.TempJob);
            for (int i = 0; i < settings.noiseLayers.Count; i++){
                var noiseTypeToUse = 0;
                if (settings.noiseLayers[i].type == NoiseType.Billow) noiseTypeToUse = 1;
                if (settings.noiseLayers[i].type == NoiseType.Ridged) noiseTypeToUse = 2;
                noiseOffsets[i] = settings.noiseLayers[i].offset;
                scales[i] = 45 * settings.noiseLayers[i].scale;
                amplitudes[i] = settings.noiseLayers[i].amplitude;
                frequencies[i] = settings.noiseLayers[i].frequency;
                lacunaries[i] = settings.noiseLayers[i].lacunarity;
                persistences[i] = settings.noiseLayers[i].persistence;
                powers[i] = settings.noiseLayers[i].layerPower;
                octaves[i] = settings.noiseLayers[i].octaves;
                removes[i] = settings.noiseLayers[0].removeLayer;
                noiseTypes[i] = noiseTypeToUse;
                noisePerlin[i] = new Perlin3D();
                noiseBillow[i] = new FractalBillow<Value3D>(octaves[i], lacunaries[i], settings.noiseLayers[i].gain, settings.noiseLayers[i].strength);
                noiseRigid[i] = new FractalRiged<ValueCubic3D>(octaves[i], lacunaries[i], settings.noiseLayers[i].gain, settings.noiseLayers[i].strength);
            }
            
            var values = new NativeArray<float>((chunkRes + 1) * (chunkRes + 1) * (chunkRes + 1), Allocator.TempJob);
            var planetJob = new GetPlanetNoise{
                ChunkResolution = chunkRes,
                Radius = planetRadius,
                VoxelScale = nodeScale / chunkRes,
                NoisePosition = noisePosition,
                PlanetCenter = center,
                CenterOffset = Vector3.one * (nodeScale / 2f),
                Seed = seed,
                CaveNoise = new Perlin3D(),
                DoNoiseLayers = settings.useNoise,
                NoiseOffset = noiseOffsets,
                Scale = scales,
                Amplitude = amplitudes,
                Frequency = frequencies,
                Lacunarity = lacunaries,
                Persistence = persistences,
                Power = powers,
                Octaves = octaves,
                Remove = removes,
                UseNoise = noiseTypes,
                Noise = noisePerlin,
                NoiseBillow = noiseBillow,
                NoiseRigid = noiseRigid,
                Map = values
            };
            var handle = planetJob.Schedule(values.Length, chunkRes + 1);
            handle.Complete();
            var map = values.ToArray();
            //Dispose off all native arrays
            noiseOffsets.Dispose();
            scales.Dispose();
            amplitudes.Dispose();
            frequencies.Dispose();
            lacunaries.Dispose();
            persistences.Dispose();
            powers.Dispose();
            octaves.Dispose();
            removes.Dispose();
            noiseTypes.Dispose();
            noisePerlin.Dispose();
            noiseBillow.Dispose();
            noiseRigid.Dispose();
            values.Dispose();
            //Return the map
            return map;
        }
        [BurstCompile]
        private struct GetPlanetNoise : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public float Radius;
            [ReadOnly] public int VoxelScale;
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 PlanetCenter;
            [ReadOnly] public float3 CenterOffset;
            [ReadOnly] public int Seed;
            [ReadOnly] public Perlin3D CaveNoise;
            [ReadOnly] public bool DoNoiseLayers;
            
            //use native arrays for all of these to allow for many noise layers
            [ReadOnly] public NativeArray<float3> NoiseOffset;
            [ReadOnly] public NativeArray<float> Scale;
            [ReadOnly] public NativeArray<float> Amplitude;
            [ReadOnly] public NativeArray<float> Frequency;
            [ReadOnly] public NativeArray<float> Lacunarity;
            [ReadOnly] public NativeArray<float> Persistence;
            [ReadOnly] public NativeArray<float> Power;
            [ReadOnly] public NativeArray<int> Octaves;
            [ReadOnly] public NativeArray<bool> Remove;
            [ReadOnly] public NativeArray<int> UseNoise;
            [ReadOnly] public NativeArray<Perlin3D> Noise;
            [ReadOnly] public NativeArray<FractalBillow<Value3D>> NoiseBillow;
            [ReadOnly] public NativeArray<FractalRiged<ValueCubic3D>> NoiseRigid;
            
            // Returned Map
            public NativeArray<float> Map;
            
            public void Execute(int index) {
                var z = index % (ChunkResolution + 1);
                var y = (index / (ChunkResolution + 1)) % (ChunkResolution + 1);
                var x = index / ((ChunkResolution + 1) * (ChunkResolution + 1));
                var voxelPosition = ((new float3(x, y, z) * VoxelScale) + NoisePosition) - CenterOffset;
                var distanceFromCenter = math.distance(voxelPosition, PlanetCenter);
                var radius = Mathf.InverseLerp(Radius, 0, distanceFromCenter) * 2 - 1;
                Map[index] = radius;
                
                if (DoNoiseLayers){
                    for (int i = 0; i < Noise.Length; i++)
                    {
                        var sampleX = (voxelPosition.x) / Scale[i] + NoiseOffset[i].x;
                        var sampleY = (voxelPosition.y) / Scale[i] + NoiseOffset[i].y;
                        var sampleZ = (voxelPosition.z) / Scale[i] + NoiseOffset[i].z;
                        var value = 0f;
                        var frequency = Frequency[i];
                        var amplitude = Amplitude[i];
                        var min = -1f;
                        var max = 1f;
                        if (UseNoise[i] == 0)
                        {
                            for (int o = 0; o < Octaves[i]; o++)
                            {
                                sampleX *= frequency;
                                sampleY *= frequency;
                                sampleZ *= frequency;
                                value += (Noise[i].GetValue(Seed, new float3(sampleX, sampleY, sampleZ))) * amplitude;
                                frequency *= Lacunarity[i];
                                amplitude *= Persistence[i];
                            }
                        }
                        else
                        {
                            if (UseNoise[i] == 1)
                            {
                                value = NoiseBillow[i].GetValue(Seed, new float3(sampleX, sampleY, sampleZ)) *
                                        amplitude;
                            }

                            if (UseNoise[i] == 2)
                            {
                                min = 0;
                                max = 1f;
                                value = NoiseRigid[i].GetValue(Seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                            }
                        }

                        var normalizedValue = Mathf.InverseLerp(min, max, value) * 0.15f;
                        if (Remove[i]) Map[index] -= (normalizedValue * Power[i]);
                        else Map[index] += (normalizedValue * Power[i]);
                    }
                }
                // Caves
                Map[index] -= CaveNoise.GetValue(Seed, x, y, z);
            }
        }
        [BurstCompile]
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