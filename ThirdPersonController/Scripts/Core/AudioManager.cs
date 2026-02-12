using UnityEngine;
using System.Collections.Generic;

namespace ThirdPersonController
{
    /// <summary>
    /// 音频管理器 - 管理所有游戏音效和背景音乐
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("音频源")]
        public AudioSource musicSource;      // 背景音乐源
        public AudioSource sfxSource;        // 音效源
        public AudioSource uiSource;         // UI音效源
        public AudioSource voiceSource;      // 语音源
        
        [Header("音频混合器")]
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
        
        [Header("音效库")]
        public AudioClip[] attackSounds;
        public AudioClip[] hitSounds;
        public AudioClip[] heavyHitSounds;
        public AudioClip[] knockdownHitSounds;
        public AudioClip[] enemyDeathSounds;
        public AudioClip[] comboSounds;
        public AudioClip berserkStartSound;
        public AudioClip[] skillSounds;
        public AudioClip[] footstepSounds;

        [Header("事件监听")]
        public bool listenToCombatEvents = true;
        
        [Header("背景音乐")]
        public AudioClip[] bgmTracks;
        private int currentBgmIndex = 0;
        
        // 对象池
        private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
        private const int POOL_SIZE = 10;
        
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
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyHit -= HandleEnemyHit;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }
        
        private void InitializeAudioSources()
        {
            // 如果没有指定音频源，自动创建
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
        
        #region 背景音乐
        
        /// <summary>
        /// 播放背景音乐
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
        /// 播放下一首BGM
        /// </summary>
        public void PlayNextBGM()
        {
            currentBgmIndex = (currentBgmIndex + 1) % bgmTracks.Length;
            PlayBGM(currentBgmIndex);
        }
        
        /// <summary>
        /// 停止BGM
        /// </summary>
        public void StopBGM()
        {
            musicSource.Stop();
        }
        
        /// <summary>
        /// 暂停BGM
        /// </summary>
        public void PauseBGM()
        {
            musicSource.Pause();
        }
        
        /// <summary>
        /// 恢复BGM
        /// </summary>
        public void ResumeBGM()
        {
            musicSource.UnPause();
        }
        
        #endregion
        
        #region 音效播放
        
        /// <summary>
        /// 播放音效（带变调）
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            
            AudioSource source = GetPooledSFXSource();
            source.pitch = pitch;
            source.volume = volume * sfxVolume * masterVolume;
            source.PlayOneShot(clip);
            
            // 播放完成后回收
            StartCoroutine(ReturnToPool(source, clip.length));
        }
        
        /// <summary>
        /// 在指定位置播放音效
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            
            AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume * masterVolume);
        }
        
        /// <summary>
        /// 随机播放攻击音效
        /// </summary>
        public void PlayAttackSound(int comboTier = 0)
        {
            if (attackSounds.Length == 0) return;
            
            int index = Random.Range(0, attackSounds.Length);
            float pitch = 1f + (comboTier * 0.1f);
            
            PlaySFX(attackSounds[index], 1f, pitch);
        }
        
        /// <summary>
        /// 播放受击音效
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
            PlaySFXAtPosition(source[index], position);
        }
        
        /// <summary>
        /// 播放敌人死亡音效
        /// </summary>
        public void PlayEnemyDeathSound(Vector3 position)
        {
            if (enemyDeathSounds.Length == 0) return;
            
            int index = Random.Range(0, enemyDeathSounds.Length);
            PlaySFXAtPosition(enemyDeathSounds[index], position);
        }
        
        /// <summary>
        /// 播放连击音效
        /// </summary>
        public void PlayComboSound(int combo)
        {
            if (comboSounds.Length == 0) return;
            
            int tier = Mathf.Min(combo / 10, comboSounds.Length - 1);
            PlaySFX(comboSounds[tier], 1f, 1f + tier * 0.1f);
        }
        
        /// <summary>
        /// 播放狂暴启动音效
        /// </summary>
        public void PlayBerserkSound()
        {
            if (berserkStartSound != null)
            {
                PlaySFX(berserkStartSound, 1.2f);
            }
        }
        
        /// <summary>
        /// 播放技能音效
        /// </summary>
        public void PlaySkillSound(int skillIndex)
        {
            if (skillSounds.Length == 0 || skillIndex < 0 || skillIndex >= skillSounds.Length) return;
            
            PlaySFX(skillSounds[skillIndex]);
        }
        
        /// <summary>
        /// 播放脚步声
        /// </summary>
        public void PlayFootstep()
        {
            if (footstepSounds.Length == 0) return;
            
            int index = Random.Range(0, footstepSounds.Length);
            PlaySFX(footstepSounds[index], 0.5f);
        }
        
        /// <summary>
        /// 播放UI音效
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
        
        #region 音量控制
        
        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }
        
        /// <summary>
        /// 设置音乐音量
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
        /// 设置音效音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
        
        /// <summary>
        /// 应用音量设置
        /// </summary>
        private void ApplyVolumeSettings()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume * masterVolume;
            
            if (sfxSource != null)
                sfxSource.volume = sfxVolume * masterVolume;
            
            if (uiSource != null)
                uiSource.volume = uiVolume * masterVolume;
            
            // 更新AudioMixer
            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", Mathf.Log10(masterVolume) * 20);
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20);
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20);
            }
        }
        
        #endregion
        
        #region 对象池
        
        private AudioSource GetPooledSFXSource()
        {
            if (sfxPool.Count > 0)
            {
                return sfxPool.Dequeue();
            }
            
            // 池为空时创建新的
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
