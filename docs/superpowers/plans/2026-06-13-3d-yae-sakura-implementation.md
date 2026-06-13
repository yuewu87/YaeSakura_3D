# 3D 八重樱角色扮演 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 Unity 中实现 3D 八重樱角色扮演应用，包含 LLM 流式对话 + GPT-SoVITS TTS 语音合成。

**Architecture:** Unity 单体架构，C# HttpClient 直连 LLM API (SSE 流式)，WebSocket 连接本地 GPT-SoVITS 服务。Uber-baked: all source under `Assets/Scripts/` with clear folder separation.

**Tech Stack:** Unity 2022.3.62f1c1, C# (.NET Standard 2.1), uGUI, UnityWebSocket or NativeWebSocket, HttpClient.

**Files to create:**
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── LLMClient.cs           # HTTP streaming SSE parser
│   │   ├── TTSClient.cs           # WebSocket TTS synthesis
│   │   ├── ChatManager.cs         # Message orchestration, sentence splitting
│   │   └── MemoryManager.cs       # JSON conversation history persistence
│   ├── Models/
│   │   └── ChatModels.cs          # Message, ChatConfig, APIProvider data types
│   ├── UI/
│   │   ├── ChatPanel.cs           # Chat scroll view + input field
│   │   └── SettingsPanel.cs       # Sidebar: API, TTS, auto-reply config
│   ├── Character/
│   │   └── LipSync.cs             # Audio amplitude → BlendShape mouth open
│   └── Config/
│       └── AppConfig.cs           # API keys, model selection via PlayerPrefs
├── Resources/
│   └── sakura_prompt.txt          # Character system prompt (from 2D project)
└── Scenes/
    └── SampleScene.unity          # Modified: chat canvas + character placeholder
```

**No external Unity packages needed beyond built-in modules. NUnit for future tests.**

---

### Task 1: Project scaffolding + data models

**Files:**
- Create: `Assets/Scripts/Models/ChatModels.cs`
- Create: `Assets/Scripts/Config/AppConfig.cs`
- Create: `Assets/Resources/sakura_prompt.txt`

- [ ] **Step 1: Create directory structure**

Run in Unity project root:
```bash
mkdir -p Assets/Scripts/Core Assets/Scripts/Models Assets/Scripts/UI Assets/Scripts/Character Assets/Scripts/Config Assets/Resources
```

- [ ] **Step 2: Create ChatModels.cs**

```csharp
// Assets/Scripts/Models/ChatModels.cs
using System;
using System.Collections.Generic;

namespace YaeSakura
{
    [Serializable]
    public class Message
    {
        public string role;    // "system", "user", "assistant"
        public string content;
    }

    [Serializable]
    public class ChatConfig
    {
        public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
        public string apiKey = "";
        public string model = "deepseek-chat";
        public string provider = "deepseek"; // "deepseek" or "qwen"
        public bool enableThinking = false;
        public float temperature = 0.7f;
        public int maxTokens = 1024;
    }

    [Serializable]
    public class TTSConfig
    {
        public string serverUrl = "ws://localhost:8770";
        public string character = "sakura";
    }

    [Serializable]
    public class AppSettings
    {
        public ChatConfig chatConfig = new ChatConfig();
        public TTSConfig ttsConfig = new TTSConfig();
        public int autoReplyMinutes = 5;
        public bool autoReplyEnabled = false;
    }

    public enum APIProvider
    {
        DeepSeek,
        Qwen
    }
}
```

- [ ] **Step 3: Request recompile**

Call `request_recompile` to verify compilation succeeds.

- [ ] **Step 4: Create AppConfig.cs**

```csharp
// Assets/Scripts/Config/AppConfig.cs
using UnityEngine;

namespace YaeSakura
{
    public static class AppConfig
    {
        private const string KEY_API_PROVIDER = "ys_api_provider";
        private const string KEY_API_URL = "ys_api_url";
        private const string KEY_API_KEY = "ys_api_key";
        private const string KEY_MODEL = "ys_model";
        private const string KEY_TTS_URL = "ys_tts_url";
        private const string KEY_AUTO_REPLY_MINUTES = "ys_auto_reply";
        private const string KEY_AUTO_REPLY_ENABLED = "ys_auto_reply_enabled";

        public static AppSettings Load()
        {
            var s = new AppSettings();
            s.chatConfig.provider = PlayerPrefs.GetString(KEY_API_PROVIDER, "deepseek");
            s.chatConfig.apiUrl = PlayerPrefs.GetString(KEY_API_URL,
                s.chatConfig.provider == "qwen"
                    ? "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions"
                    : "https://api.deepseek.com/v1/chat/completions");
            s.chatConfig.apiKey = PlayerPrefs.GetString(KEY_API_KEY, "");
            s.chatConfig.model = PlayerPrefs.GetString(KEY_MODEL,
                s.chatConfig.provider == "qwen" ? "qwen-turbo" : "deepseek-chat");
            s.ttsConfig.serverUrl = PlayerPrefs.GetString(KEY_TTS_URL, "ws://localhost:8770");
            s.autoReplyMinutes = PlayerPrefs.GetInt(KEY_AUTO_REPLY_MINUTES, 5);
            s.autoReplyEnabled = PlayerPrefs.GetInt(KEY_AUTO_REPLY_ENABLED, 0) == 1;
            return s;
        }

