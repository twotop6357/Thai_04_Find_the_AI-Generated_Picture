# Unity Startup Template Guide

A personal template for starting Unity projects for exhibitions and kiosks.

## Starting a new project

1. On GitHub, choose **Use this template → Create a new repository** for this repository and enter the new project name.
2. Clone the new repository to a short path. Replace YOUR_ACCOUNT/YOUR_PROJECT below with the new address.

```powershell
git -c core.longpaths=true clone https://github.com/YOUR_ACCOUNT/YOUR_PROJECT.git C:\Projects\MyApp
cd C:\Projects\MyApp
git config core.longpaths true
git lfs install --local
git lfs pull
git remote -v
```

3. Install **Unity 2022.3.62f3** and the target platform build modules in Unity Hub. Open the new folder with **Add project from disk**.
4. When package import finishes, open `Assets/Scenes/0_Idle.unity` and check it with Play.
5. In **Edit → Project Settings → Player**, set the Company Name, Product Name and per-platform Application Identifier for the new project, so logs and PlayerPrefs do not mix with other apps.
6. In **File → Build Settings**, check the target platform and the `0_Idle` scene, then build. Verify the screen, input, sound, API, return and shutdown on the installation PC.

## Main places to edit

| Location | Purpose |
|---|---|
| Assets/Scripts/Core/GameManager.cs | Global behavior and return to the first screen |
| Assets/Scripts/Network/APIManager.cs | API integration |
| Assets/Scripts/App/GameLifetimeScope.cs | VContainer dependency registration |
| Assets/Prefabs/GameLifetimeScope.prefab | Automatically created DI root |
| Assets/StreamingAssets/Settings.json | Inactivity return, fps, window focus, API address, hidden close button |
| Assets/StreamingAssets/ShutdownSettings.json | Automatic shutdown per weekday |
| Assets/Settings | URP rendering settings |

useInactivityTimer and resetTime in Settings.json configure the inactivity return. This project enables it with 30 seconds; missing keys are added on startup. targetFrameRate defaults to 60. apiUrl is empty by default. Do not put secret keys in settings files. Automatic shutdown is disabled for every weekday by default. StreamingAssets settings can still be edited after a build.

## Template maintenance and updates

Commit improvements to the template itself to this repository. Do individual project work in a separate repository created with **Use this template**. Template changes do not reach existing projects automatically, so review and move the files you need.

Commit before upgrading packages, run **Tools → Update Stack Packages**, then check Play and a build again. URP, Input System, Addressables and the directly installed DOTween are managed separately.

The shared code com.huliacdev.template remains an external dependency downloaded from https://github.com/wonjeong97/Template.git. Downloading does not change that repository. To manage the shared code yourself, make a separate copy and change the address in Packages/manifest.json.

## Independence and origin

Original: https://github.com/wonjeong97/0_Startup

Base commit: 901e41cff7171e923190cdf6233218586b65b127 (copied 2026-10-18)

This repository is an independent repository with new history, not a fork. The original remote and the existing Unity Cloud project identifiers were removed. Nothing pushes to the original or pulls its changes automatically. External packages are downloaded from their suppliers.

The original root has no LICENSE, so no new license was assigned. Existing third-party copyrights and licenses are preserved. Check the terms of the original work and packages before public redistribution or commercial deployment.

See the [original description](docs/ORIGINAL_README.md) for details of the existing setup and packages. Use this repository's or the new project's address instead of the clone address in that description.

## Troubleshooting

- Filename too long: use a short path such as C:\Projects and the clone command above.
- Package import failure: check the Git installation, GitHub and OpenUPM connectivity, Console errors and the Unity version.
- Only LFS pointers appear: run git lfs install --local and git lfs pull. Clone rather than downloading a ZIP.
- Roslyn build errors: check that the Assets/Plugins/Roslyn DLLs are set to Editor only.
- Shutdown schedule editor: install the .NET 8 SDK, then run Tools → Build Shutdown Scheduler Exe.

The Unity Editor run and build were not verified during this copy.
