using System.Runtime.CompilerServices;
#if NOISE_DOUBLE_PRECISION
using real = System.Double;
using real3 = Unity.Mathematics.double3;
#else
using real = System.Single;
using real3 = Unity.Mathematics.float3;
#endif

namespace Unbegames.Noise { 
	public struct DomainWarpFractalProgressive<T, U> : INoise3D where T : struct, IDomainWarp3D where U : struct, INoise3D {
		public readonly T warp;
		public readonly U noise;
		public readonly int warpSeed;
		public readonly int octaves;
		public readonly float warpFrequency;
		public readonly float warpAmp;
		public readonly float fractalBounding;

		public DomainWarpFractalProgressive(int warpSeed, int octaves, float warpFrequency, float warpAmp) : this(new T(), new U(), warpSeed, octaves, warpFrequency, warpAmp) {
		}

		public DomainWarpFractalProgressive(U noise, int warpSeed, int octaves, float warpFrequency, float warpAmp) : this(new T(), noise, warpSeed, octaves, warpFrequency, warpAmp) {
		}

		public DomainWarpFractalProgressive(T warp, U noise, int warpSeed, int octaves, float warpFrequency, float warpAmp) {
			this.warp = warp;
			this.noise = noise;
			this.warpSeed = warpSeed;
			this.octaves = octaves;
			this.warpFrequency = warpFrequency;
			this.warpAmp = warpAmp;

			fractalBounding = Utils.CalculateFractalBounding(octaves, warpAmp);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public real GetValue(int mSeed, real3 point) {
			int seed = mSeed;
			float amp = fractalBounding;
			float freq = warpFrequency;

			for (int i = 0; i < octaves; i++) {
				real3 ps = point;

				warp.Warp(warpSeed, warpFrequency, warpAmp, ps, ref point);

				seed++;
				amp *= amp;
				freq *= warpFrequency;
			}

			return noise.GetValue(seed, point);
		}
	}
}
