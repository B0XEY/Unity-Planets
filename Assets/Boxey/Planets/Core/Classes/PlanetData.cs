using System.Collections.Generic;
using Boxey.Attributes;
using UnityEngine;

namespace Boxey.Planets.Core.Classes {
    [CreateAssetMenu(fileName = "New Planet", menuName = "Boxey/Planet/New Planet", order = 0)]
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

        private void OnValidate() {
            if (noiseLayers.Count == 0) {
                noiseLayers.Add(new NoiseSettings());
            }
        }
    }
}