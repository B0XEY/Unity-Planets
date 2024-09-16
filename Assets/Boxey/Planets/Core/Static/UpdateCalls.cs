using Boxey.Planets.Core.Classes;
using Unity.Collections;
using Unity.Mathematics;

namespace Boxey.Planets.Core.Static {
    public static class UpdateCalls {
        public struct NodeUpdateCall {
            public readonly Node ParentNode;
            public readonly Node NodeToUpdate;

            public NodeUpdateCall(Node parentNode, Node nodeToUpdate) {
                ParentNode = parentNode;
                NodeToUpdate = nodeToUpdate;
            }
        }
        public struct NodeInfo {
            public readonly float VoxelScale;
            public readonly int StartIndex;
            public readonly float3 NoisePosition;
            public readonly float3 CenterOffset;
            public readonly float2[] TerraformMap;

            public NodeInfo(float scale, int index, float3 position, float3 offset, float2[] map) {
                VoxelScale = scale;
                StartIndex = index;
                NoisePosition = position;
                CenterOffset = offset;
                TerraformMap = map;
            }
        }
        public struct NodeJobInfo {
            public readonly float VoxelScale;
            public readonly int StartIndex;
            public readonly float3 NoisePosition;
            public readonly float3 CenterOffset;

            public NodeJobInfo(NodeInfo info) {
                VoxelScale = info.VoxelScale;
                StartIndex = info.StartIndex;
                NoisePosition = info.NoisePosition;
                CenterOffset = info.CenterOffset;
            }
        }
        public struct TerraformInfo {
            public readonly float3 TerraformLocation;
            public readonly float4 TerraformData; //x: radius, y: speed, zw: color
            public readonly float DeltaTime;
            public readonly bool AddTerrain;

            public TerraformInfo(float3 terraformPoint, float4 terraformData, float deltaTime, bool addTerrain) {
                TerraformLocation = terraformPoint;
                TerraformData = terraformData;
                DeltaTime = deltaTime;
                AddTerrain = addTerrain;
            }
        }
    }
}