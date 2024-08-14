# com.utilities.buildpipeline

[![Discord](https://img.shields.io/discord/855294214065487932.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/xQgMW9ufN4) [![openupm](https://img.shields.io/npm/v/com.utilities.buildpipeline?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.buildpipeline/) [![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.utilities.buildpipeline)](https://openupm.com/packages/com.utilities.buildpipeline/) [![marketplace](https://img.shields.io/static/v1?label=&labelColor=505050&message=Unity%20Build%20Pipeline%20Utility&color=0076D6&logo=github-actions&logoColor=0076D6)](https://github.com/marketplace/actions/unity-build-pipeline-utility)

A Build Pipeline utility package for the [Unity](https://unity.com/) Game Engine.

## Installing

Requires Unity 2019.4 LTS or higher.

The recommended installation method is though the unity package manager and [OpenUPM](https://openupm.com/packages/com.openai.unity).

### Via Unity Package Manager and OpenUPM

- Open your Unity project settings
- Add the OpenUPM package registry:
  - Name: `OpenUPM`
  - URL: `https://package.openupm.com`
  - Scope(s):
    - `com.utilities`

![scoped-registries](Utilities.BuildPipeline/Packages/com.utilities.buildpipeline/Documentation~/images/package-manager-scopes.png)

- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.BuildPipeline` package

### Via Unity Package Manager and Git url

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.buildpipeine.git#upm`

## Documentation

This package is designed to be use in conjunction with a CI/CD pipeline, such as [![marketplace](https://img.shields.io/static/v1?label=&labelColor=505050&message=Unity%20Build%20Pipeline%20Utility&color=0076D6&logo=github-actions&logoColor=0076D6)](https://github.com/marketplace/actions/unity-build-pipeline-utility).

### Example Usage

#### Create Github Action Workflow

1. Create a new action workflow file using [![marketplace](https://img.shields.io/static/v1?label=&labelColor=505050&message=Unity%20Build%20Pipeline%20Utility&color=0076D6&logo=github-actions&logoColor=0076D6)](https://github.com/marketplace/actions/unity-build-pipeline-utility)
`.github/workflows/unity-build.yml`

2. Add the following content to the file:

```yml
name: unity-build

on:
  push:
    branches:
      - 'main'
  pull_request:
    branches:
      - '*'
  # Allows you to run this workflow manually from the Actions tab
  workflow_dispatch:
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      # max-parallel: 2 # Use this if you're activating pro license with matrix
      matrix:
        os: [ubuntu-latest, windows-latest, macos-13]
        unity-versions:
          - 2019.4.40f1 (ffc62b691db5)
          - 2020.3.48f1 (b805b124c6b7)
          - 2021.3.41f1 (6c5a9e20c022)
          - 2022.3.40f1 (cbdda657d2f0)
          - 6000.0.13f1 (53a692e3fca9)
        include: # for each os specify the build targets
          - os: ubuntu-latest
            build-target: StandaloneLinux64
          - os: windows-latest
            build-target: StandaloneWindows64
          - os: macos-13
            build-target: StandaloneOSX

    steps:
      - uses: actions/checkout@v4

        # Installs the Unity Editor based on your project version text file
        # sets -> env.UNITY_EDITOR_PATH
        # sets -> env.UNITY_PROJECT_PATH
      - uses: RageAgainstThePixel/unity-setup@v1
        with:
          unity-version: ${{ matrix.unity-versions }}
          build-targets: ${{ matrix.build-target }}

        # Activates the installation with the provided credentials
      - uses: RageAgainstThePixel/activate-unity-license@v1
        with:
          license: 'Personal' # Choose license type to use [ Personal, Professional ]
          username: ${{ secrets.UNITY_USERNAME }}
          password: ${{ secrets.UNITY_PASSWORD }}
          # serial: ${{ secrets.UNITY_SERIAL }} # Required for pro activations

      - name: Unity Build (${{ matrix.build-target }})
        uses: RageAgainstThePixel/unity-build@v1
        with:
          build-target: ${{ matrix.build-target }}
```

### Executable Methods

These methods can be executed using the `-executeMethod` command line argument to validate, sync, and build the Unity project.

| Method | Description |
| ------ | ----------- |
| `Utilities.Editor.BuildPipeline.UnityPlayerBuildTools.ValidateProject` | Validates the Unity Project assets by forcing a symbolic link sync and creates solution files. |
| `Utilities.Editor.BuildPipeline.UnityPlayerBuildTools.SyncSolution` | Force Unity to update CSProj files and generates solution. |
| `Utilities.Editor.BuildPipeline.UnityPlayerBuildTools.StartCommandLineBuild` | Start a build using command line arguments. |

```bash
"/path/to/Unity.exe" -projectPath "/path/to/unity/project" -quit -batchmode -executeMethod Utilities.Editor.BuildPipeline.UnityPlayerBuildTools.StartCommandLineBuild
```

### Additional Custom Command Line Arguments

In addition to any already defined [Unity Editor command line arguments](https://docs.unity3d.com/Manual/EditorCommandLineArguments.html), this plugin offers some additional options:

| Argument | Description |
| -------- | ----------- |
| `-ignoreCompilerErrors` | Disables logging. |
| `-autoIncrement` | Enables auto incrementing. |
| `-versionName` | Sets the version of the application. Value must be string. |
| `-versionCode` | Sets the version code of the application. Value must be an integer. |
| `-bundleIdentifier` | Sets the bundle identifier of the application. |
| `-sceneList` | Sets the scenes of the application, list as CSV. |
| `-sceneListFile` | Sets the scenes of the application, list as JSON. |
| `-buildOutputDirectory` | Sets the output directory for the build. |
| `-acceptExternalModificationsToPlayer` | Sets the build options to accept external modifications to the player. |
| `-development` | Sets the build options to build a development build of the player. |
| `-colorSpace` | Sets the color space of the application, if the provided color space string is a valid `ColorSpace` enum value. |
| `-buildConfiguration` | Sets the build configuration of the application. Can be:  `debug`, `master`, or `release`. |
| `-export` | Creates a native code project for the target platform. |
| `-symlinkSources` | Enables the use of symbolic links for the sources. |
| `-disableDebugging` | :warning: deprecated. Use `allowDebugging`. Disables the ability to attach remote debuggers to the player. |
| `-allowDebugging` | Enables or disables the ability to attache a remote debugger to the player. Can be: `true` or `false`. |
| `-dotnetApiCompatibilityLevel` | Sets the dotnet api compatibility level of the player. Can be: `NET_2_0`, `NET_2_0_Subset`, `NET_4_6`, `NET_Unity_4_8`, `NET_Web`, `NET_Micro`, `NET_Standard`, or `NET_Standard_2_0`. |
| `-scriptingBackend` | Sets the scripting framework of the player. Can be: `Mono2x`, `IL2CPP`, or `WinRTDotNET`. |
| `-autoConnectProfiler` | Start the player with a connection to the profiler. |
| `-buildWithDeepProfilingSupport` | Enables deep profiling support in the player. |

#### Platform specific Command Line Arguments

##### Android Command Line Arguments

| Argument | Description |
| -------- | ----------- |
| `-splitBinary` | Builds an APK per CPU architecture. |
| `-splitApk` | Uses APK expansion files. |
| `-keyaliasPass` | Sets the key alias password. |
| `-keystorePass` | Sets the keystore password. |
| `-symbols` | Sets the symbol creation mode. Can be: `public`, `debugging`, or `disabled`. |

##### MacOS COmmand Line Arguments

| Argument | Description |
| -------- | ----------- |
| `-arch` | Sets the build architecture. Can be: `x64`, `arm64`, or `x64arm64`. |

#### Project Validation Command Line Arguments

| Argument | Description |
| -------- | ----------- |
| `-importTMProEssentialsAsset` | Imports the TMPro Essential assets if they are not already in the project. |
