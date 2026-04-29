# R.E.P.O. Ping System

Adds a team communication ping system to R.E.P.O., inspired by Fortnite and PEAK.

## Features

- **Quick Ping:** Middle click to place a marker where you're looking
- **Smart Detection:** Automatically detects what you're pinging:
  - Walls/floors/cart = **Go Here** (blue)
  - Items/valuables = **Loot** (green)
  - Enemies = **Enemy** (orange)
- **Radial Wheel:** Hold middle click to choose from 4 ping types manually (Go Here, Danger, Enemy, Loot)
- **3D Preview:** When pinging items, enemies, or the cart, a lit 3D preview appears inside the ping marker
- **Object Tracking:** Pings follow moving objects (enemies walking, items being carried, cart moving)
- **Arm Pointing:** Other players see your right arm point at the ping location for 1 second
- **Multiplayer Sync:** All players see pings, previews, tracking, and arm pointing in real time
- **Audio Feedback:** Each ping type has a distinct sound (only heard within 20m)
- **Auto-fade:** Pings last 4 seconds with a smooth fade out
- **Screen-Space Rendering:** Pings render as screen indicators, visible within 15m
- **Distance Display:** Shows how far you are from each ping
- **Fully Configurable:** All settings adjustable via BepInEx config (F1 with ConfigurationManager)

## Controls

- **Middle click (quick):** Place a ping (type auto-detected)
- **Middle click (hold):** Open radial wheel to select ping type manually
- **Right click:** Cancel radial wheel
- **Release on center:** Cancel selection

## How It Works

1. Aim at any surface, item, or enemy
2. Quick click middle mouse for an auto-detected ping
3. Hold middle mouse to open the selection wheel for manual type selection
4. Move your mouse to the desired ping type and release
5. The ping appears where you were aiming when you first pressed the button
6. Pings follow moving objects and are visible within 15m
7. All players with the mod see the ping, including 3D preview, tracking, and arm pointing

## Installation

Install via Gale, r2modman, or Thunderstore Mod Manager. Manual: copy `REPOPingMod.dll` to `BepInEx/plugins/`.

**Requires:** BepInEx 5.x, REPOLib 3.x

## Changelog

### 0.3.0
- Improved item/enemy/cart detection: now uses `GetComponentInParent` (same as the game) instead of limited depth search
- Fixes items not being detected as Loot on modded valuables with deep prefab hierarchies
- Verified against 801 objects: ~697 valuables, ~75 items, 29 enemy types, 2 carts, 0 false positives

### 0.2.1
- Async preview rendering: preview capture spread across 5 frames (no more freezing)
- Uses RenderTexture directly (no ReadPixels GPU stall)
- Previews now work on received pings without freezing
- Rate limiter: max 1 ping per player per 0.5 seconds
- Disable instead of destroy components on clone (no dependency errors)

### 0.2.0
- Arm pointing: other players see your right arm point at the ping for 1 second
- Uses the game's spring physics system for smooth arm animation
- Harmony patch on PlayerAvatarRightArm for multiplayer sync

### 0.1.1
- Object tracking: pings follow moving items, enemies, and cart
- Audio range limit: ping sounds only heard within 20m
- Cart preview: full cart rendered even when pinging the handle
- Improved clone cleanup: 10-pass loop with try/catch eliminates dependency errors
- Ping duration reduced to 4 seconds

### 0.1.0
- Multiplayer preview sync: other players see the 3D preview via object name matching
- Object name sent over network for exact identification

### 0.0.3
- Smart auto-detection: items = Loot, enemies = Enemy, walls = Go Here
- Cart detected as Go Here with 3D preview
- Enemy pinging with 3D preview (safe cloning)
- Fixed clone crash with enemy scripts
- Fixed player grab colliders being pinged when crouching

### 0.0.2
- Screen-space rendering (no distance limit)
- Only visible within 15m range
- Distance display on pings
- Lit item preview with studio lighting
- Doors are now pingeable

### 0.0.1
- Initial release
