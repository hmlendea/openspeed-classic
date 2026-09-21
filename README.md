# OpenSpeed Classic

OpenSpeed Classic is a .NET 10 arcade racing game skeleton built on MonoGame via the NuciXNA package family.

## Project Structure

- [OpenSpeed.Classic.slnx](OpenSpeed.Classic.slnx) contains the solution.
- [OpenSpeed.Classic](OpenSpeed.Classic) contains the game executable project.
- [OpenSpeed.Classic.UnitTests](OpenSpeed.Classic.UnitTests) contains the NUnit unit test project.
- [OpenSpeed.Classic/OpenSpeedClassicGame.cs](OpenSpeed.Classic/OpenSpeedClassicGame.cs) contains the MonoGame game shell.

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