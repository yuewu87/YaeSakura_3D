using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace YaeSakura
{
    /// Communicates with the Python sakura_api_server.py via HTTP/SSE.
    public class PythonBridgeClient
    {
        private HttpClient _http;
        private string _baseUrl;

        public PythonBridgeClient(string baseUrl = "http://127.0.0.1:5800")
        {
            _baseUrl = baseUrl;
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(120);
        }

        public async Task<bool> HealthCheck()
        {
            try
            {
                var resp = await _http.GetStringAsync($"{_baseUrl}/health");
                return resp.Contains("ok");
            }
            catch { return false; }
        }

        /// Send message and receive structured bubbles + actions via SSE.
        public async Task SendChat(
            string message,
            Action<string> onChunk,       // raw text chunk (for stream display)
            Action<List<string>, List<string>> onComplete,  // (bubbles, actions)
            Action<string> onError,
            CancellationToken cancel = default)
        {
            var body = JsonConvert.SerializeObject(new { message, stream = true });
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat");
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, cancel);
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
                        var evt = JsonConvert.DeserializeObject<SSEEvent>(data);
                        if (evt == null) continue;

                        if (evt.type == "chunk" && evt.text != null)
                        {
                            fullText.Append(evt.text);
                            onChunk?.Invoke(evt.text);
                        }
                        else if (evt.type == "complete")
                        {
                            onComplete?.Invoke(
                                evt.bubbles ?? new List<string>(),
                                evt.actions ?? new List<string>());
                        }
                        else if (evt.type == "error")
                        {
                            onError?.Invoke(evt.text ?? "unknown error");
                        }
                    }
                    catch { /* skip */ }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { onError?.Invoke(ex.Message); }
        }

        public async Task ClearHistory()
        {
            try { await _http.PostAsync($"{_baseUrl}/clear", null); }
            catch { }
        }

        public void Dispose() => _http?.Dispose();

        [Serializable]
        private class SSEEvent
        {
            public string type;
            public string text;
            public List<string> bubbles;
            public List<string> actions;
        }
    }
}
