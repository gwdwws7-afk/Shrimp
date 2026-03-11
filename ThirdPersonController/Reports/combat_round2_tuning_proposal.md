# Round2 Tuning Proposal (Combo / Stamina / Skills)

Updated: 2026-03-11  
Scope: first-pass value proposal only (no logic changes in this round).

## 0) Context and constraints

Current balance pressure from Round1 baseline:
- Stamina pressure is moderate, but chain offense + defense can still be sustained too long.
- Combo finisher damage can spike due stacked multipliers:
  step damage x damage curve x heavy multiplier x combo tier.
- Skill identity is not clear at data level: cooldown/cost values are mostly uniform.

Important logic constraints discovered:
- `DashAttackSkill` applies path damage repeatedly per frame to overlapping targets (frame-rate sensitive).
- `BerserkSkill` modifies `PlayerCombat.attackDamage`, but combo path uses `AttackStep.baseDamage` from combo definition.
  This means Berserk damage buff has limited effect unless fallback attack path is used.

These two are noted as follow-up logic items; this document still provides a safe value-first pass.

## 1) Stamina proposal

Data targets:
- Keep `maxStamina = 100`.
- Increase decision pressure for evade/block spam.
- Slightly slow full reset loop after mistakes.

Proposed updates (`StaminaSystem` scene values):
- `recoveryRate`: `15 -> 14`
- `recoveryDelay`: `1.00 -> 1.10`
- `heavyAttackCost`: `20 -> 22`
- `dodgeCost`: `15 -> 17`
- `blockCostPerSecond`: `5 -> 6`
- `sprintCostPerSecond`: `10 -> 11`
- `exhaustionDuration`: `2.00 -> 2.25`

Expected effect:
- Full recovery time (with delay): `7.67s -> 8.24s`
- Full-stamina dodge count: `6 -> 5`
- Full-stamina heavy count: `5 -> 4`
- Block sustain: `20.0s -> 16.7s`

## 2) Combo proposal

### 2.1 Global multipliers (`PlayerCombat`)

Proposed updates:
- `heavyDamageMultiplier`: `1.15 -> 1.10`
- `heavyKnockbackMultiplier`: `1.45 -> 1.35`
- `heavyRangeMultiplier`: `1.10 -> 1.08`
- `heavyRadiusMultiplier`: `1.10 -> 1.05`
- `tier1DamageMultiplier`: `1.10 -> 1.08`
- `tier2DamageMultiplier`: `1.25 -> 1.18`
- `tier3DamageMultiplier`: `1.50 -> 1.32`
- `berserkDamageMultiplier`: `2.00 -> 1.75`

### 2.2 Combo step values (`AttackComboDefinition`)

N1:
- `baseDamage`: `25 -> 24`
- `damageMultiplier`: `1.00 -> 1.00`
- `hitDelay`: `0.11 -> 0.10`
- `recoveryTime`: `0.28 -> 0.26`
- `comboWindowStart`: `0.08 -> 0.07`
- `comboWindowEnd`: `0.70 -> 0.68`

N2:
- `baseDamage`: `30 -> 30`
- `damageMultiplier`: `1.10 -> 1.05`
- `hitDelay`: `0.13 -> 0.12`
- `recoveryTime`: `0.30 -> 0.30`
- `comboWindowStart`: `0.10 -> 0.09`
- `comboWindowEnd`: `0.80 -> 0.78`
- `staminaCost`: `0 -> 2`

N3:
- `baseDamage`: `40 -> 38`
- `damageMultiplier`: `1.20 -> 1.15`
- `hitDelay`: `0.14 -> 0.14`
- `recoveryTime`: `0.42 -> 0.38`
- `comboWindowStart`: `0.12 -> 0.11`
- `comboWindowEnd`: `0.95 -> 0.88`
- `perTargetHitCooldown`: `0.10 -> 0.15`
- `staminaCost`: `0 -> 4`
- `additionalHitDelays`: `[0.08, 0.20, 0.32] -> [0.10, 0.24, 0.38]`

