using Unity.Mathematics;
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
    // 3D OpenSimplex2S case uses two offset rotated cube grids.

    /*
     * --- Rotation moved to TransformNoiseCoordinate method ---
     * const FNfloat R3 = (FNfloat)(2.0 / 3.0);
     * FNfloat r = (x + y + z) * R3; // Rotation, not skew
     * x = r - x; y = r - y; z = r - z;
     */
    public struct OpenSimplex2S : INoise3D
    {
        public real GetValue(int seed, real3 p)
        {
            var ijk = FastFloor(p);
            var pi = p - ijk;

            ijk *= Prime;

            var seed2 = seed + 1293373;

            var nMask = (int3)(-0.5f - pi);

            var p0 = pi + nMask;

            var a0 = 0.75f - p0.x * p0.x - p0.y * p0.y - p0.z * p0.z;
            var value = a0 * a0 * (a0 * a0) * GradCoord(seed, ijk.x + (nMask.x & PrimeX), ijk.y + (nMask.y & PrimeY),
                ijk.z + (nMask.z & PrimeZ), p0.x, p0.y, p0.z);

            var p1 = pi - 0.5f;

            var a1 = 0.75f - p1.x * p1.x - p1.y * p1.y - p1.z * p1.z;
            value += a1 * a1 * (a1 * a1) *
                     GradCoord(seed2, ijk.x + PrimeX, ijk.y + PrimeY, ijk.z + PrimeZ, p1.x, p1.y, p1.z);

            var aFlipMask0 = ((nMask | 1) << 1) * p1;
            var aFlipMask1 = (-2 - (nMask << 2)) * p1 - 1.0f;

            var skip5 = false;
            var a2 = aFlipMask0.x + a0;
            if (a2 > 0)
            {
                var x2 = p0.x - (nMask.x | 1);
                var y2 = p0.y;
                var z2 = p0.z;

                value += a2 * a2 * (a2 * a2) * GradCoord(seed, ijk.x + (~nMask.x & PrimeX), ijk.y + (nMask.y & PrimeY),
                    ijk.z + (nMask.z & PrimeZ), x2, y2, z2);
            }
            else
            {
                var a3 = aFlipMask0.y + aFlipMask0.z + a0;
                if (a3 > 0)
                {
                    var x3 = p0.x;
                    var y3 = p0.y - (nMask.y | 1);
                    var z3 = p0.z - (nMask.z | 1);
                    value += a3 * a3 * (a3 * a3) * GradCoord(seed, ijk.x + (nMask.x & PrimeX),
                        ijk.y + (~nMask.y & PrimeY), ijk.z + (~nMask.z & PrimeZ), x3, y3, z3);
                }

                var a4 = aFlipMask1.x + a1;
                if (a4 > 0)
                {
                    var x4 = (nMask.x | 1) + p1.x;
                    var y4 = p1.y;
                    var z4 = p1.z;
                    value += a4 * a4 * (a4 * a4) * GradCoord(seed2, ijk.x + (nMask.x & (PrimeX * 2)), ijk.y + PrimeY,
                        ijk.z + PrimeZ, x4, y4, z4);
                    skip5 = true;
                }
            }

            var skip9 = false;
            var a6 = aFlipMask0.y + a0;
            if (a6 > 0)
            {
                var x6 = p0.x;
                var y6 = p0.y - (nMask.y | 1);
                var z6 = p0.z;
                value += a6 * a6 * (a6 * a6) * GradCoord(seed, ijk.x + (nMask.x & PrimeX), ijk.y + (~nMask.y & PrimeY),
                    ijk.z + (nMask.z & PrimeZ), x6, y6, z6);
            }
            else
            {
                var a7 = aFlipMask0.x + aFlipMask0.z + a0;
                if (a7 > 0)
                {
                    var x7 = p0.x - (nMask.x | 1);
                    var y7 = p0.y;
                    var z7 = p0.z - (nMask.z | 1);
                    value += a7 * a7 * (a7 * a7) * GradCoord(seed, ijk.x + (~nMask.x & PrimeX),
                        ijk.y + (nMask.y & PrimeY), ijk.z + (~nMask.z & PrimeZ), x7, y7, z7);
                }

                var a8 = aFlipMask1.y + a1;
                if (a8 > 0)
                {
                    var x8 = p1.x;
                    var y8 = (nMask.y | 1) + p1.y;
                    var z8 = p1.z;
                    value += a8 * a8 * (a8 * a8) * GradCoord(seed2, ijk.x + PrimeX, ijk.y + (nMask.y & (PrimeY << 1)),
                        ijk.z + PrimeZ, x8, y8, z8);
                    skip9 = true;
                }
            }

            var skipD = false;
            var aA = aFlipMask0.z + a0;
            if (aA > 0)
            {
                var xA = p0.x;
                var yA = p0.y;
                var zA = p0.z - (nMask.z | 1);
                value += aA * aA * (aA * aA) * GradCoord(seed, ijk.x + (nMask.x & PrimeX), ijk.y + (nMask.y & PrimeY),
                    ijk.z + (~nMask.z & PrimeZ), xA, yA, zA);
            }
            else
            {
                var aB = aFlipMask0.x + aFlipMask0.y + a0;
                if (aB > 0)
                {
                    var xB = p0.x - (nMask.x | 1);
                    var yB = p0.y - (nMask.y | 1);
                    var zB = p0.z;
                    value += aB * aB * (aB * aB) * GradCoord(seed, ijk.x + (~nMask.x & PrimeX),
                        ijk.y + (~nMask.y & PrimeY), ijk.z + (nMask.z & PrimeZ), xB, yB, zB);
                }

                var aC = aFlipMask1.z + a1;
                if (aC > 0)
                {
                    var xC = p1.x;
                    var yC = p1.y;
                    var zC = (nMask.z | 1) + p1.z;
                    value += aC * aC * (aC * aC) * GradCoord(seed2, ijk.x + PrimeX, ijk.y + PrimeY,
                        ijk.z + (nMask.z & (PrimeZ << 1)), xC, yC, zC);
                    skipD = true;
                }
            }

            if (!skip5)
            {
                var a5 = aFlipMask1.y + aFlipMask1.z + a1;
                if (a5 > 0)
                {
                    var x5 = p1.x;
                    var y5 = (nMask.y | 1) + p1.y;
                    var z5 = (nMask.z | 1) + p1.z;
                    value += a5 * a5 * (a5 * a5) * GradCoord(seed2, ijk.x + PrimeX, ijk.y + (nMask.y & (PrimeY << 1)),
                        ijk.z + (nMask.z & (PrimeZ << 1)), x5, y5, z5);
                }
            }

            if (!skip9)
            {
                var a9 = aFlipMask1.x + aFlipMask1.z + a1;
                if (a9 > 0)
                {
                    var x9 = (nMask.x | 1) + p1.x;
                    var y9 = p1.y;
                    var z9 = (nMask.z | 1) + p1.z;
                    value += a9 * a9 * (a9 * a9) * GradCoord(seed2, ijk.x + (nMask.x & (PrimeX * 2)), ijk.y + PrimeY,
                        ijk.z + (nMask.z & (PrimeZ << 1)), x9, y9, z9);
                }
            }

            if (!skipD)
            {
                var aD = aFlipMask1.x + aFlipMask1.y + a1;
                if (aD > 0)
                {
                    var xD = (nMask.x | 1) + p1.x;
                    var yD = (nMask.y | 1) + p1.y;
                    var zD = p1.z;
                    value += aD * aD * (aD * aD) * GradCoord(seed2, ijk.x + (nMask.x & (PrimeX << 1)),
                        ijk.y + (nMask.y & (PrimeY << 1)), ijk.z + PrimeZ, xD, yD, zD);
                }
            }

            return value * 9.046026385208288f;
        }
    }
}