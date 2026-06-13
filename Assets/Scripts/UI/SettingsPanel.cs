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

        private InputField apiKeyField;
        private InputField modelField;
        private InputField ttsUrlField;
        private Toggle deepseekToggle;
        private Toggle qwenToggle;
        private Toggle autoReplyToggle;

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
            // Toggle button (gear)
            var btnGO = new GameObject("SettingsToggle", typeof(Button), typeof(UnityEngine.UI.Image));
            btnGO.transform.SetParent(transform, false);
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-8, -8);
            rt.sizeDelta = new Vector2(36, 36);
            btnGO.GetComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            var tGO = new GameObject("Text", typeof(Text));
            tGO.transform.SetParent(btnGO.transform, false);
            var t = tGO.GetComponent<Text>();
            t.text = "⚙"; t.fontSize = 20; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tRT = t.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;
            toggleButton = btnGO.GetComponent<Button>();

            // Sidebar panel
            var panelGO = new GameObject("SettingsPanel", typeof(UnityEngine.UI.Image), typeof(VerticalLayoutGroup));
            panelGO.transform.SetParent(transform, false);
            panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0); panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 0.5f);
            panelRect.sizeDelta = new Vector2(280, 0);
            panelRect.anchoredPosition = Vector2.zero;
            panelGO.GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.95f);
            var vlg = panelGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            // Title
            AddLabel("设置", 18, Color.white);

            // Provider toggles
            AddLabel("API 运营商", 12, new Color(0.7f, 0.7f, 0.75f));
            var toggleGroup = new GameObject("ProviderGroup", typeof(HorizontalLayoutGroup)).GetComponent<HorizontalLayoutGroup>();
            toggleGroup.transform.SetParent(panelRect, false);
            toggleGroup.spacing = 8;
            toggleGroup.childForceExpandWidth = false;
            var tgrLE = toggleGroup.gameObject.AddComponent<LayoutElement>();
            tgrLE.minHeight = 24;

            deepseekToggle = AddToggle("DeepSeek", toggleGroup.transform, _settings.chatConfig.provider == APIProvider.DeepSeek);
            qwenToggle = AddToggle("千问", toggleGroup.transform, _settings.chatConfig.provider == APIProvider.Qwen);

            // Toggle group -- mutual exclusion via callbacks
            deepseekToggle.onValueChanged.AddListener(val => { if (val) { qwenToggle.isOn = false; _settings.chatConfig.provider = APIProvider.DeepSeek; } else if (!qwenToggle.isOn) deepseekToggle.isOn = true; });
            qwenToggle.onValueChanged.AddListener(val => { if (val) { deepseekToggle.isOn = false; _settings.chatConfig.provider = APIProvider.Qwen; } else if (!deepseekToggle.isOn) qwenToggle.isOn = true; });

            // API Key
            AddLabel("API Key", 12, new Color(0.7f, 0.7f, 0.75f));
            apiKeyField = AddInput(_settings.chatConfig.apiKey, true);

            // Model
            AddLabel("模型名称", 12, new Color(0.7f, 0.7f, 0.75f));
            modelField = AddInput(_settings.chatConfig.model, false);

            // TTS URL
            AddLabel("TTS 服务地址", 12, new Color(0.7f, 0.7f, 0.75f));
            ttsUrlField = AddInput(_settings.ttsConfig.serverUrl, false);

            // Auto-reply toggle
            AddLabel("自动回复", 12, new Color(0.7f, 0.7f, 0.75f));
            autoReplyToggle = AddToggle("启用", panelRect, _settings.autoReplyEnabled);

            // Save button
            var saveBtn = new GameObject("SaveBtn", typeof(Button), typeof(UnityEngine.UI.Image), typeof(LayoutElement));
            saveBtn.transform.SetParent(panelRect, false);
            saveBtn.GetComponent<UnityEngine.UI.Image>().color = new Color(0.35f, 0.2f, 0.45f);
            saveBtn.GetComponent<LayoutElement>().minHeight = 36;
            var sText = new GameObject("Text", typeof(Text));
            sText.transform.SetParent(saveBtn.transform, false);
            var st = sText.GetComponent<Text>();
            st.text = "保存设置"; st.fontSize = 14; st.color = Color.white;
            st.alignment = TextAnchor.MiddleCenter;
            st.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            st.rectTransform.anchorMin = Vector2.zero; st.rectTransform.anchorMax = Vector2.one;
            st.rectTransform.sizeDelta = Vector2.zero;
            saveBtn.GetComponent<Button>().onClick.AddListener(SaveSettings);
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
            var go = new GameObject("Input", typeof(InputField), typeof(UnityEngine.UI.Image), typeof(LayoutElement));
            go.transform.SetParent(panelRect, false);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.18f);
            go.GetComponent<LayoutElement>().minHeight = 30;
            var textGO = new GameObject("Text", typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var t = textGO.GetComponent<Text>();
            t.text = defaultValue; t.fontSize = 14;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = new Color(0.85f, 0.85f, 0.9f);
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(6, 2); t.rectTransform.offsetMax = new Vector2(-6, -2);
            var input = go.GetComponent<InputField>();
            input.textComponent = t;
            input.text = defaultValue;
            if (isPassword) input.contentType = InputField.ContentType.Password;
            return input;
        }

        private Toggle AddToggle(string label, Transform parent, bool isOn)
        {
            var go = new GameObject("Toggle_" + label, typeof(Toggle), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<UnityEngine.UI.Image>().color = isOn ? new Color(0.35f, 0.2f, 0.45f) : new Color(0.1f, 0.1f, 0.18f);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 80; le.minHeight = 24;

            var bg = new GameObject("Background", typeof(UnityEngine.UI.Image));
            bg.transform.SetParent(go.transform, false);
            bg.GetComponent<RectTransform>().anchorMin = Vector2.zero; bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            bg.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);

            var check = new GameObject("Checkmark", typeof(UnityEngine.UI.Image));
            check.transform.SetParent(bg.transform, false);
            var ckRT = check.GetComponent<RectTransform>();
            ckRT.anchorMin = new Vector2(0.05f, 0.15f); ckRT.anchorMax = new Vector2(0.25f, 0.85f);
            ckRT.sizeDelta = Vector2.zero;
            check.GetComponent<UnityEngine.UI.Image>().color = Color.white;

            var labelGO = new GameObject("Label", typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            var lt = labelGO.GetComponent<Text>();
            lt.text = label; lt.fontSize = 14; lt.color = new Color(0.85f, 0.85f, 0.9f);
            lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lt.alignment = TextAnchor.MiddleLeft;
            var lRT = lt.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.3f, 0); lRT.anchorMax = Vector2.one;
            lRT.sizeDelta = Vector2.zero;
            lRT.offsetMin = new Vector2(4, 0); lRT.offsetMax = new Vector2(0, 0);

            var toggle = go.GetComponent<Toggle>();
            toggle.graphic = check.GetComponent<UnityEngine.UI.Image>();
            toggle.isOn = isOn;

            return toggle;
        }

        private void SaveSettings()
        {
            _settings.chatConfig.apiKey = apiKeyField.text;
            _settings.chatConfig.model = modelField.text;
            _settings.ttsConfig.serverUrl = ttsUrlField.text;
            _settings.autoReplyEnabled = autoReplyToggle.isOn;
            _settings.chatConfig.apiUrl = AppConfig.GetDefaultAPIUrl(_settings.chatConfig.provider);
            AppConfig.Save(_settings);
            OnSettingsChanged?.Invoke(_settings);
            panelRect.gameObject.SetActive(false);
            _isOpen = false;
        }

        private void Toggle()
        {
            _isOpen = !_isOpen;
            panelRect.gameObject.SetActive(_isOpen);
        }
    }
}
