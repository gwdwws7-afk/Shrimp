# 深渊猎手：开发任务清单

## 当前阶段: Phase 1 - 核心战斗系统

---

## 🎯 Week 1: 连击系统升级

### Day 1-2: 深渊狂暴机制
- [ ] 修改PlayerCombat.cs,添加连击计数器
- [ ] 实现50连击检测
- [ ] 创建"深渊狂暴"状态效果
  - [ ] 3秒无敌
  - [ ] 攻击范围翻倍
  - [ ] 全屏红色滤镜
  - [ ] 闪电特效
- [ ] 狂暴结束后的冷却机制

**相关文件**: `ThirdPersonController/Scripts/Player/PlayerCombat.cs`

---

### Day 3: 连击UI系统
- [ ] 创建UI_ComboCounter.cs
- [ ] 实现连击数字显示
- [ ] 添加连击进度条 (Combo Gauge)
- [ ] 连击等级可视化 (1-10/11-30/31-50/50+)
- [ ] 数字跳动动画
- [ ] 高连击时屏幕震动

**相关文件**: 
- `GameDesign/Scripts/UI/UI_ComboCounter.cs` (新建)
- `GameDesign/Prefabs/UI/ComboCounter.prefab` (新建)

---

### Day 4: 屏幕特效系统
- [ ] 创建ScreenEffectManager.cs
- [ ] 实现屏幕边缘泛红效果
- [ ] 添加残影/运动模糊
- [ ] 摄像机震动效果
- [ ] 全局光照变化 (红色调)

**相关文件**: `GameDesign/Scripts/Core/ScreenEffectManager.cs` (新建)

---

### Day 5: 整合测试
- [ ] 将连击系统集成到PlayerCombat
- [ ] 测试各种连击情况
- [ ] 调整数值平衡
- [ ] Bug修复
- [ ] 提交Git: `feat: 连击系统v1.0`

---

## ⚔️ Week 2: 技能系统

### Day 1-2: 技能框架搭建
- [ ] 创建SkillBase.cs (ScriptableObject基类)
- [ ] 创建SkillManager.cs
- [ ] 实现技能数据配置
- [ ] 技能冷却管理系统
- [ ] 技能解锁检查

**相关文件**: 
- `GameDesign/Scripts/Skills/SkillBase.cs` (新建)
- `GameDesign/Scripts/Skills/SkillManager.cs` (新建)
- `GameDesign/ScriptableObjects/Skills/` (新建文件夹)

---

### Day 3: 6个技能实现
- [ ] 深海旋风 (360度旋转)
- [ ] 震荡波 (扇形冲击波)
- [ ] 深渊突袭 (瞬移攻击)
- [ ] 狂暴化 (攻速/移速Buff)
- [ ] 异种之握 (拉怪技能)
- [ ] 终极审判 (全屏AOE)

**每个技能需要**:
- 特效预制体
- 音效
- 伤害判定
- 动画触发

---

### Day 4: 技能UI
- [ ] 创建技能快捷栏 (Q/W/E/R/T/F)
- [ ] 技能图标显示
- [ ] 冷却时间显示 (环形CD)
- [ ] 技能高亮/禁用状态
- [ ] 技能提示信息

**相关文件**: `GameDesign/Scripts/UI/UI_SkillBar.cs` (新建)

---

### Day 5: 耐力系统
- [ ] 创建StaminaSystem.cs
- [ ] 集成到PlayerMovement
- [ ] 耐力消耗 (重攻击/闪避/格挡)
- [ ] 耐力回复机制
- [ ] 耐力UI条

**相关文件**: 
- `GameDesign/Scripts/Player/StaminaSystem.cs` (新建)
- 修改: `ThirdPersonController/Scripts/Player/PlayerMovement.cs`

---

## 👥 Week 3: 大规模敌人系统 (DOTS)

### Day 1-2: DOTS框架搭建
- [ ] 安装DOTS包
  - Entities
  - Physics
  - Animation
  - Burst
  - Collections
- [ ] 创建敌人Entity原型
- [ ] 设置DOTS项目配置

**相关文件**: 
- `GameDesign/Scripts/DOTS/` (新建文件夹)
- `GameDesign/Scripts/DOTS/EnemyEntityAuthoring.cs` (新建)

---

