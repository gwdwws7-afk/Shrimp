using NUnit.Framework;
using UnityEngine;

namespace ThirdPersonController.Tests
{
    public class AudioPriorityRegressionTests
    {
        [Test]
        public void AudioManager_PlaySFX_HighPriority_UsesPriorityChannel()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            AudioClip clip = AudioClip.Create("priority-high", 2205, 1, 22050, false);
            try
            {
                manager.PlaySFX(clip, 1f, 1f, AudioEventPriority.High);
                Assert.AreEqual(AudioEventPriority.High, manager.LastPlayedPriority);
                Assert.IsTrue(manager.LastUsedPriorityChannel);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AudioManager_PlaySFX_NormalPriority_UsesPooledChannel()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            AudioClip clip = AudioClip.Create("priority-normal", 2205, 1, 22050, false);
            try
            {
                manager.PlaySFX(clip, 1f, 1f, AudioEventPriority.Normal);
                Assert.AreEqual(AudioEventPriority.Normal, manager.LastPlayedPriority);
                Assert.IsFalse(manager.LastUsedPriorityChannel);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AudioManager_PlaySFXAtPosition_CriticalPriority_RoutesToPriorityChannel()
        {
            AudioManager manager = AudioManager.Instance;
            Assert.NotNull(manager);

            AudioClip clip = AudioClip.Create("priority-critical", 2205, 1, 22050, false);
            try
            {
                manager.PlaySFXAtPosition(clip, Vector3.zero, 1f, AudioEventPriority.Critical);
                Assert.AreEqual(AudioEventPriority.Critical, manager.LastPlayedPriority);
                Assert.IsTrue(manager.LastUsedPriorityChannel);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }
    }
}
