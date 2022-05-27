using Newtonsoft.Json;
using System;

namespace AemulusModManager
{
    public class GameBananaItemUpdate
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        [JsonProperty("_sTitle")]
        public string Title { get; set; }
        [JsonProperty("_sVersion")]
        public string Version { get; set; }

        [JsonProperty("_aChangeLog")]
        public GameBananaItemUpdateChange[] Changes { get; set; }

        [JsonProperty("_sText")]
        public string Text { get; set; }

        [JsonProperty("_tsDateAdded")]
        public long DateAddedLong { get; set; }

        [JsonIgnore]
        public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);
    }
}