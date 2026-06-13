using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YaeSakura
{
    public class MemoryManager
    {
        private const int MAX_HISTORY_ROUNDS = 20;
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
                _history = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Message>>(json)
                    ?? new List<Message>();
            }
        }
    }
}
