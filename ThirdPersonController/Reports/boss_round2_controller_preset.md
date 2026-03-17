# Boss Round2 Controller Prefab Preset

Generated: 2026-03-17

## Prefabs
- `Assets/Prefabs/Bosses/BOSS_Eel_Controller.prefab`
- `Assets/Prefabs/Bosses/BOSS_Guardian_Controller.prefab`

## Runtime path
- `BossSpawnPoint` now prefers controller prefabs by prototype (Eel/Guardian).
- If prefab has `BossController`, it runs controller path and disables template behavior.
- If prefab does not have `BossController`, it falls back to `BossCombatTemplate + prototype` path.

## Eel key values
- Health: `5200`
- Defense: `8`
- Break: `staggerMax=140`, `duration=4.2s`, `cooldown=10s`, `damageMultiplier=1.65`
- Weakness: `electric x1.7`
- Attack cadence: `attackInterval=2.8s`, `decisionInterval=0.65s`

Attacks:
- `eel_tail`: `82 dmg`, `4.8 range`, `4.2s cd`
- `eel_charge`: `96 dmg`, `10.5 range`, `6.4s cd`, `phase2+`
- `eel_vortex`: `74 dmg`, `aoe 6.5`, `8.6s cd`, `phase2+`
- `eel_devour`: `134 dmg`, `4.6 range`, `10.8s cd`, `phase3+`

## Guardian key values
- Health: `6200`
- Defense: `11`
- Break: `staggerMax=165`, `duration=4.8s`, `cooldown=11.5s`, `damageMultiplier=1.7`
- Weakness: `heat x1.65`
- Attack cadence: `attackInterval=3.15s`, `decisionInterval=0.75s`

Attacks:
- `guard_slam`: `92 dmg`, `aoe 5.0`, `4.8s cd`
- `guard_spray`: `72 dmg`, `aoe 7.0`, `6.2s cd`, `phase2+`
- `guard_overload`: `122 dmg`, `aoe 7.8`, `9.2s cd`, `phase2+`
- `guard_blade`: `112 dmg`, `5.6 range`, `7.2s cd`, `phase3+`
