using Boxey.Planets.Core.Static;
using UnityEngine;

namespace Boxey.Planets.Core.Components {
    [AddComponentMenu("Boxey/Components/Sun Rotator")]
    [RequireComponent(typeof(Light))]
    public class LightRotator : MonoBehaviour{
        private Transform _mainCameraTransform;
        private Light _lightComponent;

        private void Start(){
            _mainCameraTransform = Helpers.GetCamera.transform;
            _lightComponent = GetComponent<Light>();

            if (_mainCameraTransform != null && _lightComponent != null) return;
            Debug.LogError("Main camera or Light component not found!");
            enabled = false;
        }

        private void Update(){
            var directionToCamera = _mainCameraTransform.position - transform.position;
            var rotationToCamera = Quaternion.LookRotation(directionToCamera);
            transform.rotation = rotationToCamera;
        }
    }
}
