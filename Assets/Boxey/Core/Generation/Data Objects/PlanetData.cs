using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boxey.Core.Generation.Data_Objects {
    public enum NoiseType {
        Normal,
        Ridged,
        Billow
    }
    [Serializable]
    public class NoiseSettings {
        [Header("Settings")]
        public NoiseType type = NoiseType.Normal;
        // [Label("Remove"), Tooltip("Subtracts values, makes planets smaller")] public bool removeLayer;
        [Tooltip("Subtracts values, makes planets smaller")] public bool removeLayer;
        public float scale = .35f;
        [Header("Values")]
        [Range(0,2)] public float layerPower = 1;
        [Space(5f)]
        [Range(0.01f, 5f), Tooltip("Does NOT Apply to Normal Noise Types")] public float gain = .5f;
        [Range(0.01f, 2f), Tooltip("Does NOT Apply to Normal Noise Types")] public float strength = 1;
        [Space(5f)]
        [Range(1, 5)] public int octaves = 3;
        [Range(0.01f, 5f)] public float lacunarity = 3.66f;
        [Range(0.01f, 2f)] public float amplitude = 1f;
        [Range(0.01f, 2f)] public float frequency = .804f;
        [Range(0.01f, 2f)] public float persistence = .15f;
        [Space(5f)]
        public Vector3 offset;
    }
    [CreateAssetMenu(fileName = "New Planet", menuName = "Data/New Planet", order = 0)]
    public class PlanetData : ScriptableObject{
        [Header("Terraform Settings")] 
        [Range(0.01f, 10f)] public float groundToughness = 1f;
        //Noise
        [Header("Noise Settings")]
        public bool useNoise;
        [Space(10f)]
        //[ShowIf("useNoise")] public List<NoiseSettings> noiseLayers;
        public List<NoiseSettings> noiseLayers;


        private void OnValidate(){
            if (noiseLayers.Count == 0) noiseLayers.Add(new NoiseSettings());
        }
    }
}