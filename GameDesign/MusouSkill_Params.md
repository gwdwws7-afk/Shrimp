# 无双设计 Skill - 参数配置表

## 目标指标（爽感优先）
- KPM（击杀/分钟）：90–150
- 连段平均持续时长：20–35s
- 破防窗口频率：15–25s
- 击飞频率：2–4s
- 喘息时间：5–8s

## IntensityWaveDirector（默认）
| 参数 | 默认值 | 说明 |
|------|--------|------|
| targetKillsPerMinute | 120 | 强度目标 KPM |
| intensityWindowSeconds | 20 | 统计窗口 |
| minCountMultiplier | 0.85 | 低强度倍率 |
| maxCountMultiplier | 2.1 | 高强度倍率 |
| minIntervalMultiplier | 0.45 | 高强度间隔倍率 |
| maxIntervalMultiplier | 1.2 | 低强度间隔倍率 |
| waveRampPerWave | 0.12 | 波次递增 |
| maxTotalCountMultiplier | 2.4 | 总量上限 |
| comboForMaxBonus | 80 | 连击强化上限 |
| comboIntensityBonus | 0.2 | 连击强度加成 |
| musouIntensityBonus | 0.15 | 无双加成 |
| eliteRemainingScaleAtLow | 1.2 | 低强度精英延后 |
| eliteRemainingScaleAtHigh | 0.7 | 高强度精英提前 |

## 波次配比（WaveArchetypeProfile）
| WaveIndex | Grunt | Rusher | Tank | Elite | Ranged | Controller | Suicide |
|-----------|-------|--------|------|-------|--------|------------|---------|
| 0 | 1.15 | 0.95 | 0.85 | 0.9 | 0 | 0 | 0 |
| 1 | 1 | 1 | 0.95 | 1 | 0.7 | 0.4 | 0 |
| 2 | 0.95 | 0.95 | 1 | 1.05 | 0.8 | 0.7 | 0.5 |
| 3+ | 0.9 | 0.9 | 1.05 | 1.1 | 0.85 | 0.85 | 0.75 |

## 事件节奏（WaveEventTuning）
| EventType | countMultiplier | intervalMultiplier | 说明 |
|----------|-----------------|--------------------|------|
| Reinforcement | 0.95 | 0.9 | 密度略高、节奏更紧凑 |
| Chase | 0.85 | 0.8 | 高压短促 |
| HoldPoint | 0.9 | 1.1 | 留白与位移空间 |
| ProtectTarget | 1 | 1 | 标准节奏 |
