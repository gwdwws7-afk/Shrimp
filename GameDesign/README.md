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
Month 1 (Phase 1: 核心战斗)
├── Week 1: 连击系统升级
├── Week 2: 技能系统
├── Week 3: 大规模敌人系统 (DOTS)
└── Week 4: AI生成管线

Month 2 (Phase 1 完成)
├── Week 5-6: 格挡闪避系统
├── Week 7-8: 整合测试 + 性能优化

Month 3-4 (Phase 2: 内容填充)
├── AI生成20+敌人模型
├── 5个关卡场景
├── 3个Boss设计
└── 完整成长系统

Month 5-6 (Phase 3: 打磨上线)
├── 特效/音效优化
├── UI/UX完善
├── 性能优化
└── 发布准备
```

---

## 🎯 本周目标 (Week 1)

- [ ] **Day 1-2**: 实现50连击"深渊狂暴"机制
- [ ] **Day 3**: 创建连击计数器UI
- [ ] **Day 4**: 实现屏幕特效系统
- [ ] **Day 5**: 整合测试和代码提交

**完成标准**: 可以达到50连击并触发狂暴模式

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
- ✅ 3个AI生成角色模型
- ✅ 1个AI生成场景
- ✅ 输入系统
- ✅ 动画系统

### 待开发
- 🔲 连击系统升级
- 🔲 技能系统 (6个技能)
- 🔲 大规模敌人系统 (DOTS)
- 🔲 AI生成管线自动化
- 🔲 完整UI系统
- 🔲 音效系统
- 🔲 特效系统
- 🔲 成长系统

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
3. **深度成长** - 3条技能树分支 + 装备系统
4. **程序化关卡** - 每局游戏不同体验
5. **无双连击** - 最高50+连击触发狂暴模式

---

## 📚 相关链接

- [Unity DOTS文档](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)
- [Meshy AI](https://www.meshy.ai/)
- [Tripo3D](https://trio3d.ai/)

---

**项目开始日期**: 2026-02-07  
**目标发布日期**: 2026-08-07 (6个月)  
**目标平台**: PC (Steam)

---

*祝开发顺利！*
