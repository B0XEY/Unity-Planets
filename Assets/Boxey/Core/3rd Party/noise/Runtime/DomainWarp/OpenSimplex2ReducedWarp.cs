using static Unbegames.Noise.Utils;
using Unity.Mathematics;

#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise {
	public struct OpenSimplex2ReducedWarp : IDomainWarp3D {
		public void Warp(int seed, float frequency, float amp, real3 ps, ref real3 p) {
      ps *= frequency;
      amp *= 7.71604938271605f;

      int3 ijk = FastRound(ps);

      real3 p0 = ps - ijk;
      int3 nSign = (int3)(-p0 - 1) | 1;

      real3 a0 = nSign * -p0;

      ijk *= Prime;      

      real3 pv = 0;
      
      real a = (0.6f - p0.x * p0.x) - (p0.y * p0.y + p0.z * p0.z);
      for (int l = 0; ; l++) {
        if (a > 0) {
          real aaaa = (a * a) * (a * a);          
          GradCoordOut(seed, ijk.x, ijk.y, ijk.z, out var po);
          pv += aaaa * po;          
        }

        real b = a;
        int3 ijk1 = ijk;
        real3 p1 = p0;
        
        if (a0.x >= a0.y && a0.x >= a0.z) {
          p1.x += nSign.x;
          b = b + a0.x + a0.x;
          ijk1.x -= nSign.x * PrimeX;
        } else if (a0.y > a0.x && a0.y >= a0.z) {
          p1.y += nSign.y;
          b = b + a0.y + a0.y;
          ijk1.y -= nSign.y * PrimeY;
        } else {
          p1.z += nSign.z;
          b = b + a0.z + a0.z;
          ijk1.z -= nSign.z * PrimeZ;
        }

        if (b > 1) {
          b -= 1;
          real bbbb = (b * b) * (b * b);
          GradCoordOut(seed, ijk1.x, ijk1.y, ijk1.z, out var po);

          pv += bbbb * po;
        }

        if (l == 1) break;

        a0 = 0.5f - a0;
        
        p0 = nSign * a0;
        
        a += (0.75f - a0.x) - (a0.y + a0.z);

        ijk += (nSign >> 1) & Prime; 

        nSign = -nSign;
        seed += 1293373;
      }

      p += pv * amp;
    }
	}
}