        public static void Save(AppSettings s)
        {
            PlayerPrefs.SetString(KEY_API_PROVIDER, s.chatConfig.provider);
            PlayerPrefs.SetString(KEY_API_URL, s.chatConfig.apiUrl);
            PlayerPrefs.SetString(KEY_API_KEY, s.chatConfig.apiKey);
            PlayerPrefs.SetString(KEY_MODEL, s.chatConfig.model);
            PlayerPrefs.SetString(KEY_TTS_URL, s.ttsConfig.serverUrl);
            PlayerPrefs.SetInt(KEY_AUTO_REPLY_MINUTES, s.autoReplyMinutes);
            PlayerPrefs.SetInt(KEY_AUTO_REPLY_ENABLED, s.autoReplyEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static string GetDefaultAPIUrl(string provider)
        {
            return provider == "qwen"
                ? "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions"
                : "https://api.deepseek.com/v1/chat/completions";
        }
    }
}
```

- [ ] **Step 5: Copy character prompt from 2D project**

Run:
```bash
cp "E:/Study_Projects/yuewu_bachong/need/assets/sakura_prompt.txt" "E:/Study_Projects/Yae_sakura_3D/YaeSakura_3D/Assets/Resources/sakura_prompt.txt" 2>/dev/null || echo "Prompt file not found, will create placeholder"
```

If the file doesn't exist, create a placeholder: `Assets/Resources/sakura_prompt.txt` with the character setting text from the 2D project's `need/assets/` directory.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/ Assets/Resources/
git commit -m "feat: add project scaffolding, data models, and config system"
```

---

### Task 2: LLM streaming client

**Files:**
- Create: `Assets/Scripts/Core/LLMClient.cs`

- [ ] **Step 1: Create LLMClient.cs**

```csharp
// Assets/Scripts/Core/LLMClient.cs
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

        /// Send streaming chat request. Calls onChunk for each token, onComplete when done.
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
                ["temperature"] = _config.temperature,
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
                onComplete?.Invoke(fullText.ToString());
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
            }
        }

        /// Non-streaming request for memory extraction etc.
        public async Task<string> SendSync(List<Message> messages, CancellationToken cancel = default)
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = _config.model,
                ["messages"] = messages,
                ["stream"] = false,
                ["temperature"] = 0.3f,
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

        // SSE deserialization types
        [Serializable]
        private class ChunkResponse
        {
            public List<ChunkChoice> choices;
        }

        [Serializable]
        private class ChunkChoice
        {
            public ChunkDelta delta;
        }

        [Serializable]
        private class ChunkDelta
        {
            public string content;
        }

        [Serializable]
        private class SyncResponse
        {
            public List<SyncChoice> choices;
        }

        [Serializable]
        private class SyncChoice
        {
            public SyncMessage message;
        }

        [Serializable]
        private class SyncMessage
        {
            public string content;
        }
    }
}
```

- [ ] **Step 2: Request recompile + fix errors**

Call `request_recompile`, then `get_compilation_errors`. Fix Newtonsoft.Json dependency — it's available via the `com.unity.nuget.newtonsoft-json` package already in the manifest.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/LLMClient.cs
git commit -m "feat: add LLM streaming client with DeepSeek/Qwen support"
```

---

### Task 3: Memory manager (JSON conversation history)

**Files:**
- Create: `Assets/Scripts/Core/MemoryManager.cs`

- [ ] **Step 1: Create MemoryManager.cs**

```csharp
// Assets/Scripts/Core/MemoryManager.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YaeSakura
{
    public class MemoryManager
    {
        private const int MAX_HISTORY_ROUNDS = 20; // keep last 20 rounds (40 messages)
        private const string MEMORY_FILE = "chat_history.json";

        private List<Message> _history = new List<Message>();
        private string _systemPrompt;

        public IReadOnlyList<Message> History => _history;

        public MemoryManager()
        {
            LoadSystemPrompt();
            LoadHistory();
        }

        private void LoadSystemPrompt()
        {
            var asset = Resources.Load<TextAsset>("sakura_prompt");
            _systemPrompt = asset != null ? asset.text : "你是一个AI角色扮演助手。";
        }

        public List<Message> BuildMessages(string userMessage)
        {
            var messages = new List<Message>
            {
                new Message { role = "system", content = _systemPrompt }
            };
            messages.AddRange(_history);
            messages.Add(new Message { role = "user", content = userMessage });
            return messages;
        }

        public void AddTurn(string userMessage, string assistantMessage)
        {
            _history.Add(new Message { role = "user", content = userMessage });
            _history.Add(new Message { role = "assistant", content = assistantMessage });

            // Trim to MAX_HISTORY_ROUNDS
            while (_history.Count > MAX_HISTORY_ROUNDS * 2)
            {
                _history.RemoveAt(0);
                if (_history.Count > 0 && _history[0].role == "assistant")
                    _history.RemoveAt(0);
            }

            SaveHistory();
        }

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        private void SaveHistory()
        {
            var path = Path.Combine(Application.persistentDataPath, MEMORY_FILE);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_history);
            File.WriteAllText(path, json);
        }

        private void LoadHistory()
        {
            var path = Path.Combine(Application.persistentDataPath, MEMORY_FILE);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _history = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Message>>(json) ?? new List<Message>();
            }
        }
    }
}
```

- [ ] **Step 2: Recompile**

Call `request_recompile` and verify no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/MemoryManager.cs
git commit -m "feat: add JSON-based conversation memory manager"
```

