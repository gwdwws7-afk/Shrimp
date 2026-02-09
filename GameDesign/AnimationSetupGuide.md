# 动画配置指南

## 快速配置（使用Unity编辑器）

### 方法1：使用Animator Setup工具

1. 打开Unity编辑器
2. 菜单：`Tools > Animation > Animator Setup`
3. 选择 `PlayerAnimatorController`
4. 从下拉列表选择动画片段
5. 点击"应用到Animator Controller"

### 方法2：手动配置

#### 步骤1：打开Animator Controller
1. 打开 `Assets/ThirdPersonController/Animations/AnimatorControllers/`
2. 双击打开 `PlayerAnimatorController.controller`

#### 步骤2：配置混合树（IdleWalkRun Blend）
1. 双击打开 `IdleWalkRun Blend` 混合树
2. 配置三个子动画：
   - Threshold 0: 选择Walk动画
   - Threshold 0.5: 选择Walk动画
   - Threshold 1: 选择Run动画

#### 步骤3：配置各个状态的Motion

| 状态 | 需要动画 | 推荐FBX来源 |
|------|---------|------------|
| Jump | Regular_Jump | fbx/Characters/sharkman/Meshy_AI_biped |
| Attack | Right_Jab / Spartan_Kick | fbx/Characters/sharkman/Meshy_AI_biped |
| Hit | (需要创建) | - |
| Death | (需要创建) | - |
| Crouch | (需要创建) | - |
| Climb | (需要创建) | - |
| Vault | (需要创建) | - |

## 可用FBX动画列表

```
fbx/Characters/sharkman/Meshy_AI_biped/:
├── Walking.fbx
├── Running.fbx
├── Regular_Jump.fbx
├── Right_Jab_from_Guard.fbx
├── Spartan_Kick.fbx
├── Lunge_Spin_Kick.fbx
└── Male_Head_Down_Charge.fbx

fbx/Characters/starman/Meshy_AI_biped/:
├── Walking.fbx
├── Running.fbx
└── Jump_with_Arms_and_Legs_Open.fbx
```

## 从FBX提取动画到Unity

### 步骤：
1. 选中FBX文件（如 `Walking.fbx`）
2. 在Inspector中设置 `Animation Type: 2` (Humanoid)
3. 展开 `Animations` 标签页
4. 点击 `Apply` 应用设置
5. 现在可以在Animator Controller中使用了

### 提示：
- 建议将提取的动画Clip重命名为简短名称
- 可以在FBX的Animations标签页中提取单独的动画

## 需要额外创建的动画

以下动画项目中没有，需要从其他来源获取或制作：

### 高优先级
1. **Idle** - 待机动画（基础，必须）
2. **Hit** - 受击动画（战斗系统需要）
3. **Death** - 死亡动画（玩家死亡需要）

### 中优先级
4. **Crouch** - 蹲下动画
5. **CrouchWalk** - 蹲行动画
6. **Climb** - 攀爬动画
7. **Vault** - 翻越动画

## 动画参数说明

Animator Controller 需要的参数：

```
Float:
  - Speed (0=Idle, 0.5=Walk, 1.0=Run)

Bool:
  - IsGrounded (是否在地面)
  - IsCrouching (是否蹲下)

Trigger:
  - Jump (跳跃)
  - Attack (攻击)
  - Hit (受击)
  - Death (死亡)
  - Climb (攀爬)
  - Vault (翻越)

Int:
  - ComboCount (连击计数)
```

## 测试动画

配置完成后：

1. 运行游戏
2. 测试移动（WASD）- 应该看到Walk/Run动画
3. 测试跳跃（Space）- 应该看到Jump动画
4. 测试攻击（鼠标左键）- 应该看到Attack动画

## 常见问题

### 动画不播放
- 检查Animator是否正确绑定到Player
- 检查Speed参数是否正确传递
- 检查动画的Loop Time是否启用

### 动画过渡不自然
- 调整Transition Duration
- 检查Exit Time设置
- 启用/禁用Has Exit Time

### 混合树不正常工作
- 检查Blend Type (1D或2D)
- 检查Threshold值
- 确保所有子动画都是Humanoid类型
