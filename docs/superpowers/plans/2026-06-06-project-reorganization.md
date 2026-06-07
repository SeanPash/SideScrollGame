# Project Reorganization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize custom Unity assets into logical subfolders, strip all emoji and em-dash markers from code comments and debug strings, and add commit hygiene rules to CLAUDE.md.

**Architecture:** All 64 custom C# scripts move from `Assets/` root to `Assets/Scripts/{Player,Bosses,Enemies,Combat,Projectiles,Interfaces,UI}/`. All custom prefabs move from `Assets/Enemies/` to `Assets/Prefabs/{Bosses,Enemies,Projectiles,Effects,Player,Zones}/`. Loose animation files in `Assets/Enemies/` move to `Assets/Animations/`. Every file move also moves its paired `.meta` file so Unity GUIDs are preserved and no scene or prefab references break. Third-party asset pack folders stay in place.

**Tech Stack:** Unity 2024 (6000.0.31f1), C#, PowerShell for file operations

---

## Critical: How to Move Unity Files Safely

Unity tracks assets by GUID stored in `.meta` files. Every asset has a sibling `.meta` with the same name plus `.meta`. When moving files outside the Editor, **always move the `.meta` alongside the asset** or Unity loses the reference.

The PowerShell helper used in every move task:
```powershell
function Move-UA($src, $dst) {
    New-Item -ItemType Directory -Force (Split-Path $dst -Parent) | Out-Null
    Move-Item $src $dst -Force
    if (Test-Path "$src.meta") { Move-Item "$src.meta" "$dst.meta" -Force }
}
```

Run this function definition first in any PowerShell session before executing move commands.

**Verification pattern for every task:** After moves, run:
```powershell
# Confirm file exists at new path and .meta is alongside it
Test-Path "Assets\Scripts\Player\WarriorController.cs"
Test-Path "Assets\Scripts\Player\WarriorController.cs.meta"
# Confirm old path is gone
-not (Test-Path "Assets\WarriorController.cs")
```

---

### Task 1: Update .gitignore and CLAUDE.md

**Files:**
- Modify: `.gitignore`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Add CLAUDE.md to .gitignore**

Add at the bottom of `.gitignore`:
```
# Local AI context file
CLAUDE.md
```

- [ ] **Step 2: Add commit rules section to CLAUDE.md**

Append to `CLAUDE.md`:
```markdown
## Commit Guidelines

- Never add yourself as co-author in commit messages
- Never use emojis in code comments or commit messages
- Never use em dashes (—) in comments or strings; use plain hyphens (-) instead
```

- [ ] **Step 3: Commit**
```
git add .gitignore CLAUDE.md
git commit -m "chore: add CLAUDE.md to gitignore and document commit hygiene rules"
```

---

### Task 2: Clean emoji and em-dash comments - combat/parry files

**Files:**
- Modify: `Assets/ParrySystem.cs`
- Modify: `Assets/ParryHitbox.cs`
- Modify: `Assets/BossGolemStun.cs`
- Modify: `Assets/RegularGolemStun.cs`
- Modify: `Assets/SamuraiProjectile.cs`

- [ ] **Step 1: Edit ParrySystem.cs** — remove 5 emoji comment markers and 2 em dashes

Old → New (use Edit tool, one call per change):

```
// ✅ Martial Hero Phase 1
```
→
```
// Martial Hero Phase 1
```

```
// ✅ Martial Hero Phase 2
```
→
```
// Martial Hero Phase 2
```

```
// ✅ Boss Golem — always accepts parry
```
→
```
// Boss Golem - always accepts parry
```

```
// ✅ Crab Boss — Only parryable if not flashing blue
```
→
```
// Crab Boss - only parryable if not flashing blue
```

```
// ✅ Samurai Knife Projectile
```
→
```
// Samurai knife projectile
```

- [ ] **Step 2: Edit ParryHitbox.cs**

```
Debug.Log("ParryHitbox enabled — waiting for manual trigger.");
```
→
```
Debug.Log("ParryHitbox enabled - waiting for manual trigger.");
```

- [ ] **Step 3: Edit BossGolemStun.cs**

```
Debug.Log("Parry ignored — on cooldown or no bossBehavior.");
```
→
```
Debug.Log("Parry ignored - on cooldown or no bossBehavior.");
```

```
Debug.Log("Parry threshold reached — applying stun.");
```
→
```
Debug.Log("Parry threshold reached - applying stun.");
```

- [ ] **Step 4: Edit RegularGolemStun.cs**

```
stunIconInstance.transform.SetParent(null); // not a child — follows via script
```
→
```
stunIconInstance.transform.SetParent(null); // follows via script, not parented
```

- [ ] **Step 5: Edit SamuraiProjectile.cs**

```
// No damage applied — just destroy the projectile after reflection
```
→
```
// no damage applied - destroy the projectile on reflection
```

- [ ] **Step 6: Commit**
```
git add Assets/ParrySystem.cs Assets/ParryHitbox.cs Assets/BossGolemStun.cs Assets/RegularGolemStun.cs Assets/SamuraiProjectile.cs
git commit -m "chore: remove emoji and em-dash markers from combat script comments"
```

---

### Task 3: Clean emoji and em-dash comments - Crab boss files

**Files:**
- Modify: `Assets/CrabBossBehaviorPhase1.cs`
- Modify: `Assets/CrabBossBehaviorPhase2.cs`
- Modify: `Assets/CrabBossHealth.cs`

- [ ] **Step 1: Edit CrabBossBehaviorPhase1.cs**

```
// ✅ Parry window duration: only 0.25s (ends slightly early)
```
→
```
// parry window is 0.25s
```

