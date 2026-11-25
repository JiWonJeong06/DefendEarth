using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 간단 튜토리얼(첫 빌드용). 처음 한 번만 나온다(기기에 저장).
    /// 1) 이동: UFO 왼쪽·오른쪽을 눌러 한 번씩 튀어 보기 — 그동안 몬스터는 나오지 않는다.
    /// 2) 공격: 자동 발사로 몬스터 몇 마리 잡기.
    /// 3) "이제 지구를 지켜 주세요!" 잠깐 보여 주고 끝.
    /// (1월에 전체 튜토리얼·건너뛰기·다시 보기로 확장)
    /// </summary>
    public sealed class TutorialHints : MonoBehaviour
    {
        private enum Step { Move, Attack, Go, Done }

        [SerializeField] private UfoController ufo;
        [SerializeField] private WaveDirector waves;
        [Tooltip("공격 단계에서 잡아야 하는 몬스터 수")]
        [SerializeField] private int killsToPass = 3;
        [Tooltip("글자 줄 높이(플레이 영역 아래 끝에서, 기준 픽셀)")]
        [SerializeField] private float textHeightPx = 120f;

        private Step step = Step.Done;
        private bool hoppedLeft;
        private bool hoppedRight;
        private int kills;
        private float stepStarted;
        private TextMeshPro line1;
        private TextMeshPro line2;
        private TextMeshPro arrowLeft;
        private TextMeshPro arrowRight;

        private GameConfig Config => PlayArea.Config;

        private void Start()
        {
            if (SaveSystem.Data.tutorialDone) { enabled = false; return; }

            float y = PlayArea.Bottom + Config.PixelsToUnits(textHeightPx);
            float box = PlayArea.Config.playAreaPixels.x - 16f;
            line1 = GameText.Create("Line1", transform, GameText.Style.Body, SortOrder.Hud + 10, TextAlignmentOptions.Center, box, true);
            line1.transform.position = new Vector3(0f, y, 0f);
            line2 = GameText.Create("Line2", transform, GameText.Style.Body, SortOrder.Hud + 10, TextAlignmentOptions.Center, box, true);
            line2.transform.position = new Vector3(0f, y - Config.PixelsToUnits(16f), 0f);
            line2.color = new Color(1f, 0.9f, 0.5f);

            arrowLeft = GameText.Create("ArrowL", transform, GameText.Style.Title, SortOrder.Hud + 10);
            arrowLeft.text = "<";
            arrowRight = GameText.Create("ArrowR", transform, GameText.Style.Title, SortOrder.Hud + 10);
            arrowRight.text = ">";

            if (ufo != null) ufo.Hopped += OnHopped;
            Monster.Killed += OnKilled;
            if (waves != null) waves.HoldStart = true;
            Enter(Step.Move);
        }

        private void OnDestroy()
        {
            if (ufo != null) ufo.Hopped -= OnHopped;
            Monster.Killed -= OnKilled;
        }

        private void Enter(Step next)
        {
            step = next;
            stepStarted = Time.time;
            switch (step)
            {
                case Step.Move:
                    GameText.SetKey(line1, "tut.move");
                    GameText.SetKey(line2, "tut.move_long");
                    break;
                case Step.Attack:
                    if (waves != null) waves.HoldStart = false;
                    GameText.SetKey(line1, "tut.attack");
                    line2.text = string.Empty;
                    break;
                case Step.Go:
                    GameText.SetKey(line1, "tut.go");
                    line2.text = string.Empty;
                    break;
                case Step.Done:
                    SaveSystem.Data.tutorialDone = true;
                    SaveSystem.Save();
                    gameObject.SetActive(false);
                    break;
            }
            bool arrows = step == Step.Move;
            arrowLeft.gameObject.SetActive(arrows);
            arrowRight.gameObject.SetActive(arrows);
        }

        private void OnHopped(int dir)
        {
            if (step != Step.Move) return;
            if (dir < 0) hoppedLeft = true;
            else hoppedRight = true;
            if (hoppedLeft && hoppedRight) Enter(Step.Attack);
        }

        private void OnKilled(Monster m)
        {
            if (step != Step.Attack || m.Kind != MonsterDefinition.Kind.Monster) return;
            if (++kills >= killsToPass) Enter(Step.Go);
        }

        private void Update()
        {
            if (step == Step.Done) return;
            float t = Time.time - stepStarted;
            line1.alpha = 0.7f + 0.3f * Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);

            if (step == Step.Move && ufo != null)
            {
                // UFO 양옆에 깜빡이는 화살표: 아직 안 해 본 쪽만
                Vector3 p = ufo.transform.position;
                float off = Config.PixelsToUnits(22f + 3f * Mathf.PingPong(Time.unscaledTime * 4f, 1f));
                arrowLeft.transform.position = new Vector3(p.x - off, p.y, 0f);
                arrowRight.transform.position = new Vector3(p.x + off, p.y, 0f);
                arrowLeft.alpha = hoppedLeft ? 0.25f : 1f;
                arrowRight.alpha = hoppedRight ? 0.25f : 1f;
            }

            if (step == Step.Go && t > 2.5f) Enter(Step.Done);
        }
    }
}
