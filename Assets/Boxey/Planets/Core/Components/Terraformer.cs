using Boxey.Attributes;
using Boxey.Planets.Core.Static;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Core.Components {
    [AddComponentMenu("Boxey/Components/Terraformer")]
    public class Terraformer : MonoBehaviour {
        [Header("Terraforming"), Line] 
        [SerializeField] private bool doTerraform = true;
        [ShowIf("doTerraform")] 
        [SerializeField, Range(.25f, 10)] private float brushRadius = 2.5f;
        [ShowIf("doTerraform")] 
        [SerializeField, Range(0.01f, 1f)] private float brushSpeed = .4f;
        [ShowIf("doTerraform")] 
        [SerializeField] private Vector2 terraformColor = new(0, .7f);

        private Camera _camera;
        private GameObject _point;
        private bool _terraforming;

        private void Awake() {
            _point = new GameObject("Terraform Point");
            _camera = Helpers.GetCamera;
        }
        private void Update() {
            if (!doTerraform) {
                return;
            }
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) {
                if (hit.transform.name == "Sun" || hit.transform.gameObject.layer != 3) {
                    return;
                }
                var target = hit.transform.parent.GetComponentInParent<PlanetaryObject>();
                if (target == null) {
                    _point.transform.SetParent(null);
                    return;
                }
                _point.transform.position = hit.point;
                _point.transform.SetParent(target.transform);
                float3 terraformPoint = _point.transform.position;
                //Call Function To Get all effected Nodes
                if (Input.GetKey(KeyCode.Mouse0)) {
                    _terraforming = true;
                    target.Terrafrom(terraformPoint, brushRadius, brushSpeed, true, terraformColor);
                }
                //Call Function To Get all effected Nodes
                if (Input.GetKey(KeyCode.Mouse1)) {
                    _terraforming = true;
                    target.Terrafrom(terraformPoint, brushRadius, brushSpeed, false, terraformColor);
                }
            }

            //Call Function to End terraforming
            if (Input.GetKeyUp(KeyCode.Mouse0) && _terraforming) {
                _terraforming = false;
            }
            if (Input.GetKeyUp(KeyCode.Mouse1) && _terraforming) {
                _terraforming = false;
            }
        }
        
        private void OnDrawGizmos() {
            if (!_point || !_point.transform.parent) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_point.transform.position, brushRadius);
        }
    }
}