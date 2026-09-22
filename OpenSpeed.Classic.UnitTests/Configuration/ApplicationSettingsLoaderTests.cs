using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using Microsoft.Xna.Framework.Input;

using NUnit.Framework;

using OpenSpeed.Classic.Assets;
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
                        "RootDirectory": "/test-assets",
                        "TextureVariant": "PC"
                      }
                    ]
                  },
                  "StartupTrack": {
                    "Game": "NeedForSpeed2SpecialEdition",
                    "Identifier": "Outback"
                  },
                  "StartupCar": {
                    "Game": "NeedForSpeed2SpecialEdition",
                    "Identifier": "FerrariF50"
                  },
                  "Rendering": {
                    "AreShadowsEnabled": false,
                    "Is3DfxEnabled": true
                  },
                  "Controls": {
                    "Accelerate": {
                      "Primary": "Space",
                      "Secondary": "Enter"
                    },
                    "Brake": {
                      "Primary": "Down",
                      "Secondary": "S"
                    },
                    "SteerLeft": {
                      "Primary": "Left",
                      "Secondary": "A"
                    },
                    "SteerRight": {
                      "Primary": "Right",
                      "Secondary": "D"
                    }
                  }
                }
                """);

            ApplicationSettings settings = settingsLoader.Load(filePath);
            AssetSourceSettings assetSource = settings.Assets.Sources.Single();

            Assert.Multiple(() =>
            {
                Assert.That(assetSource.Game, Is.EqualTo("NeedForSpeed2SpecialEdition"));
                Assert.That(assetSource.RootDirectory, Is.EqualTo("/test-assets"));
                Assert.That(assetSource.TextureVariant, Is.EqualTo(TrackTextureVariant.PC));
                Assert.That(
                    settings.StartupTrack.Game,
                    Is.EqualTo("NeedForSpeed2SpecialEdition"));
                Assert.That(settings.StartupTrack.Identifier, Is.EqualTo("Outback"));
                Assert.That(
                  settings.StartupCar.Game,
                  Is.EqualTo("NeedForSpeed2SpecialEdition"));
                Assert.That(settings.StartupCar.Identifier, Is.EqualTo("FerrariF50"));
                Assert.That(settings.Rendering.AreShadowsEnabled, Is.False);
                Assert.That(settings.Rendering.Is3DfxEnabled, Is.True);
                Assert.That(settings.Controls.Accelerate.Primary, Is.EqualTo(Keys.Space));
                Assert.That(settings.Controls.Accelerate.Secondary, Is.EqualTo(Keys.Enter));
            });
        }

        [Test]
        public void GivenNoTextureVariant_WhenLoading_ThenSpecialEditionTexturesAreSelected()
        {
            string filePath = WriteSettings(
                BuildSettingsJson(
                    "NeedForSpeed2SpecialEdition",
                    "/test-assets",
                    "NeedForSpeed2SpecialEdition",
                    "Outback"));

            ApplicationSettings settings = settingsLoader.Load(filePath);

            Assert.That(
                settings.Assets.Sources.Single().TextureVariant,
                Is.EqualTo(TrackTextureVariant.SE));
            Assert.That(settings.Rendering.AreShadowsEnabled);
            Assert.That(settings.Rendering.Is3DfxEnabled, Is.Null);
            Assert.That(settings.StartupCar.Identifier, Is.EqualTo("McLarenF1"));
            Assert.That(settings.Controls.Accelerate.Primary, Is.EqualTo(Keys.Up));
            Assert.That(settings.Controls.Accelerate.Secondary, Is.EqualTo(Keys.W));
        }

        [TestCase("None", "W")]
        [TestCase("Up", "Up")]
        public void GivenInvalidControlBindings_WhenLoading_ThenInvalidDataIsReported(
            string primary,
            string secondary)
        {
            string filePath = WriteSettings(
                $$"""
                {
                  "Assets": {
                    "Sources": [
                      {
                        "Game": "NeedForSpeed2SpecialEdition",
                        "RootDirectory": "/test-assets"
                      }
                    ]
                  },
                  "Controls": {
                    "Accelerate": {
                      "Primary": "{{primary}}",
                      "Secondary": "{{secondary}}"
                    }
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

        [Test]
        public void GivenAnUnknownControlKey_WhenLoading_ThenJsonExceptionIsThrown()
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
                  "Controls": {
                    "Accelerate": {
                      "Primary": "Banana",
                      "Secondary": "W"
                    }
                  },
                  "StartupTrack": {
                    "Game": "NeedForSpeed2SpecialEdition",
                    "Identifier": "Outback"
                  }
                }
                """);

            Assert.That(
                () => settingsLoader.Load(filePath),
                Throws.TypeOf<JsonException>());
        }

        [TestCase("\"Minecraft\"")]
        [TestCase("\" \"")]
        [TestCase("42")]
        [TestCase("null")]
        public void GivenAnInvalidTextureVariant_WhenLoading_ThenJsonExceptionIsThrown(
            string textureVariant)
        {
            string filePath = WriteSettings(
                $$"""
                {
                  "Assets": {
                    "Sources": [
                      {
                        "Game": "NeedForSpeed2SpecialEdition",
                        "RootDirectory": "/test-assets",
                        "TextureVariant": {{textureVariant}}
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
                Throws.TypeOf<JsonException>());
        }

        [Test]
        public void GivenAMissingSettingsFile_WhenLoading_ThenFileNotFoundIsReported()
            => Assert.That(
                () => settingsLoader.Load(Path.Combine(testDirectory, "absent.json")),
                Throws.TypeOf<FileNotFoundException>());

        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{ \"Assets\": null }")]
        [TestCase("{ \"Assets\": { \"Sources\": [] }, \"Rendering\": null }")]
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

        [TestCase("Minecraft", "McLarenF1")]
        [TestCase("NeedForSpeed2SpecialEdition", " ")]
        [TestCase("NeedForSpeed2SpecialEdition", "ReliantRobin")]
        public void GivenAnInvalidStartupCar_WhenLoading_ThenInvalidDataIsReported(
            string game,
            string identifier)
        {
            string filePath = WriteSettings(
                $$"""
                {
                  "Assets": {
                    "Sources": [
                      {
                        "Game": "NeedForSpeed2SpecialEdition",
                        "RootDirectory": "/test-assets"
                      }
                    ]
                  },
                  "StartupCar": {
                    "Game": "{{game}}",
                    "Identifier": "{{identifier}}"
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
