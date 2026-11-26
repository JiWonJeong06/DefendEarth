using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BouncyUFO
{
    /// <summary>
    /// 닉네임 정하기 창. 처음 게임을 켤 때(타이틀 → 로비 전) 한 번, 그리고 설정·프로필의 "바꾸기"에서 언제든.
    /// "적용하기"를 눌러야 저장된다(11/27). 붙여넣기 버튼은 없앴다.
    /// 입력 칸에는 세로 막대 커서(|)가 깜빡이고, 칸을 누른 자리로 커서가 옮겨 가서 그 자리에 쓰고 지운다.
    /// 폰: 칸을 누르면 폰 자판(한글·일본어 입력기)과 폰의 입력 칸이 뜨고, 커서 위치는 폰 자판을 따른다.
    /// PC(에디터): 키보드로 바로 친다. ←→ Home End로 커서 이동, Backspace·Delete로 지우기, Enter = 적용.
    /// 이 창이 열려 있는 동안만 입력기(IME)를 켜서 한/영 전환·한글 조합도 된다.
    /// </summary>
    public sealed class NicknamePanel : ModalPanel
    {
        private const float FieldWidthPx = 140f;
        private const float FieldY = 22f;

        private TextMeshPro field;
        private TextMeshPro message;
        private SpriteRenderer caretBar;
        private PixelButton fieldButton;
        private PixelButton applyButton;
        private PixelButton cancelButton;
        private string text = string.Empty;
        private int caret;
        private float caretMovedAt;
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
            var a = GameAssets.Instance;
            // 입력 칸(누르면 그 자리로 커서, 폰은 자판)
            fieldButton = CreateButton("Field", new Vector2(0f, FieldY), new Vector2(FieldWidthPx + 8f, 20f), null, new Color(0.03f, 0.04f, 0.1f));
            fieldButton.Clicked += OnFieldClicked;
            field = CreateText("Text", new Vector2(0f, FieldY), GameText.Style.Title, TextAlignmentOptions.MidlineLeft, FieldWidthPx);
            field.GetComponent<MeshRenderer>().sortingOrder = Order + 9;

            caretBar = CreateSprite("Caret", a.pixelSprite, new Vector2(0f, FieldY), 10);
            float px = a.pixelSprite.bounds.size.x;
            caretBar.transform.localScale = new Vector3(U(1f) / px, U(12f) / px, 1f);

            message = CreateText("Message", new Vector2(0f, -4f), GameText.Style.Body, TextAlignmentOptions.Center, 150f, true);

            applyButton = CreateButton("Apply", new Vector2(-34f, -44f), new Vector2(58f, 20f), "nick.apply", new Color(0.3f, 0.75f, 0.45f), GameText.Style.Body);
            applyButton.Clicked += Apply;
            cancelButton = CreateButton("Cancel", new Vector2(34f, -44f), new Vector2(58f, 20f), "ui.cancel", new Color(0.45f, 0.5f, 0.6f), GameText.Style.Body);
            cancelButton.Clicked += Close;
        }

        /// <summary>창을 연다. canCancel이 false면(처음 정할 때) 적용하기로만 닫힌다.</summary>
        public void Ask(bool canCancel, Action<string> done)
        {
            onDone = done;
            text = Nickname.Current;
            caret = text.Length;
            errorKey = null;
            cancelButton.gameObject.SetActive(canCancel);
            applyButton.transform.localPosition = new Vector3(U(canCancel ? -34f : 0f), U(-44f), 0f);
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

        /// <summary>칸을 누르면: PC는 누른 자리로 커서, 폰은 자판을 다시 연다.</summary>
        private void OnFieldClicked()
        {
            if (TouchScreenKeyboard.isSupported) { if (keyboard == null || !keyboard.active) OpenKeyboard(); return; }
            var input = TapInput.Main;
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (input == null || cam == null) return;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(input.Position.x, input.Position.y, -cam.transform.position.z));
            SetCaret(IndexAt(world.x));
        }

        /// <summary>월드 x 위치에서 가장 가까운 글자 사이 번호.</summary>
        private int IndexAt(float worldX)
        {
            GameText.SetPlain(field, text);
            field.ForceMeshUpdate();
            var info = field.textInfo;
            float localX = field.transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
            for (int i = 0; i < info.characterCount && i < text.Length; i++)
            {
                var c = info.characterInfo[i];
                float mid = (c.origin + c.xAdvance) * 0.5f;
                if (localX < mid) return i;
            }
            return text.Length;
        }

        private void SetCaret(int index)
        {
            caret = Mathf.Clamp(index, 0, text.Length);
            caretMovedAt = Time.unscaledTime; // 움직인 직후엔 커서가 보이게
        }

        private void OnComposition(IMECompositionString s) => composing = s.ToString();

        /// <summary>PC(에디터)에서 친 글자(입력기로 조합이 끝난 글자 포함)를 커서 자리에 넣는다.</summary>
        private void OnEditorText(char c)
        {
            if (!IsOpen) return;
            if (c == '\b')
            {
                if (caret > 0) { text = text.Remove(caret - 1, 1); SetCaret(caret - 1); }
            }
            else if (c == '\r' || c == '\n') { Apply(); return; }
            else if (!char.IsControl(c) && text.Length < Nickname.InputLimit)
            {
                text = text.Insert(caret, c.ToString());
                SetCaret(caret + 1);
            }
            errorKey = null;
            Refresh();
        }

        private void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (editorTyping && kb != null && composing.Length == 0)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) SetCaret(caret - 1);
                if (kb.rightArrowKey.wasPressedThisFrame) SetCaret(caret + 1);
                if (kb.homeKey.wasPressedThisFrame) SetCaret(0);
                if (kb.endKey.wasPressedThisFrame) SetCaret(text.Length);
                if (kb.deleteKey.wasPressedThisFrame && caret < text.Length) { text = text.Remove(caret, 1); errorKey = null; Refresh(); }
            }
            if (keyboard != null)
            {
                string t = (keyboard.text ?? string.Empty).Replace("\n", "").Replace("\r", "");
                if (t.Length > Nickname.InputLimit) t = t.Substring(0, Nickname.InputLimit);
                if (t != text) { text = t; errorKey = null; Refresh(); }
                try { SetCaretQuiet(keyboard.selection.start); } catch (Exception) { caret = text.Length; }
                if (keyboard.status == TouchScreenKeyboard.Status.Done) { keyboard = null; Apply(); return; }
                if (keyboard.status == TouchScreenKeyboard.Status.Canceled || keyboard.status == TouchScreenKeyboard.Status.LostFocus) keyboard = null;
            }
            DrawField();
        }

        private void SetCaretQuiet(int index)
        {
            int c = Mathf.Clamp(index, 0, text.Length);
            if (c != caret) SetCaret(c);
        }

        /// <summary>글자(조합 중인 글자는 커서 자리에 끼워서)와 세로 막대 커서를 그린다.</summary>
        private void DrawField()
        {
            caret = Mathf.Clamp(caret, 0, text.Length);
            string shown = composing.Length > 0 ? text.Insert(caret, composing) : text;
            GameText.SetPlain(field, shown);
            field.ForceMeshUpdate();

            int at = caret + composing.Length; // 커서는 조합 중인 글자 뒤에
            var info = field.textInfo;
            float localX;
            if (at <= 0 || info.characterCount == 0)
                localX = -U(FieldWidthPx) * 0.5f;
            else
            {
                var c = info.characterInfo[Mathf.Min(at, info.characterCount) - 1];
                localX = c.xAdvance;
            }
            Vector3 world = field.transform.TransformPoint(new Vector3(localX, 0f, 0f));
            float one = U(1f);
            caretBar.transform.position = new Vector3(Mathf.Round(world.x / one) * one + one * 0.5f, field.transform.position.y, 0f);
            // 움직이거나 친 직후 0.5초는 켜 두고, 그 뒤로 깜빡
            float t = Time.unscaledTime - caretMovedAt;
            caretBar.enabled = t < 0.5f || Mathf.FloorToInt((t - 0.5f) * 2f) % 2 == 0;
        }

        private void Apply()
        {
            if (!IsOpen) return;
            errorKey = Nickname.Check(text);
            if (errorKey != null) { Refresh(); Sound.Play(Sfx.Hit, 0.8f); return; }
            SaveSystem.Data.nickname = text;
            SaveSystem.Save();
            Sound.Play(Sfx.CardPick);
            var done = onDone;
            Close();
            done?.Invoke(text);
        }

        private void Refresh()
        {
            caretMovedAt = Time.unscaledTime;
            // 안내: 잘못됐으면 빨간 이유, 아니면 규칙
            GameText.SetKey(message, errorKey ?? "nick.rules");
            message.color = errorKey != null ? new Color(1f, 0.45f, 0.45f) : new Color(0.75f, 0.8f, 1f);
        }
    }
}
