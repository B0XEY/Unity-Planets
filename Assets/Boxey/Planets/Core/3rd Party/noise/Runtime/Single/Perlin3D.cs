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

namespace Unbegames.Noise {
	public struct Perlin3D : INoise3D {
		public real GetValue(int seed, real3 p) {     
      int3 p0 = FastFloor(p);

      real3 d0 = p - p0;
      real3 d1 = d0 - 1;

      real3 s = InterpQuintic(d0);      

      p0 *= Prime;
      
      int3 p1 = p0 + Prime;     

      real xf00 = lerp(GradCoord(seed, p0.x, p0.y, p0.z, d0.x, d0.y, d0.z), GradCoord(seed, p1.x, p0.y, p0.z, d1.x, d0.y, d0.z), s.x);
      real xf10 = lerp(GradCoord(seed, p0.x, p1.y, p0.z, d0.x, d1.y, d0.z), GradCoord(seed, p1.x, p1.y, p0.z, d1.x, d1.y, d0.z), s.x);
      real xf01 = lerp(GradCoord(seed, p0.x, p0.y, p1.z, d0.x, d0.y, d1.z), GradCoord(seed, p1.x, p0.y, p1.z, d1.x, d0.y, d1.z), s.x);
      real xf11 = lerp(GradCoord(seed, p0.x, p1.y, p1.z, d0.x, d1.y, d1.z), GradCoord(seed, p1.x, p1.y, p1.z, d1.x, d1.y, d1.z), s.x);

      real yf0 = lerp(xf00, xf10, s.y);
      real yf1 = lerp(xf01, xf11, s.y);

      return lerp(yf0, yf1, s.z) * 0.964921414852142333984375f;
    }
	}
}
