# 第三人称自由视角控制器 - Third Person Controller

一套完整的第三人称游戏角色控制系统，适用于 Unity 2022+ Built-in 渲染管线。

## 🎮 功能特性

### 移动系统
- ✅ **WASD** 基础移动
- ✅ **Shift** 奔跑加速
- ✅ **Ctrl** 蹲伏模式
- ✅ **Space** 跳跃（含冷却时间）
- ✅ 平滑加速/减速
- ✅ 地面检测
- ✅ 斜坡处理

### 相机系统
- ✅ 自由视角控制（鼠标）
- ✅ 相机碰撞检测
- ✅ 平滑跟随
- ✅ 滚轮缩放
- ✅ 角度限制

### 攀爬系统
- ✅ 自动检测可攀爬墙面
- ✅ 自动翻越低矮障碍
- ✅ 流畅的攀爬动画

### 战斗系统
- ✅ 无双类自由战斗（无锁定）
- ✅ 连击系统（最多3连击）
- ✅ 攻击范围检测
- ✅ 击退效果
- ✅ 粒子特效和音效

### 其他功能
- ✅ 生命值系统
- ✅ 受击反馈
- ✅ 敌人AI（巡逻、追击、攻击）
- ✅ 完整的状态机

---

## 📦 安装要求

### Unity 版本
- Unity 2022.3 LTS 或更高版本
- Built-in 渲染管线

### 必需包
1. **Input System** (新输入系统)
   - Window > Package Manager > Unity Registry > Input System
   - 安装后点击 **Yes** 重启 Unity
   
2. **AI Navigation** (导航网格)
   - Package Manager > AI Navigation
   - 用于敌人AI寻路

---

## 🚀 快速开始

### 步骤 1: 设置输入系统
1. 确保已安装 Input System 包
2. 在项目中启用新输入系统：
   - Edit > Project Settings > Player > Other Settings
   - 找到 **Active Input Handling**
   - 选择 **Input System Package (New)** 或 **Both**

### 步骤 2: 设置层（Layers）
需要在 Unity 中设置以下层：
- **Layer 6**: `Ground` - 地面检测
- **Layer 7**: `Enemy` - 敌人
- **Layer 8**: `Climbable` - 可攀爬物体

设置方法：
```
Edit > Project Settings > Tags and Layers
```

### 步骤 3: 创建玩家
1. 在场景中创建一个 Capsule 作为玩家
2. 添加组件：
   - Rigidbody
   - Capsule Collider
   - PlayerInputHandler
   - PlayerMovement
   - PlayerCamera
   - PlayerClimb
   - PlayerCombat
   - PlayerHealth
   - Animator

3. 配置 PlayerInputHandler：
   - 将 `PlayerInputActions` 赋值给 Input Actions Asset

4. 配置 PlayerMovement：
   - Ground Check: 创建一个空物体放在脚底，赋值给 Ground Check
   - Ground Layer: 设置为 Ground 层

5. 配置 PlayerCamera：
   - Target: 赋值为玩家 Transform
   - 添加相机组件到主摄像机

### 步骤 4: 创建敌人
1. 创建一个 Cube 或 Capsule 作为敌人
2. 添加组件：
   - NavMeshAgent
   - EnemyHealth
   - EnemyAI
3. 设置层为 Enemy
4. 创建巡逻点（空物体）并赋值给 EnemyAI 的 Patrol Points

### 步骤 5: 烘焙导航网格
1. 选择地面物体
2. Window > AI > Navigation
3. Object 标签页：勾选 Navigation Static
4. Bake 标签页：点击 Bake

---

## 🎮 操作指南

| 按键 | 功能 |
|------|------|
| **W/A/S/D** | 移动 |
| **鼠标** | 视角控制 |
| **左Shift** | 奔跑 |
| **左Ctrl** | 蹲伏 |
| **空格** | 跳跃 |
| **鼠标左键** | 攻击 |
| **E** | 交互/手动攀爬 |
| **鼠标滚轮** | 相机缩放 |
| **Esc** | 释放鼠标 |

---

## ⚙️ 配置参数

### PlayerMovement（移动控制）

```csharp
[Header("Movement Settings")]
public float walkSpeed = 5f;           // 行走速度
public float sprintSpeed = 10f;        // 奔跑速度
public float crouchSpeed = 2.5f;       // 蹲伏速度
public float rotationSpeed = 10f;      // 旋转速度
public float acceleration = 10f;       // 加速度
public float deceleration = 10f;       // 减速度

[Header("Jump Settings")]
public float jumpForce = 10f;          // 跳跃力度
public float jumpCooldown = 0.2f;      // 跳跃冷却
public float gravityMultiplier = 2f;   // 重力倍数

[Header("Ground Check")]
public float groundCheckRadius = 0.3f; // 地面检测半径
public LayerMask groundLayer;          // 地面层
```

### PlayerCamera（相机控制）

