using NUnit.Framework;
using UnityEngine;

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
    }
}
