using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BouncyUFO
{
    /// <summary>
    /// 닉네임 정하기 창. 처음 게임을 켤 때(타이틀 → 로비 전) 한 번, 그리고 설정의 "닉네임 바꾸기"에서 언제든.
    /// 폰: 입력 칸을 누르면 폰 자판(한글·일본어 입력기 포함)이 뜬다.
    /// PC(에디터): 키보드로 바로 친다. 이 창이 열려 있는 동안만 입력기(IME)를 켜서 한/영 전환·한글 조합도 된다.
    /// "붙여넣기" 버튼(클립보드 글자를 붙임), PC에서는 Ctrl+V도. 폰 자판의 입력 칸에서는 길게 눌러 복사·붙여넣기도 된다.
    /// 규칙은 Nickname.Check. 처음 정할 때는 취소가 없다.
    /// </summary>
    public sealed class NicknamePanel : ModalPanel
    {
        private TextMeshPro field;
        private TextMeshPro message;
        private PixelButton fieldButton;
        private PixelButton okButton;
        private PixelButton cancelButton;
        private string text = string.Empty;
        private string errorKey;
        private TouchScreenKeyboard keyboard;
        private Action<string> onDone;
        private bool editorTyping;
        private string composing = string.Empty; // PC 입력기에서 조합 중인 글자(예: 한글 "ㅎ" → "하")

        public static NicknamePanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("NicknamePanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<NicknamePanel>();
            p.Build(new Vector2(172f, 132f), "nick.title", sortingOrder, false);
            p.BuildContent();
            return p;
        }

        private void BuildContent()
        {
            // 입력 칸(누르면 자판)
            fieldButton = CreateButton("Field", new Vector2(-24f, 22f), new Vector2(106f, 20f), null, new Color(0.03f, 0.04f, 0.1f));
            fieldButton.Clicked += OpenKeyboard;
            field = CreateText("Text", new Vector2(-24f, 22f), GameText.Style.Title);
            var paste = CreateButton("Paste", new Vector2(56f, 22f), new Vector2(44f, 20f), "nick.paste", new Color(0.3f, 0.4f, 0.7f), GameText.Style.Body);
            paste.Clicked += Paste;
            field.GetComponent<MeshRenderer>().sortingOrder = Order + 9;

            message = CreateText("Message", new Vector2(0f, -6f), GameText.Style.Body, TextAlignmentOptions.Center, 150f, true);

            okButton = CreateButton("Ok", new Vector2(-34f, -44f), new Vector2(58f, 20f), "ui.ok", new Color(0.3f, 0.75f, 0.45f));
            okButton.Clicked += Confirm;
            cancelButton = CreateButton("Cancel", new Vector2(34f, -44f), new Vector2(58f, 20f), "ui.cancel", new Color(0.45f, 0.5f, 0.6f));
            cancelButton.Clicked += Close;
        }

        /// <summary>창을 연다. canCancel이 false면(처음 정할 때) 확인으로만 닫힌다.</summary>
        public void Ask(bool canCancel, Action<string> done)
        {
            onDone = done;
            text = Nickname.Current;
            errorKey = null;
            cancelButton.gameObject.SetActive(canCancel);
            okButton.transform.localPosition = new Vector3(U(canCancel ? -34f : 0f), U(-44f), 0f);
            Open();
            Refresh();
            OpenKeyboard();
        }

        protected override void OnOpened()
        {
            var kb = Keyboard.current;
            if (kb != null && !TouchScreenKeyboard.isSupported)
            {
                kb.onTextInput += OnEditorText;
                kb.onIMECompositionChange += OnComposition;
                kb.SetIMEEnabled(true); // 한/영 키·한글 조합이 되게
                var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
                if (cam != null) kb.SetIMECursorPosition(cam.WorldToScreenPoint(field.transform.position));
                editorTyping = true;
            }
        }

        protected override void OnClosed()
        {
            if (editorTyping && Keyboard.current != null)
            {
                Keyboard.current.onTextInput -= OnEditorText;
                Keyboard.current.onIMECompositionChange -= OnComposition;
                Keyboard.current.SetIMEEnabled(false);
            }
            editorTyping = false;
            composing = string.Empty;
            if (keyboard != null) keyboard.active = false;
            keyboard = null;
        }

        private void OpenKeyboard()
        {
            if (!TouchScreenKeyboard.isSupported) return;
            keyboard = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.Default, false, false, false, false, "", Nickname.InputLimit);
        }

        private void OnComposition(IMECompositionString s) => composing = s.ToString();

        /// <summary>PC(에디터)에서 키보드로 친 글자(입력기로 조합이 끝난 글자 포함).</summary>
        private void OnEditorText(char c)
        {
            if (!IsOpen) return;
            if (c == '\b') { if (text.Length > 0) text = text.Substring(0, text.Length - 1); }
            else if (c == '\r' || c == '\n') { Confirm(); return; }
            else if (!char.IsControl(c) && text.Length < Nickname.InputLimit && !(Keyboard.current != null && Keyboard.current.ctrlKey.isPressed)) text += c;
            errorKey = null;
            Refresh();
        }

        /// <summary>클립보드 글자를 뒤에 붙인다(줄바꿈은 빼고, 입력 칸 최대 글자 수까지).</summary>
        private void Paste()
        {
            string clip = GUIUtility.systemCopyBuffer ?? string.Empty;
            clip = clip.Replace("\r", "").Replace("\n", "").Trim();
            if (clip.Length == 0) return;
            text += clip;
            if (text.Length > Nickname.InputLimit) text = text.Substring(0, Nickname.InputLimit);
            if (keyboard != null) keyboard.text = text; // 열려 있는 폰 자판에도 맞춘다
            errorKey = null;
            Refresh();
        }

        private void Update()
        {
            if (!IsOpen) return;
            // PC: Ctrl+V
            var kb = Keyboard.current;
            if (editorTyping && kb != null && kb.ctrlKey.isPressed && kb.vKey.wasPressedThisFrame) Paste();
            if (keyboard != null)
            {
                string t = (keyboard.text ?? string.Empty).Replace("\n", "").Replace("\r", "");
                if (t.Length > Nickname.InputLimit) t = t.Substring(0, Nickname.InputLimit);
                if (t != text) { text = t; errorKey = null; Refresh(); }
                if (keyboard.status == TouchScreenKeyboard.Status.Done) { keyboard = null; Confirm(); return; }
                if (keyboard.status == TouchScreenKeyboard.Status.Canceled || keyboard.status == TouchScreenKeyboard.Status.LostFocus) keyboard = null;
            }
            // 깜빡이는 커서
            bool caret = Mathf.PingPong(Time.unscaledTime * 2f, 1f) > 0.5f;
            GameText.SetPlain(field, text + composing + (caret ? "_" : " "));
        }

        private void Confirm()
        {
            if (!IsOpen) return;
            errorKey = Nickname.Check(text);
            if (errorKey != null) { Refresh(); Sound.Play(Sfx.Hit, 0.8f); return; }
            SaveSystem.Data.nickname = text;
            SaveSystem.Save();
            var done = onDone;
            Close();
            done?.Invoke(text);
        }

        private void Refresh()
        {
            // 안내: 잘못됐으면 빨간 이유, 아니면 규칙
            GameText.SetKey(message, errorKey ?? "nick.rules");
            message.color = errorKey != null ? new Color(1f, 0.45f, 0.45f) : new Color(0.75f, 0.8f, 1f);
        }
    }
}