---

### Task 4: Chat UI (ScrollView bubbles + input)

**Files:**
- Create: `Assets/Scripts/UI/ChatPanel.cs`

This task creates the chat panel MonoBehaviour that manages the ScrollView-based chat bubbles and input field. The UI itself is built programmatically so it works without prefabs.

- [ ] **Step 1: Create ChatPanel.cs**

```csharp
// Assets/Scripts/UI/ChatPanel.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YaeSakura
{
    public class ChatPanel : MonoBehaviour
    {
        [Header("References (optional, auto-created if null)")]
        public ScrollRect scrollRect;
        public RectTransform contentRoot;
        public InputField inputField;
        public Button sendButton;

        [Header("Prefabs")]
        public GameObject userBubblePrefab;
        public GameObject characterBubblePrefab;
        public GameObject actionLinePrefab;

        private List<GameObject> _bubbles = new List<GameObject>();
        private Text _currentStreamBubble;
        private bool _isStreaming;

        public bool IsStreaming => _isStreaming;

        // Callbacks
        public System.Action<string> OnSendMessage;

        // Colors
        private Color userBubbleColor = new Color(0.16f, 0.23f, 0.35f);
        private Color characterBubbleColor = new Color(0.23f, 0.1f, 0.23f);
        private Color actionLineColor = new Color(0.6f, 0.6f, 0.65f);
        private Color userTextColor = new Color(0.85f, 0.85f, 0.9f);
        private Color charTextColor = new Color(0.93f, 0.82f, 0.88f);

        private void Start()
        {
            CreateUI();
            sendButton.onClick.AddListener(SendMessage);
            inputField.onSubmit.AddListener(_ => SendMessage());
        }

        private void CreateUI()
        {
            if (scrollRect == null)
            {
                var scrollGO = new GameObject("ScrollView", typeof(ScrollRect), typeof(Image));
                scrollGO.transform.SetParent(transform, false);
                var scrollRT = scrollGO.GetComponent<RectTransform>();
                scrollRT.anchorMin = new Vector2(0, 0.1f);
                scrollRT.anchorMax = new Vector2(1, 0.92f);
                scrollRT.offsetMin = Vector2.zero;
                scrollRT.offsetMax = Vector2.zero;
                scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);

                var viewport = new GameObject("Viewport", typeof(Image), typeof(Mask));
                viewport.transform.SetParent(scrollGO.transform, false);
                var vpRT = viewport.GetComponent<RectTransform>();
                vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
                vpRT.sizeDelta = Vector2.zero;
                viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);

                var content = new GameObject("Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                content.transform.SetParent(viewport.transform, false);
                contentRoot = content.GetComponent<RectTransform>();
                contentRoot.anchorMin = new Vector2(0, 1); contentRoot.anchorMax = new Vector2(1, 1);
                contentRoot.pivot = new Vector2(0.5f, 1);
                contentRoot.sizeDelta = new Vector2(0, 0);
                var vlg = content.GetComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.spacing = 6;
                vlg.padding = new RectOffset(8, 8, 8, 8);
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect = scrollGO.GetComponent<ScrollRect>();
                scrollRect.viewport = vpRT;
                scrollRect.content = contentRoot;
                scrollRect.horizontal = false;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            if (inputField == null)
            {
                var inputGO = new GameObject("InputField", typeof(InputField), typeof(Image));
                inputGO.transform.SetParent(transform, false);
                var inputRT = inputGO.GetComponent<RectTransform>();
                inputRT.anchorMin = new Vector2(0, 0); inputRT.anchorMax = new Vector2(0.78f, 0.08f);
                inputRT.offsetMin = new Vector2(8, 4); inputRT.offsetMax = new Vector2(-4, -4);
                inputGO.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.18f, 0.9f);

                var textGO = new GameObject("Text", typeof(Text));
                textGO.transform.SetParent(inputGO.transform, false);
                var textRT = textGO.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(8, 4); textRT.offsetMax = new Vector2(-8, -4);
                var text = textGO.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 16;
                text.color = new Color(0.85f, 0.85f, 0.9f);
                text.alignment = TextAnchor.MiddleLeft;
                text.supportRichText = false;

                inputField = inputGO.GetComponent<InputField>();
                inputField.textComponent = text;
                inputField.lineType = InputField.LineType.MultiLineNewline;
                inputField.placeholder = null;
                // Set placeholder text
                var phGO = new GameObject("Placeholder", typeof(Text));
                phGO.transform.SetParent(inputGO.transform, false);
                var phRT = phGO.GetComponent<RectTransform>();
                phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
                phRT.offsetMin = new Vector2(8, 4); phRT.offsetMax = new Vector2(-8, -4);
                var ph = phGO.GetComponent<Text>();
                ph.text = "输入消息...";
                ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ph.fontSize = 16;
                ph.fontStyle = FontStyle.Italic;
                ph.color = new Color(0.5f, 0.5f, 0.55f);
                ph.alignment = TextAnchor.MiddleLeft;
                inputField.placeholder = ph;
            }

            if (sendButton == null)
            {
                var btnGO = new GameObject("SendButton", typeof(Button), typeof(Image));
                btnGO.transform.SetParent(transform, false);
                var btnRT = btnGO.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.8f, 0); btnRT.anchorMax = new Vector2(1, 0.08f);
                btnRT.offsetMin = new Vector2(2, 4); btnRT.offsetMax = new Vector2(-8, -4);
                btnGO.GetComponent<Image>().color = new Color(0.35f, 0.2f, 0.45f);

                var btnTextGO = new GameObject("Text", typeof(Text));
                btnTextGO.transform.SetParent(btnGO.transform, false);
                var btnTextRT = btnTextGO.GetComponent<RectTransform>();
                btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
                btnTextRT.sizeDelta = Vector2.zero;
                var btnText = btnTextGO.GetComponent<Text>();
                btnText.text = "发送";
                btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                btnText.fontSize = 16;
                btnText.color = Color.white;
                btnText.alignment = TextAnchor.MiddleCenter;

                sendButton = btnGO.GetComponent<Button>();
            }
        }

        public void SendMessage()
        {
            var text = inputField.text.Trim();
            if (string.IsNullOrEmpty(text) || _isStreaming) return;
            inputField.text = "";
            AddBubble(text, true);
            OnSendMessage?.Invoke(text);
        }

        public void AddBubble(string text, bool isUser)
        {
            var go = new GameObject("Bubble", typeof(Text), typeof(ContentSizeFitter), typeof(LayoutElement));
            go.transform.SetParent(contentRoot, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 16;
            t.color = isUser ? userTextColor : charTextColor;
            t.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 200;
            layout.preferredWidth = contentRoot.rect.width * 0.7f;

            // Background
            var bg = go.AddComponent<Image>();
            bg.color = isUser ? userBubbleColor : characterBubbleColor;

            _bubbles.Add(go);
            ScrollToBottom();
        }

        public void AddActionLine(string text)
        {
            var go = new GameObject("ActionLine", typeof(Text), typeof(ContentSizeFitter), typeof(LayoutElement));
            go.transform.SetParent(contentRoot, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 14;
            t.fontStyle = FontStyle.Italic;
            t.color = actionLineColor;
            t.alignment = TextAnchor.MiddleCenter;
            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 200;
            _bubbles.Add(go);
            ScrollToBottom();
        }

        /// Start or continue streaming text in the current bubble.
        public Text BeginStream()
        {
            _isStreaming = true;
            var go = new GameObject("StreamBubble", typeof(Text), typeof(ContentSizeFitter), typeof(LayoutElement), typeof(Image));
            go.transform.SetParent(contentRoot, false);
            _currentStreamBubble = go.GetComponent<Text>();
            _currentStreamBubble.text = "";
            _currentStreamBubble.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _currentStreamBubble.fontSize = 16;
            _currentStreamBubble.color = charTextColor;
            _currentStreamBubble.alignment = TextAnchor.MiddleLeft;
            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 200;
            layout.preferredWidth = contentRoot.rect.width * 0.7f;
            go.GetComponent<Image>().color = characterBubbleColor;
            _bubbles.Add(go);
            return _currentStreamBubble;
        }

        public void AppendStream(string chunk)
        {
            if (_currentStreamBubble != null)
                _currentStreamBubble.text += chunk;
            ScrollToBottom();
        }

        public void FinalizeStream()
        {
            _isStreaming = false;
            _currentStreamBubble = null;
        }

        public void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            contentRoot.anchoredPosition = new Vector2(0, 0);
        }

        public void ClearAll()
        {
            foreach (var b in _bubbles)
                Destroy(b);
            _bubbles.Clear();
            _currentStreamBubble = null;
        }

        private void Update()
        {
            // Enter to send, Shift+Enter for newline
            if (inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    SendMessage();
                }
            }
        }
    }
}
```

