# 03 - Camera & Targeting Systems

## Seamless Transitioning
The game does not swap literal camera attributes; instead, it uses a lightweight dual-camera toggling system via `GameObject.SetActive()`.

---

## The Lock-On Manager
Pressing the **'F'** key activates the `LockOnManager.cs`.

*   **Target Finding:** It performs a spherical overlap check originating from the camera to dynamically find the enemy closest to the center of the screen.
*   **Line of Sight:** It fires a physics raycast to guarantee the target isn't behind a solid wall.
*   **Lock-On Proxy:** To prevent the camera from snapping erratically when the enemy plays an animation, the script spawns an invisible `Proxy Transform` exactly in the center of the enemy's chest. This provides a perfectly smooth anchor point.
*   **Target Switching:** Flicking the Mouse Scroll Wheel rapidly calculates target distances and seamlessly passes lock-on priority to enemies on the left or the right.

---

## The Camera Manager
The `CameraManager.cs` purely listens to the `LockOnManager`.

*   **Exploration Camera:** Uses `CinemachineFreeLook`. The standard, player-driven orbiting camera.
*   **Lock-On Camera:** Uses `CinemachineVirtualCamera`. It points at a `TargetGroup` (which contains both the Player and the LockOnProxy), mathematically keeping both subjects beautifully framed on screen.

When `LockOnManager` finds a target, `CameraManager` disables the FreeLook camera and enables the VirtualCamera. Cinemachine's brain handles the cinematic blending automatically!
