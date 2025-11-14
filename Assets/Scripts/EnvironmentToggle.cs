using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("UI Toggles")]
    public Toggle rainToggle; // Default = unchecked (clear)
    public Toggle nightToggle; // Default = unchecked (day)

    void Start()
    {
        // Default state: day + clear
        isDay = true;
        isRaining = false;

        if (rainToggle != null)
        {
            rainToggle.isOn = isRaining;
            rainToggle.onValueChanged.AddListener(RainToggled);
        }

        if (nightToggle != null)
        {
            nightToggle.isOn = !isDay;
            nightToggle.onValueChanged.AddListener(NightToggled);
        }

        UpdateEnvironment();
    }

    void Update()
    {
        // Toggle rain: M key
        if (Input.GetKeyDown(KeyCode.M))
        {
            isRaining = !isRaining;
            
            if (rainToggle != null)
            {
                rainToggle.isOn = isRaining;
            }

            UpdateEnvironment();
        }

        // Toggle day/night: N key
        if (Input.GetKeyDown(KeyCode.N))
        {
            isDay = !isDay;

            if (nightToggle != null)
            {
                nightToggle.isOn = !isDay;
            }

            UpdateEnvironment();
        }
    }

    void RainToggled(bool value)
    {
        // Change the toggle UI
        isRaining = value;
        UpdateEnvironment();
    }

    void NightToggled(bool value)
    {
        // Chnage the toggle UI
        isDay = !value;
        UpdateEnvironment();
    }

    void UpdateEnvironment()
    {
        // Particles 
        if (rainSystem != null)
        {
            if (isRaining) rainSystem.Play();
            else rainSystem.Stop();
        }

        // Skyboxes 
        UpdateSkybox();

        // Audio
        UpdateAudio();

        // Refresh lighting
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
