using UnityEngine;
using UnityEngine.SceneManagement;

namespace BouncyUFO
{
    /// <summary>
    /// 씬 이동과 씬 사이에 넘기는 데이터를 한곳에서 관리한다.
    /// 타이틀 → 로비 → 로딩 → 게임 → 결과. 씬을 더 만들면 여기에 이름을 추가한다.
    /// </summary>
    public static class SceneFlow
    {
        public const string Title = "Title";
        public const string Lobby = "Lobby";
        public const string Loading = "Loading";
        public const string Game = "Game";
        public const string Result = "Result";

        /// <summary>로딩 씬이 다음에 불러올 씬.</summary>
        public static string LoadingTarget { get; private set; } = Game;

        /// <summary>방금 끝난 판의 결과(게임 → 결과 씬으로 전달).</summary>
        public static RunResult LastResult { get; set; }
        public static bool HasResult { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            LoadingTarget = Game;
            HasResult = false;
        }

        public static void GoTitle() => Go(Title);
        public static void GoLobby() => Go(Lobby);
        public static void GoResult() => Go(Result);

        /// <summary>로딩 화면을 거쳐 게임을 시작한다.</summary>
        public static void StartGame()
        {
            LoadingTarget = Game;
            Go(Loading);
        }

        private static void Go(string scene)
        {
            Time.timeScale = 1f;
            UiLayer.Reset();
            SceneManager.LoadScene(scene);
        }
    }
}
