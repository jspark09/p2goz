using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles microphone capture and routes the conversation through OpenAI so the Guru can talk with players.
/// Attach this component to the Guru GameObject and wire the AudioSource in the inspector.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GuruVoiceAgent : MonoBehaviour
{
    [Header("OpenAI")]
    [Tooltip("Your OpenAI API key. Prefer loading from an environment variable in production.")]
    [SerializeField] private string openAIApiKey = "";
    [SerializeField] private string transcriptionModel = "gpt-4o-mini-transcribe";
    [SerializeField] private string chatModel = "gpt-4o-mini";
    [SerializeField] private string textToSpeechModel = "gpt-4o-mini-tts";
    [SerializeField] private string textToSpeechVoice = "alloy";

    [Header("Personality")]
    [TextArea(3, 6)]
    [SerializeField] private string guruPersona = "You are the wise Garden Guru guiding visitors through a peaceful zen garden. Speak calmly, offer reflective insights, and keep responses concise.";

    [Header("Recording")]
    [SerializeField] private bool usePushToTalk = true;
    [SerializeField] private KeyCode pushToTalkKey = KeyCode.Space;
    [SerializeField] private float maxRecordingSeconds = 15f;
    [SerializeField] private int recordingFrequency = 16000;

    [Header("Audio Output")]
    [SerializeField] private AudioSource guruAudioSource;
    [SerializeField] private float playbackVolume = 1f;

    [Header("Events")]
    public UnityEvent<string> OnUserTranscript;
    public UnityEvent<string> OnGuruResponse;

    private readonly List<OpenAIService.ChatMessage> conversation = new();
    private bool isRecording;
    private string microphoneDevice;
    private AudioClip recordingClip;

    private void Awake()
    {
        if (guruAudioSource == null)
        {
            guruAudioSource = GetComponent<AudioSource>();
        }

        guruAudioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(openAIApiKey))
        {
            string envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                openAIApiKey = envKey;
            }
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("GuruVoiceAgent: No microphone devices detected.");
            return;
        }

        microphoneDevice = Microphone.devices[0];
        Debug.Log($"GuruVoiceAgent: Using microphone '{microphoneDevice}'.");

        if (!string.IsNullOrWhiteSpace(guruPersona))
        {
            conversation.Add(new OpenAIService.ChatMessage("system", guruPersona));
        }
    }

    private void Update()
    {
        if (!usePushToTalk || string.IsNullOrEmpty(microphoneDevice))
        {
            return;
        }

        if (Input.GetKeyDown(pushToTalkKey))
        {
            StartRecording();
        }
        else if (Input.GetKeyUp(pushToTalkKey))
        {
            StopRecordingAndProcess();
        }
    }

    public void StartRecording()
    {
        if (isRecording || string.IsNullOrEmpty(microphoneDevice))
        {
            return;
        }

        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogError("GuruVoiceAgent: OpenAI API key is not configured.");
            return;
        }

        isRecording = true;
        recordingClip = Microphone.Start(microphoneDevice, false, Mathf.CeilToInt(maxRecordingSeconds), recordingFrequency);
        Debug.Log("GuruVoiceAgent: Listening...");
    }

    public void StopRecordingAndProcess()
    {
        if (!isRecording || string.IsNullOrEmpty(microphoneDevice))
        {
            return;
        }

        StartCoroutine(ProcessRecording());
    }

    private IEnumerator ProcessRecording()
    {
        if (recordingClip == null)
        {
            Debug.LogWarning("GuruVoiceAgent: Recording clip missing.");
            yield break;
        }

        int position = Microphone.GetPosition(microphoneDevice);

        // Give Unity a few frames to flush mic data if the button was released quickly.
        const int maxRetries = 5;
        int retries = 0;
        while (position <= 0 && retries < maxRetries)
        {
            yield return null;
            position = Microphone.GetPosition(microphoneDevice);
            retries++;
        }

        Microphone.End(microphoneDevice);
        isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("GuruVoiceAgent: No audio captured.");
            yield break;
        }

        position = Mathf.Min(position, recordingClip.samples);
        int channels = recordingClip.channels;
        float[] audioData = new float[position * channels];
        recordingClip.GetData(audioData, 0);

        AudioClip trimmedClip = AudioClip.Create("GuruUserInput", position, channels, recordingClip.frequency, false);
        trimmedClip.SetData(audioData, 0);

        byte[] wavData;
        try
        {
            wavData = WavUtility.FromAudioClip(trimmedClip);
        }
        catch (Exception ex)
        {
            Debug.LogError($"GuruVoiceAgent: Failed to convert audio to WAV. {ex.Message}");
            yield break;
        }

        yield return HandleConversation(wavData);
    }

    private IEnumerator HandleConversation(byte[] wavData)
    {
        string transcript = null;
        string error = null;

        yield return StartCoroutine(OpenAIService.Transcribe(
            wavData,
            openAIApiKey,
            transcriptionModel,
            text => transcript = text,
            err => error = err));

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError(error);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(transcript))
        {
            Debug.LogWarning("GuruVoiceAgent: Transcription returned empty text.");
            yield break;
        }

        OnUserTranscript?.Invoke(transcript);
        conversation.Add(new OpenAIService.ChatMessage("user", transcript));

        string guruReply = null;
        error = null;

        yield return StartCoroutine(OpenAIService.GetChatCompletion(
            conversation,
            openAIApiKey,
            chatModel,
            text => guruReply = text,
            err => error = err));

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError(error);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(guruReply))
        {
            Debug.LogWarning("GuruVoiceAgent: Chat completion returned empty text.");
            yield break;
        }

        OnGuruResponse?.Invoke(guruReply);
        conversation.Add(new OpenAIService.ChatMessage("assistant", guruReply));

        byte[] responseAudio = null;
        error = null;

        yield return StartCoroutine(OpenAIService.GenerateSpeech(
            guruReply,
            openAIApiKey,
            textToSpeechModel,
            textToSpeechVoice,
            data => responseAudio = data,
            err => error = err));

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError(error);
            yield break;
        }

        if (responseAudio == null || responseAudio.Length == 0)
        {
            Debug.LogWarning("GuruVoiceAgent: TTS returned empty audio data.");
            yield break;
        }

        AudioClip responseClip;
        try
        {
            responseClip = WavUtility.ToAudioClip(responseAudio, "GuruReply");
        }
        catch (Exception ex)
        {
            Debug.LogError($"GuruVoiceAgent: Failed to convert TTS audio. {ex.Message}\n" +
                           $"Content-Type: {OpenAIService.LastTtsContentType ?? "unknown"} | Bytes: {OpenAIService.LastTtsByteLength}\n" +
                           $"Preview (base64): {OpenAIService.LastTtsPreview}");
            yield break;
        }

        guruAudioSource.Stop();
        guruAudioSource.volume = playbackVolume;
        guruAudioSource.clip = responseClip;
        guruAudioSource.Play();
    }

    public void ResetConversation()
    {
        conversation.Clear();
    }
}
