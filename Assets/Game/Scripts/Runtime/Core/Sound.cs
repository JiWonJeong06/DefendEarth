using UnityEngine;

namespace BouncyUFO
{
    /// <summary>효과음 종류.</summary>
    public enum Sfx
    {
        Click, Start, Dash, Land, Hit, Kill, MeteorBreak, Coin, EarthHurt,
        LevelUp, CardPick, Warning, BossDown, GameOver,
    }

    /// <summary>
    /// 소리 담당. 효과음은 소리 파일 없이 코드로 만든다(8비트 게임기 느낌).
    /// 배경음악은 한 곡(Resources/Music/DefendEarth)을 모든 화면에서 끊김 없이 반복한다.
    /// 게임을 켤 때 스스로 생겨 씬이 바뀌어도 남는다. 설정의 "소리 끄기"를 따른다.
    /// </summary>
    public sealed class Sound : MonoBehaviour
    {
        private const int Rate = 22050;
        private const int Voices = 10;
        private const float SfxVolume = 0.7f;
        private const float MusicVolume = 0.4f;
        private const float MinRepeat = 0.035f; // 같은 소리를 너무 촘촘히 겹치지 않게

        private static Sound instance;

        private AudioClip[] clips;
        private float[] lastPlayed;
        private AudioSource[] voices;
        private int nextVoice;
        private AudioSource music;
        private const string MusicPath = "Music/DefendEarth";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("Sound");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Sound>();
        }

        /// <summary>효과음을 낸다. pitch로 높낮이를 조금씩 바꾸면 덜 단조롭다.</summary>
        public static void Play(Sfx id, float pitch = 1f, float volume = 1f)
        {
            var s = instance;
            if (s == null || s.clips == null || !SaveSystem.Data.sound) return;
            int i = (int)id;
            if (Time.unscaledTime - s.lastPlayed[i] < MinRepeat) return;
            s.lastPlayed[i] = Time.unscaledTime;
            var v = s.voices[s.nextVoice];
            s.nextVoice = (s.nextVoice + 1) % Voices;
            v.clip = s.clips[i];
            v.pitch = pitch;
            v.volume = SfxVolume * volume;
            v.Play();
        }

        private void Awake()
        {
            gameObject.AddComponent<AudioListener>();
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = MusicVolume;

            BuildSfx();
            music.clip = Resources.Load<AudioClip>(MusicPath);
            if (music.clip != null) music.Play();
            else Debug.LogWarning("배경음악을 찾지 못함: Resources/" + MusicPath);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            music.mute = !SaveSystem.Data.sound;
        }

        // ---------------- 효과음 ----------------

        private void BuildSfx()
        {
            int n = System.Enum.GetValues(typeof(Sfx)).Length;
            clips = new AudioClip[n];
            lastPlayed = new float[n];
            for (int i = 0; i < n; i++) lastPlayed[i] = -1f;

            Make(Sfx.Click, 0.04f, b => Tone(b, 0f, 0.035f, 1200f, 1200f, Wave.Square, 0.3f, 0.5f, 1f));
            Make(Sfx.Start, 0.36f, b =>
            {
                int[] notes = { 72, 76, 79, 84 };
                for (int i = 0; i < notes.Length; i++)
                    Tone(b, i * 0.06f, i == 3 ? 0.18f : 0.09f, N(notes[i]), N(notes[i]), Wave.Square, 0.28f, 0.25f, 1.5f);
            });
            Make(Sfx.Dash, 0.16f, b =>
            {
                Tone(b, 0f, 0.14f, 260f, 900f, Wave.Square, 0.28f, 0.25f, 1f);
                Tone(b, 0f, 0.12f, 3000f, 1500f, Wave.Noise, 0.12f, 0.5f, 1.5f);
            });
            Make(Sfx.Land, 0.08f, b => Tone(b, 0f, 0.07f, 180f, 70f, Wave.Triangle, 0.5f, 0.5f, 2f));
            Make(Sfx.Hit, 0.09f, b =>
            {
                Tone(b, 0f, 0.08f, 600f, 250f, Wave.Square, 0.28f, 0.5f, 2f);
                Tone(b, 0f, 0.06f, 6000f, 2000f, Wave.Noise, 0.22f, 0.5f, 2f);
            });
            Make(Sfx.Kill, 0.22f, b =>
            {
                Tone(b, 0f, 0.06f, 500f, 1300f, Wave.Square, 0.26f, 0.5f, 1f);
                Tone(b, 0.03f, 0.18f, 4000f, 600f, Wave.Noise, 0.32f, 0.5f, 2f);
            });
            Make(Sfx.MeteorBreak, 0.28f, b =>
            {
                Tone(b, 0f, 0.26f, 2500f, 300f, Wave.Noise, 0.42f, 0.5f, 2f);
                Tone(b, 0f, 0.15f, 200f, 80f, Wave.Triangle, 0.4f, 0.5f, 1.5f);
            });
            Make(Sfx.Coin, 0.24f, b =>
            {
                Tone(b, 0f, 0.06f, N(83), N(83), Wave.Square, 0.22f, 0.5f, 0.3f);
                Tone(b, 0.06f, 0.17f, N(88), N(88), Wave.Square, 0.22f, 0.5f, 2f);
            });
            Make(Sfx.EarthHurt, 0.42f, b =>
            {
                Tone(b, 0f, 0.4f, 900f, 100f, Wave.Noise, 0.45f, 0.5f, 1.5f);
                Tone(b, 0f, 0.35f, 110f, 40f, Wave.Triangle, 0.6f, 0.5f, 1.5f);
            });
            Make(Sfx.LevelUp, 0.5f, b =>
            {
                int[] notes = { 72, 76, 79, 84, 88 };
                for (int i = 0; i < notes.Length; i++)
                {
                    float len = i == notes.Length - 1 ? 0.22f : 0.08f;
                    Tone(b, i * 0.07f, len, N(notes[i]), N(notes[i]), Wave.Square, 0.22f, 0.5f, 1f);
                    Tone(b, i * 0.07f, len, N(notes[i] - 12), N(notes[i] - 12), Wave.Triangle, 0.25f, 0.5f, 1f);
                }
            });
            Make(Sfx.CardPick, 0.18f, b =>
            {
                Tone(b, 0f, 0.05f, N(79), N(79), Wave.Triangle, 0.45f, 0.5f, 0.5f);
                Tone(b, 0.05f, 0.12f, N(86), N(86), Wave.Triangle, 0.45f, 0.5f, 1.5f);
            });
            Make(Sfx.Warning, 0.66f, b =>
            {
                for (int i = 0; i < 4; i++)
                {
                    float f = i % 2 == 0 ? 880f : 660f;
                    Tone(b, i * 0.16f, 0.13f, f, f, Wave.Square, 0.22f, 0.5f, 0.4f);
                }
            });
            Make(Sfx.BossDown, 0.95f, b =>
            {
                int[] notes = { 72, 79, 84, 88 };
                for (int i = 0; i < notes.Length; i++)
                    Tone(b, i * 0.08f, 0.1f, N(notes[i]), N(notes[i]), Wave.Square, 0.22f, 0.5f, 1f);
                foreach (int m in new[] { 84, 88, 91 })
                    Tone(b, 0.34f, 0.6f, N(m), N(m), Wave.Square, 0.12f, 0.25f, 1.2f);
                Tone(b, 0.34f, 0.6f, N(60), N(60), Wave.Triangle, 0.35f, 0.5f, 1.2f);
            });
            Make(Sfx.GameOver, 1.1f, b =>
            {
                int[] notes = { 67, 64, 60, 55 };
                for (int i = 0; i < notes.Length; i++)
                {
                    float len = i == notes.Length - 1 ? 0.55f : 0.17f;
                    Tone(b, i * 0.18f, len, N(notes[i]), N(notes[i]), Wave.Square, 0.22f, 0.5f, 0.8f);
                    Tone(b, i * 0.18f, len, N(notes[i] - 12), N(notes[i] - 12), Wave.Triangle, 0.3f, 0.5f, 0.8f);
                }
            });
        }

