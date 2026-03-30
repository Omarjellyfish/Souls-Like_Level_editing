# Souls-Like Architecture & Integration Guide

This document acts as the master blueprint for the game's modular combat, defense, and camera systems. Every system is intentionally decoupled so they can be modified without breaking the rest of the game. As we build new features, they will be documented here.

---

## 1. Core Stats & Routing
The foundation relies on stat scripts that act as **Intermediaries**. They don't make complex combat decisions; they hold logic and fire off events that other systems listen to.

*   **`Health.cs` (Player & Enemy)** 
    *   **The Hub**: Holds `IsInvincible`, `IsVulnerable`, and `IsParrying`. 
    *   **The Logic**: If hit while parrying, it negates damage and fires `OnSuccessfulParry(attacker)`. If hit while vulnerable, multiplies damage by 300% (Riposte). Otherwise, subtracts health and passes residual Stagger damage along via `OnStaggerDamageReceived`.
*   **`Stamina.cs` (Player Only)**
    *   **The Check**: Other scripts (Dodge, Attack, Parry) must ask `TryConsumeStamina(amount)` before they are allowed to execute their logic.

---

## 2. Offense (Weapons & Attacking)
*   **`WeaponData` (ScriptableObject)**
    *   Created via `Create > Combat > Weapon Data`. Holds Base Damage, Speed, Range, Stamina Cost, and Stagger amounts globally so 50 different swords can share one stat block.
*   **`MeleeWeapon.cs`** 
    *   Attach to the physical Sword model. Provide it a `WeaponData` file.
*   **`LightAttack.cs` & `HeavyAttack.cs`**
    *   Attach alongside the `MeleeWeapon`. These detect collisions (`OnTriggerEnter`) and pass the weapon's `Damage` & `Stagger` directly to the target's `Health.TakeDamage()` method, threading through the attacker's Root GameObject.
*   **`CombatManager.cs` (Player Brain)**
    *   Reads mouse hold durations (default `0.25s`) to differentiate Light vs Heavy attacks.
    *   Checks Stamina, triggers the correct Animator hash (`"LightAttack"`, `"HeavyAttack"`), and briefly grants Hyper Armor during Heavy Attacks.
    *   **CRITICAL REQUIREMENT:** You *must* add a Unity Animation Event at the end of every swing animation calling `FinishCurrentAttack()`. Without this, the manager refuses to let you attack again.

---

## 3. Defense (Poise, Parry, & Dodge)
These systems integrate seamlessly by listening to the events coming out of `Health.cs`.

*   **`DodgeController.cs`**
    *   Calculates WASD motion relative to the Camera. Flips `Health.IsInvincible` to `true` while the animation curve drives the 5-meter dash.
*   **`PoiseSystem.cs` (Player & Enemy)**
    *   Listens to `Health.OnStaggerDamageReceived`. 
    *   If current poise hits 0: Triggers `"Stagger"` in the Animator and forces the attacker to cancel whatever swing they were doing through `CombatManager.FinishCurrentAttack()`.
    *   **Hyper Armor:** If active (via Heavy Attacks), it doubles the required stagger damage.
*   **`ParryController.cs` (Player Only)**
    *   Right click starts a `0.3s` parry window, setting `Health.IsParrying` to true. 
    *   If `Health` successfully deflects an attack, the `ParryController` finds the attacker's `PoiseSystem` and executes `SufferParry()` -> Instantly dropping their poise to 0 and opening a massive 3-second `IsVulnerable` Riposte window.

---

## 4. Camera & Targeting
*   **`LockOnManager.cs`**
    *   Press 'F' to calculate the best target near screen-center via Physics OverlapSphere and Line-of-Sight Raycasts. 
    *   Snaps an invisible `LockOnProxy` transform exactly to the center of the enemy's chest, giving the cameras a perfectly smooth anchor point.
    *   Supports scrolling mouse wheel to seamlessly switch targets left or right.
*   **`CameraManager.cs`**
    *   Listens to the `LockOnManager`.
    *   It operates independently of Cinemachine namespaces by simply using `GameObject.SetActive()`.
    *   **Setup:** Drop `CinemachineFreeLook` into the Exploration slot, and your `CinemachineVirtualCamera` (Set to "LookAt" a TargetGroup of Player + Proxy) into the LockOn slot. It perfectly handles cinematic blending between them automatically.
