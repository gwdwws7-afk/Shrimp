# 敌人AI P0回归执行清单 + 首轮参数表（Grunt/Rusher/Tank/Elite）

更新时间：2026-03-11
适用代码基线：EnemyAI 已接入 Dodge/Block/Charge/Flee + 令牌统计
配套填表文件：`C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_round1_fill_sheet.csv`

## 1) P0 回归执行清单（可逐项勾选）

### A. 执行前准备
- [ ] 场景：`C:\test\Shrimp\Assets\Scenes\Level_01_TrenchRift.unity`
- [ ] 敌人预制体已绑定 ArchetypeConfigurator（Angler/DeepseaFish/Squid/Hermit/Mantis/SeaUrchin）
- [ ] 战斗/技能回归通过（`CombatRound1RegressionTests`）
- [ ] AI回归通过（`EnemyAIP0P1RegressionTests`）
- [ ] Inspector 观察项：
  - `EnemyAI.debugCurrentState / debugStateElapsed / debugDecisionCount / debugTokenAcquireSuccessCount / debugTokenAcquireFailCount`
  - `EnemyCrowdCoordinator.TokenRequestCount / TokenGrantedCount / TokenRejectedCount / TokenUtilization / ActiveAttackersCount`

### B. 必跑 5 个用例（每项建议 3 轮）

#### 用例1：令牌饱和与公平
- [ ] 条件：同屏 12+ 敌人，`maxActiveAttackers=3`，玩家原地防守 30 秒
- [ ] 观察：`TokenRejectedCount` 会上升，但 `TokenUtilization` 长时间应在 `0.35~1.0`
- [ ] 通过标准：无长期“全员绕圈不出手”；无令牌卡死

#### 用例2：被打断清理（眩晕/抑制）
- [ ] 条件：敌人处于 Attack/Charge/Block/Dodge/Flee 任一状态时触发 `ApplyStun` 或 `SetSuppressed(true)`
- [ ] 观察：瞬时状态清零，`hasAttackToken` 释放，Block 防御加成回滚
- [ ] 通过标准：不出现“被晕仍持续冲锋/格挡防御不回退”

#### 用例3：低血逃离优先（仅对启用 canFlee 的敌人）
- [ ] 条件：将目标敌人生命压到阈值以下
- [ ] 观察：状态优先切换至 `Flee`，持续 `fleeDuration` 后退出
- [ ] 通过标准：低血阶段不再继续强攻

#### 用例4：冲锋触发窗口与命中一致性
- [ ] 条件：在 `chargeMinDistance~chargeMaxDistance` 内反复拉扯
- [ ] 观察：仅在窗口内触发 Charge，且一次 Charge 仅结算一次主命中
- [ ] 通过标准：不出现“冲锋无限多段命中”

#### 用例5：格挡行为与收益闭环
- [ ] 条件：持续对 Tank/Elite 输出，触发 Block
- [ ] 观察：Block 期间伤害明显降低，结束后恢复正常
- [ ] 通过标准：`blockDefenseBonus` 生效且可回滚，不残留

### C. P0 结论记录模板
- [ ] 记录样式（建议每个 archetype 一行）：

| Archetype | 平均决策间隔(s) | Token拒绝率 | 主要状态占比 | 异常次数 | 结论 |
|---|---:|---:|---|---:|---|
| Grunt |  |  |  |  |  |
| Rusher |  |  |  |  |  |
| Tank |  |  |  |  |  |
| Elite |  |  |  |  |  |

---

## 2) 首轮参数表（Round1，按 Archetype）

说明：
- 这是“首轮试跑值”，优先让行为特征分明、可回归。
- 完整字段级对照见 CSV：
  `C:\test\Shrimp\Assets\ThirdPersonController\Reports\enemy_ai_round1_fill_sheet.csv`

### Grunt（密度压迫，禁用高级动作）

