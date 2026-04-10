using UnityEngine;

namespace PSXShadersPro.URP.Demo
{
    public class PlayerController : MonoBehaviour
    {
        public CharacterController controller;

        public float baseSpeed = 5.0f;
        public float gravity = -9.81f;
        public float jumpHeight = 3.0f;

        public Transform groundCheck;
        public float groundDistance = 0.4f;
        public LayerMask groundMask;

        private Vector3 velocity;

        private void Update()
        {
            bool isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2.0f;
            }

            float speed;

            if (Input.GetKey("left shift") && isGrounded)
            {
                speed = baseSpeed * 2.0f;
            }
            else
            {
                speed = baseSpeed;
            }

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;

            if(move.magnitude > 1.0f)
            {
                move.Normalize();
            }

            controller.Move(move * speed * Time.deltaTime);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;

            controller.Move(velocity * Time.deltaTime);
        }
    }
}
