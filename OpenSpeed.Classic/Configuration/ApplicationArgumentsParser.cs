using System;

using NuciCLI.Arguments;

namespace OpenSpeed.Classic.Configuration
{
    public static class ApplicationArgumentsParser
    {
        private static string CaptureFrameArgumentName => "capture-frame";

        public static ApplicationArguments Parse(string[] arguments)
        {
            ArgumentNullException.ThrowIfNull(arguments);

            ArgumentParser parser = new();
            parser.AddArgument(
                CaptureFrameArgumentName,
                help: "Capture one rendered frame to the specified PNG path, then exit.",
                defaultValue: string.Empty);
            ArgumentsCollection parsedArguments = parser.ParseArgs(arguments);
            string captureFramePath = parsedArguments.Get<string>(
                CaptureFrameArgumentName);
            ApplicationArguments applicationArguments = new();

            if (!string.IsNullOrWhiteSpace(captureFramePath))
            {
                applicationArguments.CaptureFramePath = captureFramePath;
            }

            return applicationArguments;
        }
    }
}
