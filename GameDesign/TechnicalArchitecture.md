# 深渊猎手：技术架构文档

## 系统架构总览

```
┌─────────────────────────────────────────────────────────────────┐
│                        游戏管理层 (GameManager)                   │
├─────────────────────────────────────────────────────────────────┤
│  存档系统  │  关卡管理  │  事件系统  │  配置管理  │  调试工具      │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   玩家系统    │    │   战斗系统    │    │   敌人系统    │
├───────────────┤    ├───────────────┤    ├───────────────┤
│ PlayerManager │    │ CombatManager │    │ EnemyManager  │
│ - 移动        │    │ - 攻击判定    │    │ - 群体AI      │
│ - 动画        │    │ - 连击系统    │    │ - 对象池      │
│ - 输入        │    │ - 技能系统    │    │ - 生成器      │
│ - 状态        │    │ - 特效        │    │ - 行为树      │
└───────────────┘    └───────────────┘    └───────────────┘
        │                     │                     │
        └─────────────────────┼─────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      核心支撑系统                                │
├──────────┬──────────┬──────────┬──────────┬─────────────────────┤
│   UI     │   音频   │   特效   │   物理   │   AI生成管线        │
├──────────┼──────────┼──────────┼──────────┼─────────────────────┤
│ UIManager│AudioMana-│ VFXMana-│Physics   │AIAssetPipeline      │
│ - HUD    │ ger      │ ger      │System    │ - Meshy集成        │
│ - 菜单   │ - BGM    │ - 粒子   │ - 碰撞   │ - 自动绑定         │
│ - 提示   │ - SFX    │ - 后处理 │ - 触发器 │ - 预制体生成       │
└──────────┴──────────┴──────────┴──────────┴─────────────────────┘
```

---

## 核心系统详细设计

### 1. 玩家系统 (Player)

#### 1.1 架构图

```
Player (GameObject)
├── PlayerInputHandler (输入处理)
│   └── 新Input System
├── PlayerMovement (移动控制)
│   ├── 基础移动
│   ├── 冲刺
│   ├── 跳跃
│   └── 攀爬
├── PlayerCombat (战斗系统) ← 需扩展
│   ├── 攻击判定
│   ├── 连击系统
│   └── 技能调用
├── PlayerHealth (生命值)
│   ├── HP管理
│   └── 死亡处理
├── StaminaSystem (耐力系统) ← 新增
│   ├── 耐力值
│   └── 消耗/回复
└── PlayerCamera (相机控制)
    ├── 跟随
    ├── 旋转
    └── 碰撞检测
```

#### 1.2 组件依赖关系

```csharp
// PlayerCombat.cs 扩展示例
public class PlayerCombat : MonoBehaviour
{
    [Header("Dependencies")]
    private PlayerInputHandler input;      // 输入
    private PlayerMovement movement;       // 移动
    private Animator animator;             // 动画
    private StaminaSystem stamina;         // 耐力 ← 新增
    private SkillManager skillManager;     // 技能 ← 新增
    
    [Header("Combat Stats")]
    public int baseDamage = 25;
    public float attackRange = 2f;
    public float attackAngle = 120f;
    
    [Header("Combo System")]
    public int currentCombo = 0;
    public int maxCombo = 50;
    public float comboResetTime = 1.5f;
    
    // 连击等级
    public ComboTier CurrentTier => currentCombo switch
    {
        < 10 => ComboTier.Tier1,
        < 30 => ComboTier.Tier2,
        < 50 => ComboTier.Tier3,
        _ => ComboTier.Tier4 // 狂暴模式
    };
}

public enum ComboTier
{
    Tier1, // 1-10: +10%伤害
    Tier2, // 11-30: +25%伤害
    Tier3, // 31-50: +50%伤害, 回血
    Tier4  // 50+: 狂暴模式
}
```

---

### 2. 战斗系统 (Combat)

#### 2.1 伤害计算流程

