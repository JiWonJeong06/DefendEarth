using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 무한 판의 흐름을 정한다.
    /// 웨이브마다 몬스터 무리가 몰려오고, 웨이브가 오를수록 수·체력·속도가 늘고 새 종류가 섞인다.
    /// 몇 웨이브마다 끝부분에 러시(한꺼번에 몰려옴), 몇 웨이브마다 보스(회차마다 강해짐).
    /// 재화 운석은 웨이브와 따로 가끔 떨어진다.
    /// </summary>
    public sealed class WaveDirector : MonoBehaviour
    {
        private enum Phase { Break, Wave, BossWarning, Boss }

        [SerializeField] private MonsterSpawner spawner;
        [SerializeField] private WarningFlash warning;
        [SerializeField] private GoldDrops gold;

        public int Wave { get; private set; }
        public int BossRound { get; private set; }
        public Monster CurrentBoss { get; private set; }

        /// <summary>새 웨이브 시작(웨이브 번호).</summary>
        public event Action<int> WaveStarted;
        /// <summary>보스 등장(보스, 회차).</summary>
        public event Action<Monster, int> BossAppeared;
        /// <summary>보스 전투 끝(처치했는지).</summary>
        public event Action<bool> BossEnded;

        private Phase phase;
        private float phaseTimer;
        private float waveElapsed;
        private float normalDuration;
        private bool isRushWave;
        private bool rushWarned;
        private int groupsLeft;
        private float groupInterval;
        private float groupTimer;
        private float rushTimer;
        private float summonTimer;
        private float meteorTimer;

        private GameConfig Config => PlayArea.Config;

        private void OnEnable()
        {
            Monster.Killed += OnMonsterKilled;
            Monster.Escaped += OnMonsterEscaped;
        }

        private void OnDisable()
        {
            Monster.Killed -= OnMonsterKilled;
            Monster.Escaped -= OnMonsterEscaped;
        }

        private void Start()
        {
            phase = Phase.Break;
            phaseTimer = 1f; // 시작하고 잠깐 뒤 1웨이브
            meteorTimer = NextMeteorDelay();
        }

        private float HpMultiplier => 1f + Config.hpGrowthPerWave * Mathf.Max(0, Wave - 1);
        private float SpeedMultiplier => Mathf.Min(Config.speedMaxMultiplier, 1f + Config.speedGrowthPerWave * Mathf.Max(0, Wave - 1));
        private bool IsBossWave(int wave) => Config.bossEveryWaves > 0 && wave % Config.bossEveryWaves == 0;

        private void Update()
        {
            var session = GameSession.Instance;
            if (session == null || !session.IsPlaying) return;
            float dt = Time.deltaTime;

            UpdateMeteors(dt);

            switch (phase)
            {
                case Phase.Break:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) StartNextWave();
                    break;
                case Phase.Wave:
                    UpdateWave(dt);
                    break;
                case Phase.BossWarning:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) SpawnBoss();
                    break;
                case Phase.Boss:
                    UpdateBoss(dt);
                    break;
            }
        }

        private void StartNextWave()
        {
            Wave++;
            if (GameSession.Instance != null) GameSession.Instance.Stats.Wave = Wave;
            WaveStarted?.Invoke(Wave);

            if (IsBossWave(Wave) && GameAssets.Instance.boss != null)
            {
                phase = Phase.BossWarning;
                phaseTimer = Config.bossWarning;
                if (warning != null) warning.Show(WarningFlash.Kind.Boss, Config.bossWarning);
                return;
            }

            phase = Phase.Wave;
            waveElapsed = 0f;
            isRushWave = Config.rushEveryWaves > 0 && Wave % Config.rushEveryWaves == 0;
            rushWarned = false;
            normalDuration = isRushWave ? Mathf.Max(1f, Config.waveDuration - Config.rushDuration) : Config.waveDuration;
            groupsLeft = Mathf.Max(1, Mathf.RoundToInt(Config.groupsPerWaveBase + Config.groupsPerWaveStep * (Wave - 1)));
            groupInterval = normalDuration / groupsLeft;
            groupTimer = 0.2f;
            rushTimer = 0f;
        }

        private void UpdateWave(float dt)
        {
            waveElapsed += dt;

            if (groupsLeft > 0 && waveElapsed < normalDuration + 0.5f)
            {
                groupTimer -= dt;
                if (groupTimer <= 0f)
                {
                    if (spawner.TrySpawnGroup(spawner.PickMonster(Wave), HpMultiplier, SpeedMultiplier))
                    {
                        groupsLeft--;
                        groupTimer += groupInterval;
                    }
                    else groupTimer = 0.15f; // 자리가 없으면 잠깐 뒤 다시
                }
            }

            if (isRushWave)
            {
                if (!rushWarned && waveElapsed >= normalDuration - Config.rushWarning)
                {
                    rushWarned = true;
                    if (warning != null) warning.Show(WarningFlash.Kind.Rush, Config.rushWarning);
                }
                if (waveElapsed >= normalDuration && waveElapsed < Config.waveDuration)
                {
                    rushTimer -= dt;
                    if (rushTimer <= 0f)
                    {
                        rushTimer = spawner.TrySpawnGroup(spawner.PickMonster(Wave), HpMultiplier, SpeedMultiplier)
                            ? Config.rushInterval : 0.1f;
                    }
                }
            }

            if (waveElapsed >= Config.waveDuration)
            {
                phase = Phase.Break;
                phaseTimer = Config.waveBreak;
            }
        }

        private void SpawnBoss()
        {
            BossRound++;
            var def = GameAssets.Instance.boss;
            float radius = Config.PixelsToUnits(def.hitRadiusPx) * def.scale;
            float hpMul = 1f + Config.bossHpGrowth * (BossRound - 1);
            CurrentBoss = spawner.SpawnAt(def, new Vector2(0f, PlayArea.Top + radius), hpMul, 1f);
            phase = Phase.Boss;
            summonTimer = Config.bossSummonInterval * 0.5f;
            BossAppeared?.Invoke(CurrentBoss, BossRound);
        }

        private void UpdateBoss(float dt)
        {
            if (CurrentBoss == null || !CurrentBoss.IsAlive || !CurrentBoss.IsBoss)
            {
                EndBoss(false);
                return;
            }
            summonTimer -= dt;
            if (summonTimer <= 0f)
            {
                // 보스가 부하를 부른다(가장 약한 일반 몬스터 = 목록 첫 번째)
                var minion = GameAssets.Instance.monsters != null && GameAssets.Instance.monsters.Length > 0 ? GameAssets.Instance.monsters[0] : null;
                summonTimer = spawner.TrySpawnGroup(minion, HpMultiplier, SpeedMultiplier) ? Config.bossSummonInterval : 0.3f;
            }
        }

        private void EndBoss(bool killed)
        {
            if (phase != Phase.Boss) return;
            CurrentBoss = null;
            phase = Phase.Break;
            phaseTimer = Config.waveBreak + 1f;
            BossEnded?.Invoke(killed);
        }

        private void OnMonsterKilled(Monster m)
        {
            var stats = GameSession.Instance != null ? GameSession.Instance.Stats : null;
            if (stats != null && m.Kind != MonsterDefinition.Kind.Meteor) stats.AddKill(m.IsBoss);

            if (m.IsBoss && m == CurrentBoss)
            {
                // 단계 보상: 보스 회차가 오를수록 커진다
                int reward = Config.bossRewardBase + Config.bossRewardStep * (BossRound - 1);
                if (gold != null) gold.Burst(m.transform.position, reward, GoldDrops.Source.Stage);
                EndBoss(true);
            }
        }

        private void OnMonsterEscaped(Monster m)
        {
            if (m.IsBoss && m == CurrentBoss) EndBoss(false);
        }

        private void UpdateMeteors(float dt)
        {
            var def = GameAssets.Instance.meteor;
            if (def == null) return;
            meteorTimer -= dt;
            if (meteorTimer > 0f) return;
            meteorTimer = spawner.TrySpawnGroup(def, 1f, 1f) ? NextMeteorDelay() : 0.3f;
        }

        private float NextMeteorDelay() => UnityEngine.Random.Range(Config.meteorIntervalRange.x, Config.meteorIntervalRange.y);
    }
}
