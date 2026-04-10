using UnityEngine;

namespace PSXShadersPro.URP.Demo
{
    public class River : MonoBehaviour
    {
        [SerializeField] private float flowSpeed;
        [SerializeField] private new MeshRenderer renderer;
        private Material material;
        private Vector2 startOffset;

        private void Start()
        {
            material = renderer.material;
            startOffset = material.GetTextureOffset("_BaseMap");
        }

        private void Update()
        {
            var t = Time.time * flowSpeed;
            var offset = startOffset + new Vector2(0.0f, t);
            material.SetTextureOffset("_BaseMap", offset);
        }
    }
}
