using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEditor;

namespace YaeSakura
{
    public static class PostProcessSetup
    {
        [MenuItem("YaeSakura/Setup Post Processing")]
        public static void Setup()
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("No Main Camera"); return; }

            // PostProcessLayer
            var layer = cam.GetComponent<PostProcessLayer>();
            if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();
            layer.volumeLayer = -1;
            layer.volumeTrigger = cam.transform;

            // Profile
            var path = "Assets/PostProcessProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            // Bloom
            var bloom = profile.GetSetting<Bloom>();
            if (bloom == null) { bloom = profile.AddSettings<Bloom>(); bloom.active = true; }
            bloom.enabled.value = true;
            bloom.intensity.value = 0.3f;
            bloom.threshold.value = 0.8f;

            // Color Grading (ACES = cinematic)
            var grading = profile.GetSetting<ColorGrading>();
            if (grading == null) { grading = profile.AddSettings<ColorGrading>(); grading.active = true; }
            grading.enabled.value = true;
            grading.tonemapper.value = Tonemapper.ACES;
            grading.postExposure.value = 0.3f;
            grading.saturation.value = 5f;

            // Vignette
            var vignette = profile.GetSetting<Vignette>();
            if (vignette == null) { vignette = profile.AddSettings<Vignette>(); vignette.active = true; }
            vignette.enabled.value = true;
            vignette.intensity.value = 0.2f;

            // Volume
            var volGO = GameObject.Find("PostProcessVolume");
            if (volGO == null) volGO = new GameObject("PostProcessVolume");
            var volume = volGO.GetComponent<PostProcessVolume>();
            if (volume == null) volume = volGO.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            AssetDatabase.SaveAssets();
            Debug.Log("Post Processing setup complete!");
        }
    }
}
