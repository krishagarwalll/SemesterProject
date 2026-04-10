using UnityEngine;

namespace PSXShadersPro.URP.Demo
{
    public class MouseLook : MonoBehaviour
    {
        public float mouseSensitivity = 100f;

        public Transform playerBody;

        private float xRotation = 0f;
        
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        private void Update()
        {
            if(Input.GetButtonDown("Fire1"))
            {
                Cursor.visible = false;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}
