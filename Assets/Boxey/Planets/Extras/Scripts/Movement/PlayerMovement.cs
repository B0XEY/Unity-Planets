using System;
using Boxey.Attributes;
using Boxey.Planets.Core.Static;
using UnityEngine;

namespace Boxey.Planets.Extras.Scripts.Movement {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [AddComponentMenu("Boxey/Extra/Player Movement")]
    public class PlayerMovement : MonoBehaviour {
        private bool _grounded;
        private Rigidbody _rb;
        private Vector2 _rotation;
        private float _maxVelocityChange = 10.0f;
        private Vector3 _cameraHeadPos;
        private Quaternion _cameraHeadRot;
        
        [Header("Movement Settings"), Line]
        [SerializeField] private float speed = 5.0f;
        [SerializeField] private bool canJump = true;
        [SerializeField, ShowIf("canJump")] private float jumpHeight = 2.0f;
        
        [Header("Look Settings"), Line]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float lookSpeed = 2.0f;
        [SerializeField] private float lookXLimit = 60.0f;
        [Space(5f)]
        [SerializeField] private CameraMovement freeCam;

        private void Awake() {
            TryGetComponent(out _rb);
            _rb.useGravity = false;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (!playerCamera) playerCamera = Helpers.GetCamera;
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
                    //reset Camera
                    playerCamera.transform.localPosition = _cameraHeadPos;
                    playerCamera.transform.localRotation = _cameraHeadRot;
                }else {
                    //record
                    _cameraHeadPos = playerCamera.transform.localPosition;
                    _cameraHeadRot = playerCamera.transform.localRotation;
                    //stuff
                    freeCam.enabled  = true;
                    //Cursor.visible = true;
                    //Cursor.lockState = CursorLockMode.None;
                }
            }
            if (freeCam.enabled) return;
            _rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
            _rotation.x = Mathf.Clamp(_rotation.x, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(_rotation.x, 0, 0);
            var localRotation = Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed, 0f);
            transform.rotation *=localRotation;
        }

        private void FixedUpdate() {
            if (freeCam.enabled || !_grounded) return;
            // Calculate how fast we should be moving
            var forwardDir = Vector3.Cross(transform.up, -playerCamera.transform.right).normalized;
            var rightDir = Vector3.Cross(transform.up, playerCamera.transform.forward).normalized;
            var targetVelocity = (forwardDir * Input.GetAxis("Vertical") + rightDir * Input.GetAxis("Horizontal")) * speed;

            var velocity = transform.InverseTransformDirection(_rb.velocity);
            velocity.y = 0;
            velocity = transform.TransformDirection(velocity);
            var velocityChange = transform.InverseTransformDirection(targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -_maxVelocityChange, _maxVelocityChange);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -_maxVelocityChange, _maxVelocityChange);
            velocityChange.y = 0;
            velocityChange = transform.TransformDirection(velocityChange);

            _rb.AddForce(velocityChange, ForceMode.VelocityChange);

            if (Input.GetKeyDown(KeyCode.Space) && canJump && _grounded) {
                _rb.AddForce(transform.up * jumpHeight, ForceMode.VelocityChange);
                _grounded = false;
            }
        }

        private void OnCollisionEnter() {
            _grounded = true;
        }
    }
}