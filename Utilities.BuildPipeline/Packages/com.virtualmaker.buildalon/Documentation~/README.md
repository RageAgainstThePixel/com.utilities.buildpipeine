# com.virtualmaker.buildalon

[![Discord](https://img.shields.io/discord/939721153688264824.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/VM9cWJ9rjH) [![openupm](https://img.shields.io/npm/v/com.virtualmaker.buildalon?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.virtualmaker.buildalon/) [![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.virtualmaker.buildalon)](https://openupm.com/packages/com.virtualmaker.buildalon/) [![marketplace](https://img.shields.io/static/v1?label=&labelColor=505050&message=Buildalon%20Actions&color=FF1E6F&logo=github-actions&logoColor=0076D6)](https://github.com/marketplace?query=buildalon)

A Build Pipeline utility package for the [Unity](https://unity.com/) Game Engine.

This package is designed to be use in conjunction with the [Buildalon GitHub Actions](https://github.com/marketplace?query=buildalon).

## Installing

Requires Unity 2019.4 LTS or higher.

The recommended installation method is though the unity package manager and [OpenUPM](https://openupm.com/packages/com.virtualmaker.buildalon).

### Via Unity Package Manager and OpenUPM

- Open your Unity project settings
- Add the OpenUPM package registry:
  - Name: `OpenUPM`
  - URL: `https://package.openupm.com`
  - Scope(s):
    - `com.virtualmaker`

![scoped-registries](images/package-manager-scopes.png)

- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Buildalon` package

### Via Unity Package Manager and Git url

- Open your Unity Package Manager
- Add package from git url: `https://github.com/buildalon/com.virtualmaker.buildalon.git#upm`

## Documentation

### Example Usage

#### Create Github Action Workflow

- Create a new action workflow file:
  - `.github/workflows/unity-build.yml`
- Add the following content to the file:

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
      - uses: buildalon/unity-setup@v1
        with:
          unity-version: ${{ matrix.unity-versions }}
          build-targets: ${{ matrix.build-target }}

        # Activates the installation with the provided credentials
      - uses: buildalon/activate-unity-license@v1
        with:
          license: 'Personal' # Choose license type to use [ Personal, Professional ]
          username: ${{ secrets.UNITY_USERNAME }}
          password: ${{ secrets.UNITY_PASSWORD }}
          # serial: ${{ secrets.UNITY_SERIAL }} # Required for pro activations

      - uses: buildalon/unity-action@v1
        name: Project Validation
        with:
          log-name: 'project-validation'
          args: '-quit -batchmode -executeMethod Buildalon.Editor.BuildPipeline.UnityPlayerBuildTools.ValidateProject -importTMProEssentialsAsset'

      - uses: buildalon/unity-action@v1
        name: '${{ matrix.build-target }}-Build'
        with:
          log-name: '${{ matrix.build-target }}-Build'
          build-target: '${{ matrix.build-target }}'
          args: '-quit -batchmode -executeMethod Buildalon.Editor.BuildPipeline.UnityPlayerBuildTools.StartCommandLineBuild -export'

      - uses: actions/upload-artifact@v4
        id: upload-artifact
        name: 'Upload ${{ matrix.build-target }} Artifacts'
        if: success() || failure()
        with:
          compression-level: 0
          retention-days: 1
          name: '${{ github.run_number }}.${{ github.run_attempt }}-${{ matrix.os }} ${{ matrix.unity-versions }} ${{ matrix.build-target }}-Artifacts'
          path: |
            ${{ github.workspace }}/**/*.log
            ${{ env.UNITY_PROJECT_PATH || github.workspace }}/Builds/${{ matrix.build-target }}/**/*
            !${{ env.UNITY_PROJECT_PATH || github.workspace }}/Library/**/*
            !/**/*_BackUpThisFolder_ButDontShipItWithYourGame/**
            !/**/*_BurstDebugInformation_DoNotShip/**
```

### Executable Methods

These methods can be executed using the `-executeMethod` command line argument to validate, sync, and build the Unity project.

| Method | Description |
| ------ | ----------- |
| `Buildalon.Editor.BuildPipeline.UnityPlayerBuildTools.ValidateProject` | Validates the Unity Project assets by forcing a symbolic link sync and creates solution files. |
| `Buildalon.Editor.BuildPipeline.UnityPlayerBuildTools.SyncSolution` | Force Unity to update CSProj files and generates solution. |
| `Buildalon.Editor.BuildPipeline.UnityPlayerBuildTools.StartCommandLineBuild` | Start a build using command line arguments. |

```bash
"/path/to/Unity.exe" -projectPath "/path/to/unity/project" -quit -batchmode -executeMethod Buildalon.Editor.BuildPipeline.UnityPlayerBuildTools.StartCommandLineBuild
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
