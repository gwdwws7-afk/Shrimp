using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    public enum AudioEventPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// AudioManager 模块的核心实现，负责统一管理关键运行流程与对外接口。
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("设置")]
        public AudioSource musicSource; // 背景音乐播放通道，保持 BGM 独立控制。
        public AudioSource sfxSource; // 战斗/场景音效通道，承载高频短音。
        public AudioSource prioritySfxSource; // 关键反馈通道，保证高优先事件可听见。
        public AudioSource uiSource; // UI 交互音效通道，避免与战斗音效抢占。
        public AudioSource voiceSource; // 语音/台词通道，便于单独调节混音。
        
        [Header("设置")]
        public UnityEngine.Audio.AudioMixer audioMixer;
        
        [Header("音量设置")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;
        [Range(0f, 1f)]
        public float sfxVolume = 0.8f;
        [Range(0f, 1f)]
        public float uiVolume = 0.8f;
        
        [Header("设置")]
        public AudioClip[] attackSounds;
        public AudioClip[] hitSounds;
        public AudioClip[] heavyHitSounds;
        public AudioClip[] knockdownHitSounds;
        public AudioClip[] enemyDeathSounds;
        public AudioClip[] comboSounds;
        public AudioClip berserkStartSound;
        public AudioClip bossBreakWindowSound;
        public AudioClip[] skillSounds;
        public AudioClip[] footstepSounds;

        [Header("事件监听")]
        public bool listenToCombatEvents = true;
        
        [Header("背景音乐")]
        public AudioClip[] bgmTracks;
        private int currentBgmIndex = 0;
        
// 缓存容器，用于复用对象并减少运行时分配。
        private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
        private const int POOL_SIZE = 10;
        [SerializeField] private AudioEventPriority debugLastPriority = AudioEventPriority.Normal;
        [SerializeField] private bool debugLastUsedPriorityChannel = false;
        [SerializeField] private int debugPlaySfxCallCount = 0;
        [SerializeField] private string debugLastSfxClipName = string.Empty;
        public AudioEventPriority LastPlayedPriority => debugLastPriority;
        public bool LastUsedPriorityChannel => debugLastUsedPriorityChannel;
        public int DebugPlaySfxCallCount => debugPlaySfxCallCount;
        public string LastSfxClipName => debugLastSfxClipName;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            InitializeAudioSources();
            InitializeSFXPool();
        }

        private void OnEnable()
        {
            if (!listenToCombatEvents)
            {
                return;
            }

            GameEvents.OnEnemyHit += HandleEnemyHit;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnBerserkStateChanged += HandleBerserkStateChanged;
            GameEvents.OnBossBreakWindowStart += HandleBossBreakWindowStart;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyHit -= HandleEnemyHit;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnBerserkStateChanged -= HandleBerserkStateChanged;
            GameEvents.OnBossBreakWindowStart -= HandleBossBreakWindowStart;
        }
        
        private void InitializeAudioSources()
        {
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("Music Source");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (prioritySfxSource == null)
            {
                GameObject priorityObj = new GameObject("Priority SFX Source");
                priorityObj.transform.SetParent(transform);
                prioritySfxSource = priorityObj.AddComponent<AudioSource>();
                prioritySfxSource.playOnAwake = false;
            }
            
            if (uiSource == null)
            {
                GameObject uiObj = new GameObject("UI Source");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }
            
            ApplyVolumeSettings();
        }
        
        private void InitializeSFXPool()
        {
            GameObject poolParent = new GameObject("SFX Pool");
            poolParent.transform.SetParent(transform);
            
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_{i}");
                sfxObj.transform.SetParent(poolParent.transform);
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Enqueue(source);
            }
        }
        
        #region 鑳屾櫙闊充箰
        
        /// <summary>
        /// 播放BGM，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayBGM(int index)
        {
            if (bgmTracks == null || bgmTracks.Length == 0) return;
            if (index < 0 || index >= bgmTracks.Length) return;
            
            currentBgmIndex = index;
            
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }
            
            musicSource.clip = bgmTracks[index];
            musicSource.Play();
        }
        
        /// <summary>
        /// 播放Next BGM，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayNextBGM()
        {
            currentBgmIndex = (currentBgmIndex + 1) % bgmTracks.Length;
            PlayBGM(currentBgmIndex);
        }
        
        /// <summary>
        /// 停止BGM，及时收束表现避免状态叠加。
        /// </summary>
        public void StopBGM()
        {
            musicSource.Stop();
        }
        
        /// <summary>
        /// 执行 Pause BGM 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void PauseBGM()
        {
            musicSource.Pause();
        }
        
        /// <summary>
        /// 执行 Resume BGM 相关逻辑，并保证模块状态与外部调用约定一致。
        /// </summary>
        public void ResumeBGM()
        {
            musicSource.UnPause();
        }
        
        #endregion
        
        #region 闊虫晥鎾斁
        
        /// <summary>
        /// 播放SFX，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, AudioEventPriority priority = AudioEventPriority.Normal)
        {
            if (clip == null) return;
            debugPlaySfxCallCount++;
            debugLastSfxClipName = clip.name ?? string.Empty;
            debugLastPriority = priority;

            if (priority >= AudioEventPriority.High && prioritySfxSource != null)
            {
                debugLastUsedPriorityChannel = true;
                prioritySfxSource.pitch = pitch;
                prioritySfxSource.volume = volume * sfxVolume * masterVolume;
                prioritySfxSource.PlayOneShot(clip);
                return;
            }
            debugLastUsedPriorityChannel = false;
            
            AudioSource source = GetPooledSFXSource();
            source.pitch = pitch;
            source.volume = volume * sfxVolume * masterVolume;
            source.PlayOneShot(clip);
            
// 围绕 对象池 执行该步骤，用于保证流程状态与后续分支一致。
            StartCoroutine(ReturnToPool(source, clip.length));
        }
        
        /// <summary>
        /// 播放SFXAt Position，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f, AudioEventPriority priority = AudioEventPriority.Normal)
        {
            if (clip == null) return;

            if (priority >= AudioEventPriority.High)
            {
                PlaySFX(clip, volume, 1f, priority);
                return;
            }

            // Keep debug telemetry consistent with PlaySFX so tests can observe
            // normal-priority one-shot feedback routes (e.g. enemy death path).
            debugPlaySfxCallCount++;
            debugLastSfxClipName = clip.name ?? string.Empty;
            debugLastPriority = priority;
            debugLastUsedPriorityChannel = false;
            
            AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume * masterVolume);
        }
        
        /// <summary>
        /// 播放Attack Sound，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayAttackSound(int comboTier = 0)
        {
            if (attackSounds.Length == 0) return;
            
            int index = Random.Range(0, attackSounds.Length);
            float pitch = 1f + (comboTier * 0.1f);
            
            PlaySFX(attackSounds[index], 1f, pitch);
        }
        
        /// <summary>
        /// 播放Hit Sound，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayHitSound(Vector3 position)
        {
            if (hitSounds.Length == 0) return;
            
            int index = Random.Range(0, hitSounds.Length);
            PlaySFXAtPosition(hitSounds[index], position);
        }

        public void PlayHitSound(Vector3 position, EnemyHitReactionType reactionType)
        {
            AudioClip[] source = hitSounds;
            if (reactionType == EnemyHitReactionType.Knockdown && knockdownHitSounds.Length > 0)
            {
                source = knockdownHitSounds;
            }
            else if (reactionType == EnemyHitReactionType.Knockback && heavyHitSounds.Length > 0)
            {
                source = heavyHitSounds;
            }

            if (source == null || source.Length == 0)
            {
                return;
            }

            int index = Random.Range(0, source.Length);
            AudioEventPriority priority = reactionType == EnemyHitReactionType.Flinch
                ? AudioEventPriority.Normal
                : AudioEventPriority.High;
            PlaySFXAtPosition(source[index], position, 1f, priority);
        }
        
        /// <summary>
        /// 播放Enemy Death Sound，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayEnemyDeathSound(Vector3 position)
        {
            if (enemyDeathSounds.Length == 0) return;
            
            int index = Random.Range(0, enemyDeathSounds.Length);
            PlaySFXAtPosition(enemyDeathSounds[index], position);
        }
        
        /// <summary>
        /// 播放Combo Sound，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayComboSound(int combo)
        {
            if (comboSounds.Length == 0) return;
            
            int tier = Mathf.Min(combo / 10, comboSounds.Length - 1);
            PlaySFX(comboSounds[tier], 1f, 1f + tier * 0.1f);
        }
        
        /// <summary>
        /// 播放Berserk Sound，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayBerserkSound()
        {
            if (berserkStartSound != null)
            {
                PlaySFX(berserkStartSound, 1.2f, 1f, AudioEventPriority.High);
            }
        }

        public void PlayBossBreakWindowSound()
        {
            if (bossBreakWindowSound != null)
            {
                PlaySFX(bossBreakWindowSound, 1.1f, 1f, AudioEventPriority.High);
            }
        }
        
        /// <summary>
        /// 播放Skill Sound，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlaySkillSound(int skillIndex)
        {
            if (skillSounds.Length == 0 || skillIndex < 0 || skillIndex >= skillSounds.Length) return;
            
            PlaySFX(skillSounds[skillIndex], 1f, 1f, AudioEventPriority.High);
        }
        
        /// <summary>
        /// 播放Footstep，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayFootstep()
        {
            if (footstepSounds.Length == 0) return;
            
            int index = Random.Range(0, footstepSounds.Length);
            PlaySFX(footstepSounds[index], 0.5f);
        }
        
        /// <summary>
        /// 播放UISound，触发表现层反馈并保持时序一致。
        /// </summary>
        public void PlayUISound(AudioClip clip)
        {
            if (clip == null) return;
            
            uiSource.volume = uiVolume * masterVolume;
            uiSource.PlayOneShot(clip);
        }
        
        #endregion

        private void HandleEnemyHit(int damage, Vector3 position, EnemyHitReactionType reactionType)
        {
            PlayHitSound(position, reactionType);
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int expReward)
        {
            PlayEnemyDeathSound(position);
        }

        private void HandleBerserkStateChanged(bool isActive)
        {
            if (!isActive)
            {
                return;
            }

            PlayBerserkSound();
        }

        private void HandleBossBreakWindowStart()
        {
            PlayBossBreakWindowSound();
        }
        
        #region 闊抽噺鎺у埗
        
        /// <summary>
        /// 设置Master Volume，统一写入入口，便于约束副作用。
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }
        
        /// <summary>
        /// 设置Music Volume，统一写入入口，便于约束副作用。
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }
        
        /// <summary>
        /// 设置SFXVolume，统一写入入口，便于约束副作用。
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
        
        /// <summary>
        /// 应用Volume Settings，统一入口下发效果并便于后续扩展。
        /// </summary>
        private void ApplyVolumeSettings()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume * masterVolume;
            
            if (sfxSource != null)
                sfxSource.volume = sfxVolume * masterVolume;

            if (prioritySfxSource != null)
                prioritySfxSource.volume = sfxVolume * masterVolume;
            
            if (uiSource != null)
                uiSource.volume = uiVolume * masterVolume;
            
// 围绕 if 执行该步骤，用于保证流程状态与后续分支一致。
            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", Mathf.Log10(masterVolume) * 20);
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20);
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20);
            }
        }
        
        #endregion
        
        #region 瀵硅薄姹?
        
        private AudioSource GetPooledSFXSource()
        {
            if (sfxPool.Count > 0)
            {
                return sfxPool.Dequeue();
            }
            
// 围绕 游戏 执行该步骤，用于保证流程状态与后续分支一致。
            GameObject sfxObj = new GameObject("SFX_Temp");
            sfxObj.transform.SetParent(transform);
            return sfxObj.AddComponent<AudioSource>();
        }
        
        private System.Collections.IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (sfxPool.Count < POOL_SIZE)
            {
                sfxPool.Enqueue(source);
            }
            else
            {
                Destroy(source.gameObject);
            }
        }
        
        #endregion
    }
}
