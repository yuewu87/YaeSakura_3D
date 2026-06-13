using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YaeSakura
{
    /// Bridges Unity chat UI to the Python sakura_api_server.
    /// Python handles: LLM streaming, memory, sentence splitting, action extraction.
    /// Unity handles: 3D rendering, chat UI display, TTS playback.
    public class ChatManager : MonoBehaviour
    {
        public ChatPanel chatPanel;
        public AudioSource ttsAudioSource;

        private PythonBridgeClient _bridge;
        private TTSClient _tts;
        private AppSettings _settings;
        private CancellationTokenSource _currentCTS;
        private bool _isProcessing;

        private void Start()
        {
            _settings = AppConfig.Load();
            _bridge = new PythonBridgeClient("http://127.0.0.1:5800");
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

            chatPanel?.BeginStream();

            await _bridge.SendChat(
                text,
                onChunk: chunk =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        chatPanel?.AppendStream(chunk);
                    });
                },
                onComplete: (bubbles, actions) =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        chatPanel?.FinalizeStream();

                        // Display per-sentence bubbles from Python
                        foreach (var b in bubbles)
                        {
                            if (!string.IsNullOrWhiteSpace(b))
                            {
                                chatPanel?.AddBubble(b, false);
                                _tts?.EnqueueSynthesis(b);
                            }
                        }

                        // Display action lines
                        foreach (var a in actions)
                        {
                            if (!string.IsNullOrWhiteSpace(a))
                                chatPanel?.AddActionLine(a);
                        }

                        _isProcessing = false;
                    });
                },
                onError: err =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        Debug.LogError($"Chat error: {err}");
                        chatPanel?.FinalizeStream();
                        chatPanel?.AddBubble($"[错误] {err}", false);
                        _isProcessing = false;
                    });
                },
                _currentCTS.Token
            );
        }

        private void OnDestroy()
        {
            _currentCTS?.Cancel();
            _bridge?.Dispose();
            _ = _tts?.Disconnect();
        }
    }
}