- [ ] **Step 2: Recompile and fix errors**

Call `request_recompile`. Check compilation errors via `get_compilation_errors`. Fix any issues.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/ChatPanel.cs
git commit -m "feat: add programmatic chat UI with scroll view and bubbles"
```

---

### Task 6: ChatManager — orchestrate LLM + UI + sentence splitting (depends on Tasks 2,3,4,5)

**Files:**
- Create: `Assets/Scripts/Core/ChatManager.cs`

- [ ] **Step 1: Create ChatManager.cs**

```csharp
// Assets/Scripts/Core/ChatManager.cs
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YaeSakura
{
    public class ChatManager : MonoBehaviour
    {
        public ChatPanel chatPanel;

        private LLMClient _llm;
        private MemoryManager _memory;
        private TTSClient _tts;
        private AppSettings _settings;
        private CancellationTokenSource _currentCTS;
        private bool _isProcessing;

        // Sentence delimiters for splitting
        private static readonly char[] SentenceDelims = { '。', '！', '？', '！', '\n' };
        private static readonly char[] ActionDelims = { '（', '）', '(', ')' };

        private void Start()
        {
            _settings = AppConfig.Load();
            _llm = new LLMClient(_settings.chatConfig);
            _memory = new MemoryManager();
            _tts = new TTSClient(_settings.ttsConfig);

            chatPanel.OnSendMessage += HandleUserMessage;
        }

        private async void HandleUserMessage(string text)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _currentCTS = new CancellationTokenSource();

            // Stop current TTS playback
            _tts?.StopPlayback();
            // Connect TTS if not connected
            if (_tts != null && !_tts.IsConnected)
                await _tts.Connect();

            var messages = _memory.BuildMessages(text);
            var streamBubble = chatPanel.BeginStream();

            // Track sentence state for splitting
            var currentSentence = new StringBuilder();
            var fullResponse = new StringBuilder();

            await _llm.SendStreaming(
                messages,
                onChunk: chunk =>
                {
                    fullResponse.Append(chunk);
                    // Main thread UI updates
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        ProcessChunk(chunk, currentSentence, fullResponse, streamBubble);
                    });
                },
                onComplete: finalText =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        // Flush any remaining sentence
                        if (currentSentence.Length > 0)
                        {
                            var final = currentSentence.ToString().Trim();
                            if (!string.IsNullOrEmpty(final))
                                _tts?.EnqueueSynthesis(final);
                        }
                        chatPanel.FinalizeStream();
                        _memory.AddTurn(text, fullResponse.ToString());
                        _isProcessing = false;
                    });
                },
                onError: err =>
                {
                    UnityMainThreadDispatcher.Instance?.Enqueue(() =>
                    {
                        Debug.LogError($"LLM Error: {err}");
                        chatPanel.FinalizeStream();
                        chatPanel.AddBubble($"[错误] {err}", false);
                        _isProcessing = false;
                    });
                },
                _currentCTS.Token
            );
        }

        private void ProcessChunk(string chunk, StringBuilder sentence, StringBuilder full, Text bubble)
        {
            for (int i = 0; i < chunk.Length; i++)
            {
                char c = chunk[i];
                sentence.Append(c);
                chatPanel.AppendStream(c.ToString());

                if (IsSentenceEnd(c))
                {
                    var s = sentence.ToString().Trim();
                    sentence.Clear();

                    // Extract action lines (bracketed content)
                    var actionFree = StripActions(s);
                    if (!string.IsNullOrEmpty(actionFree))
                        _tts?.EnqueueSynthesis(actionFree);
                }
            }
        }

        private bool IsSentenceEnd(char c)
        {
            foreach (var d in SentenceDelims)
                if (c == d) return true;
            return false;
        }

        private string StripActions(string sentence)
        {
            // Extract bracketed content as action lines, return cleaned sentence
            var cleaned = new StringBuilder();
            int depth = 0;
            var action = new StringBuilder();
            bool foundAction = false;

            for (int i = 0; i < sentence.Length; i++)
            {
                char c = sentence[i];
                if (c == '（' || c == '(')
                {
                    depth++;
                    foundAction = true;
                }
                else if (c == '）' || c == ')')
                {
                    depth--;
                    if (depth == 0 && action.Length > 0)
                    {
                        chatPanel.AddActionLine(action.ToString());
                        action.Clear();
                    }
                }
                else if (depth > 0)
                {
                    action.Append(c);
                }
                else
                {
                    cleaned.Append(c);
                }
            }
            return cleaned.ToString().Trim();
        }

        private void OnDestroy()
        {
            _currentCTS?.Cancel();
            _llm?.Dispose();
            _tts?.Disconnect();
        }
    }
}
```

- [ ] **Step 2: Create MainThreadDispatcher helper**

```csharp
// Assets/Scripts/Core/MainThreadDispatcher.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace YaeSakura
{
    /// Simple singleton to dispatch actions to the Unity main thread.
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        public static UnityMainThreadDispatcher Instance { get; private set; }

        private Queue<Action> _queue = new Queue<Action>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                    _queue.Dequeue()?.Invoke();
            }
        }

        public void Enqueue(Action action)
        {
            lock (_queue) { _queue.Enqueue(action); }
        }
    }
}
```

- [ ] **Step 3: Recompile**

Call `request_recompile` and fix any errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/ChatManager.cs Assets/Scripts/Core/MainThreadDispatcher.cs
git commit -m "feat: add ChatManager with streaming + sentence splitting + action lines"
```

