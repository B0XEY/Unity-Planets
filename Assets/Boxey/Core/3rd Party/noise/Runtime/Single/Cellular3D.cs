using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unbegames.Noise.Utils;
using System.Runtime.CompilerServices;

#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif


namespace Unbegames.Noise {
  public interface ICellularDistanceFunction {
    void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0, ref real distance1, ref int closestHash);
  }


  public struct Cellular3D<T> : INoise3D where T : struct, ICellularDistanceFunction {
    public float mCellularJitterModifier;

    public T distanceFunc;

		public real GetValue(int seed, real3 p) {
      mCellularJitterModifier = 1; // temp

      int3 pr = FastRound(p);

      real distance0 = real.MaxValue;
      real distance1 = real.MaxValue;
      int closestHash = 0;

      float cellularJitter = 0.39614353f * mCellularJitterModifier;

      int3 primedBase = (pr - 1) * Prime;

      distanceFunc.CalcDistance(seed, p, pr, primedBase, cellularJitter, ref distance0, ref distance1, ref closestHash);

      return distance0 - 1; // TODO more return types
    }
	}

	public struct EuclideanCellularDistance : ICellularDistanceFunction {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0, ref real distance1, ref int closestHash) {
      for (int xi = pr.x - 1; xi <= pr.x + 1; xi++) {
        int yPrimed = primedBase.y;

        for (int yi = pr.y - 1; yi <= pr.y + 1; yi++) {
          int zPrimed = primedBase.z;

          for (int zi = pr.z - 1; zi <= pr.z + 1; zi++) {
            int hash = Hash(seed, primedBase.x, yPrimed, zPrimed);
            int idx = hash & (255 << 2);

            real vecX = (real)(xi - p.x) + RandVecs3D[idx] * cellularJitter;
            real vecY = (real)(yi - p.y) + RandVecs3D[idx | 1] * cellularJitter;
            real vecZ = (real)(zi - p.z) + RandVecs3D[idx | 2] * cellularJitter;

            real newDistance = vecX * vecX + vecY * vecY + vecZ * vecZ;

            distance1 = math.max(math.min(distance1, newDistance), distance0);
            if (newDistance < distance0) {
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

	public struct ManhattanCellularDistance : ICellularDistanceFunction {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0, ref real distance1, ref int closestHash) {
      for (int xi = pr.x - 1; xi <= pr.x + 1; xi++) {
        int yPrimed = primedBase.y;

        for (int yi = pr.y - 1; yi <= pr.y + 1; yi++) {
          int zPrimed = primedBase.z;

          for (int zi = pr.z - 1; zi <= pr.z + 1; zi++) {
            int hash = Hash(seed, primedBase.x, yPrimed, zPrimed);
            int idx = hash & (255 << 2);

            real vecX = (float)(xi - p.x) + RandVecs3D[idx] * cellularJitter;
            real vecY = (float)(yi - p.y) + RandVecs3D[idx | 1] * cellularJitter;
            real vecZ = (float)(zi - p.z) + RandVecs3D[idx | 2] * cellularJitter;

            real newDistance = abs(vecX) + abs(vecY) + abs(vecZ);

            distance1 = max(min(distance1, newDistance), distance0);
            if (newDistance < distance0) {
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

  public struct HybridCellularDistance : ICellularDistanceFunction {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CalcDistance(int seed, real3 p, int3 pr, int3 primedBase, float cellularJitter, ref real distance0, ref real distance1, ref int closestHash) {
      for (int xi = pr.x - 1; xi <= pr.x + 1; xi++) {
        int yPrimed = primedBase.y;

        for (int yi = pr.y - 1; yi <= pr.y + 1; yi++) {
          int zPrimed = primedBase.z;

          for (int zi = pr.z - 1; zi <= pr.z + 1; zi++) {
            int hash = Hash(seed, primedBase.x, yPrimed, zPrimed);
            int idx = hash & (255 << 2);

            real vecX = (float)(xi - p.x) + RandVecs3D[idx] * cellularJitter;
            real vecY = (float)(yi - p.y) + RandVecs3D[idx | 1] * cellularJitter;
            real vecZ = (float)(zi - p.z) + RandVecs3D[idx | 2] * cellularJitter;

            real newDistance = (abs(vecX) + abs(vecY) + abs(vecZ)) + (vecX * vecX + vecY * vecY + vecZ * vecZ);

            distance1 = max(min(distance1, newDistance), distance0);
            if (newDistance < distance0) {
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
