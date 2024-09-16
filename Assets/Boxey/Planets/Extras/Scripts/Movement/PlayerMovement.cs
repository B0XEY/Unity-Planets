using Boxey.Attributes;
using Boxey.Planets.Core.Components;
using Boxey.Planets.Core.Static;
using UnityEngine;

namespace Boxey.Planets.Extras.Scripts.Movement {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(PlanetaryGravity))]
    [AddComponentMenu("Boxey/Extra/Player Movement")]
    public class PlayerMovement : MonoBehaviour {
        private bool _grounded;
        private Rigidbody _rb;
        private PlanetaryGravity _gravity;
        private Vector2 _rotation;
        private Quaternion _cameraHeadRot;
        
        [Header("Movement Settings"), Line]
        [SerializeField] private float speed = 7;
        [SerializeField] private float runMultiplier = 1.5f;
        [SerializeField] private float jumpHeight = 2.0f;
        
        [Header("Look Settings"), Line]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float lookSpeed = 2.0f;
        [SerializeField] private float lookXLimit = 60.0f;
        [Space(5f)]
        [SerializeField] private CameraMovement freeCam;

        private void Awake() {
            TryGetComponent(out _gravity);
            TryGetComponent(out _rb);
            _rb.useGravity = false;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (!playerCamera) {
                playerCamera = Helpers.GetCamera;
            }
            playerCamera.transform.localPosition = new Vector3(0, 1.69f, 0);
            freeCam.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.P)) {
                if (freeCam.enabled) {
                    freeCam.enabled = false;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    playerCamera.transform.localPosition = new Vector3(0, 1.69f, 0);
                    playerCamera.transform.localRotation = _cameraHeadRot;
                    _gravity.SetGravityUse(true);
                }else {
                    _cameraHeadRot = playerCamera.transform.localRotation;
                    freeCam.enabled  = true;
                    _gravity.SetGravityUse(false);
                }
            }
            if (freeCam.enabled) {
                return;
            }
            _rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;
            _rotation.x = Mathf.Clamp(_rotation.x, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(_rotation.x, 0, 0);
            var localRotation = Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime, 0f);
            transform.rotation *= localRotation;
            //grounded Check
            _grounded = !Physics.SphereCast(transform.position, 1f, _gravity.GetDirectionToCenter(), out var hit);
            if (_grounded && Input.GetKeyDown(KeyCode.Space)) {
                _rb.AddForce(transform.up * jumpHeight, ForceMode.VelocityChange);
            }
        }
        private void FixedUpdate() {
            if (freeCam.enabled || !_grounded) {
                return;
            }
            // Calculate how fast we should be moving
            var forwardDir = Vector3.Cross(transform.up, -playerCamera.transform.right).normalized;
            var rightDir = Vector3.Cross(transform.up, playerCamera.transform.forward).normalized;
            float mult = 1;
            if (Input.GetKey(KeyCode.LeftShift)) {
                mult = runMultiplier;
            }
            var targetVelocity = (forwardDir * Input.GetAxis("Vertical") + rightDir * Input.GetAxis("Horizontal")) * (speed * mult);

            var velocity = transform.InverseTransformDirection(_rb.velocity);
            velocity.y = 0;
            velocity = transform.TransformDirection(velocity);
            var velocityChange = transform.InverseTransformDirection(targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -10f, 10f);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -10f, 10f);
            velocityChange.y = 0;
            velocityChange = transform.TransformDirection(velocityChange);

            _rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        public void OnTeleport() {
            _gravity.FindClosestPlanet();
            playerCamera.transform.localPosition = new Vector3(0, 1.69f, 0);
        }
    }
}