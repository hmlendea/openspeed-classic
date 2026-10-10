# OpenSpeed Classic

OpenSpeed Classic is a .NET 10 arcade racing game built on MonoGame via the NuciXNA package family. It loads and renders track geometry, placed scenery, materials, textures, horizons, and a selected player car from an original Need for Speed II Special Edition installation.

## Project Structure

- [OpenSpeed.Classic.slnx](OpenSpeed.Classic.slnx) contains the solution.
- [OpenSpeed.Classic](OpenSpeed.Classic) contains the game executable project.
- [OpenSpeed.Classic.UnitTests](OpenSpeed.Classic.UnitTests) contains the NUnit unit test project.
- [OpenSpeed.Classic/OpenSpeedClassicGame.cs](OpenSpeed.Classic/OpenSpeedClassicGame.cs) owns the MonoGame lifecycle, driving input, and track rendering.
- [OpenSpeed.Classic/Cars](OpenSpeed.Classic/Cars) contains renderer-neutral car models, NFS II car asset decoding, and loading orchestration.
- [OpenSpeed.Classic/Input](OpenSpeed.Classic/Input) contains keyboard-to-driving input mapping.
- [OpenSpeed.Classic/Physics](OpenSpeed.Classic/Physics) contains the active fixed-point player simulation and the partial assembly-derived port described under [Physics Port Checkpoint](#physics-port-checkpoint).
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

The player car is loaded from `gamedata/sim/cardata/cardata.viv` and `gamedata/carmodel/pc` beneath the configured asset root. The car is positioned at the first native route point and driven by fixed-point controls, drivetrain, and force integration. Provisional route-plane support, corridor walls, and airborne gravity retain basic track interaction while the original collision and recovery paths remain incomplete. The camera follows the simulation output. Omitting `StartupCar` selects `McLarenF1`.

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

Checkpoint: 2026-10-01. The assembly-derived physics replacement is **active by default, but incomplete**. [PlayerVehicleSimulation](OpenSpeed.Classic/Physics/PlayerVehicleSimulation.cs) replaces the old driving updater without a toggle. Keyboard sampling, control smoothing, automatic gearing, engine torque, front/rear contact forces, and motion integration now operate on persistent fixed-point state. Controls and forces run at 32 Hz; position and basis integration run at 64 Hz. The previous [CarWorldTransformUpdater](OpenSpeed.Classic/Rendering/Cars/CarWorldTransformUpdater.cs) and [FixedPointCarSolver](OpenSpeed.Classic/Rendering/Cars/FixedPointCarSolver.cs) remain available to existing callers/tests but no longer control gameplay.

The [physics components](OpenSpeed.Classic/Physics) retain x86 arithmetic, executable lookup tables, overlapping native car-state fields, descriptor finalisation, integer vectors/matrices, controls, drivetrain, two-channel contact response, the main force solver, pair-collision impulses, and ordered 32/64 Hz scheduling. The [physics tests](OpenSpeed.Classic.UnitTests/Physics) include selected native fixtures, provisional-contact tests, and explicit driving integration tests using original car/track assets. This is not complete branch coverage or original-execution parity.

The continuation implements these independently specified operations:
- [ContactGeometry](OpenSpeed.Classic/Physics/ContactGeometry.cs): contact-plane height, query-relative height, and oriented support-plane correction, including native sentinel and divisor thresholds.
- [RouteBasis](OpenSpeed.Classic/Physics/RouteBasis.cs): signed route-record loading without normalisation and primary/secondary basis copying with right/forward reflection. It does not implement extension-data queries or the complete placement callback.
- [AiMotion](OpenSpeed.Classic/Physics/AiMotion.cs): lateral-target smoothing, lateral-velocity calculation, and world-velocity composition. The scalar-speed controller and complete AI callback graph remain separate unfinished dependencies.
- [TyreResponseFinaliser](OpenSpeed.Classic/Physics/TyreResponseFinaliser.cs): response-state/surface scaling and final grip calculation with sign-preserving, direction-dependent clamps. The caller must supply the upstream requested force, wheel target, signed adjustment, and transition state; their missing calculation is not approximated.

Asset loading retains `p<car resource>.dat` and `SimTune.dat` from the original car archive and unconverted XBID 15 track records. Gameplay finalises a private descriptor copy once and registers the player simulation callbacks. The three executable angle-table counts and SHA-256 hashes correspond exactly to the supplied specification. Missing physics archive members are reported as loading errors rather than replaced with invented values.

[PhysicsRenderAdapter](OpenSpeed.Classic/Rendering/Cars/PhysicsRenderAdapter.cs) converts native position/basis to MonoGame coordinates and exposes longitudinal speed to the camera, speedometer, and post-processing. Rendering never writes simulation state. The minimap reads the resulting car transform.

[RouteIndexSearch](OpenSpeed.Classic/Physics/RouteIndexSearch.cs) selects native route records with the original wrapped metrics, strict circular walk, and coarse fallback rather than scanning every segment. [PlayerStateInitialiser](OpenSpeed.Classic/Physics/PlayerStateInitialiser.cs) supplies recovered neutral startup, reset fields, and linear/angular coefficients. Forward/reverse input then selects the drive range.

[ProvisionalRouteContact](OpenSpeed.Classic/Physics/ProvisionalRouteContact.cs) still supplies local segment interpolation, plane support, slope alignment, and corridor-wall detection. Wall response now uses [WorldContactImpulse](OpenSpeed.Classic/Physics/WorldContactImpulse.cs), including the assembly-derived coupled capacity calculation, tangential friction, and collision-event writes. Detection still assumes ordinary road material, a one-native-unit car half-width, and centre-applied wall contacts; this is not the original polygon/OBB collision solver. The player profile uses automatic transmission, profiled input smoothing, unit tuning scales, and the primary solver mode. These profile choices remain provisional. With no race countdown, automatic forward-range forcing during the original countdown window is bypassed so reverse remains usable immediately. Opposite-direction input brakes before changing range.

Still deferred: alternate contact/tyre state machine, original surface-query/body correction, OBB contact generation and the complete world-contact caller, collision-event consumption, exact launch/recovery, original race-profile construction, and complete AI/replay/special-car callbacks. The complete simulation tuning data is retained but its original contact/recovery consumers are not active. Original full-tick trace fixtures have not been provided, so native parity remains unverified.

Run the checkpoint fixtures with:

```sh
dotnet test OpenSpeed.Classic.UnitTests/OpenSpeed.Classic.UnitTests.csproj \
  --filter FullyQualifiedName~UnitTests.Physics
```

Run the explicit original-asset driving tests separately:

```sh
OPENSPEED_TEST_ASSET_ROOT="/path/to/NFS2 SE" \
  dotnet test OpenSpeed.Classic.UnitTests/OpenSpeed.Classic.UnitTests.csproj \
  --filter FullyQualifiedName~PlayerVehicleSimulationTests
```

These tests exercise acceleration, steering direction, braking/reverse, coasting, handbrake response, airborne landing, frame-time independence, and sustained driving on original track data.

The original checkpoint and source discrepancies are recorded in section 27 of the supplied external `CAR_PHYSICS_ASM_SPEC.md`; the continuation above adds to that historical inventory. That external reference and its assembly remain outside this repository; they are not runtime dependencies.

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

The car uses original per-car drivetrain descriptors for acceleration, torque, gearing, and resistance. Steering acts through the front/rear contact forces rather than the old wheelbase-based yaw formula. The handbrake selects the ported rear-contact slip response. Opposite-direction input applies the service brake until the car is nearly stationary, then selects reverse or forward range. Track collision and recovery are subject to the provisional limitations documented above.

Asset file hits and misses are written by NuciLog to the console and, when `IsFileOutputEnabled` is enabled, to the configured `LogFilePath`.

## Licence

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE) for details.
