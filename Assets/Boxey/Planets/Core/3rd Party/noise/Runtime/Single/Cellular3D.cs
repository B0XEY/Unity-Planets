using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unbegames.Noise.Utils;

#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif


namespace Unbegames.Noise
{
    public interface ICellularDistanceFunction
    {
        void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0,
            ref real distance1, ref int closestHash);
    }


    public struct Cellular3D<T> : INoise3D where T : struct, ICellularDistanceFunction
    {
        public float mCellularJitterModifier;

        public T distanceFunc;

        public real GetValue(int seed, real3 p)
        {
            mCellularJitterModifier = 1; // temp

            var pr = FastRound(p);

            var distance0 = real.MaxValue;
            var distance1 = real.MaxValue;
            var closestHash = 0;

            var cellularJitter = 0.39614353f * mCellularJitterModifier;

            var primedBase = (pr - 1) * Prime;

            distanceFunc.CalcDistance(seed, p, pr, primedBase, cellularJitter, ref distance0, ref distance1,
                ref closestHash);

            return distance0 - 1; // TODO more return types
        }
    }

    public struct EuclideanCellularDistance : ICellularDistanceFunction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0,
            ref real distance1, ref int closestHash)
        {
            for (var xi = pr.x - 1; xi <= pr.x + 1; xi++)
            {
                var yPrimed = primedBase.y;

                for (var yi = pr.y - 1; yi <= pr.y + 1; yi++)
                {
                    var zPrimed = primedBase.z;

                    for (var zi = pr.z - 1; zi <= pr.z + 1; zi++)
                    {
                        var hash = Hash(seed, primedBase.x, yPrimed, zPrimed);
                        var idx = hash & (255 << 2);

                        var vecX = xi - p.x + RandVecs3D[idx] * cellularJitter;
                        var vecY = yi - p.y + RandVecs3D[idx | 1] * cellularJitter;
                        var vecZ = zi - p.z + RandVecs3D[idx | 2] * cellularJitter;

                        var newDistance = vecX * vecX + vecY * vecY + vecZ * vecZ;

                        distance1 = max(min(distance1, newDistance), distance0);
                        if (newDistance < distance0)
                        {
                            distance0 = newDistance;
                            closestHash = hash;
                        }

                        zPrimed += PrimeZ;
                    }

                    yPrimed += PrimeY;
                }

                primedBase.x += PrimeX;
            }

            distance0 = sqrt(distance0);
        }
    }

    public struct ManhattanCellularDistance : ICellularDistanceFunction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0,
            ref real distance1, ref int closestHash)
        {
            for (var xi = pr.x - 1; xi <= pr.x + 1; xi++)
            {
                var yPrimed = primedBase.y;

                for (var yi = pr.y - 1; yi <= pr.y + 1; yi++)
                {
                    var zPrimed = primedBase.z;

                    for (var zi = pr.z - 1; zi <= pr.z + 1; zi++)
                    {
                        var hash = Hash(seed, primedBase.x, yPrimed, zPrimed);
                        var idx = hash & (255 << 2);

                        var vecX = xi - p.x + RandVecs3D[idx] * cellularJitter;
                        var vecY = yi - p.y + RandVecs3D[idx | 1] * cellularJitter;
                        var vecZ = zi - p.z + RandVecs3D[idx | 2] * cellularJitter;

                        var newDistance = abs(vecX) + abs(vecY) + abs(vecZ);

                        distance1 = max(min(distance1, newDistance), distance0);
                        if (newDistance < distance0)
                        {
                            distance0 = newDistance;
                            closestHash = hash;
                        }

                        zPrimed += PrimeZ;
                    }

                    yPrimed += PrimeY;
                }

                primedBase.x += PrimeX;
            }
        }
    }

    public struct HybridCellularDistance : ICellularDistanceFunction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0,
            ref real distance1, ref int closestHash)
        {
            for (var xi = pr.x - 1; xi <= pr.x + 1; xi++)
            {
                var yPrimed = primedBase.y;

                for (var yi = pr.y - 1; yi <= pr.y + 1; yi++)
                {
                    var zPrimed = primedBase.z;

                    for (var zi = pr.z - 1; zi <= pr.z + 1; zi++)
                    {
                        var hash = Hash(seed, primedBase.x, yPrimed, zPrimed);
                        var idx = hash & (255 << 2);

                        var vecX = xi - p.x + RandVecs3D[idx] * cellularJitter;
                        var vecY = yi - p.y + RandVecs3D[idx | 1] * cellularJitter;
                        var vecZ = zi - p.z + RandVecs3D[idx | 2] * cellularJitter;

                        var newDistance = abs(vecX) + abs(vecY) + abs(vecZ) + (vecX * vecX + vecY * vecY + vecZ * vecZ);

                        distance1 = max(min(distance1, newDistance), distance0);
                        if (newDistance < distance0)
                        {
                            distance0 = newDistance;
                            closestHash = hash;
                        }

                        zPrimed += PrimeZ;
                    }

                    yPrimed += PrimeY;
                }

                primedBase.x += PrimeX;
            }
        }
    }
}