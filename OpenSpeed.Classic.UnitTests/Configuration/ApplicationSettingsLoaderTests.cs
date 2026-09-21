using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

using OpenSpeed.Classic.Configuration;

namespace OpenSpeed.Classic.UnitTests.Configuration
{
    [TestFixture]
    public sealed class ApplicationSettingsLoaderTests
    {
        private IApplicationSettingsLoader settingsLoader = null!;
        private string testDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            settingsLoader = new ApplicationSettingsLoader();
            testDirectory = Path.Combine(
                Path.GetTempPath(),
                $"openspeed-settings-{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void GivenValidSettings_WhenLoading_ThenTheAssetSourceAndTrackAreReturned()
        {
            string filePath = WriteSettings(
                """
                {
                  "Assets": {
                    "Sources": [
                      {
                        "Game": "NeedForSpeed2SpecialEdition",
                        "RootDirectory": "/test-assets"
                      }
                    ]
                  },
                  "StartupTrack": {
                    "Game": "NeedForSpeed2SpecialEdition",
                    "Identifier": "Outback"
                  }
                }
                """);

            ApplicationSettings settings = settingsLoader.Load(filePath);
            AssetSourceSettings assetSource = settings.Assets.Sources.Single();

            Assert.Multiple(() =>
            {
                Assert.That(assetSource.Game, Is.EqualTo("NeedForSpeed2SpecialEdition"));
                Assert.That(assetSource.RootDirectory, Is.EqualTo("/test-assets"));
                Assert.That(
                    settings.StartupTrack.Game,
                    Is.EqualTo("NeedForSpeed2SpecialEdition"));
                Assert.That(settings.StartupTrack.Identifier, Is.EqualTo("Outback"));
            });
        }

        [Test]
        public void GivenAMissingSettingsFile_WhenLoading_ThenFileNotFoundIsReported()
            => Assert.That(
                () => settingsLoader.Load(Path.Combine(testDirectory, "absent.json")),
                Throws.TypeOf<FileNotFoundException>());

        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{ \"Assets\": null }")]
        public void GivenMissingSettings_WhenLoading_ThenInvalidDataIsReported(string json)
        {
            string filePath = WriteSettings(json);

            Assert.That(
                () => settingsLoader.Load(filePath),
                Throws.TypeOf<InvalidDataException>());
        }

        [TestCase("Minecraft", "/test-assets")]
        [TestCase("NeedForSpeed2SpecialEdition", " ")]
        public void GivenAnInvalidAssetSource_WhenLoading_ThenInvalidDataIsReported(
            string game,
            string rootDirectory)
        {
            string filePath = WriteSettings(
                BuildSettingsJson(game, rootDirectory, "NeedForSpeed2SpecialEdition", "Outback"));

            Assert.That(
                () => settingsLoader.Load(filePath),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void GivenDuplicateAssetSources_WhenLoading_ThenInvalidDataIsReported()
        {
            string filePath = WriteSettings(
                """
                {
                  "Assets": {
                    "Sources": [
                      {
                        "Game": "NeedForSpeed2SpecialEdition",
                        "RootDirectory": "/test-assets"
                      },
                      {
                        "Game": "NeedForSpeed2SpecialEdition",
                        "RootDirectory": "/other-assets"
                      }
                    ]
                  },
                  "StartupTrack": {
                    "Game": "NeedForSpeed2SpecialEdition",
                    "Identifier": "Outback"
                  }
                }
                """);

            Assert.That(
                () => settingsLoader.Load(filePath),
                Throws.TypeOf<InvalidDataException>());
        }

        [TestCase("Minecraft", "Outback")]
        [TestCase("NeedForSpeed2SpecialEdition", " ")]
        public void GivenAnInvalidStartupTrack_WhenLoading_ThenInvalidDataIsReported(
            string game,
            string identifier)
        {
            string filePath = WriteSettings(
                BuildSettingsJson(
                    "NeedForSpeed2SpecialEdition",
                    "/test-assets",
                    game,
                    identifier));

            Assert.That(
                () => settingsLoader.Load(filePath),
                Throws.TypeOf<InvalidDataException>());
        }

        private string WriteSettings(string json)
        {
            string filePath = Path.Combine(testDirectory, "appsettings.json");
            File.WriteAllText(filePath, json);

            return filePath;
        }

        private static string BuildSettingsJson(
            string sourceGame,
            string rootDirectory,
            string startupGame,
            string trackIdentifier)
            => $$"""
                {
                  "Assets": {
                    "Sources": [
                      {
                        "Game": "{{sourceGame}}",
                        "RootDirectory": "{{rootDirectory}}"
                      }
                    ]
                  },
                  "StartupTrack": {
                    "Game": "{{startupGame}}",
                    "Identifier": "{{trackIdentifier}}"
                  }
                }
                """;
    }
}