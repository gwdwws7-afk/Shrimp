# 深渊猎手：开发任务清单

更新: 2026-02-24
当前阶段: 内容整合 + 关卡化

---

## ✅ 已完成（近期开箱）

- [x] 统一命中判定/伤害结算管线（DamageService + HitQuery）
- [x] 普攻/技能共用事件链，连击/无双/击杀统计统一
- [x] 技能节奏落地：无敌/减伤/中断恢复
- [x] AI 眩晕接入（Shockwave/Ultimate）
- [x] 经验/等级系统 + 珍珠拾取/装备 + 天赋联动技能数值
- [x] 据点波次 UI + 清据点奖励 + 关卡事件联动
- [x] UIAutoSetup 自动生成经验条与据点波次面板
- [x] 关卡模板场景：SampleScene_Template
- [x] DOTS 基础包接入（Entities/Entities.Graphics）
- [x] Flow Field 原型（Grid/Build/Move 系统）
- [x] DOTS 敌人 Authoring/Pool 框架

---

## ⚙️ 需要配置的资产（优先）

- [ ] 创建技能 ScriptableObject 资产并绑定 SkillManager
- [ ] 填充技能特效/音效/图标（含无双/名将反馈）
- [ ] Boss 预制体放置与 Boss UI 绑定
- [ ] QuestSystem / LevelData / ChapterData 的场景接线
- [ ] AchievementSystem / StatisticsManager 的场景接线
- [ ] 伤害数字对象池化（UI_DamageText）

---

## 🗺️ 关卡制作（进行中）

- [ ] SampleScene 白模细化（名将战区域 + 分支短线）
- [ ] NavMesh Bake
- [ ] 据点节奏配置（波次/精英/名将）
- [ ] 新增更多关卡模板（复用 SampleScene_Template）

---

## 🔧 性能与稳定性

- [ ] 大规模敌人压测（50+ 同屏）
- [ ] AI 更新频率/对象池/特效 GC 压测
- [ ] DOTS/Flow Field 实战接入（场景 + 实体 + 渲染）

---

## 📝 文档同步

- [x] SessionSummary / TaskList / README 与当前实现同步
