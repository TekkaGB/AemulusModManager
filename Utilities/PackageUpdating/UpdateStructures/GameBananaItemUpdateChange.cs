using Newtonsoft.Json;

namespace AemulusModManager
{
    public class GameBananaItemUpdateChange
    {
        [JsonProperty("cat")]
        public string Category { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

}
