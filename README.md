# TheLastOne
# Modular FPS Game with Integrated Spawn System 🚀🔫

A scalable and modular First-Person Shooter (FPS) framework built in Unity (URP) featuring:
- Dynamic enemy spawn waves
- Full weapon system (fire, reload, ammo types)
- Health and damage handling
- Dual-mode camera (First-Person + Free View)
- Modular UI & event-driven architecture

## 🎮 Features

### ✅ Modular FPS Controller
- Supports First-Person perspective with smooth camera control
- Handles movement, shooting, reloading, and weapon switching

### ✅ Spawn System
- Wave-based enemy spawning with configurable difficulty scaling
- Pooling system for performance optimization
- Spawn points within a defined arena radius
- Real-time enemy counter and UI integration

### ✅ Weapon & Ammo System
- Fire point-based bullet logic
- Ammo types and inventory
- Supports multiple weapons with different configurations
- Fire effects and sounds integrated

### ✅ Camera System
- `CameraManager` handles seamless switching between First-Person and Free Camera views
- Bounds clamping and scroll zoom in Free mode
- Optimized for debugging and cinematic views

### ✅ Health & Damage
- Modular `Health` system using the `IDamageable` interface
- UI health bar auto-updates with damage and healing
- On-death pooling logic for enemies

### ✅ UI Manager
- Real-time ammo display
- Health bar
- Wave indicator and kill counter
- Modular UI event system

## 🔧 Tech Stack

- **Engine**: Unity 2022+ (URP)
- **Language**: C#
- **Architecture**: Event-driven, SOLID principles
- **Tools**: Cinemachine, TextMeshPro,
- Unity Input System, Object Pooling

## 🧪 How to Use

1. Clone this repository
2. Open the project in Unity (URP installed)
3. Press Play — use `FreeFly` camera to test, or enter First Person mode
4. Watch enemies spawn, attack, and respawn in waves
5. Toggle between views, test weapons, monitor UI

## 🎯 Controls

| Action            | Key/Mouse |
|-------------------|-----------|
| Move              | WASD      |
| Fire              | Left Click|
| Reload            | R         |
| Switch Camera     | C         |
| Toggle Cursor Lock| Esc       |

## 🔄 Integration Flow (Simplified)

1. `WaveManager` spawns enemies → enemies use `Health` → on death → notify `UIManager` and pool back.
2. Player uses `WeaponHandler` → calls `AmmoManager` & spawns bullets → bullets damage `IDamageable`.
3. `CameraManager` switches perspectives dynamically.
4. `UIManager` listens to all major events and updates UI accordingly.
---
