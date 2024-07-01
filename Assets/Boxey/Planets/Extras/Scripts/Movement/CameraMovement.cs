using Boxey.Attributes;
using UnityEngine;

namespace Boxey.Planets.Extras.Scripts.Movement {
    [AddComponentMenu("Boxey/Extra/Free Camera")]
    public class CameraMovement : MonoBehaviour {
        private float _rotationX;
        private float _rotationY;
        private float _roll;
        
        [Header("Movement"), Line]
        [SerializeField] private float movementSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;
        [Space]
        [SerializeField] private float runMultiplier = 10;
        
        [Header("Debug"), Line]
        [SerializeField] private bool draw;
        

        private void Awake() {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update() {
            float mult = 1;
            if (Input.GetKey(KeyCode.LeftShift)) mult = runMultiplier;
            float translationX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime * mult;
            float translationZ = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime * mult;
            //float translationY = Input.GetAxis("UpDown") * movementSpeed * Time.deltaTime * mult;
            transform.Translate(new Vector3(translationX, 0, translationZ));
            
            _rotationX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            _rotationY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);
            transform.rotation = Quaternion.Euler(_rotationY, _rotationX, _roll);
        }

        private void OnDrawGizmos() {
            if (!draw) return;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 5);
        }
    }
}
