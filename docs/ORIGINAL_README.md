# 0_Startup — Unity project template

A template for quickly starting exhibition and kiosk Unity apps.
Common packages, settings and shared manager wiring are already included.

## Usage

**Requirements:** Unity 2022.3.62f3, Git
(Git must be installed because some packages are fetched by Git address.)

The recommended approach is to **keep one folder that tracks only this repository, and start new projects by copying only the needed files from it**.
New projects start clean without the template's git history, and the template folder can be brought up to date at any time with `git pull`.

### 1. Prepare the template folder (once)

```bash
git clone https://github.com/wonjeong97/0_Startup.git
```

Do not work in this folder. Run `git pull` to update it before starting a new project.

### 2. Start a new project

1. Copy only the following six items from the template folder to the new project folder. Do not copy the rest, such as `Library` and `.git`.

   | Copy | Contents |
   |-----------|------|
   | `Assets/` | Scripts, prefabs, scenes, runtime settings files |
   | `Packages/` | Package list and pinned versions |
   | `ProjectSettings/` | Project settings |
   | `.gitignore` | Files excluded from git |
   | `.gitattributes` | Rules storing large images and videos in Git LFS, and DLLs and third-party files as ordinary files |
   | `CLAUDE.md` | Project description skeleton (for Claude Code) |

2. In Unity Hub, open the new folder with **Add > Add project from disk**. The first time takes a few minutes to fetch packages.
3. In **Edit > Project Settings > Player**, change **Product Name** to the new project's name.
   Company Name and Product Name determine the app log folder and PlayerPrefs location, so leaving them mixes with other apps installed on the same PC.

> Template changes do not reach projects already started.
> Upgrade the shared code (HuliacDev Template) and stack packages with `Tools > Update Stack Packages`, and pick and move only the files you need for other changes.

A folder downloaded with GitHub's **Code > Download ZIP** also works as the template folder, but must be downloaded again instead of using `git pull` to update.

## What's included

| Category | Packages |
|------|--------|
| Architecture | [VContainer](https://github.com/hadashiA/VContainer) (DI), [UniTask](https://github.com/Cysharp/UniTask) (async), [MessagePipe](https://github.com/Cysharp/MessagePipe) (Pub/Sub), [R3](https://github.com/Cysharp/R3) (reactive) |
| Utilities | [ZLogger](https://github.com/Cysharp/ZLogger) (logging), [ZString](https://github.com/Cysharp/ZString) (strings), [DOTween](https://dotween.demigiant.com/) (tweens) |
| Shared code | [HuliacDev Template](https://github.com/wonjeong97/Template) — GameManagerBase, inactivity return, automatic shutdown, UI, Fade, Sound and Video managers, API and Arduino managers, log retention |
| Unity | URP 14, Input System, Addressables, TextMeshPro, Timeline, Visual Scripting, Test Framework, Memory Profiler, Profile Analyzer |
| Dev tools | [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity), [MCP for Unity](https://github.com/CoplayDev/unity-mcp) (Claude Code integration) |

## Default setup

- **DI root:** `Assets/Prefabs/GameLifetimeScope.prefab` is the Root Lifetime Scope in VContainerSettings, so it is created automatically without being placed in a scene.
  It contains GameManager, APIManager, InactivityTimer, ShutdownScheduler and Reporter (log viewer).
- **Runtime settings:** `Assets/StreamingAssets/Settings.json` (inactivity return, frame rate, window focus restore and always on top, API address, hidden close button) and
  `ShutdownSettings.json` (automatic shutdown per weekday) can be edited after a build.
- **Automatic shutdown settings program:** `ShutdownScheduleEditor.exe` is not in the repository because of its size (about 150 MB).
  Build it with `Tools > Build Shutdown Scheduler Exe` if needed (requires the .NET 8 SDK).
- **Rendering:** UI-oriented performance settings (URP-Performant, camera post-processing off) are the default. For 3D, turn on High Fidelity and post-processing.
- **Roslyn:** `Assets/Plugins/Roslyn` is only for MCP for Unity and is set to Editor only.
  If you delete and reinstall the folder, check only Editor again in the Inspector.

## Package updates

`Tools > Update Stack Packages` upgrades only VContainer, UniTask, MessagePipe, R3, ZLogger, ZString, HuliacDev Template, NuGetForUnity and MCP for Unity.
It does not touch version-pinned packages such as URP, Input System and Addressables, or DOTween placed directly in `Assets/Plugins`.

## Folder structure

```
Assets/
├── Input/            # Input actions
├── Packages/         # NuGet packages (NuGetForUnity)
├── Plugins/          # DOTween, Roslyn (Editor only)
├── Prefabs/          # GameLifetimeScope.prefab
├── Scenes/           # 0_Idle.unity (first scene)
├── Scripts/          # GameLifetimeScope, GameManager, APIManager
├── Settings/         # URP assets
├── StreamingAssets/  # Settings.json, ShutdownSettings.json
└── UI/
Packages/
├── manifest.json       # Package list
└── packages-lock.json  # Pinned package versions
```
