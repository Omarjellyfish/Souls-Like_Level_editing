# 02 - Combat Stats & Defenses

## The System Hierarchy
The combat foundation does not rely on direct code-to-code dependencies. Instead, it relies on Stat Scripts (`Health`, `Stamina`, `Poise`) that act as **event hubs**.

---

## 1. Active Defenses
*   **`DodgeController.cs`**
    *   Toggles `Health.IsInvincible = true`. 
    *   Any sword that collides with the player during the animation curve is completely ignored.
*   **`ParryController.cs`**
    *   Toggles `Health.IsParrying = true`.
    *   If the player is hit during this tight 0.3s window, no damage is taken, and the attacker's logic is instantly interrupted via an `OnSuccessfulParry` event.
    *   Directly obliterates the attacker's `PoiseSystem` to open a 3-second Riposte window.

---

## 2. Passive Defenses
*   **`PoiseSystem.cs`**
    *   Like Dark Souls, this is a hidden HP bar that only tracks "Stagger" damage.
    *   If Poise drops to 0, the script overrides the player/enemy state to `Staggered`, freezing all movement and canceling any active attack swings.
    *   **Hyper Armor:** Granted briefly during Heavy Attacks. This halves incoming Poise damage, allowing the character to "tank" smaller hits without flinching and interrupting their swing.

---

## 3. Offense
*   **`WeaponData` (ScriptableObject)**
    *   Right-Click > Create > Combat > Weapon Data. 
    *   Centralizes base damage, speed, range, stamina cost, and stagger damage so a single weapon model can be re-used by 50 different enemies without duplicating stats.
*   **`MeleeWeapon.cs`**
    *   The script attached to the literal sword model holding the `WeaponData`.
*   **`LightAttack / HeavyAttack.cs`**
    *   These scripts detect physics collisions (`OnTriggerEnter`) and pull the stats from the `MeleeWeapon` to pass directly to the target's `Health.TakeDamage()`.