        private void Make(Sfx id, float seconds, System.Action<float[]> draw)
        {
            var b = new float[Mathf.CeilToInt(seconds * Rate)];
            draw(b);
            clips[(int)id] = ToClip(id.ToString(), b);
        }

        // ---------------- 신시사이저 ----------------

        private enum Wave { Square, Triangle, Noise }

        private static uint noiseState = 2463534242u;

        private static float NoiseValue()
        {
            noiseState ^= noiseState << 13;
            noiseState ^= noiseState >> 17;
            noiseState ^= noiseState << 5;
            return noiseState / (float)uint.MaxValue * 2f - 1f;
        }

        /// <summary>MIDI 번호 → 주파수(69 = 라 440Hz).</summary>
        private static float N(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        /// <summary>
        /// 버퍼에 소리 하나를 더한다. f0→f1로 미끄러지고, decay가 클수록 빨리 사그라든다.
        /// duty는 사각파의 폭(0.125·0.25·0.5에 따라 음색이 다름). 잡음은 주파수만큼 자주 값이 바뀐다.
        /// </summary>
        private static void Tone(float[] buf, float start, float duration, float f0, float f1, Wave wave, float volume, float duty, float decay)
        {
            int s0 = Mathf.RoundToInt(start * Rate);
            int n = Mathf.Max(1, Mathf.RoundToInt(duration * Rate));
            int attack = Mathf.Max(1, Rate / 500); // 2ms (딸깍 소리 방지)
            float ratio = f1 / f0;
            double phase = 0.0;
            float held = NoiseValue();
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) break;
                float k = i / (float)n;
                float f = f0 * Mathf.Pow(ratio, k);
                double prev = phase;
                phase += f / Rate;
                if (phase >= 1.0) phase -= System.Math.Floor(phase);
                float p = (float)phase;
                float v;
                switch (wave)
                {
                    case Wave.Square: v = p < duty ? 1f : -1f; break;
                    case Wave.Triangle: v = 4f * Mathf.Abs(p - 0.5f) - 1f; break;
                    default:
                        if (phase < prev) held = NoiseValue();
                        v = held;
                        break;
                }
                float env = Mathf.Min(1f, i / (float)attack) * Mathf.Pow(1f - k, decay);
                int tail = n - i;
                if (tail < attack) env *= tail / (float)attack;
                buf[idx] += v * env * volume;
            }
        }

        private static AudioClip ToClip(string name, float[] b)
        {
            float peak = 0f;
            for (int i = 0; i < b.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(b[i]));
            if (peak > 0.95f)
                for (int i = 0; i < b.Length; i++) b[i] *= 0.95f / peak;
            var clip = AudioClip.Create(name, b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }
    }
}
