using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController.Tests
{
    public class CombatFeedbackCoverageRegressionTests
    {
        [Test]
        public void AudioManager_CombatEvents_BerserkEvent_TriggersConfiguredClip()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            bool oldListen = manager.listenToCombatEvents;
            AudioClip oldClip = manager.berserkStartSound;
            int beforeCount = manager.DebugPlaySfxCallCount;

            AudioClip clip = AudioClip.Create("berserk-event-test", 2205, 1, 22050, false);
            try
            {
                manager.listenToCombatEvents = true;
                manager.enabled = false;
                manager.enabled = true;
                manager.berserkStartSound = clip;

                GameEvents.BerserkStateChanged(true);

                Assert.Greater(manager.DebugPlaySfxCallCount, beforeCount);
                Assert.AreEqual(clip.name, manager.LastSfxClipName);
                Assert.AreEqual(AudioEventPriority.High, manager.LastPlayedPriority);
            }
            finally
            {
                manager.berserkStartSound = oldClip;
                manager.listenToCombatEvents = oldListen;
                manager.enabled = false;
                manager.enabled = true;
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AudioManager_CombatEvents_BossBreakEvent_TriggersConfiguredClip()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            bool oldListen = manager.listenToCombatEvents;
            AudioClip oldClip = manager.bossBreakWindowSound;
            int beforeCount = manager.DebugPlaySfxCallCount;

            AudioClip clip = AudioClip.Create("boss-break-event-test", 2205, 1, 22050, false);
            try
            {
                manager.listenToCombatEvents = true;
                manager.enabled = false;
                manager.enabled = true;
                manager.bossBreakWindowSound = clip;

                GameEvents.BossBreakWindowStart();

                Assert.Greater(manager.DebugPlaySfxCallCount, beforeCount);
                Assert.AreEqual(clip.name, manager.LastSfxClipName);
                Assert.AreEqual(AudioEventPriority.High, manager.LastPlayedPriority);
            }
            finally
            {
                manager.bossBreakWindowSound = oldClip;
                manager.listenToCombatEvents = oldListen;
                manager.enabled = false;
                manager.enabled = true;
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AudioManager_CombatEvents_KnockdownHit_UsesHighPriorityPath()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            bool oldListen = manager.listenToCombatEvents;
            AudioClip[] oldKnockdown = manager.knockdownHitSounds;
            int beforeCount = manager.DebugPlaySfxCallCount;

            AudioClip clip = AudioClip.Create("enemy-knockdown-hit-test", 2205, 1, 22050, false);
            try
            {
                manager.listenToCombatEvents = true;
                manager.enabled = false;
                manager.enabled = true;
                manager.knockdownHitSounds = new[] { clip };

                GameEvents.EnemyHit(120, Vector3.zero, EnemyHitReactionType.Knockdown);

                Assert.Greater(manager.DebugPlaySfxCallCount, beforeCount);
                Assert.AreEqual(clip.name, manager.LastSfxClipName);
                Assert.AreEqual(AudioEventPriority.High, manager.LastPlayedPriority);
                Assert.IsTrue(manager.LastUsedPriorityChannel);
            }
            finally
            {
                manager.knockdownHitSounds = oldKnockdown;
                manager.listenToCombatEvents = oldListen;
                manager.enabled = false;
                manager.enabled = true;
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AudioManager_CombatEvents_EnemyKilled_TriggersDeathClip()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            bool oldListen = manager.listenToCombatEvents;
            AudioClip[] oldDeath = manager.enemyDeathSounds;
            int beforeCount = manager.DebugPlaySfxCallCount;

            AudioClip clip = AudioClip.Create("enemy-death-event-test", 2205, 1, 22050, false);
            try
            {
                manager.listenToCombatEvents = true;
                manager.enabled = false;
                manager.enabled = true;
                manager.enemyDeathSounds = new[] { clip };

                GameEvents.EnemyKilled(EnemyType.Grunt, Vector3.zero, 10);

                Assert.Greater(manager.DebugPlaySfxCallCount, beforeCount);
                Assert.AreEqual(clip.name, manager.LastSfxClipName);
            }
            finally
            {
                manager.enemyDeathSounds = oldDeath;
                manager.listenToCombatEvents = oldListen;
                manager.enabled = false;
                manager.enabled = true;
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AudioManager_CombatEventRouting_CustomSkillRoute_IsApplied()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            bool oldListen = manager.listenToCombatEvents;
            bool oldUseRouting = manager.useAudioEventRouting;
            bool oldAutoPopulate = manager.autoPopulateAudioRoutes;
            List<CombatFeedbackAudioRoute> oldRoutes = CloneAudioRoutes(manager.audioEventRoutes);

            AudioClip clip = AudioClip.Create("skill-route-test", 2205, 1, 22050, false);
            try
            {
                manager.listenToCombatEvents = true;
                manager.useAudioEventRouting = true;
                manager.autoPopulateAudioRoutes = false;
                manager.audioEventRoutes = new List<CombatFeedbackAudioRoute>
                {
                    new CombatFeedbackAudioRoute
                    {
                        eventId = CombatFeedbackEventId.SkillUsed,
                        clips = new[] { clip },
                        priority = AudioEventPriority.Critical,
                        volume = 1f,
                        pitch = 1f,
                        playAtEventPosition = false
                    }
                };
                manager.enabled = false;
                manager.enabled = true;

                GameEvents.SkillUsed("Dash", 1f);
                Assert.IsTrue(manager.LastRouteApplied, "Custom audio routing table should be used for SkillUsed.");
                Assert.AreEqual(CombatFeedbackEventId.SkillUsed, manager.LastRoutedEvent);
                Assert.AreEqual(clip.name, manager.LastSfxClipName);
                Assert.AreEqual(AudioEventPriority.Critical, manager.LastPlayedPriority);
            }
            finally
            {
                manager.audioEventRoutes = oldRoutes;
                manager.listenToCombatEvents = oldListen;
                manager.useAudioEventRouting = oldUseRouting;
                manager.autoPopulateAudioRoutes = oldAutoPopulate;
                manager.enabled = false;
                manager.enabled = true;
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AudioManager_CombatEventRouting_StaminaDepletedRoute_IsApplied()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            bool oldListen = manager.listenToCombatEvents;
            bool oldUseRouting = manager.useAudioEventRouting;
            bool oldAutoPopulate = manager.autoPopulateAudioRoutes;
            List<CombatFeedbackAudioRoute> oldRoutes = CloneAudioRoutes(manager.audioEventRoutes);

            AudioClip clip = AudioClip.Create("stamina-route-test", 2205, 1, 22050, false);
            try
            {
                manager.listenToCombatEvents = true;
                manager.useAudioEventRouting = true;
                manager.autoPopulateAudioRoutes = false;
                manager.audioEventRoutes = new List<CombatFeedbackAudioRoute>
                {
                    new CombatFeedbackAudioRoute
                    {
                        eventId = CombatFeedbackEventId.StaminaDepleted,
                        clips = new[] { clip },
                        priority = AudioEventPriority.High,
                        volume = 1f,
                        pitch = 1f,
                        playAtEventPosition = false
                    }
                };
                manager.enabled = false;
                manager.enabled = true;

                GameEvents.StaminaDepleted();
                Assert.IsTrue(manager.LastRouteApplied, "Custom audio routing table should be used for StaminaDepleted.");
                Assert.AreEqual(CombatFeedbackEventId.StaminaDepleted, manager.LastRoutedEvent);
                Assert.AreEqual(clip.name, manager.LastSfxClipName);
                Assert.AreEqual(AudioEventPriority.High, manager.LastPlayedPriority);
            }
            finally
            {
                manager.audioEventRoutes = oldRoutes;
                manager.listenToCombatEvents = oldListen;
                manager.useAudioEventRouting = oldUseRouting;
                manager.autoPopulateAudioRoutes = oldAutoPopulate;
                manager.enabled = false;
                manager.enabled = true;
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void ScreenEffectManager_CombatEventRouting_BossBreakRoute_IsApplied()
        {
            GameObject cameraObject = new GameObject("FeedbackRoute_Camera");
            Camera camera = cameraObject.AddComponent<Camera>();

            GameObject managerObject = new GameObject("FeedbackRoute_ScreenEffectManager");
            ScreenEffectManager manager = managerObject.AddComponent<ScreenEffectManager>();
            manager.mainCamera = camera;
            manager.cameraTransform = camera.transform;
            manager.useVfxEventRouting = true;
            manager.autoPopulateVfxRoutes = false;
            manager.vfxEventRoutes = new List<CombatFeedbackVfxRoute>
            {
                new CombatFeedbackVfxRoute
                {
                    eventId = CombatFeedbackEventId.BossBreakWindowStart,
                    shakeCamera = true,
                    shakeDuration = 0.05f,
                    shakeStrength = 0.08f,
                    shakeVibrato = 6,
                    flashOverlay = false
                }
            };

            try
            {
                manager.enabled = false;
                manager.enabled = true;
                GameEvents.BossBreakWindowStart();
                Assert.IsTrue(manager.LastVfxRouteApplied, "Mapped VFX route should be applied for BossBreakWindowStart.");
                Assert.AreEqual(CombatFeedbackEventId.BossBreakWindowStart, manager.LastVfxEvent);
            }
            finally
            {
                manager.enabled = false;
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void ScreenEffectManager_CombatEventRouting_SkillRoute_IsApplied()
        {
            GameObject cameraObject = new GameObject("FeedbackRouteSkill_Camera");
            Camera camera = cameraObject.AddComponent<Camera>();

            GameObject managerObject = new GameObject("FeedbackRouteSkill_ScreenEffectManager");
            ScreenEffectManager manager = managerObject.AddComponent<ScreenEffectManager>();
            manager.mainCamera = camera;
            manager.cameraTransform = camera.transform;
            manager.useVfxEventRouting = true;
            manager.autoPopulateVfxRoutes = false;
            manager.vfxEventRoutes = new List<CombatFeedbackVfxRoute>
            {
                new CombatFeedbackVfxRoute
                {
                    eventId = CombatFeedbackEventId.SkillUsed,
                    shakeCamera = true,
                    shakeDuration = 0.06f,
                    shakeStrength = 0.1f,
                    shakeVibrato = 7,
                    flashOverlay = false
                }
            };

            try
            {
                manager.enabled = false;
                manager.enabled = true;
                GameEvents.SkillUsed("SpinSlash", 0.5f);
                Assert.IsTrue(manager.LastVfxRouteApplied, "Mapped VFX route should be applied for SkillUsed.");
                Assert.AreEqual(CombatFeedbackEventId.SkillUsed, manager.LastVfxEvent);
            }
            finally
            {
                manager.enabled = false;
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static List<CombatFeedbackAudioRoute> CloneAudioRoutes(List<CombatFeedbackAudioRoute> source)
        {
            var clone = new List<CombatFeedbackAudioRoute>();
            if (source == null)
            {
                return clone;
            }

            for (int i = 0; i < source.Count; i++)
            {
                CombatFeedbackAudioRoute route = source[i];
                if (route == null)
                {
                    continue;
                }

                AudioClip[] clips = route.clips != null ? (AudioClip[])route.clips.Clone() : new AudioClip[0];
                clone.Add(new CombatFeedbackAudioRoute
                {
                    eventId = route.eventId,
                    clips = clips,
                    volume = route.volume,
                    pitch = route.pitch,
                    priority = route.priority,
                    playAtEventPosition = route.playAtEventPosition
                });
            }

            return clone;
        }
    }
}