```
Debug.Log("[CrabBoss] Parry attempt failed — not in a valid state.");
```
→
```
Debug.Log("[CrabBoss] Parry attempt failed - not in a valid state.");
```

- [ ] **Step 2: Edit CrabBossBehaviorPhase2.cs**

```
// ✅ At the VERY END of the coroutine:
```
→
```
// at the end of the coroutine:
```

```
verticalX[1] = camX - camWidth / 8f;          // ⬅️ Move closer to center
```
→
```
verticalX[1] = camX - camWidth / 8f;          // move closer to center
```

```
verticalX[2] = camX + camWidth / 8f;          // ➡️ Move closer to center
```
→
```
verticalX[2] = camX + camWidth / 8f;          // move closer to center
```

```
horizontalY[0] = camY - camHeight / 2f + 1.8f;   // ⬆️ bottom line moved up from +1.2f to +1.8f
```
→
```
horizontalY[0] = camY - camHeight / 2f + 1.8f;   // bottom line moved up from +1.2f to +1.8f
```

```
horizontalY[2] = camY + camHeight / 2f - 2.2f;   // ⬇️ top line moved down from -1.5f to -2.2f
```
→
```
horizontalY[2] = camY + camHeight / 2f - 2.2f;   // top line moved down from -1.5f to -2.2f
```

- [ ] **Step 3: Edit CrabBossHealth.cs**

```
// ✳️ Activate death handler
```
→
```
// activate death handler
```

- [ ] **Step 4: Commit**
```
git add Assets/CrabBossBehaviorPhase1.cs Assets/CrabBossBehaviorPhase2.cs Assets/CrabBossHealth.cs
git commit -m "chore: remove emoji and em-dash markers from crab boss comments"
```

---

### Task 4: Clean emoji and em-dash comments - MartialHero files

**Files:**
- Modify: `Assets/MartialHeroBehavior.cs`
- Modify: `Assets/MartialHeroPhase2.cs`
- Modify: `Assets/MartialHeroHealth.cs`
- Modify: `Assets/MartialHeroStun.cs`
- Modify: `Assets/MartialBossTriggerRoom.cs`

- [ ] **Step 1: Edit MartialHeroBehavior.cs** — 10 changes

```
        // ✅ Raycast down to check for ground at teleport spot
```
→
```
        // raycast down to check for ground at teleport position
```

```
            // ❌ No ground — don't teleport, just reappear and throw knife
            Debug.Log("[MartialHero] No ground found — throwing knife from current position.");
```
→
```
            // no ground - don't teleport, throw knife from current position
            Debug.Log("[MartialHero] No ground found - throwing knife from current position.");
```

```
            // ✅ Safe to teleport
            finalTarget = new Vector3(targetX, groundCheck.point.y + 1f, transform.position.z);
            transform.position = finalTarget;
        }

        // Reappear
        spriteRenderer.enabled = true;
        FacePlayer();
        yield return new WaitForSeconds(0.3f);

        // Throw knife
```
→
```
            // ground found - teleport
            finalTarget = new Vector3(targetX, groundCheck.point.y + 1f, transform.position.z);
            transform.position = finalTarget;
        }

        // Reappear
        spriteRenderer.enabled = true;
        FacePlayer();
        yield return new WaitForSeconds(0.3f);

        // Throw knife
```

```
        // ❌ No ground — reappear in place and throw knife instead
        Debug.Log("[MartialHero] ChargeAttack failed — no ground, switching to knife throw");
```
→
```
        // no ground - reappear in place and throw knife
        Debug.Log("[MartialHero] ChargeAttack failed - no ground, switching to knife throw");
```

```
    // ✅ Safe to teleport
    float targetY = groundCheck.point.y + 1f;
```
→
```
    // ground found - teleport
    float targetY = groundCheck.point.y + 1f;
```

```
    // 🔹 Flash blue briefly BEFORE attack
```
→
```
    // flash blue before attack
```

```
    // ▶️ Now play attack animation
```
→
```
    // play attack animation
```

```
        Debug.Log("[MartialHero] Ignored parry — not attacking.");
```
→
```
        Debug.Log("[MartialHero] Ignored parry - not attacking.");
```

- [ ] **Step 2: Edit MartialHeroPhase2.cs** — 25+ changes (process all emoji and em-dash lines)

```
spear.GetComponent<SpearProjectile>().Launch(origin, new Vector2(x, y)); // ✅ No arcHeight
```
→
```
spear.GetComponent<SpearProjectile>().Launch(origin, new Vector2(x, y));
```

```
        float x = Mathf.Lerp(startX, endX, Mathf.Pow(t, 1f)); // ✅ forces early spears to move less in X
```
→
```
        float x = Mathf.Lerp(startX, endX, Mathf.Pow(t, 1f));
```

```
        int spearCount = 16;  // ✅ Fewer spears = more spacing
```
→
```
        int spearCount = 16;
```

```
    // ✅ Use your assigned spawn point
    Vector2 origin = spearSpawnPointRight.position;
```
→
```
    Vector2 origin = spearSpawnPointRight.position;
```

```
    // ✅ Adjust these to extend and shift the landing zone
    float extraLeft = 4f;     // extend more to left
    float extraRight = -0.1f; // shift start slightly right
```
→
```
    float extraLeft = 4f;
    float extraRight = -0.1f;
```

```
        // 🎲 Randomly choose 0 or 1, but not the same as lastRoll
```
→
```
        // pick randomly, avoid repeating the last roll
```

```
            // 🟦 LEFT spear (moves rightward past center and hits ground)
```
→
```
            // left spear moves rightward past center
```

