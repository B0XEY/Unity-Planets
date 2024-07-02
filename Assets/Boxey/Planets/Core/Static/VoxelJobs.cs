using System.Runtime.CompilerServices;
using Unbegames.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Boxey.Planets.Core.Static {
    public static class VoxelJobs {
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct GetPlanetNoise : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public float Radius;
            [ReadOnly] public int CurveSamples;
            [ReadOnly] public int VoxelScale;
            
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
            // Returned Map using float 4 so we can pass mor data out like ore, material color, ect
            public NativeArray<float4> Map;
            
            public void Execute(int index) {
                var z = index % ChunkResolution;
                var y = (index / ChunkResolution) % ChunkResolution;
                var x = index / (ChunkResolution * ChunkResolution);
                var voxelPosition = ((new float3(x, y, z) * VoxelScale) + NoisePosition) - CenterOffset;
                var distanceFromCenter = math.distance(voxelPosition, PlanetCenter);
                var radiusValue = math.saturate((distanceFromCenter - Radius) / (-Radius)) * 2 - 1;
                var multiplier = DoNoiseLayers ? 2.5f : 1;
                var mapValue = radiusValue * multiplier;
                if (DoNoiseLayers) {
                    mapValue += GetValueAtPoint(voxelPosition);
                }
                //xyz will be used for uvs, and mesh colors.
                Map[index] = new float4(0, 0, 0, mapValue);
            }
            private float GetValueAtPoint(float3 position) {
                var height = 0f;
                for (var i = 0; i < NoiseLayers; i++) {
                    var sampleX = (position.x) / NoiseFloatDataOne[i].x + NoiseOffsets[i].x;
                    var sampleY = (position.y) / NoiseFloatDataOne[i].x + NoiseOffsets[i].y;
                    var sampleZ = (position.z) / NoiseFloatDataOne[i].x + NoiseOffsets[i].z;
                    var pointHeight = 0f;
                    var frequency = 1f;
                    var amplitude = 1f;

                    for (var o = 0; o < NoiseIntData[i].x; o++) {
                        sampleX *= frequency;
                        sampleY *= frequency;
                        sampleZ *= frequency;
                        var point = new float3(sampleX, sampleY, sampleZ);
                        pointHeight += (PerlinNoise.GetValue(Seed, point) + 1) * 0.5f * amplitude;
                        frequency *= NoiseFloatDataOne[i].y;
                        amplitude *= NoiseFloatDataOne[i].z;
                    }

                    var clampedValue = (pointHeight - NoiseFloatDataTwo[i].x) / (NoiseFloatDataTwo[i].y - NoiseFloatDataTwo[i].x);
                    var normalizedValue = EvaluateCurve(clampedValue, CurveSamples * i) * 0.01f * NoiseFloatDataTwo[i].z;

                    height += (normalizedValue * NoiseFloatDataOne[i].w) * NoiseIntData[i].y;
                }

                return height;
            }
            private float EvaluateCurve(float time, int offset) {
                var length = CurveSamples - 1;
                var clampedValue = time < 0 ? 0 : (time > 1 ? 1 : time);
                var curveIndex = (clampedValue * length);
                var floorIndex = (int)math.floor(curveIndex);
                if (floorIndex == length) { 
                    return NoiseCurves[length + offset]; 
                }
 
                var value1 = NoiseCurves[floorIndex + offset];
                var value2 = NoiseCurves[(floorIndex + 1) + offset];
                return math.lerp(value1, value2, math.frac(curveIndex));
            }
        }
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct MarchCubes : IJob {
            [ReadOnly] public int4 CubeIntData; //XYZ: center offset, w: size
            [ReadOnly] public float3 CubeFloatData; //x: Voxel Scale, y: Create Gate, z: Value Gate
            
            [ReadOnly] public NativeArray<float4> NoiseMap;
            [ReadOnly] public NativeArray<float> ModMap;
            
            public NativeList<float3> Vertices;
            public NativeList<float3> Normals;
            public NativeList<int> Triangles;
            
            public NativeList<float3> OriginalVertices;
            public NativeList<float3> OriginalNormals;
            public NativeList<int> OriginalTriangles;
            
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float SampleMap(int3 point){
                var index = point.x + (CubeIntData.w + 1) * (point.y + (CubeIntData.w + 1) * point.z);
                return NoiseMap[index].w + ModMap[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetCubeConfig(int3 point) {
                var configIndex = 0;
                var cornerTable = VoxelTables.CornerTable;

                for (var i = 0; i < 8; i++) {
                    var sample = SampleMap(point + cornerTable[i]);
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
                            var noiseValue = SampleMap(point);
                            if (math.abs(noiseValue - CubeFloatData.y) < .001f) {
                                continue;
                            }
                            CreateCube(point);
                        }
                    }
                }
                CalculateNormals(true);
                CreateSkirt();
                CalculateNormals(false);
            }
            private void CreateCube(int3 voxelPoint) {
                var configIndex = GetCubeConfig(voxelPoint);
                if (configIndex is 0 or 255) {
                    return;
                }
            
                var worldPosition = (voxelPoint * (int)CubeFloatData.x) - CubeIntData.xyz;
                var edgeIndex = 0;
                for (var i = 0; i < 5; i++) { //Mesh triangles
                    for (var j = 0; j < 3; j++) { // Mesh Vertex
                        var indice = VoxelTables.TriangleTable[configIndex * 16 + edgeIndex];
                        if (indice == -1) {
                            return;
                        }
                        //Get Vert Positions
                        var corner1 = VoxelTables.CornerTable[VoxelTables.EdgeIndexes[indice * 2]];
                        var corner2 = VoxelTables.CornerTable[VoxelTables.EdgeIndexes[indice * 2 + 1]];
                        float3 vert1 = worldPosition + corner1 * (int)CubeFloatData.x;
                        float3 vert2 = worldPosition + corner2 * (int)CubeFloatData.x;
                        var vert1Sample = SampleMap(voxelPoint + corner1);
                        var vert2Sample = SampleMap(voxelPoint + corner2);
                        var difference = vert2Sample - vert1Sample;
                        var vertPosition = vert1;
                        if (difference != 0) {
                            vertPosition += (vert2 - vert1) * ((CubeFloatData.z - vert1Sample) / difference);
                        }
                        //Add to the lists
                        if (math.all(math.isfinite(vertPosition))) {
                            OriginalVertices.Add(vertPosition);
                            Vertices.Add(vertPosition);
                            OriginalTriangles.Add(OriginalVertices.Length - 1);
                            Triangles.Add(OriginalVertices.Length - 1);
                        }
                        
                        edgeIndex++;
                    }
                }
            }
            private void CreateSkirt() {
                var skirtHeight = CubeFloatData.x;
                var flareAmount = skirtHeight * 0.1f;
                for (var i = 0; i < OriginalVertices.Length; i++) {
                    var vert = OriginalVertices[i];
                    var normal = OriginalNormals[i];
                    var perpVector = math.cross(normal, new float3(0.0f, 1.0f, 0.0f));
                    if (math.length(perpVector) == 0) {
                        perpVector = math.cross(normal, new float3(1.0f, 0.0f, 0.0f));
                    }
                    perpVector = math.normalize(perpVector) * flareAmount;
                    
                    var skirtVert = vert - normal * skirtHeight + perpVector;
                    if (!math.all(math.isfinite(skirtVert))) {
                        continue;
                    }
                    Vertices.Add(vert);
                    Vertices.Add(skirtVert);
                    if (i % 3 != 0) {
                        continue;
                    }
                    var baseIndex = Vertices.Length - 2;
                    Triangles.Add(baseIndex);
                    Triangles.Add(baseIndex + 1);
                    Triangles.Add(baseIndex + 3);

                    Triangles.Add(baseIndex);
                    Triangles.Add(baseIndex + 3);
                    Triangles.Add(baseIndex + 2);
                    //other sided
                    Triangles.Add(baseIndex);
                    Triangles.Add(baseIndex + 3);
                    Triangles.Add(baseIndex + 1);

                    Triangles.Add(baseIndex);
                    Triangles.Add(baseIndex + 2);
                    Triangles.Add(baseIndex + 3);
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CalculateNormals(bool firstPass) {
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
        
                if (firstPass) {
                    OriginalNormals.CopyFrom(vertexNormals);
                }else {
                    Normals.CopyFrom(vertexNormals);
                }
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
        }
        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public struct Terraform : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public int VoxelScale;
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 CenterOffset;
            [ReadOnly] public float3 TerraformPoint;
            
            [ReadOnly] public float4 TerraformData; //x: radius, y: speed, z: multiplier, w: deltaTime
            
            public NativeArray<float> MapData;
            public void Execute(int index){
                var z = index % ChunkResolution;
                var y = (index / ChunkResolution) % ChunkResolution;
                var x = index / (ChunkResolution * ChunkResolution);
                var voxelPosition = ((new float3(x, y, z) * VoxelScale) + NoisePosition) - CenterOffset; //i think issue is here
                var distanceFromPoint = math.distance(voxelPosition, TerraformPoint);
                if (distanceFromPoint < TerraformData.x && distanceFromPoint > - TerraformData.x) {
                    var weight = SmoothStep(TerraformData.x, TerraformData.x * 0.7f, distanceFromPoint);
                    MapData[index] += -(TerraformData.y * weight * TerraformData.w) * TerraformData.z;
                }
                
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float SmoothStep(float min, float max, float time) {
                time = math.saturate((time - min) / (max - min));
                return time * time * (3 - 2 * time);
            }
        }
        //Other
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct GetObjectNoise : IJobParallelFor {
            [ReadOnly] public int ChunkResolution;
            [ReadOnly] public int VoxelScale;
            
            [ReadOnly] public float3 NoisePosition;
            [ReadOnly] public float3 CenterOffset;
            
            // Returned Map
            public NativeArray<float> Map;
            
            public void Execute(int index) {
                var z = index % ChunkResolution;
                var y = (index / ChunkResolution) % ChunkResolution;
                var x = index / (ChunkResolution * ChunkResolution);
                var voxelPosition = (((new float3(x, y, z) * VoxelScale) + NoisePosition) - CenterOffset) / 100;
                var pointHeight = (noise.snoise(voxelPosition) + 1 ) * 0.5f;
                Map[index] = pointHeight;
            }
        }
    }
}