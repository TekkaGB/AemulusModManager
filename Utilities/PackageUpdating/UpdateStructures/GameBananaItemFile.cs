using AemulusModManager.Utilities.PackageUpdating;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace AemulusModManager
{
    public class GameBananaItemFile
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        [JsonProperty("_idRow")]
        public string ID { get; set; }
        [JsonProperty("_sFile")]
        public string FileName { get; set; }

        [JsonProperty("_nFilesize")]
        public long Filesize { get; set; }

        [JsonProperty("_sDownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("_sDescription")]
        public string Description { get; set; }

        [JsonProperty("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonProperty("_aMetadata")]
        [JsonExtensionData]
        public IDictionary<string, JToken> FileMetadata { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);

        [JsonIgnore]
        public string TimeSinceUpload => StringConverters.FormatTimeSpan(DateTime.UtcNow - DateAdded);
    }

}
