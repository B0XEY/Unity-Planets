using UnityEngine;

namespace Boxey.Extras {
    public class CameraMovement : MonoBehaviour {
        private float m_rotationX;
        private float m_rotationY;
        private float m_roll;
        
        [Header("Movement")]
        [SerializeField] private float movementSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;
        [Space]
        [SerializeField] private float runMult = 10;
        

        private void Awake() {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.P)) {
                var scale = Time.timeScale == 0 ? 1 : 0;
                Time.timeScale = scale;
                if (scale == 1) {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }else
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            }

            if (Input.GetKey(KeyCode.E)) m_roll -= .1f;
            if (Input.GetKey(KeyCode.Q)) m_roll += .1f;

            float mult = 1;
            if (Input.GetKey(KeyCode.LeftShift)) mult = runMult;
            float translationX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime * mult;
            float translationZ = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime * mult;
            //float translationY = Input.GetAxis("UpDown") * movementSpeed * Time.deltaTime * mult;
            transform.Translate(new Vector3(translationX, 0, translationZ));
            
            m_rotationX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            m_rotationY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            m_rotationY = Mathf.Clamp(m_rotationY, -90f, 90f);
            transform.rotation = Quaternion.Euler(m_rotationY, m_rotationX, m_roll);
        }

        private void OnDrawGizmos(){
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 5);
        }
    }
}
