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
	public struct FractalPingPong<T> : INoise3D where T : struct, INoise3D {
    public readonly T mNoise;
    public readonly int octaves;
    public readonly float lacunarity;
    public readonly float gain;
    public readonly float weightedStrength;
    public readonly float pingPongStength;
    public readonly float fractalBounding;
    public real3 offset;

    public FractalPingPong(int octaves, float lacunarity = 1.99f, float gain = 0.5f, float weightedStrength = 0, float pingPongStength = 2) : this(new T(), octaves, lacunarity, gain, weightedStrength, pingPongStength) {

    }

    public FractalPingPong(T noise, int octaves, float lacunarity = 1.99f, float gain = 0.5f, float weightedStrength = 0, float pingPongStength = 2) {
      mNoise = noise;
      this.octaves = octaves;
      this.lacunarity = lacunarity;
      this.gain = gain;
      this.pingPongStength = pingPongStength;
      this.weightedStrength = weightedStrength;
      offset = real3.zero;
      fractalBounding = CalculateFractalBounding(octaves, gain);
    }

    public real GetValue(int mSeed, real3 point) {      
      int seed = mSeed;
      real sum = 0;
      real amp = fractalBounding;
      real3 offset = this.offset;

      for (int i = 0; i < octaves; i++) {
        real noise = PingPong((mNoise.GetValue(seed++, point) + 1) * pingPongStength);
        sum += (noise - 0.5f) * 2 * amp;
        amp *= lerp(1.0f, noise, weightedStrength);

        point = point * lacunarity + offset;
        amp *= gain;
        offset *= lacunarity;
      }

      return sum;
    }
	}
}