```
攻击发起
    │
    ▼
攻击前摇 (Animation Event)
    │
    ▼
范围检测 (OverlapSphere/Box)
    │
    ▼
角度检测 (Vector3.Angle)
    │
    ▼
伤害计算
├── 基础伤害
├── 连击加成 (Tier加成)
├── 技能加成
├── Buff加成
└── 暴击计算 (随机)
    │
    ▼
应用伤害 (EnemyHealth.TakeDamage)
    │
    ▼
击退效果
├── 方向计算
├── 力度计算
└── Rigidbody.AddForce
    │
    ▼
特效播放 (VFX)
音效播放 (SFX)
    │
    ▼
连击计数+1
更新UI
```

#### 2.2 技能系统架构

```csharp
// SkillBase.cs - ScriptableObject
[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/Skill")]
public class SkillBase : ScriptableObject
{
    public string skillName;
    public string description;
    public Sprite icon;
    
    [Header("Cooldown")]
    public float cooldown = 10f;
    
    [Header("Stamina")]
    public float staminaCost = 20f;
    
    [Header("Damage")]
    public int damage = 50;
    public float range = 5f;
    
    [Header("Effects")]
    public GameObject effectPrefab;
    public AudioClip castSound;
    
    // 技能执行
    public virtual void Execute(Transform caster, Vector3 target)
    {
        // 子类重写
    }
}

// 具体技能示例
public class WhirlwindSkill : SkillBase
{
    public float duration = 2f;
    public float tickRate = 0.3f;
    
    public override void Execute(Transform caster, Vector3 target)
    {
        // 播放动画
        // 开启持续伤害Coroutine
        // 播放特效
    }
}
```

#### 2.3 技能管理器

```csharp
public class SkillManager : MonoBehaviour
{
    [Header("Skills")]
    public SkillBase[] skills = new SkillBase[6]; // Q/W/E/R/T/F
    
    [Header("Cooldowns")]
    private float[] cooldownTimers = new float[6];
    
    [Header("References")]
    private PlayerInputHandler input;
    private StaminaSystem stamina;
    
    private void Update()
    {
        UpdateCooldowns();
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (input.Skill1Pressed && CanUseSkill(0)) UseSkill(0);
        if (input.Skill2Pressed && CanUseSkill(1)) UseSkill(1);
        // ... 其他技能
    }
    
    private bool CanUseSkill(int index)
    {
        if (skills[index] == null) return false;
        if (cooldownTimers[index] > 0) return false;
        if (!stamina.HasEnough(skills[index].staminaCost)) return false;
        return true;
    }
    
    private void UseSkill(int index)
    {
        var skill = skills[index];
        stamina.Consume(skill.staminaCost);
        cooldownTimers[index] = skill.cooldown;
        skill.Execute(transform, GetTargetPosition());
    }
}
```

---

### 3. 敌人系统 (Enemy)

#### 3.1 DOTS架构

```
Enemy ECS架构
├── EnemyEntity (Entity)
│   ├── LocalTransform (位置/旋转/缩放)
│   ├── EnemyData (自定义数据)
│   │   ├── maxHealth
│   │   ├── currentHealth
│   │   ├── moveSpeed
│   │   ├── attackDamage
│   │   └── enemyType
│   ├── PhysicsCollider (碰撞体)
│   └── EnemyTag (标识组件)
│
├── EnemyMovementSystem (ISystem)
│   ├── 查询所有EnemyTag实体
│   ├── Flow Field计算移动方向
│   └── 更新LocalTransform
│
├── EnemyAttackSystem (ISystem)
│   ├── 检测与玩家距离
│   ├── 攻击CD管理
│   └── 执行攻击
│
├── EnemySpawnSystem (ISystem)
│   └── 管理对象池
│
└── EnemyDeathSystem (ISystem)
    ├── 播放死亡动画
    ├── 掉落物品
    └── 回收实体
```

#### 3.2 DOTS代码示例

