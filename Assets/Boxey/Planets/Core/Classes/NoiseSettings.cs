using System;
using Boxey.Attributes;
using UnityEngine;

namespace Boxey.Planets.Core.Classes {
    [Serializable]
    public class NoiseSettings {
        [Header("Settings"), Line] 
        [SerializeField] private string layerName;
        [SerializeField,Range(0, 2)] private float layerPower = 1;
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
}