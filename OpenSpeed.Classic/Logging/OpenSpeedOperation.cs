using NuciLog.Core;

namespace OpenSpeed.Classic.Logging
{
    public sealed class OpenSpeedOperation : Operation
    {
        private OpenSpeedOperation(string name) : base(name) { }

        public static Operation ResolveAssetFile
            => new OpenSpeedOperation(nameof(ResolveAssetFile));
    }
}