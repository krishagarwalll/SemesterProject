using UnityEngine;

namespace PSXShadersPro.URP.Demo
{
    public class GooseFlight : MonoBehaviour
    {
        [SerializeField] private Transform leftWing;
        [SerializeField] private Transform rightWing;
        [SerializeField] private float flapSpeed;
        [SerializeField] private float flapOffset;
        [SerializeField] private float flapAngle;
        [SerializeField] private float flySpeed;

        [SerializeField] private Vector2 xSpawnBounds;
        [SerializeField] private Vector2 ySpawnBounds;
        [SerializeField] private Vector2 zSpawnBounds;

        private void Update()
        {
            var position = transform.position;
            position.z += flySpeed * Time.deltaTime;
            transform.position = position;

            if(transform.position.z > zSpawnBounds.y)
            {
                Respawn();
            }

            float leftAngle = Mathf.Lerp(-flapAngle, flapAngle, (Mathf.Sin(Time.time * flapSpeed + flapOffset) + 1.0f) * 0.5f);
            float rightAngle = Mathf.Lerp(flapAngle, -flapAngle, (Mathf.Sin(Time.time * flapSpeed + flapOffset) + 1.0f) * 0.5f);
            leftWing.localRotation = Quaternion.Euler(0.0f, 0.0f, leftAngle);
            rightWing.localRotation = Quaternion.Euler(0.0f, 0.0f, rightAngle);
        }

        private void Respawn()
        {
            var position = transform.position;
            position.x = Random.Range(xSpawnBounds.x, xSpawnBounds.y);
            position.y = Random.Range(ySpawnBounds.x, ySpawnBounds.y);
            position.z = zSpawnBounds.x;
            transform.position = position;
        }
    }
}

