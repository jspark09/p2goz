using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple chat log display for GuruVoiceAgent conversations.
/// Attach this to a UI GameObject with a TextMeshProUGUI field for output.
/// </summary>
public class GuruChatUI : MonoBehaviour
{
    [SerializeField] private GuruVoiceAgent guruAgent;
    [SerializeField] private TMP_Text chatOutput;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private int maxMessages = 50;

    private readonly Queue<string> messageQueue = new();
    private readonly StringBuilder logBuilder = new();

    private void Awake()
    {
        if (chatOutput == null)
        {
            chatOutput = GetComponent<TMP_Text>();
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }

        if (guruAgent == null)
        {
            guruAgent = FindObjectOfType<GuruVoiceAgent>();
        }
    }

    private void OnEnable()
    {
        if (guruAgent != null)
        {
            guruAgent.OnUserTranscript.AddListener(HandleUserMessage);
            guruAgent.OnGuruResponse.AddListener(HandleGuruMessage);
        }
    }

    private void OnDisable()
    {
        if (guruAgent != null)
        {
            guruAgent.OnUserTranscript.RemoveListener(HandleUserMessage);
            guruAgent.OnGuruResponse.RemoveListener(HandleGuruMessage);
        }
    }

    private void HandleUserMessage(string message)
    {
        AppendLine("You", message);
    }

    private void HandleGuruMessage(string message)
    {
        AppendLine("Guru", message);
    }

    private void AppendLine(string speaker, string message)
    {
        if (chatOutput == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        messageQueue.Enqueue($"<b>{speaker}:</b> {message}");

        while (messageQueue.Count > maxMessages)
        {
            messageQueue.Dequeue();
        }

        RebuildLog();
        ScrollToBottom();
    }

    private void RebuildLog()
    {
        logBuilder.Clear();
        foreach (string entry in messageQueue)
        {
            logBuilder.AppendLine(entry);
        }

        chatOutput.text = logBuilder.ToString();
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
