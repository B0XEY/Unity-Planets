using UnityEngine;

namespace Boxey.Planets.Core.Components {
    public class PlanetaryDecoration : MonoBehaviour {
        private Vector3 _currentWindForce;
        private PlanetaryWindController _windController;
        
        public void Initialize(PlanetaryWindController wc) {
            _windController = wc;
        }

        private void Update() {
            if (_windController) {
                var windForce = _windController.GetWindDirection(transform.position);
                _currentWindForce = Vector3.Lerp(_currentWindForce, windForce, 3 * Time.deltaTime);
            }else{
                Debug.LogWarning("No wind controller!");
            }
        }
    }
}