```csharp
// EnemyData.cs - IComponentData
public struct EnemyData : IComponentData
{
    public float maxHealth;
    public float currentHealth;
    public float moveSpeed;
    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float attackTimer;
    public int enemyType; // 0=Grunt, 1=Elite, etc.
    public bool isDead;
}

// EnemyMovementSystem.cs
public partial struct EnemyMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // 获取玩家位置
        float3 playerPos = SystemAPI.GetSingleton<PlayerData>().position;
        
        // Flow Field (简化版)
        foreach (var (transform, enemyData) in 
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemyData>>()
            .WithAll<EnemyTag>())
        {
            if (enemyData.ValueRO.isDead) continue;
            
            float3 enemyPos = transform.ValueRO.Position;
            float3 direction = math.normalize(playerPos - enemyPos);
            
            // 移动
            transform.ValueRW.Position += direction * enemyData.ValueRO.moveSpeed * deltaTime;
            
            // 面向玩家
            transform.ValueRW.Rotation = quaternion.LookRotation(direction, math.up());
        }
    }
}
```

#### 3.3 对象池设计

```csharp
public class EnemyPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public int poolSize = 200;
    public GameObject enemyPrefab;
    
    private Queue<Entity> pool = new Queue<Entity>();
    private EntityManager entityManager;
    
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Entity entity = CreateEnemyEntity();
            entityManager.SetEnabled(entity, false);
            pool.Enqueue(entity);
        }
    }
    
    public Entity Spawn(Vector3 position, EnemyType type)
    {
        if (pool.Count == 0) ExpandPool();
        
        Entity entity = pool.Dequeue();
        entityManager.SetEnabled(entity, true);
        
        // 设置位置和类型
        entityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
        
        var enemyData = entityManager.GetComponentData<EnemyData>(entity);
        enemyData.enemyType = (int)type;
        enemyData.isDead = false;
        entityManager.SetComponentData(entity, enemyData);
        
        return entity;
    }
    
    public void Despawn(Entity entity)
    {
        entityManager.SetEnabled(entity, false);
        pool.Enqueue(entity);
    }
}
```

#### 3.4 LOD AI系统

```csharp
public enum AILevel
{
    Full,       // <20m: 完整AI
    Simplified, // 20-50m: 简化AI
    Minimal     // >50m: 仅位置同步
}

public partial struct EnemyLODSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 playerPos = SystemAPI.GetSingleton<PlayerData>().position;
        
        foreach (var (transform, enemyData, aiLevel) in 
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyData>, RefRW<AILevelComponent>>())
        {
            float distance = math.distance(transform.ValueRO.Position, playerPos);
            
            // 根据距离设置AI级别
            if (distance < 20f)
                aiLevel.ValueRW.level = AILevel.Full;
            else if (distance < 50f)
                aiLevel.ValueRW.level = AILevel.Simplified;
            else
                aiLevel.ValueRW.level = AILevel.Minimal;
        }
    }
}
```

---

### 4. UI系统

#### 4.1 UI架构

```
UIManager (Persistent Singleton)
├── HUD
│   ├── HPBar (玩家血条)
│   ├── StaminaBar (耐力条)
│   ├── ComboCounter (连击计数器)
│   ├── SkillBar (技能栏 Q/W/E/R/T/F)
│   ├── Minimap (小地图)
│   └── DamageTextManager (伤害数字)
├── Menus
│   ├── MainMenu
│   ├── PauseMenu
│   ├── SettingsMenu
│   └── SkillTreeMenu
└── Popups
    ├── LevelUpPopup
    ├── ItemPickupPopup
    └── TutorialPopup
```

#### 4.2 连击计数器UI

```csharp
public class UI_ComboCounter : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI comboText;
    public Image comboGauge;
    public CanvasGroup canvasGroup;
    
    [Header("Animation")]
    public float scalePunch = 1.3f;
    public float animationDuration = 0.2f;
    
    private int currentCombo = 0;
    private float displayTimer = 0f;
    
    public void UpdateCombo(int combo)
    {
        if (combo == 0)
        {
            Hide();
            return;
        }
        
        currentCombo = combo;
        comboText.text = combo.ToString();
        
        // 动画
        comboText.transform.DOPunchScale(Vector3.one * scalePunch, animationDuration);
        
        // 颜色变化 (根据连击等级)
        comboText.color = GetTierColor(combo);
        
        // 更新进度条
        UpdateGauge(combo);
        
        displayTimer = 2f; // 显示2秒后淡出
        canvasGroup.alpha = 1f;
    }
    
    private Color GetTierColor(int combo)
    {
        return combo switch
        {
            < 10 => Color.white,
            < 30 => Color.yellow,
            < 50 => Color.red,
            _ => new Color(1f, 0f, 1f) // 紫色 (狂暴)
        };
    }
    
    private void Update()
    {
        if (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0)
            {
                canvasGroup.DOFade(0f, 0.5f);
            }
        }
    }
}
```

