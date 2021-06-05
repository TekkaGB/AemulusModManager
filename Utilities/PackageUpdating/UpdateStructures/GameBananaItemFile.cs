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
        [JsonIgnore]
        public string ConvertedFileSize => StringConverters.FormatSize(Filesize);

        [JsonProperty("_sDownloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("_sDescription")]
        public string Description { get; set; }
        [JsonProperty("_bContainsExe")]
        public bool ContainsExe { get; set; }
        [JsonProperty("_nDownloadCount")]
        public int Downloads { get; set; }
        [JsonIgnore]
        public string DownloadString => StringConverters.FormatNumber(Downloads);
        
        [JsonProperty("_aMetadata")]
        [JsonExtensionData]
        public IDictionary<string, JToken> FileMetadata { get; set; }

        [JsonProperty("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);

        [JsonIgnore]
        public string TimeSinceUpload => StringConverters.FormatTimeAgo(DateTime.UtcNow - DateAdded);
    }

    public class GithubFile
    {
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
        public int Downloads { get; set; }
        public string DownloadString => StringConverters.FormatNumber(Downloads);
        public long Filesize { get; set; }
        public string ConvertedFileSize => StringConverters.FormatSize(Filesize);
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public string TimeSinceUpload => StringConverters.FormatTimeAgo(DateTime.UtcNow - DateAdded);
    }
}