| 字段 | 当前 | Round1建议 |
|---|---:|---:|
| chaseSpeed | 4.0 | 4.1 |
| stoppingDistance | 1.4 | 1.35 |
| attackCooldown | 1.55 | 1.5 |
| attackWindup | 0.4 | 0.38 |
| attackRecovery | 0.5 | 0.46 |
| canDodge / canBlock / canCharge / canFlee | false / false / false / false | false / false / false / false |
| dodgeChance / blockChance / chargeChance | 0.05 / 0.05 / 默认0.2 | 0.02 / 0.02 / 0 |

资产：`C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Grunt.asset`

### Rusher（高机动突进）

| 字段 | 当前 | Round1建议 |
|---|---:|---:|
| chaseSpeed | 5.8 | 6.0 |
| stoppingDistance | 0.95 | 0.9 |
| attackCooldown | 1.1 | 1.0 |
| dodgeChance | 0.28 | 0.34 |
| dodgeDistance / dodgeDuration / dodgeCooldown | 默认2.4 / 默认0.28 / 默认2.2 | 2.8 / 0.22 / 1.8 |
| chargeChance | 默认0.2 | 0.4 |
| chargeSpeed / chargeWindup | 11 / 0.3 | 12 / 0.26 |
| chargeMinDistance / chargeMaxDistance | 默认1.8 / 默认4.2 | 1.6 / 4.8 |
| chargeDuration / chargeCooldown | 默认0.45 / 默认3.5 | 0.36 / 2.8 |
| canBlock / canFlee | false / false | false / false |

资产：`C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Rusher.asset`

### Tank（重装前压，格挡主导）

| 字段 | 当前 | Round1建议 |
|---|---:|---:|
| chaseSpeed | 2.4 | 2.4 |
| stoppingDistance | 2.2 | 2.4 |
| attackCooldown | 2.4 | 2.2 |
| attackWindup / attackRecovery | 0.65 / 0.7 | 0.6 / 0.78 |
| blockChance | 0.35 | 0.52 |
| blockDuration / blockCooldown | 默认0.45 / 默认2.8 | 0.72 / 2.3 |
| blockDefenseBonus | 默认6 | 10 |
| canDodge / canCharge / canFlee | false / false / false | false / false / false |

资产：`C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Tank.asset`

### Elite（全能高压）

| 字段 | 当前 | Round1建议 |
|---|---:|---:|
| chaseSpeed | 5.2 | 5.4 |
| stoppingDistance | 1.2 | 1.1 |
| attackCooldown | 1.2 | 1.15 |
| dodgeChance | 0.3 | 0.35 |
| dodgeDistance / dodgeDuration / dodgeCooldown | 默认2.4 / 默认0.28 / 默认2.2 | 2.6 / 0.24 / 1.9 |
| blockChance | 0.25 | 0.32 |
| blockDuration / blockCooldown | 默认0.45 / 默认2.8 | 0.58 / 2.4 |
| blockDefenseBonus | 默认6 | 8 |
| chargeChance | 默认0.2 | 0.32 |
| chargeSpeed / chargeWindup | 11.5 / 0.35 | 12.2 / 0.3 |
| chargeMinDistance / chargeMaxDistance | 默认1.8 / 默认4.2 | 1.8 / 5.0 |
| chargeDuration / chargeCooldown | 默认0.45 / 默认3.5 | 0.4 / 3.1 |
| canFlee | false | false |

资产：`C:\test\Shrimp\Assets\GameDesign\Data\EnemyArchetype_Elite.asset`

---

## 3) 填资产建议顺序（防止互相干扰）

- [ ] 先只填 `canX + Chance + Cooldown`（验证状态切换）
- [ ] 再填 `Duration / Distance / Windup`（验证手感）
- [ ] 最后微调 `attackCooldown/Recovery`（统一节奏）
- [ ] 每次仅改一个 archetype，跑完 5 用例再切下一个

