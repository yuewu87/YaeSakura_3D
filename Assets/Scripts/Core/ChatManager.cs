using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YaeSakura
{
    /// Orchestrates LLM streaming, sentence splitting, action extraction — following Python display.py logic.
    public class ChatManager : MonoBehaviour
    {
        public ChatPanel chatPanel;
        public AudioSource ttsAudioSource;

        private LLMClient _llm;
        private MemoryManager _memory;
        private TTSClient _tts;
        private AppSettings _settings;
        private CancellationTokenSource _currentCTS;
        private bool _isProcessing;

        // Match Python: sentence ends at 。！？\n
        private static readonly Regex SentenceEnd = new Regex(@"[。！？\n]");
        // Match Python: bracketed actions （...） or (...)
        private static readonly Regex BracketAction = new Regex(@"[（(]([^）)]*)[）)]");

        private void Start()
        {
            _settings = AppConfig.Load();
            _llm = new LLMClient(_settings.chatConfig);
            _memory = new MemoryManager();
            _tts = new TTSClient(_settings.ttsConfig);

            if (ttsAudioSource != null)
                _tts.SetAudioSource(ttsAudioSource);

            if (chatPanel != null)
                chatPanel.OnSendMessage += HandleUserMessage;
        }

        private async void HandleUserMessage(string text)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _currentCTS = new CancellationTokenSource();

            _tts?.StopPlayback();

            if (_tts != null && !_tts.IsConnected)
            {
                try { await _tts.Connect(); }
                catch { /* TTS unavailable */ }
            }

            var messages = _memory.BuildMessages(text);
            chatPanel?.BeginStream();

            var fullText = new StringBuilder();

            await _llm.SendStreaming(
                messages,
                onChunk: chunk =>
                {
                    fullText.Append(chunk);
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        // Show raw text during streaming (same as 2D's current_streaming_message)
                        // Brackets are shown in stream, will be cleaned on completion
                        chatPanel?.AppendStream(chunk.ToString());
                    });
                },
                onComplete: finalText =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        chatPanel?.FinalizeStream();
                        chatPanel?.RemoveLastBubble(); // remove raw stream bubble
                        DisplayResponse(finalText);
                        _memory.AddTurn(text, finalText);
                        _isProcessing = false;
                    });
                },
                onError: err =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        Debug.LogError($"LLM Error: {err}");
                        chatPanel?.FinalizeStream();
                        chatPanel?.AddBubble($"[错误] {err}", false);
                        _isProcessing = false;
                    });
                },
                _currentCTS.Token
            );
        }

        /// Split response into bubbles and actions in original order.
        private void DisplayResponse(string fullText)
        {
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < fullText.Length; i++)
            {
                char c = fullText[i];
                if (c == '（' || c == '(')
                {
                    // Flush current dialogue text before the bracket
                    var prefixes = current.ToString().Trim();
                    current.Clear();
                    // Split by sentence ends and display
                    ShowSentences(prefixes);

                    // Collect action until closing bracket
                    var action = new System.Text.StringBuilder();
                    i++; // skip opening bracket
                    while (i < fullText.Length && fullText[i] != '）' && fullText[i] != ')')
                    {
                        action.Append(fullText[i]);
                        i++;
                    }
                    // i now at closing bracket, loop will advance past it

                    var act = action.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(act))
                        chatPanel?.AddActionLine(act);
                }
                else
                {
                    current.Append(c);
                }
            }

            // Flush remaining text
            ShowSentences(current.ToString());
        }

        private void ShowSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var parts = SentenceEnd.Split(text);
            foreach (var p in parts)
            {
                var s = p.Trim();
                if (string.IsNullOrEmpty(s)) continue;
                chatPanel?.AddBubble(s, false);
                _tts?.EnqueueSynthesis(s);
            }
        }

        private void OnDestroy()
        {
            _currentCTS?.Cancel();
            _llm?.Dispose();
            _ = _tts?.Disconnect();
        }
    }
}
