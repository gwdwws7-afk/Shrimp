# 🎮 深渊猎手：异种肃清 - 游戏设计文档

## 📁 文件夹内容

| 文件 | 描述 | 用途 |
|------|------|------|
| **GameDesignDocument.md** | 完整游戏策划案 | 游戏整体设计和规划 |
| **TaskList.md** | 开发任务清单 | 每周具体任务和进度跟踪 |
| **TechnicalArchitecture.md** | 技术架构文档 | 代码结构和技术实现细节 |

---

## 🚀 快速开始

### 1. 查看整体规划
阅读 `GameDesignDocument.md` 了解：
- 游戏核心概念和玩法
- 系统设计方案
- AI生成管线
- 开发里程碑

### 2. 查看当前任务
阅读 `TaskList.md` 了解：
- Week 1-4 具体任务
- 每日工作内容
- 完成标准

### 3. 查看技术实现
阅读 `TechnicalArchitecture.md` 了解：
- 系统架构设计
- 代码示例
- 数据流图
- 优化策略

---

## 📅 开发路线图

```
Phase A (内容闭环)
├── 10关内容与据点事件
├── 10个独立Boss (3-4技能 + 2阶段)
└── 任务链分段/失败/引导

Phase B (系统打磨)
├── 战斗手感/命中反馈
├── UI/UX一致性
└── 100-150敌人@60fps

Phase C (Steam发布)
├── 成就/云存档/卡牌
├── 手柄适配
└── 双语本地化
```

---

## 🎯 本周目标 (Week 1)

- [ ] 关卡模板固化（10关结构/事件组合）
- [ ] 2个Boss原型（3-4技能+2阶段）
- [ ] 任务链分段模板与引导提示验证
- [ ] 手柄与双语基础流程检查

**完成标准**: 可跑通2关+2 Boss 的完整流程

---

## 💻 技术栈

- **引擎**: Unity 2022.3 LTS
- **渲染管线**: Built-in (可升级到URP)
- **脚本**: C#
- **输入系统**: Legacy Input (旧 Input，已使用)
- **动画**: Animator
- **大规模敌人**: Mono + 对象池 + AI 降频（现用），DOTS 规划
- **物理**: Unity Physics
- **特效**: ParticleSystem + ScreenEffectManager
- **UI**: uGUI（现用），UI Toolkit 规划
- **版本控制**: Git

---

## 🤖 AI工具链

| 用途 | 工具 | 费用 |
|------|------|------|
| 3D角色生成 | Meshy AI / Tripo3D | ~$50/月 |
| 场景生成 | Meshy AI | 包含 |
| 贴图生成 | Stable Diffusion (本地) | 免费 |
| 音效生成 | ElevenLabs | ~$5/月 |
| 音乐生成 | Suno / Udio | ~$10/月 |
| 代码辅助 | GitHub Copilot / Cursor | ~$10/月 |

---

## 📊 项目统计

### 已有资源
- ✅ 第三人称控制器 (完整)
- ✅ 敌人基础AI (巡逻/追击/攻击)
- ✅ 战斗管线/连击/无双/技能
- ✅ 据点波次 + 事件型波次
- ✅ 成长系统(经验/天赋/珍珠)
- ✅ 战前准备/战后结算

### 待开发
- 🔲 10关内容 + 10个独立Boss
- 🔲 Boss机制/特效/音效/UI
- 🔲 Steam功能(成就/云存档/卡牌)
- 🔲 手柄完整适配
- 🔲 双语本地化
- 🔲 性能目标(100-150敌人@60fps)

---

## 📞 协作说明

### Git提交规范
```
feat: 添加新功能
fix: 修复Bug
docs: 文档更新
refactor: 重构代码
perf: 性能优化
```

### 文件命名规范
```
脚本: PlayerCombat.cs, EnemyAI.cs
预制体: ENM_Grunt_01.prefab
材质: MAT_Enemy_Grunt_01.mat
贴图: TEX_Enemy_Grunt_01_Albedo.png
动画: ANM_Player_Attack_01.anim
```

---

## 🎮 游戏特色

1. **割草式战斗** - 单次攻击可击中10+敌人
2. **AI生成内容** - 所有敌人外观由AI实时生成变异
3. **深度成长** - 天赋/珍珠/三条流派
4. **事件型据点** - 援军/追击/保护目标等变体
5. **每关Boss** - 10关独立Boss压轴

---

## 📚 相关链接

- [Unity DOTS文档](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)
- [Meshy AI](https://www.meshy.ai/)
- [Tripo3D](https://trio3d.ai/)

---

**项目开始日期**: 2026-02-07  
**目标发布日期**: TBD  
**目标平台**: PC (Steam)

---

*祝开发顺利！*
