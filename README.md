# OpenSpeed Classic

OpenSpeed Classic is a .NET 10 arcade racing game built on MonoGame via the NuciXNA package family. It loads and renders track geometry, placed scenery, materials, textures, horizons, and a selected player car from an original Need for Speed II Special Edition installation.

## Project Structure

- [OpenSpeed.Classic.slnx](OpenSpeed.Classic.slnx) contains the solution.
- [OpenSpeed.Classic](OpenSpeed.Classic) contains the game executable project.
- [OpenSpeed.Classic.UnitTests](OpenSpeed.Classic.UnitTests) contains the NUnit unit test project.
- [OpenSpeed.Classic/OpenSpeedClassicGame.cs](OpenSpeed.Classic/OpenSpeedClassicGame.cs) owns the MonoGame lifecycle, driving input, and track rendering.
- [OpenSpeed.Classic/Cars](OpenSpeed.Classic/Cars) contains renderer-neutral car models, NFS II car asset decoding, and loading orchestration.
- [OpenSpeed.Classic/Input](OpenSpeed.Classic/Input) contains keyboard-to-driving input mapping.
- [OpenSpeed.Classic/Rendering/Cars](OpenSpeed.Classic/Rendering/Cars) contains car vertex conversion, GPU resources, world placement, and rendering.
- [OpenSpeed.Classic/Rendering/Tracks](OpenSpeed.Classic/Rendering/Tracks) contains the track camera, horizon, vertex conversion, neighbour visibility, dynamic LOD, GPU batches, and renderer.
- [OpenSpeed.Classic/Tracks](OpenSpeed.Classic/Tracks) contains renderer-neutral track models and loading contracts.
- [OpenSpeed.Classic/Tracks/NeedForSpeed2](OpenSpeed.Classic/Tracks/NeedForSpeed2) contains the Need for Speed II catalogue and binary decoders.

## Original Assets

Original game assets are not distributed with this project. Configure an installation root and initial track in [OpenSpeed.Classic/appsettings.json](OpenSpeed.Classic/appsettings.json):

```json
{
  "Assets": {
    "Sources": [
      {
        "Game": "NeedForSpeed2SpecialEdition",
        "RootDirectory": "/path/to/NFS2 SE",
        "OverridesDirectory": "/path/to/NFS2 SE"
      }
    ]
  },
  "Controls": {
    "Accelerate": {
      "Primary": "Up",
      "Secondary": "W"
    },
    "Reverse": {
      "Primary": "Down",
      "Secondary": "S"
    },
    "Handbrake": {
      "Primary": "Space",
      "Secondary": "LeftShift"
    },
    "SteerLeft": {
      "Primary": "Left",
      "Secondary": "A"
    },
    "SteerRight": {
      "Primary": "Right",
      "Secondary": "D"
    }
  },
  "Rendering": {
    "AreShadowsEnabled": true,
    "Is3DfxEnabled": false
  },
  "NuciLoggerSettings": {
    "LogFilePath": "logfile.log",
    "IsFileOutputEnabled": true
  },
  "StartupCar": {
    "Game": "NeedForSpeed2SpecialEdition",
    "Identifier": "McLarenF1"
  },
  "StartupTrack": {
    "Game": "NeedForSpeed2SpecialEdition",
    "Identifier": "Outback"
  }
}
```

Relative asset roots are resolved from the process working directory. Paths within an original Windows installation are resolved case-insensitively on every platform. Each asset is loaded from `OverridesDirectory` when present there, otherwise it is loaded from `RootDirectory`. Omitting `OverridesDirectory` uses the configured `RootDirectory`.

`AreShadowsEnabled` controls the track's baked per-vertex shadow lighting. `Is3DfxEnabled` selects the `SE` 3dfx texture archive when enabled and the `PC` software-renderer archive when disabled.

Track, horizon, and car textures receive complete mipmap chains and anisotropic filtering to reduce distant aliasing and shimmer.

The player car is loaded from `gamedata/sim/cardata/cardata.viv` and `gamedata/carmodel/pc` beneath the configured asset root. It is positioned at the first decoded route point, constrained by the decoded track walls, aligned to slopes, affected by gravity, controlled by the configured keyboard bindings, and followed by the camera. Omitting `StartupCar` selects `McLarenF1`.

For compatibility with existing configurations, omitting `Is3DfxEnabled` uses the asset source's `TextureVariant`. `TextureVariant` accepts `PC` or `SE` and defaults to `SE` when omitted.

Supported track identifiers are `ProvingGrounds`, `Outback`, `LastResort`, `NorthCountry`, `PacificSpirit`, `Mediterraneo`, `MysticPeaks`, and `MonolithicStudios`.

Supported car identifiers are `McLarenF1`, `FerrariF50`, `FerrariF355`, `FordGT90`, `FordIndigo`, `FordMustangMachIII`, `JaguarXJ220`, `LotusGT1`, `LotusEspritV8`, `ItaldesignNazcaC2`, `ItaldesignCala`, `IsderaCommendatore`, `BonusCarChevrolet`, `BonusCarDaytona`, and `BonusCarFuture`.

The generic loading service selects an `ITrackFormatLoader` by game version. Additional NFS1 or NFS3 implementations can provide their own catalogue and format decoders while returning the same renderer-neutral `LoadedTrack` model.

## Development

Install the .NET 10 SDK, then restore and build the solution:

```sh
dotnet build OpenSpeed.Classic.slnx
```

Run the unit tests with:

```sh
dotnet test OpenSpeed.Classic.UnitTests/OpenSpeed.Classic.UnitTests.csproj
```

Run the game project with:

```sh
dotnet run --project OpenSpeed.Classic/OpenSpeed.Classic.csproj
```

Capture one rendered frame to a PNG file and exit with:

```sh
dotnet run --project OpenSpeed.Classic/OpenSpeed.Classic.csproj -- \
  --capture-frame /tmp/openspeed-frame.png
```

On a Wayland session where X11 reports an authorisation error, run:

```sh
env -u DISPLAY SDL_VIDEODRIVER=wayland \
	dotnet run --project OpenSpeed.Classic/OpenSpeed.Classic.csproj
```

## Controls
- `Up Arrow` or `W`: Accelerate.
- `Down Arrow` or `S`: Brake, then reverse after stopping.
- `Space` or `Left Shift`: Apply the handbrake.
- `Left Arrow` or `A`: Steer left.
- `Right Arrow` or `D`: Steer right.
- `Escape`: Exit.

Every driving action has a `Primary` and `Secondary` key in the `Controls` section of [OpenSpeed.Classic/appsettings.json](OpenSpeed.Classic/appsettings.json). Both bindings must be valid, distinct MonoGame key names.

The car accelerates progressively, retains momentum while coasting, decelerates under drag, and brakes to a stop before engaging the opposite direction. Steering while applying the handbrake initiates a speed-retaining arcade powerslide with increased yaw and rear slip. Counter-steering stabilises the slide, while releasing the handbrake progressively restores tyre grip. Normal steering uses a wheelbase-based vehicle model, remains responsive at low velocity, reduces steering angle at high velocity, and reverses while travelling backwards.

Asset file hits and misses are written by NuciLog to the console and, when `IsFileOutputEnabled` is enabled, to the configured `LogFilePath`.

## Licence

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE) for details.
