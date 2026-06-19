using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace YaeSakura
{
    /// Chat panel rebuilt with UI Toolkit — matches 2D CSS layout exactly.
    /// Structure: right panel (30%) with card containing scrollable messages + input row.
    public class ChatPanel : MonoBehaviour
    {
        public System.Action<string> OnSendMessage;

        // Root elements
        private VisualElement _right;
        private VisualElement _card;
        private ScrollView _msgs;
        private TextField _input;
        private Button _sendBtn;

        private Label _streamLabel;
        private bool _isStreaming;
        public bool IsStreaming => _isStreaming;

        // Stored for removal on completion
        private VisualElement _streamElement;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) doc = gameObject.AddComponent<UIDocument>();
            BuildUI(doc.rootVisualElement);
        }

        void BuildUI(VisualElement root)
        {
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Row;
            root.style.position = Position.Absolute;
            root.style.top = 0; root.style.left = 0; root.style.right = 0; root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore; // clicks pass through to children

            // ── Right panel ──
            _right = new VisualElement();
            _right.style.width = Length.Percent(30);
            _right.style.position = Position.Absolute;
            _right.style.top = 0; _right.style.bottom = 0; _right.style.right = 0;
            _right.style.flexDirection = FlexDirection.Column;
            _right.style.paddingTop = 8; _right.style.paddingBottom = 8;
            _right.style.paddingRight = 12;
            _right.pickingMode = PickingMode.Position;
            root.Add(_right);

            // Title bar
            var titleBar = new VisualElement();
            titleBar.style.height = 36;
            titleBar.style.backgroundColor = new Color(0.047f, 0.039f, 0.086f, 0.55f);
            titleBar.style.flexDirection = FlexDirection.Row;
            titleBar.style.alignItems = Align.Center;
            titleBar.style.justifyContent = Justify.Center;
            var title = new Label("八重樱 · 圣痕之庭");
            title.style.fontSize = 14;
            title.style.color = new Color(1, 1, 1, 0.5f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleBar.Add(title);
            _right.Add(titleBar);

            // Card — matches .card in CSS: bg, rounded, flex
            _card = new VisualElement();
            _card.style.flexGrow = 1;
            _card.style.backgroundColor = new Color(0.051f, 0.102f, 0.149f, 0.50f);
            _card.style.borderTopLeftRadius = _card.style.borderTopRightRadius = 16;
            _card.style.borderBottomLeftRadius = _card.style.borderBottomRightRadius = 16;
            _card.style.borderTopWidth = _card.style.borderRightWidth =
                _card.style.borderBottomWidth = _card.style.borderLeftWidth = 1;
            _card.style.borderTopColor = _card.style.borderRightColor =
                _card.style.borderBottomColor = _card.style.borderLeftColor = new Color(1, 1, 1, 0.08f);
            _card.style.flexDirection = FlexDirection.Column;
            _card.style.overflow = Overflow.Hidden;
            _right.Add(_card);

            // ── Messages scroll ──
            _msgs = new ScrollView(ScrollViewMode.Vertical);
            _msgs.style.flexGrow = 1;
            _msgs.style.paddingTop = 14; _msgs.style.paddingBottom = 0;
            _msgs.style.paddingLeft = 12; _msgs.style.paddingRight = 12;
            _msgs.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _msgs.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _msgs.contentContainer.style.flexDirection = FlexDirection.Column;
            _msgs.contentContainer.style.alignItems = Align.Stretch;
            _card.Add(_msgs);

            // ── Input row ──
            var inputRow = new VisualElement();
            inputRow.style.flexDirection = FlexDirection.Row;
            inputRow.style.paddingTop = 8; inputRow.style.paddingBottom = 10;
            inputRow.style.paddingLeft = 10; inputRow.style.paddingRight = 10;
            inputRow.style.minHeight = 52;

            _input = new TextField();
            _input.style.flexGrow = 1;
            _input.style.backgroundColor = new Color(1, 1, 1, 0.07f);
            _input.style.borderTopLeftRadius = _input.style.borderBottomLeftRadius = 20;
            _input.style.borderTopRightRadius = _input.style.borderBottomRightRadius = 20;
            _input.style.borderTopWidth = _input.style.borderRightWidth =
                _input.style.borderBottomWidth = _input.style.borderLeftWidth = 1;
            _input.style.borderTopColor = _input.style.borderRightColor =
                _input.style.borderBottomColor = _input.style.borderLeftColor = new Color(1, 1, 1, 0.10f);
            _input.style.paddingLeft = 14; _input.style.paddingRight = 14;
            _input.style.color = new Color(0.867f, 0.867f, 0.867f);
            _input.style.fontSize = 16;
            _input.multiline = true;
            _input.RegisterCallback<KeyDownEvent>(OnInputKey);
            inputRow.Add(_input);

            _sendBtn = new Button(SendMessage) { text = "发送" };
            _sendBtn.style.backgroundColor = new Color(1, 0.588f, 0.667f, 0.25f);
            _sendBtn.style.color = new Color(1, 1, 1, 0.8f);
            _sendBtn.style.fontSize = 16;
            _sendBtn.style.borderTopLeftRadius = _sendBtn.style.borderBottomLeftRadius = 0;
            _sendBtn.style.borderTopRightRadius = _sendBtn.style.borderBottomRightRadius = 18;
            _sendBtn.style.marginLeft = 8;
            _sendBtn.style.paddingLeft = 16; _sendBtn.style.paddingRight = 16;
            _sendBtn.style.minWidth = 64;
            inputRow.Add(_sendBtn);

            _card.Add(inputRow);
        }

        void OnInputKey(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
            {
                evt.PreventDefault();
                evt.StopPropagation();
                // Delay to avoid race with UI Toolkit text insertion
                _input.schedule.Execute(() => SendMessage());
            }
        }

        // ── Public API (same as before) ──

        public void AddBubble(string text, bool isUser)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.marginTop = 3; container.style.marginBottom = 3;
            container.style.alignItems = isUser ? Align.FlexEnd : Align.FlexStart;

            // Name
            var name = new Label(isUser ? "旅人" : "八重樱");
            name.style.fontSize = 12;
            name.style.color = isUser
                ? new Color(0.6f, 0.6f, 0.6f, 0.5f)
                : new Color(1, 0.718f, 0.773f, 0.6f);
            name.style.marginBottom = 2;
            name.style.unityTextAlign = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            name.style.paddingRight = isUser ? 6 : 0;
            name.style.paddingLeft = isUser ? 0 : 6;
            container.Add(name);

            // Bubble
            var bubble = new VisualElement();
            bubble.style.flexDirection = FlexDirection.Row;
            bubble.style.maxWidth = Length.Percent(82);
            bubble.style.backgroundColor = isUser
                ? new Color(1, 1, 1, 0.12f)
                : new Color(1, 0.784f, 0.843f, 0.30f);
            bubble.style.borderTopLeftRadius = bubble.style.borderTopRightRadius = 14;
            bubble.style.borderBottomLeftRadius = bubble.style.borderBottomRightRadius = 14;
            // Tail: one corner at 4px
            if (isUser) bubble.style.borderBottomRightRadius = 4;
            else bubble.style.borderBottomLeftRadius = 4;

            var bubbleText = new Label(text);
            bubbleText.style.fontSize = 16;
            bubbleText.style.color = isUser
                ? new Color(0.867f, 0.867f, 0.867f)
                : new Color(1, 0.91f, 0.933f);
            bubbleText.style.paddingTop = 10; bubbleText.style.paddingBottom = 10;
            bubbleText.style.paddingLeft = 14; bubbleText.style.paddingRight = 14;
            bubbleText.style.whiteSpace = WhiteSpace.Normal;
            bubbleText.style.unityTextAlign = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            bubble.Add(bubbleText);
            container.Add(bubble);

            _msgs.contentContainer.Add(container);
            ScrollToBottom();
        }

        public void AddActionLine(string text)
        {
            var action = new Label("— " + text + " —");
            action.style.fontSize = 13;
            action.style.color = new Color(1, 0.784f, 0.824f, 0.55f);
            action.style.unityFontStyleAndWeight = FontStyle.Italic;
            action.style.unityTextAlign = TextAnchor.MiddleCenter;
            action.style.marginTop = 8; action.style.marginBottom = 8;
            action.style.paddingLeft = 20; action.style.paddingRight = 20;
            _msgs.contentContainer.Add(action);
            ScrollToBottom();
        }

        public void AddTimeDivider(string text)
        {
            var div = new Label("━━  " + text + "  ━━");
            div.style.fontSize = 11;
            div.style.color = new Color(1, 1, 1, 0.25f);
            div.style.unityTextAlign = TextAnchor.MiddleCenter;
            div.style.marginTop = 12; div.style.marginBottom = 8;
            _msgs.contentContainer.Add(div);
            ScrollToBottom();
        }

        public Label BeginStream()
        {
            _isStreaming = true;
            var container = new VisualElement();
            container.style.alignItems = Align.FlexStart;
            container.style.maxWidth = Length.Percent(82);

            _streamLabel = new Label("");
            _streamLabel.style.fontSize = 16;
            _streamLabel.style.color = new Color(1, 0.91f, 0.933f);
            _streamLabel.style.whiteSpace = WhiteSpace.Normal;
            _streamLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            container.Add(_streamLabel);
            _msgs.contentContainer.Add(container);
            _streamElement = container;
            return _streamLabel;
        }

        public void AppendStream(string chunk)
        {
            if (_streamLabel != null) _streamLabel.text += chunk;
            ScrollToBottom();
        }

        public void FinalizeStream() { _isStreaming = false; _streamLabel = null; }

        public void RemoveLastBubble()
        {
            if (_streamElement != null)
            {
                _streamElement.RemoveFromHierarchy();
                _streamElement = null;
            }
        }

        public void ScrollToBottom()
        {
            _msgs?.schedule.Execute(() =>
            {
                if (_msgs.verticalScroller != null)
                    _msgs.verticalScroller.value = _msgs.contentContainer.layout.height;
            }).StartingIn(50);
        }

        public void SendMessage()
        {
            if (_isStreaming) return;
            var txt = _input.value.Trim();
            if (string.IsNullOrEmpty(txt)) return;
            _input.value = "";
            AddBubble(txt, true);
            OnSendMessage?.Invoke(txt);
        }
    }
}
