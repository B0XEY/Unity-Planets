using Unity.Mathematics;
using static Unbegames.Noise.Utils;

#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
using real4 = Unity.Mathematics.double4;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
using real4 = Unity.Mathematics.float4;
#endif

namespace Unbegames.Noise {
	public struct ValueCubic3D : INoise3D {
		public real GetValue(int seed, real3 p) {
      int3 p1 = FastFloor(p);

      real3 s = p - p1;
      
      p1 *= Prime;

      int3 p0 = p1 - Prime;
      int3 p2 = p1 + Prime;
           
      int3 p3 = p1 + unchecked(Prime * 2);

      var s1 = new real4 {
        x = ValCoord(seed, p0),
        y = ValCoord(seed, p0.x, p1.y, p0.z),
        z = ValCoord(seed, p0.x, p2.y, p0.z),
        w = ValCoord(seed, p0.x, p3.y, p0.z)
      };
      var s2 = new real4 {
        x = ValCoord(seed, p1.x, p0.y, p0.z),
        y = ValCoord(seed, p1.x, p1.y, p0.z),
        z = ValCoord(seed, p1.x, p2.y, p0.z),
        w = ValCoord(seed, p1.x, p3.y, p0.z)
      };      
      var s3 = new real4 {
        x = ValCoord(seed, p2.x, p0.y, p0.z),
        y = ValCoord(seed, p2.x, p1.y, p0.z),
        z = ValCoord(seed, p2.x, p2.y, p0.z),
        w = ValCoord(seed, p2.x, p3.y, p0.z)
      };      
      var s4 = new real4 {
        x = ValCoord(seed, p3.x, p0.y, p0.z),
        y = ValCoord(seed, p3.x, p1.y, p0.z),
        z = ValCoord(seed, p3.x, p2.y, p0.z),
        w = ValCoord(seed, p3.x, p3.y, p0.z)
      };

      var t1 = new real4 {
        x = ValCoord(seed, p0.x, p0.y, p1.z),
        y = ValCoord(seed, p0.x, p1.y, p1.z),
        z = ValCoord(seed, p0.x, p2.y, p1.z),
        w = ValCoord(seed, p0.x, p3.y, p1.z)
      };      
      var t2 = new real4 {
        x = ValCoord(seed, p1.x, p0.y, p1.z),
        y = ValCoord(seed, p1.x, p1.y, p1.z),
        z = ValCoord(seed, p1.x, p2.y, p1.z),
        w = ValCoord(seed, p1.x, p3.y, p1.z)
      };      
      var t3 = new real4 {
        x = ValCoord(seed, p2.x, p0.y, p1.z),
        y = ValCoord(seed, p2.x, p1.y, p1.z),
        z = ValCoord(seed, p2.x, p2.y, p1.z),
        w = ValCoord(seed, p2.x, p3.y, p1.z)
      };      
      var t4 = new real4 {
        x = ValCoord(seed, p3.x, p0.y, p1.z),
        y = ValCoord(seed, p3.x, p1.y, p1.z),
        z = ValCoord(seed, p3.x, p2.y, p1.z),
        w = ValCoord(seed, p3.x, p3.y, p1.z)
      };     

      var u1 = new real4 {
        x = ValCoord(seed, p0.x, p0.y, p2.z),
        y = ValCoord(seed, p0.x, p1.y, p2.z),
        z = ValCoord(seed, p0.x, p2.y, p2.z),
        w = ValCoord(seed, p0.x, p3.y, p2.z)
      };  
      var u2 = new real4 {
        x = ValCoord(seed, p1.x, p0.y, p2.z),
        y = ValCoord(seed, p1.x, p1.y, p2.z),
        z = ValCoord(seed, p1.x, p2.y, p2.z),
        w = ValCoord(seed, p1.x, p3.y, p2.z),
      };   
      var u3 = new real4 {
        x = ValCoord(seed, p2.x, p0.y, p2.z),
        y = ValCoord(seed, p2.x, p1.y, p2.z),
        z = ValCoord(seed, p2.x, p2.y, p2.z),
        w = ValCoord(seed, p2.x, p3.y, p2.z)
      };
      var u4 = new real4 {
        x = ValCoord(seed, p3.x, p0.y, p2.z),
        y = ValCoord(seed, p3.x, p1.y, p2.z),
        z = ValCoord(seed, p3.x, p2.y, p2.z),
        w = ValCoord(seed, p3.x, p3.y, p2.z)
      };     

      var v1 = new real4 {
        x = ValCoord(seed, p0.x, p0.y, p3.z),
        y = ValCoord(seed, p0.x, p1.y, p3.z),
        z = ValCoord(seed, p0.x, p2.y, p3.z),
        w = ValCoord(seed, p0.x, p3.y, p3.z)
      };
      var v2 = new real4 {
        x = ValCoord(seed, p1.x, p0.y, p3.z),
        y = ValCoord(seed, p1.x, p1.y, p3.z),
        z = ValCoord(seed, p1.x, p2.y, p3.z),
        w = ValCoord(seed, p1.x, p3.y, p3.z)
      };
      var v3 = new real4 {
        x = ValCoord(seed, p2.x, p0.y, p3.z),
        y = ValCoord(seed, p2.x, p1.y, p3.z),
        z = ValCoord(seed, p2.x, p2.y, p3.z),
        w = ValCoord(seed, p2.x, p3.y, p3.z)
      };
      var v4 = new real4 {
        x = ValCoord(seed, p3.x, p0.y, p3.z),
        y = ValCoord(seed, p3.x, p1.y, p3.z),
        z = ValCoord(seed, p3.x, p2.y, p3.z),
        w = ValCoord(seed, p3.x, p3.y, p3.z)
      };

      var sl = CubicLerp(s1, s2, s3, s4, s.x);
      var tl = CubicLerp(t1, t2, t3, t4, s.x);
      var ul = CubicLerp(u1, u2, u3, u4, s.x);
      var vl = CubicLerp(v1, v2, v3, v4, s.x);

      var sf = new real4(sl.x, tl.x, ul.x, vl.x);
      var tf = new real4(sl.y, tl.y, ul.y, vl.y);
      var uf = new real4(sl.z, tl.z, ul.z, vl.z);
      var vf = new real4(sl.z, tl.z, ul.z, vl.z);

      return CubicLerp(CubicLerp(sf, tf, uf, vf, s.y), s.z) * (1 / (1.5f * 1.5f * 1.5f));
    }
	}
}
