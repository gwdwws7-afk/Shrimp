# Round1 Combat Baseline

Updated: 2026-03-11
Data sources:
- `Assets/Scenes/Level_01_TrenchRift.unity`
- `Assets/Scenes/SampleScene_Template.unity`
- `Assets/ThirdPersonController/Combat/Combos/AttackComboDefinition.asset`
- `Assets/Resources/Skills/*.asset`

## 1) Stamina Pressure Baseline (StaminaSystem)

Scene values (same in both scenes):
- `maxStamina = 100`
- `recoveryRate = 15/s`
- `recoveryDelay = 1.0s`
- `heavyAttackCost = 20`
- `dodgeCost = 15`
- `blockCostPerSecond = 5/s`
- `sprintCostPerSecond = 10/s`
- `exhaustionDuration = 2.0s`

Derived metrics:
- Heavy attack pressure: `20%` (20/100)
- Dodge pressure: `15%` (15/100)
- Full-stamina block sustain: `20.0s` (100/5)
- Full-stamina sprint sustain: `10.0s` (100/10)
- Time from 0 to full (without delay): `6.67s` (100/15)
- Time from 0 to full (with delay): `~7.67s`

## 2) Combo Gain Baseline (AttackComboDefinition)

Global:
- `comboResetTime = 1.1s`
- `inputBufferTime = 0.35s`
- `maxComboCount = 999` (asset level)

Step-level gains (`theoreticalDamage = baseDamage * damageMultiplier`,
`actionWindow = hitDelay + recoveryTime`):
- `N1`: damage `25`, window `0.39s`, theoretical DPS `64.10`, stamina `0`
- `N2`: damage `33`, window `0.43s`, theoretical DPS `76.74`, stamina `0`
- `N3`: damage `48`, window `0.56s`, theoretical DPS `85.71`, stamina `0` (plus extra hit delays)
- `B1`: damage `77`, window `0.73s`, theoretical DPS `105.48`, stamina `10`

Scene override (`PlayerCombat`):
- `maxComboCount = 50` (overrides asset-level 999)
- `berserkThreshold = 50`
- `tier1DamageMultiplier = 1.1`
- `tier2DamageMultiplier = 1.25`
- `tier3DamageMultiplier = 1.5`

## 3) Skill Value Curve Baseline (Resources/Skills)

Default loadout:
- `Whirlwind / Shockwave / Dash Attack / Berserk / Pull / Ultimate Judgment`

Current common values (nearly identical across all 6):
- `damage = 50`
- `staminaCost = 20`
- `cooldown = 10s`
- damage per stamina: `2.5`
- cooldown-normalized DPS: `5.0`

Burst estimate by action window
(`actionDuration = max(castDuration, impactDelay + recoveryDelay)`):
- `Whirlwind`: `0.50s`, `100.00 dmg/s`
- `Shockwave`: `0.50s`, `100.00 dmg/s`
- `Dash Attack`: `0.50s`, `100.00 dmg/s`
- `Berserk`: `0.50s`, `100.00 dmg/s`
- `Pull`: `0.56s`, `89.29 dmg/s`
- `Ultimate Judgment`: `0.64s`, `78.12 dmg/s`

## 4) Round1 Regression Cases (5)

Mapped test file:
- `Assets/ThirdPersonController/Tests/PlayMode/CombatRound1RegressionTests.cs`

Covered cases:
- Combo gain boundary: hit accumulation and cap behavior
- Stamina pressure boundary: insufficient stamina enters exhaustion and blocks follow-up consume
- Skill core path: successful cast consumes stamina and starts cooldown
- Interrupted flow: Skill state is interruptible by Dodge and emits interrupt event
- Low-FPS timing: timeline fallback still triggers impact/recovery exactly once
