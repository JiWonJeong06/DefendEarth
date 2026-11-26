using System;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// "정말 ~할까요?" 확인 창(예 / 아니요). 게임 종료 확인 등에 쓴다.
    /// </summary>
    public sealed class ConfirmPanel : ModalPanel
    {
        private TextMeshPro message;
        private Action onYes;

        public static ConfirmPanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("ConfirmPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ConfirmPanel>();
            p.Build(new Vector2(150f, 90f), null, sortingOrder, false);
            p.message = p.CreateText("Message", new Vector2(0f, 16f), GameText.Style.Title, TextAlignmentOptions.Center, 136f, true);
            var yes = p.CreateButton("Yes", new Vector2(-34f, -22f), new Vector2(58f, 20f), "ui.yes", new Color(0.8f, 0.35f, 0.35f));
            var no = p.CreateButton("No", new Vector2(34f, -22f), new Vector2(58f, 20f), "ui.no", new Color(0.35f, 0.45f, 0.7f));
            yes.Clicked += () =>
            {
                var action = p.onYes;
                p.Close();
                action?.Invoke();
            };
            no.Clicked += p.Close;
            return p;
        }

        public void Ask(string messageKey, Action yes)
        {
            onYes = yes;
            GameText.SetKey(message, messageKey);
            Open();
        }
    }

    /// <summary>앱 끝내기. 에디터에서는 Play를 멈춘다.</summary>
    public static class AppQuit
    {
        public static void Now()
        {
            SaveSystem.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
