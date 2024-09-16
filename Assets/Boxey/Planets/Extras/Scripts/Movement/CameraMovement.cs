using Boxey.Attributes;
using UnityEngine;

namespace Boxey.Planets.Extras.Scripts.Movement {
    [AddComponentMenu("Boxey/Extra/Free Camera")]
    public class CameraMovement : MonoBehaviour {
        private float _rotationX;
        private float _rotationY;
        private float _runMultiplier;
        
        private GUIStyle _style;
        
        [Header("Movement"), Line]
        [SerializeField] private float movementSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;
        [Space]
        [SerializeField] private bool teleportPlayer;
        [SerializeField, ShowIf("teleportPlayer")] private Transform player;
        

        private void Awake() {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _style = new GUIStyle {
                fontSize = 25,
                normal = {
                    textColor = Color.white
                }
            };
        }
        private void Update() {
            var scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            switch (scroll) {
                case > 0:
                    _runMultiplier += scroll * 7.5f;
                    break;
                case < 0:
                    _runMultiplier += scroll * 17.5f;
                    break;
            }
            _runMultiplier = Mathf.Clamp(_runMultiplier, 1, 500);
            var translationX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime * _runMultiplier;
            var translationZ = Input.GetAxis("Vertical") * movementSpeed * Time.deltaTime * _runMultiplier;
            transform.Translate(new Vector3(translationX, 0, translationZ));
            
            _rotationX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            _rotationY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);
            transform.rotation = Quaternion.Euler(_rotationY, _rotationX, 0);
            
            //teleport Player
            if (Input.GetKeyDown(KeyCode.Space) && teleportPlayer) {
                var position = new Vector3(transform.position.x, transform.position.y - 1.69f, transform.position.z);
                player.transform.position = position;
                player.GetComponent<PlayerMovement>().OnTeleport();
            }
        }
        
        private void OnGUI() {
            //Updates Colors
            var displayText = $"Current Move Speed: {_runMultiplier}";
            GUI.Label(new Rect(500f, 10f, 300f, 100f), displayText, _style);
        }
    }
}
