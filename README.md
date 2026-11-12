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
- [RouteBasis](OpenSpeed.Classic/Physics/RouteBasis.cs): signed route-record loading, contact-mode and ordinary-mode refresh, and primary/secondary basis copying with right/forward reflection. Ordinary refresh reconstructs the right row from normal/forward and supports supplied signed-word extension directions. The native assertion/query dependency and extension buffer must be supplied; the complete placement callback remains unfinished.
- [AiMotion](OpenSpeed.Classic/Physics/AiMotion.cs): lateral-target smoothing, lateral-velocity calculation, and world-velocity composition. The scalar-speed controller and complete AI callback graph remain separate unfinished dependencies.
- [TyreResponseFinaliser](OpenSpeed.Classic/Physics/TyreResponseFinaliser.cs): response-state/surface scaling and final grip calculation with sign-preserving, direction-dependent clamps. The caller must supply the upstream requested force, wheel target, signed adjustment, and transition state; their missing calculation is not approximated.

[RouteContactPreparation](OpenSpeed.Classic/Physics/RouteContactPreparation.cs) also exposes the assembly-derived route-query dispatch (`sub_4633A0`): it passes the current native position and signed mode `1 - contactMode` to a caller-supplied query. The caller retains ownership of the native query context. The [preparation tests](OpenSpeed.Classic.UnitTests/Physics/RouteContactPreparationTests.cs) cover mode-byte boundaries, null dependencies, exception propagation, and absence of direct car-state writes. This wrapper does not implement polygon queries or activate native contact preparation in gameplay.

[ContactModeDistance](OpenSpeed.Classic/Physics/ContactModeDistance.cs) calculates the recovered contact-mode distance against one or two supplied active cars without mutating their state. In the two-car case it minimises planar distance and circular route distance independently before combining them. Active-car selection and complete native transition integration remain unfinished; this helper is not scheduled in gameplay.

[ContactModeTransition](OpenSpeed.Classic/Physics/ContactModeTransition.cs) implements the recovered distance threshold, entry stores, and exit sequence with explicit distance, route-query, basis-refresh, and contact-reset dependencies. [RouteContactReset](OpenSpeed.Classic/Physics/RouteContactReset.cs) implements the conditional reset wrapper, retaining its unresolved native correction predicate as a required callback. These components are tested independently, not scheduled in gameplay; active-car selection, native query geometry, and the correction predicate still prevent a complete native transition.

[PairCollisionResolver](OpenSpeed.Classic/Physics/PairCollisionResolver.cs) preserves native iterative contact/impulse ordering, including eight additional attempts, fallback callbacks, and the final contact query. [CollisionFallback](OpenSpeed.Classic/Physics/CollisionFallback.cs) supplies the recovered fallback impulse arithmetic and angular clamp with an injected impact notification. Native contact generation, motion refresh, and pair-completion race state remain required dependencies; these additions do not activate car-pair collisions in gameplay.

[CollisionMotionRefresh](OpenSpeed.Classic/Physics/CollisionMotionRefresh.cs) implements the correction-result dispatch and both native projection/response-reset paths. The alternate path recalculates engine speed for driven gears; the primary path preserves the supplied assembly's unreachable engine-update branch. The native route-correction routine remains an explicit required dependency, and original-execution parity remains unverified.

[ContactBasis](OpenSpeed.Classic/Physics/ContactBasis.cs) implements the native secondary-basis constructor: role/projection-selected copying or cross-product reconstruction, native vector normalisation, and preservation of the supplied normal. It does not replace provisional gameplay contact until the native contact caller and query geometry are available.

[PairCollisionRaceEvent](OpenSpeed.Classic/Physics/PairCollisionRaceEvent.cs) evaluates the native pair-completion race-event gate without mutating either car. The caller must latch a qualifying event into race state; a negative result must not clear an existing event. Native race-state ownership and gameplay pair-collision integration remain unfinished.

[WorldContactSupport](OpenSpeed.Classic/Physics/WorldContactSupport.cs) selects the native oriented support corner, including zero-projection signs and multiplication-before-negation rounding. [WorldContactResponse](OpenSpeed.Classic/Physics/WorldContactResponse.cs) exposes a separate world-plane response that performs penetration correction, inward-only contact impulses, vertical-energy adjustment, and angular damping/clamping. The existing provisional wall-response API remains unchanged; native plane detection and gameplay activation remain separate work.

