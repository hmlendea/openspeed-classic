using System;

using NUnit.Framework;

using OpenSpeed.Classic.Configuration;

namespace OpenSpeed.Classic.UnitTests.Configuration
{
    [TestFixture]
    public sealed class ApplicationArgumentsParserTests
    {
        [Test]
        public void GivenNoArguments_WhenParsing_ThenFrameCaptureIsDisabled()
        {
            ApplicationArguments arguments = ApplicationArgumentsParser.Parse([]);

            Assert.That(arguments.CaptureFramePath, Is.Null);
        }

        [TestCase("")]
        [TestCase(" ")]
        public void GivenABlankCaptureFramePath_WhenParsing_ThenFrameCaptureIsDisabled(
            string captureFramePath)
        {
            ApplicationArguments arguments = ApplicationArgumentsParser.Parse(
                ["--capture-frame", captureFramePath]);

            Assert.That(arguments.CaptureFramePath, Is.Null);
        }

        [Test]
        public void GivenACaptureFramePath_WhenParsing_ThenFrameCaptureIsConfigured()
        {
            ApplicationArguments arguments = ApplicationArgumentsParser.Parse(
                ["--capture-frame", "captures/frame.png"]);

            Assert.That(arguments.CaptureFramePath, Is.EqualTo("captures/frame.png"));
        }

        [Test]
        public void GivenAnUnknownArgument_WhenParsing_ThenTheArgumentIsRejected()
            => Assert.That(
                () => ApplicationArgumentsParser.Parse(["--minecraft", "true"]),
                Throws.TypeOf<ArgumentException>());

        [Test]
        public void GivenACaptureFrameArgumentWithoutAPath_WhenParsing_ThenTheArgumentIsRejected()
            => Assert.That(
                () => ApplicationArgumentsParser.Parse(["--capture-frame"]),
                Throws.TypeOf<ArgumentNullException>());

        [Test]
        public void GivenANullArgumentArray_WhenParsing_ThenTheArgumentIsRejected()
            => Assert.That(
                () => ApplicationArgumentsParser.Parse(null!),
                Throws.TypeOf<ArgumentNullException>());
    }
}
