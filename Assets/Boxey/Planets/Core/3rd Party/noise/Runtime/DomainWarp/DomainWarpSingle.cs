using System.Runtime.CompilerServices;
#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise {
	public struct DomainWarpSingle<T, U> : INoise3D where T : struct, IDomainWarp3D where U : struct, INoise3D {
		public readonly T warp;
		public readonly U noise;
		public readonly int warpSeed;
		public readonly float warpFrequency;
		public readonly float warpAmp;

		public DomainWarpSingle(int warpSeed, float warpFrequency, float warpAmp) : this(new T(), new U(), warpSeed, warpFrequency, warpAmp) {
		}

		public DomainWarpSingle(U noise, int warpSeed, float warpFrequency, float warpAmp) : this(new T(), noise, warpSeed, warpFrequency, warpAmp) {
		}

		public DomainWarpSingle(T warp, U noise, int warpSeed, float warpFrequency, float warpAmp) {
			this.warp = warp;
			this.noise = noise;
			this.warpSeed = warpSeed;
			this.warpFrequency = warpFrequency;
			this.warpAmp = warpAmp;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public real GetValue(int seed, real3 point) {
			warp.Warp(warpSeed, warpFrequency, warpAmp, point, ref point);
			return noise.GetValue(seed, point);
		}
	}
}
