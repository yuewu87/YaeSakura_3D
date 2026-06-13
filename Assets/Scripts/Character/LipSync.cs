using UnityEngine;

namespace YaeSakura
{
    /// Drives character mouth BlendShape based on AudioSource output amplitude.
    public class LipSync : MonoBehaviour
    {
        public AudioSource audioSource;
        public SkinnedMeshRenderer skinnedMesh;
        public int mouthBlendShapeIndex = 0;
        public float sensitivity = 2f;
        public float smoothSpeed = 8f;
        public float minThreshold = 0.02f;

        private float _currentValue;
        private float[] _samples = new float[256];

        private void Update()
        {
            if (audioSource == null || skinnedMesh == null) return;

            float target = 0f;
            if (audioSource.isPlaying)
            {
                audioSource.GetOutputData(_samples, 0);
                float sum = 0f;
                for (int i = 0; i < _samples.Length; i++)
                    sum += Mathf.Abs(_samples[i]);
                float rms = sum / _samples.Length;
                target = Mathf.Clamp01(rms * sensitivity);
                if (target < minThreshold) target = 0f;
            }

            _currentValue = Mathf.Lerp(_currentValue, target, Time.deltaTime * smoothSpeed);
            skinnedMesh.SetBlendShapeWeight(mouthBlendShapeIndex, _currentValue * 100f);
        }
    }
}