[TrackRoadGeometry](OpenSpeed.Classic/Physics/TrackRoadGeometry.cs) now supplies finite polygon road detection to gameplay from loaded original track blocks. High-detail and unrestricted polygons are triangulated and converted back to native fixed-point coordinates; inclusive X/Z containment prevents infinite-plane contact beyond polygon edges. Route height selects among overlapping levels. Gameplay uses detected polygon planes for road height and slope; missing polygons produce airborne contact rather than a route-plane fallback. This is a provisional detector over original asset geometry, not a reconstruction of the executable's packed collision query. Route progression, corridor walls, centre-based grounding, material mapping, scenery eligibility, and collision triangulation remain provisional or unverified.

Geometry normals use wide integer edge/cross arithmetic before fixed-point normalisation so large road polygons do not overflow into missing planes. This protects the provisional detector; it is not evidence of matching the executable's collision-query arithmetic.

Road normals and inclusive horizontal bounds are cached at track-load time. Physics queries reject out-of-bounds triangles before finite containment tests and reuse cached planes rather than rebuilding normals every tick; candidate ordering and height tie behaviour remain unchanged.

When polygon geometry is available, a missing or non-upward route normal no longer prevents road detection. The route record's height supplies only the overlapping-level hint in that case; a valid polygon plane still supplies the contact normal. Route-only callers retain their existing normal gate.

Detected road heights use wide integer barycentric interpolation of the original triangle vertices, avoiding slope drift from rounded normals and wrapped coordinate differences on wide polygons. Solver normals remain fixed-point and this geometry adapter remains provisional.

In polygon-aware gameplay, road detection runs before provisional corridor-wall correction. A car outside every eligible road polygon is not snapped into the route corridor or given a wall impulse merely to manufacture a road hit. Route-only consumers retain their existing corridor behaviour.

[PairCollisionBroadPhase](OpenSpeed.Classic/Physics/PairCollisionBroadPhase.cs) implements the recovered car-pair broad phase: contact-mode and active-body gates, one-step cool-down decrement, the `0x640000` distance-metric suppression, signed axis rejection at the summed radii, per-pair angular pre/post scaling, angular clamping, and the closing motion refresh. Pair resolution is an injected dependency, so OBB contact generation and impulse application remain unfinished and car-pair collision is not yet active in gameplay.

[OrientedBoxScratchCalculator](OpenSpeed.Classic/Physics/OrientedBoxScratchCalculator.cs) precomputes the process-wide scratch matrices `sub_4543F0` builds before its candidate loop: row-to-row rotation dot products, per-axis extent projections on both boxes, and centre deltas expressed in each box's basis. [OrientedBoxScratch](OpenSpeed.Classic/Physics/OrientedBoxScratch.cs) holds those arrays and can be reset per pair. [OrientedBoxCandidate](OpenSpeed.Classic/Physics/OrientedBoxCandidate.cs) implements all four `sub_455100` candidate helpers. `sub_457CD0` probes from the first box's frame against the first box's extents and emits the second box's corner; `sub_4577D0` is the same probe plus the second box's positive world normal. `sub_457A10` mirrors it with a negated centre delta, the second box's extents, and the first box's corner; `sub_457590` is the same probe plus the first box's negated world normal. [OrientedBoxCandidateArithmetic](OpenSpeed.Classic/Physics/OrientedBoxCandidateArithmetic.cs) holds the shared probe, extent-test, corner, normal, and validation arithmetic. The `sub_455100` candidate ordering, impulse application, and the iterative pair loop remain unfinished; this stage is not scheduled in gameplay.

Asset loading retains `p<car resource>.dat` and `SimTune.dat` from the original car archive and unconverted XBID 15 track records. Gameplay finalises a private descriptor copy once and registers the player simulation callbacks. The three executable angle-table counts and SHA-256 hashes correspond exactly to the supplied specification. Missing physics archive members are reported as loading errors rather than replaced with invented values.

[PhysicsRenderAdapter](OpenSpeed.Classic/Rendering/Cars/PhysicsRenderAdapter.cs) converts native position/basis to MonoGame coordinates and exposes longitudinal speed to the camera, speedometer, and post-processing. Rendering never writes simulation state. The minimap reads the resulting car transform.

[RouteIndexSearch](OpenSpeed.Classic/Physics/RouteIndexSearch.cs) selects native route records with the original wrapped metrics, strict circular walk, and coarse fallback rather than scanning every segment. It also exposes the source circular index-distance calculation for contact-mode transition callers, preserving signed comparisons and wrapped arithmetic without clamping raw inputs. The complete native contact-mode transition remains unfinished. [PlayerStateInitialiser](OpenSpeed.Classic/Physics/PlayerStateInitialiser.cs) supplies recovered neutral startup, reset fields, and linear/angular coefficients. Forward/reverse input then selects the drive range.

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
