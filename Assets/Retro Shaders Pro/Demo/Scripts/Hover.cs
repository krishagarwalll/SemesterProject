using UnityEngine;

namespace PSXShadersPro.URP.Demo
{
    public class Hover : MonoBehaviour
    {
        [SerializeField] private Vector3 offset;
        [SerializeField] private float animDuration;
        [SerializeField] private Vector3 rotationAngles;

        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            float t = Mathf.Sin(Time.time / animDuration) * 0.5f + 0.5f;
            transform.position = Vector3.Lerp(startPosition - offset, startPosition + offset, t);
            transform.Rotate(rotationAngles * Time.deltaTime);
        }
    }
}