```
            // 🟥 RIGHT spear (moves leftward past center and hits ground)
```
→
```
            // right spear moves leftward past center
```

```
        // 🕹️ Get a new random roll that is NOT the same as the last
```
→
```
        // pick a new roll that differs from the last
```

```
        // 🗡️ Trigger chosen spear attack
```
→
```
        // trigger chosen spear attack
```

```
        // ✅ Update cooldown
        lastSpearAttackTime = Time.time;
```
→
```
        lastSpearAttackTime = Time.time;
```

```
    // ❌ No parry window
    currentAttackPhase = "GroundSlam";
```
→
```
    // not parryable
    currentAttackPhase = "GroundSlam";
```

```
        // ✅ Raycast down to check for ground at teleport spot
        Vector2 rayOrigin = new Vector2(targetX, transform.position.y + 2f);
        RaycastHit2D groundCheck = Physics2D.Raycast(rayOrigin, Vector2.down, 5f, LayerMask.GetMask("Ground"));

        Vector3 finalTarget;

        if (!groundCheck.collider)
        {
            // ❌ No ground — don't teleport, just reappear and throw knife
            Debug.Log("[MartialHero] No ground found — throwing knife from current position.");
            finalTarget = transform.position;
        }
        else
        {
            // ✅ Safe to teleport
            finalTarget = new Vector3(targetX, groundCheck.point.y + 1f, transform.position.z);
            transform.position = finalTarget;
        }

        // Reappear
        spriteRenderer.enabled = true;
        FacePlayer();
        yield return new WaitForSeconds(0.3f);

        // Throw knife
        animator.Play("Attack3");
        yield return new WaitForSeconds(0.3f);

        Transform spawnPoint = transform.localScale.x > 0 ? rightSpawnPoint : leftSpawnPoint;
        GameObject knife = Instantiate(knifeProjectilePrefab, spawnPoint.position, Quaternion.identity);
        knife.GetComponent<SamuraiProjectile>().SetDirection(transform.localScale.x > 0 ? Vector2.right : Vector2.left);

        yield return new WaitForSeconds(0.3f);
        animator.Play("Idle");
        isAttacking = false;

        StartCoroutine(KnifeThrowCooldown());
        yield return new WaitForSeconds(1f);

    }
```
→
```
        // raycast down to check for ground at teleport position
        Vector2 rayOrigin = new Vector2(targetX, transform.position.y + 2f);
        RaycastHit2D groundCheck = Physics2D.Raycast(rayOrigin, Vector2.down, 5f, LayerMask.GetMask("Ground"));

        Vector3 finalTarget;

        if (!groundCheck.collider)
        {
            // no ground - throw knife from current position
            Debug.Log("[MartialHero] No ground found - throwing knife from current position.");
            finalTarget = transform.position;
        }
        else
        {
            // ground found - teleport
            finalTarget = new Vector3(targetX, groundCheck.point.y + 1f, transform.position.z);
            transform.position = finalTarget;
        }

        // Reappear
        spriteRenderer.enabled = true;
        FacePlayer();
        yield return new WaitForSeconds(0.3f);

        // Throw knife
        animator.Play("Attack3");
        yield return new WaitForSeconds(0.3f);

        Transform spawnPoint = transform.localScale.x > 0 ? rightSpawnPoint : leftSpawnPoint;
        GameObject knife = Instantiate(knifeProjectilePrefab, spawnPoint.position, Quaternion.identity);
        knife.GetComponent<SamuraiProjectile>().SetDirection(transform.localScale.x > 0 ? Vector2.right : Vector2.left);

        yield return new WaitForSeconds(0.3f);
        animator.Play("Idle");
        isAttacking = false;

        StartCoroutine(KnifeThrowCooldown());
        yield return new WaitForSeconds(1f);

    }
```

```
    if (!groundCheck.collider)
    {
        // ❌ No ground — reappear in place and throw knife instead
        Debug.Log("[MartialHero] ChargeAttack failed — no ground, switching to knife throw");
```
→
```
    if (!groundCheck.collider)
    {
        // no ground - reappear in place and throw knife
        Debug.Log("[MartialHero] ChargeAttack failed - no ground, switching to knife throw");
```

```
    // ✅ Safe to teleport
    float targetY = groundCheck.point.y + 1f;
    Vector3 finalTarget = new Vector3(targetX, targetY, transform.position.z);
    transform.position = finalTarget;

    spriteRenderer.enabled = true;
    spriteRenderer.color = Color.white;

    FacePlayer();

    currentAttackPhase = "ChargeAttack";
```
→
```
    // ground found - teleport
    float targetY = groundCheck.point.y + 1f;
    Vector3 finalTarget = new Vector3(targetX, targetY, transform.position.z);
    transform.position = finalTarget;

    spriteRenderer.enabled = true;
    spriteRenderer.color = Color.white;

    FacePlayer();

    currentAttackPhase = "ChargeAttack";
```

```
        // 🧠 Phase 2 trigger at 50% health
```
→
```
        // phase 2 trigger at 50% health
```

```
            activeSpearRoutine = StartCoroutine(SpearPhaseLoop()); // ✅ No manual attack
```
→
```
            activeSpearRoutine = StartCoroutine(SpearPhaseLoop());
```

```
        // 🧠 Phase 3 trigger at 35% health
```
→
```
        // phase 3 trigger at 35% health
```

```
            activeSpearRoutine = StartCoroutine(AlternateSpearPhasePattern()); // ✅ No manual attack
```
→
```
            activeSpearRoutine = StartCoroutine(AlternateSpearPhasePattern());
```

