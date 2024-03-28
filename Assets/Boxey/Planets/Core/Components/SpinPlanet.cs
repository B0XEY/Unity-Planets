using Boxey.Attributes;
using UnityEngine;

namespace Boxey.Planets.Core.Components {
    public class SpinPlanet : MonoBehaviour {
        [Header("Settings"), Line] [SerializeField]
        private Vector3 rotationValue;

        private void Update() {
            transform.Rotate(rotationValue * Time.deltaTime, Space.Self);
        }
    }
}
