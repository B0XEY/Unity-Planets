# Unbegames.Noise

This is a port of [FastNoise Lite library](https://github.com/Auburn/FastNoise) using Unity.Mathematics for Burst Compiler.

## Limitations

This library has several limitations right now, that can be addresed in the future:

* if you are not using Burst, it will be probably faster to use FastNoise Lite instead;
* only 3d noise was ported;
* point transformations not yet implemeted;
* Cellular3D only returns distance as a result;
* code optimisation is in progress;
* there are only fractal derivatives implemented right now.

## Usage

You can use this library in your Unity game by using the Package Manager and referencing this package. 

`Package Manager -> Add package from git URL... -> https://github.com/unbeGames/noise.git`

### Examples

Simple noise:

```C#
namespace MyNamespace {
    using Unbegames.Noise;
    ...
    int seed = 15;
    float3 point = new float3(3,3,3);
    var noise = new Perlin3D();
    float result = noise.GetValue(seed, point);
    ...
}
```

Using fractals:

```C#
int octaves = 5;
float lacunarity = 1.99f;
float gain = 0.5f;
var noise = new FractalBillow<Value3D>(octaves, lacunarity, gain);
```

Nested fractals:

```C#
var fractal = new FractalRiged<ValueCubic3D>(octaves, lacunarity, gain);
var noise = new FractalFBM<FractalRiged<ValueCubic3D>>(fractal, octaves, lacunarity, gain);
```

Using warp:
```C#
var noise = new DomainWarpSingle<BasicGridWarp, ValueCubic3D>(warpSeed, warpFrequency, warpAmp);

```

## Double precision

Add `NOISE_DOUBLE_PRECISION` into scripting define symbols.

## Licensing

MIT