---

### Task 5: TTS WebSocket client

**Files:**
- Create: `Assets/Scripts/Core/TTSClient.cs`

Uses `System.Net.WebSockets.ClientWebSocket` (built-in .NET). GPT-SoVITS WebSocket protocol: send JSON `{"text": "...", "character": "sakura"}`, receive binary WAV audio.

- [ ] **Step 1: Create TTSClient.cs**

```csharp
// Assets/Scripts/Core/TTSClient.cs
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
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            _ws?.Dispose();
        }

        public void SetAudioSource(AudioSource source)
        {
            _audioSource = source;
            _originalVolume = source.volume;
        }

        public void EnqueueSynthesis(string text)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(text)) return;
            _ = SynthesizeAsync(text);
        }

        private async Task SynthesizeAsync(string text)
        {
            try
            {
                var request = $"{{\"text\":\"{text}\",\"character\":\"{_config.character}\"}}";
                var bytes = Encoding.UTF8.GetBytes(request);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);

                // Receive binary WAV data
                var buffer = new byte[8192];
                var audioData = new List<byte>();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Binary)
                        audioData.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                lock (_playQueue) { _playQueue.Enqueue(audioData.ToArray()); }
                if (!_isPlaying) PlayNext();
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
                    // Wait for playback
                    while (_audioSource.isPlaying)
                        await Task.Delay(100);
                }
            }
        }

        public void StopPlayback()
        {
            // 5-step fade out over ~750ms
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
            // Parse WAV header: 44 bytes header, then PCM data
            if (wavData.Length < 44) return null;
            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int dataSize = BitConverter.ToInt32(wavData, 40);
            int sampleCount = dataSize / 2; // 16-bit = 2 bytes per sample

            var clip = AudioClip.Create("tts", sampleCount, channels, sampleRate, false);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                samples[i] = BitConverter.ToInt16(wavData, 44 + i * 2) / 32768f;

            clip.SetData(samples, 0);
            return clip;
        }
    }
}
```