```
        // 🧠 Final Phase at 25% health
```
→
```
        // final phase at 25% health
```

```
            activeSpearRoutine = StartCoroutine(FinalSpearPhaseRotation()); // ✅ No manual attack
```
→
```
            activeSpearRoutine = StartCoroutine(FinalSpearPhaseRotation());
```

```
            // 🧠 Regular attack logic
```
→
```
            // regular attack logic
```

```
    // 🔹 Flash blue briefly BEFORE attack
```
→
```
    // flash blue before attack
```

```
    // ▶️ Now play attack animation
```
→
```
    // play attack animation
```

```
        Debug.LogError("[Parry] ❌ Hitbox prefab is missing.");
```
→
```
        Debug.LogError("[Parry] Hitbox prefab is missing.");
```

```
    Debug.Log($"[Parry] ✅ Spawning hitbox at {transform.position} for {duration}s during {currentAttackPhase}");
```
→
```
    Debug.Log($"[Parry] Spawning hitbox at {transform.position} for {duration}s during {currentAttackPhase}");
```

```
        Debug.Log("[MartialHero] Ignored parry — not attacking.");
```
→ (this appears in Phase2 as well)
```
        Debug.Log("[MartialHero] Ignored parry - not attacking.");
```

- [ ] **Step 3: Edit MartialHeroHealth.cs**

```
            Debug.LogWarning("[MartialHeroHealth] Animator still null — skipping animation.");
```
→
```
            Debug.LogWarning("[MartialHeroHealth] Animator still null - skipping animation.");
```

```
            Debug.LogWarning("[MartialHeroHealth] SpriteRenderer still null — skipping flash.");
```
→
```
            Debug.LogWarning("[MartialHeroHealth] SpriteRenderer still null - skipping flash.");
```

- [ ] **Step 4: Edit MartialHeroStun.cs**

```
        Debug.Log("[MartialHeroStun] Parry registered — triggering stun.");
```
→
```
        Debug.Log("[MartialHeroStun] Parry registered - triggering stun.");
```

- [ ] **Step 5: Edit MartialBossTriggerRoom.cs**

```
        // ✅ Activate invisible walls
```
→
```
        // activate invisible walls
```

- [ ] **Step 6: Commit**
```
git add Assets/MartialHeroBehavior.cs Assets/MartialHeroPhase2.cs Assets/MartialHeroHealth.cs Assets/MartialHeroStun.cs Assets/MartialBossTriggerRoom.cs
git commit -m "chore: remove emoji and em-dash markers from Martial Hero comments"
```

---

### Task 5: Clean emoji and em-dash comments - Player and utility files

**Files:**
- Modify: `Assets/WarriorController.cs`
- Modify: `Assets/PlayerSceneSpawn.cs`
- Modify: `Assets/PlayerMartialSceneSpawn.cs`
- Modify: `Assets/SlimePlayerSpawner.cs`
- Modify: `Assets/PurpleWizardStun.cs`

- [ ] **Step 1: Edit WarriorController.cs**

```
        // 🔥 Play screen shake when hitting ground
```
→
```
        // screen shake on ground hit
```

```
    // ✅ Delay slightly before checking for parry (ensure physics overlap is fresh)
```
→
```
    // small delay before parry check so physics overlap is current
```

```
    Debug.Log("Parry started — hitbox activated.");
```
→
```
    Debug.Log("Parry started - hitbox activated.");
```

```
        Debug.Log("Parry missed — stamina used.");
```
→
```
        Debug.Log("Parry missed - stamina used.");
```

```
        Debug.Log("Parry successful — no stamina used.");
```
→
```
        Debug.Log("Parry successful - no stamina used.");
```

- [ ] **Step 2: Edit PlayerSceneSpawn.cs**

```
            // ✅ Always re-enable control
```
→
```
            // always re-enable control
```

```
            // ✅ Re-enable control
```
→
```
            // re-enable control
```

```
            Debug.Log("Warrior already exists — using existing player.");
```
→
```
            Debug.Log("Warrior already exists - using existing player.");
```

- [ ] **Step 3: Edit PlayerMartialSceneSpawn.cs**

```
            Debug.Log("Warrior already exists — using existing player.");
```
→
```
            Debug.Log("Warrior already exists - using existing player.");
```

```
        // ✅ No existing player — spawn new one
```
→
```
        // no existing player - spawn new one
```

```
            // ✅ Re-enable control on new player
```
→
```
            // re-enable control on new player
```

- [ ] **Step 4: Edit SlimePlayerSpawner.cs**

```
            Debug.Log("Warrior already exists — using existing player.");
```
→
```
            Debug.Log("Warrior already exists - using existing player.");
```

```
        // ✅ No existing player — spawn new one
```
→
```
        // no existing player - spawn new one
```

- [ ] **Step 5: Edit PurpleWizardStun.cs**

```
        Debug.Log("[WizardStunHandler] Parry registered — triggering stun.");
```
→
```
        Debug.Log("[WizardStunHandler] Parry registered - triggering stun.");
```

- [ ] **Step 6: Commit**
```
git add Assets/WarriorController.cs Assets/PlayerSceneSpawn.cs Assets/PlayerMartialSceneSpawn.cs Assets/SlimePlayerSpawner.cs Assets/PurpleWizardStun.cs
git commit -m "chore: remove emoji and em-dash markers from player and utility comments"
```

---

### Task 6: Create Scripts folder structure and move all custom scripts

