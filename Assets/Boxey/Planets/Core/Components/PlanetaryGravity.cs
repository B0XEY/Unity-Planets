using Boxey.Attributes;
using Boxey.Planets.Core.Generation;
using UnityEngine;

namespace Boxey.Planets.Core.Components {
    [RequireComponent(typeof(Rigidbody)), AddComponentMenu("Boxey/Components/Gravity Object")]
    public class PlanetaryGravity : MonoBehaviour {
        private PlanetaryObject[] _allPlanets;
        private PlanetaryObject _currentClosest;

        private bool _useGravity = true;

        private Rigidbody _rb;
        
        [Header("Settings"), Line] 
        [SerializeField] private bool alignToPlanet = true;

        private void OnEnable() {
            TryGetComponent(out _rb);
            _rb.useGravity = false;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            GetAllPlanets();
            FindClosestPlanet();
        }

        private void FixedUpdate() {
            if (!_useGravity) {
                return;
            }
            var dirToCenter = GetDirectionToCenter();
            var g = GetGravityStrength();
            _rb.AddForce(dirToCenter * (g * _rb.mass));

            if (alignToPlanet) {
                var targetRotation = Quaternion.FromToRotation(transform.up, -dirToCenter);
                targetRotation *= transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1 * Time.deltaTime);
            }
        }

        private float GetGravityStrength() {
            if (!_currentClosest) {
                return 0f;
            }
            var currentActiveGravity = _currentClosest.GetPlanetGravity();
            var dstToCenter = Vector3.Distance(transform.position, _currentClosest.transform.position);
            var dstToSurface = dstToCenter - _currentClosest.GetPlanetRadius();
            if (dstToSurface > 0) {
                currentActiveGravity = _currentClosest.GetPlanetGravity() / (dstToSurface);
            }
            return Mathf.Abs(currentActiveGravity);
        }
        private void GetAllPlanets() {
            _allPlanets = FindObjectsOfType<PlanetaryObject>();
        }
        
        public void FindClosestPlanet() {
            if (_allPlanets.Length <= 0) {
                Debug.LogWarning("No planets created!");
                return;
            }
            _currentClosest = _allPlanets[0];
            var dst = float.MaxValue;
            foreach (var pObj in _allPlanets) {
                var dstFromPlanet = Vector3.Distance(pObj.transform.position, transform.position);
                if (dstFromPlanet < dst) {
                    _currentClosest = pObj; // closer planet
                    dst = dstFromPlanet;
                }
            }
        }
        public Vector3 GetDirectionToCenter() {
            var dirToCenter = _currentClosest.transform.position - transform.position;
            dirToCenter.Normalize();
            return dirToCenter;
        }
        public void SetGravityUse(bool value) => _useGravity = value;
    }
}