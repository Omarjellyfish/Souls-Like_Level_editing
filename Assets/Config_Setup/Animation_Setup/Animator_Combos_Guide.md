# Combat Animator Setup Guide

### 1. The "Blend Tree" Trap
It is very common to assume combat combos use Blend Trees, but in Unity action games, this is actually a trap! Blend Trees are designed for *continuous mathematical blending* (like smoothly fading your legs from Walking to Running based on your joystick speed). 

If you put a 3-hit sword combo into a Blend Tree, Unity will try to morph the arm positions between Swing 1 and Swing 2 halfway through the animation, resulting in "mushy" floating arms. Even worse, **you cannot place precise Animation Events** (like turning on your sword's damage collider or opening the precise combo window) accurately inside a Blend Tree!

### 2. The Solution: Sub-State Machines
Instead of Blend Trees, you must use **Sub-State Machines**. A Sub-State Machine looks like a grey hexagon/folder inside your Animator. It keeps your main Animator completely clean (acting exactly like a Blend Tree visually from the outside) while allowing you to use discrete, perfect animation nodes inside!

---

## Step-by-Step Animator Setup

### A. Create the Sub-State Machines
1. In your Animator, right-click an empty space and choose **Create Sub-State Machine**.
2. Name it `Light_Combo`.
3. Create another one named `Heavy_Combo`.
4. Create another one named `Defensive_Actions` (for Parry, Dodge, and Stagger).

*This keeps your main Animator incredibly clean. You'll just see 3 neat grey folders instead of a giant spiderweb of transition lines!*

### B. The Light Combo (Inside the Sub-State)
Double-click your `Light_Combo` Sub-State node to open it.
1. Drag your 3 Light Swing Animations into this space.
2. Look for the aquamarine **Any State** node inside the Sub-State, and draw arrows from it to your swings.
   - Arrow to Swing 1: Trigger `LightAttack`, Int `ComboStep = 1`
   - Arrow to Swing 2: Trigger `LightAttack`, Int `ComboStep = 2`
   - Arrow to Swing 3: Trigger `LightAttack`, Int `ComboStep = 3`
3. (Optional but recommended): Uncheck "Can Transition To Self" on all these arrows in the Inspector so they don't visually glitch if you click fast.
4. Add the Animation Events to the timeline on your 3 raw animations:
   - **~70% Time**: `OpenComboWindow`
   - **100% Time**: `FinishCurrentAttack`

### C. The Heavy Combo
Double-click your `Heavy_Combo` Sub-State.
1. Drag your 3 Heavy Swing Animations here.
2. Link them exactly like the Light ones, but use the `HeavyAttack` Trigger instead!
   - Arrow to Heavy 1: Trigger `HeavyAttack`, Int `ComboStep = 1`
   - Arrow to Heavy 2: Trigger `HeavyAttack`, Int `ComboStep = 2` (etc...)
3. Add the exact same Animation Events to their timelines!

### D. Definesive Actions (Parry, Stagger, Dodge, JumpingAttack)
Open your `Defensive_Actions` Sub-State.
1. Drag your `Parry`, `Stagger`, `Dodge`, and `JumpingAttack` animations here.
2. Draw lines directly from `Any State` to each of them:
   - To Parry: Trigger `Parry`
   - To Stagger: Trigger `Stagger`
   - To Jumping Attack: Trigger `JumpingAttack`
3. **CRITICAL RESET EVENTS:** 
   - Add `FinishCurrentAttack` to the very *end* of your **Parry** animation so the script unlocks your movement!
   - Add `FinishCurrentAttack` to the very *end* of your **Stagger** animation so you can swing your sword again after flinching!
   - Add `FinishCurrentAttack` to the very *end* of your **JumpingAttack**!
