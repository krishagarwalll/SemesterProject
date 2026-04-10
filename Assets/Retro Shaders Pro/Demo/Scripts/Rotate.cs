using UnityEngine;

namespace PSXShadersPro.URP.Demo
{
    public class Rotate : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationAnglesPerSecond;

        private void Update()
        {
            transform.Rotate(rotationAnglesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}

