using static Unity.Mathematics.math;
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
	public struct BasicGridWarp : IDomainWarp3D {     
		public void Warp(int seed, float frequency, float amp, real3 ps, ref real3 p) {     
      real3 pf = ps * frequency;      
      int3 p0 = FastFloor(pf);
      
      real3 s = InterpHermite(pf - p0);
      
      p0 *= Prime;

      int3 p1 = p0 + Prime;

      int hash0 = Hash(seed, p0.x, p0.y, p0.z) & (255 << 2);
      int hash1 = Hash(seed, p1.x, p0.y, p0.z) & (255 << 2);

      real lx0x = lerp(RandVecs3D[hash0], RandVecs3D[hash1], s.x);
      real ly0x = lerp(RandVecs3D[hash0 | 1], RandVecs3D[hash1 | 1], s.x);
      real lz0x = lerp(RandVecs3D[hash0 | 2], RandVecs3D[hash1 | 2], s.x);

      hash0 = Hash(seed, p0.x, p1.y, p0.z) & (255 << 2);
      hash1 = Hash(seed, p1.x, p1.y, p0.z) & (255 << 2);

      real lx1x = lerp(RandVecs3D[hash0], RandVecs3D[hash1], s.x);
      real ly1x = lerp(RandVecs3D[hash0 | 1], RandVecs3D[hash1 | 1], s.x);
      real lz1x = lerp(RandVecs3D[hash0 | 2], RandVecs3D[hash1 | 2], s.x);

      real lx0y = lerp(lx0x, lx1x, s.y);
      real ly0y = lerp(ly0x, ly1x, s.y);
      real lz0y = lerp(lz0x, lz1x, s.y);

      hash0 = Hash(seed, p0.x, p0.y, p1.z) & (255 << 2);
      hash1 = Hash(seed, p1.x, p0.y, p1.z) & (255 << 2);

      lx0x = lerp(RandVecs3D[hash0], RandVecs3D[hash1], s.x);
      ly0x = lerp(RandVecs3D[hash0 | 1], RandVecs3D[hash1 | 1], s.x);
      lz0x = lerp(RandVecs3D[hash0 | 2], RandVecs3D[hash1 | 2], s.x);

      hash0 = Hash(seed, p0.x, p1.y, p1.z) & (255 << 2);
      hash1 = Hash(seed, p1.x, p1.y, p1.z) & (255 << 2);

      lx1x = lerp(RandVecs3D[hash0], RandVecs3D[hash1], s.x);
      ly1x = lerp(RandVecs3D[hash0 | 1], RandVecs3D[hash1 | 1], s.x);
      lz1x = lerp(RandVecs3D[hash0 | 2], RandVecs3D[hash1 | 2], s.x);

      p.x += lerp(lx0y, lerp(lx0x, lx1x, s.y), s.z) * amp;
      p.y += lerp(ly0y, lerp(ly0x, ly1x, s.y), s.z) * amp;
      p.z += lerp(lz0y, lerp(lz0x, lz1x, s.y), s.z) * amp;
    }
	}
}
