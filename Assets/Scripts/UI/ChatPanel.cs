using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YaeSakura
{
    /// Chat panel matching the 2D version's right-side card layout.
    /// Structure: Card (dark rounded panel) containing scrollable messages + input row at bottom.
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

        // Exact 2D-version colors
        static Color CardBg      = new Color(0.051f, 0.102f, 0.149f, 0.50f);  // rgba(13,26,38,0.5)
        static Color CardBorder  = new Color(1f, 1f, 1f, 0.08f);
        static Color AssistBg    = new Color(1f, 0.784f, 0.843f, 0.30f);       // rgba(255,200,215,0.3)
        static Color UserBg      = new Color(1f, 1f, 1f, 0.12f);               // rgba(255,255,255,0.12)
        static Color AssistTxt   = new Color(1f, 0.91f, 0.933f);               // #ffe8ee
        static Color UserTxt     = new Color(0.867f, 0.867f, 0.867f);          // #ddd
        static Color ActionTxt   = new Color(1f, 0.784f, 0.824f, 0.55f);       // rgba(255,200,210,0.55)
        static Color AssistName  = new Color(1f, 0.718f, 0.773f, 0.60f);       // #ffb7c5 0.6
        static Color UserName    = new Color(0.6f, 0.6f, 0.6f, 0.50f);         // #999 0.5
        static Color TimeDiv     = new Color(1f, 1f, 1f, 0.25f);
        static Color InputBg     = new Color(1f, 1f, 1f, 0.06f);
        static Color InputBorder = new Color(1f, 1f, 1f, 0.10f);
        static Color SendBg      = new Color(1f, 0.588f, 0.667f, 0.25f);       // rgba(255,150,170,0.25)
        static Color SendTxt     = new Color(1f, 1f, 1f, 0.80f);
        static Color Placeholder = new Color(1f, 1f, 1f, 0.30f);

        private Font _font;

        void Start()
        {
            _font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14);
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateUI();
            sendButton.onClick.AddListener(SendMessage);
        }

        // ── Build UI ──────────────────────────────────────────────

        void CreateUI()
        {
            // ── Title bar (thin, top of right panel, matches 2D #title-bar) ──
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(transform, false);
            var tbRT = titleBar.AddComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0.70f, 0.92f);
            tbRT.anchorMax = new Vector2(1.0f, 1);
            tbRT.offsetMin = new Vector2(2, 0);
            tbRT.offsetMax = new Vector2(-12, -2);
            var tbBg = titleBar.AddComponent<UnityEngine.UI.Image>();
            tbBg.color = new Color(0.047f, 0.039f, 0.086f, 0.55f);

            var ttGO = new GameObject("Title");
            ttGO.transform.SetParent(titleBar.transform, false);
            var tt = ttGO.AddComponent<UnityEngine.UI.Text>();
            tt.text = "八重樱 · 圣痕之庭";
            tt.font = _font; tt.fontSize = 13; tt.fontStyle = FontStyle.Bold;
            tt.color = new Color(1f, 1f, 1f, 0.45f);
            tt.alignment = TextAnchor.MiddleCenter;
            var ttRT = ttGO.GetComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero; ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = new Vector2(8, 0); ttRT.offsetMax = new Vector2(-8, 0);

            // ── Right panel: 30% width, below title bar ──
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var pRT = panel.AddComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.70f, 0);
            pRT.anchorMax = new Vector2(1.0f, 0.92f);
            pRT.offsetMin = new Vector2(0, 6);
            pRT.offsetMax = new Vector2(-12, -4);

            // Card background
            var card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            var cRT = card.AddComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero;
            cRT.anchorMax = Vector2.one;
            cRT.offsetMin = Vector2.zero;
            cRT.offsetMax = Vector2.zero;
            var cardImg = card.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = CardBg;

            // Messages scroll area (flex:1 in the 2D version)
            var msgsGO = new GameObject("Messages");
            msgsGO.transform.SetParent(card.transform, false);
            var msgsRT = msgsGO.AddComponent<RectTransform>();
            msgsRT.anchorMin = new Vector2(0, 0.10f);
            msgsRT.anchorMax = new Vector2(1, 1);
            msgsRT.offsetMin = Vector2.zero;
            msgsRT.offsetMax = Vector2.zero;
            scrollRect = msgsGO.AddComponent<ScrollRect>();

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(msgsGO.transform, false);
            var vpRT = vp.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            vp.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            contentRoot = content.AddComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0, 1); contentRoot.anchorMax = new Vector2(1, 1);
            contentRoot.pivot = new Vector2(0.5f, 1);
            contentRoot.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 6;
            vlg.padding = new RectOffset(12, 12, 14, 0);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // ── Input row (bottom of card) ──
            var inputRow = new GameObject("InputRow");
            inputRow.transform.SetParent(card.transform, false);
            var irRT = inputRow.AddComponent<RectTransform>();
            irRT.anchorMin = new Vector2(0, 0);
            irRT.anchorMax = new Vector2(1, 0.10f);
            irRT.offsetMin = new Vector2(8, 4);
            irRT.offsetMax = new Vector2(-8, -8);

            // Input field
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(inputRow.transform, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0.1f);
            inputRT.anchorMax = new Vector2(0.80f, 0.9f);
            inputRT.sizeDelta = Vector2.zero;
            inputField = inputGO.AddComponent<InputField>();
            inputField.lineType = InputField.LineType.MultiLineNewline;
            var inBg = inputGO.AddComponent<UnityEngine.UI.Image>();
            inBg.color = InputBg;

            var tGO = new GameObject("Text");
            tGO.transform.SetParent(inputGO.transform, false);
            var tRT = tGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(14, 2); tRT.offsetMax = new Vector2(-14, -2);
            var t = tGO.AddComponent<UnityEngine.UI.Text>();
            t.font = _font; t.fontSize = 16; t.color = UserTxt;
            t.alignment = TextAnchor.MiddleLeft; t.supportRichText = false;
            inputField.textComponent = t;

            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(inputGO.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(16, 2); phRT.offsetMax = new Vector2(-16, -2);
            var ph = phGO.AddComponent<UnityEngine.UI.Text>();
            ph.text = "说点什么..."; ph.font = _font; ph.fontSize = 14;
            ph.fontStyle = FontStyle.Italic; ph.color = Placeholder;
            ph.alignment = TextAnchor.MiddleLeft;
            inputField.placeholder = ph;

            // Send button
            var btnGO = new GameObject("SendButton");
            btnGO.transform.SetParent(inputRow.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.82f, 0.1f);
            btnRT.anchorMax = new Vector2(1.0f, 0.9f);
            btnRT.sizeDelta = Vector2.zero;
            sendButton = btnGO.AddComponent<Button>();
            var btnBg = btnGO.AddComponent<UnityEngine.UI.Image>();
            btnBg.color = SendBg;

            var btnTGO = new GameObject("Text");
            btnTGO.transform.SetParent(btnGO.transform, false);
            var btnTRT = btnTGO.AddComponent<RectTransform>();
            btnTRT.anchorMin = Vector2.zero; btnTRT.anchorMax = Vector2.one;
            var btnT = btnTGO.AddComponent<UnityEngine.UI.Text>();
            btnT.text = "发送"; btnT.font = _font; btnT.fontSize = 16;
            btnT.color = SendTxt; btnT.alignment = TextAnchor.MiddleCenter;
        }

        // ── Messages ──────────────────────────────────────────────

        public void AddTimeDivider(string text)
        {
            if (contentRoot == null) return;
            var go = new GameObject("Time");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = "━━  " + text + "  ━━";
            t.font = _font; t.fontSize = 11; t.color = TimeDiv;
            t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<LayoutElement>().minWidth = 140;
            _bubbles.Add(go);
            ScrollToBottom();
        }

        public void AddActionLine(string text)
        {
            if (contentRoot == null) return;
            // Decorative: "— text —" with italic (matching 2D ::before/::after gradient lines)
            var go = new GameObject("Action");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = "— " + text + " —";
            t.font = _font; t.fontSize = 14; t.fontStyle = FontStyle.Italic;
            t.color = ActionTxt; t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<LayoutElement>().minWidth = 140;
            _bubbles.Add(go);
            ScrollToBottom();
        }

        public void AddBubble(string text, bool isUser)
        {
            if (contentRoot == null) return;

            // Container (message-container in 2D)
            var container = new GameObject(isUser ? "UserMsg" : "AssistMsg");
            container.transform.SetParent(contentRoot, false);
            var cle = container.AddComponent<LayoutElement>();
            cle.minWidth = 140;

            // Name label
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(container.transform, false);
            var nameT = nameGO.AddComponent<UnityEngine.UI.Text>();
            nameT.text = isUser ? "旅人" : "八重樱";
            nameT.font = _font; nameT.fontSize = 12;
            nameT.color = isUser ? UserName : AssistName;
            nameT.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            var nRT = nameGO.GetComponent<RectTransform>();
            nRT.anchorMin = nRT.anchorMax = isUser ? new Vector2(1, 1) : new Vector2(0, 1);
            nRT.pivot = isUser ? new Vector2(1, 1) : new Vector2(0, 1);
            nRT.sizeDelta = new Vector2(200, 13);
            nRT.anchoredPosition = isUser ? new Vector2(0, 0) : new Vector2(0, 0);

            // Bubble text with background
            var bubble = new GameObject("Bubble");
            bubble.transform.SetParent(container.transform, false);
            var bubbleRT = bubble.AddComponent<RectTransform>();

            // Background image (fills bubble behind text)
            var bgGO = new GameObject("Bg");
            bgGO.transform.SetParent(bubble.transform, false);
            var bg = bgGO.AddComponent<UnityEngine.UI.Image>();
            bg.color = isUser ? UserBg : AssistBg;
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Text (sits on top, defines the bubble size)
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(bubble.transform, false);
            var bt = textGO.AddComponent<UnityEngine.UI.Text>();
            bt.text = text;
            bt.font = _font; bt.fontSize = 16; bt.lineSpacing = 1.3f;
            bt.color = isUser ? UserTxt : AssistTxt;
            bt.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            bt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0.3f); textRT.anchorMax = new Vector2(1, 0.7f);
            textRT.offsetMin = new Vector2(10, 0); textRT.offsetMax = new Vector2(-10, 0);
            textGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Bubble sizing: driven by text's preferred height + padding
            bubble.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            float maxW = (contentRoot.rect.width > 0 ? contentRoot.rect.width : 350f) * 0.82f;
            var le = bubble.AddComponent<LayoutElement>();
            le.minWidth = 100; le.preferredWidth = maxW;

            // Anchor: user→right, assistant→left
            bubbleRT.anchorMin = bubbleRT.anchorMax = isUser ? new Vector2(1, 1) : new Vector2(0, 1);
            bubbleRT.pivot = isUser ? new Vector2(1, 1) : new Vector2(0, 1);
            bubbleRT.anchoredPosition = new Vector2(0, -15);

            _bubbles.Add(container);
            ScrollToBottom();
        }

        public UnityEngine.UI.Text BeginStream()
        {
            _isStreaming = true;
            var go = new GameObject("Stream");
            go.transform.SetParent(contentRoot, false);
            _currentStreamBubble = go.AddComponent<UnityEngine.UI.Text>();
            _currentStreamBubble.text = "";
            _currentStreamBubble.font = _font; _currentStreamBubble.fontSize = 16;
            _currentStreamBubble.color = AssistTxt;
            _currentStreamBubble.alignment = TextAnchor.MiddleLeft;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 100;
            le.preferredWidth = (contentRoot.rect.width > 0 ? contentRoot.rect.width : 350f) * 0.82f;
            _bubbles.Add(go);
            return _currentStreamBubble;
        }

        public void AppendStream(string chunk)
        {
            if (_currentStreamBubble != null) _currentStreamBubble.text += chunk;
            ScrollToBottom();
        }

        public void FinalizeStream() { _isStreaming = false; _currentStreamBubble = null; }

        public void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            if (contentRoot != null) contentRoot.anchoredPosition = new Vector2(0, 0);
        }

        public void RemoveLastBubble()
        {
            if (_bubbles.Count > 0)
            {
                var last = _bubbles[_bubbles.Count - 1];
                if (last != null) Destroy(last);
                _bubbles.RemoveAt(_bubbles.Count - 1);
            }
        }

        public void ClearAll()
        {
            foreach (var b in _bubbles) Destroy(b);
            _bubbles.Clear();
            _currentStreamBubble = null;
        }

        public void SendMessage()
        {
            if (inputField == null || _isStreaming) return;
            var txt = inputField.text.Trim();
            if (string.IsNullOrEmpty(txt)) return;
            inputField.text = "";
            AddBubble(txt, true);
            OnSendMessage?.Invoke(txt);
        }

        void Update()
        {
            if (inputField != null && inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                    SendMessage();
            }
        }
    }
}