**Files created:**
- `Assets/Scripts/Player/` (and all player .cs files)
- `Assets/Scripts/Bosses/` (and all boss .cs files)
- `Assets/Scripts/Enemies/` (and all enemy .cs files)
- `Assets/Scripts/Combat/` (and combat .cs files)
- `Assets/Scripts/Projectiles/` (and all projectile .cs files)
- `Assets/Scripts/Interfaces/` (and interface .cs files)
- `Assets/Scripts/UI/` (and UI/effects .cs files)

- [ ] **Step 1: Run the following PowerShell script from the repo root**

```powershell
Set-Location "C:\Users\seanp\Documents\GitHub\SideScrollGame"

function Move-UA($src, $dst) {
    New-Item -ItemType Directory -Force (Split-Path $dst -Parent) | Out-Null
    Move-Item $src $dst -Force
    if (Test-Path "$src.meta") { Move-Item "$src.meta" "$dst.meta" -Force }
}

# --- Player ---
Move-UA "Assets\WarriorController.cs"       "Assets\Scripts\Player\WarriorController.cs"
Move-UA "Assets\PlayerStats.cs"             "Assets\Scripts\Player\PlayerStats.cs"
Move-UA "Assets\PlayerHealth.cs"            "Assets\Scripts\Player\PlayerHealth.cs"
Move-UA "Assets\PlayerSceneSpawn.cs"        "Assets\Scripts\Player\PlayerSceneSpawn.cs"
Move-UA "Assets\PlayerMartialSceneSpawn.cs" "Assets\Scripts\Player\PlayerMartialSceneSpawn.cs"
Move-UA "Assets\PlayerSpawner.cs"           "Assets\Scripts\Player\PlayerSpawner.cs"
Move-UA "Assets\SlimePlayerSpawner.cs"      "Assets\Scripts\Player\SlimePlayerSpawner.cs"

# --- Bosses ---
Move-UA "Assets\CrabBossBehavior.cs"         "Assets\Scripts\Bosses\CrabBossBehavior.cs"
Move-UA "Assets\CrabBossBehaviorPhase1.cs"   "Assets\Scripts\Bosses\CrabBossBehaviorPhase1.cs"
Move-UA "Assets\CrabBossBehaviorPhase2.cs"   "Assets\Scripts\Bosses\CrabBossBehaviorPhase2.cs"
Move-UA "Assets\CrabBossHealth.cs"           "Assets\Scripts\Bosses\CrabBossHealth.cs"
Move-UA "Assets\CrabBossDeathHandler.cs"     "Assets\Scripts\Bosses\CrabBossDeathHandler.cs"
Move-UA "Assets\SlimeBossBehavior.cs"        "Assets\Scripts\Bosses\SlimeBossBehavior.cs"
Move-UA "Assets\SlimeBossHealth.cs"          "Assets\Scripts\Bosses\SlimeBossHealth.cs"
Move-UA "Assets\SlimeBossPhaseManager.cs"    "Assets\Scripts\Bosses\SlimeBossPhaseManager.cs"
Move-UA "Assets\SlimeBossTriggerRoom.cs"     "Assets\Scripts\Bosses\SlimeBossTriggerRoom.cs"
Move-UA "Assets\BossGolemBehavior.cs"        "Assets\Scripts\Bosses\BossGolemBehavior.cs"
Move-UA "Assets\BossGolemStun.cs"            "Assets\Scripts\Bosses\BossGolemStun.cs"
Move-UA "Assets\BossRoomTrigger.cs"          "Assets\Scripts\Bosses\BossRoomTrigger.cs"
Move-UA "Assets\MartialHeroBehavior.cs"      "Assets\Scripts\Bosses\MartialHeroBehavior.cs"
Move-UA "Assets\MartialHeroPhase2.cs"        "Assets\Scripts\Bosses\MartialHeroPhase2.cs"
Move-UA "Assets\MartialHeroHealth.cs"        "Assets\Scripts\Bosses\MartialHeroHealth.cs"
Move-UA "Assets\MartialHeroStun.cs"          "Assets\Scripts\Bosses\MartialHeroStun.cs"
Move-UA "Assets\MartialBossTriggerRoom.cs"   "Assets\Scripts\Bosses\MartialBossTriggerRoom.cs"
Move-UA "Assets\TornadoSlimeBossBehavior.cs" "Assets\Scripts\Bosses\TornadoSlimeBossBehavior.cs"
Move-UA "Assets\TornadoSlimeBossHealth.cs"   "Assets\Scripts\Bosses\TornadoSlimeBossHealth.cs"

# --- Enemies ---
Move-UA "Assets\RegularGolemBehavior.cs"   "Assets\Scripts\Enemies\RegularGolemBehavior.cs"
Move-UA "Assets\RegularGolemHealth.cs"     "Assets\Scripts\Enemies\RegularGolemHealth.cs"
Move-UA "Assets\RegularGolemStun.cs"       "Assets\Scripts\Enemies\RegularGolemStun.cs"
Move-UA "Assets\RegularCrabBehavior.cs"    "Assets\Scripts\Enemies\RegularCrabBehavior.cs"
Move-UA "Assets\RegularCrabHealth.cs"      "Assets\Scripts\Enemies\RegularCrabHealth.cs"
Move-UA "Assets\ExplodingSlimeBehavior.cs" "Assets\Scripts\Enemies\ExplodingSlimeBehavior.cs"
Move-UA "Assets\SlimeBehavior.cs"          "Assets\Scripts\Enemies\SlimeBehavior.cs"
Move-UA "Assets\SlimeHealth.cs"            "Assets\Scripts\Enemies\SlimeHealth.cs"
Move-UA "Assets\SlimeLandingPush.cs"       "Assets\Scripts\Enemies\SlimeLandingPush.cs"
Move-UA "Assets\PurpleWizardBehavior.cs"   "Assets\Scripts\Enemies\PurpleWizardBehavior.cs"
Move-UA "Assets\PurpleWizardStun.cs"       "Assets\Scripts\Enemies\PurpleWizardStun.cs"
Move-UA "Assets\ShootingWizardBehavior.cs" "Assets\Scripts\Enemies\ShootingWizardBehavior.cs"
Move-UA "Assets\ShootingWizardHealth.cs"   "Assets\Scripts\Enemies\ShootingWizardHealth.cs"

# --- Combat ---
Move-UA "Assets\ParrySystem.cs"  "Assets\Scripts\Combat\ParrySystem.cs"
Move-UA "Assets\ParryHitbox.cs"  "Assets\Scripts\Combat\ParryHitbox.cs"
Move-UA "Assets\DealDamage.cs"   "Assets\Scripts\Combat\DealDamage.cs"

# --- Interfaces ---
Move-UA "Assets\IAttackState.cs" "Assets\Scripts\Interfaces\IAttackState.cs"
Move-UA "Assets\IDamageable.cs"  "Assets\Scripts\Interfaces\IDamageable.cs"
Move-UA "Assets\IStunnable.cs"   "Assets\Scripts\Interfaces\IStunnable.cs"

# --- Projectiles ---
Move-UA "Assets\ArcProjectile.cs"            "Assets\Scripts\Projectiles\ArcProjectile.cs"
Move-UA "Assets\CrabArcProjectile.cs"        "Assets\Scripts\Projectiles\CrabArcProjectile.cs"
Move-UA "Assets\CrabPearlProjectile.cs"      "Assets\Scripts\Projectiles\CrabPearlProjectile.cs"
Move-UA "Assets\HorizontalProjectile.cs"     "Assets\Scripts\Projectiles\HorizontalProjectile.cs"
Move-UA "Assets\ProjectileBehaivor.cs"       "Assets\Scripts\Projectiles\ProjectileBehaivor.cs"
Move-UA "Assets\SamuraiProjectile.cs"        "Assets\Scripts\Projectiles\SamuraiProjectile.cs"
Move-UA "Assets\SpearProjectile.cs"          "Assets\Scripts\Projectiles\SpearProjectile.cs"
Move-UA "Assets\WizardProjectileBehavior.cs" "Assets\Scripts\Projectiles\WizardProjectileBehavior.cs"
Move-UA "Assets\MiniTornadoProjectile.cs"    "Assets\Scripts\Projectiles\MiniTornadoProjectile.cs"
Move-UA "Assets\Fallingball.cs"              "Assets\Scripts\Projectiles\Fallingball.cs"

# --- UI / Effects ---
Move-UA "Assets\CameraShake.cs"              "Assets\Scripts\UI\CameraShake.cs"
Move-UA "Assets\FollowBoss.cs"               "Assets\Scripts\UI\FollowBoss.cs"
Move-UA "Assets\StunIconFollow.cs"           "Assets\Scripts\UI\StunIconFollow.cs"
Move-UA "Assets\MartialHeroStunIconFollow.cs" "Assets\Scripts\UI\MartialHeroStunIconFollow.cs"
Move-UA "Assets\RegularStunIconFollow.cs"    "Assets\Scripts\UI\RegularStunIconFollow.cs"
Move-UA "Assets\PurpleStunIconFollow.cs"     "Assets\Scripts\UI\PurpleStunIconFollow.cs"
Move-UA "Assets\RedLineFollow.cs"            "Assets\Scripts\UI\RedLineFollow.cs"
Move-UA "Assets\RedLineShotBehavior.cs"      "Assets\Scripts\UI\RedLineShotBehavior.cs"
Move-UA "Assets\RedLineBlink.cs"             "Assets\Scripts\UI\RedLineBlink.cs"

Write-Output "Script moves complete"
```

