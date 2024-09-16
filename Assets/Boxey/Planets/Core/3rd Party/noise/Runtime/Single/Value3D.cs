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
    public struct Value3D : INoise3D
    {
        public real GetValue(int seed, real3 p)
        {
            var p0 = FastFloor(p);

            var s = InterpHermite(p - p0);

            p0 *= Prime;

            var p1 = p0 + Prime;

            var xf00 = lerp(ValCoord(seed, p0.x, p0.y, p0.z), ValCoord(seed, p1.x, p0.y, p0.z), s.x);
            var xf10 = lerp(ValCoord(seed, p0.x, p1.y, p0.z), ValCoord(seed, p1.x, p1.y, p0.z), s.x);
            var xf01 = lerp(ValCoord(seed, p0.x, p0.y, p1.z), ValCoord(seed, p1.x, p0.y, p1.z), s.x);
            var xf11 = lerp(ValCoord(seed, p0.x, p1.y, p1.z), ValCoord(seed, p1.x, p1.y, p1.z), s.x);

            var yf0 = lerp(xf00, xf10, s.y);
            var yf1 = lerp(xf01, xf11, s.y);

            return lerp(yf0, yf1, s.z);
        }
    }
}