- [ ] **Step 2: Recompile and fix errors**

Call `request_recompile`. The `ClientWebSocket` is available in .NET Standard 2.1. Ensure Unity project is set to .NET Standard 2.1 in Player Settings (it is by default for 2022.3).

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/TTSClient.cs
git commit -m "feat: add TTS WebSocket client for GPT-SoVITS"
```

---

### Task 7: Lip sync (audio amplitude → BlendShape)

**Files:**
- Create: `Assets/Scripts/Character/LipSync.cs`

- [ ] **Step 1: Create LipSync.cs**

```csharp
// Assets/Scripts/Character/LipSync.cs
using UnityEngine;

namespace YaeSakura
{
    /// Drives character mouth BlendShape based on AudioSource output amplitude.
    public class LipSync : MonoBehaviour
    {
        public AudioSource audioSource;
        public SkinnedMeshRenderer skinnedMesh;
        public int mouthBlendShapeIndex = 0; // adjust to match model's "Mouth_Open" index
        public float sensitivity = 2f;
        public float smoothSpeed = 8f;
        public float minThreshold = 0.02f;

        private float _currentValue;
        private float[] _samples = new float[256];

        private void Update()
        {
            if (audioSource == null || skinnedMesh == null) return;

            float target = 0f;
            if (audioSource.isPlaying)
            {
                audioSource.GetOutputData(_samples, 0);
                float sum = 0f;
                for (int i = 0; i < _samples.Length; i++)
                    sum += Mathf.Abs(_samples[i]);
                float rms = sum / _samples.Length;
                target = Mathf.Clamp01(rms * sensitivity);
                if (target < minThreshold) target = 0f;
            }

            _currentValue = Mathf.Lerp(_currentValue, target, Time.deltaTime * smoothSpeed);
            skinnedMesh.SetBlendShapeWeight(mouthBlendShapeIndex, _currentValue * 100f);
        }
    }
}
```

- [ ] **Step 2: Recompile**

Call `request_recompile`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Character/LipSync.cs
git commit -m "feat: add lip sync component (audio amplitude → BlendShape)"
```

---

### Task 8: Scene setup — wire everything together

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity` (via MCP execute_code in Unity Editor)

This task creates the full scene hierarchy using MCP `execute_code`: Canvas with ChatPanel, ChatManager, MainThreadDispatcher, TTS AudioSource.

- [ ] **Step 1: Create scene hierarchy via MCP**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class SetupScene
{
    public static string Run()
    {
        // Camera
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f);
        }

        // Canvas
        var canvasGO = new GameObject("ChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // ChatPanel
        var panelGO = new GameObject("ChatPanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        var chatPanel = panelGO.AddComponent<YaeSakura.ChatPanel>();

        // ChatManager
        var managerGO = new GameObject("ChatManager", typeof(YaeSakura.ChatManager));
        var manager = managerGO.GetComponent<YaeSakura.ChatManager>();
        manager.chatPanel = chatPanel;

        // MainThreadDispatcher
        var dispatcherGO = new GameObject("MainThreadDispatcher", typeof(YaeSakura.UnityMainThreadDispatcher));

        // TTS AudioSource
        var audioGO = new GameObject("TTSAudioSource", typeof(AudioSource));
        var audioSource = audioGO.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Character placeholder (cube for now, replace with FBX later)
        var character = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        character.name = "YaeSakura_Character";
        character.transform.position = new Vector3(0, 0.5f, 0);
        character.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);

        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0, -0.5f, 0);

        // Character camera (for future RenderTexture in overlay mode)
        var charCamGO = new GameObject("CharacterCamera", typeof(Camera));
        var charCam = charCamGO.GetComponent<Camera>();
        charCam.transform.position = new Vector3(0, 0.5f, 1.5f);
        charCam.transform.LookAt(new Vector3(0, 0.3f, 0));
        charCam.clearFlags = CameraClearFlags.SolidColor;
        charCam.backgroundColor = new Color(0, 0, 0, 0);
        charCam.enabled = false; // disabled by default

        return "Scene setup complete. Objects created:\n"
             + "- ChatCanvas (uGUI Canvas + ChatPanel)\n"
             + "- ChatManager (orchestrator)\n"
             + "- MainThreadDispatcher\n"
             + "- TTSAudioSource\n"
             + "- YaeSakura_Character (placeholder Capsule)\n"
             + "- Floor\n"
             + "- CharacterCamera (off by default, for RenderTexture)";
    }
}
```

- [ ] **Step 2: Run the setup**

Call `execute_code` with the above snippet, verify scene objects are created.

- [ ] **Step 3: Capture scene view to verify**

Call `capture_scene_view` to verify the scene looks correct.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/
git commit -m "feat: wire up scene with ChatManager, UI canvas, and character placeholder"
```

---

### Task 9: Settings panel (sidebar)

**Files:**
- Create: `Assets/Scripts/UI/SettingsPanel.cs`

- [ ] **Step 1: Create SettingsPanel.cs**

```csharp
// Assets/Scripts/UI/SettingsPanel.cs
using UnityEngine;
using UnityEngine.UI;

