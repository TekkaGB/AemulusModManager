using Newtonsoft.Json;
using System.Collections.Generic;

namespace AemulusModManager
{
    /* Disclaimer: These classes are taken (slightly modified) from Reloaded II's Gamebanana Resolver*/
    class GameBananaItem
    {
        [JsonProperty("Updates().bSubmissionHasUpdates()")]
        public bool HasUpdates { get; set; }

        [JsonProperty("Updates().aGetLatestUpdates()")]
        public GameBananaItemUpdate[] Updates { get; set; }

        [JsonProperty("Files().aFiles()")]
        public List<GameBananaItemFile> Files { get; set; }

    }
}
