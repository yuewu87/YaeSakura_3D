using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YaeSakura
{
    public class LLMClient
    {
        private HttpClient _http;
        private ChatConfig _config;

        public LLMClient(ChatConfig config)
        {
            _config = config;
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(60);
        }

        public void UpdateConfig(ChatConfig config)
        {
            _config = config;
        }

        public async Task SendStreaming(
            List<Message> messages,
            Action<string> onChunk,
            Action<string> onComplete,
            Action<string> onError,
            CancellationToken cancel = default)
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = _config.model,
                ["messages"] = messages,
                ["stream"] = true,
                ["temperature"] = (double)_config.temperature,
                ["max_tokens"] = _config.maxTokens
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            var request = new HttpRequestMessage(HttpMethod.Post, _config.apiUrl);
            request.Headers.Add("Authorization", $"Bearer {_config.apiKey}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancel);

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                var fullText = new StringBuilder();

                while (!reader.EndOfStream && !cancel.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;

                    var data = line.Substring(6);
                    if (data == "[DONE]") break;

                    try
                    {
                        var chunk = Newtonsoft.Json.JsonConvert.DeserializeObject<ChunkResponse>(data);
                        if (chunk?.choices != null && chunk.choices.Count > 0)
                        {
                            var delta = chunk.choices[0].delta;
                            if (delta?.content != null)
                            {
                                fullText.Append(delta.content);
                                onChunk?.Invoke(delta.content);
                            }
                        }
                    }
                    catch { /* skip malformed chunks */ }
                }

                onComplete?.Invoke(fullText.ToString());
            }
            catch (OperationCanceledException)
            {
                onComplete?.Invoke("");
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
            }
        }

        public async Task<string> SendSync(List<Message> messages, CancellationToken cancel = default)
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = _config.model,
                ["messages"] = messages,
                ["stream"] = false,
                ["temperature"] = 0.3,
                ["max_tokens"] = 512
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            var request = new HttpRequestMessage(HttpMethod.Post, _config.apiUrl);
            request.Headers.Add("Authorization", $"Bearer {_config.apiKey}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, cancel);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<SyncResponse>(text);
            return result?.choices?[0]?.message?.content ?? "";
        }

        public void Dispose() => _http?.Dispose();

        [Serializable]
        private class ChunkResponse { public List<ChunkChoice> choices; }

        [Serializable]
        private class ChunkChoice { public ChunkDelta delta; }

        [Serializable]
        private class ChunkDelta { public string content; }

        [Serializable]
        private class SyncResponse { public List<SyncChoice> choices; }

        [Serializable]
        private class SyncChoice { public SyncMessage message; }

        [Serializable]
        private class SyncMessage { public string content; }
    }
}