namespace YaeSakura
{
    /// Slide-out sidebar for API config, TTS, and auto-reply settings.
    public class SettingsPanel : MonoBehaviour
    {
        public Button toggleButton;
        public RectTransform panelRect;
        private bool _isOpen;
        private AppSettings _settings;

        private Dropdown providerDropdown;
        private InputField apiKeyField;
        private InputField modelField;
        private InputField ttsUrlField;
        private Dropdown autoReplyDropdown;

        public System.Action<AppSettings> OnSettingsChanged;

        private void Start()
        {
            _settings = AppConfig.Load();
            CreateUI();
            toggleButton.onClick.AddListener(Toggle);
            panelRect.gameObject.SetActive(false);
        }

        private void CreateUI()
        {
            // Toggle button (gear icon placeholder)
            if (toggleButton == null)
            {
                var btnGO = new GameObject("SettingsToggle", typeof(Button), typeof(Image), typeof(LayoutElement));
                btnGO.transform.SetParent(transform, false);
                var rt = btnGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-8, -8);
                rt.sizeDelta = new Vector2(36, 36);
                btnGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
                var btnText = new GameObject("Text", typeof(Text));
                btnText.transform.SetParent(btnGO.transform, false);
                var t = btnText.GetComponent<Text>();
                t.text = "⛭";
                t.fontSize = 20; t.color = Color.white;
                t.alignment = TextAnchor.MiddleCenter;
                t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.sizeDelta = Vector2.zero;
                toggleButton = btnGO.GetComponent<Button>();
            }

            // Sidebar panel
            if (panelRect == null)
            {
                var panelGO = new GameObject("SettingsPanel", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                panelGO.transform.SetParent(transform, false);
                panelRect = panelGO.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(1, 0); panelRect.anchorMax = new Vector2(1, 1);
                panelRect.pivot = new Vector2(1, 0.5f);
                panelRect.sizeDelta = new Vector2(280, 0);
                panelRect.anchoredPosition = Vector2.zero;
                panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.95f);
                var vlg = panelGO.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(12, 12, 12, 12);
                vlg.spacing = 10;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.UpperLeft;
            }

            // Title
            AddLabel("设置", 18, Color.white);

            // Provider dropdown
            AddLabel("API 运营商", 12, new Color(0.7f, 0.7f, 0.75f));
            providerDropdown = AddDropdown(new[] { "DeepSeek", "千问" },
                _settings.chatConfig.provider == "qwen" ? 1 : 0);
            providerDropdown.onValueChanged.AddListener(idx =>
            {
                _settings.chatConfig.provider = idx == 1 ? "qwen" : "deepseek";
                _settings.chatConfig.apiUrl = AppConfig.GetDefaultAPIUrl(_settings.chatConfig.provider);
            });

            // API Key
            AddLabel("API Key", 12, new Color(0.7f, 0.7f, 0.75f));
            apiKeyField = AddInput(_settings.chatConfig.apiKey, true);

            // Model
            AddLabel("模型", 12, new Color(0.7f, 0.7f, 0.75f));
            modelField = AddInput(_settings.chatConfig.model, false);

            // TTS URL
            AddLabel("TTS 服务地址", 12, new Color(0.7f, 0.7f, 0.75f));
            ttsUrlField = AddInput(_settings.ttsConfig.serverUrl, false);

            // Auto-reply
            AddLabel("自动回复", 12, new Color(0.7f, 0.7f, 0.75f));
            autoReplyDropdown = AddDropdown(new[] { "关闭", "1分钟", "5分钟", "10分钟", "30分钟" },
                MapAutoReplyIndex(_settings.autoReplyMinutes, _settings.autoReplyEnabled));
            autoReplyDropdown.onValueChanged.AddListener(idx =>
            {
                _settings.autoReplyEnabled = idx > 0;
                _settings.autoReplyMinutes = idx switch { 1 => 1, 2 => 5, 3 => 10, 4 => 30, _ => 5 };
            });

            // Save button
            var saveBtnGO = new GameObject("SaveBtn", typeof(Button), typeof(Image));
            saveBtnGO.transform.SetParent(panelRect, false);
            saveBtnGO.GetComponent<Image>().color = new Color(0.35f, 0.2f, 0.45f);
            var sText = new GameObject("Text", typeof(Text));
            sText.transform.SetParent(saveBtnGO.transform, false);
            var st = sText.GetComponent<Text>();
            st.text = "保存设置"; st.fontSize = 14; st.color = Color.white; st.alignment = TextAnchor.MiddleCenter;
            st.rectTransform.anchorMin = Vector2.zero; st.rectTransform.anchorMax = Vector2.one; st.rectTransform.sizeDelta = Vector2.zero;
            var sLE = saveBtnGO.AddComponent<LayoutElement>();
            sLE.minHeight = 36;
            saveBtnGO.GetComponent<Button>().onClick.AddListener(SaveSettings);
        }

