using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentToggle : MonoBehaviour
{
    [Header("Weather")]
    public ParticleSystem rainSystem;
    private bool isRaining = false; // Default = clear

    [Header("Skyboxes")]
    public Material dayClearSkybox;
    public Material nightClearSkybox;
    public Material dayRainSkybox;
    public Material nightRainSkybox;

    [Header("Audio")]
    public AudioSource dayClearAudio;
    public AudioSource nightClearAudio;
    public AudioSource dayRainAudio;
    public AudioSource nightRainAudio;

    private bool isDay = true; // Default = day

    void Start()
    {
        // Ensure default state: day + clear
        isDay = true;
        isRaining = false;

        UpdateEnvironment();
    }

    void Update()
    {
        // Toggle rain: M key
        if (Input.GetKeyDown(KeyCode.M))
        {
            isRaining = !isRaining;
            UpdateEnvironment();
        }

        // Toggle day/night: N key
        if (Input.GetKeyDown(KeyCode.N))
        {
            isDay = !isDay;
            UpdateEnvironment();
        }
    }

    void UpdateEnvironment()
    {
        // --- Particles ---
        if (rainSystem != null)
        {
            if (isRaining) rainSystem.Play();
            else rainSystem.Stop();
        }

        // --- Skyboxes ---
        UpdateSkybox();

        // --- Audio ---
        UpdateAudio();

        // --- Refresh lighting ---
        DynamicGI.UpdateEnvironment();
    }

    void UpdateSkybox()
    {
        SetSkybox(dayClearSkybox, isDay && !isRaining);
        SetSkybox(nightClearSkybox, !isDay && !isRaining);
        SetSkybox(dayRainSkybox, isDay && isRaining);
        SetSkybox(nightRainSkybox, !isDay && isRaining);
    }

    void SetSkybox(Material skybox, bool shouldSet)
    {
        if (skybox == null) return;
        if (shouldSet) RenderSettings.skybox = skybox;
    }

    void UpdateAudio()
    {
        PlayAudio(dayClearAudio, isDay && !isRaining);
        PlayAudio(nightClearAudio, !isDay && !isRaining);
        PlayAudio(dayRainAudio, isDay && isRaining);
        PlayAudio(nightRainAudio, !isDay && isRaining);
    }

    void PlayAudio(AudioSource source, bool shouldPlay)
    {
        if (source == null) return;

        if (shouldPlay)
        {
            if (!source.isPlaying) source.Play();
        }
        else
        {
            if (source.isPlaying) source.Stop();
        }
    }
}
