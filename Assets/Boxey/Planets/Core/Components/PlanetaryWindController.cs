using Boxey.Attributes;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Boxey.Planets.Core.Components {
    [RequireComponent(typeof(PlanetaryObject))]
    [AddComponentMenu("Boxey/Planetary Wind Controller", -10000)]
    public class PlanetaryWindController : MonoBehaviour {
        [Header("Settings"), Line] 
        [SerializeField] private int mapSize = 128;
        [SerializeField] private Vector2 mapUpdateTimeRange = new(25f, 75f);
        
        private PlanetaryObject _planet;
        private float _rootScale;
        private float _t;
        private float3[] _windMap;

        private void Start() {
            TryGetComponent(out _planet);
            _rootScale = _planet.RootNodeScale;
        }
        private void Update() {
            _t -= Time.deltaTime;
            if (_t <= 0) UpdateWindMap();
        }

        public Vector3 GetWindDirection(Vector3 position) {
            var samplePosition = WorldToMapPosition(position);
            return SampleWindMap(samplePosition);
        }
        private void UpdateWindMap() {
            _windMap = JobManager.GetPlanetWindMap(mapSize, transform.position, _rootScale, _planet.Seed);
            _t = Random.Range(mapUpdateTimeRange.x, mapUpdateTimeRange.y);
        }
        private Vector3 WorldToMapPosition(Vector3 position) {
            var halfScale = _rootScale / 2f;
            var normalizedPosition = (position + new Vector3(halfScale, halfScale, halfScale)) / _rootScale;
            var mapPosition = new Vector3(
                normalizedPosition.x * (mapSize - 1),
                normalizedPosition.y * (mapSize - 1),
                normalizedPosition.z * (mapSize - 1)
            );
            return mapPosition;
        }
        private Vector3 SampleWindMap(Vector3 position) {
            var x = Mathf.Clamp(Mathf.FloorToInt(position.x), 0, mapSize - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt(position.y), 0, mapSize - 1);
            var z = Mathf.Clamp(Mathf.FloorToInt(position.z), 0, mapSize - 1);
            var i = x + mapSize * (y + mapSize * z);
            return _windMap[i];
        }
    }
}