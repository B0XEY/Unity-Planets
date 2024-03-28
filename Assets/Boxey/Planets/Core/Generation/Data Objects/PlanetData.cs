using System;
using System.Collections.Generic;
using Boxey.Attributes;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Generation.Data_Objects {
    [Serializable]
    public class NoiseSettings {
        [Header("Settings"), Line]
        [SerializeField] private string layerName;
        [Range(0,2)] public float layerPower = 1;
        public bool remove;
        [Header("Values"), Line]
        public float scale = .35f;
        [Range(1, 5)] public int octaves = 3;
        [Range(0.01f, 2f)] public float persistence = .15f;
        [Range(0.01f, 5f)] public float lacunarity = 3.66f;
        public Vector3 offset;
        [Space(5f)]
        public AnimationCurve heightCurve;
    }
    [CreateAssetMenu(fileName = "New Planet", menuName = "Data/New Planet", order = 0)]
    public class PlanetData : ScriptableObject{
        [Header("Planet Settings"), Line]
        [Range(0.01f, 10f)] public float groundToughness = 1f;
        [Space] 
        public float2[] caveLayers;
        //Noise
        [Header("Noise Settings"), Line]
        public bool useNoise;
        [Space(10f)]
        public List<NoiseSettings> noiseLayers;


        private void OnValidate(){
            if (noiseLayers.Count == 0) noiseLayers.Add(new NoiseSettings());
        }
    }
}