# Among Us Anti-Cheat Mod

A lightweight, open-source anti-cheat plugin for Among Us built with **.NET 6.0**, **BepInEx 6**, and **HarmonyLib**. Designed to detect and prevent common exploits and game-breaking cheats in lobbies.

## 🚀 Features

*   **Speed Detection:** Identifies players moving beyond the maximum allowed speed limits.
*   **Noclip Detection:** Prevents players from walking through walls and unauthorized boundaries.
*   **Cooldown Detection:** Catches abnormal or zero kill/ability cooldown manipulations.
*   **Task Cheat Detection:** Monitors and flags invalid task completions.
*   **Vent Detection:** Tracks unauthorized or impossible vent entries and movements.

## 🛠️ Tech Stack

*   **.NET 6.0**
*   **BepInEx 6** (Modding Framework)
*   **HarmonyLib** (Runtime Method Patching)

## 📦 Installation

1. Ensure you have **BepInEx 6** properly installed in your Among Us game directory.
2. Download the latest release `.dll` or build the project from source.
3. Drop the compiled plugin DLL into your `BepInEx/plugins` folder.
4. Launch the game.