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

        static Color CardBg    = new Color(0.051f, 0.102f, 0.149f, 0.50f);
        static Color AssistBg  = new Color(1f, 0.784f, 0.843f, 0.30f);
        static Color UserBg    = new Color(1f, 1f, 1f, 0.12f);
        static Color AssistTxt = new Color(1f, 0.91f, 0.933f);
        static Color UserTxt   = new Color(0.867f, 0.867f, 0.867f);
        static Color ActionTxt = new Color(1f, 0.784f, 0.824f, 0.55f);
        static Color AssistName= new Color(1f, 0.718f, 0.773f, 0.60f);
        static Color UserName  = new Color(0.6f, 0.6f, 0.6f, 0.50f);
        static Color InputBg   = new Color(1f, 1f, 1f, 0.07f);
        static Color SendBg    = new Color(1f, 0.588f, 0.667f, 0.25f);
        static Color SendTxt   = new Color(1f, 1f, 1f, 0.80f);
        static Color TitleBg2  = new Color(0.047f, 0.039f, 0.086f, 0.55f);

        private Font _font;

        void Start()
        {
            _font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14);
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateUI();
            sendButton.onClick.AddListener(SendMessage);
        }

        void CreateUI()
        {
            // Right-side panel root
            var panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);
            var pRT = panel.AddComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.70f, 0); pRT.anchorMax = new Vector2(1, 1);
            pRT.offsetMin = new Vector2(8, 8); pRT.offsetMax = new Vector2(-12, -8);

            // Card fill
            var card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            var cRT = card.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0.06f); cRT.anchorMax = new Vector2(1, 0.92f);
            cRT.offsetMin = Vector2.zero; cRT.offsetMax = Vector2.zero;
            var cardImg = card.AddComponent<UnityEngine.UI.Image>();
            cardImg.color = CardBg;
            // Load rounded corner sprite (9-sliced for smooth resize)
            var roundedSprite = Resources.Load<Sprite>("rounded_card");
            if (roundedSprite != null) { cardImg.sprite = roundedSprite; cardImg.type = UnityEngine.UI.Image.Type.Sliced; }

            // ── Messages scroll ──
            var msgsGO = new GameObject("Msgs");
            msgsGO.transform.SetParent(card.transform, false);
            var msgsRT = msgsGO.AddComponent<RectTransform>();
            msgsRT.anchorMin = new Vector2(0, 0.10f); msgsRT.anchorMax = new Vector2(1, 1);
            msgsRT.offsetMin = Vector2.zero; msgsRT.offsetMax = Vector2.zero;
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
            vlg.spacing = 10;
            vlg.padding = new RectOffset(0, 0, 14, 0);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;

            // ── Input row ──
            var inputRow = new GameObject("InputRow");
            inputRow.transform.SetParent(card.transform, false);
            var irRT = inputRow.AddComponent<RectTransform>();
            irRT.anchorMin = new Vector2(0, 0); irRT.anchorMax = new Vector2(1, 0.10f);
            irRT.offsetMin = new Vector2(10, 4); irRT.offsetMax = new Vector2(-10, -6);

            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(inputRow.transform, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0.05f); inputRT.anchorMax = new Vector2(0.80f, 0.95f);
            inputRT.sizeDelta = Vector2.zero;
            inputField = inputGO.AddComponent<InputField>();
            inputField.lineType = InputField.LineType.MultiLineNewline;
            var inBg = inputGO.AddComponent<UnityEngine.UI.Image>();
            inBg.color = InputBg;

            var tGO = new GameObject("Text");
            tGO.transform.SetParent(inputGO.transform, false);
            var tRT = tGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(14, 3); tRT.offsetMax = new Vector2(-14, -3);
            var t = tGO.AddComponent<UnityEngine.UI.Text>();
            t.font = _font; t.fontSize = 16; t.color = UserTxt;
            t.alignment = TextAnchor.MiddleLeft;
            inputField.textComponent = t;

            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(inputGO.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(16, 3); phRT.offsetMax = new Vector2(-16, -3);
            var ph = phGO.AddComponent<UnityEngine.UI.Text>();
            ph.text = "说点什么..."; ph.font = _font; ph.fontSize = 14;
            ph.fontStyle = FontStyle.Italic; ph.color = new Color(1,1,1,0.3f);
            ph.alignment = TextAnchor.MiddleLeft;
            inputField.placeholder = ph;

            var btnGO = new GameObject("SendButton");
            btnGO.transform.SetParent(inputRow.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.82f, 0.05f); btnRT.anchorMax = new Vector2(1, 0.95f);
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

            // ── Title bar (thin, top of card) ──
            var tb = new GameObject("TitleBar");
            tb.transform.SetParent(panel.transform, false);
            var tbRT = tb.AddComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 0.92f); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.offsetMin = Vector2.zero; tbRT.offsetMax = Vector2.zero;
            var tbBg = tb.AddComponent<UnityEngine.UI.Image>();
            tbBg.color = TitleBg2;
            var tt = new GameObject("Title");
            tt.transform.SetParent(tb.transform, false);
            var ttT = tt.AddComponent<UnityEngine.UI.Text>();
            ttT.text = "八重樱 · 圣痕之庭"; ttT.font = _font;
            ttT.fontSize = 14; ttT.fontStyle = FontStyle.Bold;
            ttT.color = new Color(1,1,1,0.45f); ttT.alignment = TextAnchor.MiddleCenter;
            var ttRT = tt.GetComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero; ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = Vector2.zero; ttRT.offsetMax = Vector2.zero;
        }

        // ── Bubble methods ──

        public void AddBubble(string text, bool isUser)
        {
            if (contentRoot == null) return;

            // Container row: wraps name + bubble
            var row = new GameObject(isUser ? "UserRow" : "AssistRow");
            row.transform.SetParent(contentRoot, false);
            var rLE = row.AddComponent<LayoutElement>();
            rLE.minWidth = 100;
            rLE.preferredHeight = 24; // will be overridden by content

            // Name label
            var label = new GameObject("Label");
            label.transform.SetParent(row.transform, false);
            var lt = label.AddComponent<UnityEngine.UI.Text>();
            lt.text = isUser ? "旅人" : "八重樱";
            lt.font = _font; lt.fontSize = 12;
            lt.color = isUser ? UserName : AssistName;
            lt.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            var lRT = label.GetComponent<RectTransform>();
            lRT.anchorMin = lRT.anchorMax = isUser ? new Vector2(1,1) : new Vector2(0,1);
            lRT.pivot = isUser ? new Vector2(1,1) : new Vector2(0,1);
            lRT.sizeDelta = new Vector2(200, 14);
            lRT.anchoredPosition = Vector2.zero;

            // Bubble: bg + text
            var bubble = new GameObject("Bubble");
            bubble.transform.SetParent(row.transform, false);
            var bRT = bubble.AddComponent<RectTransform>();
            bubble.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bg = new GameObject("Bg");
            bg.transform.SetParent(bubble.transform, false);
            var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = isUser ? UserBg : AssistBg;
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            var txt = new GameObject("Txt");
            txt.transform.SetParent(bubble.transform, false);
            var bt = txt.AddComponent<UnityEngine.UI.Text>();
            bt.text = text;
            bt.font = _font; bt.fontSize = 16; bt.lineSpacing = 1.3f;
            bt.color = isUser ? UserTxt : AssistTxt;
            bt.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            bt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tRT = txt.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 0.15f); tRT.anchorMax = new Vector2(1, 0.85f);
            tRT.offsetMin = new Vector2(14, 0); tRT.offsetMax = new Vector2(-14, 0);
            txt.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float maxW = (contentRoot.rect.width > 0 ? contentRoot.rect.width : 350f) * 0.85f;
            var le = bubble.AddComponent<LayoutElement>();
            le.minWidth = 100; le.preferredWidth = maxW;

            bRT.anchorMin = bRT.anchorMax = isUser ? new Vector2(1,1) : new Vector2(0,1);
            bRT.pivot = isUser ? new Vector2(1,1) : new Vector2(0,1);
            bRT.anchoredPosition = new Vector2(0, -16);

            _bubbles.Add(row);
            ScrollToBottom();
        }

        public void AddActionLine(string text)
        {
            if (contentRoot == null) return;
            var go = new GameObject("Action");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = "— " + text + " —";
            t.font = _font; t.fontSize = 13; t.fontStyle = FontStyle.Italic;
            t.color = ActionTxt; t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<LayoutElement>().minWidth = 140;
            _bubbles.Add(go);
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
            le.preferredWidth = (contentRoot.rect.width > 0 ? contentRoot.rect.width : 350f) * 0.85f;
            _bubbles.Add(go);
            return _currentStreamBubble;
        }

        public void AppendStream(string chunk) { if (_currentStreamBubble != null) _currentStreamBubble.text += chunk; ScrollToBottom(); }
        public void FinalizeStream() { _isStreaming = false; _currentStreamBubble = null; }
        public void RemoveLastBubble() { if (_bubbles.Count > 0) { var l = _bubbles[_bubbles.Count-1]; if (l) Destroy(l); _bubbles.RemoveAt(_bubbles.Count-1); } }

        void ScrollToBottom() { Canvas.ForceUpdateCanvases(); if (contentRoot != null) contentRoot.anchoredPosition = new Vector2(0, 0); }

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
