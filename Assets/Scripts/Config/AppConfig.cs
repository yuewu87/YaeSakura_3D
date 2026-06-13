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
        private const string KEY_ENABLE_THINKING = "ys_enable_thinking";
        private const string KEY_TEMPERATURE = "ys_temperature";
        private const string KEY_MAX_TOKENS = "ys_max_tokens";
        private const string KEY_TTS_CHARACTER = "ys_tts_character";

        public static AppSettings Load()
        {
            var s = new AppSettings();
            s.chatConfig.provider = PlayerPrefs.GetString(KEY_API_PROVIDER, "deepseek") == "qwen" ? APIProvider.Qwen : APIProvider.DeepSeek;
            s.chatConfig.apiUrl = PlayerPrefs.GetString(KEY_API_URL, GetDefaultAPIUrl(s.chatConfig.provider));
            s.chatConfig.apiKey = PlayerPrefs.GetString(KEY_API_KEY, "");
            s.chatConfig.model = PlayerPrefs.GetString(KEY_MODEL,
                s.chatConfig.provider == APIProvider.Qwen ? "qwen-turbo" : "deepseek-chat");
            s.ttsConfig.serverUrl = PlayerPrefs.GetString(KEY_TTS_URL, "ws://localhost:8770");
            s.autoReplyMinutes = PlayerPrefs.GetInt(KEY_AUTO_REPLY_MINUTES, 5);
            s.autoReplyEnabled = PlayerPrefs.GetInt(KEY_AUTO_REPLY_ENABLED, 0) == 1;
            s.chatConfig.enableThinking = PlayerPrefs.GetInt(KEY_ENABLE_THINKING, 0) == 1;
            s.chatConfig.temperature = PlayerPrefs.GetFloat(KEY_TEMPERATURE, 0.7f);
            s.chatConfig.maxTokens = PlayerPrefs.GetInt(KEY_MAX_TOKENS, 1024);
            s.ttsConfig.character = PlayerPrefs.GetString(KEY_TTS_CHARACTER, "sakura");
            return s;
        }

        public static void Save(AppSettings s)
        {
            if (s == null) return;
            PlayerPrefs.SetString(KEY_API_PROVIDER, s.chatConfig.provider.ToString().ToLower());
            PlayerPrefs.SetString(KEY_API_URL, s.chatConfig.apiUrl);
            PlayerPrefs.SetString(KEY_API_KEY, s.chatConfig.apiKey);
            PlayerPrefs.SetString(KEY_MODEL, s.chatConfig.model);
            PlayerPrefs.SetString(KEY_TTS_URL, s.ttsConfig.serverUrl);
            PlayerPrefs.SetInt(KEY_AUTO_REPLY_MINUTES, s.autoReplyMinutes);
            PlayerPrefs.SetInt(KEY_AUTO_REPLY_ENABLED, s.autoReplyEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_ENABLE_THINKING, s.chatConfig.enableThinking ? 1 : 0);
            PlayerPrefs.SetFloat(KEY_TEMPERATURE, s.chatConfig.temperature);
            PlayerPrefs.SetInt(KEY_MAX_TOKENS, s.chatConfig.maxTokens);
            PlayerPrefs.SetString(KEY_TTS_CHARACTER, s.ttsConfig.character);
            PlayerPrefs.Save();
        }

        public static string GetDefaultAPIUrl(APIProvider provider)
        {
            return provider == APIProvider.Qwen
                ? "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions"
                : "https://api.deepseek.com/v1/chat/completions";
        }
    }
}
