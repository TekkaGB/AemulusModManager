using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AemulusModManager
{
    /* Disclaimer: These classes are taken (slightly modified) from Reloaded II's Gamebanana Resolver*/
    public class GameBananaItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("Game().name")]
        public string Game { get; set; }
        [JsonProperty("Updates().bSubmissionHasUpdates()")]
        public bool HasUpdates { get; set; }

        [JsonProperty("Updates().aGetLatestUpdates()")]
        public GameBananaItemUpdate[] Updates { get; set; }

        [JsonProperty("Files().aFiles()")]
        public Dictionary<string, GameBananaItemFile> Files { get; set; }
        [JsonProperty("Preview().sSubFeedImageUrl()")]
        public Uri SubFeedImage { get; set; }

    }
}
