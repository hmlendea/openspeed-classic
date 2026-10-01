using System.Collections.Generic;

namespace OpenSpeed.Classic.Configuration
{
    public sealed class AssetsSettings
    {
        public IEnumerable<AssetSourceSettings> Sources { get; set; } = [];
    }
}