#### 4.3 技能栏UI

```csharp
public class UI_SkillBar : MonoBehaviour
{
    [System.Serializable]
    public class SkillSlot
    {
        public Image icon;
        public Image cooldownOverlay;
        public TextMeshProUGUI cooldownText;
        public TextMeshProUGUI keyText;
        public Button button;
    }
    
    public SkillSlot[] skillSlots = new SkillSlot[6];
    private SkillManager skillManager;
    
    void Update()
    {
        for (int i = 0; i < 6; i++)
        {
            UpdateSlot(i);
        }
    }
    
    void UpdateSlot(int index)
    {
        var slot = skillSlots[index];
        var skill = skillManager.GetSkill(index);
        
        if (skill == null)
        {
            slot.icon.gameObject.SetActive(false);
            return;
        }
        
        slot.icon.gameObject.SetActive(true);
        slot.icon.sprite = skill.icon;
        
        // 更新冷却显示
        float cooldown = skillManager.GetCooldown(index);
        if (cooldown > 0)
        {
            slot.cooldownOverlay.fillAmount = cooldown / skill.cooldown;
            slot.cooldownText.text = cooldown.ToString("F1");
            slot.icon.color = Color.gray;
        }
        else
        {
            slot.cooldownOverlay.fillAmount = 0;
            slot.cooldownText.text = "";
            slot.icon.color = Color.white;
        }
    }
}
```

---

### 5. 特效系统

#### 5.1 特效分类

```
VFXManager
├── Combat Effects
│   ├── Attack Trails (武器轨迹)
│   ├── Hit Effects (受击特效)
│   ├── Blood Effects (血液)
│   └── Death Effects (死亡)
├── Skill Effects
│   ├── Whirlwind (旋风)
│   ├── Shockwave (冲击波)
│   ├── Dash (冲刺)
│   └── Ultimate (终极技能)
├── Environment Effects
│   ├── Fog (雾效)
│   ├── Sparks (火花)
│   └── Ambient (环境)
└── UI Effects
    ├── Damage Numbers (伤害数字)
    ├── Combo Popup (连击提示)
    └── Level Up (升级特效)
```

#### 5.2 屏幕特效管理器

```csharp
public class ScreenEffectManager : MonoBehaviour
{
    public static ScreenEffectManager Instance { get; private set; }
    
    [Header("Post Processing")]
    public Volume globalVolume;
    private Vignette vignette;
    private ChromaticAberration chromatic;
    
    [Header("Camera Shake")]
    public CinemachineImpulseSource impulseSource;
    
    void Awake()
    {
        Instance = this;
        globalVolume.profile.TryGet(out vignette);
        globalVolume.profile.TryGet(out chromatic);
    }
    
    // 屏幕边缘泛红 (低血量)
    public void SetVignetteIntensity(float intensity, Color color)
    {
        vignette.intensity.value = intensity;
        vignette.color.value = color;
    }
    
    // 屏幕震动
    public void ShakeCamera(float amplitude, float duration)
    {
        impulseSource.GenerateImpulseWithForce(amplitude);
    }
    
    // 色散效果 (受击)
    public void TriggerChromatic(float intensity, float duration)
    {
        DOVirtual.Float(0, intensity, 0.1f, value => chromatic.intensity.value = value)
            .OnComplete(() => 
            {
                DOVirtual.Float(intensity, 0, duration, value => chromatic.intensity.value = value);
            });
    }
    
    // 狂暴模式特效
    public void EnterBerserkMode(float duration)
    {
        // 红色滤镜
        DOVirtual.Color(Color.white, Color.red, 0.5f, color => 
        {
            // 应用到全局光照或后处理
        });
        
        // 持续震动
        InvokeRepeating(nameof(ContinuousShake), 0, 0.1f);
        
        // 持续时间后恢复
        Invoke(nameof(ExitBerserkMode), duration);
    }
    
    void ContinuousShake()
    {
        ShakeCamera(0.5f, 0.1f);
    }
    
    void ExitBerserkMode()
    {
        CancelInvoke(nameof(ContinuousShake));
        // 恢复颜色
    }
}
```

