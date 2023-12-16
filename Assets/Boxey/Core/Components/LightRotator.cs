using Boxey.Core.Static;
using UnityEngine;

namespace Boxey.Core.Components {
    [RequireComponent(typeof(Light))]
    public class LightRotator : MonoBehaviour{
        private Transform m_mainCameraTransform;
        private Light m_lightComponent;

        private void Start(){
            m_mainCameraTransform = Helpers.GetCamera.transform;
            m_lightComponent = GetComponent<Light>();

            if (m_mainCameraTransform != null && m_lightComponent != null) return;
            Debug.LogError("Main camera or Light component not found!");
            enabled = false;
        }

        private void Update(){
            var directionToCamera = m_mainCameraTransform.position - transform.position;
            var rotationToCamera = Quaternion.LookRotation(directionToCamera);
            transform.rotation = rotationToCamera;
        }
    }
}
