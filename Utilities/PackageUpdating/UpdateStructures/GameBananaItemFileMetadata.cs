using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace AemulusModManager.Utilities.PackageUpdating.UpdateStructures
{
    public class GameBananaItemFileMetadata
    {
        [JsonProperty("_sMimeType")]
        public string FileType { set; get; }

        [JsonProperty("_aArchiveFileTree")]
        [JsonExtensionData]
        public IDictionary<string, JToken> FileTree { get; set; }
    }
}
