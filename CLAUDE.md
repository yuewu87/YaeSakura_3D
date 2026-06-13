# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

YaeSakura_3D is a Unity 2022.3.62f1c1 project (currently early-stage — the only scene is `Assets/Scenes/SampleScene.unity`). No C# scripts or assembly definitions exist yet.

## Unity Editor Integration (Funplay MCP)

This project uses Funplay MCP to bridge Claude Code with the Unity Editor. The MCP server runs at `http://127.0.0.1:8766/` (configured in `.mcp.json`).

Key MCP tools available:
- `execute_code` — Compile and run C# snippets in the editor (supports `IFunplayCommand` interface for undo-tracked object creation/modification)
- `find_game_objects`, `get_hierarchy`, `get_selection`, `set_selection` — Scene inspection and navigation
- `get_component_properties`, `set_component_property`, `set_component_properties` — Read/write component fields
- `enter_play_mode`, `exit_play_mode`, `simulate_key_press`, `simulate_mouse_click` — Play mode control
- `capture_game_view`, `capture_scene_view` — Screenshots
- `request_recompile`, `wait_for_compilation`, `get_compilation_errors` — Script compilation
- `get_console_logs` — Read Debug.Log output

**After editing `.cs` files externally**, call `request_recompile` (or `wait_for_compilation`) so Unity picks up changes before entering play mode or running code.

## Common Commands

No build pipeline or test suite exists yet. When they do:
- **Open project**: Open the repo root in Unity Hub (editor version 2022.3.62f1c1).
- **Run tests**: Unity Test Runner via Window → General → Test Runner, or `execute_code` with test runner APIs.

## Architecture

- **No assembly definitions**: All scripts placed in `Assets/` compile into `Assembly-CSharp.dll` by default. When the project grows, add `.asmdef` files to partition code into assemblies.
- **Package dependencies** (from `Packages/manifest.json`): TextMeshPro, Timeline, UGUI (Unity UI), Visual Scripting, and all standard built-in modules (physics, animation, audio, etc.).
- **The Funplay MCP package** (`com.gamebooom.unity.mcp`) is installed from GitHub — this is what enables the Claude Code ↔ Unity bridge. Do not remove it without understanding the impact on editor tooling.

## Version Control

- Binary assets (3D models, textures, audio, fonts) are tracked via **Git LFS** — see `.gitattributes` for the full list.
- Library/, Temp/, Logs/, UserSettings/ are gitignored (standard Unity template).
- `.meta` files are committed alongside assets — never delete or skip them.
