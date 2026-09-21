# OpenSpeed Classic

OpenSpeed Classic is a .NET 10 arcade racing game built on MonoGame via the NuciXNA package family. It loads track geometry, materials, and textures from an original Need for Speed II Special Edition installation.

## Project Structure

- [OpenSpeed.Classic.slnx](OpenSpeed.Classic.slnx) contains the solution.
- [OpenSpeed.Classic](OpenSpeed.Classic) contains the game executable project.
- [OpenSpeed.Classic.UnitTests](OpenSpeed.Classic.UnitTests) contains the NUnit unit test project.
- [OpenSpeed.Classic/OpenSpeedClassicGame.cs](OpenSpeed.Classic/OpenSpeedClassicGame.cs) contains the MonoGame game shell.
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
				"RootDirectory": "/path/to/NFS2 SE"
			}
		]
	},
	"StartupTrack": {
		"Game": "NeedForSpeed2SpecialEdition",
		"Identifier": "Outback"
	}
}
```

Relative asset roots are resolved from the process working directory. Paths within an original Windows installation are resolved case-insensitively on every platform.

Supported track identifiers are `ProvingGrounds`, `Outback`, `LastResort`, `NorthCountry`, `PacificSpirit`, `Mediterraneo`, `MysticPeaks`, and `MonolithicStudios`.

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

## Licence

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE) for details.