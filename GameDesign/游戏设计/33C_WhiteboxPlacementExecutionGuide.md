# 33C 白盒摆关执行说明（非直线）

## 文件用途
- `33A_WhiteboxLayoutSpec_10Levels_FinalPolish.csv`：10关拓扑与锚点坐标规格。
- `33B_EnemySpawnPointPlan_10Levels_FinalPolish.csv`：按据点/门控的敌人扇区点位与波次窗口。

## 摆关顺序（必须按序）
1. 先放锚点：`Entry -> Hub -> Stronghold_A -> Transition -> Stronghold_B -> BossGate -> BossArena`。
2. 再搭主路：按 `main_lane_width_m` 建主推进通道。
3. 再搭支路：按 `risk_shortcut_path` 建高风险短路。
4. 再补战斗面：按 `arena_a_size_m / arena_b_size_m / boss_pre_room_size_m` 划战斗区。
5. 最后放敌点：导入 `33B` 中对应关卡扇区点位。

## 反直线硬规则
- 每关 `loop_count >= 2`，`min_turns >= 4`。
- 任一据点必须有左右至少两个夹击扇区（`LeftFlank + RightFlank`）。
- `BossGate` 前必须有双截击点（`GateLeftIntercept + GateRightIntercept`，L03+）。
- 主路与支路交汇处必须可视化（地标/灯光/高差其一）。

## 快速验收
- 从入口到强度主峰，不出现超过 `18s` 的无战斗空跑。
- 重开到首个主战点不超过 `35s`。
- 玩家在每个据点都能找到至少 1 个回整位（低压区）。
