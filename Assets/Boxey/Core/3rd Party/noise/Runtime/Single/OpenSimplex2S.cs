using Unity.Mathematics;
using static Unbegames.Noise.Utils;

#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise {
  // 3D OpenSimplex2S case uses two offset rotated cube grids.

  /*
   * --- Rotation moved to TransformNoiseCoordinate method ---
   * const FNfloat R3 = (FNfloat)(2.0 / 3.0);
   * FNfloat r = (x + y + z) * R3; // Rotation, not skew
   * x = r - x; y = r - y; z = r - z;
  */
  public struct OpenSimplex2S : INoise3D {
		public real GetValue(int seed, real3 p) {
      int3 ijk = FastFloor(p);
      real3 pi = p - ijk;

      ijk *= Prime;
      
      int seed2 = seed + 1293373;

      int3 nMask = (int3)(-0.5f - pi);     

      real3 p0 = pi + nMask;      

      real a0 = 0.75f - p0.x * p0.x - p0.y * p0.y - p0.z * p0.z;
      real value = (a0 * a0) * (a0 * a0) * GradCoord(seed, ijk.x + (nMask.x & PrimeX), ijk.y + (nMask.y & PrimeY), ijk.z + (nMask.z & PrimeZ), p0.x, p0.y, p0.z);

      real3 p1 = pi - 0.5f;

      real a1 = 0.75f - p1.x * p1.x - p1.y * p1.y - p1.z * p1.z;
      value += (a1 * a1) * (a1 * a1) * GradCoord(seed2, ijk.x + PrimeX, ijk.y + PrimeY, ijk.z + PrimeZ, p1.x, p1.y, p1.z);

      real3 aFlipMask0 = ((nMask | 1) << 1) * p1;
      real3 aFlipMask1 = (-2 - (nMask << 2)) * p1 - 1.0f;     

      bool skip5 = false;
      real a2 = aFlipMask0.x + a0;
      if (a2 > 0) {
        real x2 = p0.x - (nMask.x | 1);
        real y2 = p0.y;
        real z2 = p0.z;

        value += (a2 * a2) * (a2 * a2) * GradCoord(seed, ijk.x + (~nMask.x & PrimeX), ijk.y + (nMask.y & PrimeY), ijk.z + (nMask.z & PrimeZ), x2, y2, z2);
      } else {
        real a3 = aFlipMask0.y + aFlipMask0.z + a0;
        if (a3 > 0) {
          real x3 = p0.x;
          real y3 = p0.y - (nMask.y | 1);
          real z3 = p0.z - (nMask.z | 1);
          value += (a3 * a3) * (a3 * a3) * GradCoord(seed, ijk.x + (nMask.x & PrimeX), ijk.y + (~nMask.y & PrimeY), ijk.z + (~nMask.z & PrimeZ), x3, y3, z3);
        }

        real a4 = aFlipMask1.x + a1;
        if (a4 > 0) {
          real x4 = (nMask.x | 1) + p1.x;
          real y4 = p1.y;
          real z4 = p1.z;
          value += (a4 * a4) * (a4 * a4) * GradCoord(seed2, ijk.x + (nMask.x & (PrimeX * 2)), ijk.y + PrimeY, ijk.z + PrimeZ, x4, y4, z4);
          skip5 = true;
        }
      }

      bool skip9 = false;
      real a6 = aFlipMask0.y + a0;
      if (a6 > 0) {
        real x6 = p0.x;
        real y6 = p0.y - (nMask.y | 1);
        real z6 = p0.z;
        value += (a6 * a6) * (a6 * a6) * GradCoord(seed, ijk.x + (nMask.x & PrimeX), ijk.y + (~nMask.y & PrimeY), ijk.z + (nMask.z & PrimeZ), x6, y6, z6);
      } else {
        real a7 = aFlipMask0.x + aFlipMask0.z + a0;
        if (a7 > 0) {
          real x7 = p0.x - (nMask.x | 1);
          real y7 = p0.y;
          real z7 = p0.z - (nMask.z | 1);
          value += (a7 * a7) * (a7 * a7) * GradCoord(seed, ijk.x + (~nMask.x & PrimeX), ijk.y + (nMask.y & PrimeY), ijk.z + (~nMask.z & PrimeZ), x7, y7, z7);
        }

        real a8 = aFlipMask1.y + a1;
        if (a8 > 0) {
          real x8 = p1.x;
          real y8 = (nMask.y | 1) + p1.y;
          real z8 = p1.z;
          value += (a8 * a8) * (a8 * a8) * GradCoord(seed2, ijk.x + PrimeX, ijk.y + (nMask.y & (PrimeY << 1)), ijk.z + PrimeZ, x8, y8, z8);
          skip9 = true;
        }
      }

      bool skipD = false;
      real aA = aFlipMask0.z + a0;
      if (aA > 0) {
        real xA = p0.x;
        real yA = p0.y;
        real zA = p0.z - (nMask.z | 1);
        value += (aA * aA) * (aA * aA) * GradCoord(seed, ijk.x + (nMask.x & PrimeX), ijk.y + (nMask.y & PrimeY), ijk.z + (~nMask.z & PrimeZ), xA, yA, zA);
      } else {
        real aB = aFlipMask0.x + aFlipMask0.y + a0;
        if (aB > 0) {
          real xB = p0.x - (nMask.x | 1);
          real yB = p0.y - (nMask.y | 1);
          real zB = p0.z;
          value += (aB * aB) * (aB * aB) * GradCoord(seed, ijk.x + (~nMask.x & PrimeX), ijk.y + (~nMask.y & PrimeY), ijk.z + (nMask.z & PrimeZ), xB, yB, zB);
        }

        real aC = aFlipMask1.z + a1;
        if (aC > 0) {
          real xC = p1.x;
          real yC = p1.y;
          real zC = (nMask.z | 1) + p1.z;
          value += (aC * aC) * (aC * aC) * GradCoord(seed2, ijk.x + PrimeX, ijk.y + PrimeY, ijk.z + (nMask.z & (PrimeZ << 1)), xC, yC, zC);
          skipD = true;
        }
      }

      if (!skip5) {
        real a5 = aFlipMask1.y + aFlipMask1.z + a1;
        if (a5 > 0) {
          real x5 = p1.x;
          real y5 = (nMask.y | 1) + p1.y;
          real z5 = (nMask.z | 1) + p1.z;
          value += (a5 * a5) * (a5 * a5) * GradCoord(seed2, ijk.x + PrimeX, ijk.y + (nMask.y & (PrimeY << 1)), ijk.z + (nMask.z & (PrimeZ << 1)), x5, y5, z5);
        }
      }

      if (!skip9) {
        real a9 = aFlipMask1.x + aFlipMask1.z + a1;
        if (a9 > 0) {
          real x9 = (nMask.x | 1) + p1.x;
          real y9 = p1.y;
          real z9 = (nMask.z | 1) + p1.z;
          value += (a9 * a9) * (a9 * a9) * GradCoord(seed2, ijk.x + (nMask.x & (PrimeX * 2)), ijk.y + PrimeY, ijk.z + (nMask.z & (PrimeZ << 1)), x9, y9, z9);
        }
      }

      if (!skipD) {
        real aD = aFlipMask1.x + aFlipMask1.y + a1;
        if (aD > 0) {
          real xD = (nMask.x | 1) + p1.x;
          real yD = (nMask.y | 1) + p1.y;
          real zD = p1.z;
          value += (aD * aD) * (aD * aD) * GradCoord(seed2, ijk.x + (nMask.x & (PrimeX << 1)), ijk.y + (nMask.y & (PrimeY << 1)), ijk.z + PrimeZ, xD, yD, zD);
        }
      }

      return value * 9.046026385208288f;
    }
	}
}