```csharp
[Header("Rotation Settings")]
public float mouseSensitivity = 3f;              // 鼠标灵敏度
public float minVerticalAngle = -30f;            // 最小垂直角度
public float maxVerticalAngle = 60f;             // 最大垂直角度

[Header("Distance Settings")]
public float defaultDistance = 5f;               // 默认距离
public float minDistance = 2f;                   // 最小距离
public float maxDistance = 10f;                  // 最大距离

[Header("Collision Settings")]
public LayerMask collisionLayers;                // 碰撞层
public float collisionRadius = 0.3f;             // 碰撞检测半径
```

### PlayerClimb（攀爬系统）

```csharp
[Header("Climb Detection")]
public float climbCheckDistance = 0.6f;          // 检测距离
public float climbCheckHeight = 1.5f;            // 检测高度
public float maxClimbHeight = 3f;                // 最大攀爬高度
public float minClimbHeight = 0.5f;              // 最小攀爬高度（低于此值自动翻越）
public bool autoClimb = true;                    // 自动攀爬
```

### PlayerCombat（战斗系统）

```csharp
[Header("Attack Settings")]
public float attackRange = 2f;                   // 攻击范围
public float attackAngle = 120f;                 // 攻击角度（扇形）
public float attackCooldown = 0.5f;              // 攻击冷却
public int attackDamage = 25;                    // 攻击伤害
public float attackKnockback = 5f;               // 击退力度

[Header("Combo Settings")]
public int maxComboCount = 3;                    // 最大连击数
public float comboResetTime = 1.5f;              // 连击重置时间
public float comboWindowTime = 0.8f;             // 连击窗口时间
```

---

## 🔧 高级用法

### 自定义攀爬检测
```csharp
// 修改可攀爬层
GetComponent<PlayerClimb>().climbableLayers = LayerMask.GetMask("Wall", "Rock");
```

### 监听生命值变化
```csharp
PlayerHealth health = GetComponent<PlayerHealth>();
health.OnHealthChanged += (current, max) => {
    Debug.Log($"Health: {current}/{max}");
};
health.OnDeath += () => {
    Debug.Log("Player Died!");
};
```

### 切换相机目标
```csharp
PlayerCamera cam = Camera.main.GetComponent<PlayerCamera>();
cam.SetTarget(newPlayerTransform);
```

---

## 📂 文件结构

```
Assets/ThirdPersonController/
├── Scripts/
│   ├── Player/
│   │   ├── PlayerInputHandler.cs      # 输入处理
│   │   ├── PlayerMovement.cs          # 移动控制
│   │   ├── PlayerCamera.cs            # 相机控制
│   │   ├── PlayerClimb.cs             # 攀爬系统
│   │   ├── PlayerCombat.cs            # 战斗系统
│   │   └── PlayerHealth.cs            # 生命值
│   ├── Enemy/
│   │   ├── EnemyHealth.cs             # 敌人生命
│   │   └── EnemyAI.cs                 # 敌人AI
│   └── Core/
│       ├── StateMachine.cs            # 状态机基类
│       └── Utilities.cs               # 工具类
├── Animations/
│   └── AnimatorControllers/
│       └── PlayerAnimatorController.controller
├── Inputs/
│   └── PlayerInputActions.inputactions
├── Materials/
│   ├── GroundMaterial.mat
│   └── WallMaterial.mat
├── Prefabs/
└── ThirdPersonDemoScene.unity
```

---

## 🎨 动画设置

控制器支持以下动画触发器：
- **Speed** (Float): 0-1 混合 Idle/Walk/Run
- **IsGrounded** (Bool): 是否着地
- **Jump** (Trigger): 跳跃
- **IsCrouching** (Bool): 是否蹲伏
- **Attack** (Trigger): 攻击
- **ComboCount** (Int): 连击计数 (0-3)
- **Hit** (Trigger): 受击
- **Death** (Trigger): 死亡
- **Climb** (Trigger): 攀爬
- **Vault** (Trigger): 翻越

---

## 🐛 常见问题

### Q: 角色移动不流畅/抖动
A: 检查 Rigidbody 的 Interpolation 设置为 Interpolate

### Q: 相机穿墙
A: 确保 Collision Layers 包含了所有需要检测的层

### Q: 敌人不移动
A: 确保已烘焙 Navigation Mesh，且地面标记为 Navigation Static

### Q: 攀爬功能不工作
A: 检查可攀爬物体是否在 Climbable 层，且高度在 min/max 范围内

### Q: 输入没反应
A: 确保在 PlayerInputHandler 中正确赋值了 Input Actions Asset

---

## 📝 更新日志

### v1.0.0
- ✅ 基础移动系统（WASD + 奔跑 + 蹲伏 + 跳跃）
- ✅ 第三人称相机系统
- ✅ 自动攀爬系统
- ✅ 无双类战斗系统
- ✅ 敌人AI系统
- ✅ 完整示例场景

---

## 📄 许可证

MIT License - 可自由用于个人和商业项目。

---

## 🤝 联系方式

如有问题或建议，欢迎反馈！
