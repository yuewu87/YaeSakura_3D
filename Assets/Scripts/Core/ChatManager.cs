using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YaeSakura
{
    /// Orchestrates LLM streaming, sentence splitting, TTS, and chat UI.
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

        private static readonly char[] SentenceDelims = { '。', '！', '？', '\n' };

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

            // Ensure TTS is connected (fire-and-forget, non-blocking)
            if (_tts != null && !_tts.IsConnected)
            {
                try { await _tts.Connect(); }
                catch { /* TTS unavailable, continue without voice */ }
            }

            var messages = _memory.BuildMessages(text);
            var streamBubble = chatPanel?.BeginStream();

            var currentSentence = new StringBuilder();
            var fullResponse = new StringBuilder();

            await _llm.SendStreaming(
                messages,
                onChunk: chunk =>
                {
                    fullResponse.Append(chunk);
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        ProcessChunk(chunk, currentSentence);
                    });
                },
                onComplete: finalText =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        if (currentSentence.Length > 0)
                        {
                            var final = currentSentence.ToString().Trim();
                            if (!string.IsNullOrEmpty(final))
                                _tts?.EnqueueSynthesis(final);
                        }
                        // Flush any pending bracket action
                        if (_inBracket && _currentAction.Length > 0)
                        {
                            chatPanel?.AddActionLine(_currentAction.ToString());
                            _currentAction.Clear();
                        }
                        _inBracket = false;
                        chatPanel?.FinalizeStream();
                        _memory.AddTurn(text, fullResponse.ToString());
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

        private bool _inBracket;
        private StringBuilder _currentAction = new StringBuilder();

        private void ProcessChunk(string chunk, StringBuilder sentence)
        {
            for (int i = 0; i < chunk.Length; i++)
            {
                char c = chunk[i];
                sentence.Append(c);

                if (c == '（' || c == '(')
                {
                    _inBracket = true;
                    _currentAction.Clear();
                }
                else if (c == '）' || c == ')')
                {
                    _inBracket = false;
                    if (_currentAction.Length > 0)
                    {
                        chatPanel?.AddActionLine(_currentAction.ToString());
                        _currentAction.Clear();
                    }
                }
                else if (_inBracket)
                {
                    _currentAction.Append(c); // collect action text, don't show in bubble
                }
                else
                {
                    chatPanel?.AppendStream(c.ToString()); // only show non-bracket text
                }

                if (IsSentenceEnd(c) && !_inBracket)
                {
                    var s = sentence.ToString().Trim();
                    sentence.Clear();
                    var actionFree = StripActionsForTTS(s);
                    if (!string.IsNullOrEmpty(actionFree))
                        _tts?.EnqueueSynthesis(actionFree);
                }
            }
        }

        private bool IsSentenceEnd(char c)
        {
            for (int i = 0; i < SentenceDelims.Length; i++)
                if (c == SentenceDelims[i]) return true;
            return false;
        }

        /// Strip bracketed content from text (for TTS). Does NOT call AddActionLine.
        private string StripActionsForTTS(string sentence)
        {
            var cleaned = new StringBuilder();
            int depth = 0;
            for (int i = 0; i < sentence.Length; i++)
            {
                char c = sentence[i];
                if (c == '（' || c == '(') depth++;
                else if (c == '）' || c == ')') depth--;
                else if (depth == 0) cleaned.Append(c);
            }
            if (depth > 0) { cleaned.Insert(0, "（"); }
            return cleaned.ToString().Trim();
        }

        private void OnDestroy()
        {
            _currentCTS?.Cancel();
            _llm?.Dispose();
            _ = _tts?.Disconnect();
        }
    }
}
