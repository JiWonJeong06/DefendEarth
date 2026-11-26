using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 번역 키를 기억했다가 언어가 바뀌면 글자를 다시 넣는다. GameText.SetKey가 자동으로 붙인다.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class LocalizedText : MonoBehaviour
    {
        private string key;
        private TextMeshPro text;

        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        private void Awake() => text = GetComponent<TextMeshPro>();
        private void OnEnable() { Localization.Changed += Refresh; Refresh(); }
        private void OnDisable() => Localization.Changed -= Refresh;

        private void Refresh()
        {
            if (text == null) text = GetComponent<TextMeshPro>();
            if (!string.IsNullOrEmpty(key)) text.text = Localization.Get(key);
        }
    }
}
