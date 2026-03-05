using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public class SteamAchievementTracker : MonoBehaviour
    {
        [Header("Steam")]
        public bool enableAchievements = true;
        public bool logUnlocks = true;

        private const float SurgeComboDuration = 20f;
        private const int SurgeComboThreshold = 50;
        private const float TimekeeperSeconds = 900f;
        private const int StunTargetCount = 50;
        private const int HeavyKillTarget = 50;
        private const int SkillKillTarget = 100;
        private const int PredatorTarget = 1000;
        private const int TalentTarget = 10;
        private const int PearlInitiateTarget = 5;
        private const int PearlAdeptTarget = 20;
        private const int PearlMasterTarget = 50;

        private static class SteamAchievementId
        {
            public const string IntoTrench = "into_trench";
            public const string ResearchWreckage = "research_wreckage";
            public const string ThermalVeins = "thermal_veins";
            public const string CoralThicket = "coral_thicket";
            public const string SunkenDistrict = "sunken_district";
            public const string DarkPipeline = "dark_pipeline";
            public const string AbyssHangar = "abyss_hangar";
            public const string MoltenRift = "molten_rift";
            public const string SilentSanctum = "silent_sanctum";
            public const string AbyssCore = "abyss_core";
            public const string Breaker = "breaker";
            public const string CleanExecution = "clean_execution";
            public const string LastStand = "last_stand";
            public const string Predator = "predator";
            public const string Relentless = "relentless";
            public const string Unstoppable = "unstoppable";
            public const string HeavyHand = "heavy_hand";
            public const string SkillMastery = "skill_mastery";
            public const string ShockAndAwe = "shock_and_awe";
            public const string NoMercy = "no_mercy";
            public const string ReinforcementsDenied = "reinforcements_denied";
            public const string PursuitBroken = "pursuit_broken";
            public const string Guardian = "guardian";
            public const string HoldTheLine = "hold_the_line";
            public const string Untouched = "untouched";
            public const string PearlInitiate = "pearl_initiate";
            public const string PearlAdept = "pearl_adept";
            public const string PearlMaster = "pearl_master";
            public const string PathOfOffense = "path_of_offense";
            public const string PathOfControl = "path_of_control";
            public const string PathOfSurvival = "path_of_survival";
            public const string TalentSeeker = "talent_seeker";
            public const string FullKit = "full_kit";
            public const string Timekeeper = "timekeeper";
            public const string Surge = "surge";
            public const string GlassCannon = "glass_cannon";
        }

        private SteamIntegrationService steam;
        private SaveManager saveManager;
        private PlayerHealth playerHealth;
        private TalentTree talentTree;
        private LongTermProgressionSystem progression;
        private ProgressionMilestoneData milestoneData;

        private readonly HashSet<string> unlocked = new HashSet<string>();

        private int totalKills;
        private int heavyKills;
        private int skillKills;
        private int stunCount;
        private int pearlsCollected;
        private int spentTalentPoints;
        private int currentCombo;
        private float combo50Timer;
        private float levelStartTime;
        private bool levelHealed;
        private bool levelPlayerDied;
        private bool waveDamaged;
        private bool strongholdDamaged;
        private bool waveActive;
        private bool strongholdActive;

        private void Awake()
        {
            steam = SteamIntegrationService.Instance;
            saveManager = SaveManager.Instance;
            RefreshReferences();
            InitializeCounters();
        }

        private void OnEnable()
        {
            GameEvents.OnLevelStarted += HandleLevelStarted;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
            GameEvents.OnPlayerHealed += HandlePlayerHealed;
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
            GameEvents.OnComboCountChanged += HandleComboChanged;
            GameEvents.OnEnemyKilledDetailed += HandleEnemyKilledDetailed;
            GameEvents.OnEnemyHit += HandleEnemyHit;
            GameEvents.OnPearlCollected += HandlePearlCollected;
            GameEvents.OnBossBreakWindowStart += HandleBossBreakWindowStart;
            GameEvents.OnBossDefeated += HandleBossDefeated;
            GameEvents.OnWaveStarted += HandleWaveStarted;
            GameEvents.OnWaveCompleted += HandleWaveCompleted;
            GameEvents.OnStrongholdStarted += HandleStrongholdStarted;
            GameEvents.OnStrongholdCompleted += HandleStrongholdCompleted;
            GameEvents.OnWaveEventCompleted += HandleWaveEventCompleted;
            GameEvents.OnTalentUnlocked += HandleTalentUnlocked;
            GameEvents.OnProgressionMilestoneClaimed += HandleMilestoneClaimed;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelStarted -= HandleLevelStarted;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnPlayerHealed -= HandlePlayerHealed;
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
            GameEvents.OnComboCountChanged -= HandleComboChanged;
            GameEvents.OnEnemyKilledDetailed -= HandleEnemyKilledDetailed;
            GameEvents.OnEnemyHit -= HandleEnemyHit;
            GameEvents.OnPearlCollected -= HandlePearlCollected;
            GameEvents.OnBossBreakWindowStart -= HandleBossBreakWindowStart;
            GameEvents.OnBossDefeated -= HandleBossDefeated;
            GameEvents.OnWaveStarted -= HandleWaveStarted;
            GameEvents.OnWaveCompleted -= HandleWaveCompleted;
            GameEvents.OnStrongholdStarted -= HandleStrongholdStarted;
            GameEvents.OnStrongholdCompleted -= HandleStrongholdCompleted;
            GameEvents.OnWaveEventCompleted -= HandleWaveEventCompleted;
            GameEvents.OnTalentUnlocked -= HandleTalentUnlocked;
            GameEvents.OnProgressionMilestoneClaimed -= HandleMilestoneClaimed;
        }

        private void Update()
        {
            if (!enableAchievements)
            {
                return;
            }

            UpdateSurgeTimer();
        }

        private void RefreshReferences()
        {
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }

            if (talentTree == null)
            {
                talentTree = FindObjectOfType<TalentTree>();
            }

            if (progression == null)
            {
                progression = FindObjectOfType<LongTermProgressionSystem>();
            }

            if (progression != null)
            {
                milestoneData = progression.milestoneData;
            }
        }

        private void InitializeCounters()
        {
            if (saveManager != null && saveManager.CurrentData != null)
            {
                totalKills = saveManager.CurrentData.totalKills;
                pearlsCollected = saveManager.CurrentData.pearlsCollected;
            }

            spentTalentPoints = CalculateSpentTalentPoints();
            CheckPearlAchievements();
            CheckTalentAchievements();
            CheckProgressionRouteAchievements();
            CheckFullKit();
        }

        private int CalculateSpentTalentPoints()
        {
            if (talentTree == null || talentTree.data == null || talentTree.data.nodes == null)
            {
                return 0;
            }

            int spent = 0;
            for (int i = 0; i < talentTree.data.nodes.Count; i++)
            {
                TalentNodeData node = talentTree.data.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.id))
                {
                    continue;
                }

                if (talentTree.unlockedNodes.Contains(node.id))
                {
                    spent += Mathf.Max(0, node.cost);
                }
            }

            return spent;
        }

        private void HandleLevelStarted(int levelId)
        {
            levelStartTime = Time.time;
            levelHealed = false;
            levelPlayerDied = false;
            combo50Timer = 0f;
            RefreshReferences();
        }

        private void HandleLevelCompleted(int levelId)
        {
            UnlockLevelAchievements(levelId);
            CheckTimekeeper();
            if (!levelHealed)
            {
                TryUnlock(SteamAchievementId.GlassCannon);
            }
        }

        private void HandlePlayerHealed(int amount)
        {
            levelHealed = true;
        }

        private void HandlePlayerDamaged(float damage, Vector3 source)
        {
            if (waveActive)
            {
                waveDamaged = true;
            }

            if (strongholdActive)
            {
                strongholdDamaged = true;
            }
        }

        private void HandlePlayerDeath()
        {
            levelPlayerDied = true;
        }

        private void HandleComboChanged(int comboCount)
        {
            currentCombo = comboCount;
            if (comboCount >= 80)
            {
                TryUnlock(SteamAchievementId.Relentless);
            }

            if (comboCount >= 120)
            {
                TryUnlock(SteamAchievementId.Unstoppable);
            }
        }

        private void UpdateSurgeTimer()
        {
            if (currentCombo >= SurgeComboThreshold)
            {
                combo50Timer += Time.deltaTime;
                if (combo50Timer >= SurgeComboDuration)
                {
                    TryUnlock(SteamAchievementId.Surge);
                }
            }
            else
            {
                combo50Timer = 0f;
            }
        }

        private void HandleEnemyKilledDetailed(EnemyType type, Vector3 position, int expReward, DamageSourceType sourceType, bool isHeavyAttack)
        {
            totalKills++;
            if (totalKills >= PredatorTarget)
            {
                TryUnlock(SteamAchievementId.Predator);
            }

            if (isHeavyAttack)
            {
                heavyKills++;
                if (heavyKills >= HeavyKillTarget)
                {
                    TryUnlock(SteamAchievementId.HeavyHand);
                }
            }

            if (sourceType == DamageSourceType.PlayerSkill)
            {
                skillKills++;
                if (skillKills >= SkillKillTarget)
                {
                    TryUnlock(SteamAchievementId.SkillMastery);
                }
            }
        }

        private void HandleEnemyHit(int damage, Vector3 position, EnemyHitReactionType reactionType)
        {
            if (reactionType == EnemyHitReactionType.Knockdown)
            {
                stunCount++;
                if (stunCount >= StunTargetCount)
                {
                    TryUnlock(SteamAchievementId.ShockAndAwe);
                }
            }
        }

        private void HandlePearlCollected(string pearlId)
        {
            pearlsCollected++;
            CheckPearlAchievements();
        }

        private void HandleBossBreakWindowStart()
        {
            TryUnlock(SteamAchievementId.Breaker);
        }

        private void HandleBossDefeated(BossSpawnPoint boss)
        {
            if (!levelPlayerDied)
            {
                TryUnlock(SteamAchievementId.CleanExecution);
            }

            if (playerHealth != null && playerHealth.HealthPercent <= 0.2f)
            {
                TryUnlock(SteamAchievementId.LastStand);
            }
        }

        private void HandleWaveStarted(StrongholdController stronghold, int waveIndex)
        {
            waveActive = true;
            waveDamaged = false;
        }

        private void HandleWaveCompleted(StrongholdController stronghold, int waveIndex)
        {
            if (waveActive && !waveDamaged)
            {
                TryUnlock(SteamAchievementId.Untouched);
            }

            waveActive = false;
        }

        private void HandleStrongholdStarted(StrongholdController stronghold)
        {
            strongholdActive = true;
            strongholdDamaged = false;
        }

        private void HandleStrongholdCompleted(StrongholdController stronghold)
        {
            if (strongholdActive && !strongholdDamaged)
            {
                TryUnlock(SteamAchievementId.NoMercy);
            }

            strongholdActive = false;
        }

        private void HandleWaveEventCompleted(StrongholdController stronghold, int waveIndex, WaveEventType eventType)
        {
            switch (eventType)
            {
                case WaveEventType.Reinforcement:
                    TryUnlock(SteamAchievementId.ReinforcementsDenied);
                    break;
                case WaveEventType.Chase:
                    TryUnlock(SteamAchievementId.PursuitBroken);
                    break;
                case WaveEventType.ProtectTarget:
                    TryUnlock(SteamAchievementId.Guardian);
                    break;
                case WaveEventType.HoldPoint:
                    TryUnlock(SteamAchievementId.HoldTheLine);
                    break;
            }
        }

        private void HandleTalentUnlocked(string nodeId, int cost)
        {
            spentTalentPoints += Mathf.Max(0, cost);
            CheckTalentAchievements();
        }

        private void HandleMilestoneClaimed(string milestoneId, ProgressionRoute route)
        {
            CheckProgressionRouteAchievements();
            CheckFullKit();
        }

        private void CheckPearlAchievements()
        {
            if (pearlsCollected >= PearlInitiateTarget)
            {
                TryUnlock(SteamAchievementId.PearlInitiate);
            }

            if (pearlsCollected >= PearlAdeptTarget)
            {
                TryUnlock(SteamAchievementId.PearlAdept);
            }

            if (pearlsCollected >= PearlMasterTarget)
            {
                TryUnlock(SteamAchievementId.PearlMaster);
            }
        }

        private void CheckTalentAchievements()
        {
            if (spentTalentPoints >= TalentTarget)
            {
                TryUnlock(SteamAchievementId.TalentSeeker);
            }
        }

        private void CheckProgressionRouteAchievements()
        {
            if (milestoneData == null || milestoneData.milestones == null)
            {
                return;
            }

            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            int offenseCount = 0;
            int controlCount = 0;
            int survivalCount = 0;
            HashSet<string> claimed = new HashSet<string>(saveManager.CurrentData.claimedProgressionMilestones);

            for (int i = 0; i < milestoneData.milestones.Count; i++)
            {
                ProgressionMilestone milestone = milestoneData.milestones[i];
                if (milestone == null || string.IsNullOrEmpty(milestone.id))
                {
                    continue;
                }

                if (!claimed.Contains(milestone.id))
                {
                    continue;
                }

                switch (milestone.route)
                {
                    case ProgressionRoute.Offense:
                        offenseCount++;
                        break;
                    case ProgressionRoute.Control:
                        controlCount++;
                        break;
                    case ProgressionRoute.Survival:
                        survivalCount++;
                        break;
                }
            }

            if (offenseCount >= 3)
            {
                TryUnlock(SteamAchievementId.PathOfOffense);
            }

            if (controlCount >= 3)
            {
                TryUnlock(SteamAchievementId.PathOfControl);
            }

            if (survivalCount >= 3)
            {
                TryUnlock(SteamAchievementId.PathOfSurvival);
            }
        }

        private void CheckFullKit()
        {
            if (saveManager == null || saveManager.CurrentData == null)
            {
                return;
            }

            int maxSlots = GetMaxPearlSlots();
            if (maxSlots > 0 && saveManager.CurrentData.unlockedPearlSlots >= maxSlots)
            {
                TryUnlock(SteamAchievementId.FullKit);
            }
        }

        private int GetMaxPearlSlots()
        {
            const int baseSlots = 3;
            if (milestoneData == null || milestoneData.milestones == null)
            {
                return baseSlots;
            }

            int extraSlots = 0;
            for (int i = 0; i < milestoneData.milestones.Count; i++)
            {
                ProgressionMilestone milestone = milestoneData.milestones[i];
                if (milestone == null)
                {
                    continue;
                }

                extraSlots += Mathf.Max(0, milestone.grantPearlSlots);
            }

            return baseSlots + extraSlots;
        }

        private void UnlockLevelAchievements(int levelId)
        {
            switch (levelId)
            {
                case 1:
                    TryUnlock(SteamAchievementId.IntoTrench);
                    break;
                case 2:
                    TryUnlock(SteamAchievementId.ResearchWreckage);
                    break;
                case 3:
                    TryUnlock(SteamAchievementId.ThermalVeins);
                    break;
                case 4:
                    TryUnlock(SteamAchievementId.CoralThicket);
                    break;
                case 5:
                    TryUnlock(SteamAchievementId.SunkenDistrict);
                    break;
                case 6:
                    TryUnlock(SteamAchievementId.DarkPipeline);
                    break;
                case 7:
                    TryUnlock(SteamAchievementId.AbyssHangar);
                    break;
                case 8:
                    TryUnlock(SteamAchievementId.MoltenRift);
                    break;
                case 9:
                    TryUnlock(SteamAchievementId.SilentSanctum);
                    break;
                case 10:
                    TryUnlock(SteamAchievementId.AbyssCore);
                    break;
            }
        }

        private void CheckTimekeeper()
        {
            if (levelStartTime <= 0f)
            {
                return;
            }

            float elapsed = Time.time - levelStartTime;
            if (elapsed <= TimekeeperSeconds)
            {
                TryUnlock(SteamAchievementId.Timekeeper);
            }
        }

        private void TryUnlock(string achievementId)
        {
            if (!enableAchievements || string.IsNullOrEmpty(achievementId))
            {
                return;
            }

            if (unlocked.Contains(achievementId))
            {
                return;
            }

            unlocked.Add(achievementId);
            steam?.UnlockAchievement(achievementId);

            if (logUnlocks)
            {
                Debug.Log($"[Steam] Achievement unlocked: {achievementId}");
            }
        }
    }
}
