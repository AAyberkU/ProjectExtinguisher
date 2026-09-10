# 🧯 Project Extinguisher

**Project Extinguisher** is a 2D isometric, hex-based puzzle game developed in Unity. Navigate Larry through a grid of hexagonal tiles, strategically utilizing limited moves, avoiding obstacles, and using special tiles like catapults and health boosts to reach the goal.

## 🎮 Gameplay Overview

- **Grid System:** Pointy-top axial hex board (61 cells total).
- **Movement:** Larry moves step-by-step to adjacent walkable tiles. Each move consumes points.
- **Special Tiles:** 
  - 🛑 **Blocked:** Impassable terrain.
  - 🚀 **Catapults:** Launches Larry across the board in specific directions, chaining jumps together.
  - 💖 **Health/Move Bonus:** Grants extra moves to keep your run alive.
- **Goal:** Reach the exit tile before your move counter hits zero!

## 🤖 Development Approach: The Agentic Workflow

A key highlight of this project is its development process. **Project Extinguisher** was built leveraging an **Agentic AI Workflow**. 

Instead of traditional ad-hoc prompting, the project maintains repository-wide agent guidance in `AGENTS.md` and living documentation in the `docs/` folder (`design.md`, `progress.md`, and `bugs.md`). These markdown files serve as the explicit memory and structural source of truth for AI coding assistants.

By keeping a continuous, updated documentation loop:
- **Context is Preserved:** The AI consistently understands the single-scene architecture and data structures without needing to blindly scan the entire codebase.
- **Rapid & Cohesive Iteration:** Complex systems (like the hex coordinate math and catapult chain-reactions) were implemented seamlessly because the agent always referred back to the core `design.md`.
- **Structured Progress:** Bugs and completed tasks are tracked in `progress.md` and `bugs.md`, allowing the agentic workflow to pick up exactly where the last session left off.

This repository serves not just as a game, but as a practical example of how to organize and structure a Unity project to collaborate efficiently with autonomous AI coding agents.

## 🛠️ Technical Details

- **Engine:** Unity (2D)
- **Architecture:** Single-scene design (`GameplayScene`). The game does not load new scenes for new levels.
- **Level Design:** Levels are authored as `ScriptableObjects` (`LevelData`). A `LevelLoader` dynamically applies this data to a shared pool of hex cell prefabs at runtime.
- **Input:** Modern Unity Input System with robust UI click-guarding.

## 🚀 Getting Started

1. Clone the repository.
2. Open the project in Unity.
3. Open `Assets/Scenes/GameplayScene.unity`.
4. Press **Play**! The `LevelLoader` handles initializing the board based on the selected starting `LevelData`.

## 📁 Key Directories
- `Assets/Scripts/`: Core game logic (`HexGridManager`, `TileActivationController`, `LarryController`).
- `Assets/Levels/`: `ScriptableObject` assets defining each level's constraints and layout.
- `Assets/Art/`: Isometric sprites and UI elements.
- `docs/`: The core documentation driving the AI agentic workflow.

---
*Built with ❤️ and 🤖.*