B1:
- `baseDamage`: `55 -> 46`
- `damageMultiplier`: `1.40 -> 1.25`
- `range`: `4.6 -> 4.4`
- `angle`: `240 -> 220`
- `knockback`: `12 -> 10.5`
- `hitDelay`: `0.18 -> 0.22`
- `recoveryTime`: `0.55 -> 0.60`
- `comboWindowStart`: `0.20 -> 0.24`
- `comboWindowEnd`: `0.85 -> 0.82`
- `staminaCost`: `10 -> 14`

Combo chain pressure (N1->N2->N3->B1):
- Stamina spend: `10 -> 20` (from only B1 cost to full chain cost model)

## 3) Skill proposal

Goal:
- Split skills into clearer roles (mobility, control, sustain, ultimate).
- Remove uniform `10s / 20 stamina / 50 damage` pattern.

### Dash Attack (`SKILL_DashAttack`)
- `pathDamage`: `30 -> 20`
- `hitBoxWidth`: `2.0 -> 1.4`
- `dashDistance`: `8.0 -> 7.5`
- `cooldown`: `10 -> 12`
- `staminaCost`: `20 -> 18`

Rationale:
- Mitigate current frame-rate-sensitive multi-hit behavior until logic-level hit gating is added.

### Shockwave (`SKILL_Shockwave`)
- `damage`: `50 -> 42`
- `stunDuration`: `2.0 -> 1.6`
- `cooldown`: `10 -> 9`
- `staminaCost`: `20 -> 18`
- `impactDelay`: `0.22 -> 0.18`
- `recoveryDelay`: `0.28 -> 0.24`

Rationale:
- Faster, lower-burst control tool.

### Pull (`SKILL_Pull`)
- `landingDamage`: `40 -> 46`
- `floatDuration`: `1.5 -> 1.2`
- `pullRadius`: `10 -> 9`
- `cooldown`: `10 -> 11`
- `staminaCost`: `20 -> 20` (keep)

Rationale:
- Improve reliability and payoff of delayed utility.

### Whirlwind (`SKILL_Whirlwind`)
- `tickDamage`: `15 -> 14`
- `tickRate`: `0.30 -> 0.35`
- `duration`: `2.0 -> 1.8`
- `cooldown`: `10 -> 13`
- `staminaCost`: `20 -> 24`

Rationale:
- Reduce sustained AoE uptime and stamina efficiency.

### Berserk (`SKILL_Berserk`)
- `duration`: `8 -> 7`
- `damageMultiplier`: `1.30 -> 1.20`
- `lifeRegenPerSecond`: `5 -> 4`
- `cooldown`: `10 -> 14`
- `staminaCost`: `20 -> 22`

Rationale:
- Keep identity as defensive/offensive steroid but reduce full-uptime dominance.
- Note: damage component needs logic follow-up to fully affect combo path.

### Ultimate (`SKILL_Ultimate`)
- `damage`: `50 -> 95`
- `effectRadius`: `20 -> 16`
- `stunDuration`: `3.0 -> 2.2`
- `cooldown`: `10 -> 24`
- `staminaCost`: `20 -> 40`
- `impactDelay`: `0.26 -> 0.34`
- `recoveryDelay`: `0.38 -> 0.52`

Rationale:
- Make it a true high-commit, high-impact ultimate instead of a low-risk spammable nuke.

## 4) Validation checklist after applying values

Re-run Round1 regression tests:
- `PlayerCombat_RegisterHit_IncrementsAndCapsCombo`
- `Stamina_InsufficientConsume_EntersExhaustionAndBlocksFurtherConsumption`
- `SkillManager_TryUseSkill_SuccessConsumesStaminaAndStartsCooldown`
- `ActionController_SkillInterruptedByDodge_EmitsInterruptEvent`
- `SkillTimeline_FallbackInvokesImpactAndRecoveryOnce_WithCoarseFrameWait`

Manual playtest checkpoints (15-20 minutes):
- Can player still complete one full N1->N2->N3->B1 chain without starvation in normal pacing?
- Is dodge spam naturally self-limiting in elite/Boss encounters?
- Does each skill feel role-distinct within one combat wave?
- Is ultimate now a planned button instead of rotation filler?

## 5) Follow-up logic tasks (not in this value-only round)

High priority:
- Add per-target hit gating to `DashAttackSkill` path damage (time gate or hit set).
- Route Berserk offensive buff through combo damage path (not only `attackDamage` fallback field).

Without these two fixes, final balance quality will remain capped even after value tuning.
