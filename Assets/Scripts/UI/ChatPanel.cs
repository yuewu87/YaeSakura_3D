using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YaeSakura
{
    public class ChatPanel : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public RectTransform contentRoot;
        public InputField inputField;
        public Button sendButton;

        private List<GameObject> _bubbles = new List<GameObject>();
        private UnityEngine.UI.Text _currentStreamBubble;
        private bool _isStreaming;

        public bool IsStreaming => _isStreaming;
        public System.Action<string> OnSendMessage;

        // 2D-version matching colors
        private static Color BgDark    = new Color(0.102f, 0.102f, 0.180f); // #1a1a2e
        private static Color AssistBg  = new Color(1f, 0.784f, 0.843f, 0.30f);
        private static Color UserBg    = new Color(1f, 1f, 1f, 0.12f);
        private static Color AssistText= new Color(1f, 0.910f, 0.933f); // #ffe8ee
        private static Color UserText  = new Color(0.867f, 0.867f, 0.867f); // #ddd
        private static Color ActionText= new Color(1f, 0.784f, 0.824f, 0.55f);
        private static Color InputBg   = new Color(1f, 1f, 1f, 0.06f);
        private static Color InputBorder= new Color(1f, 1f, 1f, 0.10f);
        private static Color SendBg    = new Color(1f, 0.588f, 0.667f, 0.25f);
        private static Color TimeText  = new Color(1f, 1f, 1f, 0.25f);
        private static Color TitleText = new Color(1f, 1f, 1f, 0.5f);
        private static Color TitleBg   = new Color(0.047f, 0.039f, 0.086f, 0.60f);

        private Font _font;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateUI();
            sendButton.onClick.AddListener(SendMessage);
        }

        private void CreateUI()
        {
            // === Title Bar ===
            var titleGO = new GameObject("TitleBar");
            titleGO.transform.SetParent(transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.1f, 0.92f);
            titleRT.anchorMax = new Vector2(0.9f, 1);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            var titleBg = titleGO.AddComponent<UnityEngine.UI.Image>();
            titleBg.color = TitleBg;

            var titleTextGO = new GameObject("Title");
            titleTextGO.transform.SetParent(titleGO.transform, false);
            var titleT = titleTextGO.AddComponent<UnityEngine.UI.Text>();
            titleT.text = "八重樱 · 圣痕之庭";
            titleT.font = _font;
            titleT.fontSize = 14;
            titleT.fontStyle = FontStyle.Bold;
            titleT.color = TitleText;
            titleT.alignment = TextAnchor.MiddleLeft;
            var titleTRT = titleTextGO.GetComponent<RectTransform>();
            titleTRT.anchorMin = Vector2.zero; titleTRT.anchorMax = Vector2.one;
            titleTRT.offsetMin = new Vector2(10, 0); titleTRT.offsetMax = Vector2.zero;

            // === Chat Container Panel (半透明卡片包裹对话和输入) ===
            var containerGO = new GameObject("ChatContainer");
            containerGO.transform.SetParent(transform, false);
            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.1f, 0.08f);
            containerRT.anchorMax = new Vector2(0.9f, 0.92f);
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;
            var containerBg = containerGO.AddComponent<UnityEngine.UI.Image>();
            containerBg.color = new Color(0.05f, 0.10f, 0.15f, 0.5f); // rgba(13,26,38,0.5)

            // === ScrollView ===
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(containerGO.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0.10f);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;
            scrollRect = scrollGO.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            contentRoot = content.AddComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0, 1); contentRoot.anchorMax = new Vector2(1, 1);
            contentRoot.pivot = new Vector2(0.5f, 1);
            contentRoot.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // === Input area ===
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(containerGO.transform, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0);
            inputRT.anchorMax = new Vector2(0.79f, 0.10f);
            inputRT.offsetMin = new Vector2(12, 2); inputRT.offsetMax = new Vector2(-4, -2);
            inputField = inputGO.AddComponent<InputField>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(12, 3); textRT.offsetMax = new Vector2(-12, -3);
            var text = textGO.AddComponent<UnityEngine.UI.Text>();
            text.font = _font;
            text.fontSize = 16;
            text.color = UserText;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            inputField.textComponent = text;
            inputField.lineType = InputField.LineType.MultiLineNewline;

            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(inputGO.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(14, 3); phRT.offsetMax = new Vector2(-14, -3);
            var ph = phGO.AddComponent<UnityEngine.UI.Text>();
            ph.text = "输入消息...";
            ph.font = _font;
            ph.fontSize = 14;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1f, 1f, 1f, 0.3f);
            ph.alignment = TextAnchor.MiddleLeft;
            inputField.placeholder = ph;

            // === Send Button ===
            var btnGO = new GameObject("SendButton");
            btnGO.transform.SetParent(containerGO.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.8f, 0);
            btnRT.anchorMax = new Vector2(0.98f, 0.10f);
            btnRT.offsetMin = new Vector2(4, 2); btnRT.offsetMax = new Vector2(-8, -2);
            sendButton = btnGO.AddComponent<Button>();
            var btnBg = btnGO.AddComponent<UnityEngine.UI.Image>();
            btnBg.color = SendBg;

            var btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = btnTextGO.AddComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
            var btnText = btnTextGO.AddComponent<UnityEngine.UI.Text>();
            btnText.text = "发送";
            btnText.font = _font;
            btnText.fontSize = 16;
            btnText.color = new Color(1f, 1f, 1f, 0.8f);
            btnText.alignment = TextAnchor.MiddleCenter;
        }

        public void AddBubble(string text, bool isUser)
        {
            if (contentRoot == null) return;
            // Wrapper
            var wrapper = new GameObject("Wrapper");
            wrapper.transform.SetParent(contentRoot, false);
            var wLE = wrapper.AddComponent<LayoutElement>();
            wLE.minWidth = 200;

            // Name label
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(wrapper.transform, false);
            var nameT = nameGO.AddComponent<UnityEngine.UI.Text>();
            nameT.text = isUser ? "旅人" : "八重樱";
            nameT.font = _font;
            nameT.fontSize = 12;
            nameT.color = isUser ? new Color(0.6f, 0.6f, 0.6f, 0.5f) : new Color(1f, 0.718f, 0.773f, 0.6f);
            nameT.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.sizeDelta = new Vector2(0, 14);
            nameRT.anchoredPosition = Vector2.zero;

            // Bubble
            var go = new GameObject("Bubble");
            go.transform.SetParent(wrapper.transform, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.font = _font;
            t.fontSize = 16;
            t.color = isUser ? UserText : AssistText;
            t.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Offset: user bubbles shift right
            var bubbleRT = go.GetComponent<RectTransform>();
            float maxW = contentRoot.rect.width * 0.7f;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 200;
            le.preferredWidth = maxW;
            if (isUser)
            {
                bubbleRT.anchorMin = new Vector2(1, 1); bubbleRT.anchorMax = new Vector2(1, 1);
                bubbleRT.pivot = new Vector2(1, 1);
                bubbleRT.anchoredPosition = new Vector2(0, -14);
            }
            else
            {
                bubbleRT.anchorMin = new Vector2(0, 1); bubbleRT.anchorMax = new Vector2(0, 1);
                bubbleRT.pivot = new Vector2(0, 1);
                bubbleRT.anchoredPosition = new Vector2(0, -14);
            }

            // Background using Image (if available)
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            if (bg != null) bg.color = isUser ? UserBg : AssistBg;

            _bubbles.Add(wrapper);
            ScrollToBottom();
        }

        public void AddActionLine(string text)
        {
            if (contentRoot == null) return;
            var go = new GameObject("ActionLine");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.font = _font;
            t.fontSize = 14;
            t.fontStyle = FontStyle.Italic;
            t.color = ActionText;
            t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 200;
            _bubbles.Add(go);
            ScrollToBottom();
        }

        public void AddTimeDivider(string text)
        {
            if (contentRoot == null) return;
            var go = new GameObject("TimeDivider");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = "━━  " + text + "  ━━";
            t.font = _font;
            t.fontSize = 12;
            t.color = TimeText;
            t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _bubbles.Add(go);
            ScrollToBottom();
        }

        public UnityEngine.UI.Text BeginStream()
        {
            _isStreaming = true;
            var go = new GameObject("StreamBubble");
            go.transform.SetParent(contentRoot, false);
            _currentStreamBubble = go.AddComponent<UnityEngine.UI.Text>();
            _currentStreamBubble.text = "";
            _currentStreamBubble.font = _font;
            _currentStreamBubble.fontSize = 16;
            _currentStreamBubble.color = AssistText;
            _currentStreamBubble.alignment = TextAnchor.MiddleLeft;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 200;
            le.preferredWidth = contentRoot.rect.width * 0.7f;
            _bubbles.Add(go);
            return _currentStreamBubble;
        }

        public void AppendStream(string chunk)
        {
            if (_currentStreamBubble != null) _currentStreamBubble.text += chunk;
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
            foreach (var b in _bubbles) Destroy(b);
            _bubbles.Clear();
            _currentStreamBubble = null;
        }

        public void SendMessage()
        {
            if (inputField == null) return;
            var txt = inputField.text.Trim();
            if (string.IsNullOrEmpty(txt) || _isStreaming) return;
            inputField.text = "";
            AddBubble(txt, true);
            OnSendMessage?.Invoke(txt);
        }

        private void Update()
        {
            if (inputField != null && inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                    SendMessage();
            }
        }
    }
}
