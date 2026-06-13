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
