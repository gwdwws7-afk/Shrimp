# Enemy AI P3 Round2 Runbook

## Goal
- Run a repeatable stress pass with target counts `100` and `150`.
- Export a single CSV with frame-time, GC, and AI throughput metrics.

## Scene Setup
- Add `EnemyPerformanceMetricsSampler` to a scene object.
- Add `EnemyPerformanceStressHarness` to the same or another scene object.
- Assign:
  - `enemyPrefab`: enemy prefab used for stress.
  - `player`: player transform (optional if tag `Player` exists).
  - `metricsSampler`: sampler reference.

## Default Steps
- `P3_100`: target `100`, warmup `8s`, sample `20s`.
- `P3_150`: target `150`, warmup `10s`, sample `24s`.

## Run
- Press `F9` to start stress sequence.
- Press `F10` to clear all active enemies.
- After all steps finish, CSV is exported by sampler.

## Output
- Default CSV file: `enemy_ai_p3_stress_metrics.csv`
- Default path: `Assets/ThirdPersonController/Reports/`
- Key columns:
  - `avg_frame_ms`, `p95_frame_ms`, `p99_frame_ms`
  - `avg_fps`, `p1_fps`
  - `avg_gc_alloc_bytes_per_frame`, `p95_gc_alloc_bytes_per_frame`
  - `avg_ai_decisions_per_s`, `p95_ai_decisions_per_s`, `avg_active_enemies`

## Acceptance Check (Internal)
- `P3_150` should not show long sustained frame spikes compared with the previous baseline.
- `avg_active_enemies` should be close to configured step target.