- [ ] **Step 2: Verify no .cs files remain in Assets/ root**

```powershell
$remaining = Get-ChildItem "Assets\*.cs" -ErrorAction SilentlyContinue
if ($remaining) { Write-Warning "Remaining scripts: $($remaining.Name -join ', ')" } else { Write-Output "All scripts moved" }
```

Expected output: `All scripts moved`

- [ ] **Step 3: Verify key files exist in new locations with .meta**

```powershell
@(
    "Assets\Scripts\Player\WarriorController.cs",
    "Assets\Scripts\Player\WarriorController.cs.meta",
    "Assets\Scripts\Bosses\MartialHeroBehavior.cs",
    "Assets\Scripts\Bosses\MartialHeroBehavior.cs.meta",
    "Assets\Scripts\Combat\ParrySystem.cs",
    "Assets\Scripts\Interfaces\IDamageable.cs"
) | ForEach-Object { if (Test-Path $_) { "OK: $_" } else { "MISSING: $_" } }
```

Expected: all lines show `OK:`

- [ ] **Step 4: Commit**
```
git add -A
git commit -m "refactor: move all custom scripts from Assets root into Scripts subfolders"
```

---

### Task 7: Create Prefabs folder structure and move all custom prefabs

**Files:**
- Move all custom prefabs from `Assets/Enemies/` to `Assets/Prefabs/{Bosses,Enemies,Projectiles,Effects,Player,Zones}/`

- [ ] **Step 1: Run the following PowerShell script**

