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
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0.1f);
            scrollRT.anchorMax = new Vector2(1, 0.92f);
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
            vlg.spacing = 6;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // InputField
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(transform, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0); inputRT.anchorMax = new Vector2(0.78f, 0.08f);
            inputRT.offsetMin = new Vector2(8, 4); inputRT.offsetMax = new Vector2(-4, -4);
            inputField = inputGO.AddComponent<InputField>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 4); textRT.offsetMax = new Vector2(-8, -4);
            var text = textGO.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = new Color(0.85f, 0.85f, 0.9f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            inputField.textComponent = text;
            inputField.lineType = InputField.LineType.MultiLineNewline;

            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(inputGO.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(8, 4); phRT.offsetMax = new Vector2(-8, -4);
            var ph = phGO.AddComponent<UnityEngine.UI.Text>();
            ph.text = "输入消息...";
            ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ph.fontSize = 16;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(0.5f, 0.5f, 0.55f);
            ph.alignment = TextAnchor.MiddleLeft;
            inputField.placeholder = ph;

            // Send Button
            var btnGO = new GameObject("SendButton");
            btnGO.transform.SetParent(transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.8f, 0); btnRT.anchorMax = new Vector2(1, 0.08f);
            btnRT.offsetMin = new Vector2(2, 4); btnRT.offsetMax = new Vector2(-8, -4);
            sendButton = btnGO.AddComponent<Button>();

            var btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = btnTextGO.AddComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;
            var btnText = btnTextGO.AddComponent<UnityEngine.UI.Text>();
            btnText.text = "发送";
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 16;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
        }

        public void AddBubble(string text, bool isUser)
        {
            if (contentRoot == null) return;
            var go = new GameObject("Bubble");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 16;
            t.color = isUser ? userTextColor : charTextColor;
            t.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout = go.AddComponent<LayoutElement>();
            layout.minWidth = 200;
            layout.preferredWidth = contentRoot.rect.width * 0.7f;

            _bubbles.Add(go);
            ScrollToBottom();
        }

        public void AddActionLine(string text)
        {
            var go = new GameObject("ActionLine");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 14;
            t.fontStyle = FontStyle.Italic;
            t.color = actionLineColor;
            t.alignment = TextAnchor.MiddleCenter;
            var fitter2 = go.AddComponent<ContentSizeFitter>();
            fitter2.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout2 = go.AddComponent<LayoutElement>();
            layout2.minWidth = 200;
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
            _currentStreamBubble.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _currentStreamBubble.fontSize = 16;
            _currentStreamBubble.color = charTextColor;
            _currentStreamBubble.alignment = TextAnchor.MiddleLeft;
            var fitter3 = go.AddComponent<ContentSizeFitter>();
            fitter3.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter3.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var layout3 = go.AddComponent<LayoutElement>();
            layout3.minWidth = 200;
            layout3.preferredWidth = contentRoot.rect.width * 0.7f;
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
            if (inputField == null) return;
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
