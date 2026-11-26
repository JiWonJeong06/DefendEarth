namespace BouncyUFO
{
    /// <summary>
    /// 캐릭터 직업 하나(11/27 사용자 결정). 직업 + UFO 조합으로 판의 성격이 달라진다.
    /// 얻는 법: 기본(신입)은 처음부터, 나머지는 **플레이 레벨 + 골드**로 해금(UFO도 같은 방식).
    /// 나중에 조건형 직업·UFO(예: 특정 몬스터를 몇 번 처치, 지구 체력 1로 버티기)를 더할 예정 → Unlock에 조건을 붙인다.
    /// </summary>
    public sealed class JobType
    {
        public string nameKey;
        public string descKey;
        public UnlockRule unlock = new UnlockRule();

        // ---- 효과(0·1이면 없음) ----
        /// <summary>돌진 속도 배율(멀리 날기).</summary>
        public float dashSpeed = 1f;
        /// <summary>돌진 피해 +.</summary>
        public int damage;
        /// <summary>연쇄 피해 +.</summary>
        public int chainDamage;
        /// <summary>다이아 운석을 부술 때 다이아 +.</summary>
        public int diamondBonus;
        /// <summary>상점 증강 가격 -(최소 1).</summary>
        public int priceDiscount;
        /// <summary>지구 최대 체력 +.</summary>
        public int earthHp;
        /// <summary>지구 회복 증강 배율.</summary>
        public float healMultiplier = 1f;
        /// <summary>상점 진열 개수(0 = 기본 3).</summary>
        public int shopSlots;
        /// <summary>상점마다 첫 새로고침 무료.</summary>
        public bool freeFirstRefresh;
    }

    /// <summary>
    /// 해금 조건. 지금은 플레이 레벨 + 골드. 나중에 조건형(처치 수 등)을 여기에 더한다.
    /// </summary>
    public sealed class UnlockRule
    {
        /// <summary>필요한 플레이(계정) 레벨. 1 = 처음부터.</summary>
        public int level = 1;
        /// <summary>해금 가격(골드). 0 = 무료.</summary>
        public int gold;

        public bool IsFree => level <= 1 && gold <= 0;
        public static int PlayerLevel => Account.FromTotal(SaveSystem.Data.accountXp).level;
        public bool LevelMet => PlayerLevel >= level;
    }

    /// <summary>직업 목록과 지금 고른 직업. 번호는 저장에 쓰이므로 순서를 바꾸지 말고 뒤에만 붙인다.</summary>
    public static class JobTypes
    {
        public static readonly JobType[] All =
        {
            // 0 신입: 능력 없음(처음부터)
            new JobType { nameKey = "job.rookie.name", descKey = "job.rookie.desc" },
            // 1 파일럿: 돌진 15% 멀리
            new JobType { nameKey = "job.pilot.name", descKey = "job.pilot.desc", unlock = new UnlockRule { level = 2, gold = 300 }, dashSpeed = 1.15f },
            // 2 격투가: 돌진 피해 +1, 연쇄 피해 +1
            new JobType { nameKey = "job.brawler.name", descKey = "job.brawler.desc", unlock = new UnlockRule { level = 3, gold = 500 }, damage = 1, chainDamage = 1 },
            // 3 상인: 다이아 운석 +1, 증강 1 싸게
            new JobType { nameKey = "job.merchant.name", descKey = "job.merchant.desc", unlock = new UnlockRule { level = 4, gold = 600 }, diamondBonus = 1, priceDiscount = 1 },
            // 4 정비사: 지구 최대 체력 +30, 지구 회복 2배
            new JobType { nameKey = "job.engineer.name", descKey = "job.engineer.desc", unlock = new UnlockRule { level = 5, gold = 700 }, earthHp = 30, healMultiplier = 2f },
            // 5 과학자: 상점 4칸, 첫 새로고침 무료
            new JobType { nameKey = "job.scientist.name", descKey = "job.scientist.desc", unlock = new UnlockRule { level = 6, gold = 900 }, shopSlots = 4, freeFirstRefresh = true },
        };

        public static bool IsOwned(int index) => index == 0 || All[index].unlock.IsFree || SaveSystem.Data.ownedJobs.Contains(index);

        /// <summary>지금 고른 직업 번호(가지고 있지 않으면 신입).</summary>
        public static int SelectedIndex
        {
            get
            {
                int i = SaveSystem.Data.selectedJob;
                return i >= 0 && i < All.Length && IsOwned(i) ? i : 0;
            }
        }

        /// <summary>이번 판의 직업. 튜토리얼은 늘 신입(능력 없음).</summary>
        public static JobType Current => SceneFlow.IsTutorial ? All[0] : All[SelectedIndex];
    }
}