```powershell
Set-Location "C:\Users\seanp\Documents\GitHub\SideScrollGame"

function Move-UA($src, $dst) {
    New-Item -ItemType Directory -Force (Split-Path $dst -Parent) | Out-Null
    Move-Item $src $dst -Force
    if (Test-Path "$src.meta") { Move-Item "$src.meta" "$dst.meta" -Force }
}

# --- Player ---
Move-UA "Assets\Enemies\Warrior.prefab"              "Assets\Prefabs\Player\Warrior.prefab"

# --- Bosses ---
Move-UA "Assets\Enemies\BossGolem.prefab"            "Assets\Prefabs\Bosses\BossGolem.prefab"
Move-UA "Assets\Enemies\CrabBoss.prefab"             "Assets\Prefabs\Bosses\CrabBoss.prefab"
Move-UA "Assets\Enemies\SlimeBoss.prefab"            "Assets\Prefabs\Bosses\SlimeBoss.prefab"
Move-UA "Assets\Enemies\TornadoSlimeBossClone.prefab" "Assets\Prefabs\Bosses\TornadoSlimeBossClone.prefab"

# --- Enemies ---
Move-UA "Assets\Enemies\RegularCrab.prefab"          "Assets\Prefabs\Enemies\RegularCrab.prefab"
Move-UA "Assets\Enemies\RegularGolem.prefab"         "Assets\Prefabs\Enemies\RegularGolem.prefab"
Move-UA "Assets\Enemies\Slime.prefab"                "Assets\Prefabs\Enemies\Slime.prefab"
Move-UA "Assets\Enemies\PurpleWizard.prefab"         "Assets\Prefabs\Enemies\PurpleWizard.prefab"
Move-UA "Assets\Enemies\Samurai.prefab"              "Assets\Prefabs\Enemies\Samurai.prefab"
Move-UA "Assets\Enemies\ShootingWizard.prefab"       "Assets\Prefabs\Enemies\ShootingWizard.prefab"
Move-UA "Assets\Enemies\ExplodingSlimes.prefab"      "Assets\Prefabs\Enemies\ExplodingSlimes.prefab"

# --- Projectiles ---
Move-UA "Assets\Enemies\Projectile.prefab"              "Assets\Prefabs\Projectiles\Projectile.prefab"
Move-UA "Assets\Enemies\BossGolemProjectile.prefab"     "Assets\Prefabs\Projectiles\BossGolemProjectile.prefab"
Move-UA "Assets\Enemies\KnifeProjectile.prefab"         "Assets\Prefabs\Projectiles\KnifeProjectile.prefab"
Move-UA "Assets\Enemies\HorizontalProjectile.prefab"    "Assets\Prefabs\Projectiles\HorizontalProjectile.prefab"
Move-UA "Assets\Enemies\ArcPearlAttack.prefab"          "Assets\Prefabs\Projectiles\ArcPearlAttack.prefab"
Move-UA "Assets\Enemies\ArcPearlAttack 1.prefab"        "Assets\Prefabs\Projectiles\ArcPearlAttack 1.prefab"
Move-UA "Assets\Enemies\FallingPearlProjectile.prefab"  "Assets\Prefabs\Projectiles\FallingPearlProjectile.prefab"
Move-UA "Assets\Enemies\GroundShotPearlProjectile.prefab" "Assets\Prefabs\Projectiles\GroundShotPearlProjectile.prefab"

# --- Effects ---
Move-UA "Assets\Enemies\RedLine.prefab"                 "Assets\Prefabs\Effects\RedLine.prefab"
Move-UA "Assets\Enemies\RedLineHorizontalShot.prefab"   "Assets\Prefabs\Effects\RedLineHorizontalShot.prefab"
Move-UA "Assets\Enemies\RedLineMemoryShot.prefab"       "Assets\Prefabs\Effects\RedLineMemoryShot.prefab"
Move-UA "Assets\Enemies\RedLineStatic.prefab"           "Assets\Prefabs\Effects\RedLineStatic.prefab"
Move-UA "Assets\Enemies\RedLineVerticalShot.prefab"     "Assets\Prefabs\Effects\RedLineVerticalShot.prefab"

# --- Zones ---
Move-UA "Assets\Enemies\ZoneLeft.prefab"                "Assets\Prefabs\Zones\ZoneLeft.prefab"
Move-UA "Assets\Enemies\ZoneMiddle.prefab"              "Assets\Prefabs\Zones\ZoneMiddle.prefab"
Move-UA "Assets\Enemies\ZoneRight.prefab"               "Assets\Prefabs\Zones\ZoneRight.prefab"

# Spear is from third-party Cainos pack - move to Prefabs root rather than delete
Move-UA "Assets\Enemies\PF Village Props - Spear.prefab" "Assets\Prefabs\PF Village Props - Spear.prefab"

Write-Output "Prefab moves complete"
```