        private void AddLabel(string text, int size, Color color)
        {
            var go = new GameObject("Label", typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(panelRect, false);
            var t = go.GetComponent<Text>();
            t.text = text; t.fontSize = size; t.color = color;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<LayoutElement>().minHeight = size + 4;
        }

        private InputField AddInput(string defaultValue, bool isPassword)
        {
            var go = new GameObject("Input", typeof(InputField), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(panelRect, false);
            go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.18f);
            go.GetComponent<LayoutElement>().minHeight = 30;
            var textGO = new GameObject("Text", typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var t = textGO.GetComponent<Text>();
            t.text = defaultValue; t.fontSize = 14; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = new Color(0.85f, 0.85f, 0.9f); t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(6, 2); t.rectTransform.offsetMax = new Vector2(-6, -2);
            var input = go.GetComponent<InputField>();
            input.textComponent = t;
            input.text = defaultValue;
            if (isPassword) input.contentType = InputField.ContentType.Password;
            return input;
        }

        private Dropdown AddDropdown(string[] options, int defaultIdx)
        {
            var go = new GameObject("Dropdown", typeof(Dropdown), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(panelRect, false);
            go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.18f);
            go.GetComponent<LayoutElement>().minHeight = 30;

            // Template (required by Dropdown)
            var tmpl = new GameObject("Template", typeof(Image), typeof(ScrollRect));
            tmpl.transform.SetParent(go.transform, false);
            tmpl.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.15f);
            var tmplRT = tmpl.GetComponent<RectTransform>();
            tmplRT.anchorMin = new Vector2(0, 0); tmplRT.anchorMax = new Vector2(1, 0);
            tmplRT.pivot = new Vector2(0.5f, 1); tmplRT.sizeDelta = new Vector2(0, 120);

            var viewport = new GameObject("Viewport", typeof(Image), typeof(Mask));
            viewport.transform.SetParent(tmpl.transform, false);
            viewport.GetComponent<RectTransform>().anchorMin = Vector2.zero; viewport.GetComponent<RectTransform>().anchorMax = Vector2.one;
            viewport.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            var content = new GameObject("Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            content.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1); content.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            content.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);

            var item = new GameObject("Item", typeof(Toggle), typeof(Image));
            item.transform.SetParent(content.transform, false);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            var itemText = new GameObject("Item Label", typeof(Text));
            itemText.transform.SetParent(item.transform, false);
            var it = itemText.GetComponent<Text>();
            it.fontSize = 14; it.color = Color.white; it.alignment = TextAnchor.MiddleLeft;
            it.rectTransform.anchorMin = Vector2.zero; it.rectTransform.anchorMax = Vector2.one;
            it.rectTransform.offsetMin = new Vector2(8, 0); it.rectTransform.offsetMax = new Vector2(-8, 0);

            var dd = go.GetComponent<Dropdown>();
            dd.template = tmplRT;
            dd.captionText = null; // We'll just show in the main button area
            dd.options = new System.Collections.Generic.List<Dropdown.OptionData>();
            foreach (var o in options) dd.options.Add(new Dropdown.OptionData(o));
            dd.value = defaultIdx;

            // Caption text
            var captionGO = new GameObject("Label", typeof(Text));
            captionGO.transform.SetParent(go.transform, false);
            var capRT = captionGO.GetComponent<RectTransform>();
            capRT.anchorMin = Vector2.zero; capRT.anchorMax = Vector2.one;
            capRT.offsetMin = new Vector2(8, 2); capRT.offsetMax = new Vector2(-8, -2);
            var capT = captionGO.GetComponent<Text>();
            capT.text = options[defaultIdx]; capT.fontSize = 14; capT.color = new Color(0.85f, 0.85f, 0.9f);
            capT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dd.captionText = capT;

            return dd;
        }

        private int MapAutoReplyIndex(int minutes, bool enabled)
        {
            if (!enabled) return 0;
            return minutes switch { 1 => 1, 5 => 2, 10 => 3, 30 => 4, _ => 2 };
        }

        private void SaveSettings()
        {
            _settings.chatConfig.apiKey = apiKeyField.text;
            _settings.chatConfig.model = modelField.text;
            _settings.ttsConfig.serverUrl = ttsUrlField.text;
            AppConfig.Save(_settings);
            OnSettingsChanged?.Invoke(_settings);
        }

        private void Toggle()
        {
            _isOpen = !_isOpen;
            panelRect.gameObject.SetActive(_isOpen);
        }
    }
}
```

- [ ] **Step 2: Recompile**

Call `request_recompile`. Note: the Dropdown creation is verbose but avoids needing prefabs. If compilation errors occur due to the Dropdown template complexity, simplify by using basic InputFields only for the first version.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/SettingsPanel.cs
git commit -m "feat: add settings sidebar for API/TTS/auto-reply config"
```

---

### Task 10: Integration verification

**Files:** None (verification only)

- [ ] **Step 1: Verify compilation**

Call `get_compilation_errors` — should be clean.

- [ ] **Step 2: Enter play mode and test basic scene**

Call `enter_play_mode`, verify the scene loads without errors. Call `get_console_logs` to check for any runtime errors.

- [ ] **Step 3: Exit play mode**

Call `exit_play_mode`.

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat: complete 3D Yae Sakura core implementation"
```

---

## Future Tasks (out of scope, documented for reference)

- **Character model import**: MMD FBX → Unity rig + animations
- **Overlay mode**: Windows transparent window via native plugin
- **Auto-reply**: Idle timer + random prompts
- **Memory bridge**: HTTP to Python backend vault system
- **Visual perception**: Camera + MediaPipe integration
- **Music sensing**: Windows SMTC + audio analysis
