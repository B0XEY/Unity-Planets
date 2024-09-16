using System.Runtime.CompilerServices;
using Unbegames.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Static {
    public static class VoxelJobs {
        //color getter 74x40 grid 19x10 for colors so 1 color every 4px
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct GetNoiseMap : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public float Radius;
            [ReadOnly] public int CurveSamples;
            [ReadOnly] public float VoxelScale;
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 PlanetCenter;
            [ReadOnly] public float3 CenterOffset;
            [ReadOnly] public bool DoNoiseLayers;
            [ReadOnly] public Perlin3D PerlinNoise;
            [ReadOnly] public int Seed;
            [ReadOnly] public int NoiseLayers;
            //Group data
            [ReadOnly] public NativeArray<float4> NoiseFloatDataOne; //x: scale, y: lacunarity, z: persistence, w: layerPower
            [ReadOnly] public NativeArray<float3> NoiseFloatDataTwo; //x: min, y: max, z: height multi
            [ReadOnly] public NativeArray<int2> NoiseIntData; //x: octaves, y: remove
            //Single Data
            [ReadOnly] public NativeArray<float3> NoiseOffsets;
            [ReadOnly] public NativeArray<float> NoiseCurves;
            // Returned Map using float 3, so we can pass mor data out like ore, material color, ect
            public NativeArray<float3> Map;
            
            public void Execute(int index) {
                var x = index % ChunkResolution;
                var y = index / ChunkResolution % ChunkResolution;
                var z = index / (ChunkResolution * ChunkResolution);
                var voxelWorldPosition = (new float3(x, y, z) * VoxelScale) + NoisePosition - CenterOffset;
                var distanceFromCenter = math.distance(voxelWorldPosition, PlanetCenter);
                var radiusValue = math.saturate((distanceFromCenter - Radius) / -Radius) * 2 - 1;
                
                var mapX = DoNoiseLayers ? (radiusValue * 2.5f + GetValueAtPoint(voxelWorldPosition)) : (radiusValue * 7.5f);
                Map[index] = new float3(mapX, GetPerlin(voxelWorldPosition), Pack(new float2(.5f, 1)));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float GetValueAtPoint(float3 position) {
                var height = 0f;
                for (var i = 0; i < NoiseLayers; i++) {
                    var noiseDataOne = NoiseFloatDataOne[i];
                    var noiseDataTwo = NoiseFloatDataTwo[i];
                    var noiseOffset = NoiseOffsets[i];

                    var scale = noiseDataOne.x;
                    var lacunarity = noiseDataOne.y;
                    var persistence = noiseDataOne.z;
                    var layerPower = noiseDataOne.w;
                    
                    var samplePos = (position / scale) + noiseOffset;
                    var pointHeight = 0f;
                    var frequency = 1f;
                    var amplitude = 1f;

                    for (var o = 0; o < NoiseIntData[i].x; o++) {
                        var point = samplePos * frequency;
                        pointHeight += GetPerlin(point, i) * amplitude;
                        frequency *= lacunarity;
                        amplitude *= persistence;
                    }

                    var clampedValue = (pointHeight - noiseDataTwo.x) / (noiseDataTwo.y - noiseDataTwo.x);
                    var normalizedValue = EvaluateCurve(clampedValue, CurveSamples * i) * 0.01f * noiseDataTwo.z;
                    height += normalizedValue * layerPower * NoiseIntData[i].y;
                }

                return height;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float GetPerlin(float3 position, int offset = 0) {
                return (PerlinNoise.GetValue(Seed + offset, position) + 1) * 0.5f;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float EvaluateCurve(float time, int offset) {
                var length = CurveSamples - 1;
                //var clampedValue = time < 0 ? 0 : (time > 1 ? 1 : time);
                var clampedValue = math.saturate(time);
                var curveIndex = clampedValue * length;
                var floorIndex = (int)math.floor(curveIndex);
                var safeIndex = math.min(floorIndex + 1, length);

                var value1 = NoiseCurves[floorIndex + offset];
                var value2 = NoiseCurves[safeIndex + offset];
                return math.lerp(value1, value2, math.frac(curveIndex));
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float Pack(float2 uv) {
                var intX = (uint)(uv.x * 65535f);
                var intY = (uint)(uv.y * 65535f);
                var packed = (intX << 16) | intY;
                // Reinterpret the packed uint as a float
                return math.asfloat(packed);
            }
        }
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct GetTerraformMap : IJobParallelFor {
            [ReadOnly] public int StartingIndex;
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public float VoxelScale;
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 CenterOffset;
            [ReadOnly] public NativeArray<UpdateCalls.TerraformInfo> TerraformInfo;
            
            public NativeArray<float2> Map;
            
            public void Execute(int index) {
                var x = index % ChunkResolution;
                var y = index / ChunkResolution % ChunkResolution;
                var z = index / (ChunkResolution * ChunkResolution);
                var voxelWorldPosition = (new float3(x, y, z) * VoxelScale) + NoisePosition - CenterOffset;
                
                var mapX = Map[index].x;
                var mapY = Map[index].y;
                
                for (var i = StartingIndex; i < TerraformInfo.Length; i++) {
                    var info = TerraformInfo[i];
                    var distanceSquared = math.lengthsq(voxelWorldPosition - info.TerraformLocation);
                    var chunkResSquared = ChunkResolution * ChunkResolution;
                    if (distanceSquared > chunkResSquared) {
                        continue;
                    }
                    var data = TerraformCall(info, distanceSquared);
                    mapX += data.x;
                    mapY = data.x != 0 ? Pack(data.yz) : mapY;
                }
                
                Map[index] = new float2(mapX, mapY);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float3 TerraformCall(UpdateCalls.TerraformInfo info, float distance) {
                var output = new float3(0);
                var dataInfo = info.TerraformData;
                var radiusSquared = dataInfo.x * dataInfo.x;
                var radiusSquared07 = radiusSquared * 0.7f * 0.7f;
                
                if (distance < radiusSquared && distance > -radiusSquared) {
                    var weight = SmoothStep(radiusSquared, radiusSquared07, distance);
                    var mult = info.AddTerrain ? 1 : -1;
                    var value = output.x + -(dataInfo.y * weight * info.DeltaTime) * mult;
                    output = new float3(value, dataInfo.w, dataInfo.z);
                }
                return output;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float SmoothStep(float min, float max, float time) {
                var t = math.saturate((time - min) / (max - min));
                return t * t * (3 - 2 * t);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float Pack(float2 uv) {
                var intX = (uint)(uv.x * 65535f);
                var intY = (uint)(uv.y * 65535f);
                var packed = (intX << 16) | intY;
                // Reinterpret the packed uint as a float
                return math.asfloat(packed);
            }
        }
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct GetTerraformMapChunks : IJobParallelForBatch {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public int VoxelsPerNode;
            [ReadOnly] public NativeArray<UpdateCalls.NodeJobInfo> NodeInfo;
            [ReadOnly] public NativeArray<UpdateCalls.TerraformInfo> TerraformInfo;
            
            public NativeArray<float2> NodeMaps;

            public void Execute(int startIndex, int count) {
                var chunkResSquared = ChunkResolution * ChunkResolution;
                for (int index = startIndex; index < startIndex + count; index++) {
                    var currentInfo = NodeInfo[(int)math.ceil(index / (float)VoxelsPerNode)];
                    for (var voxelIndex = 0; voxelIndex < VoxelsPerNode; voxelIndex++) {
                        var x = voxelIndex % ChunkResolution;
                        var y = voxelIndex / ChunkResolution % ChunkResolution;
                        var z = voxelIndex / (ChunkResolution * ChunkResolution);
                        var voxelWorldPosition = (new float3(x, y, z) * currentInfo.VoxelScale) + currentInfo.NoisePosition - currentInfo.CenterOffset;

                        var mapX = NodeMaps[index].x;
                        var mapY = NodeMaps[index].y;

                        for (var i = currentInfo.StartIndex; i < TerraformInfo.Length; i++) {
                            var info = TerraformInfo[i];
                            var distanceSquared = math.lengthsq(voxelWorldPosition - info.TerraformLocation);
                            if (distanceSquared > chunkResSquared) {
                                continue;
                            }
                            var data = TerraformCall(info, distanceSquared);
                            mapX += data.x;
                            mapY = data.x != 0 ? Pack(data.yz) : mapY;
                        }

                        NodeMaps[index] = new float2(mapX, mapY);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float3 TerraformCall(UpdateCalls.TerraformInfo info, float distance) {
                var output = new float3(0);
                var dataInfo = info.TerraformData;
                var radiusSquared = dataInfo.x * dataInfo.x;
                var radiusSquared07 = radiusSquared * 0.7f * 0.7f;

                if (distance < radiusSquared && distance > -radiusSquared) {
                    var weight = SmoothStep(radiusSquared, radiusSquared07, distance);
                    var mult = info.AddTerrain ? 1 : -1;
                    var value = output.x + -(dataInfo.y * weight * info.DeltaTime) * mult;
                    output = new float3(value, dataInfo.w, dataInfo.z);
                }
                return output;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float SmoothStep(float min, float max, float time) {
                var t = math.saturate((time - min) / (max - min));
                return t * t * (3 - 2 * t);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float Pack(float2 uv) {
                var intX = (uint)(uv.x * 65535f);
                var intY = (uint)(uv.y * 65535f);
                var packed = (intX << 16) | intY;
                // Reinterpret the packed uint as a float
                return math.asfloat(packed);
            }
        }
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct MarchCubes : IJob {
            [ReadOnly] public int4 CubeIntData; //XYZ: center offset, w: size
            [ReadOnly] public float3 CubeFloatData; //x: Voxel Scale, y: Create Gate, z: Value Gate
            [ReadOnly] public NativeArray<float3> NoiseMap;
            [ReadOnly] public NativeArray<float2> TerraformMap;

            public NativeList<float3> Vertices;
            public NativeList<float3> Normals;
            public NativeList<int> Triangles;
            public NativeList<float2> UVOne;
            public NativeList<float2> UVTwo;

            public NativeList<float3> OriginalVertices;
            public NativeList<float3> OriginalNormals;
            public NativeList<int> OriginalTriangles;
            public NativeList<float2> OriginalUVOne; // biome
            public NativeList<float2> OriginalUVTwo; //ore


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float SampleMap(int3 point, int axis) {
                var index = point.x + (CubeIntData.w + 1) * (point.y + (CubeIntData.w + 1) * point.z);
                return axis switch {
                    1 => NoiseMap[index].y,
                    2 => TerraformMap[index].y == 0 ? NoiseMap[index].z : TerraformMap[index].y,
                    _ => NoiseMap[index].x + TerraformMap[index].x
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetCubeConfig(int3 point) {
                var configIndex = 0;
                var cornerTable = VoxelTables.CornerTable;

                for (var i = 0; i < 8; i++) {
                    var sample = SampleMap(point + (int3)cornerTable[i], 0);
                    if (sample < CubeFloatData.z) {
                        configIndex |= 1 << i;
                    }
                }

                return configIndex;
            }
            public void Execute() {
                for (var x = 0; x < CubeIntData.w; x++) {
                    for (var y = 0; y < CubeIntData.w; y++) {
                        for (var z = 0; z < CubeIntData.w; z++) {
                            var point = new int3(x, y, z);
                            var noiseValue = SampleMap(point, 0);
                            if (math.abs(noiseValue - CubeFloatData.y) < .001f) {
                                continue;
                            }
                            CreateCube(point);
                        }
                    }
                }
                CalculateNormals();
            }
            private void CreateCube(int3 voxelPoint) {
                var configIndex = GetCubeConfig(voxelPoint);
                if (configIndex is 0 or 255) {
                    return;
                }
                var voxelScale = CubeFloatData.x;
                var localPosition = (float3)voxelPoint * voxelScale - CubeIntData.xyz;
                var edgeIndex = 0;
                for (var i = 0; i < 5; i++) { //Mesh triangles
                    for (var j = 0; j < 3; j++) {
                        // Mesh Vertex
                        var indice = VoxelTables.TriangleTable[configIndex * 16 + edgeIndex];
                        if (indice == -1) {
                            return;
                        }

                        //Get Vert Positions
                        var corner1 = VoxelTables.CornerTable[VoxelTables.EdgeIndexes[indice * 2]];
                        var corner2 = VoxelTables.CornerTable[VoxelTables.EdgeIndexes[indice * 2 + 1]];
                        var vert1 = localPosition + corner1 * voxelScale;
                        var vert2 = localPosition + corner2 * voxelScale;
                        var vert1Sample = SampleMap(voxelPoint + (int3)corner1, 0);
                        var vert2Sample = SampleMap(voxelPoint + (int3)corner2, 0);
                        var difference = vert2Sample - vert1Sample;
                        var vertPosition = vert1;
                        if (math.abs(difference - difference) < 0.001f) {
                            vertPosition += (vert2 - vert1) * ((CubeFloatData.z - vert1Sample) / difference);
                        }

                        //Add to the lists
                        if (math.all(math.isfinite(vertPosition))) {
                            OriginalVertices.Add(vertPosition);
                            Vertices.Add(vertPosition);
                            OriginalTriangles.Add(OriginalVertices.Length - 1);
                            Triangles.Add(OriginalVertices.Length - 1);

                            var uv1 = UnPack(SampleMap(voxelPoint, 1));
                            var biomeUV = new float2(uv1.x, uv1.y);
                            OriginalUVOne.Add(biomeUV);
                            UVOne.Add(biomeUV);

                            var uv2 = UnPack(SampleMap(voxelPoint, 2));
                            var colorUV = new float2(uv2.x, uv2.y);
                            OriginalUVTwo.Add(colorUV);
                            UVTwo.Add(colorUV);
                        }

                        edgeIndex++;
                    }
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CalculateNormals() {
                var vertexNormals = new NativeArray<float3>(Vertices.Length, Allocator.Temp);
                var triangleCont = Triangles.Length / 3;

                for (var i = 0; i < triangleCont; i++) {
                    var normalTriangleIndex = i * 3;
                    var vertexIndexA = Triangles[normalTriangleIndex];
                    var vertexIndexB = Triangles[normalTriangleIndex + 1];
                    var vertexIndexC = Triangles[normalTriangleIndex + 2];

                    var triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC);
                    vertexNormals[vertexIndexA] += triangleNormal;
                    vertexNormals[vertexIndexB] += triangleNormal;
                    vertexNormals[vertexIndexC] += triangleNormal;
                }

                for (var i = 0; i < vertexNormals.Length; i++) {
                    vertexNormals[i] = math.normalize(vertexNormals[i]);
                }
                
                Normals.CopyFrom(vertexNormals);
                vertexNormals.Dispose();
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float3 SurfaceNormal(int indexA, int indexB, int indexC) {
                var pointA = Vertices[indexA];
                var pointB = Vertices[indexB];
                var pointC = Vertices[indexC];
                var sideAb = pointB - pointA;
                var sideAc = pointC - pointA;
                return math.normalize(math.cross(sideAb, sideAc));
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float2 UnPack(float value) {
                var packed = math.asuint(value);
                var intX = (packed >> 16) & 0xFFFF;
                var intY = packed & 0xFFFF;
                var x = intX / 65535f;
                var y = intY / 65535f;
                return new float2(x, y);
            }
        }
        //Wind
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct GetWindMap : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public float VoxelScale;
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 CenterOffset;
            [ReadOnly] public Perlin3D PerlinNoise;
            [ReadOnly] public int Seed;
            public NativeArray<float3> Map;

            public void Execute(int index) {
                var z = index % ChunkResolution;
                var y = index / ChunkResolution % ChunkResolution;
                var x = index / (ChunkResolution * ChunkResolution);
                var worldPosition = new float3(x, y, z) * VoxelScale + NoisePosition - CenterOffset;
                Map[index] = GetWindDirection(worldPosition / 800);
            }

            private float3 GetWindDirection(float3 position) {
                var x = PerlinNoise.GetValue(Seed - 1, position);
                var y = PerlinNoise.GetValue(Seed, position);
                var z = PerlinNoise.GetValue(Seed + 1, position);
                return new float3(x, y, z);
            }
        }
        //foliage
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct GetGrassPosition : IJobParallelFor {
            [ReadOnly] public NativeArray<float3> Vertices;
            [ReadOnly] public NativeArray<int> Triangles;
            [ReadOnly] public int Seed;
            [ReadOnly] public int Density;
            
            [ReadOnly] public int Divisions;
            [ReadOnly] public float3 NodePosition;
            
            public NativeArray<Matrix4x4> GrassPositions;
            public void Execute(int index) {
                var triangleIndex = (index / Density) / 3;

                var idx0 = Triangles[triangleIndex * 3 + 0];
                var idx1 = Triangles[triangleIndex * 3 + 1];
                var idx2 = Triangles[triangleIndex * 3 + 2];

                var v0 = Vertices[idx0];
                var v1 = Vertices[idx1];
                var v2 = Vertices[idx2];

                var u = Rand((uint)(index + Seed));
                var v = Rand((uint)(index * 2 + Seed));

                if (u + v > 1.0f) {
                    u = 1.0f - u;
                    v = 1.0f - v;
                }

                var randomPoint = (1.0f - u - v) * v0 + u * v1 + v * v2 + NodePosition;
                GrassPositions[index] = Matrix4x4.TRS(randomPoint, Quaternion.identity, new float3(Divisions * Divisions));
            }
            private static float Rand(uint seed) {
                seed = (seed << 13) ^ seed;
                return 1.0f - ((seed * (seed * seed * 15731u + 789221u) + 1376312589u) & 0x7fffffff) / 2147483648.0f;
            }
        }
        
    }
}