- [ ] **Step 2: Move pearl sprites from Assets/Enemies/ to Assets/Sprites/**

```powershell
function Move-UA($src, $dst) {
    New-Item -ItemType Directory -Force (Split-Path $dst -Parent) | Out-Null
    Move-Item $src $dst -Force
    if (Test-Path "$src.meta") { Move-Item "$src.meta" "$dst.meta" -Force }
}
Move-UA "Assets\Enemies\Pearl.3.png"                 "Assets\Sprites\Pearl.3.png"
Move-UA "Assets\Enemies\pearl-removebg-preview.png"  "Assets\Sprites\pearl-removebg-preview.png"
```

- [ ] **Step 3: Verify Assets/Enemies/ contains only animation files now**

```powershell
Get-ChildItem "Assets\Enemies" | Select-Object Name
```

Expected: only `Charge.anim`, `Explode.anim`, `GroundAttack.anim` (and their .meta files).

- [ ] **Step 4: Commit**
```
git add -A
git commit -m "refactor: move custom prefabs and sprites from Assets/Enemies into Prefabs subfolders"
```

---

### Task 8: Move loose animation files

**Files:**
- Move `Assets/Enemies/*.anim` → `Assets/Animations/Bosses/`
- Move any loose `.anim` files from `Assets/` root → `Assets/Animations/`

- [ ] **Step 1: Move animations from Assets/Enemies/**

```powershell
Set-Location "C:\Users\seanp\Documents\GitHub\SideScrollGame"

function Move-UA($src, $dst) {
    New-Item -ItemType Directory -Force (Split-Path $dst -Parent) | Out-Null
    Move-Item $src $dst -Force
    if (Test-Path "$src.meta") { Move-Item "$src.meta" "$dst.meta" -Force }
}

# Boss animations in Enemies folder
Move-UA "Assets\Enemies\Charge.anim"       "Assets\Animations\Bosses\Charge.anim"
Move-UA "Assets\Enemies\Explode.anim"      "Assets\Animations\Bosses\Explode.anim"
Move-UA "Assets\Enemies\GroundAttack.anim" "Assets\Animations\Bosses\GroundAttack.anim"
```

- [ ] **Step 2: Check for loose animation files in Assets/ root**

```powershell
Get-ChildItem "Assets\*.anim" | Select-Object Name
```

Move any listed files using Move-UA to the appropriate `Assets\Animations\` subfolder.

- [ ] **Step 3: Verify Assets/Enemies/ is now empty (can be deleted)**

```powershell
$remaining = Get-ChildItem "Assets\Enemies" -ErrorAction SilentlyContinue
if (-not $remaining) { Remove-Item "Assets\Enemies" -Recurse -Force; Remove-Item "Assets\Enemies.meta" -ErrorAction SilentlyContinue; Write-Output "Assets/Enemies removed" }
else { Write-Output "Still contains: $($remaining.Name -join ', ')" }
```

- [ ] **Step 4: Commit**
```
git add -A
git commit -m "refactor: move animation files into Assets/Animations and remove empty Enemies folder"
```

---

### Task 9: Remove unused test sprites from Assets/ root

- [ ] **Step 1: Check whether cat*.png files are referenced in any scene or prefab**

```powershell
Select-String -Path "Assets\Scenes\*.unity","Assets\Prefabs\**\*.prefab" -Pattern "cat" -SimpleMatch | Select-Object Filename, LineNumber, Line
```

If no results, the cat PNG files are safe to delete.

- [ ] **Step 2: Delete if unreferenced**

```powershell
@("cat1.fly.png","Cat1.flynew.png","cat2.fly.png","cat3.fly.png") | ForEach-Object {
    $path = "Assets\$_"
    if (Test-Path $path) {
        Remove-Item $path -Force
        Remove-Item "$path.meta" -Force -ErrorAction SilentlyContinue
        Write-Output "Deleted: $_"
    }
}
```

- [ ] **Step 3: Commit**
```
git add -A
git commit -m "chore: remove unused test sprite files from Assets root"
```

---

### Task 10: Update CLAUDE.md to reflect new structure

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Update the Key Files table and Architecture section**

In `CLAUDE.md`, update the file paths in the Key Files table:

| Old path | New path |
|----------|----------|
| `Assets/WarriorController.cs` | `Assets/Scripts/Player/WarriorController.cs` |
| `Assets/PlayerStats.cs` | `Assets/Scripts/Player/PlayerStats.cs` |
| `Assets/ParrySystem.cs` | `Assets/Scripts/Combat/ParrySystem.cs` |
| `Assets/DealDamage.cs` | `Assets/Scripts/Combat/DealDamage.cs` |
| `Assets/Enemies/CrabBoss/CrabBossBehavior.cs` | `Assets/Scripts/Bosses/CrabBossBehavior.cs` |

Also update the "Third-Party Assets" note to clarify that game scripts are now in `Assets/Scripts/` and custom prefabs in `Assets/Prefabs/`.

- [ ] **Step 2: Commit**

Note: CLAUDE.md is now gitignored, so this commit only applies locally. No commit needed.

---

## Summary of New Structure

```
Assets/
├── Scripts/
│   ├── Player/          (WarriorController, PlayerStats, PlayerHealth, spawners)
│   ├── Bosses/          (all boss behavior, health, stun, trigger scripts)
│   ├── Enemies/         (regular enemy behavior, health, stun)
│   ├── Combat/          (ParrySystem, ParryHitbox, DealDamage)
│   ├── Projectiles/     (all projectile scripts)
│   ├── Interfaces/      (IDamageable, IAttackState, IStunnable)
│   └── UI/              (CameraShake, FollowBoss, stun icon followers, red line)
├── Prefabs/
│   ├── Bosses/          (BossGolem, CrabBoss, SlimeBoss, TornadoSlimeBossClone)
│   ├── Enemies/         (RegularCrab, RegularGolem, Slime, PurpleWizard, Samurai, ShootingWizard, ExplodingSlimes)
│   ├── Projectiles/     (all projectile prefabs)
│   ├── Effects/         (RedLine variants)
│   ├── Player/          (Warrior)
│   └── Zones/           (ZoneLeft, ZoneMiddle, ZoneRight)
├── Animations/
│   └── Bosses/          (Charge, Explode, GroundAttack)
├── Sprites/             (pearl sprites)
├── Scenes/              (unchanged - 4 game scenes)
├── Settings/            (unchanged)
└── [Third-party packs]  (unchanged - Cainos, EnemyGalore, EvilWizard, etc.)
```
