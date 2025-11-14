using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioGroupController : MonoBehaviour
{
    [Header("Target GameObject with AudioSources")]
    public GameObject audioRoot; // drag your object here in Inspector

    [Header("UI")]
    public Slider volumeSlider;

    private List<AudioSource> audioSources = new List<AudioSource>();

    private void Start()
    {
        if (audioRoot != null)
        {
            // Collect all AudioSources under the dragged object
            audioSources.AddRange(audioRoot.GetComponentsInChildren<AudioSource>());
        }

        // Initialize slider
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = 0.5f; // default volume
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Apply initial volume
        ApplyVolume(volumeSlider != null ? volumeSlider.value : 0.5f);
    }

    public void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
    }

    private void ApplyVolume(float value)
    {
        foreach (var source in audioSources)
        {
            if (source != null)
            {
                source.volume = value;
            }
        }
    }
}

