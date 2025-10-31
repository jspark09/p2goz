using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public static class OpenAIService
{
    private const string TranscriptionUrl = "https://api.openai.com/v1/audio/transcriptions";
    private const string ChatUrl = "https://api.openai.com/v1/chat/completions";
    private const string TextToSpeechUrl = "https://api.openai.com/v1/audio/speech";

    public static IEnumerator Transcribe(byte[] wavData, string apiKey, string model, Action<string> onSuccess, Action<string> onError)
    {
        if (wavData == null || wavData.Length == 0)
        {
            onError?.Invoke("No audio data supplied for transcription.");
            yield break;
        }

        var form = new WWWForm();
        form.AddBinaryData("file", wavData, "speech.wav", "audio/wav");
        form.AddField("model", model);

        using var request = UnityWebRequest.Post(TranscriptionUrl, form);
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"Transcription failed: {request.error}\n{request.downloadHandler.text}");
            yield break;
        }

        try
        {
            var response = JsonUtility.FromJson<TranscriptionResponse>(request.downloadHandler.text);
            onSuccess?.Invoke(response?.text?.Trim() ?? string.Empty);
        }
        catch (Exception ex)
        {
            onError?.Invoke($"Failed to parse transcription: {ex.Message}\n{request.downloadHandler.text}");
        }
    }

    public static IEnumerator GetChatCompletion(List<ChatMessage> messages, string apiKey, string model, Action<string> onSuccess, Action<string> onError, float temperature = 0.7f)
    {
        if (messages == null || messages.Count == 0)
        {
            onError?.Invoke("Conversation history is empty.");
            yield break;
        }

        var requestBody = new ChatCompletionRequest
        {
            model = model,
            temperature = temperature,
            messages = messages
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using var request = new UnityWebRequest(ChatUrl, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"Chat completion failed: {request.error}\n{request.downloadHandler.text}");
            yield break;
        }

        try
        {
            var response = JsonUtility.FromJson<ChatCompletionResponse>(request.downloadHandler.text);
            string content = response?.choices != null && response.choices.Length > 0
                ? response.choices[0].message?.content
                : string.Empty;

            onSuccess?.Invoke(content?.Trim() ?? string.Empty);
        }
        catch (Exception ex)
        {
            onError?.Invoke($"Failed to parse chat response: {ex.Message}\n{request.downloadHandler.text}");
        }
    }

    public static IEnumerator GenerateSpeech(string text, string apiKey, string model, string voice, Action<byte[]> onSuccess, Action<string> onError, string format = "wav", float speed = 1f)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            onError?.Invoke("TTS was asked to speak an empty string.");
            yield break;
        }

        var payload = new TextToSpeechRequest
        {
            model = model,
            voice = voice,
            input = text,
            format = format,
            response_format = format,
            speed = speed
        };

        string requestJson = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);

        using var request = new UnityWebRequest(TextToSpeechUrl, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "audio/wav");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"Text-to-speech failed: {request.error}\n{request.downloadHandler.text}");
            yield break;
        }

        byte[] rawData = request.downloadHandler.data;
        string contentType = request.GetResponseHeader("Content-Type");

        LastTtsContentType = contentType;
        LastTtsByteLength = rawData?.Length ?? 0;
        LastTtsPreview = rawData != null && rawData.Length > 0
            ? Convert.ToBase64String(rawData, 0, Math.Min(rawData.Length, 48))
            : string.Empty;

        bool looksJson = (!string.IsNullOrEmpty(contentType) && contentType.Contains("application/json")) ||
                         (rawData.Length > 0 && rawData[0] == '{');

        if (looksJson)
        {
            string responseJson = request.downloadHandler.text;
            if (!TryDecodeAudioFromJson(responseJson, out rawData, out string parseError))
            {
                onError?.Invoke(parseError);
                yield break;
            }

            LastTtsContentType = $"{contentType ?? "json"} (decoded)";
            LastTtsByteLength = rawData?.Length ?? 0;
            LastTtsPreview = rawData != null && rawData.Length > 0
                ? Convert.ToBase64String(rawData, 0, Math.Min(rawData.Length, 48))
                : string.Empty;
        }

        onSuccess?.Invoke(rawData);
    }

    public static string LastTtsContentType { get; private set; }
    public static int LastTtsByteLength { get; private set; }
    public static string LastTtsPreview { get; private set; }

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    private class ChatCompletionRequest
    {
        public string model;
        public List<ChatMessage> messages;
        public float temperature;
    }

    [Serializable]
    private class ChatCompletionResponse
    {
        public ChatChoice[] choices;
    }

    [Serializable]
    private class ChatChoice
    {
        public ChatMessage message;
    }

    [Serializable]
    private class TranscriptionResponse
    {
        public string text;
    }

    [Serializable]
    private class TextToSpeechRequest
    {
        public string model;
        public string voice;
        public string input;
        public string format;
        public string response_format;
        public float speed;
    }

    private static bool TryDecodeAudioFromJson(string json, out byte[] audioBytes, out string error)
    {
        audioBytes = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Text-to-speech returned an empty JSON payload.";
            return false;
        }

        List<string> base64Segments = ExtractBase64Segments(json);
        if (base64Segments.Count == 0)
        {
            error = $"Text-to-speech returned JSON without recognizable audio data: {json}";
            return false;
        }

        try
        {
            using var memoryStream = new MemoryStream();
            foreach (string segment in base64Segments)
            {
                string cleanSegment = segment.Replace("\\u003d", "=")
                                             .Replace("\\/", "/")
                                             .Replace("\\n", string.Empty)
                                             .Replace("\n", string.Empty)
                                             .Replace("\r", string.Empty);

                if (cleanSegment.Length < 32)
                {
                    continue;
                }

                byte[] chunk = Convert.FromBase64String(cleanSegment);
                memoryStream.Write(chunk, 0, chunk.Length);
            }

            if (memoryStream.Length == 0)
            {
                error = $"Text-to-speech returned base64 data, but it did not decode to audio bytes.";
                return false;
            }

            audioBytes = memoryStream.ToArray();
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to decode base64 audio: {ex.Message}\n{json}";
            return false;
        }
    }

    private static List<string> ExtractBase64Segments(string json)
    {
        var segments = new List<string>();
        if (string.IsNullOrEmpty(json))
        {
            return segments;
        }

        var regex = new Regex("\"(?:data|audio|b64_audio|b64_json|content|base64|buffer)\"\\s*:\\s*\"([^\"]+)\"",
                              RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (Match match in regex.Matches(json))
        {
            if (match.Groups.Count > 1)
            {
                segments.Add(match.Groups[1].Value);
            }
        }

        return segments;
    }
}
