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

        // 2D-version color scheme
        private static Color ColCardBg    = new Color(0.051f, 0.102f, 0.149f, 0.65f);
        private static Color ColAssistBg  = new Color(1f, 0.784f, 0.843f, 0.22f);
        private static Color ColUserBg    = new Color(1f, 1f, 1f, 0.10f);
        private static Color ColAssistTxt = new Color(1f, 0.910f, 0.933f);
        private static Color ColUserTxt   = new Color(0.87f, 0.87f, 0.87f);
        private static Color ColAction    = new Color(1f, 0.80f, 0.82f, 0.55f);
        private static Color ColSendBtn   = new Color(1f, 0.588f, 0.667f, 0.25f);
        private static Color ColTitleBg   = new Color(0.055f, 0.045f, 0.095f, 0.65f);
        private static Color ColTitleTxt  = new Color(1f, 1f, 1f, 0.50f);
        private static Color ColInputBg   = new Color(1f, 1f, 1f, 0.07f);
        private static Color ColTimeDiv   = new Color(1f, 1f, 1f, 0.25f);
        private static Color ColAssistantName = new Color(1f, 0.718f, 0.773f, 0.65f);
        private static Color ColUserName  = new Color(0.6f, 0.6f, 0.6f, 0.5f);

        private Font _font;
        private float _cardW; // cached container width

        private void Start()
        {
            _font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14);
            if (_font == null)
                _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (_font == null)
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
            titleRT.anchorMin = new Vector2(0.7f, 0.92f);
            titleRT.anchorMax = new Vector2(1f, 1);
            titleRT.offsetMin = new Vector2(2, 0);
            titleRT.offsetMax = new Vector2(-2, 0);
            var titleBg = titleGO.AddComponent<UnityEngine.UI.Image>();
            titleBg.color = ColTitleBg;

            var titleTGO = new GameObject("Title");
            titleTGO.transform.SetParent(titleGO.transform, false);
            var titleT = titleTGO.AddComponent<UnityEngine.UI.Text>();
            titleT.text = "八重樱 · 圣痕之庭";
            titleT.font = _font;
            titleT.fontSize = 14;
            titleT.fontStyle = FontStyle.Bold;
            titleT.color = ColTitleTxt;
            titleT.alignment = TextAnchor.MiddleCenter;
            var tTRT = titleTGO.GetComponent<RectTransform>();
            tTRT.anchorMin = Vector2.zero; tTRT.anchorMax = Vector2.one;
            tTRT.offsetMin = new Vector2(10, 0); tTRT.offsetMax = new Vector2(-10, 0);

            // === Card Container ===
            var card = new GameObject("Card");
            card.transform.SetParent(transform, false);
            var cardRT = card.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.7f, 0.06f);
            cardRT.anchorMax = new Vector2(1f, 0.92f);
            cardRT.offsetMin = new Vector2(4, 0);
            cardRT.offsetMax = new Vector2(-8, -6);
            var cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = ColCardBg;

            // === ScrollView ===
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(card.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0.10f);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;
            scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGO.transform, false);
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
            vlg.spacing = 8;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // === Separator line ===
            var sep = new GameObject("Separator");
            sep.transform.SetParent(card.transform, false);
            var sepRT = sep.AddComponent<RectTransform>();
            sepRT.anchorMin = new Vector2(0, 0.10f);
            sepRT.anchorMax = new Vector2(1, 0.10f);
            sepRT.sizeDelta = new Vector2(0, 1);
            var sepImg = sep.AddComponent<UnityEngine.UI.Image>();
            sepImg.color = new Color(1f, 1f, 1f, 0.06f);

            // === Input area ===
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(card.transform, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0.01f);
            inputRT.anchorMax = new Vector2(0.78f, 0.10f);
            inputRT.offsetMin = new Vector2(8, 1); inputRT.offsetMax = new Vector2(-3, -1);
            inputField = inputGO.AddComponent<InputField>();
            var inputBg = inputGO.AddComponent<UnityEngine.UI.Image>();
            inputBg.color = ColInputBg;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 3); textRT.offsetMax = new Vector2(-10, -3);
            var text = textGO.AddComponent<UnityEngine.UI.Text>();
            text.font = _font; text.fontSize = 15;
            text.color = ColUserTxt; text.alignment = TextAnchor.MiddleLeft;
            inputField.textComponent = text;
            inputField.lineType = InputField.LineType.MultiLineNewline;

            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(inputGO.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(12, 3); phRT.offsetMax = new Vector2(-12, -3);
            var ph = phGO.AddComponent<UnityEngine.UI.Text>();
            ph.text = "说点什么...";
            ph.font = _font; ph.fontSize = 14;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1f, 1f, 1f, 0.25f);
            ph.alignment = TextAnchor.MiddleLeft;
            inputField.placeholder = ph;

            // === Send Button ===
            var btnGO = new GameObject("SendButton");
            btnGO.transform.SetParent(card.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.80f, 0.01f);
            btnRT.anchorMax = new Vector2(0.97f, 0.10f);
            btnRT.offsetMin = new Vector2(3, 1); btnRT.offsetMax = new Vector2(-6, -1);
            sendButton = btnGO.AddComponent<Button>();
            var btnBg = btnGO.AddComponent<UnityEngine.UI.Image>();
            btnBg.color = ColSendBtn;

            var btnTGO = new GameObject("Text");
            btnTGO.transform.SetParent(btnGO.transform, false);
            var btnTRT = btnTGO.AddComponent<RectTransform>();
            btnTRT.anchorMin = Vector2.zero; btnTRT.anchorMax = Vector2.one;
            var btnT = btnTGO.AddComponent<UnityEngine.UI.Text>();
            btnT.text = "发送";
            btnT.font = _font; btnT.fontSize = 15;
            btnT.color = new Color(1f, 1f, 1f, 0.8f);
            btnT.alignment = TextAnchor.MiddleCenter;
        }

        // ---- Public methods ----

        public void AddBubble(string text, bool isUser)
        {
            if (contentRoot == null) return;

            // Wrapper with horizontal layout for alignment
            var wrapper = new GameObject("Row");
            wrapper.transform.SetParent(contentRoot, false);
            var wLE = wrapper.AddComponent<LayoutElement>();
            wLE.minWidth = 100;

            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(wrapper.transform, false);
            var nameT = nameGO.AddComponent<UnityEngine.UI.Text>();
            nameT.text = isUser ? "旅人" : "八重樱";
            nameT.font = _font; nameT.fontSize = 11;
            nameT.color = isUser ? ColUserName : ColAssistantName;
            nameT.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            var nRT = nameGO.GetComponent<RectTransform>();
            nRT.anchorMin = isUser ? new Vector2(1,1) : new Vector2(0,1);
            nRT.anchorMax = isUser ? new Vector2(1,1) : new Vector2(0,1);
            nRT.pivot = isUser ? new Vector2(1,1) : new Vector2(0,1);
            nRT.sizeDelta = new Vector2(200, 12);
            nRT.anchoredPosition = isUser ? new Vector2(0, 0) : new Vector2(0, 0);

            var go = new GameObject("Bubble");
            go.transform.SetParent(wrapper.transform, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.font = _font; t.fontSize = 15;
            t.color = isUser ? ColUserTxt : ColAssistTxt;
            t.alignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float maxW = (contentRoot.rect.width > 0 ? contentRoot.rect.width : 350f) * 0.80f;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 140; le.preferredWidth = maxW;

            var bRT = go.GetComponent<RectTransform>();
            if (isUser)
            {
                bRT.anchorMin = bRT.anchorMax = new Vector2(1, 1);
                bRT.pivot = new Vector2(1, 1);
                bRT.anchoredPosition = new Vector2(0, -14);
            }
            else
            {
                bRT.anchorMin = bRT.anchorMax = new Vector2(0, 1);
                bRT.pivot = new Vector2(0, 1);
                bRT.anchoredPosition = new Vector2(0, -14);
            }

            var bg = go.AddComponent<UnityEngine.UI.Image>();
            if (bg != null) bg.color = isUser ? ColUserBg : ColAssistBg;

            _bubbles.Add(wrapper);
            ScrollToBottom();
        }

        public void AddActionLine(string text)
        {
            if (contentRoot == null) return;
            var go = new GameObject("Action");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.font = _font; t.fontSize = 13;
            t.fontStyle = FontStyle.Italic;
            t.color = ColAction; t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.AddComponent<LayoutElement>().minWidth = 140;
            _bubbles.Add(go);
            ScrollToBottom();
        }

        public void AddTimeDivider(string text)
        {
            if (contentRoot == null) return;
            var go = new GameObject("Time");
            go.transform.SetParent(contentRoot, false);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = "━━ " + text + " ━━";
            t.font = _font; t.fontSize = 11;
            t.color = ColTimeDiv; t.alignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
            _currentStreamBubble.font = _font; _currentStreamBubble.fontSize = 15;
            _currentStreamBubble.color = ColAssistTxt;
            _currentStreamBubble.alignment = TextAnchor.MiddleLeft;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 140;
            le.preferredWidth = (contentRoot.rect.width > 0 ? contentRoot.rect.width : 350f) * 0.80f;
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
