# 01 - The Ultra-Modular Player Architecture

## The Core Philosophy
The player is built using a **Modularity First** approach. The movement system and the combat systems are completely separate "plugins". They do not rely on each other to compile, meaning you can drop the dodge roll into a completely different game and it will just work.

---

## The Movement Core (Combat-Blind)
These scripts live in `Souls-Like_Player/` and are completely unaware that swords or combat even exist.

1. **`SoulsLike_InputHandler`**
   - **Job:** The ONLY script that reads WASD, Left Shift (Sprint), and Space (Jump). 
   - **Why:** If you ever switch to a Gamepad or the New Unity Input System, you only edit this one file.

2. **`SoulsLike_Movement`**
   - **Job:** Handles CharacterController physics, jumping math, falling gravity, and rotating the character towards the camera's forward direction.

3. **`SoulsLike_WallRunController`**
   - **Job:** Shoots raycasts left and right while Airborne+Sprinting. Pauses normal gravity and glues the player to the wall.

4. **`SoulsLike_AnimationHandler`**
   - **Job:** Reads the speed from the InputHandler and passes it to the Animator for locomotion blending.

---

## The State Manager (The Bridge)
Since the Movement Core doesn't know about Combat, how do we stop the player from jumping while they are swinging a sword? 

The **`SoulsLike_StateManager`**!
Every single script on the player looks for this central hub. It holds an enum (`Idle, Moving, Airborne, Attacking, Dodging, Parrying, Staggered`). 

**How it works:**
Whenever a plugin wants to act, it politely asks the State Manager.
*   **Movement:** *"Hey StateManager, the player pressed Space. Can I enter the Airborne state?"*
*   **StateManager:** *"No. Currently, the state is set to Attacking. Jump denied!"*

---

## The Combat Plugins (Movement-Blind)
These scripts read their own inputs (Click, Alt, Right Click). Before executing, they simply call `_stateManager.TryEnterState()`. If it returns true, they execute and tell the Animator to play their specific animation.

1. **`CombatManager` (LMB)**
   - Requests `Attacking` state. Reads clicks, triggers combo phases, and listens for the `FinishCurrentAttack` Unity Event to release the Attacking state back to `Idle`.
2. **`DodgeController` (ALT)**
   - Requests `Dodging` state. Immediately moves the character mathematically across a curve, ignoring standard Locomotion. Releases state when curve finishes.
3. **`ParryController` (RMB)**
   - Requests `Parrying` state. Listens to `Health.cs` to see if a deflection occurred! Can easily open an enemy up for a Riposte.
