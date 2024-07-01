using System;
using System.Collections.Generic;
using Boxey.Attributes;
using UnityEngine;

namespace Boxey.Planets.Core.Generation.Data_Objects {
    [Serializable]
    public class NoiseSettings {
        [Header("Settings"), Line]
        [SerializeField] private string layerName;
        [SerializeField, Range(0,2)] private float layerPower = 1;
        [SerializeField] private bool remove;
        [Header("Values"), Line]
        [SerializeField] private float scale = 1;
        [SerializeField, Range(1, 5)] private int octaves = 3;
        [SerializeField, Range(0.01f, 2f)] private float persistence = .15f;
        [SerializeField, Range(0.01f, 5f)] private float lacunarity = 3.66f;
        [SerializeField] private Vector3 offset;
        [Space(5f)]
        [SerializeField] private float heightMultiplier = 1;
        [SerializeField] private AnimationCurve heightCurve;

        public float LayerPower => layerPower;
        public bool Remove => remove;
        public float Scale => scale;
        public int Octaves => octaves;
        public float Persistence => persistence;
        public float Lacunarity => lacunarity;
        public Vector3 Offset => offset;
        public float HeightMultiplier => heightMultiplier;
        public AnimationCurve HeightCurve => heightCurve;
    }
    [CreateAssetMenu(fileName = "New Planet", menuName = "Data/New Planet", order = 0)]
    public class PlanetData : ScriptableObject {
        [Header("Planet Settings"), Line]
        [SerializeField] private float planetGravity = 9.81f;
        [SerializeField] private float atmosphereScale = 1.5f;
        [SerializeField] private AtmosphereProfile atmosphere;
        [SerializeField] private Material planetMaterial;
        [Space(5f)]
        [SerializeField, Range(0.01f, 10f)] private float groundToughness = 1f;
        //Noise
        [Header("Noise Settings"), Line]
        [SerializeField] private bool useNoise;
        [Space(10f)]
        [SerializeField] private List<NoiseSettings> noiseLayers;

        public float PlanetGravity => planetGravity;
        public float AtmosphereScale => atmosphereScale;
        public AtmosphereProfile Atmosphere => atmosphere;
        public Material PlanetMaterial => planetMaterial;
        public float GroundToughness => groundToughness;
        public bool UseNoise => useNoise;
        public List<NoiseSettings> NoiseLayers => noiseLayers;

        private void OnValidate(){
            if (noiseLayers.Count == 0) noiseLayers.Add(new NoiseSettings());
        }
    }
}