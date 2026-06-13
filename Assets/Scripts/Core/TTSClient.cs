using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YaeSakura
{
    public class TTSClient
    {
        private TTSConfig _config;
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private Queue<byte[]> _playQueue = new Queue<byte[]>();
        private bool _isPlaying;
        private AudioSource _audioSource;
        private float _originalVolume = 1f;

        public bool IsConnected => _ws?.State == WebSocketState.Open;

        public TTSClient(TTSConfig config) { _config = config; }

        public async Task Connect()
        {
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            try
            {
                await _ws.ConnectAsync(new Uri(_config.serverUrl), _cts.Token);
                Debug.Log("[TTS] Connected to GPT-SoVITS");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS] Connection failed: {ex.Message}");
            }
        }

        public async Task Disconnect()
        {
            _cts?.Cancel();
            if (_ws?.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch { }
            }
            _ws?.Dispose();
        }

        public void SetAudioSource(AudioSource source)
        {
            _audioSource = source;
            if (source != null) _originalVolume = source.volume;
        }

        public void EnqueueSynthesis(string text)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(text)) return;
            _ = SynthesizeAsync(text);
        }

        private async Task SynthesizeAsync(string text)
        {
            if (_ws == null) return;
            try
            {
                var request = $"{{\"text\":\"{text}\",\"character\":\"{_config.character}\"}}";
                var bytes = Encoding.UTF8.GetBytes(request);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);

                var buffer = new byte[8192];
                var audioData = new List<byte>();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Binary)
                        audioData.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                if (audioData.Count > 0)
                {
                    lock (_playQueue) { _playQueue.Enqueue(audioData.ToArray()); }
                    if (!_isPlaying) PlayNext();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TTS] Synthesis error: {ex.Message}");
            }
        }

        private async void PlayNext()
        {
            _isPlaying = true;
            while (true)
            {
                byte[] wavData;
                lock (_playQueue)
                {
                    if (_playQueue.Count == 0) { _isPlaying = false; return; }
                    wavData = _playQueue.Dequeue();
                }

                var clip = WavToAudioClip(wavData);
                if (clip != null && _audioSource != null)
                {
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    while (_audioSource.isPlaying)
                        await Task.Delay(100);
                }
            }
        }

        public void StopPlayback()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _ = FadeOutAsync();
            lock (_playQueue) { _playQueue.Clear(); }
        }

        private async Task FadeOutAsync()
        {
            if (_audioSource == null) return;
            for (int i = 0; i < 5; i++)
            {
                _audioSource.volume = _originalVolume * (1f - (i + 1) / 5f);
                await Task.Delay(150);
            }
            _audioSource.Stop();
            _audioSource.volume = _originalVolume;
        }

        private AudioClip WavToAudioClip(byte[] wavData)
        {
            if (wavData.Length < 44) return null;
            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int dataSize = BitConverter.ToInt32(wavData, 40);
            int sampleCount = dataSize / 2;

            var clip = AudioClip.Create("tts", sampleCount, channels, sampleRate, false);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                samples[i] = BitConverter.ToInt16(wavData, 44 + i * 2) / 32768f;

            clip.SetData(samples, 0);
            return clip;
        }
    }
}
