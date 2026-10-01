# OpenSpeed Classic

OpenSpeed Classic is a .NET 10 arcade racing game built on MonoGame via the NuciXNA package family. It loads and renders track geometry, placed scenery, materials, textures, horizons, and a selected player car from an original Need for Speed II Special Edition installation.

## Project Structure

- [OpenSpeed.Classic.slnx](OpenSpeed.Classic.slnx) contains the solution.
- [OpenSpeed.Classic](OpenSpeed.Classic) contains the game executable project.
- [OpenSpeed.Classic.UnitTests](OpenSpeed.Classic.UnitTests) contains the NUnit unit test project.
- [OpenSpeed.Classic/OpenSpeedClassicGame.cs](OpenSpeed.Classic/OpenSpeedClassicGame.cs) owns the MonoGame lifecycle, driving input, and track rendering.
- [OpenSpeed.Classic/Cars](OpenSpeed.Classic/Cars) contains renderer-neutral car models, NFS II car asset decoding, and loading orchestration.
- [OpenSpeed.Classic/Input](OpenSpeed.Classic/Input) contains keyboard-to-driving input mapping.
- [OpenSpeed.Classic/Physics](OpenSpeed.Classic/Physics) contains the inactive, assembly-derived physics port described under [Physics Port Checkpoint](#physics-port-checkpoint).
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
    "Is3DfxEnabled": false,
    "IsVignetteEnabled": true,
    "MotionBlurMinimumSpeedKilometresPerHour": 80.0,
    "ScreenHeight": 968,
    "ScreenWidth": 1720
  },
  "NuciLoggerSettings": {
    "LogFilePath": "logfile.log",
    "IsFileOutputEnabled": true
  },
  "StartupCar": {
    "Game": "NeedForSpeed2SpecialEdition",
    "Identifier": "McLarenF1",
    "Colour": "#7CB342"
  },
  "StartupTrack": {
    "Game": "NeedForSpeed2SpecialEdition",
    "Identifier": "Outback"
  }
}
```

Relative asset roots are resolved from the process working directory. Paths within an original Windows installation are resolved case-insensitively on every platform. Each asset is loaded from `OverridesDirectory` when present there, otherwise it is loaded from `RootDirectory`. Omitting `OverridesDirectory` uses the configured `RootDirectory`.

`AreShadowsEnabled` controls the track's baked per-vertex shadow lighting. `Is3DfxEnabled` selects the `SE` 3dfx texture archive when enabled and the `PC` software-renderer archive when disabled. `MotionBlurMinimumSpeedKilometresPerHour` sets the speed above which temporal motion blur becomes active. A central oval remains free of motion blur, which intensifies gradually from its boundary towards every screen edge. `IsVignetteEnabled` toggles an independent oval vignette that reaches 15% darkness only at the screen boundary. `ScreenWidth` and `ScreenHeight` configure the window resolution and must both be positive.

Track, horizon, and car textures receive complete mipmap chains and anisotropic filtering to reduce distant aliasing and shimmer.

The player car is loaded from `gamedata/sim/cardata/cardata.viv` and `gamedata/carmodel/pc` beneath the configured asset root. Car geometry is enlarged uniformly while retaining matching physical wall clearance. The car is positioned at the first decoded route point, constrained by the decoded track walls, aligned to slopes, affected by gravity, controlled by the configured keyboard bindings, and followed by the camera. Uphill momentum is preserved at convex crests, permitting sufficiently rapid cars to become briefly airborne before gravity returns them to the road. Omitting `StartupCar` selects `McLarenF1`.

`StartupCar.Colour` optionally specifies the car paint as six hexadecimal RGB digits, with or without a leading `#`, for example `"#7CB342"`. Letter casing is ignored. Omitting the property or setting it to `null` preserves the default car colour. Custom paint retains texture shading and non-paint details, and the minimap arrow uses the configured RGB colour. Invalid colour values are reported during configuration loading.

For compatibility with existing configurations, omitting `Is3DfxEnabled` uses the asset source's `TextureVariant`. `TextureVariant` accepts `PC` or `SE` and defaults to `SE` when omitted.

Supported track identifiers are `ProvingGrounds`, `Outback`, `LastResort`, `NorthCountry`, `PacificSpirit`, `Mediterraneo`, `MysticPeaks`, and `MonolithicStudios`.

Supported car identifiers are `McLarenF1`, `FerrariF50`, `FerrariF355`, `FordGT90`, `FordIndigo`, `FordMustangMachIII`, `JaguarXJ220`, `LotusGT1`, `LotusEspritV8`, `ItaldesignNazcaC2`, `ItaldesignCala`, `IsderaCommendatore`, `BonusCarChevrolet`, `BonusCarDaytona`, and `BonusCarFuture`.

The lower-left minimap displays the road centreline within 400 metres of the player, centred on a marker that uses the car's paint colour. It rotates with the car's heading, so forward remains upwards, independently of the camera. The route uses a uniform line width with rounded joins, independent of the driveable area's width, and is clipped to the circular [minimap background](OpenSpeed.Classic/Content/Minimap.png). Both the road lines and player arrow have narrow pure black outlines. The minimap is drawn at double resolution and downsampled with linear filtering to antialias its lines, player marker, and circular boundary.

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

### Physics Port Checkpoint

Checkpoint: 2026-10-01. The assembly-derived physics replacement is **incomplete and inactive**. The game still uses [CarWorldTransformUpdater](OpenSpeed.Classic/Rendering/Cars/CarWorldTransformUpdater.cs) and the existing driving model. The previous [FixedPointCarSolver](OpenSpeed.Classic/Rendering/Cars/FixedPointCarSolver.cs) is a separate partial implementation, not the replacement simulation.

The [physics components](OpenSpeed.Classic/Physics) retain x86 arithmetic, executable lookup tables, overlapping native car-state fields, descriptor finalisation, integer vectors/matrices, controls, drivetrain, two-channel contact response, the main force-solver candidate, pair-collision impulses, and ordered 32/64 Hz scheduling. The [physics tests](OpenSpeed.Classic.UnitTests/Physics) cover 91 selected fixtures and regression cases; this is not complete branch coverage or original-execution parity.

Asset loading now retains `p<car resource>.dat` and `SimTune.dat` from the original car archive and unconverted XBID 15 track records. Descriptor finalisation and simulation callbacks are not invoked by gameplay. The three executable angle-table counts and SHA-256 hashes correspond exactly to the supplied specification. Missing physics archive members are reported as loading errors rather than replaced with invented values.

Remaining integration requires the alternate contact/tyre state machine, original track-surface queries and body correction, OBB contact generation and iterative resolution, world-contact impulses, collision-event consumption, launch/recovery, runtime car-type initialisation, role-specific AI/replay/special-car callbacks, and a one-way rendering adapter. The dispatcher accepts explicit callbacks for unfinished paths; no surrogate solver is installed. Original full-tick trace fixtures have not been provided, so native parity remains unverified. Preserve the current driving path until these dependencies are completed and validated.

Run the checkpoint fixtures with:

```sh
dotnet test OpenSpeed.Classic.UnitTests/OpenSpeed.Classic.UnitTests.csproj \
  --filter FullyQualifiedName~UnitTests.Physics
```

The corresponding checkpoint and source discrepancies are recorded in section 27 of the supplied external `CAR_PHYSICS_ASM_SPEC.md`. That external reference and its assembly remain outside this repository; they are not runtime dependencies.

### Running And Capturing

Run the game project with:

```sh
dotnet run --project OpenSpeed.Classic/OpenSpeed.Classic.csproj
```

Capture one rendered frame, including the HUD, to a PNG file and exit with:

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
