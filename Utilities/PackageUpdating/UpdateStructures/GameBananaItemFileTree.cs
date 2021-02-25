using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace AemulusModManager.Utilities.PackageUpdating.UpdateStructures
{
    public class GameBananaItemFileTree
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> Contents { get; set; }
    }
}