---

### 6. 音频系统

#### 6.1 音频管理器

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;
    
    [Header("Audio Clips")]
    public AudioClip[] attackSounds;
    public AudioClip[] hitSounds;
    public AudioClip[] enemyDeathSounds;
    public AudioClip[] comboSounds; // 不同连击等级的音效
    public AudioClip berserkStartSound;
    public AudioClip bgmMain;
    
    [Header("Mixer")]
    public AudioMixer audioMixer;
    
    void Awake()
    {
        Instance = this;
    }
    
    // 播放音效 (带变调)
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }
    
    // 随机播放攻击音效
    public void PlayAttackSound(int comboTier)
    {
        if (attackSounds.Length == 0) return;
        
        // 根据连击等级选择音效
        int index = Mathf.Min(comboTier, attackSounds.Length - 1);
        AudioClip clip = attackSounds[Random.Range(0, attackSounds.Length)];
        
        // 高连击时音调更高
        float pitch = 1f + (comboTier * 0.1f);
        
        PlaySFX(clip, 1f, pitch);
    }
    
    // 播放背景音乐
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }
    
    // 音量控制
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }
}
```

---

## 数据流图

### 攻击流程数据流

```
输入层
└── InputSystem (Attack键按下)
    │
    ▼
逻辑层
├── PlayerCombat.PerformAttack()
│   ├── 检查耐力
│   ├── 检查技能CD
│   └── 触发动画
│
├── CombatCalculator.CalculateDamage()
│   ├── 基础伤害: 25
│   ├── 连击加成: ×1.5 (Tier3)
│   ├── 技能加成: ×1.2 (狂暴化Buff)
│   └── 暴击判定: ×2.0 (20%几率)
│       └── 最终伤害: 90
│
└── HitDetection.DetectEnemies()
    ├── OverlapSphere (范围2m)
    ├── 角度检测 (120度)
    └── 返回: Enemy[]
        │
        ▼
应用层
├── EnemyHealth.TakeDamage(90)
│   ├── 减伤计算 (护甲)
│   ├── HP -= 最终伤害
│   └── 检查死亡
│
├── EnemyPhysics.ApplyKnockback()
│   └── Rigidbody.AddForce()
│
├── VFXManager.SpawnHitEffect()
│   └── 粒子特效
│
├── AudioManager.PlayHitSound()
│   └── 受击音效
│
└── UI_Manager.ShowDamageNumber(90)
    └── 伤害数字
        │
        ▼
反馈层
├── 屏幕震动
├── 连击计数+1
├── 连击UI更新
└── 经验值增加
```

---

## 性能优化策略

### 1. 渲染优化

```
GPU Instancing
├── 同类敌人使用相同材质
├── 启用GPU Instancing
└── 减少Draw Call

LOD System
├── LOD0: 0-20m (高模)
├── LOD1: 20-50m (中模)
├── LOD2: 50-100m (低模)
└── Culled: >100m (不渲染)

Occlusion Culling
├── 预计算遮挡数据
├── 墙后敌人不渲染
└── 减少Overdraw

Texture Streaming
├── Mipmap链
├── 按需加载
└── 内存优化
```

### 2. AI优化

```
DOTS优化
├── Burst Compile数学运算
├── Job System并行处理
├── Entity Component Data
└── Cache Friendly

AI LOD
├── Full AI: <20m
├── Simple AI: 20-50m (仅追逐)
└── Minimal AI: >50m (仅位置同步)

