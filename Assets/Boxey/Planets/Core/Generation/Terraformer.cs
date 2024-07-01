using Boxey.Attributes;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Generation {
    [AddComponentMenu("Boxey/Components/Terraformer")]
    public class Terraformer : MonoBehaviour {
        private Camera _camera;
        private GameObject _point;
        private PlanetaryObject _target;
        private bool _terraforming;
        
        [Header("Terraforming"), Line]
        [SerializeField] private bool doTerraform = true;
        [ShowIf("doTerraform")]
        [SerializeField, Range(.25f, 10)] private float brushRadius = 2.5f;
        [ShowIf("doTerraform")]
        [SerializeField,Range(0.01f, 1f)] private float brushSpeed = .4f;
        
        private void Awake() {
            _point = new GameObject("Terraform Point");
            _camera = Helpers.GetCamera;
        }
        
        private void Update() {
            if (!doTerraform) return;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) {
                if (hit.transform.name == "Sun" || hit.transform.gameObject.layer != 3) return;
                _target = hit.transform.parent.GetComponentInParent<PlanetaryObject>();
                if (_target == null) return;
                _point.transform.position = hit.point;
                float3 terraformPoint = _point.transform.position;
                //Call Function To Get all effected Nodes
                if (Input.GetKey(KeyCode.Mouse0)) {
                    _terraforming = true;
                    _target.Terrafrom(terraformPoint, brushRadius, brushSpeed, true);
                }
                //Call Function To Get all effected Nodes
                if (Input.GetKey(KeyCode.Mouse1)) {
                    _terraforming = true;
                    _target.Terrafrom(terraformPoint, brushRadius, brushSpeed, false);
                }
                //Call Function to End terraforming
                if (Input.GetKeyUp(KeyCode.Mouse0) && _terraforming) {
                    _terraforming = false;
                    _target.FinishTerrafrom();
                }
                if (Input.GetKeyUp(KeyCode.Mouse1) && _terraforming) {
                    _terraforming = false;
                    _target.FinishTerrafrom();
                }
            }
        }

        private void OnDrawGizmos() {
            if (_point == null) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_point.transform.position, brushRadius);
        }
    }
}