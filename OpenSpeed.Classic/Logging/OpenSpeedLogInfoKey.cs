using NuciLog.Core;

namespace OpenSpeed.Classic.Logging
{
    public sealed class OpenSpeedLogInfoKey : LogInfoKey
    {
        private OpenSpeedLogInfoKey(string name) : base(name) { }

        public static LogInfoKey FilePath => new OpenSpeedLogInfoKey(nameof(FilePath));
    }
}