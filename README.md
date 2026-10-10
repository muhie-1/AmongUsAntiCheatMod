# Among Us Anti-Cheat Plugin

![License](https://img.shields.io/badge/license-MIT-green.svg)
![Game Version](https://img.shields.io/badge/Among%20Us-v2024.x-blue.svg)

An open-source anti-cheat detection mod for **Among Us** built with **BepInEx (IL2CPP)** and **HarmonyLib**.

## Features

- **Speed Hack Detection:** Monitors player velocity in real-time and flags abnormal speed bursts.
- **Unauthorized Kill Detection:** Prevents non-impostor players from triggering kill actions.
- **Console Logging:** Outputs security warnings directly to the BepInEx debug console.

## Installation

1. Download and install [BepInEx 6 (IL2CPP build)](https://github.com/BepInEx/BepInEx) into your Among Us root directory.
2. Download the latest `AmongUsAntiCheat.dll` release from this repository.
3. Place `AmongUsAntiCheat.dll` inside the `Among Us/BepInEx/plugins/` directory.
4. Launch the game.

## Building from Source

1. Clone the repository:
   ```bash
   git clone [https://github.com/your-username/AmongUsAntiCheat.git](https://github.com/your-username/AmongUsAntiCheat.git)