### Day 3: 对象池系统
- [ ] 创建EnemyPool.cs
- [ ] 预生成200个敌人实体
- [ ] 激活/停用管理
- [ ] 自动扩容机制

**相关文件**: `GameDesign/Scripts/DOTS/EnemyPool.cs` (新建)

---

### Day 4: 群体寻路 (Flow Field)
- [ ] 实现Flow Field算法
- [ ] 网格化管理
- [ ] 玩家目标追踪
- [ ] 避障系统

**相关文件**: 
- `GameDesign/Scripts/DOTS/FlowFieldSystem.cs` (新建)
- `GameDesign/Scripts/DOTS/GridManager.cs` (新建)

---

### Day 5: LOD AI系统
- [ ] 根据距离切换AI复杂度
  - <20m: 完整AI (攻击/闪避/技能)
  - 20-50m: 简化AI (只追逐)
  - >50m: 仅位置同步
- [ ] AI组管理 (Batch更新)

---

### Weekend: 性能测试
- [ ] 测试50敌人同屏
- [ ] Profiler分析
- [ ] 优化瓶颈
- [ ] 目标: 60fps稳定

---

## 🤖 Week 4: AI生成管线

### Day 1: 自动化脚本
- [ ] 创建AIAssetPipeline.cs
- [ ] 自动化导入Meshy生成文件
- [ ] 材质球自动设置
- [ ] 预制体自动生成

**相关文件**: `GameDesign/Editor/AIAssetPipeline.cs` (新建)

---

### Day 2-3: 生成敌人模型
目标: 20个杂兵 + 5个精英

**杂兵类型**:
- [ ] 变异螃蟹 (3种)
- [ ] 深海鱼人 (4种)
- [ ] 寄生触手怪 (3种)
- [ ] 机械水母 (3种)
- [ ] 腐蚀战士 (4种)
- [ ] 异种猎犬 (3种)

**生成流程**:
1. Meshy生成
2. AccuRIG绑定
3. Mixamo动画
4. Unity导入
5. 测试

**存储位置**: `GameDesign/Models/Enemies/`

---

### Day 4: 场景模块化
- [ ] 设计房间模板规格
- [ ] 生成10个战斗房模块
- [ ] 生成5个走廊模块
- [ ] 材质统一处理

---

### Day 5: 生成管线优化
- [ ] 批量生成脚本
- [ ] 命名自动化
- [ ] 质量检查清单
- [ ] 失败重试机制

---

## 📊 每周检查点

### Week 1 完成标准
- [ ] 可以达到50连击并触发狂暴
- [ ] UI正常显示连击数
- [ ] 屏幕特效正确播放
- [ ] 无严重Bug

### Week 2 完成标准
- [ ] 6个技能全部可用
- [ ] 技能UI正常显示
- [ ] 耐力系统工作正常
- [ ] 技能平衡性合理

### Week 3 完成标准
- [ ] 50敌人同屏 @ 60fps
- [ ] 寻路系统正常工作
- [ ] 对象池无内存泄漏
- [ ] LOD切换无明显卡顿

### Week 4 完成标准
- [ ] 20个敌人生成完成
- [ ] 自动化脚本可用
- [ ] 场景模块可用
- [ ] 生成流程文档化

---

## 🐛 Bug跟踪

| ID | 描述 | 严重度 | 状态 | 备注 |
|----|------|--------|------|------|
| | | | | |

---

## 💡 优化想法

- [ ] 考虑使用GPU Instancing渲染大量敌人
- [ ] 考虑敌人死亡时的溶解特效
- [ ] 考虑连击音效变化 (低→高连击音调变化)
- [ ] 考虑加入慢动作击杀特写

---

## 📚 学习资源

### DOTS学习
- [Unity DOTS Samples](https://github.com/Unity-Technologies/EntityComponentSystemSamples)
- [DOTS官方文档](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)

### Flow Field寻路
- [Flow Field Pathfinding](https://leifnode.com/2013/12/flow-field-pathfinding/)
- [Unity多人 RTS教程](https://www.youtube.com/watch?v=rrdRrLeLZ20)

### AI生成工具
- [Meshy AI文档](https://docs.meshy.ai/)
- [Tripo3D API](https://trio3d.ai/)

---

**最后更新**: 2026-02-07  
**更新人**: 开发团队
