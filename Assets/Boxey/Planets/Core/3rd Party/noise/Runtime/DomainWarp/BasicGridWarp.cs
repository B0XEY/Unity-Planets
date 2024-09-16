using static Unity.Mathematics.math;
using static Unbegames.Noise.Utils;

#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise
{
    public struct BasicGridWarp : IDomainWarp3D
    {
        public void Warp(int seed, float frequency, float amp, real3 ps, ref real3 p)
        {
            var pf = ps * frequency;
            var p0 = FastFloor(pf);

            var s = InterpHermite(pf - p0);

            p0 *= Prime;

            var p1 = p0 + Prime;

            var hash0 = Hash(seed, p0.x, p0.y, p0.z) & (255 << 2);
            var hash1 = Hash(seed, p1.x, p0.y, p0.z) & (255 << 2);

            var lx0x = lerp(RandVecs3D[hash0], RandVecs3D[hash1], s.x);
            var ly0x = lerp(RandVecs3D[hash0 | 1], RandVecs3D[hash1 | 1], s.x);
            var lz0x = lerp(RandVecs3D[hash0 | 2], RandVecs3D[hash1 | 2], s.x);

            hash0 = Hash(seed, p0.x, p1.y, p0.z) & (255 << 2);
            hash1 = Hash(seed, p1.x, p1.y, p0.z) & (255 << 2);

            var lx1x = lerp(RandVecs3D[hash0], RandVecs3D[hash1], s.x);
            var ly1x = lerp(RandVecs3D[hash0 | 1], RandVecs3D[hash1 | 1], s.x);
            var lz1x = lerp(RandVecs3D[hash0 | 2], RandVecs3D[hash1 | 2], s.x);

            var lx0y = lerp(lx0x, lx1x, s.y);
            var ly0y = lerp(ly0x, ly1x, s.y);
            var lz0y = lerp(lz0x, lz1x, s.y);

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