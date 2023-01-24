# com.utilities.buildpipeline

[![openupm](https://img.shields.io/npm/v/com.utilities.buildpipeline?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.buildpipeline/)

[![marketplace](https://img.shields.io/static/v1?label=&labelColor=505050&message=Unity%20Build%20Pipeline%20Utility&color=0076D6&logo=github-actions&logoColor=0076D6)](https://github.com/marketplace/actions/unity-build-pipeline-utility)

A Build Pipeline utility package for the [Unity](https://unity.com/) Game Engine.

## Installing

### Via Unity Package Manager and OpenUPM

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](Utilities.BuildPipeline/Packages/com.utilities.buildpipeline/Documentation~/images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - `Name: OpenUPM`
  - `URL: https://package.openupm.com`
  - `Scope(s):`
    - `com.utilities`
- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.BuildPipeline` package

### Via Unity Package Manager and Git url

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.buildpipeine.git#upm`

## Getting Started

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
  group: ${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        include:
          - os: windows-latest
            build-target: StandaloneWindows64
          - os: macos-latest
            build-target: StandaloneOSX
          - os: ubuntu-latest
            build-target: StandaloneLinux64

    steps:
      - uses: actions/checkout@v3

        # Installs the Unity Editor based on your project version text file
        # sets -> env.UNITY_EDITOR_PATH
        # sets -> env.UNITY_PROJECT_PATH
        # https://github.com/XRTK/unity-setup
      - uses: xrtk/unity-setup@v7.1
        with:
          modules: ${{ matrix.build-target }}

      #   # Activates the installation with the provided credentials
      #   # https://github.com/XRTK/activate-unity-license
      # - uses: xrtk/activate-unity-license@v2
      #   with:
      #     # Required
      #     username: ${{ secrets.UNITY_USERNAME }}
      #     password: ${{ secrets.UNITY_PASSWORD }}
      #     # Optional
      #     license-type: 'Personal' # Chooses license type to use [ Personal, Professional ]
      #     serial: ${{ secrets.UNITY_SERIAL }} # Required for pro/plus activations

      - name: Unity Build (${{ matrix.build-target }})
        uses: RageAgainstThePixel/unity-build@v5
        with:
          build-target: ${{ matrix.build-target }}
```
