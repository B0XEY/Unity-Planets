#if NOISE_DOUBLE_PRECISION
using real3 = Unity.Mathematics.double3;
#else
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise {
	public interface IDomainWarp3D {
		void Warp(int seed, float frequency, float amp, real3 origPoint, ref real3 point);
	}
}