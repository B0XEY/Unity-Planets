using Boxey.Attributes;
using UnityEngine;

namespace Boxey.Planets.Core.Components {
    [AddComponentMenu("Boxey/Components/Planet Rotator")]
    public class SpinPlanet : MonoBehaviour {
        [Header("Settings"), Line] [SerializeField]
        private Vector3 rotationValue = new (0,0.001f,0);

        private void Update() {
            transform.Rotate(rotationValue * Time.deltaTime, Space.Self);
        }
    }
}
