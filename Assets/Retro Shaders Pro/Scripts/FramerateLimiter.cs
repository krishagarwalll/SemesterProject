using UnityEngine;

namespace RetroShadersPro.URP
{
    public class FramerateLimiter : MonoBehaviour
    {
        [SerializeField]
        private int targetFrameRate = 30;

        private void Start()
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
