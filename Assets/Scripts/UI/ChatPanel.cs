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
        private Text _currentStreamBubble;
        private bool _isStreaming;

        public bool IsStreaming => _isStreaming;
        public System.Action<string> OnSendMessage;

        private Color userBubbleColor = new Color(0.16f, 0.23f, 0.35f);
        private Color characterBubbleColor = new Color(0.23f, 0.1f, 0.23f);
        private Color actionLineColor = new Color(0.6f, 0.6f, 0.65f);
        private Color userTextColor = new Color(0.85f, 0.85f, 0.9f);
        private Color charTextColor = new Color(0.93f, 0.82f, 0.88f);

        private void Start()
        {
            CreateUI();
            sendButton.onClick.AddListener(SendMessage);
        }

        private void CreateUI()
        {
            // ScrollView
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

            // InputField
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

            // Send Button
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

        public void AddBubble(string text, bool isUser)
        {
            var go = new GameObject("Bubble", typeof(Text), typeof(ContentSizeFitter), typeof(LayoutElement), typeof(Image));
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

            go.GetComponent<Image>().color = isUser ? userBubbleColor : characterBubbleColor;
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
            {
                _currentStreamBubble.text += chunk;
            }
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

        public void SendMessage()
        {
            var txt = inputField.text.Trim();
            if (string.IsNullOrEmpty(txt) || _isStreaming) return;
            inputField.text = "";
            AddBubble(txt, true);
            OnSendMessage?.Invoke(txt);
        }

        private void Update()
        {
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
