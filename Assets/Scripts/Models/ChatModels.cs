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
