# 无双设计 Skill - 执行清单

## 0) 准备
- 确认 `IntensityWaveDirector` 存在（自动创建或手动放置）
- 确认敌人 prefab 都绑定 `EnemyArchetypeConfigurator`
- 确认关卡 `StrongholdSequence` + `BossSpawnPoint` 已正确引用

## 1) 关卡节奏
- Stronghold_01: 节奏建立（Wave1-3）
- Stronghold_02: 强度提升（Wave3-5）
- Boss Gate: 据点完成后刷 Boss

## 2) 波次与事件
- 波次配比：按 `WaveArchetypeProfile` 执行
- 事件节奏：按 `WaveEventTuning` 执行
- 不逐关改数值，先用统一规则跑通

## 3) 奖励与成长
- 任务奖励链：QuestType → RewardTier → Chapter → Stronghold → 难度 → 关卡
- 掉落：珍珠/消耗品/深渊币随倍率生效
- 成长：天赋/珍珠/技能回转

## 4) UI 交互
- HUD：连段/资源/破防/弱点高可读
- 任务：只显示关键目标
- 奖励：突出技能/珍珠/消耗品

## 5) 验证
- 先跑 2 关（早期/中期）收集指标
- 再扩展到 10 关
