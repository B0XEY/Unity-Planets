#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise {
	public interface INoise3D {
		real GetValue(int seed, real3 point);
	}

	public interface INoiseDeriv3D {
		real GetValue(int seed, real3 point, out real3 deriv);
	}
}