Batch Update
├── 敌人分组更新
├── 非每帧更新
└── 优先级队列
```

### 3. 内存优化

```
对象池
├── 敌人对象池 (200个)
├── 特效对象池
├── 音效对象池
└── UI对象池

动态加载
├── 关卡分块加载
├── 异步资源加载
└── 资源释放策略

Asset Bundle
├── 按关卡分包
├── 按需下载
└── 缓存管理
```

---

## 工具类库

### 1. 通用工具 (Utilities)

```csharp
public static class GameUtils
{
    // 角度检测
    public static bool IsInAngle(Vector3 from, Vector3 to, Vector3 forward, float angle)
    {
        Vector3 direction = (to - from).normalized;
        float angleToTarget = Vector3.Angle(forward, direction);
        return angleToTarget <= angle * 0.5f;
    }
    
    // 随机选择 (带权重)
    public static T WeightedRandom<T>(List<T> items, List<float> weights)
    {
        float totalWeight = 0;
        foreach (var w in weights) totalWeight += w;
        
        float random = Random.Range(0, totalWeight);
        float current = 0;
        
        for (int i = 0; i < items.Count; i++)
        {
            current += weights[i];
            if (random <= current) return items[i];
        }
        
        return items[items.Count - 1];
    }
    
    // 缓动函数
    public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
    public static float EaseInOutCubic(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
}
```

### 2. 事件系统 (EventSystem)

```csharp
public static class GameEvents
{
    // 连击事件
    public static event Action<int> OnComboChanged;
    public static void ComboChanged(int combo) => OnComboChanged?.Invoke(combo);
    
    // 敌人死亡事件
    public static event Action<EnemyType, Vector3> OnEnemyKilled;
    public static void EnemyKilled(EnemyType type, Vector3 position) => OnEnemyKilled?.Invoke(type, position);
    
    // 技能释放事件
    public static event Action<string> OnSkillUsed;
    public static void SkillUsed(string skillName) => OnSkillUsed?.Invoke(skillName);
    
    // 玩家受伤事件
    public static event Action<float> OnPlayerDamaged;
    public static void PlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);
}
```

### 3. 单例模式基类

```csharp
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    
    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}

// 使用示例
public class GameManager : Singleton<GameManager>
{
    // 游戏管理逻辑
}
```

---

## 文件结构规范

```
Assets/
├── _GameDesign/
│   ├── Scripts/
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── Combat/
│   │   ├── Skills/
│   │   ├── DOTS/
│   │   ├── UI/
│   │   ├── VFX/
│   │   ├── Audio/
│   │   ├── Core/
│   │   └── Utils/
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── Enemies/
│   │   ├── Skills/
│   │   ├── Effects/
│   │   └── UI/
│   ├── ScriptableObjects/
│   │   ├── Skills/
│   │   ├── Enemies/
│   │   └── Weapons/
│   ├── Models/
│   │   ├── Characters/
│   │   ├── Enemies/
│   │   └── Environment/
│   ├── Materials/
│   ├── Textures/
│   ├── Animations/
│   ├── Audio/
│   ├── Scenes/
│   └── Resources/
│
├── Plugins/
├── ThirdParty/
└── StreamingAssets/
```

---

## 版本控制策略

### Git分支模型

```
main (稳定版本)
├── develop (开发分支)
│   ├── feature/combo-system (连击系统)
│   ├── feature/skill-system (技能系统)
│   ├── feature/dots-enemies (DOTS敌人)
│   ├── feature/ai-pipeline (AI生成管线)
│   └── feature/ui-system (UI系统)
├── release/v0.1 (测试版本)
└── hotfix/xxx (紧急修复)
```

### 提交规范

```
feat: 添加新功能
fix: 修复Bug
docs: 文档更新
style: 代码格式调整
refactor: 重构
perf: 性能优化
test: 测试相关
chore: 构建/工具相关

示例:
feat: 实现50连击狂暴机制
fix: 修复敌人穿墙问题
docs: 更新技能系统设计文档
```

---

**文档版本**: 1.0  
**最后更新**: 2026-02-07  
**维护者**: 开发团队
