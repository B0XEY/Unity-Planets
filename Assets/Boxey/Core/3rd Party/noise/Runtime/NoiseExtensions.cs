#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise {
	public static class NoiseExtensions {
		public static real GetValue<T>(this T noise, int seed, real x, real y, real z) where T : struct, INoise3D {
			return noise.GetValue(seed, new real3(x, y, z));
		}

		public static real GetValue<T>(this T noise, int seed, float frequency, real3 point) where T : struct, INoise3D {
			point *= frequency;
			return noise.GetValue(seed, point);
		}

		public static real GetValue<T>(this T noise, int seed, float frequency, real x, real y, real z) where T : struct, INoise3D {
			return noise.GetValue(seed, frequency, new real3(x, y, z));
		}
	}
}
